using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components.StockLevels;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.Business.Components.StockLevels;

public class ComponentStockLevelReader(IFactoryCatalogDbContext db)
{
    private static readonly Dictionary<string, Expression<Func<ComponentStockLevelDto, object>>> SortableFields = new()
    {
        ["erpId"] = r => r.ErpId,
        ["name"] = r => r.Name,
        ["family"] = r => r.Family,
        ["factoryId"] = r => r.FactoryId,
        ["warehouseStock"] = r => r.WarehouseStock,
        ["productionFloorStock"] = r => r.ProductionFloorStock,
        ["totalStock"] = r => r.TotalStock,
        ["criticalLevel"] = r => r.CriticalLevel,
        ["status"] = r => r.Status
    };

    public async Task<List<ComponentStockLevelDto>> Read(ListRequest request, CancellationToken cancellationToken)
    {
        var query = db.Components.AsNoTracking().Where(c => c.Status == ComponentStatus.Active);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(c => c.ErpId.ToLower().Contains(search) || c.Name.ToLower().Contains(search));
        }

        var components = await query.ToDictionaryAsync(c => c.ErpId, cancellationToken);
        var factoryRows = await db.ComponentFactories.AsNoTracking().ToListAsync(cancellationToken);

        return factoryRows
            .Where(f => components.ContainsKey(f.ComponentErpId))
            .Select(f => ToRow(components[f.ComponentErpId], f))
            .Where(r => r.Status != StockAlertStatus.Ok)
            .OrderBy(r => r.ErpId)
            .ThenBy(r => r.FactoryId)
            .AsQueryable()
            .ApplyFilter(request, "erpId", r => r.ErpId)
            .ApplyFilter(request, "name", r => r.Name)
            .ApplyFilter(request, "family", r => r.Family)
            .ApplyFilter(request, "factoryId", r => r.FactoryId)
            .ApplyFilter(request, "warehouseStock", r => r.WarehouseStock)
            .ApplyFilter(request, "productionFloorStock", r => r.ProductionFloorStock)
            .ApplyFilter(request, "totalStock", r => r.TotalStock)
            .ApplyFilter(request, "criticalLevel", r => r.CriticalLevel)
            .ApplyFilter(request, "status", r => r.Status)
            .ApplySorting(request, SortableFields, "erpId")
            .ToList();
    }

    private static ComponentStockLevelDto ToRow(Component component, ComponentFactory row)
    {
        return new ComponentStockLevelDto(
            component.ErpId,
            component.Name,
            component.Family,
            row.FactoryId,
            row.WarehouseStock,
            row.ProductionFloorStock,
            row.GetTotalStock(),
            row.GetEffectiveCriticalLevel(component.CriticalStockLevel),
            row.CriticalStockLevelOverride != null,
            row.GetStockAlertStatus(component.CriticalStockLevel),
            row.ManualOosSetBy,
            row.ManualOosSetAt,
            row.ManualOosNote);
    }
}
