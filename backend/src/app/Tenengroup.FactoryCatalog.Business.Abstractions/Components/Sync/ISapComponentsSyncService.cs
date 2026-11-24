namespace Tenengroup.FactoryCatalog.Business.Abstractions.Components.Sync;

public interface ISapComponentsSyncService
{
    Task Run(string triggeredBy, CancellationToken cancellationToken = default);
}
