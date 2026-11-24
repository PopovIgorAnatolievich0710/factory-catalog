using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components.Sync;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Sync;
using Tenengroup.FactoryCatalog.Business.Skus.Validation;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.Shared;

namespace Tenengroup.FactoryCatalog.Business.Components.Sync;

public class SapComponentsSyncService(
    IFactoryCatalogDbContext db,
    ISapExportReader exportReader,
    ISyncRunTracker syncRunTracker,
    IHistoryWriter historyWriter,
    SkuMappingValidator skuMappingValidator,
    TimeProvider timeProvider,
    ILogger<SapComponentsSyncService> logger) : ISapComponentsSyncService
{
    public const string ChangedBy = "SAP sync";
    public const string UnitCostSource = "SAP";

    public async Task Run(string triggeredBy, CancellationToken cancellationToken = default)
    {
        var runId = await syncRunTracker.Start(SyncTypes.SapComponents, triggeredBy, cancellationToken);

        try
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var export = await exportReader.Read(DateOnly.FromDateTime(now), cancellationToken);
            if (export.Components.Count == 0)
            {
                throw new InvalidDataException("The SAP export has no valid component rows");
            }

            foreach (var error in export.Errors)
            {
                logger.LogWarning("SAP export row skipped: {File} line {Line}: {Message}", error.File, error.Line, error.Message);
            }

            var components = await db.Components.ToDictionaryAsync(c => c.ErpId, cancellationToken);
            var addedErpIds = new HashSet<string>();
            var updatedErpIds = new HashSet<string>();

            SyncComponents(export, components, addedErpIds, updatedErpIds, now);
            var failedStockRows = await SyncStock(export, components, updatedErpIds, now, cancellationToken);

            await db.SaveChangesAsync(cancellationToken);
            await skuMappingValidator.ValidateAll(HistorySource.Sync, ChangedBy, cancellationToken);

            updatedErpIds.ExceptWith(addedErpIds);
            await syncRunTracker.Complete(
                runId, addedErpIds.Count, updatedErpIds.Count, export.Errors.Count + failedStockRows, cancellationToken);
        }
        catch (Exception e)
        {
            await syncRunTracker.Fail(runId, e.Message, CancellationToken.None);
            throw;
        }
    }

    private void SyncComponents(
        SapExport export,
        Dictionary<string, Component> components,
        HashSet<string> addedErpIds,
        HashSet<string> updatedErpIds,
        DateTime now)
    {
        foreach (var row in export.Components)
        {
            if (!components.TryGetValue(row.ErpId, out var component))
            {
                component = new Component { ErpId = row.ErpId, CreatedAt = now, UpdatedAt = now };
                Apply(component, row, isNew: true);
                component.LastSapSyncAt = now;
                db.Components.Add(component);
                components[row.ErpId] = component;
                addedErpIds.Add(row.ErpId);
                RecordHistory(row.ErpId, "status", null, row.Status);
                continue;
            }

            if (Apply(component, row, isNew: false))
            {
                component.UpdatedAt = now;
                updatedErpIds.Add(component.ErpId);
            }

            component.LastSapSyncAt = now;
        }

        var exportedErpIds = export.Components.Select(c => c.ErpId).ToHashSet();
        foreach (var component in components.Values.Where(c => !exportedErpIds.Contains(c.ErpId) && c.Status == ComponentStatus.Active))
        {
            RecordHistory(component.ErpId, "status", component.Status, ComponentStatus.Inactive);
            component.Status = ComponentStatus.Inactive;
            component.UpdatedAt = now;
            updatedErpIds.Add(component.ErpId);
        }
    }

    private bool Apply(Component component, SapComponentRow row, bool isNew)
    {
        var changed = false;

        void Set<T>(string field, T oldValue, T newValue, Action<T> assign)
        {
            if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
            {
                return;
            }

            if (!isNew)
            {
                RecordHistory(component.ErpId, field, oldValue, newValue);
            }

            assign(newValue);
            changed = true;
        }

        Set("name", component.Name, row.Name, v => component.Name = v);
        Set("status", component.Status, row.Status, v => component.Status = v);
        Set("family", component.Family, row.Family, v => component.Family = v);
        Set("material", component.Material, row.Material, v => component.Material = v);
        Set("measuredBy", component.MeasuredBy, row.MeasuredBy, v => component.MeasuredBy = v);
        Set("measureValue", component.MeasureValue, row.MeasureValue, v => component.MeasureValue = v);
        Set("measureUnit", component.MeasureUnit, row.MeasureUnit, v => component.MeasureUnit = v);
        Set("unitOfMeasureType", component.UnitOfMeasureType, row.UnitOfMeasureType, v => component.UnitOfMeasureType = v);
        Set("unitOfMeasureQuantity", component.UnitOfMeasureQuantity, row.UnitOfMeasureQuantity, v => component.UnitOfMeasureQuantity = v);
        Set("unitOfMeasureUnit", component.UnitOfMeasureUnit, row.UnitOfMeasureUnit, v => component.UnitOfMeasureUnit = v);
        Set("countryOfOrigin", component.CountryOfOrigin, row.CountryOfOrigin, v => component.CountryOfOrigin = v);
        Set("hsCode", component.HsCode, row.HsCode, v => component.HsCode = v);

        if (row.UnitCostAmount != null)
        {
            Set("unitCostAmount", component.UnitCostAmount, row.UnitCostAmount, v => component.UnitCostAmount = v);
            Set("unitCostCurrency", component.UnitCostCurrency, row.UnitCostCurrency, v => component.UnitCostCurrency = v);
            component.UnitCostSource = UnitCostSource;
        }

        return changed;
    }

    private async Task<int> SyncStock(
        SapExport export,
        Dictionary<string, Component> components,
        HashSet<string> updatedErpIds,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var factoryIds = (await db.Factories.AsNoTracking().Select(f => f.Id).ToListAsync(cancellationToken)).ToHashSet();
        var failedRows = 0;
        var snapshot = new Dictionary<(string ErpId, string FactoryId), (decimal Warehouse, decimal Floor)>();

        foreach (var row in export.Stock)
        {
            if (!components.ContainsKey(row.ErpId) || !factoryIds.Contains(row.FactoryId))
            {
                logger.LogWarning("SAP stock row skipped: unknown component {ErpId} or factory {FactoryId}", row.ErpId, row.FactoryId);
                failedRows++;
                continue;
            }

            var key = (row.ErpId, row.FactoryId);
            var stock = snapshot.GetValueOrDefault(key);
            snapshot[key] = row.Location == SapStockLocation.Warehouse
                ? (stock.Warehouse + row.Quantity, stock.Floor)
                : (stock.Warehouse, stock.Floor + row.Quantity);
        }

        var stockRows = (await db.ComponentFactories.ToListAsync(cancellationToken))
            .ToDictionary(f => (f.ComponentErpId, f.FactoryId));

        foreach (var key in snapshot.Keys.Where(key => !stockRows.ContainsKey(key)))
        {
            var created = new ComponentFactory { ComponentErpId = key.ErpId, FactoryId = key.FactoryId };
            db.ComponentFactories.Add(created);
            stockRows[key] = created;
        }

        foreach (var (key, stockRow) in stockRows)
        {
            var stock = snapshot.GetValueOrDefault(key);
            if (stockRow.WarehouseStock != stock.Warehouse || stockRow.ProductionFloorStock != stock.Floor)
            {
                updatedErpIds.Add(key.Item1);
            }

            stockRow.WarehouseStock = stock.Warehouse;
            stockRow.ProductionFloorStock = stock.Floor;
            stockRow.StockSyncedAt = now;
        }

        return failedRows;
    }

    private void RecordHistory(string erpId, string field, object oldValue, object newValue)
    {
        historyWriter.Record(HistoryEntityTypes.Component, erpId, field, oldValue, newValue, HistorySource.Sync, changedBy: ChangedBy);
    }
}
