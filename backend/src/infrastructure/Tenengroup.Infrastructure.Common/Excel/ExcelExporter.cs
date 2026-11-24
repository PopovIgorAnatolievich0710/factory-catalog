using ClosedXML.Excel;

namespace Tenengroup.Infrastructure.Common.Excel;

public static class ExcelExporter
{
    public static ExcelFile Export<T>(
        string page,
        IEnumerable<T> rows,
        IReadOnlyCollection<ExcelColumn<T>> availableColumns,
        IReadOnlyCollection<string> selectedColumns,
        DateTime utcNow)
    {
        var columns = SelectColumns(availableColumns, selectedColumns);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(page.Length > 31 ? page[..31] : page);

        for (var c = 0; c < columns.Count; c++)
        {
            sheet.Cell(1, c + 1).Value = columns[c].Header;
        }

        sheet.Row(1).Style.Font.Bold = true;

        var rowNumber = 2;
        foreach (var row in rows)
        {
            for (var c = 0; c < columns.Count; c++)
            {
                sheet.Cell(rowNumber, c + 1).Value = XLCellValue.FromObject(columns[c].Value(row));
            }

            rowNumber++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelFile(stream.ToArray(), $"{page}_{utcNow:yyyyMMdd_HHmmss}.xlsx");
    }

    private static List<ExcelColumn<T>> SelectColumns<T>(
        IReadOnlyCollection<ExcelColumn<T>> availableColumns,
        IReadOnlyCollection<string> selectedColumns)
    {
        if (selectedColumns.Count == 0)
        {
            return availableColumns.ToList();
        }

        return selectedColumns
            .Select(key => availableColumns.FirstOrDefault(c => string.Equals(c.Key, key, StringComparison.OrdinalIgnoreCase)))
            .OfType<ExcelColumn<T>>()
            .ToList();
    }
}
