using System.Text;

namespace Tenengroup.FactoryCatalog.Business.Components.Sync;

public class CsvTable
{
    private readonly Dictionary<string, int> columnIndexes;

    private CsvTable(string fileName, Dictionary<string, int> columnIndexes, List<CsvRow> rows)
    {
        FileName = fileName;
        this.columnIndexes = columnIndexes;
        Rows = rows;
    }

    public string FileName { get; }

    public IReadOnlyList<CsvRow> Rows { get; }

    public static CsvTable Parse(string fileName, string content, IEnumerable<string> requiredColumns)
    {
        var records = ReadRecords(content.TrimStart('﻿'));
        if (records.Count == 0)
        {
            throw new InvalidDataException($"{fileName} is empty");
        }

        var columnIndexes = records[0]
            .Select((name, index) => new { Name = name.Trim(), Index = index })
            .Where(c => c.Name.Length > 0)
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Index, StringComparer.OrdinalIgnoreCase);

        var missing = requiredColumns.Where(c => !columnIndexes.ContainsKey(c)).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidDataException($"{fileName} has no column(s): {string.Join(", ", missing)}");
        }

        var rows = records
            .Select((values, index) => new CsvRow(index + 1, values))
            .Skip(1)
            .Where(r => r.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
            .ToList();

        return new CsvTable(fileName, columnIndexes, rows);
    }

    public string Get(CsvRow row, string column)
    {
        if (!columnIndexes.TryGetValue(column, out var index) || index >= row.Values.Length)
        {
            return null;
        }

        var value = row.Values[index].Trim();

        return value.Length == 0 ? null : value;
    }

    private static List<string[]> ReadRecords(string content)
    {
        var records = new List<string[]>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var hasContent = false;

        for (var i = 0; i < content.Length; i++)
        {
            var c = content[i];

            if (inQuotes)
            {
                if (c == '"' && i + 1 < content.Length && content[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    field.Append(c);
                }

                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    hasContent = true;
                    break;
                case ',':
                    fields.Add(field.ToString());
                    field.Clear();
                    hasContent = true;
                    break;
                case '\r':
                    break;
                case '\n':
                    fields.Add(field.ToString());
                    field.Clear();
                    records.Add(fields.ToArray());
                    fields.Clear();
                    hasContent = false;
                    break;
                default:
                    field.Append(c);
                    hasContent = true;
                    break;
            }
        }

        if (hasContent || field.Length > 0)
        {
            fields.Add(field.ToString());
            records.Add(fields.ToArray());
        }

        return records;
    }
}
