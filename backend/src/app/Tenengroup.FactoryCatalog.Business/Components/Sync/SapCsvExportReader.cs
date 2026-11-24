using System.Globalization;
using Microsoft.Extensions.Options;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components.Sync;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.Infrastructure.Common.Aws.S3;

namespace Tenengroup.FactoryCatalog.Business.Components.Sync;

public class SapCsvExportReader(IS3FileReader s3FileReader, IOptions<SapExportOptions> options) : ISapExportReader
{
    public const string ComponentsFileName = "components.csv";
    public const string StockFileName = "stock.csv";

    private static readonly string[] ComponentColumns = ["erp_id", "name", "status"];
    private static readonly string[] StockColumns = ["erp_id", "factory_id", "location", "quantity"];

    public async Task<SapExport> Read(DateOnly exportDate, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Bucket))
        {
            throw new InvalidOperationException("SapExport:Bucket is not configured");
        }

        var folder = string.Join('/', new[] { settings.Prefix.Trim('/'), exportDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) }
            .Where(part => part.Length > 0));

        var components = await s3FileReader.ReadAllText(settings.Bucket, $"{folder}/{ComponentsFileName}", cancellationToken);
        var stock = await s3FileReader.ReadAllText(settings.Bucket, $"{folder}/{StockFileName}", cancellationToken);

        return Parse(components, stock);
    }

    public static SapExport Parse(string componentsCsv, string stockCsv)
    {
        var errors = new List<SapExportRowError>();
        var components = new List<SapComponentRow>();
        var stock = new List<SapStockRow>();

        var componentsTable = CsvTable.Parse(ComponentsFileName, componentsCsv, ComponentColumns);
        var seenErpIds = new HashSet<string>();
        foreach (var row in componentsTable.Rows)
        {
            try
            {
                var component = ParseComponent(componentsTable, row);
                if (!seenErpIds.Add(component.ErpId))
                {
                    throw new FormatException($"duplicate erp_id '{component.ErpId}'");
                }

                components.Add(component);
            }
            catch (FormatException e)
            {
                errors.Add(new SapExportRowError(ComponentsFileName, row.Line, e.Message));
            }
        }

        var stockTable = CsvTable.Parse(StockFileName, stockCsv, StockColumns);
        foreach (var row in stockTable.Rows)
        {
            try
            {
                stock.Add(ParseStock(stockTable, row));
            }
            catch (FormatException e)
            {
                errors.Add(new SapExportRowError(StockFileName, row.Line, e.Message));
            }
        }

        return new SapExport(components, stock, errors);
    }

    private static SapComponentRow ParseComponent(CsvTable table, CsvRow row)
    {
        return new SapComponentRow(
            Required(table, row, "erp_id", 100),
            Required(table, row, "name", 300),
            Required(table, row, "status", 20).ToLowerInvariant() switch
            {
                "active" => ComponentStatus.Active,
                "inactive" => ComponentStatus.Inactive,
                var other => throw new FormatException($"status '{other}' is not active or inactive")
            },
            Text(table, row, "family", 100),
            Text(table, row, "material", 200),
            Text(table, row, "measured_by", 50),
            Number(table, row, "measure_value"),
            Text(table, row, "measure_unit", 20),
            Text(table, row, "uom_type", 50),
            Number(table, row, "uom_quantity"),
            Text(table, row, "uom_unit", 20),
            Text(table, row, "country_of_origin", 2)?.ToUpperInvariant(),
            Text(table, row, "hs_code", 20),
            Number(table, row, "unit_cost_amount"),
            Text(table, row, "unit_cost_currency", 3)?.ToUpperInvariant());
    }

    private static SapStockRow ParseStock(CsvTable table, CsvRow row)
    {
        return new SapStockRow(
            Required(table, row, "erp_id", 100),
            Required(table, row, "factory_id", 20),
            Required(table, row, "location", 20).ToLowerInvariant() switch
            {
                "warehouse" => SapStockLocation.Warehouse,
                "production_floor" => SapStockLocation.ProductionFloor,
                var other => throw new FormatException($"location '{other}' is not warehouse or production_floor")
            },
            Number(table, row, "quantity") ?? throw new FormatException("quantity is required"));
    }

    private static string Required(CsvTable table, CsvRow row, string column, int maxLength)
    {
        return Text(table, row, column, maxLength) ?? throw new FormatException($"{column} is required");
    }

    private static string Text(CsvTable table, CsvRow row, string column, int maxLength)
    {
        var value = table.Get(row, column);

        return value is { Length: var length } && length > maxLength
            ? throw new FormatException($"{column} is longer than {maxLength} characters")
            : value;
    }

    private static decimal? Number(CsvTable table, CsvRow row, string column)
    {
        var value = table.Get(row, column);
        if (value == null)
        {
            return null;
        }

        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
            ? number
            : throw new FormatException($"{column} '{value}' is not a number");
    }
}
