using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Tenengroup.Balancer.Business.Abstractions;
using Tenengroup.Balancer.Business.Abstractions.Allocations;
using Tenengroup.Balancer.Business.Abstractions.Catalog;

namespace Tenengroup.Balancer.Business.Catalog;

public class ComponentBreakdownClient(
    HttpClient httpClient,
    MasterCatalogOptions options,
    ILogger<ComponentBreakdownClient> logger) : IComponentBreakdownClient
{
    private const decimal DefaultQuantity = 1m;

    private string? token;

    public async Task<IReadOnlyCollection<CbdComponent>?> Get(int siteId, string userSelections, CancellationToken cancellationToken)
    {
        if (httpClient.BaseAddress == null)
        {
            logger.LogWarning("MasterCatalog:BaseUrl is empty; the component breakdown is not requested");
            return null;
        }

        try
        {
            token ??= await Authenticate(cancellationToken);

            using var request = new HttpRequestMessage(HttpMethod.Post, $"component-breakdown/v2/{siteId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(userSelections, Encoding.UTF8, "application/json");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Component breakdown for site {SiteId} failed with {StatusCode}: {Body}",
                    siteId, (int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
                return null;
            }

            var breakdown = await response.Content.ReadFromJsonAsync<BreakdownResponse>(cancellationToken);

            return (breakdown?.MatchedFields ?? [])
                .SelectMany(f => f.Components ?? [])
                .Concat(breakdown?.StaticComponents ?? [])
                .Where(c => !string.IsNullOrWhiteSpace(c.ErpId))
                .GroupBy(c => c.ErpId.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new CbdComponent(g.Key, g.Sum(c => c.Quantity ?? DefaultQuantity)))
                .ToList();
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            logger.LogError(e, "Component breakdown for site {SiteId} could not be read", siteId);
            return null;
        }
    }

    private async Task<string> Authenticate(CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "auth/local",
            new { identifier = options.Username, password = options.Password },
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);

        return auth?.Jwt ?? throw new HttpRequestException("The master catalog returned no token");
    }

    private record AuthResponse([property: JsonPropertyName("jwt")] string? Jwt);

    private record BreakdownResponse(
        [property: JsonPropertyName("matched_fields")] IReadOnlyCollection<MatchedField>? MatchedFields,
        [property: JsonPropertyName("static_components")] IReadOnlyCollection<BreakdownComponent>? StaticComponents);

    private record MatchedField([property: JsonPropertyName("components")] IReadOnlyCollection<BreakdownComponent>? Components);

    private record BreakdownComponent(
        [property: JsonPropertyName("erp_id")] string? ErpId,
        [property: JsonPropertyName("quantity")] decimal? Quantity);
}
