using System.Globalization;
using ClosedXML.Excel;
using Tenengroup.FactoryCatalog.Business.Abstractions.Processes;

namespace Tenengroup.FactoryCatalog.Business.Processes.Import;

internal record ProcessImportRawRow(
    int Row,
    string ProcessId,
    string Name,
    string Type,
    string MeasuredIn,
    string Status,
    string Description);

internal record ProcessFactoryImportRawRow(
    int Row,
    string ProcessId,
    string FactoryId,
    string RunsHere,
    string MachineType,
    string LaborCost,
    string Currency,
    string MaxPerDay);

internal record ProcessImportFile(
    List<ProcessImportRawRow> Processes,
    List<ProcessFactoryImportRawRow> Factories,
    List<ProcessImportRowErrorDto> Errors);

internal static class ProcessImportFileReader
{
    public const string ProcessesSheet = "Processes";
    public const string FactoriesSheet = "Factories";

    public const string ProcessIdHeader = "Process ID";
    public const string NameHeader = "Name";
    public const string TypeHeader = "Type";
    public const string MeasuredInHeader = "Measured in";
    public const string StatusHeader = "Status";
    public const string DescriptionHeader = "Description";
    public const string FactoryIdHeader = "Factory ID";
    public const string RunsHereHeader = "Runs here";
    public const string MachineTypeHeader = "Machine type";
    public const string LaborCostHeader = "Labor cost";
    public const string CurrencyHeader = "Currency";
    public const string MaxPerDayHeader = "Max per day";

    private const int HeaderRow = 1;

    private static readonly string[] RequiredProcessHeaders = [ProcessIdHeader, NameHeader, TypeHeader, MeasuredInHeader];
    private static readonly string[] RequiredFactoryHeaders = [ProcessIdHeader, FactoryIdHeader, RunsHereHeader];

    public static string FactoriesField(string header)
    {
        return $"{FactoriesSheet}: {header}";
    }

    public static ProcessImportFile Read(byte[] content)
    {
        var file = new ProcessImportFile([], [], []);
        XLWorkbook workbook;

        try
        {
            workbook = new XLWorkbook(new MemoryStream(content));
        }
        catch (Exception)
        {
            file.Errors.Add(new ProcessImportRowErrorDto(0, null, "The file is not a readable .xlsx workbook."));
            return file;
        }

        using (workbook)
        {
            var factoriesSheet = FindSheet(workbook, FactoriesSheet);
            var processesSheet = FindSheet(workbook, ProcessesSheet)
                                 ?? workbook.Worksheets.FirstOrDefault(s => s != factoriesSheet);

            if (processesSheet == null)
            {
                file.Errors.Add(new ProcessImportRowErrorDto(0, null, $"The workbook has no '{ProcessesSheet}' sheet."));
                return file;
            }

            var processHeaders = ReadHeaders(processesSheet);
            AddMissingHeaderErrors(file, processHeaders, RequiredProcessHeaders, h => h);

            var factoryHeaders = factoriesSheet == null ? null : ReadHeaders(factoriesSheet);
            if (factoryHeaders != null)
            {
                AddMissingHeaderErrors(file, factoryHeaders, RequiredFactoryHeaders, FactoriesField);
            }

            if (file.Errors.Count > 0)
            {
                return file;
            }

            foreach (var row in DataRows(processesSheet, processHeaders))
            {
                file.Processes.Add(new ProcessImportRawRow(
                    row.RowNumber(),
                    Text(row, processHeaders, ProcessIdHeader),
                    Text(row, processHeaders, NameHeader),
                    Text(row, processHeaders, TypeHeader),
                    Text(row, processHeaders, MeasuredInHeader),
                    Text(row, processHeaders, StatusHeader),
                    Text(row, processHeaders, DescriptionHeader)));
            }

            if (factoryHeaders != null)
            {
                foreach (var row in DataRows(factoriesSheet, factoryHeaders))
                {
                    file.Factories.Add(new ProcessFactoryImportRawRow(
                        row.RowNumber(),
                        Text(row, factoryHeaders, ProcessIdHeader),
                        Text(row, factoryHeaders, FactoryIdHeader),
                        Text(row, factoryHeaders, RunsHereHeader),
                        Text(row, factoryHeaders, MachineTypeHeader),
                        Text(row, factoryHeaders, LaborCostHeader),
                        Text(row, factoryHeaders, CurrencyHeader),
                        Text(row, factoryHeaders, MaxPerDayHeader)));
                }
            }
        }

        return file;
    }

    private static IXLWorksheet FindSheet(XLWorkbook workbook, string name)
    {
        return workbook.Worksheets.FirstOrDefault(s => string.Equals(s.Name.Trim(), name, StringComparison.OrdinalIgnoreCase));
    }

    private static Dictionary<string, int> ReadHeaders(IXLWorksheet sheet)
    {
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var cell in sheet.Row(HeaderRow).CellsUsed())
        {
            headers.TryAdd(CellText(cell), cell.Address.ColumnNumber);
        }

        return headers;
    }

    private static void AddMissingHeaderErrors(
        ProcessImportFile file,
        Dictionary<string, int> headers,
        string[] requiredHeaders,
        Func<string, string> toField)
    {
        file.Errors.AddRange(requiredHeaders
            .Where(h => !headers.ContainsKey(h))
            .Select(h => new ProcessImportRowErrorDto(HeaderRow, toField(h), $"Column '{h}' is missing.")));
    }

    private static IEnumerable<IXLRow> DataRows(IXLWorksheet sheet, Dictionary<string, int> headers)
    {
        return sheet.RowsUsed()
            .Where(r => r.RowNumber() > HeaderRow)
            .Where(r => headers.Values.Any(column => CellText(r.Cell(column)).Length > 0));
    }

    private static string Text(IXLRow row, Dictionary<string, int> headers, string header)
    {
        return headers.TryGetValue(header, out var column) ? CellText(row.Cell(column)) : string.Empty;
    }

    private static string CellText(IXLCell cell)
    {
        var value = cell.Value;

        return value.IsBlank ? string.Empty : value.ToString(CultureInfo.InvariantCulture).Trim();
    }
}
