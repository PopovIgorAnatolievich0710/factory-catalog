using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.FactoryShare;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Skus.FactoryShare;

public class BulkSetSkuProducibleCommandHandler(IFactoryCatalogDbContext db, SkuFactoryShareService shareService)
    : IRequestHandler<BulkSetSkuProducibleCommand, BulkSetSkuProducibleResultDto>
{
    public async Task<BulkSetSkuProducibleResultDto> Handle(BulkSetSkuProducibleCommand request, CancellationToken cancellationToken)
    {
        if (request.SkuUuids.Count == 0)
        {
            throw new ValidationException("skuUuids", "Select at least one SKU.");
        }

        await shareService.EnsureEditableFactory(request.FactoryId, cancellationToken);

        var skus = await db.Skus
            .Include(s => s.Factories)
            .Where(s => request.SkuUuids.Contains(s.SkuUuid) && s.ProductType == SkuProductType.Manufactured)
            .ToListAsync(cancellationToken);

        var updated = skus.Count(sku => shareService.SetProducible(sku, request.FactoryId, request.Producible, HistorySource.User));
        await db.SaveChangesAsync(cancellationToken);

        return new BulkSetSkuProducibleResultDto(updated);
    }
}
