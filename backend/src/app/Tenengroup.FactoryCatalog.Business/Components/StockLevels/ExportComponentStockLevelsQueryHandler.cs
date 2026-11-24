using Tenengroup.FactoryCatalog.Business.Abstractions.Components.StockLevels;
using Tenengroup.Infrastructure.Common.Excel;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Components.StockLevels;

public class ExportComponentStockLevelsQueryHandler(ComponentStockLevelReader reader, TimeProvider timeProvider)
    : IRequestHandler<ExportComponentStockLevelsQuery, ExcelFile>
{
    private static readonly ExcelColumn<ComponentStockLevelDto>[] Columns =
    [
        new("erpId", "ERP ID", r => r.ErpId),
        new("name", "Name", r => r.Name),
        new("family", "Family", r => r.Family),
        new("factoryId", "Factory", r => r.FactoryId),
        new("warehouseStock", "Warehouse", r => r.WarehouseStock),
        new("productionFloorStock", "Production floor", r => r.ProductionFloorStock),
        new("totalStock", "Total", r => r.TotalStock),
        new("criticalLevel", "Critical level", r => r.CriticalLevel),
        new("status", "Status", r => r.Status.ToLabel()),
        new("manualOosSetBy", "Manual flag set by", r => r.ManualOosSetBy),
        new("manualOosSetAt", "Manual flag set at", r => r.ManualOosSetAt),
        new("manualOosNote", "Manual flag note", r => r.ManualOosNote)
    ];

    public async Task<ExcelFile> Handle(ExportComponentStockLevelsQuery request, CancellationToken cancellationToken)
    {
        var rows = await reader.Read(request.Request, cancellationToken);

        return ExcelExporter.Export(
            "component-stock-levels", rows, Columns, request.Request.Columns, timeProvider.GetUtcNow().UtcDateTime);
    }
}
