namespace Tenengroup.FactoryCatalog.Domains.Shared;

public class HistoryEntry
{
    public long Id { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string? FactoryId { get; set; }

    public string Field { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string ChangedBy { get; set; } = string.Empty;

    public DateTime ChangedAt { get; set; }

    public HistorySource Source { get; set; }
}
