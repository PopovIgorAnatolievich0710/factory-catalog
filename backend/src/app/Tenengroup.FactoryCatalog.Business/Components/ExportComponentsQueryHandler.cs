using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.Infrastructure.Common.Excel;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Components;

public class ExportComponentsQueryHandler(
    ComponentListReader reader,
    IFactoryCatalogDbContext db,
    TimeProvider timeProvider) : IRequestHandler<ExportComponentsQuery, ExcelFile>
{
    private static readonly ExcelColumn<ComponentListItemDto>[] Columns =
    [
        new("erpId", "ERP ID", c => c.ErpId),
        new("name", "Name", c => c.Name),
        new("family", "Family", c => c.Family),
        new("status", "Status", c => c.Status.ToLabel()),
        new("statusFactories", "Status factories", c => string.Join(", ", c.StatusFactoryIds)),
        new("totalStock", "Total stock", c => c.TotalStock),
        new("criticalLevel", "Critical level", c => c.CriticalLevel),
        new("skusUsing", "SKUs using", c => c.SkusUsing),
        new("material", "Material", c => c.Material),
        new("unitCost", "Unit cost", c => c.UnitCost),
        new("unitCostCurrency", "Unit cost currency", c => c.UnitCostCurrency),
        new("componentStatus", "SAP status", c => c.ComponentStatus.ToString())
    ];

    public async Task<ExcelFile> Handle(ExportComponentsQuery request, CancellationToken cancellationToken)
    {
        var rows = await reader.Read(request.Request, cancellationToken);
        var factoryIds = await db.Factories.AsNoTracking().OrderBy(f => f.Id).Select(f => f.Id).ToListAsync(cancellationToken);
        var columns = Columns
            .Concat(factoryIds.Select(id => new ExcelColumn<ComponentListItemDto>(
                ComponentListReader.FactoryStockPrefix + id,
                $"Stock {id}",
                c => ComponentListReader.StockIn(c, id))))
            .ToList();

        return ExcelExporter.Export("components", rows, columns, request.Request.Columns, timeProvider.GetUtcNow().UtcDateTime);
    }
}
