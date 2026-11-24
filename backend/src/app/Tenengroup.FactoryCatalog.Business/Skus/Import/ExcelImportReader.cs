using System.Globalization;
using ClosedXML.Excel;
using Tenengroup.Infrastructure.Common.Exceptions;

namespace Tenengroup.FactoryCatalog.Business.Skus.Import;

internal record ExcelImportRow(int RowNumber, IReadOnlyDictionary<string, string> Values)
{
    public string Value(string column) => Values.GetValueOrDefault(column, string.Empty);
}

internal static class ExcelImportReader
{
    public static XLWorkbook Open(byte[] file)
    {
        try
        {
            return new XLWorkbook(new MemoryStream(file));
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            throw new ValidationException("file", "The file is not a valid .xlsx workbook.");
        }
    }

    public static IReadOnlyList<ExcelImportRow> ReadRows(XLWorkbook workbook, IReadOnlyCollection<string> requiredColumns)
    {
        var sheet = workbook.Worksheets.First();
        var headerCells = sheet.Row(1).CellsUsed().ToList();
        var columnNumbers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerCells)
        {
            columnNumbers[Text(cell)] = cell.Address.ColumnNumber;
        }

        var missing = requiredColumns.Where(c => !columnNumbers.ContainsKey(c)).ToList();
        if (missing.Count > 0)
        {
            throw new ValidationException("file", $"Missing column(s): {string.Join(", ", missing)}. Expected: {string.Join(", ", requiredColumns)}.");
        }

        var rows = new List<ExcelImportRow>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var values = requiredColumns.ToDictionary(
                column => column,
                column => Text(sheet.Cell(rowNumber, columnNumbers[column])),
                StringComparer.OrdinalIgnoreCase);
            if (values.Values.Any(v => v.Length > 0))
            {
                rows.Add(new ExcelImportRow(rowNumber, values));
            }
        }

        return rows;
    }

    public static IReadOnlyDictionary<string, string> ReadKeyValues(XLWorkbook workbook, string sheetName)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!workbook.Worksheets.TryGetWorksheet(sheetName, out var sheet))
        {
            return values;
        }

        foreach (var row in sheet.RowsUsed())
        {
            var key = Text(row.Cell(1));
            if (key.Length > 0)
            {
                values[key] = Text(row.Cell(2));
            }
        }

        return values;
    }

    private static string Text(IXLCell cell)
    {
        return cell.Value.ToString(CultureInfo.InvariantCulture).Trim();
    }
}
