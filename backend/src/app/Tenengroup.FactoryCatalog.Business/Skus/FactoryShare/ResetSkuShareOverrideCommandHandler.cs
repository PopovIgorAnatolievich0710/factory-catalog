using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.FactoryShare;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Skus.FactoryShare;

public class ResetSkuShareOverrideCommandHandler(IFactoryCatalogDbContext db, SkuFactoryShareService shareService)
    : IRequestHandler<ResetSkuShareOverrideCommand, SkuFactorySharesDto>
{
    public async Task<SkuFactorySharesDto> Handle(ResetSkuShareOverrideCommand request, CancellationToken cancellationToken)
    {
        await shareService.EnsureEditableFactory(request.FactoryId, cancellationToken);
        var sku = await shareService.GetManufacturedSku(request.SkuUuid, cancellationToken);

        shareService.ResetOverride(sku, request.FactoryId, await shareService.GetDefaultShares(cancellationToken));
        await db.SaveChangesAsync(cancellationToken);

        return await shareService.GetShares(sku, cancellationToken);
    }
}
