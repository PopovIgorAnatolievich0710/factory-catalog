using Tenengroup.FactoryCatalog.Business.Quotes;

namespace Tenengroup.FactoryCatalog.Business.Skus.FactoryShare;

public static class FactoryShareCalculator
{
    public const decimal TotalPct = 100m;
    public const int Decimals = 2;

    public static IReadOnlyDictionary<string, decimal> Effective(IReadOnlyCollection<FactoryShareInput> factories)
    {
        var shares = factories.ToDictionary(f => f.FactoryId, _ => 0m);
        var remaining = factories
            .Where(f => f.CanProduce)
            .ToDictionary(f => f.FactoryId, f => f.OverridePct ?? f.DefaultPct);

        foreach (var share in ShareRedistribution.Redistribute(remaining))
        {
            shares[share.Key] = share.Value;
        }

        return shares;
    }

    public static IReadOnlyDictionary<string, decimal> Split(decimal pool, IReadOnlyCollection<KeyValuePair<string, decimal>> weights)
    {
        var shares = new Dictionary<string, decimal>();
        if (weights.Count == 0)
        {
            return shares;
        }

        var totalWeight = weights.Sum(w => w.Value);
        foreach (var weight in weights)
        {
            var portion = totalWeight == 0 ? pool / weights.Count : pool * weight.Value / totalWeight;
            shares[weight.Key] = Math.Round(portion, Decimals, MidpointRounding.AwayFromZero);
        }

        var largest = shares
            .OrderByDescending(share => share.Value)
            .ThenBy(share => share.Key, StringComparer.Ordinal)
            .First()
            .Key;
        shares[largest] += pool - shares.Values.Sum();

        return shares;
    }
}
