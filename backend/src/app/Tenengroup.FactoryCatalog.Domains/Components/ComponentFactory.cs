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
}
