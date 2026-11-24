using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.LegacyBalancer;

public record GetCountrySkuListsQuery(string? FactoryId) : IRequest<IReadOnlyList<CountrySkuListDto>>;

public record CountrySkuListDto(
    string FactoryId,
    string CountryName,
    string? CountryCode,
    IReadOnlyList<CountrySkuDto> Skus);

public record CountrySkuDto(Guid Id, string Sku, bool Producible, bool Removed);
