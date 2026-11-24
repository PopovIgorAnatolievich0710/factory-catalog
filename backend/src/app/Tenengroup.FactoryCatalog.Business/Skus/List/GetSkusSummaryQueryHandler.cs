using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.List;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Skus.List;

public class GetSkusSummaryQueryHandler(IFactoryCatalogDbContext db) : IRequestHandler<GetSkusSummaryQuery, SkusSummaryDto>
{
    public async Task<SkusSummaryDto> Handle(GetSkusSummaryQuery request, CancellationToken cancellationToken)
    {
        var skus = db.Skus.AsNoTracking();
        return new SkusSummaryDto(
            await skus.CountAsync(cancellationToken),
            await skus.CountAsync(s => s.ProductType == SkuProductType.Dropshipper, cancellationToken),
            await skus.CountAsync(s => s.ValidationStatus == SkuValidationStatus.Invalid, cancellationToken));
    }
}
