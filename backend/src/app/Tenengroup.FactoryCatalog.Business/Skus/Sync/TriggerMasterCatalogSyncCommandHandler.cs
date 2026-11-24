using Hangfire;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Sync;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Sync;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Skus.Sync;

public class TriggerMasterCatalogSyncCommandHandler(
    ISyncRunTracker syncRunTracker,
    ICurrentUser currentUser,
    IBackgroundJobClient backgroundJobs)
    : IRequestHandler<TriggerMasterCatalogSyncCommand, SyncStatusDto>
{
    public async Task<SyncStatusDto> Handle(TriggerMasterCatalogSyncCommand request, CancellationToken cancellationToken)
    {
        var triggeredBy = string.IsNullOrEmpty(currentUser.Email) ? currentUser.Name : currentUser.Email;
        var runId = await syncRunTracker.Start(SyncTypes.MasterCatalogSkus, triggeredBy, cancellationToken);
        try
        {
            backgroundJobs.Enqueue<IMasterCatalogSyncJob>(job => job.RunStarted(runId, CancellationToken.None));
        }
        catch (Exception e)
        {
            await syncRunTracker.Fail(runId, e.Message, cancellationToken);
            throw;
        }

        return await syncRunTracker.GetStatus(SyncTypes.MasterCatalogSkus, cancellationToken);
    }
}
