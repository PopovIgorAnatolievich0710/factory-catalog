using Tenengroup.FactoryCatalog.Domains.Components;

namespace Tenengroup.FactoryCatalog.Business.Components;

public record ComponentStockSummary(StockAlertStatus Status, IReadOnlyCollection<string> FactoryIds, decimal TotalStock)
{
    public bool IsOutOfStockOrConflict => Status is StockAlertStatus.OutOfStock or StockAlertStatus.OosConflict;

    public static ComponentStockSummary From(decimal? criticalLevel, IEnumerable<ComponentFactory> factoryRows)
    {
        var statuses = factoryRows
            .OrderBy(f => f.FactoryId)
            .Select(f => new { f.FactoryId, Status = f.GetStockAlertStatus(criticalLevel), Total = f.GetTotalStock() })
            .ToList();

        var worst = statuses.Count == 0 ? StockAlertStatus.Ok : statuses.Max(s => s.Status);
        var factoryIds = worst == StockAlertStatus.Ok
            ? []
            : statuses.Where(s => s.Status == worst).Select(s => s.FactoryId).ToList();

        return new ComponentStockSummary(worst, factoryIds, statuses.Sum(s => s.Total));
    }
}
