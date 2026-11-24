namespace Tenengroup.FactoryCatalog.Domains.Shared;

public class UserFactory
{
    public long Id { get; set; }

    public string UserEmail { get; set; } = string.Empty;

    public string FactoryId { get; set; } = string.Empty;

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public Factory? Factory { get; set; }
}
