using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.List;
using Tenengroup.Infrastructure.Common.Excel;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Skus.List;

public class ExportSkusQueryHandler(SkuListReader reader, TimeProvider timeProvider) : IRequestHandler<ExportSkusQuery, ExcelFile>
{
    public const string PageName = "skus";

    private static readonly ExcelColumn<SkuListItemDto>[] Columns =
    [
        new("siteSku", "Site SKU", s => s.SiteSku),
        new("factorySku", "Factory SKU", s => string.Join(", ", s.FactorySkus)),
        new("name", "SKU name", s => s.Name),
        new("productType", "Type", s => s.ProductType.ToString()),
        new("status", "Status", s => s.Status.ToString()),
        new("producibleAt", "Producible at", s => string.Join(", ", s.ProducibleAt)),
        new("validationStatus", "Validation", s => s.ValidationStatus.ToString()),
        new("productionConflict", "Production conflict", s => string.Join(", ", s.ProductionConflictFactoryIds)),
        new("skuUuid", "UUID", s => s.SkuUuid.ToString()),
        new("dropshipperName", "Dropshipper", s => s.DropshipperName),
        new("lastMasterSyncAt", "Last master catalog sync", s => s.LastMasterSyncAt)
    ];

    public async Task<ExcelFile> Handle(ExportSkusQuery request, CancellationToken cancellationToken)
    {
        var rows = await reader.Read(request.Request, paged: false, cancellationToken);
        return ExcelExporter.Export(PageName, rows.Items, Columns, request.Request.Columns, timeProvider.GetUtcNow().UtcDateTime);
    }
}
