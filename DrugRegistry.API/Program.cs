using DrugRegistry.API.Database;
using DrugRegistry.API.Extensions;
using DrugRegistry.API.Jobs;
using DrugRegistry.API.Scraping;
using DrugRegistry.API.Services;
using DrugRegistry.API.Services.Interfaces;
using DrugRegistry.API.Swagger;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Quartz;

var builder = WebApplication.CreateBuilder(args);
var dbConnectionString = builder.Configuration.GetConnectionString("Database");

builder.Services
    .AddEndpointsApiExplorer()
    .AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v2", new OpenApiInfo
        {
            Title = "DrugRegistry API V2",
            Version = "v2"
        });
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "DrugRegistry API V1 (Deprecated)",
            Version = "v1"
        });
        options.DocInclusionPredicate((docName, apiDescription) =>
        {
            var relativePath = apiDescription.RelativePath;
            if (string.IsNullOrWhiteSpace(relativePath)) return false;

            var normalizedPath = relativePath.TrimStart('/');
            var isApiPath = normalizedPath.StartsWith("api/", StringComparison.OrdinalIgnoreCase);
            var isV2Path = normalizedPath.StartsWith("api/v2/", StringComparison.OrdinalIgnoreCase);

            if (!isApiPath) return false;

            return docName switch
            {
                "v2" => isV2Path,
                "v1" => !isV2Path,
                _ => false
            };
        });
        options.OperationFilter<V1DeprecatedOperationFilter>();
    })
    .AddDbContextFactory<AppDbContext>(options => options.UseNpgsql(dbConnectionString))
    .RegisterServices()
    .AddHttpClient()
    .AddQuartz(q => q.UseMicrosoftDependencyInjectionJobFactory())
    .AddQuartzHostedService(opt => opt.WaitForJobsToComplete = false)
    .AddScoped<IGeocodingService, EmptyGeocodingService>()
    .AddScoped<DrugScraper>()
    .AddScoped<PharmacyScraper>()
    .AddScoped<CsvEanSeeder>();

var app = builder.Build();

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v2/swagger.json", "DrugRegistry API V2");
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "DrugRegistry API V1 (Deprecated)");
});

using (var serviceScope = app.Services.CreateScope())
{
    var services = serviceScope.ServiceProvider;

    var schedulerFactory = services.GetRequiredService<ISchedulerFactory>();
    var scheduler = await schedulerFactory.GetScheduler();
    await scheduler.ScheduleJobs(Jobs.JobsDictionary, true);

    var dbContext = services.GetRequiredService<AppDbContext>();

    if (!dbContext.Drugs.Any()) await scheduler.TriggerJob(Jobs.DrugScrapingJobDetail.Key);

    if (!dbContext.Pharmacies.Any()) await scheduler.TriggerJob(Jobs.PharmacyScrapingJobDetail.Key);

    var seeder = services.GetRequiredService<CsvEanSeeder>();
    await seeder.SeedAsync();
}


app.MapEndpoints().Run();
