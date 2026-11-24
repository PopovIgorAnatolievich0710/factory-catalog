using Hangfire;
using Microsoft.Extensions.Configuration;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components.Sync;

namespace Tenengroup.FactoryCatalog.CompositionRoot.Jobs;

public static class ComponentsJobs
{
    public const string SapComponentsSyncJobId = "sap-components-sync";

    public static void RegisterComponentsJobs(this IRecurringJobManager recurringJobs, IConfiguration configuration)
    {
        var cronExpression = configuration.GetSection(SapExportOptions.SectionName)[nameof(SapExportOptions.CronExpression)];

        recurringJobs.AddOrUpdate<ISapComponentsSyncJob>(
            SapComponentsSyncJobId,
            job => job.Run(CancellationToken.None),
            string.IsNullOrWhiteSpace(cronExpression) ? SapExportOptions.DefaultCronExpression : cronExpression,
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
    }
}
