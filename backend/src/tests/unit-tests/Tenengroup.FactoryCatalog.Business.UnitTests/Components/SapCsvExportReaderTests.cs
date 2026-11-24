using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components.Sync;
using Tenengroup.FactoryCatalog.Business.Components.Sync;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.Infrastructure.Common.Aws.S3;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Components;

[TestFixture]
public class SapCsvExportReaderTests
{
    private const string ComponentsHeader =
        "erp_id,name,status,family,material,measured_by,measure_value,measure_unit,uom_type,uom_quantity,uom_unit,country_of_origin,hs_code,unit_cost_amount,unit_cost_currency";

    private const string StockHeader = "erp_id,factory_id,location,quantity";

    [Test]
    public void Parse_ReadsComponentAndStockRows()
    {
        var components = ComponentsHeader + "\r\n" +
                         "R.WG.10K,\"White gold board, 10K\",active,Board,10K white gold,thickness,0.8,mm,weight,1,kg,us,7113.11.00,245.5,usd\r\n" +
                         "C-2,Clasp,Inactive,,,,,,,,,,,,\r\n";
        var stock = StockHeader + "\nR.WG.10K,110,warehouse,12.4\nR.WG.10K,110,production_floor,1.1\n";

        var export = SapCsvExportReader.Parse(components, stock);

        Assert.That(export.Errors, Is.Empty);
        Assert.That(export.Components, Has.Count.EqualTo(2));
        var first = export.Components.First();
        Assert.That(first.ErpId, Is.EqualTo("R.WG.10K"));
        Assert.That(first.Name, Is.EqualTo("White gold board, 10K"));
        Assert.That(first.Status, Is.EqualTo(ComponentStatus.Active));
        Assert.That(first.MeasureValue, Is.EqualTo(0.8m));
        Assert.That(first.CountryOfOrigin, Is.EqualTo("US"));
        Assert.That(first.UnitCostAmount, Is.EqualTo(245.5m));
        Assert.That(first.UnitCostCurrency, Is.EqualTo("USD"));
        Assert.That(export.Components.Last().Status, Is.EqualTo(ComponentStatus.Inactive));
        Assert.That(export.Components.Last().Family, Is.Null);
        Assert.That(export.Stock, Is.EqualTo(new[]
        {
            new SapStockRow("R.WG.10K", "110", SapStockLocation.Warehouse, 12.4m),
            new SapStockRow("R.WG.10K", "110", SapStockLocation.ProductionFloor, 1.1m)
        }));
    }

    [Test]
    public void Parse_InvalidRows_AreReportedAndSkipped()
    {
        var components = "erp_id,name,status\nA,Alpha,active\n,NoId,active\nB,Beta,unknown\nA,Duplicate,active\n\n";
        var stock = StockHeader + "\nA,110,warehouse,abc\nA,110,shelf,1\nA,110,warehouse,2\n";

        var export = SapCsvExportReader.Parse(components, stock);

        Assert.That(export.Components.Select(c => c.ErpId), Is.EqualTo(new[] { "A" }));
        Assert.That(export.Stock, Has.Count.EqualTo(1));
        Assert.That(export.Errors.Select(e => (e.File, e.Line)), Is.EqualTo(new[]
        {
            ("components.csv", 3), ("components.csv", 4), ("components.csv", 5), ("stock.csv", 2), ("stock.csv", 3)
        }));
    }

    [Test]
    public void Parse_MissingRequiredColumn_Throws()
    {
        var exception = Assert.Throws<InvalidDataException>(() =>
            SapCsvExportReader.Parse("erp_id,name\nA,Alpha\n", StockHeader + "\n"));

        Assert.That(exception.Message, Does.Contain("status"));
    }

    [Test]
    public async Task Read_TakesBothFilesFromTheDatedFolder()
    {
        var s3 = new Mock<IS3FileReader>();
        s3.Setup(x => x.ReadAllText("bucket", "sap/export/2026-11-24/components.csv", It.IsAny<CancellationToken>()))
            .ReturnsAsync("erp_id,name,status\nA,Alpha,active\n");
        s3.Setup(x => x.ReadAllText("bucket", "sap/export/2026-11-24/stock.csv", It.IsAny<CancellationToken>()))
            .ReturnsAsync(StockHeader + "\n");
        var reader = new SapCsvExportReader(s3.Object, Options.Create(new SapExportOptions { Bucket = "bucket", Prefix = "/sap/export/" }));

        var export = await reader.Read(new DateOnly(2026, 11, 24));

        Assert.That(export.Components, Has.Count.EqualTo(1));
    }
}
