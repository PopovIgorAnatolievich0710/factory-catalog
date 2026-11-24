using ClosedXML.Excel;
using NUnit.Framework;
using Tenengroup.Infrastructure.Common.Excel;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Excel;

[TestFixture]
public class ExcelExporterTests
{
    private record Row(string ErpId, string Name, decimal Stock);

    private static readonly ExcelColumn<Row>[] Columns =
    [
        new("erpId", "ERP ID", r => r.ErpId),
        new("name", "Name", r => r.Name),
        new("stock", "Total stock", r => r.Stock)
    ];

    private static readonly Row[] Rows = [new("R.1", "Board", 12.5m), new("R.2", "Chain", 0m)];

    private static readonly DateTime Now = new(2026, 11, 24, 9, 5, 7, DateTimeKind.Utc);

    [Test]
    public void Export_FileNameIsPageAndTimestamp()
    {
        var file = ExcelExporter.Export("components", Rows, Columns, [], Now);

        Assert.That(file.FileName, Is.EqualTo("components_20261124_090507.xlsx"));
    }

    [Test]
    public void Export_SelectedColumnsInRequestedOrder()
    {
        var file = ExcelExporter.Export("components", Rows, Columns, ["stock", "ERPID", "unknown"], Now);

        using var workbook = new XLWorkbook(new MemoryStream(file.Content));
        var sheet = workbook.Worksheet(1);

        Assert.That(sheet.Cell(1, 1).GetString(), Is.EqualTo("Total stock"));
        Assert.That(sheet.Cell(1, 2).GetString(), Is.EqualTo("ERP ID"));
        Assert.That(sheet.Cell(1, 3).IsEmpty(), Is.True);
        Assert.That(sheet.Cell(2, 1).GetDouble(), Is.EqualTo(12.5));
        Assert.That(sheet.Cell(3, 2).GetString(), Is.EqualTo("R.2"));
        Assert.That(sheet.LastRowUsed().RowNumber(), Is.EqualTo(3));
    }

    [Test]
    public void Export_NoSelection_AllColumns()
    {
        var file = ExcelExporter.Export("components", Rows, Columns, [], Now);

        using var workbook = new XLWorkbook(new MemoryStream(file.Content));

        Assert.That(workbook.Worksheet(1).LastColumnUsed().ColumnNumber(), Is.EqualTo(3));
    }
}
