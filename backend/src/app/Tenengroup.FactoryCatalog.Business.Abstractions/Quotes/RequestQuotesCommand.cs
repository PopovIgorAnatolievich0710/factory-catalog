using Tenengroup.FactoryCatalog.Domains.Quotes;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Quotes;

public record RequestQuotesCommand(
    Guid RequestId,
    string? OrderId,
    string? OrderItemId,
    string Sku,
    int Quantity,
    IReadOnlyCollection<CbdItem> Cbd,
    QuoteMode Mode) : IRequest<QuoteSetDto>;

public record CbdItem(string ErpId, decimal Quantity);
