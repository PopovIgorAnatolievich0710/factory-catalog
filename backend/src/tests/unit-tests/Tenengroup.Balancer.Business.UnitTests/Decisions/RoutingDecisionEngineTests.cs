using NUnit.Framework;
using Tenengroup.Balancer.Business.Abstractions.Decisions;
using Tenengroup.Balancer.Business.Decisions;
using Tenengroup.Infrastructure.Common.Json;
using static Tenengroup.Balancer.Business.UnitTests.TestData;

namespace Tenengroup.Balancer.Business.UnitTests.Decisions;

[TestFixture]
public class RoutingDecisionEngineTests
{
    private readonly RoutingDecisionEngine engine = new();

    private static DecisionItem Item(string id, params Abstractions.Catalog.FactoryQuote[] quotes) => new(id, Set(quotes));

    private ItemDecision DecideSingle(DecisionInput input) => engine.Decide(input).Items.Single();

    [Test]
    public void Picks_the_lowest_total_of_production_plus_shipping()
    {
        var decision = DecideSingle(Input(
            [Item("I1", Quote("110", 45m), Quote("125", 40m))],
            [Route("110", 5m), Route("125", 20m)]));

        Assert.That(decision.Winner.FactoryId, Is.EqualTo("110"));
        Assert.That(decision.Winner.TotalCost, Is.EqualTo(50m));
        Assert.That(decision.Winner.ProductionCost, Is.EqualTo(45m));
        Assert.That(decision.Winner.ShippingCost, Is.EqualTo(5m));
        Assert.That(decision.IsFallback, Is.False);
        Assert.That(decision.Reason, Is.EqualTo("Lowest total cost"));
        Assert.That(decision.Candidates.Count(c => c.IsWinner), Is.EqualTo(1));
    }

    [Test]
    public void Eta_is_production_time_plus_transit()
    {
        var decision = DecideSingle(Input(
            [Item("I1", Quote("110", 45m))],
            [Route("110", 5m, 3, 5)],
            productionDays: new Dictionary<string, int> { ["110"] = 4 }));

        Assert.That(decision.Winner.EtaFrom, Is.EqualTo(Today.AddDays(7)));
        Assert.That(decision.Winner.EtaTo, Is.EqualTo(Today.AddDays(9)));
    }

    [Test]
    public void Uses_the_default_production_days_when_a_factory_has_no_history()
    {
        var decision = DecideSingle(Input([Item("I1", Quote("110", 45m))], [Route("110", 5m, 3, 5)]));

        Assert.That(decision.Winner.EtaTo, Is.EqualTo(Today.AddDays(TestData.Settings.DefaultProductionDays + 5)));
    }

    [Test]
    public void Drops_a_factory_without_a_route_to_the_destination()
    {
        var decision = DecideSingle(Input(
            [Item("I1", Quote("110", 45m), Quote("125", 30m))],
            [Route("110", 5m)]));

        Assert.That(decision.Winner.FactoryId, Is.EqualTo("110"));
        Assert.That(decision.Candidates.Single(c => c.FactoryId == "125").ExcludedReason, Is.EqualTo(ExclusionReasons.NoRoute));
        Assert.That(decision.Reason, Is.EqualTo("Only feasible factory"));
    }

    [Test]
    public void Excludes_a_non_producible_factory_with_the_catalog_reasons()
    {
        var decision = DecideSingle(Input(
            [Item("I1", Quote("110", 45m), NotProducible("125", "COMPONENT_OOS", "PROCESS_OFFLINE"))],
            [Route("110", 5m), Route("125", 1m)]));

        Assert.That(decision.Winner.FactoryId, Is.EqualTo("110"));
        Assert.That(decision.Candidates.Single(c => c.FactoryId == "125").ExcludedReason, Is.EqualTo("COMPONENT_OOS,PROCESS_OFFLINE"));
    }

    [Test]
    public void Excludes_a_factory_with_no_capacity_left()
    {
        var decision = DecideSingle(Input(
            [Item("I1", Quote("110", 45m), Quote("125", 30m, capacityPct: 0))],
            [Route("110", 5m), Route("125", 5m)]));

        Assert.That(decision.Winner.FactoryId, Is.EqualTo("110"));
        Assert.That(decision.Candidates.Single(c => c.FactoryId == "125").ExcludedReason, Is.EqualTo(ExclusionReasons.NoCapacity));
    }

    [Test]
    public void Late_penalty_makes_the_on_time_factory_win_over_a_slightly_cheaper_late_one()
    {
        var decision = DecideSingle(Input(
            [Item("I1", Quote("110", 45m), Quote("125", 43m))],
            [Route("110", 5m, 2, 3), Route("125", 5m, 5, 8)],
            promised: Today.AddDays(6)));

        Assert.That(decision.Winner.FactoryId, Is.EqualTo("110"));
        Assert.That(decision.Winner.MeetsPromise, Is.True);
        Assert.That(decision.Candidates.Single(c => c.FactoryId == "125").MeetsPromise, Is.False);
        Assert.That(decision.Reason, Does.StartWith("Lowest total cost that meets the delivery promise; cheaper 125"));
        Assert.That(decision.CheapestFeasibleTotalCost, Is.EqualTo(48m));
    }

    [Test]
    public void Marks_the_promise_as_missed_when_no_factory_can_meet_it()
    {
        var decision = DecideSingle(Input(
            [Item("I1", Quote("110", 45m))],
            [Route("110", 5m, 5, 8)],
            promised: Today.AddDays(2)));

        Assert.That(decision.Winner.MeetsPromise, Is.False);
        Assert.That(decision.IsFallback, Is.False);
    }

    [Test]
    public void Sla_is_met_even_at_a_much_higher_cost()
    {
        var decision = DecideSingle(Input(
            [Item("I1", Quote("110", 200m), Quote("125", 40m))],
            [Route("110", 5m, 1, 2), Route("125", 5m, 6, 9)],
            sla: Today.AddDays(5)));

        Assert.That(decision.Winner.FactoryId, Is.EqualTo("110"));
        Assert.That(decision.Winner.MeetsSla, Is.True);
        Assert.That(decision.Reason, Is.EqualTo("Meets the SLA; cheaper 125 does not"));
    }

    [Test]
    public void Without_an_sla_the_cheaper_factory_wins()
    {
        var decision = DecideSingle(Input(
            [Item("I1", Quote("110", 200m), Quote("125", 40m))],
            [Route("110", 5m, 1, 2), Route("125", 5m, 6, 9)]));

        Assert.That(decision.Winner.FactoryId, Is.EqualTo("125"));
    }

    [Test]
    public void Share_breaks_a_cost_tie()
    {
        var decision = DecideSingle(Input(
            [Item("I1", Quote("110", 45m, sharePct: 30m), Quote("125", 45m, sharePct: 70m))],
            [Route("110", 5m), Route("125", 5m)]));

        Assert.That(decision.Winner.FactoryId, Is.EqualTo("125"));
    }

    [Test]
    public void Share_spreads_volume_to_the_factory_that_is_below_its_target()
    {
        var decision = DecideSingle(Input(
            [Item("I1", Quote("110", 45m, sharePct: 30m), Quote("125", 45m, sharePct: 70m))],
            [Route("110", 5m), Route("125", 5m)],
            actualShare: new Dictionary<string, decimal> { ["110"] = 5m, ["125"] = 95m }));

        Assert.That(decision.Winner.FactoryId, Is.EqualTo("110"));
    }

    [Test]
    public void Partial_capacity_lowers_the_share_and_frees_it_for_the_other_factories()
    {
        var decision = DecideSingle(Input(
            [Item("I1", Quote("110", 45m, sharePct: 50m, capacityPct: 20), Quote("125", 45m, sharePct: 50m, capacityPct: 100))],
            [Route("110", 5m), Route("125", 5m)]));

        Assert.That(decision.Winner.FactoryId, Is.EqualTo("125"));
        Assert.That(decision.Candidates.Single(c => c.FactoryId == "110").EffectiveSharePct, Is.EqualTo(16.67m));
        Assert.That(decision.Candidates.Single(c => c.FactoryId == "125").EffectiveSharePct, Is.EqualTo(83.33m));
    }

    [Test]
    public void A_large_cost_advantage_beats_the_share_adjustment()
    {
        var decision = DecideSingle(Input(
            [Item("I1", Quote("110", 30m, sharePct: 10m), Quote("125", 45m, sharePct: 90m))],
            [Route("110", 5m), Route("125", 5m)]));

        Assert.That(decision.Winner.FactoryId, Is.EqualTo("110"));
    }

    [Test]
    public void Reserved_capacity_is_not_available_to_regular_orders()
    {
        var decision = DecideSingle(Input(
            [Item("I1", Quote("110", 30m, capacityPct: 40), Quote("125", 45m))],
            [Route("110", 5m), Route("125", 5m)],
            reserved: new Dictionary<string, int> { ["110"] = 40 }));

        Assert.That(decision.Winner.FactoryId, Is.EqualTo("125"));
        Assert.That(decision.Candidates.Single(c => c.FactoryId == "110").ExcludedReason, Is.EqualTo(ExclusionReasons.CapacityReserved));
    }

    [Test]
    public void Reserved_capacity_is_used_by_event_orders()
    {
        var decision = DecideSingle(Input(
            [Item("I1", Quote("110", 30m, capacityPct: 40), Quote("125", 45m))],
            [Route("110", 5m), Route("125", 5m)],
            isEventOrder: true,
            reserved: new Dictionary<string, int> { ["110"] = 40 }));

        Assert.That(decision.Winner.FactoryId, Is.EqualTo("110"));
    }

    [Test]
    public void A_partial_reservation_only_lowers_the_capacity_of_regular_orders()
    {
        var decision = DecideSingle(Input(
            [Item("I1", Quote("110", 30m, capacityPct: 100), Quote("125", 45m))],
            [Route("110", 5m), Route("125", 5m)],
            reserved: new Dictionary<string, int> { ["110"] = 60 }));

        Assert.That(decision.Winner.FactoryId, Is.EqualTo("110"));
        Assert.That(decision.Winner.CapacityPct, Is.EqualTo(40));
    }

    [Test]
    public void Items_of_one_order_go_to_the_same_factory_when_it_can_build_them_all()
    {
        var decision = engine.Decide(Input(
            [
                Item("I1", Quote("110", 40m), Quote("125", 44m)),
                Item("I2", Quote("110", 50m), Quote("125", 41m))
            ],
            [Route("110", 5m), Route("125", 5m)]));

        Assert.That(decision.Items.Select(i => i.Winner.FactoryId).Distinct().ToList(), Is.EqualTo(new[] { "125" }));
        Assert.That(decision.Items.All(i => !i.IsSplit), Is.True);
        Assert.That(decision.Items.First(i => i.OrderItemId == "I1").Reason, Does.StartWith("Keeps the order in one factory"));
        Assert.That(decision.Items.First(i => i.OrderItemId == "I1").CheapestFeasibleTotalCost, Is.EqualTo(45m));
    }

    [Test]
    public void An_order_is_never_split_while_one_factory_can_build_all_items()
    {
        var decision = engine.Decide(Input(
            [
                Item("I1", Quote("110", 10m), Quote("125", 90m)),
                Item("I2", Quote("110", 90m), Quote("125", 10m))
            ],
            [Route("110", 5m), Route("125", 5m)]));

        Assert.That(decision.Items.Select(i => i.Winner.FactoryId).Distinct().Count(), Is.EqualTo(1));
    }

    [Test]
    public void An_order_is_split_only_when_no_single_factory_can_build_it_and_the_split_is_flagged()
    {
        var decision = engine.Decide(Input(
            [
                Item("I1", Quote("110", 40m), NotProducible("125", "NOT_PRODUCIBLE")),
                Item("I2", NotProducible("110", "COMPONENT_OOS"), Quote("125", 41m))
            ],
            [Route("110", 5m), Route("125", 5m)]));

        Assert.That(decision.Items.First(i => i.OrderItemId == "I1").Winner.FactoryId, Is.EqualTo("110"));
        Assert.That(decision.Items.First(i => i.OrderItemId == "I2").Winner.FactoryId, Is.EqualTo("125"));
        Assert.That(decision.Items.All(i => i.IsSplit), Is.True);
    }

    [Test]
    public void A_forced_split_uses_as_few_factories_as_the_split_penalty_justifies()
    {
        var decision = engine.Decide(Input(
            [
                Item("I1", Quote("110", 40m), NotProducible("125"), Quote("315", 41m)),
                Item("I2", NotProducible("110"), Quote("125", 40m), Quote("315", 41m)),
                Item("I3", Quote("110", 40m), Quote("125", 40m), NotProducible("315"))
            ],
            [Route("110", 5m), Route("125", 5m), Route("315", 5m)]));

        Assert.That(decision.Items.Select(i => i.Winner.FactoryId).Distinct().Count(), Is.EqualTo(2));
    }

    [Test]
    public void A_later_item_follows_the_factory_that_already_has_the_order()
    {
        var decision = DecideSingle(Input(
            [Item("I2", Quote("110", 44m), Quote("125", 45m))],
            [Route("110", 5m), Route("125", 5m)],
            anchors: ["125"]));

        Assert.That(decision.Winner.FactoryId, Is.EqualTo("125"));
        Assert.That(decision.IsSplit, Is.False);
        Assert.That(decision.Reason, Does.StartWith("Keeps the order with the factory that already has its other items"));
    }

    [Test]
    public void A_later_item_is_split_from_its_order_when_the_anchor_factory_cannot_build_it()
    {
        var decision = DecideSingle(Input(
            [Item("I2", Quote("110", 44m), NotProducible("125", "COMPONENT_OOS"))],
            [Route("110", 5m), Route("125", 5m)],
            anchors: ["125"]));

        Assert.That(decision.Winner.FactoryId, Is.EqualTo("110"));
        Assert.That(decision.IsSplit, Is.True);
    }

    [Test]
    public void Success_rate_is_ignored_while_the_factor_is_switched_off()
    {
        var decision = DecideSingle(Input(
            [Item("I1", Quote("110", 44m), Quote("125", 45m))],
            [Route("110", 5m), Route("125", 5m)],
            successRates: new Dictionary<string, decimal> { ["110"] = 0.5m, ["125"] = 1m }));

        Assert.That(decision.Winner.FactoryId, Is.EqualTo("110"));
    }

    [Test]
    public void Success_rate_drives_routing_when_switched_on_with_a_weight()
    {
        var decision = DecideSingle(Input(
            [Item("I1", Quote("110", 44m), Quote("125", 45m))],
            [Route("110", 5m), Route("125", 5m)],
            successRates: new Dictionary<string, decimal> { ["110"] = 0.5m, ["125"] = 1m },
            settings: TestData.Settings with { SuccessRateEnabled = true, SuccessRateWeight = 0.5m }));

        Assert.That(decision.Winner.FactoryId, Is.EqualTo("125"));
    }

    [Test]
    public void Falls_back_to_the_default_share_when_quotes_are_unavailable()
    {
        var decision = DecideSingle(Input(
            [new DecisionItem("I1", null)],
            [Route("110", 5m)],
            fallbackShare: new Dictionary<string, decimal> { ["110"] = 30m, ["125"] = 70m }));

        Assert.That(decision.IsFallback, Is.True);
        Assert.That(decision.FallbackReason, Is.EqualTo(FallbackReasons.QuotesUnavailable));
        Assert.That(decision.Winner.FactoryId, Is.EqualTo("125"));
        Assert.That(decision.Winner.TotalCost, Is.Null);
        Assert.That(decision.Reason, Does.Contain("Fallback by default factory share"));
    }

    [Test]
    public void Fallback_spreads_volume_by_the_share_deficit()
    {
        var decision = DecideSingle(Input(
            [new DecisionItem("I1", null)],
            [],
            fallbackShare: new Dictionary<string, decimal> { ["110"] = 30m, ["125"] = 70m },
            actualShare: new Dictionary<string, decimal> { ["110"] = 10m, ["125"] = 90m }));

        Assert.That(decision.Winner.FactoryId, Is.EqualTo("110"));
    }

    [Test]
    public void Falls_back_to_the_quoted_share_when_routes_are_unavailable()
    {
        var decision = DecideSingle(Input(
            [Item("I1", Quote("110", 45m, sharePct: 80m), Quote("125", 40m, sharePct: 20m), NotProducible("315", "NOT_PRODUCIBLE"))],
            [],
            fallbackShare: new Dictionary<string, decimal> { ["315"] = 100m }));

        Assert.That(decision.FallbackReason, Is.EqualTo(FallbackReasons.RoutesUnavailable));
        Assert.That(decision.Winner.FactoryId, Is.EqualTo("110"));
        Assert.That(decision.Winner.ProductionCost, Is.EqualTo(45m));
        Assert.That(decision.Winner.QuoteId, Is.EqualTo("PQ-O1-I1-110"));
    }

    [Test]
    public void No_item_is_left_unrouted_when_no_factory_is_feasible()
    {
        var decision = DecideSingle(Input(
            [Item("I1", NotProducible("110", "COMPONENT_OOS"), NotProducible("125", "PROCESS_OFFLINE"))],
            [Route("110", 5m), Route("125", 5m)],
            fallbackShare: new Dictionary<string, decimal> { ["110"] = 60m, ["125"] = 40m }));

        Assert.That(decision.FallbackReason, Is.EqualTo(FallbackReasons.NoFeasibleFactory));
        Assert.That(decision.Winner.FactoryId, Is.EqualTo("110"));
        Assert.That(decision.Candidates, Has.Count.EqualTo(2));
    }

    [Test]
    public void A_fallback_item_does_not_block_the_routing_of_the_other_items()
    {
        var decision = engine.Decide(Input(
            [new DecisionItem("I1", null), Item("I2", Quote("110", 45m), Quote("125", 50m))],
            [Route("110", 5m), Route("125", 5m)],
            fallbackShare: new Dictionary<string, decimal> { ["125"] = 100m }));

        Assert.That(decision.Items.First(i => i.OrderItemId == "I1").IsFallback, Is.True);
        Assert.That(decision.Items.First(i => i.OrderItemId == "I2").IsFallback, Is.False);
        Assert.That(decision.Items.First(i => i.OrderItemId == "I2").Winner.FactoryId, Is.EqualTo("110"));
    }

    [Test]
    public void Throws_when_nothing_can_be_routed_and_no_fallback_share_exists()
    {
        Assert.Throws<InvalidOperationException>(() => engine.Decide(Input([new DecisionItem("I1", null)], [])));
    }

    [Test]
    public void The_same_input_always_gives_the_same_decision()
    {
        DecisionInput Build() => Input(
            [
                Item("I1", Quote("315", 45m), Quote("110", 45m), Quote("125", 45m)),
                Item("I2", Quote("125", 45m), Quote("315", 45m), Quote("110", 45m))
            ],
            [Route("125", 5m), Route("110", 5m), Route("315", 5m)],
            promised: Today.AddDays(10));

        var first = engine.Decide(Build()).ToJsonString();
        var second = engine.Decide(Build()).ToJsonString();

        Assert.That(second, Is.EqualTo(first));
        Assert.That(engine.Decide(Build()).Items.Select(i => i.Winner.FactoryId).Distinct().Single(), Is.EqualTo("110"));
    }

    [Test]
    public void Large_orders_are_decided_without_enumerating_every_combination()
    {
        var items = Enumerable.Range(1, 12)
            .Select(i => i % 2 == 0
                ? Item($"I{i}", Quote("110", 40m), Quote("125", 41m), Quote("315", 42m), NotProducible("400"))
                : Item($"I{i}", NotProducible("110"), Quote("125", 41m), Quote("315", 42m), Quote("400", 39m)))
            .Append(Item("I13", Quote("110", 40m), NotProducible("125"), NotProducible("315"), NotProducible("400")))
            .Append(Item("I14", NotProducible("110"), NotProducible("125"), NotProducible("315"), Quote("400", 40m)))
            .ToList();

        var decision = engine.Decide(Input(items, [Route("110", 5m), Route("125", 5m), Route("315", 5m), Route("400", 5m)]));

        Assert.That(decision.Items, Has.Count.EqualTo(14));
        Assert.That(decision.Items.All(i => !i.IsFallback && i.Winner.ExcludedReason == null), Is.True);
        Assert.That(decision.Items.Select(i => i.Winner.FactoryId).Distinct().Count(), Is.EqualTo(2));
    }
}
