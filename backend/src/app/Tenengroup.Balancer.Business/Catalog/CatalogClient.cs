using System.Net.Http.Headers;
using System.Net.Http.Json;
using Tenengroup.Balancer.Business.Abstractions.Catalog;
using Tenengroup.Infrastructure.Common.Json;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.Balancer.Business.Catalog;

public class CatalogClient(HttpClient httpClient) : ICatalogClient
{
    internal const string PermissionsToggleName = "permissions.enable_all";

    public Task<QuoteSet> RequestQuotes(QuoteRequest request, CancellationToken cancellationToken)
    {
        return Post<QuoteSet>("v1/quotes", request, cancellationToken);
    }

    public Task<QuoteSet> GetQuoteSet(string quoteKey, CancellationToken cancellationToken)
    {
        return Get<QuoteSet>($"v1/quotes/{Uri.EscapeDataString(quoteKey)}", cancellationToken);
    }

    public Task<QuoteDetails> SelectQuote(string quoteId, CancellationToken cancellationToken)
    {
        return Post<QuoteDetails>($"v1/quotes/{Uri.EscapeDataString(quoteId)}/select", null, cancellationToken);
    }

    public Task<QuoteDetails> MarkQuoteProduced(string quoteId, MarkQuoteProducedRequest request, CancellationToken cancellationToken)
    {
        return Post<QuoteDetails>($"v1/quotes/{Uri.EscapeDataString(quoteId)}/produced", request, cancellationToken);
    }

    public Task<QuoteDetails> MarkQuoteTransferred(string quoteId, CancellationToken cancellationToken)
    {
        return Post<QuoteDetails>($"v1/quotes/{Uri.EscapeDataString(quoteId)}/transferred", null, cancellationToken);
    }

    public Task<QuoteSearchResult> SearchQuotes(ListRequest request, CancellationToken cancellationToken)
    {
        return Post<QuoteSearchResult>("v1/quotes/search", request, cancellationToken);
    }

    public Task<DropshipperCost> GetDropshipperCost(string sku, CancellationToken cancellationToken)
    {
        return Get<DropshipperCost>($"v1/quotes/dropshipper-cost?sku={Uri.EscapeDataString(sku)}", cancellationToken);
    }

    public async Task<IReadOnlyCollection<CatalogFactory>> GetFactories(CancellationToken cancellationToken)
    {
        return await Get<List<CatalogFactory>>("v1/factories", cancellationToken);
    }

    public async Task<bool> IsPermissionEnforcementEnabled(CancellationToken cancellationToken)
    {
        var toggles = await Get<FeatureToggleEnvelope>("v1/feature-toggle", cancellationToken);
        return toggles.Data.Any(t => t.Name == PermissionsToggleName && t.IsEnabled);
    }

    public async Task<CatalogUser> GetUser(string authorizationHeader, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, "v1/user/me");
        message.Headers.Authorization = AuthenticationHeaderValue.Parse(authorizationHeader);

        using var response = await httpClient.SendAsync(message, cancellationToken);
        return await Read<CatalogUser>(response, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProcessCapacity>> GetProcessCapacity(string factoryId, CancellationToken cancellationToken)
    {
        var rows = await Get<List<ProcessCapacityRow>>(
            $"v1/process-capacity/factories/{Uri.EscapeDataString(factoryId)}", cancellationToken);

        return rows
            .SelectMany(row => row.Cells
                .Where(cell => cell.FactoryId == factoryId && cell.RunsHere)
                .Select(cell => new ProcessCapacity(
                    row.ProcessId,
                    row.Name,
                    row.Type,
                    cell.CapacityPct,
                    cell.MaxPerDay,
                    cell.Today?.Used)))
            .ToList();
    }

    private async Task<T> Get<T>(string url, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(url, cancellationToken);
        return await Read<T>(response, cancellationToken);
    }

    private async Task<T> Post<T>(string url, object body, CancellationToken cancellationToken)
    {
        using var content = body == null ? null : JsonContent.Create(body, body.GetType(), options: JsonExtensions.Options);
        using var response = await httpClient.PostAsync(url, content, cancellationToken);
        return await Read<T>(response, cancellationToken);
    }

    private static async Task<T> Read<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Factory catalog responded {(int)response.StatusCode} to {response.RequestMessage?.Method} {response.RequestMessage?.RequestUri}: {body}",
                null,
                response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<T>(JsonExtensions.Options, cancellationToken);
    }

    private record FeatureToggleEnvelope(List<FeatureToggleItem> Data);

    private record FeatureToggleItem(string Name, bool IsEnabled);

    private record ProcessCapacityRow(string ProcessId, string Name, string? Type, List<ProcessCapacityCell> Cells);

    private record ProcessCapacityCell(string FactoryId, bool RunsHere, int CapacityPct, decimal? MaxPerDay, ProcessCapacityToday? Today);

    private record ProcessCapacityToday(decimal Available, decimal Used, int RemainingPct);
}
