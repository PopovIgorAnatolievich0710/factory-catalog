using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Import;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.ProcessMappings;
using Tenengroup.FactoryCatalog.Business.Skus.Import;
using Tenengroup.FactoryCatalog.Business.Skus.Validation;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Skus.ProcessMappings;

public class ImportProcessMappingsCommandHandler(
    IFactoryCatalogDbContext db,
    ProcessMappingImportParser parser,
    SkuMappingValidator validator,
    IHistoryWriter historyWriter,
    TimeProvider timeProvider,
    ILogger<ImportProcessMappingsCommandHandler> logger)
    : IRequestHandler<ImportProcessMappingsCommand, ImportValidationResultDto>
{
    public async Task<ImportValidationResultDto> Handle(ImportProcessMappingsCommand request, CancellationToken cancellationToken)
    {
        var import = await parser.Parse(request.File, cancellationToken);
        ImportErrors.ThrowIfAny(import.Errors);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var skuUuids = import.Rows.Select(r => r.SkuUuid).Distinct().ToList();
        var skus = await db.Skus
            .Include(s => s.Components)
            .Include(s => s.Processes)
            .Where(s => skuUuids.Contains(s.SkuUuid))
            .ToListAsync(cancellationToken);
        foreach (var sku in skus)
        {
            var rows = import.Rows.Where(r => r.SkuUuid == sku.SkuUuid).ToList();
            var skuId = sku.SkuUuid.ToString();
            foreach (var removed in sku.Processes.Where(p => rows.All(r => r.ProcessId != p.ProcessId)).ToList())
            {
                sku.Processes.Remove(removed);
                historyWriter.Record(HistoryEntityTypes.Sku, skuId, Field(removed.ProcessId), removed.Quantity, null, HistorySource.Import);
            }

            foreach (var row in rows)
            {
                var existing = sku.Processes.FirstOrDefault(p => p.ProcessId == row.ProcessId);
                historyWriter.Record(HistoryEntityTypes.Sku, skuId, Field(row.ProcessId), existing?.Quantity, row.Quantity, HistorySource.Import);
                if (existing == null)
                {
                    sku.Processes.Add(new SkuProcess { SkuUuid = sku.SkuUuid, ProcessId = row.ProcessId, Quantity = row.Quantity });
                }
                else
                {
                    existing.Quantity = row.Quantity;
                }
            }

            sku.UpdatedAt = now;
        }

        await validator.Validate(skus, HistorySource.Import, null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "SKU process mappings imported: {Rows} rows for {Skus} SKUs, approved by {ApprovedBy}",
            import.Rows.Count, skus.Count, import.ApprovedBy);

        return ImportErrors.ToResult(import.TotalRows, import.Errors);
    }

    private static string Field(string processId) => $"process:{processId}";
}
