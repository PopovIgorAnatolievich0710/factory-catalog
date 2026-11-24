using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.FactoryShare;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Exceptions;

namespace Tenengroup.FactoryCatalog.Business.Skus.FactoryShare;

public class SkuFactoryShareService(
    IFactoryCatalogDbContext db,
    IHistoryWriter historyWriter,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public const string ProducibleField = "producible";
    public const string OverrideField = "overrideSharePct";

    public async Task<IReadOnlyList<string>> GetActiveFactoryIds(CancellationToken cancellationToken)
    {
        return await db.Factories
            .AsNoTracking()
            .Where(f => f.IsActive)
            .OrderBy(f => f.Id)
            .Select(f => f.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, decimal>> GetDefaultShares(CancellationToken cancellationToken)
    {
        return await db.GlobalFactoryShares
            .AsNoTracking()
            .ToDictionaryAsync(s => s.FactoryId, s => s.Pct, cancellationToken);
    }

    public async Task EnsureEditableFactory(string factoryId, CancellationToken cancellationToken)
    {
        await currentUser.EnsureCanEditFactory(factoryId, cancellationToken);
        if (!await db.Factories.AnyAsync(f => f.Id == factoryId && f.IsActive, cancellationToken))
        {
            throw new EntityNotFoundException(nameof(Factory), factoryId);
        }
    }

    public async Task<Sku> GetManufacturedSku(Guid skuUuid, CancellationToken cancellationToken)
    {
        var sku = await db.Skus
                      .Include(s => s.Factories)
                      .FirstOrDefaultAsync(s => s.SkuUuid == skuUuid, cancellationToken)
                  ?? throw new EntityNotFoundException(nameof(Sku), skuUuid);
        if (sku.ProductType == SkuProductType.Dropshipper)
        {
            throw new ValidationException("skuUuid", "Dropshipper SKUs have no factory share.");
        }

        return sku;
    }

    public async Task<SkuFactorySharesDto> GetShares(Sku sku, CancellationToken cancellationToken)
    {
        var factories = SkuFactoryShareBuilder.Build(
            await GetActiveFactoryIds(cancellationToken),
            sku.Factories,
            await GetDefaultShares(cancellationToken));

        return new SkuFactorySharesDto(sku.SkuUuid, factories, factories.Sum(f => f.EffectiveSharePct));
    }

    public bool SetProducible(Sku sku, string factoryId, bool producible, HistorySource source)
    {
        var row = sku.Factories.FirstOrDefault(f => f.FactoryId == factoryId);
        if (row == null)
        {
            if (!producible)
            {
                return false;
            }

            row = new SkuFactory { SkuUuid = sku.SkuUuid, FactoryId = factoryId };
            sku.Factories.Add(row);
        }

        var before = Snapshot(sku);
        SkuFactoryShares.SetProducible(sku.Factories, factoryId, producible);

        return RecordChanges(sku, before, factoryId, source);
    }

    public void SetOverride(Sku sku, string factoryId, decimal pct, IReadOnlyDictionary<string, decimal> defaults)
    {
        var before = Snapshot(sku);
        var error = SkuFactoryShares.SetOverride(
            sku.Factories,
            factoryId,
            pct,
            defaults,
            string.IsNullOrEmpty(currentUser.Email) ? currentUser.Name : currentUser.Email,
            timeProvider.GetUtcNow().UtcDateTime);
        if (error != null)
        {
            throw new ValidationException("sharePct", error);
        }

        RecordChanges(sku, before, factoryId, HistorySource.User);
    }

    public void ResetOverride(Sku sku, string factoryId, IReadOnlyDictionary<string, decimal> defaults)
    {
        var before = Snapshot(sku);
        SkuFactoryShares.ResetOverride(sku.Factories, factoryId, defaults);
        RecordChanges(sku, before, factoryId, HistorySource.User);
    }

    private static Dictionary<string, (bool Producible, decimal? OverridePct)> Snapshot(Sku sku)
    {
        return sku.Factories.ToDictionary(f => f.FactoryId, f => (f.Producible, f.OverrideSharePct));
    }

    private bool RecordChanges(
        Sku sku,
        Dictionary<string, (bool Producible, decimal? OverridePct)> before,
        string editedFactoryId,
        HistorySource source)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var skuId = sku.SkuUuid.ToString();
        var changed = false;
        foreach (var row in sku.Factories)
        {
            var old = before.GetValueOrDefault(row.FactoryId);
            if (old.Producible == row.Producible && old.OverridePct == row.OverrideSharePct)
            {
                continue;
            }

            var rowSource = row.FactoryId == editedFactoryId ? source : HistorySource.System;
            historyWriter.Record(HistoryEntityTypes.Sku, skuId, ProducibleField, old.Producible, row.Producible, rowSource, row.FactoryId);
            historyWriter.Record(HistoryEntityTypes.Sku, skuId, OverrideField, old.OverridePct, row.OverrideSharePct, rowSource, row.FactoryId);
            row.UpdatedAt = now;
            changed = true;
        }

        return changed;
    }
}
