using Tenengroup.FactoryCatalog.Domains.Quotes;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Quotes;

public record QuoteSetDto(
    string QuoteKey,
    DateTime ValidUntil,
    IReadOnlyList<QuoteDto> Quotes,
    Guid RequestId,
    string? OrderId,
    string? OrderItemId,
    Guid SkuUuid,
    int Quantity,
    QuoteMode Mode,
    DateTime CreatedAt);

public record QuoteDto(
    string QuoteId,
    string FactoryId,
    bool Producible,
    IReadOnlyList<string> Reasons,
    QuoteCostDto? Cost,
    int? CapacitySignalPct,
    decimal SharePct,
    IReadOnlyList<QuoteComponentDto> Components,
    IReadOnlyList<QuoteProcessDto> Processes,
    QuoteStatus Status,
    DateTime? SelectedAt,
    DateTime? ProducedAt,
    DateTime? TransferredAt,
    string? ReproductionOf,
    IReadOnlyList<ProductionLogEntryDto>? ProductionLog,
    IReadOnlyList<string> Warnings);

public record QuoteCostDto(decimal Material, decimal Labor, decimal Packing, decimal Total, string Currency);

public record QuoteComponentDto(
    string ErpId,
    decimal Quantity,
    decimal? UnitCost,
    string StockStatus,
    string? AlternativeUsed,
    decimal? Cost,
    string? Currency);

public record QuoteProcessDto(
    string ProcessId,
    decimal Quantity,
    int? CapacityPct,
    decimal? LaborCost,
    int? RemainingPct,
    string? Currency);

public record ProductionLogEntryDto(string ProcessId, string? MachineType);

public record QuoteDetailsDto(
    string QuoteKey,
    Guid RequestId,
    string? OrderId,
    string? OrderItemId,
    Guid SkuUuid,
    int Quantity,
    QuoteMode Mode,
    DateTime ValidUntil,
    DateTime CreatedAt,
    QuoteDto Quote);
