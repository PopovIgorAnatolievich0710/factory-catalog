namespace Tenengroup.FactoryCatalog.Business.Abstractions.Components.Sync;

public interface ISapComponentsSyncJob
{
    Task Run(CancellationToken cancellationToken);
}
