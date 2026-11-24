using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.FactoryShare;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.Processes;
using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Details;

public record SkuDetailsDto(
    Guid SkuUuid,
    string SiteSku,
    string Name,
    SkuProductType ProductType,
    SkuStatus Status,
    IReadOnlyCollection<SkuAliasDto> Aliases,
    SkuDropshipperDto? Dropshipper,
    SkuValidationDto Validation,
    DateTime? LastMasterSyncAt,
    IReadOnlyCollection<SkuComponentDto> Components,
    IReadOnlyCollection<SkuProcessDto> Processes,
    IReadOnlyCollection<SkuFactoryShareDto> Factories,
    decimal ShareTotalPct,
    SkuProductionConflictDto ProductionConflict);

public record SkuAliasDto(string Id, SkuAliasType Type, bool Deprecated);

public record SkuDropshipperDto(string? DropshipperId, string? Name, SkuMoneyDto? TotalCost);

public record SkuMoneyDto(decimal Amount, string? Currency);

public record SkuValidationDto(SkuValidationStatus Status, IReadOnlyCollection<string> Issues);

public record SkuComponentDto(
    string ErpId,
    string? Name,
    ComponentStatus? ComponentStatus,
    decimal Quantity,
    ComponentUsage Usage,
    IReadOnlyCollection<SkuComponentFactoryStockDto> Stock);

public record SkuComponentFactoryStockDto(string FactoryId, decimal TotalStock, StockAlertStatus StockStatus);

public record SkuProcessDto(
    string ProcessId,
    string? Name,
    ProcessType? Type,
    ProcessUnit? MeasuredIn,
    decimal Quantity,
    IReadOnlyCollection<SkuProcessFactoryCapacityDto> Capacity);

public record SkuProcessFactoryCapacityDto(string FactoryId, bool RunsHere, int? CapacityPct, string? MachineType);

public record SkuProductionConflictDto(SkuProductionConflictStatus Status, IReadOnlyCollection<SkuProductionConflictIssueDto> Issues);

public enum SkuProductionConflictStatus
{
    None,
    Conflict
}

public record SkuProductionConflictIssueDto(
    string FactoryId,
    ProductionConflictKind Kind,
    string ProcessId,
    string? ProcessName,
    ProcessType? ProcessType,
    string? MachineType,
    string Expected,
    string Actual,
    string OrderId,
    string OrderItemId,
    DateTime SeenAt);
