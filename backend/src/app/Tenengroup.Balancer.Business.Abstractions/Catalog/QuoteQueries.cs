using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.Balancer.Business.Abstractions.Catalog;

public record GetQuoteSetQuery(string QuoteKey) : IRequest<QuoteSet>;

public record SearchQuotesQuery(ListRequest Request) : IRequest<QuoteSearchResult>;
