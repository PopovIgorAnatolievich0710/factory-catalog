using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Processes;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Quotes;
using Tenengroup.Infrastructure.Common.Json;

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
            .Select(q => new { q.QuoteSetId, q.FactoryId, q.ProcessesJson })
            .ToListAsync(cancellationToken);

        var used = new Dictionary<ProcessFactoryKey, int>();

        if (produced.Count == 0)
        {
            return used;
        }

        var quoteSetIds = produced.Select(q => q.QuoteSetId).Distinct().ToList();
        var quoteSets = (await db.QuoteSets
                .AsNoTracking()
                .Where(s => quoteSetIds.Contains(s.Id))
                .Select(s => new { s.Id, s.SkuUuid, s.Quantity })
                .ToListAsync(cancellationToken))
            .ToDictionary(s => s.Id);

        var quotes = produced
            .Where(q => quoteSets.ContainsKey(q.QuoteSetId))
            .Select(q => new { q.FactoryId, QuoteSet = quoteSets[q.QuoteSetId], ProcessIds = ReadProcessIds(q.ProcessesJson) })
            .ToList();

        var unmappedSkuUuids = quotes.Where(q => q.ProcessIds.Count == 0).Select(q => q.QuoteSet.SkuUuid).Distinct().ToList();
        var skuProcessIds = unmappedSkuUuids.Count == 0
            ? null
            : (await db.SkuProcesses
                .AsNoTracking()
                .Where(p => unmappedSkuUuids.Contains(p.SkuUuid))
                .Select(p => new { p.SkuUuid, p.ProcessId })
                .ToListAsync(cancellationToken))
            .ToLookup(p => p.SkuUuid, p => p.ProcessId);

        foreach (var quote in quotes)
        {
            var processIds = quote.ProcessIds.Count > 0 ? quote.ProcessIds : skuProcessIds[quote.QuoteSet.SkuUuid].ToList();

            foreach (var processId in processIds.Distinct())
            {
                var key = new ProcessFactoryKey(processId, quote.FactoryId);
                used[key] = used.GetValueOrDefault(key) + quote.QuoteSet.Quantity;
            }
        }

        return used;
    }

    private static List<string> ReadProcessIds(string processesJson)
    {
        if (string.IsNullOrWhiteSpace(processesJson))
        {
            return [];
        }

        try
        {
            return (processesJson.FromJsonString<List<QuoteProcess>>() ?? [])
                .Where(p => !string.IsNullOrEmpty(p?.ProcessId))
                .Select(p => p.ProcessId)
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private record QuoteProcess(string ProcessId);
}
