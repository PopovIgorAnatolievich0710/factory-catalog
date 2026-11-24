namespace Tenengroup.FactoryCatalog.Business.Abstractions.Components.Sync;

public record SapStockRow(string ErpId, string FactoryId, SapStockLocation Location, decimal Quantity);
