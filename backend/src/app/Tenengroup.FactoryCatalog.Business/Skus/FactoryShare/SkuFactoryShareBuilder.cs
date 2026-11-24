using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.FactoryShare;
using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.Business.Skus.FactoryShare;

internal static class SkuFactoryShareBuilder
{
    public static IReadOnlyCollection<SkuFactoryShareDto> Build(
        IReadOnlyCollection<string> factoryIds,
        IReadOnlyCollection<SkuFactory> rows,
        IReadOnlyDictionary<string, decimal> defaults)
    {
        var rowsByFactory = rows
            .Where(r => factoryIds.Contains(r.FactoryId))
            .ToDictionary(r => r.FactoryId);
        var effective = SkuFactoryShares.Effective(rowsByFactory.Values, defaults);

        return factoryIds
            .Select(factoryId =>
            {
                var row = rowsByFactory.GetValueOrDefault(factoryId);
                return new SkuFactoryShareDto(
                    factoryId,
                    row?.Producible ?? false,
                    defaults.GetValueOrDefault(factoryId),
                    row?.OverrideSharePct,
                    effective.GetValueOrDefault(factoryId),
                    Source(row),
                    row?.OverrideBy,
                    row?.OverrideAt);
            })
            .ToList();
    }

    private static SkuShareSource Source(SkuFactory row)
    {
        if (row is not { Producible: true })
        {
            return SkuShareSource.NotProducible;
        }

        if (row.OverrideSharePct == null)
        {
            return SkuShareSource.Default;
        }

        return SkuFactoryShares.IsManagerOverride(row) ? SkuShareSource.Override : SkuShareSource.Rebalanced;
    }
}
