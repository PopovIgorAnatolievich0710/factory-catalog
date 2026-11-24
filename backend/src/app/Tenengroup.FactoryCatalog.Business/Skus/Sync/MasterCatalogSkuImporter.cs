using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Sync;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.Business.Skus.Sync;

public record MasterCatalogImportResult(int Added, int Updated, int Failed);

public class MasterCatalogSkuImporter(
    IFactoryCatalogDbContext db,
    IHistoryWriter historyWriter,
    TimeProvider timeProvider,
    ILogger<MasterCatalogSkuImporter> logger)
{
    public const string ChangedBy = "Master catalog sync";

    private const int SiteSkuMaxLength = 200;
    private const int NameMaxLength = 500;
    private const int AliasMaxLength = 200;
    private const int ErpIdMaxLength = 100;
    private const int DropshipperIdMaxLength = 100;
    private const int DropshipperNameMaxLength = 200;
    private const int CurrencyLength = 3;

    private record ParsedComponent(string ErpId, decimal Quantity, ComponentUsage Usage);

    private record ParsedSku(
        SkuProductType ProductType,
        SkuStatus Status,
        IReadOnlyCollection<MasterCatalogSkuAlias> Aliases,
        IReadOnlyCollection<ParsedComponent> Components);

    public async Task<MasterCatalogImportResult> Import(IReadOnlyCollection<MasterCatalogSku> items, CancellationToken cancellationToken)
    {
        var uuids = items.Select(i => i.SkuUuid).Distinct().ToList();
        var skus = await db.Skus
            .Include(s => s.Aliases)
            .Include(s => s.Components)
            .Include(s => s.Processes)
            .Include(s => s.Factories)
            .Where(s => uuids.Contains(s.SkuUuid))
            .ToDictionaryAsync(s => s.SkuUuid, cancellationToken);
        var aliasIds = items
            .SelectMany(i => i.Aliases ?? [])
            .Select(a => a.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();
        var aliasOwners = await db.SkuAliases
            .AsNoTracking()
            .Where(a => aliasIds.Contains(a.Alias))
            .ToDictionaryAsync(a => a.Alias, a => a.SkuUuid, cancellationToken);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var added = 0;
        var updated = 0;
        var failed = 0;
        foreach (var item in items)
        {
            var error = Parse(item, aliasOwners, out var parsed);
            if (error != null)
            {
                failed++;
                logger.LogWarning("Master catalog SKU {SkuUuid} skipped: {Error}", item.SkuUuid, error);
                continue;
            }

            var isNew = !skus.TryGetValue(item.SkuUuid, out var sku);
            if (isNew)
            {
                sku = new Sku { SkuUuid = item.SkuUuid, CreatedAt = now, ValidationStatus = SkuValidationStatus.Valid };
                db.Skus.Add(sku);
                skus[item.SkuUuid] = sku;
                historyWriter.Record(
                    HistoryEntityTypes.Sku, sku.SkuUuid.ToString(), "created", null, item.SiteSku, HistorySource.Sync, changedBy: ChangedBy);
            }

            var changed = Apply(sku, item, parsed, isNew);
            foreach (var alias in parsed.Aliases)
            {
                aliasOwners[alias.Id] = sku.SkuUuid;
            }

            sku.LastMasterSyncAt = now;
            if (isNew)
            {
                sku.UpdatedAt = now;
                added++;
            }
            else if (changed)
            {
                sku.UpdatedAt = now;
                updated++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return new MasterCatalogImportResult(added, updated, failed);
    }

    private static string Parse(MasterCatalogSku item, IReadOnlyDictionary<string, Guid> aliasOwners, out ParsedSku parsed)
    {
        parsed = null;
        if (item.SkuUuid == Guid.Empty)
        {
            return "skuUuid is missing";
        }

        if (string.IsNullOrWhiteSpace(item.SiteSku) || item.SiteSku.Length > SiteSkuMaxLength)
        {
            return "siteSku is missing or longer than 200 characters";
        }

        if (string.IsNullOrWhiteSpace(item.Name) || item.Name.Length > NameMaxLength)
        {
            return "name is missing or longer than 500 characters";
        }

        if (!TryParseEnum<SkuProductType>(item.ProductType, out var productType))
        {
            return $"productType '{item.ProductType}' is not supported";
        }

        if (!TryParseEnum<SkuStatus>(item.Status, out var status))
        {
            return $"status '{item.Status}' is not supported";
        }

        var aliases = item.Aliases ?? [];
        foreach (var alias in aliases)
        {
            if (string.IsNullOrWhiteSpace(alias.Id) || alias.Id.Length > AliasMaxLength)
            {
                return "alias id is missing or longer than 200 characters";
            }

            if (!TryParseEnum<SkuAliasType>(alias.Type, out _))
            {
                return $"alias type '{alias.Type}' is not supported";
            }

            if (aliasOwners.TryGetValue(alias.Id, out var owner) && owner != item.SkuUuid)
            {
                return $"alias '{alias.Id}' already belongs to SKU {owner}";
            }
        }

        if (aliases.Select(a => a.Id).Distinct().Count() != aliases.Count)
        {
            return "aliases contain duplicates";
        }

        var components = new List<ParsedComponent>();
        if (productType == SkuProductType.Dropshipper)
        {
            var dropshipper = item.Dropshipper;
            if (dropshipper?.TotalCost == null || string.IsNullOrWhiteSpace(dropshipper.DropshipperId))
            {
                return "dropshipper id and total cost are required for a dropshipper product";
            }

            if (dropshipper.DropshipperId.Length > DropshipperIdMaxLength
                || dropshipper.Name?.Length > DropshipperNameMaxLength
                || dropshipper.TotalCost.Currency?.Length != CurrencyLength
                || dropshipper.TotalCost.Amount < 0)
            {
                return "dropshipper id, name or total cost is not valid";
            }
        }
        else
        {
            foreach (var component in item.Components ?? [])
            {
                if (string.IsNullOrWhiteSpace(component.ErpId) || component.ErpId.Length > ErpIdMaxLength)
                {
                    return "component erpId is missing or longer than 100 characters";
                }

                if (component.Quantity <= 0)
                {
                    return $"component {component.ErpId} quantity must be greater than 0";
                }

                if (!TryParseEnum<ComponentUsage>(component.Usage, out var usage))
                {
                    return $"component {component.ErpId} usage '{component.Usage}' is not supported";
                }

                if (components.Any(c => c.ErpId == component.ErpId))
                {
                    return $"component {component.ErpId} is listed more than once";
                }

                components.Add(new ParsedComponent(component.ErpId, component.Quantity, usage));
            }
        }

        parsed = new ParsedSku(productType, status, aliases, components);
        return null;
    }

    private static bool TryParseEnum<T>(string value, out T result) where T : struct, Enum
    {
        return Enum.TryParse(value, ignoreCase: true, out result) && Enum.IsDefined(result) && !int.TryParse(value, out _);
    }

    private bool Apply(Sku sku, MasterCatalogSku item, ParsedSku parsed, bool isNew)
    {
        var isDropshipper = parsed.ProductType == SkuProductType.Dropshipper;
        var dropshipper = isDropshipper ? item.Dropshipper : null;
        var changed = false;

        changed |= Change(sku, isNew, "siteSku", sku.SiteSku, item.SiteSku, () => sku.SiteSku = item.SiteSku);
        changed |= Change(sku, isNew, "name", sku.Name, item.Name, () => sku.Name = item.Name);
        changed |= Change(sku, isNew, "productType", sku.ProductType, parsed.ProductType, () => sku.ProductType = parsed.ProductType);
        changed |= Change(sku, isNew, "status", sku.Status, parsed.Status, () => sku.Status = parsed.Status);
        changed |= Change(sku, isNew, "dropshipperId", sku.DropshipperId, dropshipper?.DropshipperId,
            () => sku.DropshipperId = dropshipper?.DropshipperId);
        changed |= Change(sku, isNew, "dropshipperName", sku.DropshipperName, dropshipper?.Name,
            () => sku.DropshipperName = dropshipper?.Name);
        changed |= Change(sku, isNew, "dropshipperTotalCostAmount", sku.DropshipperTotalCostAmount, dropshipper?.TotalCost.Amount,
            () => sku.DropshipperTotalCostAmount = dropshipper?.TotalCost.Amount);
        changed |= Change(sku, isNew, "dropshipperTotalCostCurrency", sku.DropshipperTotalCostCurrency, dropshipper?.TotalCost.Currency,
            () => sku.DropshipperTotalCostCurrency = dropshipper?.TotalCost.Currency);

        changed |= ApplyAliases(sku, parsed, isNew);
        changed |= ApplyComponents(sku, parsed, isNew);
        if (isDropshipper)
        {
            changed |= ClearFactoryData(sku);
        }

        return changed;
    }

    private bool ApplyAliases(Sku sku, ParsedSku parsed, bool isNew)
    {
        var changed = false;
        foreach (var removed in sku.Aliases.Where(a => parsed.Aliases.All(p => p.Id != a.Alias)).ToList())
        {
            sku.Aliases.Remove(removed);
            changed |= Change(sku, isNew, "alias", removed.Alias, null, () => { });
        }

        foreach (var alias in parsed.Aliases)
        {
            var existing = sku.Aliases.FirstOrDefault(a => a.Alias == alias.Id);
            if (existing == null)
            {
                sku.Aliases.Add(new SkuAlias
                {
                    SkuUuid = sku.SkuUuid,
                    Alias = alias.Id,
                    Type = Enum.Parse<SkuAliasType>(alias.Type, ignoreCase: true),
                    Deprecated = alias.Deprecated
                });
                changed |= Change(sku, isNew, "alias", null, alias.Id, () => { });
            }
            else
            {
                changed |= Change(sku, isNew, $"alias:{alias.Id}:deprecated", existing.Deprecated, alias.Deprecated,
                    () => existing.Deprecated = alias.Deprecated);
            }
        }

        return changed;
    }

    private bool ApplyComponents(Sku sku, ParsedSku parsed, bool isNew)
    {
        var changed = false;
        foreach (var removed in sku.Components.Where(c => parsed.Components.All(p => p.ErpId != c.ComponentErpId)).ToList())
        {
            sku.Components.Remove(removed);
            changed |= Change(sku, isNew, $"component:{removed.ComponentErpId}", Describe(removed.Quantity, removed.Usage), null, () => { });
        }

        foreach (var component in parsed.Components)
        {
            var existing = sku.Components.FirstOrDefault(c => c.ComponentErpId == component.ErpId);
            if (existing == null)
            {
                sku.Components.Add(new SkuComponent
                {
                    SkuUuid = sku.SkuUuid,
                    ComponentErpId = component.ErpId,
                    Quantity = component.Quantity,
                    Usage = component.Usage
                });
                changed |= Change(sku, isNew, $"component:{component.ErpId}", null, Describe(component.Quantity, component.Usage), () => { });
            }
            else
            {
                changed |= Change(
                    sku, isNew, $"component:{component.ErpId}",
                    Describe(existing.Quantity, existing.Usage), Describe(component.Quantity, component.Usage),
                    () =>
                    {
                        existing.Quantity = component.Quantity;
                        existing.Usage = component.Usage;
                    });
            }
        }

        return changed;
    }

    private bool ClearFactoryData(Sku sku)
    {
        var changed = false;
        foreach (var process in sku.Processes.ToList())
        {
            sku.Processes.Remove(process);
            changed |= Change(sku, false, $"process:{process.ProcessId}", (decimal?)process.Quantity, null, () => { });
        }

        foreach (var factory in sku.Factories.ToList())
        {
            sku.Factories.Remove(factory);
            historyWriter.Record(
                HistoryEntityTypes.Sku, sku.SkuUuid.ToString(), "producible", factory.Producible, false, HistorySource.Sync, factory.FactoryId,
                ChangedBy);
            changed = true;
        }

        return changed;
    }

    private static string Describe(decimal quantity, ComponentUsage usage)
    {
        return FormattableString.Invariant($"{quantity:0.####} {usage}");
    }

    private bool Change<T>(Sku sku, bool isNew, string field, T oldValue, T newValue, Action apply)
    {
        if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
        {
            return false;
        }

        if (!isNew)
        {
            historyWriter.Record(
                HistoryEntityTypes.Sku, sku.SkuUuid.ToString(), field, oldValue, newValue, HistorySource.Sync, changedBy: ChangedBy);
        }

        apply();
        return true;
    }
}
