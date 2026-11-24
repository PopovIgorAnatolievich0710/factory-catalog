using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.Business.Skus.FactoryShare;

public static class SkuFactoryShares
{
    public static bool IsManagerOverride(SkuFactory factory)
    {
        return factory.OverrideSharePct != null && factory.OverrideBy != null;
    }

    public static IReadOnlyDictionary<string, decimal> Effective(
        IReadOnlyCollection<SkuFactory> factories,
        IReadOnlyDictionary<string, decimal> defaults)
    {
        return FactoryShareCalculator.Effective(factories
            .Select(f => new FactoryShareInput(f.FactoryId, defaults.GetValueOrDefault(f.FactoryId), f.OverrideSharePct, f.Producible))
            .ToList());
    }

    public static string? SetOverride(
        IReadOnlyCollection<SkuFactory> factories,
        string factoryId,
        decimal pct,
        IReadOnlyDictionary<string, decimal> defaults,
        string overrideBy,
        DateTime overrideAt)
    {
        var target = factories.FirstOrDefault(f => f.FactoryId == factoryId && f.Producible);
        if (target == null)
        {
            return $"Factory {factoryId} is not producible for this SKU, its share is 0%.";
        }

        if (pct < 0 || pct > FactoryShareCalculator.TotalPct)
        {
            return "Share must be between 0 and 100.";
        }

        if (decimal.Round(pct, FactoryShareCalculator.Decimals) != pct)
        {
            return "Share can have at most 2 decimals.";
        }

        var others = factories.Where(f => f.Producible && f.FactoryId != factoryId).ToList();
        if (others.Count == 0 && pct != FactoryShareCalculator.TotalPct)
        {
            return $"Factory {factoryId} is the only producible factory, its share must be 100%.";
        }

        var effective = Effective(factories, defaults);
        target.OverrideSharePct = pct;
        target.OverrideBy = overrideBy;
        target.OverrideAt = overrideAt;
        SplitAmong(others, FactoryShareCalculator.TotalPct - pct, effective);

        return null;
    }

    public static void ResetOverride(
        IReadOnlyCollection<SkuFactory> factories,
        string factoryId,
        IReadOnlyDictionary<string, decimal> defaults)
    {
        var target = factories.FirstOrDefault(f => f.FactoryId == factoryId);
        if (target == null)
        {
            return;
        }

        var effective = Effective(factories, defaults);
        ClearOverride(target);
        if (ClearRebalancedWithoutManagerOverride(factories) || !target.Producible)
        {
            return;
        }

        var others = factories.Where(f => f.Producible && f.FactoryId != factoryId).ToList();
        SplitAmong(others, FactoryShareCalculator.TotalPct - defaults.GetValueOrDefault(factoryId), effective);
    }

    public static void SetProducible(IReadOnlyCollection<SkuFactory> factories, string factoryId, bool producible)
    {
        var target = factories.First(f => f.FactoryId == factoryId);
        target.Producible = producible;
        if (!producible)
        {
            ClearOverride(target);
        }

        ClearRebalancedWithoutManagerOverride(factories);
    }

    private static bool ClearRebalancedWithoutManagerOverride(IReadOnlyCollection<SkuFactory> factories)
    {
        if (factories.Any(f => f.Producible && IsManagerOverride(f)))
        {
            return false;
        }

        foreach (var factory in factories)
        {
            ClearOverride(factory);
        }

        return true;
    }

    private static void SplitAmong(
        IReadOnlyCollection<SkuFactory> factories,
        decimal pool,
        IReadOnlyDictionary<string, decimal> weights)
    {
        var shares = FactoryShareCalculator.Split(
            pool,
            factories.Select(f => new KeyValuePair<string, decimal>(f.FactoryId, weights[f.FactoryId])).ToList());
        foreach (var factory in factories)
        {
            factory.OverrideSharePct = shares[factory.FactoryId];
        }
    }

    private static void ClearOverride(SkuFactory factory)
    {
        factory.OverrideSharePct = null;
        factory.OverrideBy = null;
        factory.OverrideAt = null;
    }
}
