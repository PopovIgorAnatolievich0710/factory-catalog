namespace Tenengroup.FactoryCatalog.Domains.Components;

public class Component
{
    public string ErpId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public ComponentStatus Status { get; set; }

    public string? Family { get; set; }

    public string? Material { get; set; }

    public string? MeasuredBy { get; set; }

    public decimal? MeasureValue { get; set; }

    public string? MeasureUnit { get; set; }

    public string? UnitOfMeasureType { get; set; }

    public decimal? UnitOfMeasureQuantity { get; set; }

    public string? UnitOfMeasureUnit { get; set; }

    public string? CountryOfOrigin { get; set; }

    public string? HsCode { get; set; }

    public decimal? UnitCostAmount { get; set; }

    public string? UnitCostCurrency { get; set; }

    public string? UnitCostSource { get; set; }

    public decimal? CriticalStockLevel { get; set; }

    public DateTime? LastSapSyncAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<ComponentFactory> Factories { get; set; } = [];

    public List<ComponentAlternative> Alternatives { get; set; } = [];
}
