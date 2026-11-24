using Tenengroup.FactoryCatalog.Business.Abstractions.Quotes;
using Tenengroup.FactoryCatalog.Domains.Components;

namespace Tenengroup.FactoryCatalog.Business.Quotes;

internal static class StockStatusRule
{
    public static string Resolve(ComponentFactory stock, decimal? globalCriticalLevel)
    {
        var total = stock == null ? 0 : stock.WarehouseStock + stock.ProductionFloorStock;

        if (stock is { ManualOosActive: true } || total <= 0)
        {
            return StockStatuses.OutOfStock;
        }

        var criticalLevel = stock.CriticalStockLevelOverride ?? globalCriticalLevel;

        return total < criticalLevel ? StockStatuses.Low : StockStatuses.Ok;
    }
}
