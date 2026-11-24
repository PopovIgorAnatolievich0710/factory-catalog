namespace Tenengroup.FactoryCatalog.Domains.Shared;

public class Factory
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? CountryCode { get; set; }

    public bool IsActive { get; set; }
}
