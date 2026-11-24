using Hangfire;
using Microsoft.Extensions.Configuration;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Sync;
using Tenengroup.FactoryCatalog.CompositionRoot.Modules;

namespace Tenengroup.FactoryCatalog.CompositionRoot.Jobs;

public static class SkusJobs
{
    public const string MasterCatalogSyncJobId = "master-catalog-skus-sync";
    public const string DefaultMasterCatalogCron = "0 3 * * *";

    public static void RegisterSkusJobs(this IRecurringJobManager recurringJobs, IConfiguration configuration)
    {
        var cron = configuration.GetSection(SkusModule.MasterCatalogSection)["CronExpression"];
        recurringJobs.AddOrUpdate<IMasterCatalogSyncJob>(
            MasterCatalogSyncJobId,
            job => job.Run(CancellationToken.None),
            string.IsNullOrWhiteSpace(cron) ? DefaultMasterCatalogCron : cron);
    }
}
