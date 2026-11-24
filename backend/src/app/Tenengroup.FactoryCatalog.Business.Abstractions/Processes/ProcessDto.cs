using Tenengroup.FactoryCatalog.Domains.Processes;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Processes;

public record ProcessDto(
    string ProcessId,
    string Name,
    ProcessType Type,
    ProcessUnit MeasuredIn,
    string? Description,
    ProcessStatus Status,
    DateTime LastUpdate,
    IReadOnlyCollection<ProcessFactoryDto> Factories,
    IReadOnlyCollection<ProcessSkuDto> Skus);

public record ProcessFactoryDto(
    string FactoryId,
    string FactoryName,
    bool RunsHere,
    string? MachineType,
    int CapacityPct,
    CapacityStatus CapacityStatus,
    CapacityNoteDto? CapacityNote,
    string? CapacityUpdatedBy,
    DateTime? CapacityUpdatedAt,
    LaborCostDto? LaborCost,
    decimal? MaxPerDay,
    ProcessTodayDto? Today);

public record CapacityNoteDto(string? Reason, DateOnly? ExpectedRecovery);

public record LaborCostDto(decimal AmountPerUnit, string? Currency, DateOnly? EffectiveFrom);

public record ProcessTodayDto(decimal Available, decimal Used, int RemainingPct);

public record ProcessSkuDto(Guid SkuUuid, string Name, string SiteSku, decimal Quantity);
