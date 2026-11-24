using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Import;
using Tenengroup.FactoryCatalog.Business.Skus.Import;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.Business.Skus.ProcessMappings;

public record ProcessMappingImportRow(Guid SkuUuid, string ProcessId, decimal Quantity);

public record ProcessMappingImport(
    int TotalRows,
    IReadOnlyCollection<ProcessMappingImportRow> Rows,
    IReadOnlyCollection<ImportRowErrorDto> Errors,
    string ApprovedBy);

public class ProcessMappingImportParser(IFactoryCatalogDbContext db, SkuIdentifierResolver resolver)
{
    public const string SkuColumn = "SKU";
    public const string ProcessColumn = "Process ID";
    public const string QuantityColumn = "Quantity";
    public const string ApprovalSheet = "Approval";
    public const string ApprovedByKey = "Approved by";
    public const string ApprovedOnKey = "Approved on";
    public const string ApprovalField = "approval";
    public const int FileRow = 0;

    private static readonly string[] Columns = [SkuColumn, ProcessColumn, QuantityColumn];

    public async Task<ProcessMappingImport> Parse(byte[] file, CancellationToken cancellationToken)
    {
        using var workbook = ExcelImportReader.Open(file);
        var sheetRows = ExcelImportReader.ReadRows(workbook, Columns);
        var approval = ExcelImportReader.ReadKeyValues(workbook, ApprovalSheet);
        var approvedBy = approval.GetValueOrDefault(ApprovedByKey, string.Empty);

        var errors = new List<ImportRowErrorDto>();
        if (approvedBy.Length == 0 || approval.GetValueOrDefault(ApprovedOnKey, string.Empty).Length == 0)
        {
            errors.Add(new ImportRowErrorDto(
                FileRow,
                ApprovalField,
                $"Only approved files can be imported: fill '{ApprovedByKey}' and '{ApprovedOnKey}' on the '{ApprovalSheet}' sheet."));
        }

        var resolution = await resolver.Load(sheetRows.Select(r => r.Value(SkuColumn)).ToList(), cancellationToken);
        var processIds = await db.Processes.AsNoTracking().Select(p => p.ProcessId).ToListAsync(cancellationToken);

        var rows = new List<ProcessMappingImportRow>();
        foreach (var sheetRow in sheetRows)
        {
            var rowErrors = new List<ImportRowErrorDto>();
            void Error(string field, string message) => rowErrors.Add(new ImportRowErrorDto(sheetRow.RowNumber, field, message));

            var identifier = sheetRow.Value(SkuColumn);
            ResolvedSku sku = null;
            if (identifier.Length == 0)
            {
                Error(SkuColumn, "SKU is required.");
            }
            else
            {
                sku = resolution.Resolve(identifier, out var resolveError);
                if (sku == null)
                {
                    Error(SkuColumn, resolveError);
                }
                else if (sku.ProductType == SkuProductType.Dropshipper)
                {
                    Error(SkuColumn, $"SKU '{identifier}' is a dropshipper product and has no processes.");
                }
            }

            var processText = sheetRow.Value(ProcessColumn);
            var processId = processIds.FirstOrDefault(id => string.Equals(id, processText, StringComparison.OrdinalIgnoreCase));
            if (processText.Length == 0)
            {
                Error(ProcessColumn, "Process ID is required.");
            }
            else if (processId == null)
            {
                Error(ProcessColumn, $"Process '{processText}' is not found in the catalog.");
            }

            if (!decimal.TryParse(sheetRow.Value(QuantityColumn), NumberStyles.Number, CultureInfo.InvariantCulture, out var quantity)
                || quantity <= 0)
            {
                Error(QuantityColumn, "Quantity must be a number greater than 0, in the process unit.");
            }

            if (rowErrors.Count == 0 && rows.Any(r => r.SkuUuid == sku.SkuUuid && r.ProcessId == processId))
            {
                Error(ProcessColumn, $"SKU '{identifier}' and process {processId} are listed more than once.");
            }

            if (rowErrors.Count == 0)
            {
                rows.Add(new ProcessMappingImportRow(sku.SkuUuid, processId, quantity));
            }

            errors.AddRange(rowErrors);
        }

        return new ProcessMappingImport(sheetRows.Count, rows, errors, approvedBy);
    }
}
