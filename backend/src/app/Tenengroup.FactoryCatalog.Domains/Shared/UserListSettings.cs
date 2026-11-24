namespace Tenengroup.FactoryCatalog.Domains.Shared;

public class UserListSettings
{
    public const int MaxColumns = 8;

    public long Id { get; set; }

    public string UserEmail { get; set; } = string.Empty;

    public string ListKey { get; set; } = string.Empty;

    public List<string> Columns { get; set; } = [];

    public DateTime UpdatedAt { get; set; }
}
