namespace Tenengroup.FactoryCatalog.Domains.Skus;

public class GlobalFactoryShare
{
    public string FactoryId { get; set; } = string.Empty;

    public decimal Pct { get; set; }

    public string UpdatedBy { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }
}
