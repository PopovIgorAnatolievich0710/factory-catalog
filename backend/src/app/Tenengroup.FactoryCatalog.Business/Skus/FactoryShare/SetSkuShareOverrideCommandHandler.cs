using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.FactoryShare;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Skus.FactoryShare;

public class SetSkuShareOverrideCommandHandler(IFactoryCatalogDbContext db, SkuFactoryShareService shareService)
    : IRequestHandler<SetSkuShareOverrideCommand, SkuFactorySharesDto>
{
    public async Task<SkuFactorySharesDto> Handle(SetSkuShareOverrideCommand request, CancellationToken cancellationToken)
    {
        await shareService.EnsureEditableFactory(request.FactoryId, cancellationToken);
        var sku = await shareService.GetManufacturedSku(request.SkuUuid, cancellationToken);

        shareService.SetOverride(sku, request.FactoryId, request.SharePct, await shareService.GetDefaultShares(cancellationToken));
        await db.SaveChangesAsync(cancellationToken);

        return await shareService.GetShares(sku, cancellationToken);
    }
}
