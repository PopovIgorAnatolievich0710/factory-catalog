using Moq;
using Tenengroup.FactoryCatalog.Business.Abstractions.Quotes;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.LegacyBalancer;
using Tenengroup.FactoryCatalog.Domains.Processes;
using Tenengroup.FactoryCatalog.Domains.Quotes;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Json;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Quotes;

internal class CatalogFixture
{
    public const string Factory110 = "110";
    public const string Factory125 = "125";
    public const string Factory315 = "315";
    public const string Board = "R.VM.BRD.08MM";
    public const string Engraving = "ENGRAVING";

    public static readonly DateTime Now = new(2026, 11, 24, 10, 0, 0, DateTimeKind.Utc);
    public static readonly Guid SkuUuid = Guid.Parse("8f3c1a2e-5b7d-4c1a-9e21-7d4e5f6a8b90");

    public CatalogFixture()
    {
        Factories = Db.SetupSet(x => x.Factories);
        Components = Db.SetupSet(x => x.Components);
        ComponentFactories = Db.SetupSet(x => x.ComponentFactories);
        ComponentAlternatives = Db.SetupSet(x => x.ComponentAlternatives);
        ProcessFactories = Db.SetupSet(x => x.ProcessFactories);
        Skus = Db.SetupSet(x => x.Skus);
        SkuAliases = Db.SetupSet(x => x.SkuAliases);
        SkuComponents = Db.SetupSet(x => x.SkuComponents);
        SkuProcesses = Db.SetupSet(x => x.SkuProcesses);
        SkuFactories = Db.SetupSet(x => x.SkuFactories);
        GlobalFactoryShares = Db.SetupSet(x => x.GlobalFactoryShares);
        QuoteSets = Db.SetupSet(x => x.QuoteSets);
        Quotes = Db.SetupSet(x => x.Quotes);
        SkipLogs = Db.SetupSet(x => x.BalancerSkipLogs);

        CurrentUser.Setup(u => u.IsServiceAccount).Returns(true);
    }

    public Mock<IFactoryCatalogDbContext> Db { get; } = new();

    public Mock<ICurrentUser> CurrentUser { get; } = new();

    public TimeProvider Time { get; } = TestDb.FixedTime(Now);

    public List<Factory> Factories { get; }

    public List<Component> Components { get; }

    public List<ComponentFactory> ComponentFactories { get; }

    public List<ComponentAlternative> ComponentAlternatives { get; }

    public List<ProcessFactory> ProcessFactories { get; }

    public List<Sku> Skus { get; }

    public List<SkuAlias> SkuAliases { get; }

    public List<SkuComponent> SkuComponents { get; }

    public List<SkuProcess> SkuProcesses { get; }

    public List<SkuFactory> SkuFactories { get; }

    public List<GlobalFactoryShare> GlobalFactoryShares { get; }

    public List<QuoteSet> QuoteSets { get; }

    public List<Quote> Quotes { get; }

    public List<BalancerSkipLog> SkipLogs { get; }

    public static CatalogFixture Standard()
    {
        var fixture = new CatalogFixture();
        fixture.AddFactory(Factory110, sharePct: 60);
        fixture.AddFactory(Factory125, sharePct: 40);
        fixture.AddSku(SkuUuid);
        fixture.AddComponent(Board, unitCost: 21m);
        fixture.MapProcess(SkuUuid, Engraving, 2m);
        fixture.MapProcess(SkuUuid, Process.PackingProcessId, 1m);

        foreach (var factoryId in new[] { Factory110, Factory125 })
        {
            fixture.SetStock(Board, factoryId, warehouse: 10);
            fixture.SetProcessFactory(Engraving, factoryId, laborCost: 0.55m);
            fixture.SetProcessFactory(Process.PackingProcessId, factoryId, laborCost: 1.4m);
            fixture.SetSkuFactory(SkuUuid, factoryId);
        }

        return fixture;
    }

    public void AddFactory(string id, decimal? sharePct = null, bool isActive = true)
    {
        Factories.Add(new Factory { Id = id, Name = $"Factory {id}", IsActive = isActive });

        if (sharePct != null)
        {
            GlobalFactoryShares.Add(new GlobalFactoryShare { FactoryId = id, Pct = sharePct.Value });
        }
    }

    public Sku AddSku(
        Guid skuUuid,
        string siteSku = "TGI-N-4609",
        SkuProductType productType = SkuProductType.Manufactured,
        SkuValidationStatus validationStatus = SkuValidationStatus.Valid,
        SkuStatus status = SkuStatus.Active)
    {
        var sku = new Sku
        {
            SkuUuid = skuUuid,
            SiteSku = siteSku,
            Name = siteSku,
            ProductType = productType,
            ValidationStatus = validationStatus,
            Status = status
        };
        Skus.Add(sku);

        return sku;
    }

    public void AddAlias(Guid skuUuid, string alias)
    {
        SkuAliases.Add(new SkuAlias { SkuUuid = skuUuid, Alias = alias, Type = SkuAliasType.Factory, Deprecated = true });
    }

    public Component AddComponent(
        string erpId,
        decimal? unitCost = null,
        decimal? criticalLevel = null,
        ComponentStatus status = ComponentStatus.Active,
        string currency = "USD")
    {
        var component = new Component
        {
            ErpId = erpId,
            Name = erpId,
            Status = status,
            UnitCostAmount = unitCost,
            UnitCostCurrency = unitCost == null ? null : currency,
            CriticalStockLevel = criticalLevel
        };
        Components.Add(component);

        return component;
    }

    public ComponentFactory SetStock(
        string erpId,
        string factoryId,
        decimal warehouse,
        decimal productionFloor = 0,
        bool manualOos = false,
        decimal? criticalOverride = null)
    {
        ComponentFactories.RemoveAll(s => s.ComponentErpId == erpId && s.FactoryId == factoryId);

        var stock = new ComponentFactory
        {
            ComponentErpId = erpId,
            FactoryId = factoryId,
            WarehouseStock = warehouse,
            ProductionFloorStock = productionFloor,
            ManualOosActive = manualOos,
            CriticalStockLevelOverride = criticalOverride
        };
        ComponentFactories.Add(stock);

        return stock;
    }

    public void AddAlternative(string erpId, string alternativeErpId, bool forBalancer = true, bool forPlanner = false)
    {
        ComponentAlternatives.Add(new ComponentAlternative
        {
            ComponentErpId = erpId,
            AlternativeErpId = alternativeErpId,
            ForBalancer = forBalancer,
            ForPlanner = forPlanner
        });
    }

    public void MapComponent(Guid skuUuid, string erpId, ComponentUsage usage, decimal quantity = 1)
    {
        SkuComponents.Add(new SkuComponent { SkuUuid = skuUuid, ComponentErpId = erpId, Usage = usage, Quantity = quantity });
    }

    public void MapProcess(Guid skuUuid, string processId, decimal quantity)
    {
        SkuProcesses.Add(new SkuProcess { SkuUuid = skuUuid, ProcessId = processId, Quantity = quantity });
    }

    public ProcessFactory SetProcessFactory(
        string processId,
        string factoryId,
        bool runsHere = true,
        int capacityPct = 100,
        decimal? laborCost = null,
        decimal? maxPerDay = null,
        string currency = "USD")
    {
        ProcessFactories.RemoveAll(p => p.ProcessId == processId && p.FactoryId == factoryId);

        var processFactory = new ProcessFactory
        {
            ProcessId = processId,
            FactoryId = factoryId,
            RunsHere = runsHere,
            CapacityPct = capacityPct,
            LaborCostAmount = laborCost,
            LaborCostCurrency = laborCost == null ? null : currency,
            MaxPerDay = maxPerDay
        };
        ProcessFactories.Add(processFactory);

        return processFactory;
    }

    public SkuFactory SetSkuFactory(Guid skuUuid, string factoryId, bool producible = true, decimal? overrideSharePct = null)
    {
        SkuFactories.RemoveAll(f => f.SkuUuid == skuUuid && f.FactoryId == factoryId);

        var skuFactory = new SkuFactory
        {
            SkuUuid = skuUuid,
            FactoryId = factoryId,
            Producible = producible,
            OverrideSharePct = overrideSharePct
        };
        SkuFactories.Add(skuFactory);

        return skuFactory;
    }

    public QuoteSet AddQuoteSet(
        long id,
        string orderId = "ORD456789",
        string orderItemId = "ITM001",
        QuoteMode mode = QuoteMode.Live,
        Guid? skuUuid = null)
    {
        var set = new QuoteSet
        {
            Id = id,
            QuoteKey = $"PQ-{orderId}-{orderItemId}",
            RequestId = Guid.NewGuid(),
            OrderId = orderId,
            OrderItemId = orderItemId,
            SkuUuid = skuUuid ?? SkuUuid,
            Quantity = 1,
            Mode = mode,
            ValidUntil = Now.AddMinutes(15),
            CreatedAt = Now
        };
        QuoteSets.Add(set);

        return set;
    }

    public Quote AddQuote(
        long id,
        QuoteSet set,
        string factoryId,
        QuoteStatus status = QuoteStatus.Quoted,
        bool producible = true,
        DateTime? producedAt = null,
        DateTime? selectedAt = null)
    {
        var quote = new Quote
        {
            Id = id,
            QuoteId = $"{set.QuoteKey}-{factoryId}",
            QuoteSetId = set.Id,
            FactoryId = factoryId,
            Producible = producible,
            Reasons = producible ? [] : [QuoteReasonCodes.ComponentOos],
            MaterialCost = producible ? 38.2m : null,
            LaborCost = producible ? 6.1m : null,
            PackingCost = producible ? 1.4m : null,
            TotalCost = producible ? 45.7m : null,
            Currency = producible ? "USD" : null,
            CapacitySignalPct = producible ? 80 : null,
            SharePct = producible ? 100 : 0,
            ComponentsJson = new[] { new QuoteComponentDto(Board, 1, 38.2m, StockStatuses.Ok, null, 38.2m, "USD") }.ToJsonString(),
            ProcessesJson = new[]
            {
                new QuoteProcessDto(Engraving, 2, 80, 6.1m, 80, "USD"),
                new QuoteProcessDto(Process.PackingProcessId, 1, 100, 1.4m, 100, "USD")
            }.ToJsonString(),
            Status = status,
            SelectedAt = selectedAt ?? (status == QuoteStatus.Quoted ? null : Now.AddHours(-2)),
            ProducedAt = producedAt,
            CreatedAt = Now.AddHours(-3)
        };
        Quotes.Add(quote);

        return quote;
    }
}
