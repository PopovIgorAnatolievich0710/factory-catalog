using System.Text.Json;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.Balancer.Business.Abstractions.Catalog;

public interface ICatalogClient
{
    Task<QuoteSet> RequestQuotes(QuoteRequest request, CancellationToken cancellationToken);

    Task<QuoteSet> GetQuoteSet(string quoteKey, CancellationToken cancellationToken);

    Task<QuoteLifecycleResult> SelectQuote(string quoteId, CancellationToken cancellationToken);

    Task<QuoteLifecycleResult> MarkQuoteProduced(string quoteId, MarkQuoteProducedRequest request, CancellationToken cancellationToken);

    Task<QuoteLifecycleResult> MarkQuoteTransferred(string quoteId, CancellationToken cancellationToken);

    Task<JsonElement> SearchQuotes(ListRequest request, CancellationToken cancellationToken);

    Task<DropshipperCost> GetDropshipperCost(string sku, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CatalogFactory>> GetFactories(CancellationToken cancellationToken);

    Task<bool> IsPermissionEnforcementEnabled(CancellationToken cancellationToken);

    Task<CatalogUser> GetUser(string authorizationHeader, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProcessCapacity>> GetProcessCapacity(string factoryId, DateOnly date, CancellationToken cancellationToken);
}
