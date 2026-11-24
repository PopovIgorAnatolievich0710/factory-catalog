using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.FactoryShare;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Import;
using Tenengroup.FactoryCatalog.Business.Skus.Import;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Skus.FactoryShare;

public class ImportProducibleCommandHandler(
    IFactoryCatalogDbContext db,
    ProducibleImportParser parser,
    SkuFactoryShareService shareService)
    : IRequestHandler<ImportProducibleCommand, ImportValidationResultDto>
{
    public async Task<ImportValidationResultDto> Handle(ImportProducibleCommand request, CancellationToken cancellationToken)
    {
        var import = await parser.Parse(request.File, cancellationToken);
        ImportErrors.ThrowIfAny(import.Errors);

        var skuUuids = import.Rows.Select(r => r.SkuUuid).Distinct().ToList();
        var skus = await db.Skus
            .Include(s => s.Factories)
            .Where(s => skuUuids.Contains(s.SkuUuid))
            .ToDictionaryAsync(s => s.SkuUuid, cancellationToken);
        foreach (var row in import.Rows)
        {
            shareService.SetProducible(skus[row.SkuUuid], row.FactoryId, row.Producible, HistorySource.Import);
        }

        await db.SaveChangesAsync(cancellationToken);

        return ImportErrors.ToResult(import.TotalRows, import.Errors);
    }
}
