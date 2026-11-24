using Tenengroup.FactoryCatalog.Business.Abstractions.Quotes;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Quotes;

public class GetDropshipperCostQueryHandler(IFactoryCatalogDbContext db)
    : IRequestHandler<GetDropshipperCostQuery, DropshipperCostDto>
{
    public async Task<DropshipperCostDto> Handle(GetDropshipperCostQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Sku))
        {
            throw new ValidationException("sku", "sku (the SKU UUID or an alias) is required.");
        }

        var sku = await SkuLookup.Find(db, request.Sku, cancellationToken)
                  ?? throw new EntityNotFoundException("SKU", request.Sku);

        if (sku.ProductType != SkuProductType.Dropshipper)
        {
            throw new ValidationException("sku", $"SKU '{request.Sku}' is not a dropshipper SKU.");
        }

        return new DropshipperCostDto(
            sku.SkuUuid,
            sku.SiteSku,
            sku.DropshipperId,
            sku.DropshipperName,
            sku.DropshipperTotalCostAmount == null
                ? null
                : new DropshipperTotalCostDto(sku.DropshipperTotalCostAmount.Value, sku.DropshipperTotalCostCurrency));
    }
}
