using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.Business.Skus.Validation;

public class SkuMappingValidator(IFactoryCatalogDbContext db, IHistoryWriter historyWriter)
{
    public const int BatchSize = 500;
    public const string StatusField = "validationStatus";
    public const string IssuesField = "validationIssues";

    public async Task ValidateAll(HistorySource source, string changedBy, CancellationToken cancellationToken)
    {
        for (var skip = 0; ; skip += BatchSize)
        {
            var skus = await db.Skus
                .Include(s => s.Components)
                .Include(s => s.Processes)
                .OrderBy(s => s.SkuUuid)
                .Skip(skip)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);
            if (skus.Count == 0)
            {
                return;
            }

            await Validate(skus, source, changedBy, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task Validate(IReadOnlyCollection<Sku> skus, HistorySource source, string changedBy, CancellationToken cancellationToken)
    {
        var erpIds = skus.SelectMany(s => s.Components).Select(c => c.ComponentErpId).Distinct().ToList();
        var componentStatuses = await db.Components
            .AsNoTracking()
            .Where(c => erpIds.Contains(c.ErpId))
            .ToDictionaryAsync(c => c.ErpId, c => c.Status, cancellationToken);
        var processIds = (await db.Processes.AsNoTracking().Select(p => p.ProcessId).ToListAsync(cancellationToken)).ToHashSet();

        foreach (var sku in skus)
        {
            var issues = SkuMappingValidation.Issues(sku, componentStatuses, processIds);
            var status = issues.Count == 0 ? SkuValidationStatus.Valid : SkuValidationStatus.Invalid;
            if (status == sku.ValidationStatus && issues.SequenceEqual(sku.ValidationIssues))
            {
                continue;
            }

            var skuId = sku.SkuUuid.ToString();
            historyWriter.Record(HistoryEntityTypes.Sku, skuId, StatusField, sku.ValidationStatus, status, source, changedBy: changedBy);
            historyWriter.Record(
                HistoryEntityTypes.Sku, skuId, IssuesField, string.Join("; ", sku.ValidationIssues), string.Join("; ", issues), source,
                changedBy: changedBy);
            sku.ValidationStatus = status;
            sku.ValidationIssues = issues;
        }
    }
}
