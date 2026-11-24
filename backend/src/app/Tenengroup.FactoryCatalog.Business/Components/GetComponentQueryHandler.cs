using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.FactoryCatalog.Business.Components;

public class GetComponentQueryHandler(IFactoryCatalogDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetComponentQuery, ComponentDetailsDto>
{
    public async Task<ComponentDetailsDto> Handle(GetComponentQuery request, CancellationToken cancellationToken)
    {
        var component = await db.Components.AsNoTracking()
                            .FirstOrDefaultAsync(c => c.ErpId == request.ErpId, cancellationToken)
                        ?? throw new EntityNotFoundException(nameof(Component), request.ErpId);

        var factoryRows = await db.ComponentFactories.AsNoTracking()
            .Where(f => f.ComponentErpId == component.ErpId)
            .OrderBy(f => f.FactoryId)
            .ToListAsync(cancellationToken);

        var alternatives = await db.ComponentAlternatives.AsNoTracking()
            .Where(a => a.ComponentErpId == component.ErpId)
            .OrderBy(a => a.AlternativeErpId)
            .ToListAsync(cancellationToken);
        var alternativeIds = alternatives.Select(a => a.AlternativeErpId).ToList();
        var alternativeComponents = await db.Components.AsNoTracking()
            .Where(c => alternativeIds.Contains(c.ErpId))
            .ToDictionaryAsync(c => c.ErpId, cancellationToken);
        var alternativeStock = (await db.ComponentFactories.AsNoTracking()
                .Where(f => alternativeIds.Contains(f.ComponentErpId))
                .OrderBy(f => f.FactoryId)
                .ToListAsync(cancellationToken))
            .ToLookup(f => f.ComponentErpId);

        var usedInSkus = await (
                from skuComponent in db.SkuComponents.AsNoTracking()
                join sku in db.Skus.AsNoTracking() on skuComponent.SkuUuid equals sku.SkuUuid
                where skuComponent.ComponentErpId == component.ErpId && sku.ProductType == SkuProductType.Manufactured
                orderby sku.SiteSku
                select new ComponentSkuUsageDto(sku.SkuUuid, sku.Name, sku.SiteSku, skuComponent.Quantity, skuComponent.Usage))
            .ToListAsync(cancellationToken);

        var canViewCost = await currentUser.HasPermission(Permission.ComponentsViewCost, cancellationToken);
        var summary = ComponentStockSummary.From(component.CriticalStockLevel, factoryRows);

        return new ComponentDetailsDto(
            component.ErpId,
            component.Name,
            component.Status,
            component.Family,
            component.Material,
            new ComponentMeasureDto(component.MeasuredBy, component.MeasureValue, component.MeasureUnit),
            new ComponentUnitOfMeasureDto(component.UnitOfMeasureType, component.UnitOfMeasureQuantity, component.UnitOfMeasureUnit),
            component.CountryOfOrigin,
            component.HsCode,
            canViewCost
                ? new ComponentUnitCostDto(component.UnitCostAmount, component.UnitCostCurrency, component.UnitCostSource)
                : null,
            component.CriticalStockLevel,
            summary.Status,
            summary.FactoryIds,
            component.Status == ComponentStatus.Active && summary.IsOutOfStockOrConflict,
            factoryRows.Select(f => ToFactoryDto(f, component.CriticalStockLevel)).ToList(),
            alternatives
                .Where(a => alternativeComponents.ContainsKey(a.AlternativeErpId))
                .Select(a => ToAlternativeDto(a, alternativeComponents[a.AlternativeErpId], alternativeStock[a.AlternativeErpId]))
                .ToList(),
            usedInSkus,
            component.LastSapSyncAt);
    }

    private static ComponentFactoryDto ToFactoryDto(ComponentFactory row, decimal? criticalLevel)
    {
        return new ComponentFactoryDto(
            row.FactoryId,
            row.WarehouseStock,
            row.ProductionFloorStock,
            row.GetTotalStock(),
            row.GetEffectiveCriticalLevel(criticalLevel),
            row.CriticalStockLevelOverride,
            row.GetStockStatus(criticalLevel),
            row.IsOosConflict(criticalLevel),
            row.GetStockAlertStatus(criticalLevel),
            new ComponentManualOosDto(row.ManualOosActive, row.ManualOosSetBy, row.ManualOosSetAt, row.ManualOosNote),
            row.StockSyncedAt);
    }

    private static ComponentAlternativeDto ToAlternativeDto(
        ComponentAlternative alternative,
        Component alternativeComponent,
        IEnumerable<ComponentFactory> stock)
    {
        return new ComponentAlternativeDto(
            alternativeComponent.ErpId,
            alternativeComponent.Name,
            alternativeComponent.Status,
            alternative.ForBalancer,
            alternative.ForPlanner,
            stock
                .Select(f => new ComponentAlternativeStockDto(
                    f.FactoryId,
                    f.GetTotalStock(),
                    f.GetStockAlertStatus(alternativeComponent.CriticalStockLevel)))
                .ToList());
    }
}
