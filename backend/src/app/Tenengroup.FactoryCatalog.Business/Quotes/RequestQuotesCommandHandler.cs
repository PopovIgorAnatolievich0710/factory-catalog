using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Quotes;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Processes;
using Tenengroup.FactoryCatalog.Domains.Quotes;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Json;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Quotes;

public class RequestQuotesCommandHandler(
    IFactoryCatalogDbContext db,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    QuotesOptions options) : IRequestHandler<RequestQuotesCommand, QuoteSetDto>
{
    private const int MaxOrderKeyLength = 90;
    private const int OkStockSignalPct = 100;
    private const int LowStockSignalPct = 50;
    private const int FullSignalPct = 100;
    private const int CostDecimals = 4;

    public async Task<QuoteSetDto> Handle(RequestQuotesCommand request, CancellationToken cancellationToken)
    {
        ServiceAccountAccess.Ensure(currentUser);

        var isStored = request.Mode != QuoteMode.Estimate;
        Validate(request, isStored);

        if (isStored)
        {
            var existing = await FindStored(request, cancellationToken);
            if (existing != null)
            {
                return existing;
            }
        }

        var sku = await SkuLookup.Find(db, request.Sku, cancellationToken)
                  ?? throw new EntityNotFoundException("SKU", request.Sku);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var cbd = request.Cbd
            .GroupBy(c => c.ErpId.Trim())
            .Select(g => new CbdItem(g.Key, g.Sum(c => c.Quantity)))
            .ToList();

        var set = new QuoteSet
        {
            QuoteKey = isStored ? await NextQuoteKey(request, cancellationToken) : $"EST-{request.RequestId:N}",
            RequestId = request.RequestId,
            OrderId = request.OrderId?.Trim(),
            OrderItemId = request.OrderItemId?.Trim(),
            SkuUuid = sku.SkuUuid,
            Quantity = request.Quantity,
            Mode = request.Mode,
            CbdJson = cbd.ToJsonString(),
            ValidUntil = now.AddMinutes(options.ValidityMinutes),
            CreatedAt = now
        };

        var quotes = await BuildQuotes(set, sku, cbd, now, cancellationToken);

        if (isStored)
        {
            db.QuoteSets.Add(set);
            foreach (var quote in quotes)
            {
                quote.QuoteSet = set;
                db.Quotes.Add(quote);
            }

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                db.DiscardChanges();

                var storedByConcurrentRequest = await FindStored(request, cancellationToken);
                if (storedByConcurrentRequest == null)
                {
                    throw;
                }

                return storedByConcurrentRequest;
            }
        }

        return QuoteMapper.ToSetDto(set, quotes, new Dictionary<long, string>());
    }

    private static void Validate(RequestQuotesCommand request, bool isStored)
    {
        var errors = new Dictionary<string, List<string>>();

        if (request.RequestId == Guid.Empty)
        {
            errors["requestId"] = ["requestId is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Sku))
        {
            errors["skuUuid"] = ["skuUuid (the SKU UUID or a legacy alias) is required."];
        }

        if (request.Quantity < 1)
        {
            errors["quantity"] = ["quantity must be at least 1."];
        }

        if (request.Cbd == null)
        {
            errors["cbd"] = ["cbd is required."];
        }
        else if (request.Cbd.Any(c => string.IsNullOrWhiteSpace(c.ErpId) || c.Quantity <= 0))
        {
            errors["cbd"] = ["Every cbd entry needs an erpId and a quantity above 0."];
        }

        ValidateOrderKey(errors, "orderId", request.OrderId, isStored);
        ValidateOrderKey(errors, "orderItemId", request.OrderItemId, isStored);

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static void ValidateOrderKey(Dictionary<string, List<string>> errors, string field, string value, bool isStored)
    {
        if (isStored && string.IsNullOrWhiteSpace(value))
        {
            errors[field] = [$"{field} is required for live and shadow quotes."];
        }
        else if (value?.Trim().Length > MaxOrderKeyLength)
        {
            errors[field] = [$"{field} must be at most {MaxOrderKeyLength} characters."];
        }
    }

    private async Task<QuoteSetDto> FindStored(RequestQuotesCommand request, CancellationToken cancellationToken)
    {
        var set = await db.QuoteSets
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.RequestId == request.RequestId, cancellationToken);

        if (set == null)
        {
            return null;
        }

        if (set.OrderId != request.OrderId.Trim() || set.OrderItemId != request.OrderItemId.Trim())
        {
            throw new ConflictException(
                $"requestId '{request.RequestId}' was already used for order item '{set.OrderId}' / '{set.OrderItemId}'.");
        }

        var quotes = await db.Quotes
            .AsNoTracking()
            .Where(q => q.QuoteSetId == set.Id)
            .OrderBy(q => q.Id)
            .ToListAsync(cancellationToken);

        return QuoteMapper.ToSetDto(set, quotes, await QuoteMapper.LoadOriginalQuoteIds(db, quotes, cancellationToken));
    }

    private async Task<string> NextQuoteKey(RequestQuotesCommand request, CancellationToken cancellationToken)
    {
        var baseKey = $"PQ-{request.OrderId.Trim()}-{request.OrderItemId.Trim()}";
        var quoteKey = baseKey;
        var revision = 1;

        while (await db.QuoteSets.AnyAsync(s => s.QuoteKey == quoteKey, cancellationToken))
        {
            revision++;
            quoteKey = $"{baseKey}-R{revision}";
        }

        return quoteKey;
    }

    private async Task<List<Quote>> BuildQuotes(
        QuoteSet set,
        Sku sku,
        IReadOnlyList<CbdItem> cbd,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var factoryIds = await db.Factories
            .AsNoTracking()
            .Where(f => f.IsActive)
            .OrderBy(f => f.Id)
            .Select(f => f.Id)
            .ToListAsync(cancellationToken);

        if (sku.ProductType == SkuProductType.Dropshipper)
        {
            return factoryIds
                .Select(factoryId => NewQuote(set, factoryId, now, [QuoteReasonCodes.Dropshipper], [], []))
                .ToList();
        }

        var stock = await ComponentStock.Load(db, cbd.Select(c => c.ErpId).ToList(), cancellationToken);

        var skuProcesses = await db.SkuProcesses
            .AsNoTracking()
            .Where(p => p.SkuUuid == sku.SkuUuid)
            .OrderBy(p => p.ProcessId)
            .ToListAsync(cancellationToken);
        var processIds = skuProcesses.Select(p => p.ProcessId).ToList();

        var processFactories = (await db.ProcessFactories
                .AsNoTracking()
                .Where(p => processIds.Contains(p.ProcessId))
                .ToListAsync(cancellationToken))
            .ToDictionary(p => (p.ProcessId, p.FactoryId));

        var usedToday = await LoadUsedToday(processIds, now, cancellationToken);

        var skuFactories = (await db.SkuFactories
                .AsNoTracking()
                .Where(f => f.SkuUuid == sku.SkuUuid)
                .ToListAsync(cancellationToken))
            .ToDictionary(f => f.FactoryId);

        var globalShares = (await db.GlobalFactoryShares.AsNoTracking().ToListAsync(cancellationToken))
            .ToDictionary(s => s.FactoryId, s => s.Pct);

        var setReasons = new List<string>();
        if (sku.ValidationStatus == SkuValidationStatus.Invalid)
        {
            setReasons.Add(QuoteReasonCodes.SkuInvalid);
        }

        if (cbd.Any(c => !stock.IsKnown(c.ErpId)))
        {
            setReasons.Add(QuoteReasonCodes.UnknownComponent);
        }

        var quotes = new List<Quote>();

        foreach (var factoryId in factoryIds)
        {
            var reasons = new List<string>(setReasons);
            var signals = new List<int>();

            if (skuFactories.GetValueOrDefault(factoryId) is not { Producible: true })
            {
                reasons.Add(QuoteReasonCodes.NotProducible);
            }

            var components = cbd
                .Select(item => BuildComponentLine(item, set.Quantity, factoryId, stock, reasons, signals))
                .ToList();

            var processes = skuProcesses
                .Select(process => BuildProcessLine(
                    process,
                    set.Quantity,
                    processFactories.GetValueOrDefault((process.ProcessId, factoryId)),
                    usedToday.GetValueOrDefault((process.ProcessId, factoryId)),
                    reasons,
                    signals))
                .ToList();

            var quote = NewQuote(set, factoryId, now, reasons.Distinct().ToList(), components, processes);

            if (quote.Producible)
            {
                quote.MaterialCost = Math.Round(components.Sum(c => c.Cost ?? 0), CostDecimals);
                quote.LaborCost = Math.Round(
                    processes.Where(p => p.ProcessId != Process.PackingProcessId).Sum(p => p.LaborCost ?? 0), CostDecimals);
                quote.PackingCost = Math.Round(
                    processes.Where(p => p.ProcessId == Process.PackingProcessId).Sum(p => p.LaborCost ?? 0), CostDecimals);
                quote.TotalCost = quote.MaterialCost + quote.LaborCost + quote.PackingCost;
                quote.Currency = options.Currency;
                quote.CapacitySignalPct = signals.Count == 0 ? FullSignalPct : signals.Min();
            }

            quotes.Add(quote);
        }

        var shares = ShareRedistribution.Redistribute(quotes
            .Where(q => q.Producible)
            .ToDictionary(
                q => q.FactoryId,
                q => skuFactories[q.FactoryId].OverrideSharePct ?? globalShares.GetValueOrDefault(q.FactoryId)));

        foreach (var quote in quotes.Where(q => q.Producible))
        {
            quote.SharePct = shares[quote.FactoryId];
        }

        return quotes;
    }

    private static QuoteComponentDto BuildComponentLine(
        CbdItem item,
        int itemQuantity,
        string factoryId,
        ComponentStock stock,
        List<string> reasons,
        List<int> signals)
    {
        var quantity = item.Quantity * itemQuantity;

        if (!stock.IsKnown(item.ErpId))
        {
            return new QuoteComponentDto(item.ErpId, quantity, null, StockStatuses.Unknown, null, null, null);
        }

        var status = stock.Status(item.ErpId, factoryId);
        var usedErpId = item.ErpId;
        string alternativeUsed = null;

        if (status == StockStatuses.OutOfStock)
        {
            alternativeUsed = stock.FindAlternativeInStock(item.ErpId, factoryId);

            if (alternativeUsed == null)
            {
                reasons.Add(QuoteReasonCodes.ComponentOos);
            }
            else
            {
                usedErpId = alternativeUsed;
            }
        }

        signals.Add(StockSignalPct(stock.Status(usedErpId, factoryId)));

        var used = stock.Get(usedErpId);

        return new QuoteComponentDto(
            item.ErpId,
            quantity,
            used.UnitCostAmount,
            status,
            alternativeUsed,
            used.UnitCostAmount == null ? null : Math.Round(used.UnitCostAmount.Value * quantity, CostDecimals),
            used.UnitCostAmount == null ? null : used.UnitCostCurrency);
    }

    private static QuoteProcessDto BuildProcessLine(
        SkuProcess process,
        int itemQuantity,
        ProcessFactory processFactory,
        int usedToday,
        List<string> reasons,
        List<int> signals)
    {
        var quantity = process.Quantity * itemQuantity;

        if (processFactory is not { RunsHere: true })
        {
            reasons.Add(QuoteReasonCodes.ProcessNotAvailable);

            return new QuoteProcessDto(process.ProcessId, quantity, null, null, null, null);
        }

        if (processFactory.CapacityPct == 0)
        {
            reasons.Add(QuoteReasonCodes.ProcessOffline);
        }

        var remainingPct = RemainingPct(processFactory, usedToday);
        signals.Add(remainingPct);

        return new QuoteProcessDto(
            process.ProcessId,
            quantity,
            processFactory.CapacityPct,
            processFactory.LaborCostAmount == null ? null : Math.Round(processFactory.LaborCostAmount.Value * quantity, CostDecimals),
            remainingPct,
            processFactory.LaborCostAmount == null ? null : processFactory.LaborCostCurrency);
    }

    private static int StockSignalPct(string stockStatus)
    {
        return stockStatus switch
        {
            StockStatuses.Ok => OkStockSignalPct,
            StockStatuses.Low => LowStockSignalPct,
            _ => 0
        };
    }

    private static int RemainingPct(ProcessFactory processFactory, int usedToday)
    {
        if (processFactory.MaxPerDay is not > 0)
        {
            return processFactory.CapacityPct;
        }

        var maxPerDay = processFactory.MaxPerDay.Value;
        var remaining = (maxPerDay * processFactory.CapacityPct / ProcessFactory.FullCapacityPct - usedToday) / maxPerDay * 100;

        return (int)Math.Clamp(Math.Floor(remaining), 0, 100);
    }

    private async Task<Dictionary<(string ProcessId, string FactoryId), int>> LoadUsedToday(
        List<string> processIds,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (processIds.Count == 0)
        {
            return new Dictionary<(string ProcessId, string FactoryId), int>();
        }

        var dayStart = now.Date;
        var dayEnd = dayStart.AddDays(1);

        var used = await db.Quotes
            .AsNoTracking()
            .Where(q => q.Status == QuoteStatus.Produced && q.ProducedAt >= dayStart && q.ProducedAt < dayEnd)
            .Join(db.QuoteSets, q => q.QuoteSetId, s => s.Id, (q, s) => new { q.FactoryId, s.SkuUuid })
            .Join(
                db.SkuProcesses.Where(p => processIds.Contains(p.ProcessId)),
                produced => produced.SkuUuid,
                p => p.SkuUuid,
                (produced, p) => new { produced.FactoryId, p.ProcessId })
            .GroupBy(x => new { x.FactoryId, x.ProcessId })
            .Select(g => new { g.Key.FactoryId, g.Key.ProcessId, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return used.ToDictionary(x => (x.ProcessId, x.FactoryId), x => x.Count);
    }

    private static Quote NewQuote(
        QuoteSet set,
        string factoryId,
        DateTime now,
        List<string> reasons,
        List<QuoteComponentDto> components,
        List<QuoteProcessDto> processes)
    {
        return new Quote
        {
            QuoteId = $"{set.QuoteKey}-{factoryId}",
            FactoryId = factoryId,
            Producible = reasons.Count == 0,
            Reasons = reasons,
            ComponentsJson = components.ToJsonString(),
            ProcessesJson = processes.ToJsonString(),
            Status = QuoteStatus.Quoted,
            CreatedAt = now
        };
    }
}
