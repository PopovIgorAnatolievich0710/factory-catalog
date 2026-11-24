namespace Tenengroup.FactoryCatalog.Domains.Shared;

public class SyncRun
{
    public long Id { get; set; }

    public string SyncType { get; set; } = string.Empty;

    public SyncRunStatus Status { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public int Added { get; set; }

    public int Updated { get; set; }

    public int Failed { get; set; }

    public string? Error { get; set; }

    public string TriggeredBy { get; set; } = string.Empty;
}
