using System.Net.Http.Json;
using System.Text.Json;
using Tenengroup.Balancer.Business.Abstractions.Shipping;
using Tenengroup.Infrastructure.Common.Json;

namespace Tenengroup.Balancer.Business.Shipping;

public class ShippingRouteClient(HttpClient httpClient) : IShippingRouteClient
{
    internal const int PageSize = 500;
    private const string ActiveStatus = "Active";
    private const int ActiveStatusNumber = 0;

    public async Task<IReadOnlyCollection<ShippingRouteDto>> GetRoutes(CancellationToken cancellationToken)
    {
        var routes = new List<ShipmentServiceRoute>();
        int total;

        do
        {
            var criteria = new { Statuses = new[] { ActiveStatusNumber },Page = new { Offset = routes.Count, Limit = PageSize } };
            using var response = await httpClient.PostAsJsonAsync("v1/shippingRoute/getByCriteria", criteria, JsonExtensions.Options, cancellationToken);
            response.EnsureSuccessStatusCode();

            var page = await response.Content.ReadFromJsonAsync<RoutePage>(JsonExtensions.Options, cancellationToken);
            var data = page?.Data ?? [];
            routes.AddRange(data);
            total = data.Count == 0 ? routes.Count : page.TotalRowCount;
        }
        while (routes.Count < total);

        return routes
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

    private record RoutePage(List<ShipmentServiceRoute> Data, int TotalRowCount);

    private record ShipmentServiceRoute(
        int[] PackingFacilityIds,
        string[] ToCountryCodes,
        decimal ShippingCost,
        string CurrencyCode,
        int PackingTimeFrame,
        int ShippingTimeFrame,
        JsonElement Status);
}
