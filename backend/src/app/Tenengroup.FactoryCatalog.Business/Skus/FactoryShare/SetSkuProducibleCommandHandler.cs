using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.FactoryShare;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Skus.FactoryShare;

public class SetSkuProducibleCommandHandler(IFactoryCatalogDbContext db, SkuFactoryShareService shareService)
    : IRequestHandler<SetSkuProducibleCommand, SkuFactorySharesDto>
{
    public async Task<SkuFactorySharesDto> Handle(SetSkuProducibleCommand request, CancellationToken cancellationToken)
    {
        await shareService.EnsureEditableFactory(request.FactoryId, cancellationToken);
        var sku = await shareService.GetManufacturedSku(request.SkuUuid, cancellationToken);

        shareService.SetProducible(sku, request.FactoryId, request.Producible, HistorySource.User);
        await db.SaveChangesAsync(cancellationToken);

        return await shareService.GetShares(sku, cancellationToken);
    }
}
