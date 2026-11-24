namespace Tenengroup.FactoryCatalog.Domains.Components;

public class ComponentAlternative
{
    public long Id { get; set; }

    public string ComponentErpId { get; set; } = string.Empty;

    public string AlternativeErpId { get; set; } = string.Empty;

    public bool ForBalancer { get; set; }

    public bool ForPlanner { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Component? Component { get; set; }

    public Component? Alternative { get; set; }
}
