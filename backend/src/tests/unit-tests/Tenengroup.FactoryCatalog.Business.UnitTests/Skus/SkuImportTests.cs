using ClosedXML.Excel;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.FactoryShare;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.ProcessMappings;
using Tenengroup.FactoryCatalog.Business.Skus.FactoryShare;
using Tenengroup.FactoryCatalog.Business.Skus.Import;
using Tenengroup.FactoryCatalog.Business.Skus.ProcessMappings;
using Tenengroup.FactoryCatalog.Business.Skus.Validation;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Processes;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Exceptions;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Skus;

[TestFixture]
public class SkuImportTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Uuid = Guid.Parse("8f3c1a2e-5b7d-4c1a-9e21-7d4e5f6a8b90");
    private static readonly Guid DropshipperUuid = Guid.NewGuid();

    private Mock<IFactoryCatalogDbContext> db;
    private Mock<IHistoryWriter> history;
    private Mock<ICurrentUser> currentUser;
    private Sku sku;
    private Sku second;

    [SetUp]
    public void SetUp()
    {
        db = new Mock<IFactoryCatalogDbContext>();
        history = new Mock<IHistoryWriter>();
        currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.CanEditFactory("110", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        sku = new Sku
        {
            SkuUuid = Uuid,
            SiteSku = "TGI-N-4609",
            ProductType = SkuProductType.Manufactured,
            Processes = [new SkuProcess { SkuUuid = Uuid, ProcessId = "PACKING", Quantity = 1 }]
        };
        second = new Sku { SkuUuid = Guid.NewGuid(), SiteSku = "TGI-R-1", ProductType = SkuProductType.Manufactured };
        db.SetupSet(x => x.Skus, sku, second,
            new Sku { SkuUuid = DropshipperUuid, SiteSku = "DROP-1", ProductType = SkuProductType.Dropshipper },
            new Sku { SkuUuid = Guid.NewGuid(), SiteSku = "DUP", ProductType = SkuProductType.Manufactured },
            new Sku { SkuUuid = Guid.NewGuid(), SiteSku = "dup", ProductType = SkuProductType.Manufactured });
        db.SetupSet(x => x.SkuAliases, new SkuAlias { SkuUuid = second.SkuUuid, Alias = "TGN-R-1", Type = SkuAliasType.Factory });
        db.SetupSet(x => x.Processes, new Process { ProcessId = "CUTTING" }, new Process { ProcessId = "PACKING" });
        db.SetupSet(x => x.Components);
        db.SetupSet(x => x.GlobalFactoryShares);
        db.SetupSet(x => x.Factories, new Factory { Id = "110", IsActive = true }, new Factory { Id = "125", IsActive = true });
    }

    private static byte[] Workbook(string[] header, object[][] rows, bool approved = true)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Data");
        for (var c = 0; c < header.Length; c++)
        {
            sheet.Cell(1, c + 1).Value = header[c];
        }

        for (var r = 0; r < rows.Length; r++)
        {
            for (var c = 0; c < rows[r].Length; c++)
            {
                sheet.Cell(r + 2, c + 1).Value = XLCellValue.FromObject(rows[r][c]);
            }
        }

        if (approved)
        {
            var approval = workbook.Worksheets.Add(ProcessMappingImportParser.ApprovalSheet);
            approval.Cell(1, 1).Value = ProcessMappingImportParser.ApprovedByKey;
            approval.Cell(1, 2).Value = "ops.lead@tenengroup.com";
            approval.Cell(2, 1).Value = ProcessMappingImportParser.ApprovedOnKey;
            approval.Cell(2, 2).Value = "2026-11-20";
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] Mappings(object[][] rows, bool approved = true)
    {
        return Workbook(
            [ProcessMappingImportParser.SkuColumn, ProcessMappingImportParser.ProcessColumn, ProcessMappingImportParser.QuantityColumn],
            rows, approved);
    }

    private static byte[] Producible(object[][] rows)
    {
        return Workbook(
            [ProducibleImportParser.SkuColumn, ProducibleImportParser.FactoryColumn, ProducibleImportParser.ProducibleColumn], rows);
    }

    private ProcessMappingImportParser MappingParser() => new(db.Object, new SkuIdentifierResolver(db.Object));

    private SkuFactoryShareService ShareService() => new(db.Object, history.Object, currentUser.Object, TestDb.FixedTime(Now));

    private ProducibleImportParser ProducibleParser() => new(new SkuIdentifierResolver(db.Object), ShareService(), currentUser.Object);

    [Test]
    public async Task ValidateProcessMappings_ReportsEveryRowErrorWithoutSaving()
    {
        var file = Mappings(
        [
            [Uuid.ToString(), "CUTTING", 4],
            ["tgn-r-1", "cutting", 2.5],
            ["UNKNOWN", "CUTTING", 1],
            ["TGI-N-4609", "PLATING", 1],
            ["TGI-N-4609", "PACKING", 0],
            ["DROP-1", "PACKING", 1],
            ["DUP", "PACKING", 1],
            [Uuid.ToString(), "CUTTING", 5],
            ["", "PACKING", "abc"]
        ]);

        var result = await new ValidateProcessMappingImportQueryHandler(MappingParser())
            .Handle(new ValidateProcessMappingImportQuery(file), CancellationToken.None);

        Assert.That(result.TotalRows, Is.EqualTo(9));
        Assert.That(result.ValidRows, Is.EqualTo(2));
        Assert.That(result.Errors.Select(e => (e.Row, e.Field)), Is.EqualTo(new[]
        {
            (4, ProcessMappingImportParser.SkuColumn),
            (5, ProcessMappingImportParser.ProcessColumn),
            (6, ProcessMappingImportParser.QuantityColumn),
            (7, ProcessMappingImportParser.SkuColumn),
            (8, ProcessMappingImportParser.SkuColumn),
            (9, ProcessMappingImportParser.ProcessColumn),
            (10, ProcessMappingImportParser.SkuColumn),
            (10, ProcessMappingImportParser.QuantityColumn)
        }));
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ValidateProcessMappings_FileWithoutApproval_Rejected()
    {
        var result = await new ValidateProcessMappingImportQueryHandler(MappingParser())
            .Handle(new ValidateProcessMappingImportQuery(Mappings([[Uuid.ToString(), "CUTTING", 4]], approved: false)), CancellationToken.None);

        var error = result.Errors.Single();
        Assert.That(error.Row, Is.EqualTo(ProcessMappingImportParser.FileRow));
        Assert.That(error.Field, Is.EqualTo(ProcessMappingImportParser.ApprovalField));
        Assert.That(result.ValidRows, Is.EqualTo(1));
    }

    [Test]
    public void ValidateProcessMappings_MissingColumnOrNotExcel_Rejected()
    {
        var handler = new ValidateProcessMappingImportQueryHandler(MappingParser());

        Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new ValidateProcessMappingImportQuery(Workbook(["SKU", "Quantity"], [])), CancellationToken.None));
        Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new ValidateProcessMappingImportQuery([1, 2, 3]), CancellationToken.None));
    }

    private ImportProcessMappingsCommandHandler ImportMappingsHandler()
    {
        return new ImportProcessMappingsCommandHandler(
            db.Object, MappingParser(), new SkuMappingValidator(db.Object, history.Object), history.Object,
            TestDb.FixedTime(Now), NullLogger<ImportProcessMappingsCommandHandler>.Instance);
    }

    [Test]
    public async Task ImportProcessMappings_ReplacesTheMappingOfEachSkuInTheFile()
    {
        var file = Mappings([[Uuid.ToString(), "CUTTING", 4], ["TGN-R-1", "PACKING", 1]]);

        var result = await ImportMappingsHandler().Handle(new ImportProcessMappingsCommand(file), CancellationToken.None);

        Assert.That(result.ValidRows, Is.EqualTo(2));
        Assert.That(sku.Processes.Select(p => (p.ProcessId, p.Quantity)), Is.EqualTo(new[] { ("CUTTING", 4m) }));
        Assert.That(second.Processes.Select(p => (p.ProcessId, p.Quantity)), Is.EqualTo(new[] { ("PACKING", 1m) }));
        history.Verify(h => h.Record(
            HistoryEntityTypes.Sku, Uuid.ToString(), "process:CUTTING", null, 4m, HistorySource.Import, null, null));
        history.Verify(h => h.Record(
            HistoryEntityTypes.Sku, Uuid.ToString(), "process:PACKING", 1m, null, HistorySource.Import, null, null));
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void ImportProcessMappings_FileWithErrors_NothingSaved()
    {
        var file = Mappings([[Uuid.ToString(), "CUTTING", 4], ["UNKNOWN", "CUTTING", 1]]);

        var exception = Assert.ThrowsAsync<ValidationException>(() =>
            ImportMappingsHandler().Handle(new ImportProcessMappingsCommand(file), CancellationToken.None));

        Assert.That(exception.ValidationMessages.Keys, Is.EqualTo(new[] { "row 3" }));
        Assert.That(sku.Processes.Single().ProcessId, Is.EqualTo("PACKING"));
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ValidateProducible_OnlyOwnFactoryRowsAreAccepted()
    {
        var file = Producible(
        [
            ["TGI-N-4609", "110", "Yes"],
            ["TGI-N-4609", "125", "Yes"],
            ["TGI-R-1", "777", "No"],
            ["TGI-R-1", "110", "maybe"],
            ["DROP-1", "110", "Yes"],
            ["TGI-N-4609", "110", "No"]
        ]);

        var result = await new ValidateProducibleImportQueryHandler(ProducibleParser())
            .Handle(new ValidateProducibleImportQuery(file), CancellationToken.None);

        Assert.That(result.TotalRows, Is.EqualTo(6));
        Assert.That(result.ValidRows, Is.EqualTo(1));
        Assert.That(result.Errors.Select(e => (e.Row, e.Field)), Is.EqualTo(new[]
        {
            (3, ProducibleImportParser.FactoryColumn),
            (4, ProducibleImportParser.FactoryColumn),
            (5, ProducibleImportParser.ProducibleColumn),
            (6, ProducibleImportParser.SkuColumn),
            (7, ProducibleImportParser.SkuColumn)
        }));
    }

    [Test]
    public async Task ImportProducible_SetsFlagsWithImportHistory()
    {
        second.Factories.Add(new SkuFactory { SkuUuid = second.SkuUuid, FactoryId = "110", Producible = true });
        var file = Producible([["TGI-N-4609", "110", "Yes"], ["TGN-R-1", "110", false]]);

        await new ImportProducibleCommandHandler(db.Object, ProducibleParser(), ShareService())
            .Handle(new ImportProducibleCommand(file), CancellationToken.None);

        Assert.That(sku.Factories.Single().Producible, Is.True);
        Assert.That(second.Factories.Single().Producible, Is.False);
        history.Verify(h => h.Record(
            HistoryEntityTypes.Sku, Uuid.ToString(), SkuFactoryShareService.ProducibleField, false, true, HistorySource.Import, "110", null));
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void ImportProducible_ForeignFactoryRow_NothingSaved()
    {
        var file = Producible([["TGI-N-4609", "110", "Yes"], ["TGI-N-4609", "125", "Yes"]]);

        Assert.ThrowsAsync<ValidationException>(() => new ImportProducibleCommandHandler(db.Object, ProducibleParser(), ShareService())
            .Handle(new ImportProducibleCommand(file), CancellationToken.None));

        Assert.That(sku.Factories, Is.Empty);
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
