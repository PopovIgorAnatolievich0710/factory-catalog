namespace Tenengroup.FactoryCatalog.Domains.Processes;

public class Process
{
    public const string PackingProcessId = "PACKING";

    public string ProcessId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public ProcessType Type { get; set; }

    public ProcessUnit MeasuredIn { get; set; }

    public string? Description { get; set; }

    public ProcessStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<ProcessFactory> Factories { get; set; } = [];
}
