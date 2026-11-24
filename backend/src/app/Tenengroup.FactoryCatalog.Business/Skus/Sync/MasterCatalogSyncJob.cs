using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Sync;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Sync;
using Tenengroup.FactoryCatalog.Business.Skus.Validation;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Exceptions;

namespace Tenengroup.FactoryCatalog.Business.Skus.Sync;

public class MasterCatalogSyncJob(
    IFactoryCatalogDbContext db,
    IMasterCatalogClient client,
    ISyncRunTracker syncRunTracker,
    MasterCatalogSkuImporter importer,
    SkuMappingValidator validator,
    ILogger<MasterCatalogSyncJob> logger)
    : IMasterCatalogSyncJob
{
    public const string ScheduleTrigger = "schedule";
    public const int PageSize = 500;

    public async Task Run(CancellationToken cancellationToken)
    {
        long runId;
        try
        {
            runId = await syncRunTracker.Start(SyncTypes.MasterCatalogSkus, ScheduleTrigger, cancellationToken);
        }
        catch (ConflictException)
        {
            logger.LogWarning("Scheduled master catalog sync skipped: a sync is already running");
            return;
        }

        await RunStarted(runId, cancellationToken);
    }

    public async Task RunStarted(long runId, CancellationToken cancellationToken)
    {
        try
        {
            var changedSince = await db.SyncRuns
                .AsNoTracking()
                .Where(r => r.SyncType == SyncTypes.MasterCatalogSkus && r.Status == SyncRunStatus.Succeeded)
                .OrderByDescending(r => r.StartedAt)
                .Select(r => (DateTime?)r.StartedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var added = 0;
            var updated = 0;
            var failed = 0;
            var hasMore = true;
            for (var page = 1; hasMore; page++)
            {
                var skuPage = await client.GetChangedSkus(changedSince, page, PageSize, cancellationToken);
                var result = await importer.Import(skuPage.Items ?? [], cancellationToken);
                added += result.Added;
                updated += result.Updated;
                failed += result.Failed;
                hasMore = skuPage.HasMore;
            }

            await validator.ValidateAll(HistorySource.Sync, MasterCatalogSkuImporter.ChangedBy, cancellationToken);
            await syncRunTracker.Complete(runId, added, updated, failed, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Master catalog sync run {RunId} failed", runId);
            await syncRunTracker.Fail(runId, e.Message, CancellationToken.None);
            throw;
        }
    }
}
