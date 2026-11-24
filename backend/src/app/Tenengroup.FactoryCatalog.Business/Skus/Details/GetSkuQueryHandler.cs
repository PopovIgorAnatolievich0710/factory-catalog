using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Details;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.FactoryShare;
using Tenengroup.FactoryCatalog.Business.Skus.FactoryShare;
using Tenengroup.FactoryCatalog.Business.Skus.ProductionConflicts;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Skus.Details;

public class GetSkuQueryHandler(
    IFactoryCatalogDbContext db,
    SkuFactoryShareService shareService,
    ProductionConflictReader conflictReader)
    : IRequestHandler<GetSkuQuery, SkuDetailsDto>
{
    public async Task<SkuDetailsDto> Handle(GetSkuQuery request, CancellationToken cancellationToken)
    {
        var sku = await db.Skus
                      .AsNoTracking()
                      .Include(s => s.Aliases)
                      .Include(s => s.Components)
                      .Include(s => s.Processes)
                      .Include(s => s.Factories)
                      .FirstOrDefaultAsync(s => s.SkuUuid == request.SkuUuid, cancellationToken)
                  ?? throw new EntityNotFoundException(nameof(Sku), request.SkuUuid);

        var isDropshipper = sku.ProductType == SkuProductType.Dropshipper;
        var factoryIds = await shareService.GetActiveFactoryIds(cancellationToken);
        IReadOnlyCollection<SkuFactoryShareDto> factories = isDropshipper
            ? []
            : SkuFactoryShareBuilder.Build(factoryIds, sku.Factories, await shareService.GetDefaultShares(cancellationToken));
        var conflicts = isDropshipper
            ? []
            : (await conflictReader.Get([sku.SkuUuid], cancellationToken)).GetValueOrDefault(sku.SkuUuid) ?? [];

        return new SkuDetailsDto(
            sku.SkuUuid,
            sku.SiteSku,
            sku.Name,
            sku.ProductType,
            sku.Status,
            sku.Aliases.OrderBy(a => a.Alias).Select(a => new SkuAliasDto(a.Alias, a.Type, a.Deprecated)).ToList(),
            isDropshipper
                ? new SkuDropshipperDto(
                    sku.DropshipperId,
                    sku.DropshipperName,
                    sku.DropshipperTotalCostAmount == null
                        ? null
                        : new SkuMoneyDto(sku.DropshipperTotalCostAmount.Value, sku.DropshipperTotalCostCurrency))
                : null,
            new SkuValidationDto(sku.ValidationStatus, sku.ValidationIssues),
            sku.LastMasterSyncAt,
            isDropshipper ? [] : await GetComponents(sku, factoryIds, cancellationToken),
            isDropshipper ? [] : await GetProcesses(sku, factoryIds, cancellationToken),
            factories,
            factories.Sum(f => f.EffectiveSharePct),
            new SkuProductionConflictDto(
                conflicts.Count == 0 ? SkuProductionConflictStatus.None : SkuProductionConflictStatus.Conflict,
                conflicts));
    }

    private async Task<IReadOnlyCollection<SkuComponentDto>> GetComponents(
        Sku sku,
        IReadOnlyList<string> factoryIds,
        CancellationToken cancellationToken)
    {
        var erpIds = sku.Components.Select(c => c.ComponentErpId).ToList();
        var components = await db.Components
            .AsNoTracking()
            .Where(c => erpIds.Contains(c.ErpId))
            .Select(c => new { c.ErpId, c.Name, c.Status, c.CriticalStockLevel })
            .ToListAsync(cancellationToken);
        var stocks = await db.ComponentFactories
            .AsNoTracking()
            .Where(f => erpIds.Contains(f.ComponentErpId))
            .ToListAsync(cancellationToken);

        return sku.Components
            .OrderBy(c => c.ComponentErpId)
            .Select(mapping =>
            {
                var component = components.FirstOrDefault(c => c.ErpId == mapping.ComponentErpId);
                var stock = component == null
                    ? []
                    : factoryIds
                        .Select(factoryId =>
                        {
                            var row = stocks.FirstOrDefault(s => s.ComponentErpId == component.ErpId && s.FactoryId == factoryId)
                                      ?? new ComponentFactory { ComponentErpId = component.ErpId, FactoryId = factoryId };
                            return new SkuComponentFactoryStockDto(
                                factoryId, row.GetTotalStock(), row.GetStockAlertStatus(component.CriticalStockLevel));
                        })
                        .ToList();

                return new SkuComponentDto(
                    mapping.ComponentErpId, component?.Name, component?.Status, mapping.Quantity, mapping.Usage, stock);
            })
            .ToList();
    }

    private async Task<IReadOnlyCollection<SkuProcessDto>> GetProcesses(
        Sku sku,
        IReadOnlyList<string> factoryIds,
        CancellationToken cancellationToken)
    {
        var processIds = sku.Processes.Select(p => p.ProcessId).ToList();
        var processes = await db.Processes
            .AsNoTracking()
            .Where(p => processIds.Contains(p.ProcessId))
            .Select(p => new { p.ProcessId, p.Name, p.Type, p.MeasuredIn })
            .ToListAsync(cancellationToken);
        var processFactories = await db.ProcessFactories
            .AsNoTracking()
            .Where(f => processIds.Contains(f.ProcessId))
            .ToListAsync(cancellationToken);

        return sku.Processes
            .OrderBy(p => p.ProcessId)
            .Select(mapping =>
            {
                var process = processes.FirstOrDefault(p => p.ProcessId == mapping.ProcessId);
                var capacity = factoryIds
                    .Select(factoryId =>
                    {
                        var row = processFactories.FirstOrDefault(f => f.ProcessId == mapping.ProcessId && f.FactoryId == factoryId);
                        var runsHere = row?.RunsHere ?? false;
                        return new SkuProcessFactoryCapacityDto(
                            factoryId, runsHere, runsHere ? row.CapacityPct : null, runsHere ? row.MachineType : null);
                    })
                    .ToList();

                return new SkuProcessDto(
                    mapping.ProcessId, process?.Name, process?.Type, process?.MeasuredIn, mapping.Quantity, capacity);
            })
            .ToList();
    }
}
