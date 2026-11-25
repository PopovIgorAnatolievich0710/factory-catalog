using System.Net.Http.Json;
using Tenengroup.Balancer.Business.Abstractions;
using Tenengroup.Balancer.Business.Abstractions.Shipping;
using Tenengroup.Infrastructure.Common.Json;

namespace Tenengroup.Balancer.Business.Shipping;

public class ShippingRouteClient(HttpClient httpClient, BalancerOptions options) : IShippingRouteClient
{
    internal const int PageSize = 500;
    private const int ActiveStatus = 0;
    private static readonly int[] DefaultWorkingDays = [1, 2, 3, 4, 5];

    public async Task<IReadOnlyCollection<ShippingRouteDto>> GetRoutes(CancellationToken cancellationToken)
    {
        var routes = new List<ShipmentServiceRoute>();
        int total;

        do
        {
            var criteria = new { Statuses = new[] { ActiveStatus }, Page = new { Offset = routes.Count, Limit = PageSize } };
            using var response = await httpClient.PostAsJsonAsync("v1/shippingRoute/getByCriteria", criteria, JsonExtensions.Options, cancellationToken);
            response.EnsureSuccessStatusCode();

            var page = await response.Content.ReadFromJsonAsync<RoutePage>(JsonExtensions.Options, cancellationToken);
            var data = page?.Data ?? [];
            routes.AddRange(data);
            total = data.Count == 0 ? routes.Count : page.TotalRowCount;
        }
        while (routes.Count < total);

        var allFacilities = options.PackingFacilities.Values.Distinct().ToList();

        return routes
            .Where(route => route.Status == ActiveStatus && route.CarrierServiceType?.Method == options.ShippingServiceLevel)
            .SelectMany(route => (route.PackingFacilityIds?.Select(id => id.ToString()) ?? allFacilities).SelectMany(facilityId =>
                (route.ToCountryCodes ?? []).Select(country => new ShippingRouteDto(
                    facilityId,
                    country.ToUpperInvariant(),
                    route.ShippingCost,
                    route.CurrencyCode,
                    route.ShippingTimeFrame,
                    route.ShippingTimeFrame + route.PackingTimeFrame,
                    route.Priority,
                    route.ShippingWorkingDays is { Length: > 0 } ? route.ShippingWorkingDays : DefaultWorkingDays))))
            .ToList();
    }

    private record RoutePage(List<ShipmentServiceRoute> Data, int TotalRowCount);

    private record CarrierServiceType(int? Method);

    private record ShipmentServiceRoute(
        int[]? PackingFacilityIds,
        string[]? ToCountryCodes,
        decimal ShippingCost,
        string CurrencyCode,
        int PackingTimeFrame,
        int ShippingTimeFrame,
        int Status,
        bool Priority,
        int[]? ShippingWorkingDays,
        CarrierServiceType? CarrierServiceType);
}
