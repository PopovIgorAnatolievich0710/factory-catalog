using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.LegacyBalancer;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.LegacyBalancer;

public class GetCountrySkuListsQueryHandler(IFactoryCatalogDbContext db)
    : IRequestHandler<GetCountrySkuListsQuery, IReadOnlyList<CountrySkuListDto>>
{
    public async Task<IReadOnlyList<CountrySkuListDto>> Handle(GetCountrySkuListsQuery request, CancellationToken cancellationToken)
    {
        var factoryId = string.IsNullOrWhiteSpace(request.FactoryId) ? null : request.FactoryId.Trim();

        var factories = await db.Factories
            .AsNoTracking()
            .Where(f => f.IsActive && (factoryId == null || f.Id == factoryId))
            .OrderBy(f => f.Id)
            .ToListAsync(cancellationToken);

        if (factoryId != null && factories.Count == 0)
        {
            throw new EntityNotFoundException("Factory", factoryId);
        }

        var skus = (await db.Skus
                .AsNoTracking()
                .Where(s => s.ProductType == SkuProductType.Manufactured)
                .Select(s => new { s.SkuUuid, s.SiteSku, s.Status })
                .ToListAsync(cancellationToken))
            .ToDictionary(s => s.SkuUuid);

        var aliases = (await db.SkuAliases
                .AsNoTracking()
                .Select(a => new { a.SkuUuid, a.Alias })
                .ToListAsync(cancellationToken))
            .ToLookup(a => a.SkuUuid, a => a.Alias);

        var skuFactories = (await db.SkuFactories
                .AsNoTracking()
                .Select(f => new { f.SkuUuid, f.FactoryId, f.Producible })
                .ToListAsync(cancellationToken))
            .Where(f => skus.ContainsKey(f.SkuUuid))
            .ToLookup(f => f.FactoryId);

        return factories
            .Select(factory => new CountrySkuListDto(
                factory.Id,
                factory.Name,
                factory.CountryCode,
                skuFactories[factory.Id]
                    .SelectMany(skuFactory =>
                    {
                        var sku = skus[skuFactory.SkuUuid];
                        var removed = sku.Status == SkuStatus.Retired || !skuFactory.Producible;

                        return aliases[sku.SkuUuid]
                            .Prepend(sku.SiteSku)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .Select(identifier => new CountrySkuDto(sku.SkuUuid, identifier, skuFactory.Producible, removed));
                    })
                    .OrderBy(s => s.Sku, StringComparer.OrdinalIgnoreCase)
                    .ToList()))
            .ToList();
    }
}
