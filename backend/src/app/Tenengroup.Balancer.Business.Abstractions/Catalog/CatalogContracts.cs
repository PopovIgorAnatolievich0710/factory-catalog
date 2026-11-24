namespace Tenengroup.Balancer.Business.Abstractions.Catalog;

public static class QuoteModes
{
    public const string Live = "live";
    public const string Shadow = "shadow";
    public const string Estimate = "estimate";
}

public static class QuoteReasonCodes
{
    public const string Dropshipper = "DROPSHIPPER";
}

public record CbdComponent(string ErpId, decimal Quantity);

public record QuoteRequest(
    Guid RequestId,
    string? OrderId,
    string? OrderItemId,
    string SkuUuid,
    int Quantity,
    IReadOnlyCollection<CbdComponent> Cbd,
    string Mode);

public record QuoteCost(decimal Material, decimal Labor, decimal Packing, decimal Total, string Currency);

public record QuoteComponent(
    string ErpId,
    decimal Quantity,
    decimal? UnitCost,
    string? StockStatus,
    string? AlternativeUsed,
    decimal? Cost = null,
    string? Currency = null);

public record QuoteProcess(
    string ProcessId,
    decimal Quantity,
    int? CapacityPct,
    decimal? LaborCost,
    int? RemainingPct,
    string? Currency = null);

public record CatalogProductionLogEntry(string ProcessId, string? MachineType);

public record FactoryQuote(
    string QuoteId,
    string FactoryId,
    bool Producible,
    IReadOnlyCollection<string>? Reasons,
    QuoteCost? Cost,
    int? CapacitySignalPct,
    decimal SharePct,
    IReadOnlyCollection<QuoteComponent>? Components,
    IReadOnlyCollection<QuoteProcess>? Processes,
    string? Status = null,
    string? ReproductionOf = null,
    IReadOnlyCollection<string>? Warnings = null,
    DateTime? SelectedAt = null,
    DateTime? ProducedAt = null,
    DateTime? TransferredAt = null,
    IReadOnlyCollection<CatalogProductionLogEntry>? ProductionLog = null);

public record QuoteSet(
    string QuoteKey,
    DateTime? ValidUntil,
    IReadOnlyCollection<FactoryQuote> Quotes,
    Guid? RequestId = null,
    string? OrderId = null,
    string? OrderItemId = null,
    string? SkuUuid = null,
    int? Quantity = null,
    string? Mode = null,
    DateTime? CreatedAt = null);

public record QuoteDetails(
    string QuoteKey,
    Guid RequestId,
    string? OrderId,
    string? OrderItemId,
    string SkuUuid,
    int Quantity,
    string Mode,
    DateTime ValidUntil,
    DateTime CreatedAt,
    FactoryQuote Quote);

public record QuoteSearchResult(IReadOnlyCollection<QuoteDetails> Items, int TotalCount);

public record MarkQuoteProducedRequest(
    DateTime ProducedAt,
    IReadOnlyCollection<CatalogProductionLogEntry> ProductionLog,
    string? ReproductionOfQuoteId);

public record CatalogMoney(decimal Amount, string Currency);

public record DropshipperCost(
    string SkuUuid,
    string? SiteSku,
    string? DropshipperId,
    string? DropshipperName,
    CatalogMoney? TotalCost);

public record CatalogFactory(string Id, string Name, string? CountryCode, bool IsActive);

public record CatalogUser(string Email, string Name, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> FactoryIds);

public record ProcessCapacity(string ProcessId, string Name, string? Type, int CapacityPct, decimal? MaxPerDay, decimal? UsedToday);
