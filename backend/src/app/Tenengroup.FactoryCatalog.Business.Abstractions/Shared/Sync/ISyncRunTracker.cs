namespace Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Sync;

public interface ISyncRunTracker
{
    Task<long> Start(string syncType, string triggeredBy, CancellationToken cancellationToken = default);

    Task Complete(long runId, int added, int updated, int failed, CancellationToken cancellationToken = default);

    Task Fail(long runId, string error, CancellationToken cancellationToken = default);

    Task<SyncStatusDto> GetStatus(string syncType, CancellationToken cancellationToken = default);
}
