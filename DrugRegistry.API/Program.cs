using System.Net;
using System.Threading.RateLimiting;
using DrugRegistry.API;
using DrugRegistry.API.Database;
using DrugRegistry.API.Extensions;
using DrugRegistry.API.Jobs;
using DrugRegistry.API.Scraping;
using DrugRegistry.API.Services;
using DrugRegistry.API.Services.Interfaces;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Scalar.AspNetCore;
using IPNetwork = System.Net.IPNetwork;

var builder = WebApplication.CreateBuilder(args);
var dbConnectionString = builder.Configuration.GetConnectionString("Database");
if (string.IsNullOrWhiteSpace(dbConnectionString))
    throw new InvalidOperationException("ConnectionStrings:Database must be configured.");

builder.Services
    .AddEndpointsApiExplorer()
    .AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        };
    })
    .AddOpenApi("v2", options => { options.ShouldInclude = IsV2ApiDescription; })
    .AddOpenApi("v1", options =>
    {
        options.ShouldInclude = IsV1ApiDescription;
        options.AddOperationTransformer((operation, _, _) =>
        {
            operation.Deprecated = true;
            return Task.CompletedTask;
        });
    })
    .AddDbContextFactory<AppDbContext>(options => options.UseNpgsql(dbConnectionString))
    .RegisterServices()
    .AddHttpClient()
    .AddCors(options =>
    {
        options.AddPolicy(ApiLimits.Cors.PolicyName, policy =>
        {
            var allowedOrigins = GetAllowedOrigins(builder.Configuration);
            if (allowedOrigins.Length == 0)
            {
                policy.SetIsOriginAllowed(_ => false);
                return;
            }

            policy
                .WithOrigins(allowedOrigins)
                .WithMethods("GET", "HEAD", "OPTIONS")
                .AllowAnyHeader();
        });
    })
    .AddOutputCache(options =>
    {
        options.AddPolicy(ApiLimits.CachePolicies.List,
            policy => policy.Expire(TimeSpan.FromMinutes(2)).SetVaryByQuery("*"));
        options.AddPolicy(ApiLimits.CachePolicies.Detail,
            policy => policy.Expire(TimeSpan.FromMinutes(10)).SetVaryByRouteValue("*"));
        options.AddPolicy(ApiLimits.CachePolicies.Lookup,
            policy => policy.Expire(TimeSpan.FromMinutes(30)).SetVaryByRouteValue("*"));
    })
    .AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy(ApiLimits.RateLimitPolicies.PublicApi, httpContext =>
            CreateFixedWindowPartition(httpContext, 120));
        options.AddPolicy(ApiLimits.RateLimitPolicies.Docs, httpContext =>
            CreateFixedWindowPartition(httpContext, 30));
        options.AddPolicy(ApiLimits.RateLimitPolicies.Health, httpContext =>
            CreateFixedWindowPartition(httpContext, 60));
    })
    .Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        ConfigureKnownForwarders(builder.Configuration, options);
    })
    .AddQuartz(q => q.UseMicrosoftDependencyInjectionJobFactory())
    .AddQuartzHostedService(opt => opt.WaitForJobsToComplete = false)
    .AddScoped<IGeocodingService, EmptyGeocodingService>()
    .AddScoped<DrugScraper>()
    .AddScoped<PharmacyScraper>()
    .AddScoped<CsvEanSeeder>();

var app = builder.Build();

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

app.UseForwardedHeaders();

app.Use(async (context, next) =>
{
    var scheme = context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? context.Request.Scheme;
    context.Request.Scheme = scheme;
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}

// Only use HTTPS redirection in development; in production, the reverse proxy handles this
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors(ApiLimits.Cors.PolicyName);
app.UseRateLimiter();
app.UseOutputCache();

app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }))
    .ExcludeFromDescription()
    .RequireRateLimiting(ApiLimits.RateLimitPolicies.Health);

app.MapGet("/health/ready", async (
        IDbContextFactory<AppDbContext> dbContextFactory,
        CancellationToken cancellationToken) =>
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Database.CanConnectAsync(cancellationToken)
            ? Results.Ok(new { status = "Ready" })
            : Results.Problem(
                title: "Service unavailable",
                detail: "Database is not reachable.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
    })
    .ExcludeFromDescription()
    .RequireRateLimiting(ApiLimits.RateLimitPolicies.Health);

app.MapOpenApi()
    .RequireRateLimiting(ApiLimits.RateLimitPolicies.Docs);

app.MapScalarApiReference("/docs", options => options
        .WithTitle("DrugRegistry API")
        .WithOpenApiRoutePattern("/openapi/{documentName}.json")
        .AddDocument("v2", "DrugRegistry API V2", "/openapi/v2.json", true)
        .AddDocument("v1", "DrugRegistry API V1 (Deprecated)", "/openapi/v1.json"))
    .RequireRateLimiting(ApiLimits.RateLimitPolicies.Docs);

using (var serviceScope = app.Services.CreateScope())
{
    var services = serviceScope.ServiceProvider;

    var schedulerFactory = services.GetRequiredService<ISchedulerFactory>();
    var scheduler = await schedulerFactory.GetScheduler();
    await scheduler.ScheduleJobs(Jobs.JobsDictionary, true);

    if (builder.Configuration.GetValue("DataIngestion:RunBootstrapOnStartup", false))
    {
        var dbContext = services.GetRequiredService<AppDbContext>();

        if (!dbContext.Drugs.Any()) await scheduler.TriggerJob(Jobs.DrugScrapingJobDetail.Key);

        if (!dbContext.Pharmacies.Any()) await scheduler.TriggerJob(Jobs.PharmacyScrapingJobDetail.Key);

        var seeder = services.GetRequiredService<CsvEanSeeder>();
        await seeder.SeedAsync();
    }
}


app.MapEndpoints().Run();

static bool IsV2ApiDescription(ApiDescription apiDescription)
{
    var normalizedPath = apiDescription.RelativePath?.TrimStart('/');
    return normalizedPath?.StartsWith("api/v2/", StringComparison.OrdinalIgnoreCase) == true;
}

static bool IsV1ApiDescription(ApiDescription apiDescription)
{
    var normalizedPath = apiDescription.RelativePath?.TrimStart('/');
    return normalizedPath?.StartsWith("api/", StringComparison.OrdinalIgnoreCase) == true &&
           !normalizedPath.StartsWith("api/v2/", StringComparison.OrdinalIgnoreCase);
}

static string[] GetAllowedOrigins(IConfiguration configuration)
{
    return GetConfiguredValues(configuration, "Cors:AllowedOrigins");
}

static void ConfigureKnownForwarders(IConfiguration configuration, ForwardedHeadersOptions options)
{
    foreach (var knownProxy in GetConfiguredValues(configuration, "ForwardedHeaders:KnownProxies"))
    {
        if (!IPAddress.TryParse(knownProxy, out var proxyAddress))
            throw new InvalidOperationException(
                $"ForwardedHeaders:KnownProxies contains invalid IP address '{knownProxy}'.");

        options.KnownProxies.Add(proxyAddress);
    }

    foreach (var knownNetwork in GetConfiguredValues(configuration, "ForwardedHeaders:KnownNetworks"))
    {
        if (!IPNetwork.TryParse(knownNetwork, out var proxyNetwork))
            throw new InvalidOperationException(
                $"ForwardedHeaders:KnownNetworks contains invalid CIDR network '{knownNetwork}'.");

        options.KnownIPNetworks.Add(proxyNetwork);
    }
}

static string[] GetConfiguredValues(IConfiguration configuration, string key)
{
    var configuredValues = configuration.GetSection(key).Get<string[]>();
    if (configuredValues is { Length: > 0 })
        return configuredValues.Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    var commaSeparatedValues = configuration[key];
    return string.IsNullOrWhiteSpace(commaSeparatedValues)
        ? []
        : commaSeparatedValues.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}

static RateLimitPartition<string> CreateFixedWindowPartition(HttpContext httpContext, int permitLimit)
{
    var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    return RateLimitPartition.GetFixedWindowLimiter(
        partitionKey,
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
}