using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Details;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Quotes;

namespace Tenengroup.FactoryCatalog.Business.Skus.ProductionConflicts;

public class ProductionConflictReader(IFactoryCatalogDbContext db)
{
    public async Task<IReadOnlyDictionary<Guid, List<SkuProductionConflictIssueDto>>> Get(
        IReadOnlyCollection<Guid> skuUuids,
        CancellationToken cancellationToken)
    {
        var issues = new Dictionary<Guid, List<SkuProductionConflictIssueDto>>();
        var produced = await db.Quotes
            .AsNoTracking()
            .Where(q => q.Status == QuoteStatus.Produced && q.ProductionLogJson != null && q.ProducedAt != null)
            .Join(
                db.QuoteSets.AsNoTracking().Where(s => skuUuids.Contains(s.SkuUuid)),
                q => q.QuoteSetId,
                s => s.Id,
                (q, s) => new { q.Id, s.SkuUuid, q.FactoryId, ProducedAt = q.ProducedAt.Value, s.OrderId, s.OrderItemId })
            .ToListAsync(cancellationToken);
        if (produced.Count == 0)
        {
            return issues;
        }

        var latest = produced
            .GroupBy(p => new { p.SkuUuid, p.FactoryId })
            .Select(g => g.OrderByDescending(p => p.ProducedAt).ThenByDescending(p => p.Id).First())
            .ToList();
        var latestIds = latest.Select(p => p.Id).ToList();
        var logs = await db.Quotes
            .AsNoTracking()
            .Where(q => latestIds.Contains(q.Id))
            .ToDictionaryAsync(q => q.Id, q => q.ProductionLogJson, cancellationToken);

        var conflictSkuUuids = latest.Select(p => p.SkuUuid).Distinct().ToList();
        var mappings = await db.SkuProcesses
            .AsNoTracking()
            .Where(p => conflictSkuUuids.Contains(p.SkuUuid))
            .Select(p => new { p.SkuUuid, p.ProcessId })
            .ToListAsync(cancellationToken);
        var processes = await db.Processes
            .AsNoTracking()
            .Select(p => new { p.ProcessId, p.Name, p.Type })
            .ToListAsync(cancellationToken);
        var machineTypes = await db.ProcessFactories
            .AsNoTracking()
            .Select(p => new { p.ProcessId, p.FactoryId, p.MachineType })
            .ToListAsync(cancellationToken);

        foreach (var quote in latest)
        {
            var assigned = mappings
                .Where(m => m.SkuUuid == quote.SkuUuid)
                .Select(m => new ProductionLogEntry(
                    m.ProcessId,
                    machineTypes.FirstOrDefault(t => t.ProcessId == m.ProcessId && t.FactoryId == quote.FactoryId)?.MachineType))
                .ToList();
            var conflicts = ProductionConflictDetector.Detect(assigned, ProductionConflictDetector.ParseLog(logs[quote.Id]));
            if (conflicts.Count == 0)
            {
                continue;
            }

            if (!issues.TryGetValue(quote.SkuUuid, out var skuIssues))
            {
                issues[quote.SkuUuid] = skuIssues = [];
            }

            skuIssues.AddRange(conflicts.Select(c =>
            {
                var process = processes.FirstOrDefault(p => string.Equals(p.ProcessId, c.ProcessId, StringComparison.OrdinalIgnoreCase));
                return new SkuProductionConflictIssueDto(
                    quote.FactoryId, c.Kind, c.ProcessId, process?.Name, process?.Type, c.MachineType, c.Expected, c.Actual,
                    quote.OrderId, quote.OrderItemId, quote.ProducedAt);
            }));
        }

        return issues;
    }
}
