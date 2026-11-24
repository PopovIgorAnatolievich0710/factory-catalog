using System.ComponentModel.DataAnnotations;
using Tenengroup.FactoryCatalog.Domains.Processes;

namespace Tenengroup.FactoryCatalog.Api.Models.Processes;

public class CreateProcessModel
{
    public string ProcessId { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    public ProcessType? Type { get; set; }

    [Required]
    public ProcessUnit? MeasuredIn { get; set; }

    public string Description { get; set; }
}

public class UpdateProcessModel
{
    [Required]
    public string Name { get; set; }

    [Required]
    public ProcessType? Type { get; set; }

    [Required]
    public ProcessUnit? MeasuredIn { get; set; }

    [Required]
    public ProcessStatus? Status { get; set; }

    public string Description { get; set; }
}

public class UpdateProcessFactorySetupModel
{
    [Required]
    public bool? RunsHere { get; set; }

    public string MachineType { get; set; }

    public decimal? LaborCostAmount { get; set; }

    public string LaborCostCurrency { get; set; }

    public decimal? MaxPerDay { get; set; }
}

public class SetProcessCapacityModel
{
    [Required]
    public int? CapacityPct { get; set; }

    public string Reason { get; set; }

    public DateOnly? ExpectedRecovery { get; set; }
}
