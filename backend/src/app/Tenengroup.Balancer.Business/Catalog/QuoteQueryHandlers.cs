using Tenengroup.Balancer.Business.Abstractions.Catalog;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.Balancer.Business.Catalog;

public class GetQuoteSetQueryHandler(ICatalogClient catalogClient) : IRequestHandler<GetQuoteSetQuery, QuoteSet>
{
    public Task<QuoteSet> Handle(GetQuoteSetQuery request, CancellationToken cancellationToken)
    {
        return catalogClient.GetQuoteSet(request.QuoteKey, cancellationToken);
    }
}

public class SearchQuotesQueryHandler(ICatalogClient catalogClient) : IRequestHandler<SearchQuotesQuery, QuoteSearchResult>
{
    public Task<QuoteSearchResult> Handle(SearchQuotesQuery request, CancellationToken cancellationToken)
    {
        return catalogClient.SearchQuotes(request.Request, cancellationToken);
    }
}
