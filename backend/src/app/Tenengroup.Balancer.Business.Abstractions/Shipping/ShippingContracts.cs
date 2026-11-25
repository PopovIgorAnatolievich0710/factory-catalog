namespace Tenengroup.Balancer.Business.Abstractions.Shipping;

public record ShippingRouteDto(
    string OriginFacilityId,
    string DestinationCountry,
    decimal CostAmount,
    string CostCurrency,
    int TransitDaysMin,
    int TransitDaysMax,
    bool Priority,
    IReadOnlyCollection<int> WorkingDays);

public interface IShippingRouteClient
{
    Task<IReadOnlyCollection<ShippingRouteDto>> GetRoutes(CancellationToken cancellationToken);
}

public interface IShippingRouteRefreshJob
{
    Task Run(CancellationToken cancellationToken);
}
