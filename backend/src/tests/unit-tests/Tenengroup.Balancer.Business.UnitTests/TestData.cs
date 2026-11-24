using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using Tenengroup.Balancer.Business.Abstractions.Catalog;
using Tenengroup.Balancer.Business.Abstractions.Decisions;
using Tenengroup.Balancer.DataAccess.Abstractions;
using Tenengroup.Balancer.Domains;

namespace Tenengroup.Balancer.Business.UnitTests;

internal static class TestData
{
    public static readonly DateTime Now = new(2026, 11, 24, 10, 0, 0, DateTimeKind.Utc);
    public static readonly DateOnly Today = new(2026, 11, 24);

    public static readonly DecisionSettings Settings = new(5m, 3m, 0.1m, false, 0.5m, 3);

    public static List<T> SetupSet<T>(
        this Mock<IBalancerDbContext> db,
        Expression<Func<IBalancerDbContext, DbSet<T>>> set,
        params T[] items) where T : class
    {
        var list = items.ToList();
        var dbSet = new Mock<DbSet<T>>();
        db.Setup(set).ReturnsDbSet(list, dbSet);
        dbSet.Setup(s => s.Add(It.IsAny<T>())).Callback<T>(list.Add);
        dbSet.Setup(s => s.RemoveRange(It.IsAny<IEnumerable<T>>()))
            .Callback<IEnumerable<T>>(removed => list.RemoveAll(removed.ToList().Contains));

        return list;
    }

    public static Mock<IBalancerDbContext> EmptyDb()
    {
        var db = new Mock<IBalancerDbContext>();
        db.SetupSet(x => x.ShippingRoutes);
        db.SetupSet(x => x.Allocations);
        db.SetupSet(x => x.AllocationCostLines);
        db.SetupSet(x => x.LegacyDecisions);
        db.SetupSet(x => x.CapacityReservations);
        db.SetupSet(x => x.AllocationTransfers);
        db.SetupSet(x => x.ProductionRecords);
        db.SetupSet(x => x.Settings);

        return db;
    }

    public static TimeProvider FixedTime()
    {
        var time = new Mock<TimeProvider> { CallBase = true };
        time.Setup(t => t.GetUtcNow()).Returns(new DateTimeOffset(Now, TimeSpan.Zero));

        return time.Object;
    }

    public static FactoryQuote Quote(string factoryId, decimal total, decimal sharePct = 50m, int? capacityPct = 100, string quoteKey = "PQ-O1-I1")
    {
        return new FactoryQuote(
            $"{quoteKey}-{factoryId}",
            factoryId,
            true,
            [],
            new QuoteCost(total - 2m, 1.5m, 0.5m, total, "USD"),
            capacityPct,
            sharePct,
            [new QuoteComponent("CMP-1", 1m, total - 2m, "ok", null, total - 2m)],
            [new QuoteProcess("ENGRAVING", 2m, 100, 1.5m, 100), new QuoteProcess("PACKING", 1m, 100, 0.5m, 100)]);
    }

    public static FactoryQuote NotProducible(string factoryId, params string[] reasons)
    {
        return new FactoryQuote($"PQ-O1-I1-{factoryId}", factoryId, false, reasons, null, null, 0m, [], []);
    }

    public static QuoteSet Set(params FactoryQuote[] quotes) => new("PQ-O1-I1", Now.AddMinutes(15), quotes);

    public static RouteOption Route(string factoryId, decimal cost, int min = 3, int max = 5) => new(factoryId, cost, "USD", min, max);

    public static DecisionInput Input(
        IReadOnlyCollection<DecisionItem> items,
        IReadOnlyCollection<RouteOption> routes,
        DateOnly? promised = null,
        DateOnly? sla = null,
        bool isEventOrder = false,
        Dictionary<string, int> reserved = null,
        Dictionary<string, decimal> actualShare = null,
        Dictionary<string, decimal> fallbackShare = null,
        Dictionary<string, int> productionDays = null,
        Dictionary<string, decimal> successRates = null,
        IReadOnlyCollection<string> anchors = null,
        DecisionSettings settings = null)
    {
        return new DecisionInput(
            items,
            Today,
            promised,
            sla,
            isEventOrder,
            routes,
            reserved ?? new Dictionary<string, int>(),
            actualShare ?? new Dictionary<string, decimal>(),
            fallbackShare ?? new Dictionary<string, decimal>(),
            productionDays ?? new Dictionary<string, int>(),
            successRates ?? new Dictionary<string, decimal>(),
            anchors ?? [],
            settings ?? Settings);
    }

    public static Allocation OpenAllocation(string orderId = "O1", string orderItemId = "I1", string factoryId = "110")
    {
        return new Allocation
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            OrderItemId = orderItemId,
            SkuUuid = "SKU-1",
            Quantity = 1,
            FulfillmentType = FulfillmentType.Factory,
            DestinationCountry = "DE",
            Mode = AllocationMode.Live,
            Status = AllocationStatus.Allocated,
            QuoteKey = $"PQ-{orderId}-{orderItemId}",
            SelectedQuoteId = $"PQ-{orderId}-{orderItemId}-{factoryId}",
            FactoryId = factoryId,
            OriginalFactoryId = factoryId,
            PackingFacilityId = factoryId,
            MaterialCost = 38m,
            LaborCost = 6m,
            PackingCost = 1m,
            ProductionCost = 45m,
            ShippingCost = 10m,
            TotalCost = 55m,
            CheapestFeasibleTotalCost = 55m,
            Currency = "USD",
            EtaFrom = Today.AddDays(6),
            EtaTo = Today.AddDays(8),
            DueDate = Today.AddDays(3),
            DecidedAt = Now,
            DecisionReason = "Lowest total cost"
        };
    }
}
