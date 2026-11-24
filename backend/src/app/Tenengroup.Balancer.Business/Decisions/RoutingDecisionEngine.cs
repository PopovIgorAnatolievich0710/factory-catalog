using Tenengroup.Balancer.Business.Abstractions.Catalog;
using Tenengroup.Balancer.Business.Abstractions.Decisions;

namespace Tenengroup.Balancer.Business.Decisions;

public class RoutingDecisionEngine : IRoutingDecisionEngine
{
    internal const int MaxCombinations = 50_000;

    public OrderDecision Decide(DecisionInput input)
    {
        var evaluated = input.Items.Select(item => Evaluate(item, input)).ToList();
        var routable = evaluated.Where(e => e.Feasible.Count > 0).ToList();
        var assignment = Assign(routable, input);

        var winners = evaluated.ToDictionary(
            e => e.OrderItemId,
            e => assignment.TryGetValue(e.OrderItemId, out var factoryId) ? factoryId : PickFallbackFactory(e, input));

        var isSplit = winners.Values.Concat(input.AnchorFactoryIds).Distinct().Count() > 1;
        var keptTogether = routable.Count > 1 && assignment.Values.Distinct().Count() == 1;

        return new OrderDecision(evaluated
            .Select(e => assignment.ContainsKey(e.OrderItemId)
                ? BuildRouted(e, winners[e.OrderItemId], isSplit, keptTogether, input)
                : BuildFallback(e, winners[e.OrderItemId], isSplit))
            .ToList());
    }

    private static EvaluatedItem Evaluate(DecisionItem item, DecisionInput input)
    {
        if (item.QuoteSet == null)
        {
            return new EvaluatedItem(item.OrderItemId, [], FallbackReasons.QuotesUnavailable, new Dictionary<string, decimal>());
        }

        var drafts = item.QuoteSet.Quotes.Select(quote => Draft(quote, input)).ToList();
        var feasible = drafts.Where(d => d.ExcludedReason == null).ToList();
        var weightSum = feasible.Sum(d => d.Quote.SharePct * d.EffectiveCapacityPct / 100m);

        var candidates = drafts
            .Select(d => d.ExcludedReason == null
                ? Score(d, weightSum, feasible.Count, input)
                : Excluded(d))
            .ToList();

        var producibleShares = item.QuoteSet.Quotes
            .Where(q => q.Producible && q.SharePct > 0)
            .ToDictionary(q => q.FactoryId, q => q.SharePct);

        var fallbackReason = feasible.Count > 0
            ? null
            : input.Routes.Count == 0 && producibleShares.Count > 0
                ? FallbackReasons.RoutesUnavailable
                : FallbackReasons.NoFeasibleFactory;

        return new EvaluatedItem(item.OrderItemId, candidates, fallbackReason, producibleShares);
    }

    private static CandidateDraft Draft(FactoryQuote quote, DecisionInput input)
    {
        var route = input.Routes.FirstOrDefault(r => r.OriginFacilityId == quote.FactoryId);
        var capacity = quote.CapacitySignalPct ?? 100;
        var reserved = input.IsEventOrder ? 0 : input.ReservedCapacityPct.GetValueOrDefault(quote.FactoryId);
        var effectiveCapacity = capacity - reserved;

        string excludedReason = null;
        if (!quote.Producible || quote.Cost == null)
        {
            excludedReason = quote.Reasons is { Count: > 0 }
                ? string.Join(",", quote.Reasons)
                : ExclusionReasons.NotProducible;
        }
        else if (capacity <= 0)
        {
            excludedReason = ExclusionReasons.NoCapacity;
        }
        else if (effectiveCapacity <= 0)
        {
            excludedReason = ExclusionReasons.CapacityReserved;
        }
        else if (route == null)
        {
            excludedReason = ExclusionReasons.NoRoute;
        }

        return new CandidateDraft(quote, route, effectiveCapacity, excludedReason);
    }

    private static Candidate Excluded(CandidateDraft draft)
    {
        return new Candidate(
            draft.Quote.FactoryId,
            draft.Quote.QuoteId,
            draft.Quote.Cost?.Total,
            draft.Route?.CostAmount,
            null,
            draft.Quote.Cost?.Currency,
            null,
            null,
            draft.Route?.TransitDaysMax,
            draft.Quote.SharePct,
            draft.Quote.CapacitySignalPct,
            null,
            null,
            null,
            null,
            draft.ExcludedReason,
            false);
    }

    private static Candidate Score(CandidateDraft draft, decimal weightSum, int feasibleCount, DecisionInput input)
    {
        var settings = input.Settings;
        var factoryId = draft.Quote.FactoryId;
        var effectiveShare = weightSum > 0
            ? draft.Quote.SharePct * draft.EffectiveCapacityPct / 100m / weightSum * 100m
            : 100m / feasibleCount;

        var productionDays = input.ProductionDays.GetValueOrDefault(factoryId, settings.DefaultProductionDays);
        var etaFrom = input.Today.AddDays(productionDays + draft.Route.TransitDaysMin);
        var etaTo = input.Today.AddDays(productionDays + draft.Route.TransitDaysMax);
        var total = draft.Quote.Cost.Total + draft.Route.CostAmount;

        var daysLate = input.PromisedDeliveryDate.HasValue
            ? Math.Max(0, etaTo.DayNumber - input.PromisedDeliveryDate.Value.DayNumber)
            : 0;
        var shareDeficit = effectiveShare - input.ActualSharePct.GetValueOrDefault(factoryId);
        var successPenalty = settings.SuccessRateEnabled
            ? settings.SuccessRateWeight * (1m - input.SuccessRates.GetValueOrDefault(factoryId, 1m)) * total
            : 0m;
        var score = total * (1m - settings.ShareWeight * shareDeficit / 100m)
                    + settings.LatePenaltyPerDay * daysLate
                    + successPenalty;

        return new Candidate(
            factoryId,
            draft.Quote.QuoteId,
            draft.Quote.Cost.Total,
            draft.Route.CostAmount,
            total,
            draft.Quote.Cost.Currency,
            etaFrom,
            etaTo,
            draft.Route.TransitDaysMax,
            draft.Quote.SharePct,
            draft.EffectiveCapacityPct,
            Math.Round(effectiveShare, 2),
            Math.Round(score, 4),
            input.PromisedDeliveryDate.HasValue ? etaTo <= input.PromisedDeliveryDate.Value : null,
            input.SlaDeliveryDate.HasValue ? etaTo <= input.SlaDeliveryDate.Value : null,
            null,
            false);
    }

    private static Dictionary<string, string> Assign(List<EvaluatedItem> routable, DecisionInput input)
    {
        if (routable.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        var common = routable
            .Select(e => e.Feasible.Select(c => c.FactoryId))
            .Aggregate((left, right) => left.Intersect(right))
            .ToList();

        Ranking together = null;
        if (common.Count > 0)
        {
            together = common
                .Select(factoryId => Rank(routable.Select(e => e.FeasibleFor(factoryId)).ToList(), input))
                .Aggregate((left, right) => right.IsBetterThan(left) ? right : left);

            if (together.SlaMisses == 0)
            {
                return routable.ToDictionary(e => e.OrderItemId, _ => together.Candidates[0].FactoryId);
            }
        }

        var combinations = routable.Aggregate(1L, (count, e) => Math.Min(count * e.Feasible.Count, MaxCombinations + 1L));
        var chosen = combinations <= MaxCombinations ? BestCombination(routable, input) : Greedy(routable, input);

        if (together != null && Rank(chosen, input).SlaMisses >= together.SlaMisses)
        {
            chosen = together.Candidates;
        }

        return routable
            .Select((e, index) => (e.OrderItemId, chosen[index].FactoryId))
            .ToDictionary(x => x.OrderItemId, x => x.FactoryId);
    }

    private static List<Candidate> BestCombination(List<EvaluatedItem> routable, DecisionInput input)
    {
        Ranking best = null;
        var current = new Candidate[routable.Count];

        void Visit(int index)
        {
            if (index == routable.Count)
            {
                var ranking = Rank(current.ToList(), input);
                if (best == null || ranking.IsBetterThan(best))
                {
                    best = ranking;
                }

                return;
            }

            foreach (var candidate in routable[index].Feasible)
            {
                current[index] = candidate;
                Visit(index + 1);
            }
        }

        Visit(0);
        return best.Candidates;
    }

    private static List<Candidate> Greedy(List<EvaluatedItem> routable, DecisionInput input)
    {
        var chosen = new List<Candidate>();

        foreach (var item in routable)
        {
            var best = item.Feasible
                .Select(candidate => Rank(chosen.Append(candidate).ToList(), input))
                .Aggregate((left, right) => right.IsBetterThan(left) ? right : left);
            chosen = best.Candidates;
        }

        return chosen;
    }

    private static Ranking Rank(List<Candidate> candidates, DecisionInput input)
    {
        var factories = candidates.Select(c => c.FactoryId).Concat(input.AnchorFactoryIds).Distinct().Count();

        return new Ranking(
            candidates,
            candidates.Count(c => c.MeetsSla == false),
            candidates.Sum(c => c.Score.Value) + input.Settings.SplitPenalty * (factories - 1),
            candidates.Sum(c => c.EffectiveSharePct.Value - input.ActualSharePct.GetValueOrDefault(c.FactoryId)),
            string.Join("|", candidates.Select(c => c.FactoryId)));
    }

    private static string PickFallbackFactory(EvaluatedItem item, DecisionInput input)
    {
        var shares = item.ProducibleShares.Count > 0 ? item.ProducibleShares : input.FallbackSharePct;
        if (shares.Count == 0)
        {
            throw new InvalidOperationException(
                $"Order item '{item.OrderItemId}' cannot be routed: no producible quote and no fallback factory share is configured.");
        }

        return shares
            .OrderByDescending(s => s.Value - input.ActualSharePct.GetValueOrDefault(s.Key))
            .ThenBy(s => s.Key, StringComparer.Ordinal)
            .First().Key;
    }

    private static ItemDecision BuildRouted(EvaluatedItem item, string factoryId, bool isSplit, bool keptTogether, DecisionInput input)
    {
        var candidates = item.Candidates.Select(c => c with { IsWinner = c.FactoryId == factoryId }).ToList();
        var winner = candidates.First(c => c.IsWinner);
        var cheapest = item.Feasible.OrderBy(c => c.TotalCost).ThenBy(c => c.FactoryId, StringComparer.Ordinal).First();

        return new ItemDecision(
            item.OrderItemId,
            winner,
            candidates,
            null,
            cheapest.TotalCost,
            isSplit,
            Explain(winner, cheapest, item.Feasible.Count, keptTogether, input));
    }

    private static string Explain(Candidate winner, Candidate cheapest, int feasibleCount, bool keptTogether, DecisionInput input)
    {
        if (feasibleCount == 1)
        {
            return "Only feasible factory";
        }

        if (winner.TotalCost == cheapest.TotalCost)
        {
            return winner.MeetsPromise switch
            {
                true => "Lowest total cost that meets the delivery promise",
                false => "Lowest total cost; the delivery promise is missed",
                _ => "Lowest total cost"
            };
        }

        if (cheapest.MeetsSla == false && winner.MeetsSla == true)
        {
            return $"Meets the SLA; cheaper {cheapest.FactoryId} does not";
        }

        if (cheapest.MeetsPromise == false && winner.MeetsPromise == true)
        {
            return $"Lowest total cost that meets the delivery promise; cheaper {cheapest.FactoryId} misses it";
        }

        if (keptTogether)
        {
            return $"Keeps the order in one factory; {cheapest.FactoryId} is cheaper for this item only";
        }

        if (input.AnchorFactoryIds.Contains(winner.FactoryId))
        {
            return $"Keeps the order with the factory that already has its other items; {cheapest.FactoryId} is cheaper";
        }

        return $"Share, capacity and penalty adjustments outweigh the cost advantage of {cheapest.FactoryId}";
    }

    private static ItemDecision BuildFallback(EvaluatedItem item, string factoryId, bool isSplit)
    {
        var candidates = item.Candidates.Select(c => c with { IsWinner = c.FactoryId == factoryId }).ToList();
        var winner = candidates.FirstOrDefault(c => c.IsWinner);

        if (winner == null)
        {
            winner = new Candidate(factoryId, null, null, null, null, null, null, null, null, 0, null, null, null, null, null, null, true);
            candidates.Add(winner);
        }

        return new ItemDecision(
            item.OrderItemId,
            winner,
            candidates,
            item.FallbackReason,
            null,
            isSplit,
            $"Fallback by default factory share ({item.FallbackReason})");
    }

    private record CandidateDraft(FactoryQuote Quote, RouteOption Route, int EffectiveCapacityPct, string ExcludedReason);

    private record EvaluatedItem(
        string OrderItemId,
        List<Candidate> Candidates,
        string FallbackReason,
        Dictionary<string, decimal> ProducibleShares)
    {
        public List<Candidate> Feasible { get; } = Candidates.Where(c => c.ExcludedReason == null).ToList();

        public Candidate FeasibleFor(string factoryId) => Feasible.First(c => c.FactoryId == factoryId);
    }

    private record Ranking(List<Candidate> Candidates, int SlaMisses, decimal Score, decimal ShareDeficit, string Signature)
    {
        public bool IsBetterThan(Ranking other)
        {
            if (SlaMisses != other.SlaMisses)
            {
                return SlaMisses < other.SlaMisses;
            }

            if (Score != other.Score)
            {
                return Score < other.Score;
            }

            if (ShareDeficit != other.ShareDeficit)
            {
                return ShareDeficit > other.ShareDeficit;
            }

            return string.CompareOrdinal(Signature, other.Signature) < 0;
        }
    }
}
