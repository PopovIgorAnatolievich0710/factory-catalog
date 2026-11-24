namespace Tenengroup.FactoryCatalog.Business.Quotes;

internal static class ShareRedistribution
{
    private const decimal FullShare = 100m;

    public static Dictionary<string, decimal> Redistribute(IReadOnlyDictionary<string, decimal> remainingFactoryShares)
    {
        if (remainingFactoryShares.Count == 0)
        {
            return new Dictionary<string, decimal>();
        }

        var total = remainingFactoryShares.Values.Sum();
        var result = remainingFactoryShares.ToDictionary(
            share => share.Key,
            share => Math.Round(
                total > 0 ? share.Value / total * FullShare : FullShare / remainingFactoryShares.Count,
                2,
                MidpointRounding.AwayFromZero));

        var largest = result
            .OrderByDescending(share => share.Value)
            .ThenBy(share => share.Key, StringComparer.Ordinal)
            .First()
            .Key;
        result[largest] += FullShare - result.Values.Sum();

        return result;
    }
}
