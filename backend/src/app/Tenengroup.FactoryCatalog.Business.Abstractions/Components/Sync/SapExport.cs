namespace Tenengroup.FactoryCatalog.Business.Abstractions.Components.Sync;

public record SapExport(
    IReadOnlyCollection<SapComponentRow> Components,
    IReadOnlyCollection<SapStockRow> Stock,
    IReadOnlyCollection<SapExportRowError> Errors);
