namespace Tenengroup.FactoryCatalog.Domains.Skus;

public record SkuComponentStock(decimal Total, SkuComponentStockStatus Status, bool OosConflict)
{
    public static SkuComponentStock Evaluate(
        decimal warehouseStock,
        decimal productionFloorStock,
        decimal? globalCriticalLevel,
        decimal? criticalLevelOverride,
        bool manualOosActive)
    {
        var total = warehouseStock + productionFloorStock;
        var criticalLevel = criticalLevelOverride ?? globalCriticalLevel;
        var status = manualOosActive || total <= 0
            ? SkuComponentStockStatus.OutOfStock
            : total < criticalLevel
                ? SkuComponentStockStatus.Low
                : SkuComponentStockStatus.Ok;
        var oosConflict = manualOosActive && total >= criticalLevel;

        return new SkuComponentStock(total, status, oosConflict);
    }
}
