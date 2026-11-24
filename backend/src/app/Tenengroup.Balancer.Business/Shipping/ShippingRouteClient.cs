using System.Net.Http.Json;
using System.Text.Json;
using Tenengroup.Balancer.Business.Abstractions.Shipping;
using Tenengroup.Infrastructure.Common.Json;

namespace Tenengroup.Balancer.Business.Shipping;

public class ShippingRouteClient(HttpClient httpClient) : IShippingRouteClient
{
    private const string ActiveStatus = "Active";
    private const int ActiveStatusNumber = 0;

    public async Task<IReadOnlyCollection<ShippingRouteDto>> GetRoutes(CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync("v1/shippingRoute/getByCriteria", new { }, cancellationToken);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<RouteEnvelope>(JsonExtensions.Options, cancellationToken);

        return (envelope?.Data ?? [])
            .Where(IsActive)
            .SelectMany(route => (route.PackingFacilityIds ?? []).SelectMany(facilityId =>
                (route.ToCountryCodes ?? []).Select(country => new ShippingRouteDto(
                    facilityId.ToString(),
                    country.ToUpperInvariant(),
                    route.ShippingCost,
                    route.CurrencyCode,
                    route.ShippingTimeFrame,
                    route.ShippingTimeFrame + route.PackingTimeFrame))))
            .ToList();
    }

    private static bool IsActive(ShipmentServiceRoute route)
    {
        return route.Status.ValueKind switch
        {
            JsonValueKind.String => string.Equals(route.Status.GetString(), ActiveStatus, StringComparison.OrdinalIgnoreCase),
            JsonValueKind.Number => route.Status.GetInt32() == ActiveStatusNumber,
            _ => true
        };
    }

    private record RouteEnvelope(List<ShipmentServiceRoute> Data);

    private record ShipmentServiceRoute(
        int[] PackingFacilityIds,
        string[] ToCountryCodes,
        decimal ShippingCost,
        string CurrencyCode,
        int PackingTimeFrame,
        int ShippingTimeFrame,
        JsonElement Status);
}
