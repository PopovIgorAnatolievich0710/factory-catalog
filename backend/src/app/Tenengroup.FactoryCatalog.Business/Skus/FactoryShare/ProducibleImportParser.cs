using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Import;
using Tenengroup.FactoryCatalog.Business.Skus.Import;
using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.Business.Skus.FactoryShare;

public record ProducibleImportRow(Guid SkuUuid, string FactoryId, bool Producible);

public record ProducibleImport(int TotalRows, IReadOnlyCollection<ProducibleImportRow> Rows, IReadOnlyCollection<ImportRowErrorDto> Errors);

public class ProducibleImportParser(
    SkuIdentifierResolver resolver,
    SkuFactoryShareService shareService,
    ICurrentUser currentUser)
{
    public const string SkuColumn = "SKU";
    public const string FactoryColumn = "Factory ID";
    public const string ProducibleColumn = "Producible";

    private static readonly string[] Columns = [SkuColumn, FactoryColumn, ProducibleColumn];
    private static readonly string[] TrueValues = ["yes", "y", "true", "1"];
    private static readonly string[] FalseValues = ["no", "n", "false", "0"];

    public async Task<ProducibleImport> Parse(byte[] file, CancellationToken cancellationToken)
    {
        using var workbook = ExcelImportReader.Open(file);
        var sheetRows = ExcelImportReader.ReadRows(workbook, Columns);
        var resolution = await resolver.Load(sheetRows.Select(r => r.Value(SkuColumn)).ToList(), cancellationToken);
        var activeFactoryIds = await shareService.GetActiveFactoryIds(cancellationToken);
        var editableFactoryIds = new List<string>();
        foreach (var factoryId in activeFactoryIds)
        {
            if (await currentUser.CanEditFactory(factoryId, cancellationToken))
            {
                editableFactoryIds.Add(factoryId);
            }
        }

        var rows = new List<ProducibleImportRow>();
        var errors = new List<ImportRowErrorDto>();
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
                    Error(SkuColumn, $"SKU '{identifier}' is a dropshipper product and has no factory share.");
                }
            }

            var factoryId = sheetRow.Value(FactoryColumn);
            if (!activeFactoryIds.Contains(factoryId))
            {
                Error(FactoryColumn, $"Factory '{factoryId}' is not an active factory.");
            }
            else if (!editableFactoryIds.Contains(factoryId))
            {
                Error(FactoryColumn, $"You can edit only your own factory, not {factoryId}.");
            }

            var producibleText = sheetRow.Value(ProducibleColumn).ToLowerInvariant();
            var isTrue = TrueValues.Contains(producibleText);
            if (!isTrue && !FalseValues.Contains(producibleText))
            {
                Error(ProducibleColumn, "Producible must be Yes or No.");
            }

            if (rowErrors.Count == 0 && rows.Any(r => r.SkuUuid == sku.SkuUuid && r.FactoryId == factoryId))
            {
                Error(SkuColumn, $"SKU '{identifier}' and factory {factoryId} are listed more than once.");
            }

            if (rowErrors.Count == 0)
            {
                rows.Add(new ProducibleImportRow(sku.SkuUuid, factoryId, isTrue));
            }

            errors.AddRange(rowErrors);
        }

        return new ProducibleImport(sheetRows.Count, rows, errors);
    }
}
