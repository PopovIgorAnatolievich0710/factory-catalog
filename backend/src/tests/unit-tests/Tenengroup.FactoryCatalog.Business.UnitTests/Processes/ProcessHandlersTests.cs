using Moq;
using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Abstractions.Processes;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.Business.Processes;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Processes;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Paging;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Processes;

[TestFixture]
public class ProcessHandlersTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid RingSku = Guid.NewGuid();

    private Mock<IFactoryCatalogDbContext> db;
    private Mock<ICurrentUser> currentUser;
    private Mock<IHistoryWriter> historyWriter;
    private List<Process> processes;
    private List<ProcessFactory> factoryRows;

    [SetUp]
    public void SetUp()
    {
        db = new Mock<IFactoryCatalogDbContext>();
        currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.HasPermission(Permission.ProcessesViewCost, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        currentUser.Setup(u => u.EnsureCanEditFactory("125", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("not your factory"));
        historyWriter = new Mock<IHistoryWriter>();

        processes = db.SetupSet(x => x.Processes,
            new Process { ProcessId = "ENGRAVING", Name = "Engraving", Type = ProcessType.Machine, MeasuredIn = ProcessUnit.Minutes, Status = ProcessStatus.Active, UpdatedAt = Now.AddDays(-5) },
            new Process { ProcessId = "PACKING", Name = "Packing", Type = ProcessType.Work, MeasuredIn = ProcessUnit.Units, Status = ProcessStatus.Active },
            new Process { ProcessId = "POLISH", Name = "Polishing", Type = ProcessType.Work, MeasuredIn = ProcessUnit.Minutes, Status = ProcessStatus.Inactive });
        factoryRows = db.SetupSet(x => x.ProcessFactories,
            new ProcessFactory { ProcessId = "ENGRAVING", FactoryId = "110", RunsHere = true, MachineType = "LASER", CapacityPct = 80, MaxPerDay = 1000, LaborCostAmount = 0.5m, LaborCostCurrency = "USD", UpdatedAt = Now.AddDays(-1) },
            new ProcessFactory { ProcessId = "ENGRAVING", FactoryId = "125", RunsHere = true, MachineType = "CNC", CapacityPct = 100, LaborCostAmount = 0.7m, LaborCostCurrency = "USD" },
            new ProcessFactory { ProcessId = "ENGRAVING", FactoryId = "315", RunsHere = false, CapacityPct = 0 },
            new ProcessFactory { ProcessId = "PACKING", FactoryId = "110", RunsHere = true, LaborCostAmount = 1m, LaborCostCurrency = "USD" },
            new ProcessFactory { ProcessId = "PACKING", FactoryId = "125", RunsHere = true, LaborCostAmount = 300m, LaborCostCurrency = "HUF" });
        db.SetupSet(x => x.Factories,
            new Factory { Id = "110", Name = "Factory 110", IsActive = true },
            new Factory { Id = "125", Name = "Factory 125", IsActive = true },
            new Factory { Id = "315", Name = "Factory 315", IsActive = true });
        db.SetupSet(x => x.SkuProcesses,
            new SkuProcess { SkuUuid = RingSku, ProcessId = "ENGRAVING", Quantity = 2 },
            new SkuProcess { SkuUuid = RingSku, ProcessId = "PACKING", Quantity = 1 },
            new SkuProcess { SkuUuid = Guid.NewGuid(), ProcessId = "PACKING", Quantity = 1 });
        db.SetupSet(x => x.Skus, new Sku { SkuUuid = RingSku, Name = "Ring", SiteSku = "110-12-4492-33" });
        db.SetupSet(x => x.Quotes);
        db.SetupSet(x => x.QuoteSets);
    }

    private async Task<List<ProcessListItemDto>> Search(ListRequest request)
    {
        var result = await new SearchProcessesQueryHandler(new ProcessListReader(db.Object, currentUser.Object))
            .Handle(new SearchProcessesQuery(request), CancellationToken.None);

        return result.Items.ToList();
    }

    private Task<ProcessDto> Get(string processId)
    {
        return new GetProcessQueryHandler(db.Object, new ProcessUsageReader(db.Object, TestDb.FixedTime(Now)), currentUser.Object)
            .Handle(new GetProcessQuery(processId), CancellationToken.None);
    }

    private Task<string> Create(string processId, string name, ProcessType type = ProcessType.Work)
    {
        return new CreateProcessCommandHandler(db.Object, historyWriter.Object, TestDb.FixedTime(Now))
            .Handle(new CreateProcessCommand(processId, name, type, ProcessUnit.Minutes, null), CancellationToken.None);
    }

    private Task Update(string processId, string name, ProcessType type, ProcessStatus status = ProcessStatus.Active)
    {
        return new UpdateProcessCommandHandler(db.Object, historyWriter.Object, TestDb.FixedTime(Now))
            .Handle(new UpdateProcessCommand(processId, name, type, ProcessUnit.Minutes, status, null), CancellationToken.None);
    }

    private Task Setup(string processId, string factoryId, bool runsHere, string machineType = null, decimal? cost = null, string currency = null, decimal? maxPerDay = null)
    {
        return new UpdateProcessFactorySetupCommandHandler(db.Object, currentUser.Object, historyWriter.Object, TestDb.FixedTime(Now))
            .Handle(new UpdateProcessFactorySetupCommand(processId, factoryId, runsHere, machineType, cost, currency, maxPerDay), CancellationToken.None);
    }

    [Test]
    public async Task Search_DerivedColumns()
    {
        var items = await Search(new ListRequest());

        Assert.That(items.Select(i => i.ProcessId), Is.EqualTo(new[] { "ENGRAVING", "PACKING", "POLISH" }));
        var engraving = items[0];
        Assert.That(engraving.MachineType, Is.EqualTo("LASER, CNC"));
        Assert.That(engraving.RunningFactoryIds, Is.EqualTo(new[] { "110", "125" }));
        Assert.That(engraving.LowestCapacityPct, Is.EqualTo(80));
        Assert.That(engraving.AvgLaborCost, Is.EqualTo(0.6m));
        Assert.That(engraving.AvgLaborCostCurrency, Is.EqualTo("USD"));
        Assert.That(engraving.SkusUsing, Is.EqualTo(1));
        Assert.That(items[1].MachineType, Is.Null);
        Assert.That(items[1].AvgLaborCost, Is.Null);
        Assert.That(items[1].SkusUsing, Is.EqualTo(2));
        Assert.That(items[2].RunningFactoryIds, Is.Empty);
        Assert.That(items[2].LowestCapacityPct, Is.Null);
    }

    [Test]
    public async Task Search_SearchFiltersSortAndPaging()
    {
        Assert.That((await Search(new ListRequest { Search = "pack" })).Select(i => i.ProcessId), Is.EqualTo(new[] { "PACKING" }));
        Assert.That(
            (await Search(new ListRequest { Filters = { ["type"] = new ListFilter { Values = ["work"] } } })).Select(i => i.ProcessId),
            Is.EqualTo(new[] { "PACKING", "POLISH" }));
        Assert.That(
            (await Search(new ListRequest { Filters = { ["runningFactoryIds"] = new ListFilter { Values = ["125"] } } })).Count,
            Is.EqualTo(2));
        Assert.That(
            (await Search(new ListRequest { Filters = { ["lowestCapacityPct"] = new ListFilter { Operator = "lt", Value = "100" } } })).Select(i => i.ProcessId),
            Is.EqualTo(new[] { "ENGRAVING" }));
        Assert.That(
            (await Search(new ListRequest { SortBy = "skusUsing", SortDirection = "desc", PageSize = 1 })).Select(i => i.ProcessId),
            Is.EqualTo(new[] { "PACKING" }));
    }

    [Test]
    public async Task Search_WithoutCostPermission_HidesLaborCost()
    {
        currentUser.Setup(u => u.HasPermission(Permission.ProcessesViewCost, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Assert.That((await Search(new ListRequest())).Select(i => i.AvgLaborCost), Is.All.Null);
    }

    [Test]
    public async Task Export_ContainsEveryMatchingRow()
    {
        var file = await new ExportProcessesQueryHandler(new ProcessListReader(db.Object, currentUser.Object), TestDb.FixedTime(Now))
            .Handle(new ExportProcessesQuery(new ExportRequest { PageSize = 1, Columns = ["processId", "name"] }), CancellationToken.None);

        using var workbook = new ClosedXML.Excel.XLWorkbook(new MemoryStream(file.Content));
        var sheet = workbook.Worksheet(1);
        Assert.That(file.FileName, Is.EqualTo("processes_20261124_100000.xlsx"));
        Assert.That(sheet.Cell(1, 1).GetString(), Is.EqualTo("Process ID"));
        Assert.That(sheet.LastRowUsed().RowNumber(), Is.EqualTo(4));
    }

    [Test]
    public async Task Get_ReturnsEveryTab()
    {
        var process = await Get("ENGRAVING");

        Assert.That(process.MeasuredIn, Is.EqualTo(ProcessUnit.Minutes));
        Assert.That(process.LastUpdate, Is.EqualTo(Now.AddDays(-1)));
        Assert.That(process.Factories.Select(f => f.FactoryId), Is.EqualTo(new[] { "110", "125", "315" }));
        var own = process.Factories.First();
        Assert.That(own.MachineType, Is.EqualTo("LASER"));
        Assert.That(own.LaborCost.AmountPerUnit, Is.EqualTo(0.5m));
        Assert.That(own.Today.Available, Is.EqualTo(800));
        Assert.That(own.Today.RemainingPct, Is.EqualTo(80));
        Assert.That(process.Factories.Last().RunsHere, Is.False);
        Assert.That(process.Factories.Last().Today, Is.Null);
        var sku = process.Skus.Single();
        Assert.That(sku.SiteSku, Is.EqualTo("110-12-4492-33"));
        Assert.That(sku.Quantity, Is.EqualTo(2));
    }

    [Test]
    public async Task Get_WithoutCostPermission_HidesLaborCost()
    {
        currentUser.Setup(u => u.HasPermission(Permission.ProcessesViewCost, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Assert.That((await Get("ENGRAVING")).Factories.Select(f => f.LaborCost), Is.All.Null);
    }

    [Test]
    public void Get_Unknown_NotFound()
    {
        Assert.ThrowsAsync<EntityNotFoundException>(() => Get("NOPE"));
    }

    [Test]
    public async Task Create_DerivesIdAndLogs()
    {
        var id = await Create(null, " Stone setting ", ProcessType.Machine);

        Assert.That(id, Is.EqualTo("STONE_SETTING"));
        var created = processes.Single(p => p.ProcessId == id);
        Assert.That(created.Name, Is.EqualTo("Stone setting"));
        Assert.That(created.Status, Is.EqualTo(ProcessStatus.Active));
        Assert.That(created.CreatedAt, Is.EqualTo(Now));
        historyWriter.Verify(h => h.Record(HistoryEntityTypes.Process, id, "process", null, "Created", HistorySource.User, null, null), Times.Once);
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Create_DuplicateName_Rejected()
    {
        var exception = Assert.ThrowsAsync<ValidationException>(() => Create("ENGRAVING_2", "engraving"));

        Assert.That(exception.ValidationMessages.Keys, Is.EqualTo(new[] { "name" }));
        Assert.That(processes, Has.Count.EqualTo(3));
    }

    [Test]
    public void Create_DuplicateOrInvalidId_Rejected()
    {
        Assert.That(
            Assert.ThrowsAsync<ValidationException>(() => Create("engraving", "Other")).ValidationMessages.Keys,
            Is.EqualTo(new[] { "processId" }));
        Assert.That(
            Assert.ThrowsAsync<ValidationException>(() => Create("BAD ID", "Other")).ValidationMessages.Keys,
            Is.EqualTo(new[] { "processId" }));
        Assert.That(
            Assert.ThrowsAsync<ValidationException>(() => Create(null, " ")).ValidationMessages.Keys,
            Is.EqualTo(new[] { "name" }));
    }

    [Test]
    public async Task Update_ChangesSpecificationAndLogsEachField()
    {
        await Update("POLISH", "Hand polishing", ProcessType.Work);

        var process = processes.Single(p => p.ProcessId == "POLISH");
        Assert.That(process.Name, Is.EqualTo("Hand polishing"));
        Assert.That(process.Status, Is.EqualTo(ProcessStatus.Active));
        Assert.That(process.UpdatedAt, Is.EqualTo(Now));
        historyWriter.Verify(h => h.Record(HistoryEntityTypes.Process, "POLISH", "name", "Polishing", "Hand polishing", HistorySource.User, null, null), Times.Once);
        historyWriter.Verify(h => h.Record(HistoryEntityTypes.Process, "POLISH", "status", "Inactive", "Active", HistorySource.User, null, null), Times.Once);
    }

    [Test]
    public void Update_NameOfAnotherProcess_Rejected()
    {
        var exception = Assert.ThrowsAsync<ValidationException>(() => Update("POLISH", "ENGRAVING", ProcessType.Work));

        Assert.That(exception.ValidationMessages.Keys, Is.EqualTo(new[] { "name" }));
    }

    [Test]
    public async Task Update_Deactivate_KeepsTheProcess()
    {
        await Update("ENGRAVING", "Engraving", ProcessType.Machine, ProcessStatus.Inactive);

        Assert.That(processes, Has.Count.EqualTo(3));
        Assert.That(processes[0].Status, Is.EqualTo(ProcessStatus.Inactive));
    }

    [Test]
    public async Task Update_MachineToWork_ClearsMachineTypes()
    {
        await Update("ENGRAVING", "Engraving", ProcessType.Work);

        Assert.That(factoryRows.Where(r => r.ProcessId == "ENGRAVING").Select(r => r.MachineType), Is.All.Null);
        historyWriter.Verify(h => h.Record(HistoryEntityTypes.Process, "ENGRAVING", "machineType", "LASER", null, HistorySource.User, "110", null), Times.Once);
    }

    [Test]
    public void Update_Packing_MustStayActiveWorkProcess()
    {
        Assert.That(
            Assert.ThrowsAsync<ValidationException>(() => Update("PACKING", "Packing", ProcessType.Machine)).ValidationMessages.Keys,
            Is.EqualTo(new[] { "type" }));
        Assert.That(
            Assert.ThrowsAsync<ValidationException>(() => Update("PACKING", "Packing", ProcessType.Work, ProcessStatus.Inactive)).ValidationMessages.Keys,
            Is.EqualTo(new[] { "status" }));
    }

    [Test]
    public async Task Update_NothingChanged_NoSave()
    {
        await Update("ENGRAVING", "Engraving", ProcessType.Machine);

        historyWriter.VerifyNoOtherCalls();
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void Setup_OtherFactory_Forbidden()
    {
        Assert.ThrowsAsync<ForbiddenException>(() => Setup("ENGRAVING", "125", false));

        Assert.That(factoryRows.Single(r => r.ProcessId == "ENGRAVING" && r.FactoryId == "125").RunsHere, Is.True);
    }

    [Test]
    public async Task Setup_FirstTime_CreatesRowAtFullCapacity()
    {
        await Setup("POLISH", "110", true, cost: 0.4m, currency: "usd", maxPerDay: 200);

        var row = factoryRows.Single(r => r.ProcessId == "POLISH");
        Assert.That(row.RunsHere, Is.True);
        Assert.That(row.CapacityPct, Is.EqualTo(100));
        Assert.That(row.LaborCostAmount, Is.EqualTo(0.4m));
        Assert.That(row.LaborCostCurrency, Is.EqualTo("USD"));
        Assert.That(row.LaborCostEffectiveFrom, Is.EqualTo(new DateOnly(2026, 11, 24)));
        Assert.That(row.MaxPerDay, Is.EqualTo(200));
        historyWriter.Verify(h => h.Record(HistoryEntityTypes.Process, "POLISH", "runsHere", false, true, HistorySource.User, "110", null), Times.Once);
        historyWriter.Verify(h => h.Record(HistoryEntityTypes.Process, "POLISH", "laborCostAmount", null, 0.4m, HistorySource.User, "110", null), Times.Once);
    }

    [Test]
    public async Task Setup_StopRunning_KeepsCapacityUntouched()
    {
        await Setup("ENGRAVING", "110", false, "LASER", 0.5m, "USD", 1000);

        var row = factoryRows[0];
        Assert.That(row.RunsHere, Is.False);
        Assert.That(row.CapacityPct, Is.EqualTo(80));
        Assert.That(row.LaborCostEffectiveFrom, Is.Null);
    }

    [Test]
    public void Setup_InvalidValues_Rejected()
    {
        Assert.That(
            Assert.ThrowsAsync<ValidationException>(() => Setup("PACKING", "110", true, machineType: "LASER")).ValidationMessages.Keys,
            Is.EqualTo(new[] { "machineType" }));
        Assert.That(
            Assert.ThrowsAsync<ValidationException>(() => Setup("PACKING", "110", true, cost: -1, currency: "USD")).ValidationMessages.Keys,
            Is.EqualTo(new[] { "laborCostAmount" }));
        Assert.That(
            Assert.ThrowsAsync<ValidationException>(() => Setup("PACKING", "110", true, cost: 1)).ValidationMessages.Keys,
            Is.EqualTo(new[] { "laborCostCurrency" }));
        Assert.That(
            Assert.ThrowsAsync<ValidationException>(() => Setup("PACKING", "110", true, maxPerDay: -5)).ValidationMessages.Keys,
            Is.EqualTo(new[] { "maxPerDay" }));
    }

    [Test]
    public void Setup_UnknownProcessOrFactory_NotFound()
    {
        Assert.ThrowsAsync<EntityNotFoundException>(() => Setup("NOPE", "110", true));
        Assert.ThrowsAsync<EntityNotFoundException>(() => Setup("PACKING", "999", true));
    }
}
