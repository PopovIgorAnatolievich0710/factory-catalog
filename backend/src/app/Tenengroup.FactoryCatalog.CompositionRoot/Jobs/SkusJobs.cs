using Hangfire;
using Microsoft.Extensions.Configuration;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Sync;

namespace Tenengroup.FactoryCatalog.CompositionRoot.Jobs;

public static class SkusJobs
{
    public const string MasterCatalogSyncJobId = "master-catalog-skus-sync";

    public static void RegisterSkusJobs(this IRecurringJobManager recurringJobs, IConfiguration configuration)
    {
        var cronExpression = configuration.GetSection(MasterCatalogOptions.SectionName)[nameof(MasterCatalogOptions.CronExpression)];

        recurringJobs.AddOrUpdate<IMasterCatalogSyncJob>(
            MasterCatalogSyncJobId,
            job => job.Run(CancellationToken.None),
            string.IsNullOrWhiteSpace(cronExpression) ? MasterCatalogOptions.DefaultCronExpression : cronExpression,
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
    }
}
