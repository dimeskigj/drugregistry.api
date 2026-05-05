using DrugRegistry.API.Database;
using DrugRegistry.API.Extensions;
using DrugRegistry.API.Jobs;
using DrugRegistry.API.Scraping;
using DrugRegistry.API.Services;
using DrugRegistry.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Quartz;

var builder = WebApplication.CreateBuilder(args);
var dbConnectionString = builder.Configuration.GetConnectionString("Database");

builder.Services
    .AddEndpointsApiExplorer()
    .AddSwaggerGen()
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
app.UseSwaggerUI();

using (var serviceScope = app.Services.CreateScope())
{
    var services = serviceScope.ServiceProvider;

    var schedulerFactory = services.GetRequiredService<ISchedulerFactory>();
    var scheduler = await schedulerFactory.GetScheduler();
    await scheduler.ScheduleJobs(Jobs.JobsDictionary, true);

    var dbContext = services.GetRequiredService<AppDbContext>();

    if (!dbContext.Drugs.Any()) await scheduler.TriggerJob(Jobs.DrugScrapingJobDetail.Key);

    if (!dbContext.Pharmacies.Any()) await scheduler.TriggerJob(Jobs.PharmacyScrapingJobDetail.Key);

    if (!dbContext.DrugEanData.Any())
    {
        var seeder = services.GetRequiredService<CsvEanSeeder>();
        await seeder.SeedAsync();
    }
}


app.MapEndpoints().Run();