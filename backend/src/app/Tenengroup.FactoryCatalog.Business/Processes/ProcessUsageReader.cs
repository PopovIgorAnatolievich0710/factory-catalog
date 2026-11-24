using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Processes;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Quotes;

namespace Tenengroup.FactoryCatalog.Business.Processes;

public class ProcessUsageReader(IFactoryCatalogDbContext db, TimeProvider timeProvider) : IProcessUsageReader
{
    public async Task<IReadOnlyDictionary<ProcessFactoryKey, int>> GetUsedToday(CancellationToken cancellationToken)
    {
        var dayStart = timeProvider.GetUtcNow().UtcDateTime.Date;
        var dayEnd = dayStart.AddDays(1);

        var produced = await db.Quotes
            .AsNoTracking()
            .Where(q => q.Status == QuoteStatus.Produced && q.ProducedAt >= dayStart && q.ProducedAt < dayEnd)
            .Select(q => new { q.QuoteSetId, q.FactoryId })
            .ToListAsync(cancellationToken);

        var used = new Dictionary<ProcessFactoryKey, int>();

        if (produced.Count == 0)
        {
            return used;
        }

        var quoteSetIds = produced.Select(q => q.QuoteSetId).Distinct().ToList();
        var skuByQuoteSet = (await db.QuoteSets
                .AsNoTracking()
                .Where(s => quoteSetIds.Contains(s.Id))
                .Select(s => new { s.Id, s.SkuUuid })
                .ToListAsync(cancellationToken))
            .ToDictionary(s => s.Id, s => s.SkuUuid);

        var skuUuids = skuByQuoteSet.Values.Distinct().ToList();
        var processIdsBySku = (await db.SkuProcesses
                .AsNoTracking()
                .Where(p => skuUuids.Contains(p.SkuUuid))
                .Select(p => new { p.SkuUuid, p.ProcessId })
                .ToListAsync(cancellationToken))
            .ToLookup(p => p.SkuUuid, p => p.ProcessId);

        foreach (var quote in produced.Where(q => skuByQuoteSet.ContainsKey(q.QuoteSetId)))
        {
            foreach (var processId in processIdsBySku[skuByQuoteSet[quote.QuoteSetId]])
            {
                var key = new ProcessFactoryKey(processId, quote.FactoryId);
                used[key] = used.GetValueOrDefault(key) + 1;
            }
        }

        return used;
    }
}
