namespace Tenengroup.FactoryCatalog.Domains.Skus;

public class Sku
{
    public Guid SkuUuid { get; set; }

    public string SiteSku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public SkuProductType ProductType { get; set; }

    public SkuStatus Status { get; set; }

    public string? DropshipperId { get; set; }

    public string? DropshipperName { get; set; }

    public decimal? DropshipperTotalCostAmount { get; set; }

    public string? DropshipperTotalCostCurrency { get; set; }

    public SkuValidationStatus ValidationStatus { get; set; }

    public List<string> ValidationIssues { get; set; } = [];

    public DateTime? LastMasterSyncAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<SkuAlias> Aliases { get; set; } = [];

    public List<SkuComponent> Components { get; set; } = [];

    public List<SkuProcess> Processes { get; set; } = [];

    public List<SkuFactory> Factories { get; set; } = [];
}
