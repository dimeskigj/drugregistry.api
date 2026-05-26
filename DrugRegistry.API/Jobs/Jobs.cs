using Quartz;

namespace DrugRegistry.API.Jobs;

public static class Jobs
{
    private const string EverySundayAt2300 = "0 0 23 ? * SUN *";
    private const string EveryWednesdayAt2300 = "0 0 23 ? * WED *";

    public static readonly IJobDetail DrugScrapingJobDetail = JobBuilder
        .Create<DrugScrapingJob>()
        .WithIdentity(Constants.Quartz.DrugScrapingJobName)
        .Build();

    private static readonly ITrigger DrugScrapingTrigger = TriggerBuilder
        .Create()
        .ForJob(DrugScrapingJobDetail)
        .WithIdentity(Constants.Quartz.DrugScrapingTriggerName)
        .WithCronSchedule(EverySundayAt2300)
        .Build();

    public static readonly IJobDetail PharmacyScrapingJobDetail = JobBuilder
        .Create<PharmacyScrapingJob>()
        .WithIdentity(Constants.Quartz.PharmacyScrapingJobName)
        .Build();

    private static readonly ITrigger PharmacyScrapingTrigger = TriggerBuilder
        .Create()
        .ForJob(PharmacyScrapingJobDetail)
        .WithIdentity(Constants.Quartz.PharmacyScrapingTriggerName)
        .WithCronSchedule(EveryWednesdayAt2300)
        .Build();

    public static readonly Dictionary<IJobDetail, IReadOnlyCollection<ITrigger>> JobsDictionary =
        new()
        {
            { DrugScrapingJobDetail, new[] { DrugScrapingTrigger } },
            { PharmacyScrapingJobDetail, new[] { PharmacyScrapingTrigger } }
        };
}