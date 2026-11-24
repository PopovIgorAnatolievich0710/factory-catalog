using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Paging;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.FactoryCatalog.Business.Components;

public class ComponentListReader(IFactoryCatalogDbContext db, ICurrentUser currentUser)
{
    public const string FactoryStockPrefix = "stock_";

    public async Task<List<ComponentListItemDto>> Read(ListRequest request, CancellationToken cancellationToken)
    {
        var query = db.Components.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(c => c.ErpId.ToLower().Contains(search) || c.Name.ToLower().Contains(search));
        }

        var components = await query.ToListAsync(cancellationToken);
        var factoryRows = (await db.ComponentFactories.AsNoTracking().ToListAsync(cancellationToken))
            .ToLookup(f => f.ComponentErpId);
        var skuCounts = await (
                from skuComponent in db.SkuComponents.AsNoTracking()
                join sku in db.Skus.AsNoTracking() on skuComponent.SkuUuid equals sku.SkuUuid
                where sku.ProductType == SkuProductType.Manufactured
                group skuComponent by skuComponent.ComponentErpId
                into usage
                select new { ErpId = usage.Key, Count = usage.Count() })
            .ToDictionaryAsync(x => x.ErpId, x => x.Count, cancellationToken);
        var canViewCost = await currentUser.HasPermission(Permission.ComponentsViewCost, cancellationToken);

        var rows = components
            .Select(c => ToRow(c, factoryRows[c.ErpId].ToList(), skuCounts.GetValueOrDefault(c.ErpId), canViewCost))
            .AsQueryable()
            .ApplyFilter(request, "erpId", r => r.ErpId)
            .ApplyFilter(request, "name", r => r.Name)
            .ApplyFilter(request, "family", r => r.Family)
            .ApplyFilter(request, "material", r => r.Material)
            .ApplyFilter(request, "componentStatus", r => r.ComponentStatus)
            .ApplyFilter(request, "status", r => r.Status)
            .ApplyFilter(request, "totalStock", r => r.TotalStock)
            .ApplyFilter(request, "criticalLevel", r => r.CriticalLevel)
            .ApplyFilter(request, "unitCost", r => r.UnitCost)
            .ApplyFilter(request, "skusUsing", r => r.SkusUsing);

        var sortableFields = new Dictionary<string, Expression<Func<ComponentListItemDto, object>>>
        {
            ["erpId"] = r => r.ErpId,
            ["name"] = r => r.Name,
            ["family"] = r => r.Family,
            ["material"] = r => r.Material,
            ["componentStatus"] = r => r.ComponentStatus,
            ["status"] = r => r.Status,
            ["totalStock"] = r => r.TotalStock,
            ["criticalLevel"] = r => r.CriticalLevel,
            ["unitCost"] = r => r.UnitCost,
            ["skusUsing"] = r => r.SkusUsing
        };

        var factoryFields = request.Filters.Keys.Append(request.SortBy)
            .Where(field => field != null && field.StartsWith(FactoryStockPrefix, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var field in factoryFields)
        {
            var factoryId = field[FactoryStockPrefix.Length..];
            rows = rows.ApplyFilter(request, field, r => StockIn(r, factoryId));
            sortableFields[field] = r => StockIn(r, factoryId);
        }

        return rows.ApplySorting(request, sortableFields, "erpId").ToList();
    }

    public static decimal StockIn(ComponentListItemDto row, string factoryId)
    {
        return row.StockByFactory.FirstOrDefault(s => string.Equals(s.Key, factoryId, StringComparison.OrdinalIgnoreCase)).Value;
    }

    private static ComponentListItemDto ToRow(
        Component component,
        IReadOnlyCollection<ComponentFactory> factoryRows,
        int skusUsing,
        bool canViewCost)
    {
        var summary = ComponentStockSummary.From(component.CriticalStockLevel, factoryRows);

        return new ComponentListItemDto(
            component.ErpId,
            component.Name,
            component.Family,
            component.Material,
            component.Status,
            summary.Status,
            summary.FactoryIds,
            component.Status == ComponentStatus.Active && summary.IsOutOfStockOrConflict,
            summary.TotalStock,
            component.CriticalStockLevel,
            factoryRows.Any(f => f.CriticalStockLevelOverride != null),
            canViewCost ? component.UnitCostAmount : null,
            canViewCost ? component.UnitCostCurrency : null,
            skusUsing,
            factoryRows.ToDictionary(f => f.FactoryId, f => f.GetTotalStock()),
            component.LastSapSyncAt);
    }
}
