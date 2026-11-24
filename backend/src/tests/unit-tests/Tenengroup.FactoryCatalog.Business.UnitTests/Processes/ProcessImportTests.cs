using ClosedXML.Excel;
using Moq;
using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Abstractions.Processes;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.Business.Processes.Import;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Processes;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Exceptions;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Processes;

[TestFixture]
public class ProcessImportTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 10, 0, 0, DateTimeKind.Utc);

    private static readonly object[] ProcessHeader = ["Process ID", "Name", "Type", "Measured in", "Status", "Description"];
    private static readonly object[] FactoryHeader = ["Process ID", "Factory ID", "Runs here", "Machine type", "Labor cost", "Currency", "Max per day"];

    private Mock<IFactoryCatalogDbContext> db;
    private Mock<IHistoryWriter> historyWriter;
    private List<Process> processes;
    private List<ProcessFactory> factoryRows;

    [SetUp]
    public void SetUp()
    {
        db = new Mock<IFactoryCatalogDbContext>();
        historyWriter = new Mock<IHistoryWriter>();
        processes = db.SetupSet(x => x.Processes,
            new Process { ProcessId = "PACKING", Name = "Packing", Type = ProcessType.Work, MeasuredIn = ProcessUnit.Units, Status = ProcessStatus.Active });
        factoryRows = db.SetupSet(x => x.ProcessFactories);
        db.SetupSet(x => x.Factories,
            new Factory { Id = "110", Name = "Factory 110", IsActive = true },
            new Factory { Id = "125", Name = "Factory 125", IsActive = true });
    }

    private static byte[] Workbook(object[][] processRows, object[][] factoryRows = null)
    {
        using var workbook = new XLWorkbook();
        Fill(workbook.Worksheets.Add("Processes"), ProcessHeader, processRows);

        if (factoryRows != null)
        {
            Fill(workbook.Worksheets.Add("Factories"), FactoryHeader, factoryRows);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    private static void Fill(IXLWorksheet sheet, object[] header, object[][] rows)
    {
        var all = new[] { header }.Concat(rows).ToList();

        for (var r = 0; r < all.Count; r++)
        {
            for (var c = 0; c < all[r].Length; c++)
            {
                sheet.Cell(r + 1, c + 1).Value = XLCellValue.FromObject(all[r][c]);
            }
        }
    }

    private static byte[] ValidFile()
    {
        return Workbook(
            [
                ["engraving", "Engraving", "Machine", "minutes", "Active", "Text engraving"],
                ["POLISH", "Polishing", "work", "cm²", "", ""],
                ["PACKING", "Packing", "Work", "units", "", "Packing of a finished order item"]
            ],
            [
                ["ENGRAVING", "110", "Yes", "LASER", 0.55, "usd", 1000],
                ["ENGRAVING", "125", "no", "", "", "", ""],
                ["PACKING", "110", true, "", 1.2, "USD", 500]
            ]);
    }

    private Task<ProcessImportValidationDto> Validate(byte[] content)
    {
        return new ValidateProcessImportQueryHandler(new ProcessImportPlanner(db.Object))
            .Handle(new ValidateProcessImportQuery(content), CancellationToken.None);
    }

    private Task<ProcessImportResultDto> Import(byte[] content)
    {
        return new ImportProcessesCommandHandler(new ProcessImportPlanner(db.Object), db.Object, historyWriter.Object, TestDb.FixedTime(Now))
            .Handle(new ImportProcessesCommand(content), CancellationToken.None);
    }

    [Test]
    public async Task Validate_ValidFile_NoErrorsAndNothingSaved()
    {
        var result = await Validate(ValidFile());

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.TotalRows, Is.EqualTo(6));
        Assert.That(result.ValidRows, Is.EqualTo(6));
        Assert.That(processes, Has.Count.EqualTo(1));
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Validate_ReportsEveryRowError()
    {
        processes.Add(new Process { ProcessId = "LEGACY", Name = "Legacy", Type = ProcessType.Work });

        var result = await Validate(Workbook(
            [
                ["ENGRAVING", "Engraving", "Robot", "minutes", "", ""],
                ["BAD ID", "Bad", "Work", "minutes", "", ""],
                ["POLISH", "", "Work", "furlongs", "Paused", ""],
                ["STONE", "legacy", "Work", "units", "", ""],
                ["CAST", "Casting", "Work", "units", "", ""],
                ["CAST", "Casting again", "Work", "units", "", ""],
                ["PACKING", "Packing 2", "Machine", "units", "", ""]
            ],
            [
                ["UNKNOWN", "110", "yes", "", "", "", ""],
                ["PACKING", "999", "yes", "", "", "", ""],
                ["PACKING", "110", "maybe", "LASER", "abc", "", -1],
                ["CAST", "110", "yes", "", 2, "", ""],
                ["CAST", "110", "yes", "", "", "", ""]
            ]));

        var errors = result.Errors.Select(e => $"{e.Row}|{e.Field}").ToList();
        Assert.That(errors, Is.EquivalentTo(new[]
        {
            "2|Type",
            "3|Process ID",
            "4|Measured in", "4|Status", "4|Name",
            "5|Name",
            "7|Process ID",
            "8|Type",
            "2|Factories: Process ID",
            "3|Factories: Factory ID",
            "4|Factories: Runs here", "4|Factories: Labor cost", "4|Factories: Machine type", "4|Factories: Max per day",
            "5|Factories: Currency",
            "6|Factories: Factory ID"
        }));
        Assert.That(result.TotalRows, Is.EqualTo(12));
        Assert.That(result.ValidRows, Is.EqualTo(1));
    }

    [Test]
    public async Task Validate_MissingColumnOrUnreadableFile()
    {
        using var workbook = new XLWorkbook();
        Fill(workbook.Worksheets.Add("Processes"), ["Process ID", "Name"], []);
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var missing = await Validate(stream.ToArray());
        var unreadable = await Validate([1, 2, 3]);
        var empty = await Validate(Workbook([]));

        Assert.That(missing.Errors.Select(e => e.Field), Is.EqualTo(new[] { "Type", "Measured in" }));
        Assert.That(unreadable.Errors, Has.Count.EqualTo(1));
        Assert.That(empty.Errors, Has.Count.EqualTo(1));
        Assert.That(empty.ValidRows, Is.EqualTo(0));
    }

    [Test]
    public void Import_FileWithErrors_SavesNothing()
    {
        var content = Workbook(
        [
            ["ENGRAVING", "Engraving", "Machine", "minutes", "", ""],
            ["POLISH", "Polishing", "Robot", "minutes", "", ""]
        ]);

        var exception = Assert.ThrowsAsync<ValidationException>(() => Import(content));

        Assert.That(exception.ValidationMessages["file"], Has.Count.EqualTo(1));
        Assert.That(processes, Has.Count.EqualTo(1));
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Import_CreatesUpdatesAndLogsWithImportSource()
    {
        var result = await Import(ValidFile());

        Assert.That(result.ProcessesCreated, Is.EqualTo(2));
        Assert.That(result.ProcessesUpdated, Is.EqualTo(1));
        Assert.That(result.FactoryRowsChanged, Is.EqualTo(2));
        Assert.That(processes.Select(p => p.ProcessId), Is.EquivalentTo(new[] { "PACKING", "ENGRAVING", "POLISH" }));
        var polish = processes.Single(p => p.ProcessId == "POLISH");
        Assert.That(polish.MeasuredIn, Is.EqualTo(ProcessUnit.Cm2));
        Assert.That(polish.Status, Is.EqualTo(ProcessStatus.Active));
        Assert.That(processes[0].Description, Is.EqualTo("Packing of a finished order item"));
        var laser = factoryRows.Single(r => r.ProcessId == "ENGRAVING");
        Assert.That(laser.FactoryId, Is.EqualTo("110"));
        Assert.That(laser.MachineType, Is.EqualTo("LASER"));
        Assert.That(laser.LaborCostAmount, Is.EqualTo(0.55m));
        Assert.That(laser.LaborCostCurrency, Is.EqualTo("USD"));
        Assert.That(laser.MaxPerDay, Is.EqualTo(1000));
        Assert.That(laser.CapacityPct, Is.EqualTo(100));
        historyWriter.Verify(h => h.Record(HistoryEntityTypes.Process, "ENGRAVING", "process", null, "Created", HistorySource.Import, null, null), Times.Once);
        historyWriter.Verify(h => h.Record(HistoryEntityTypes.Process, "PACKING", "runsHere", false, true, HistorySource.Import, "110", null), Times.Once);
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Import_SameFileAgain_ChangesNothing()
    {
        await Import(ValidFile());
        historyWriter.Invocations.Clear();
        db.Invocations.Clear();
        factoryRows.ForEach(r => r.CapacityPct = 40);

        var result = await Import(ValidFile());

        Assert.That(result.ProcessesCreated, Is.EqualTo(0));
        Assert.That(result.ProcessesUpdated, Is.EqualTo(0));
        Assert.That(result.FactoryRowsChanged, Is.EqualTo(0));
        Assert.That(processes, Has.Count.EqualTo(3));
        Assert.That(factoryRows, Has.Count.EqualTo(2));
        Assert.That(factoryRows.Select(r => r.CapacityPct), Is.All.EqualTo(40));
        historyWriter.VerifyNoOtherCalls();
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
