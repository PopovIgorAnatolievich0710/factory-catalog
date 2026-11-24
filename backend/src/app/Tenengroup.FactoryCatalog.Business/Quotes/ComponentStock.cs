using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Quotes;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Components;

namespace Tenengroup.FactoryCatalog.Business.Quotes;

internal class ComponentStock
{
    private readonly Dictionary<string, Component> components;
    private readonly Dictionary<(string ErpId, string FactoryId), ComponentFactory> stockRows;
    private readonly ILookup<string, string> balancerAlternatives;

    private ComponentStock(
        Dictionary<string, Component> components,
        Dictionary<(string ErpId, string FactoryId), ComponentFactory> stockRows,
        ILookup<string, string> balancerAlternatives)
    {
        this.components = components;
        this.stockRows = stockRows;
        this.balancerAlternatives = balancerAlternatives;
    }

    public static async Task<ComponentStock> Load(
        IFactoryCatalogDbContext db,
        IReadOnlyCollection<string> erpIds,
        CancellationToken cancellationToken)
    {
        var alternatives = await db.ComponentAlternatives
            .AsNoTracking()
            .Where(a => a.ForBalancer && erpIds.Contains(a.ComponentErpId))
            .ToListAsync(cancellationToken);

        var allErpIds = erpIds.Concat(alternatives.Select(a => a.AlternativeErpId)).Distinct().ToList();

        var components = await db.Components
            .AsNoTracking()
            .Where(c => allErpIds.Contains(c.ErpId))
            .ToListAsync(cancellationToken);

        var stockRows = await db.ComponentFactories
            .AsNoTracking()
            .Where(s => allErpIds.Contains(s.ComponentErpId))
            .ToListAsync(cancellationToken);

        return new ComponentStock(
            components.ToDictionary(c => c.ErpId),
            stockRows.ToDictionary(s => (s.ComponentErpId, s.FactoryId)),
            alternatives.ToLookup(a => a.ComponentErpId, a => a.AlternativeErpId));
    }

    public bool IsKnown(string erpId) => components.ContainsKey(erpId);

    public Component Get(string erpId) => components[erpId];

    public string Status(string erpId, string factoryId)
    {
        if (!stockRows.TryGetValue((erpId, factoryId), out var stockRow))
        {
            return StockStatuses.OutOfStock;
        }

        return stockRow.GetStockStatus(components[erpId].CriticalStockLevel) switch
        {
            StockStatus.OutOfStock => StockStatuses.OutOfStock,
            StockStatus.Low => StockStatuses.Low,
            _ => StockStatuses.Ok
        };
    }

    public string FindAlternativeInStock(string erpId, string factoryId)
    {
        return balancerAlternatives[erpId]
            .Where(alternative => IsKnown(alternative) && components[alternative].Status == ComponentStatus.Active)
            .Select(alternative => new { ErpId = alternative, Status = Status(alternative, factoryId) })
            .Where(alternative => alternative.Status != StockStatuses.OutOfStock)
            .OrderBy(alternative => alternative.Status == StockStatuses.Ok ? 0 : 1)
            .ThenBy(alternative => alternative.ErpId, StringComparer.Ordinal)
            .Select(alternative => alternative.ErpId)
            .FirstOrDefault();
    }
}
