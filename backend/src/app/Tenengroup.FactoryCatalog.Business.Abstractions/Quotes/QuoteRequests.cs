using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Quotes;

public record GetQuoteSetQuery(string QuoteKey) : IRequest<QuoteSetDto>;

public record SearchQuotesQuery(ListRequest Request) : IRequest<ListResult<QuoteDetailsDto>>;

public record SelectQuoteCommand(string QuoteId) : IRequest<QuoteDetailsDto>;

public record MarkQuoteProducedCommand(
    string QuoteId,
    DateTime ProducedAt,
    IReadOnlyList<ProductionLogEntryDto> ProductionLog,
    string? ReproductionOfQuoteId) : IRequest<QuoteDetailsDto>;

public record MarkQuoteTransferredCommand(string QuoteId) : IRequest<QuoteDetailsDto>;

public record GetDropshipperCostQuery(string Sku) : IRequest<DropshipperCostDto>;

public record DropshipperCostDto(
    Guid SkuUuid,
    string SiteSku,
    string? DropshipperId,
    string? DropshipperName,
    DropshipperTotalCostDto? TotalCost);

public record DropshipperTotalCostDto(decimal Amount, string? Currency);
