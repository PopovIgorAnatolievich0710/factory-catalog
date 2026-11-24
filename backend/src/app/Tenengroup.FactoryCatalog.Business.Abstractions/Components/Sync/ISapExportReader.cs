namespace Tenengroup.FactoryCatalog.Business.Abstractions.Components.Sync;

public interface ISapExportReader
{
    Task<SapExport> Read(DateOnly exportDate, CancellationToken cancellationToken = default);
}
