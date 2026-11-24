using Hangfire;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Sync;

public interface IMasterCatalogSyncJob
{
    [AutomaticRetry(Attempts = 0)]
    Task Run(CancellationToken cancellationToken);

    [AutomaticRetry(Attempts = 0)]
    Task RunStarted(long runId, CancellationToken cancellationToken);
}
