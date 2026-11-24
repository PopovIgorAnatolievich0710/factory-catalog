using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Quotes;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Processes;
using Tenengroup.FactoryCatalog.Domains.Quotes;
using Tenengroup.Infrastructure.Common.Json;

namespace Tenengroup.FactoryCatalog.Business.Quotes;

internal static class QuoteMapper
{
    public static async Task<Dictionary<long, string>> LoadOriginalQuoteIds(
        IFactoryCatalogDbContext db,
        IEnumerable<Quote> quotes,
        CancellationToken cancellationToken)
    {
        var originalIds = quotes
            .Where(q => q.ReproductionOfQuoteId != null)
            .Select(q => q.ReproductionOfQuoteId.Value)
            .Distinct()
            .ToList();

        if (originalIds.Count == 0)
        {
            return new Dictionary<long, string>();
        }

        return await db.Quotes
            .AsNoTracking()
            .Where(q => originalIds.Contains(q.Id))
            .ToDictionaryAsync(q => q.Id, q => q.QuoteId, cancellationToken);
    }

    public static QuoteSetDto ToSetDto(
        QuoteSet set,
        IEnumerable<Quote> quotes,
        IReadOnlyDictionary<long, string> originalQuoteIds)
    {
        return new QuoteSetDto(
            set.QuoteKey,
            set.ValidUntil,
            quotes.Select(q => ToDto(q, originalQuoteIds)).ToList(),
            set.RequestId,
            set.OrderId,
            set.OrderItemId,
            set.SkuUuid,
            set.Quantity,
            set.Mode,
            set.CreatedAt);
    }

    public static QuoteDetailsDto ToDetailsDto(
        QuoteSet set,
        Quote quote,
        IReadOnlyDictionary<long, string> originalQuoteIds)
    {
        return new QuoteDetailsDto(
            set.QuoteKey,
            set.RequestId,
            set.OrderId,
            set.OrderItemId,
            set.SkuUuid,
            set.Quantity,
            set.Mode,
            set.ValidUntil,
            set.CreatedAt,
            ToDto(quote, originalQuoteIds));
    }

    private static QuoteDto ToDto(Quote quote, IReadOnlyDictionary<long, string> originalQuoteIds)
    {
        var components = quote.ComponentsJson.FromJsonString<List<QuoteComponentDto>>();
        var processes = quote.ProcessesJson.FromJsonString<List<QuoteProcessDto>>();

        return new QuoteDto(
            quote.QuoteId,
            quote.FactoryId,
            quote.Producible,
            quote.Reasons,
            quote.Producible
                ? new QuoteCostDto(
                    quote.MaterialCost ?? 0,
                    quote.LaborCost ?? 0,
                    quote.PackingCost ?? 0,
                    quote.TotalCost ?? 0,
                    quote.Currency)
                : null,
            quote.CapacitySignalPct,
            quote.SharePct,
            components,
            processes,
            quote.Status,
            quote.SelectedAt,
            quote.ProducedAt,
            quote.TransferredAt,
            quote.ReproductionOfQuoteId == null ? null : originalQuoteIds.GetValueOrDefault(quote.ReproductionOfQuoteId.Value),
            quote.ProductionLogJson?.FromJsonString<List<ProductionLogEntryDto>>(),
            quote.Producible ? Warnings(quote.Currency, components, processes) : []);
    }

    private static List<string> Warnings(
        string currency,
        IReadOnlyCollection<QuoteComponentDto> components,
        IReadOnlyCollection<QuoteProcessDto> processes)
    {
        var warnings = new List<string>();

        if (components.Any(c => c.UnitCost == null))
        {
            warnings.Add(QuoteWarningCodes.ComponentCostMissing);
        }

        if (processes.Any(p => p.LaborCost == null))
        {
            warnings.Add(QuoteWarningCodes.LaborCostMissing);
        }

        if (processes.All(p => p.ProcessId != Process.PackingProcessId))
        {
            warnings.Add(QuoteWarningCodes.PackingNotMapped);
        }

        if (components.Any(c => c.Currency != null && c.Currency != currency)
            || processes.Any(p => p.Currency != null && p.Currency != currency))
        {
            warnings.Add(QuoteWarningCodes.CurrencyMismatch);
        }

        return warnings;
    }
}
