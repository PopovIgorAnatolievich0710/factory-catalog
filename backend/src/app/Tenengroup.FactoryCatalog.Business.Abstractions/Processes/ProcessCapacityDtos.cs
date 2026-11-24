using Tenengroup.FactoryCatalog.Domains.Processes;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Processes;

public record ProcessCapacityRowDto(
    string ProcessId,
    string Name,
    ProcessType Type,
    IReadOnlyCollection<ProcessCapacityCellDto> Cells);

public record ProcessCapacityCellDto(
    string FactoryId,
    bool RunsHere,
    int CapacityPct,
    CapacityStatus Status,
    string? Reason,
    DateOnly? ExpectedRecovery,
    string? UpdatedBy,
    DateTime? UpdatedAt,
    decimal? MaxPerDay,
    ProcessTodayDto? Today);
