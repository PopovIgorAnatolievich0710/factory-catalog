namespace Tenengroup.FactoryCatalog.Domains.Components;

public class ComponentFactory
{
    public string ComponentErpId { get; set; } = string.Empty;

    public string FactoryId { get; set; } = string.Empty;

    public decimal WarehouseStock { get; set; }

    public decimal ProductionFloorStock { get; set; }

    public DateTime? StockSyncedAt { get; set; }

    public decimal? CriticalStockLevelOverride { get; set; }

    public bool ManualOosActive { get; set; }

    public string? ManualOosSetBy { get; set; }

    public DateTime? ManualOosSetAt { get; set; }

    public string? ManualOosNote { get; set; }

    public Component? Component { get; set; }

    public decimal GetTotalStock()
    {
        return WarehouseStock + ProductionFloorStock;
    }

    public decimal? GetEffectiveCriticalLevel(decimal? globalCriticalLevel)
    {
        return CriticalStockLevelOverride ?? globalCriticalLevel;
    }

    public StockStatus GetStockStatus(decimal? globalCriticalLevel)
    {
        var total = GetTotalStock();

        if (ManualOosActive || total <= 0)
        {
            return StockStatus.OutOfStock;
        }

        return total < GetEffectiveCriticalLevel(globalCriticalLevel) ? StockStatus.Low : StockStatus.Ok;
    }

    public bool IsOosConflict(decimal? globalCriticalLevel)
    {
        return ManualOosActive && GetTotalStock() >= GetEffectiveCriticalLevel(globalCriticalLevel);
    }

    public StockAlertStatus GetStockAlertStatus(decimal? globalCriticalLevel)
    {
        if (IsOosConflict(globalCriticalLevel))
        {
            return StockAlertStatus.OosConflict;
        }

        return GetStockStatus(globalCriticalLevel) switch
        {
            StockStatus.OutOfStock => StockAlertStatus.OutOfStock,
            StockStatus.Low => StockAlertStatus.Low,
            _ => StockAlertStatus.Ok
        };
    }
}
