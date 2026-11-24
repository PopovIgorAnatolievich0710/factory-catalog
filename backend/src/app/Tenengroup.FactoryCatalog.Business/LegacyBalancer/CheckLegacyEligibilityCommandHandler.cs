using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.LegacyBalancer;
using Tenengroup.FactoryCatalog.Business.Abstractions.Quotes;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.Business.Quotes;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.LegacyBalancer;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.LegacyBalancer;

public class CheckLegacyEligibilityCommandHandler(
    IFactoryCatalogDbContext db,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<CheckLegacyEligibilityCommand, LegacyEligibilityDto>
{
    private const int MaxOrderKeyLength = 100;
    private const string NoFactoryQualifiesMessage = "No candidate factory qualifies; keep the current allocation behaviour.";

    public async Task<LegacyEligibilityDto> Handle(CheckLegacyEligibilityCommand request, CancellationToken cancellationToken)
    {
        ServiceAccountAccess.Ensure(currentUser);

        var orderId = request.OrderId?.Trim();
        var orderItemId = string.IsNullOrWhiteSpace(request.OrderItemId) ? null : request.OrderItemId.Trim();
        var factoryIds = (request.FactoryIds ?? [])
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Select(f => f.Trim())
            .Distinct()
            .ToList();

        await Validate(orderId, orderItemId, factoryIds, cancellationToken);

        var sku = string.IsNullOrWhiteSpace(request.Sku) ? null : await SkuLookup.Find(db, request.Sku, cancellationToken);

        var countedComponents = (request.CbdErpIds ?? [])
            .Where(erpId => !string.IsNullOrWhiteSpace(erpId))
            .Select(erpId => erpId.Trim())
            .ToList();
        List<string> processIds = [];

        if (sku != null)
        {
            countedComponents.AddRange(await db.SkuComponents
                .AsNoTracking()
                .Where(c => c.SkuUuid == sku.SkuUuid && c.Usage == ComponentUsage.Static)
                .OrderBy(c => c.ComponentErpId)
                .Select(c => c.ComponentErpId)
                .ToListAsync(cancellationToken));

            processIds = await db.SkuProcesses
                .AsNoTracking()
                .Where(p => p.SkuUuid == sku.SkuUuid)
                .Select(p => p.ProcessId)
                .ToListAsync(cancellationToken);
        }

        countedComponents = countedComponents.Distinct().ToList();

        var stock = await ComponentStock.Load(db, countedComponents, cancellationToken);
        var knownComponents = countedComponents.Where(stock.IsKnown).ToList();

        var offlineProcesses = await db.ProcessFactories
            .AsNoTracking()
            .Where(p => processIds.Contains(p.ProcessId) && factoryIds.Contains(p.FactoryId) && p.CapacityPct == 0)
            .OrderBy(p => p.ProcessId)
            .ToListAsync(cancellationToken);

        var factories = factoryIds
            .Select(factoryId =>
            {
                var skips = knownComponents
                    .Where(erpId => stock.Status(erpId, factoryId) == StockStatuses.OutOfStock
                                    && stock.FindAlternativeInStock(erpId, factoryId) == null)
                    .Select(erpId => new LegacySkipDto(SkipReason.ComponentOos, erpId, null))
                    .Concat(offlineProcesses
                        .Where(p => p.FactoryId == factoryId)
                        .Select(p => new LegacySkipDto(SkipReason.ProcessOffline, null, p.ProcessId)))
                    .ToList();

                return new LegacyFactoryEligibilityDto(factoryId, skips.Count == 0, skips);
            })
            .ToList();

        await LogSkips(orderId, orderItemId, sku?.SkuUuid, factories, cancellationToken);

        var firstEligible = factories.FirstOrDefault(f => f.Eligible)?.FactoryId;

        return new LegacyEligibilityDto(
            orderId,
            orderItemId,
            sku?.SkuUuid,
            firstEligible != null,
            firstEligible,
            firstEligible == null ? NoFactoryQualifiesMessage : null,
            factories,
            countedComponents.Where(erpId => !stock.IsKnown(erpId)).ToList());
    }

    private async Task Validate(string orderId, string orderItemId, List<string> factoryIds, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrEmpty(orderId))
        {
            errors["orderId"] = ["orderId is required."];
        }
        else if (orderId.Length > MaxOrderKeyLength)
        {
            errors["orderId"] = [$"orderId must be at most {MaxOrderKeyLength} characters."];
        }

        if (orderItemId?.Length > MaxOrderKeyLength)
        {
            errors["orderItemId"] = [$"orderItemId must be at most {MaxOrderKeyLength} characters."];
        }

        if (factoryIds.Count == 0)
        {
            errors["factoryIds"] = ["At least one candidate factory is required."];
        }
        else
        {
            var knownFactoryIds = await db.Factories
                .AsNoTracking()
                .Where(f => factoryIds.Contains(f.Id))
                .Select(f => f.Id)
                .ToListAsync(cancellationToken);
            var unknownFactoryIds = factoryIds.Except(knownFactoryIds).ToList();

            if (unknownFactoryIds.Count > 0)
            {
                errors["factoryIds"] = [$"Unknown factories: {string.Join(", ", unknownFactoryIds)}."];
            }
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private async Task LogSkips(
        string orderId,
        string orderItemId,
        Guid? skuUuid,
        IReadOnlyCollection<LegacyFactoryEligibilityDto> factories,
        CancellationToken cancellationToken)
    {
        if (factories.All(f => f.Eligible))
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var dayStart = now.Date;

        var loggedToday = (await db.BalancerSkipLogs
                .AsNoTracking()
                .Where(l => l.OrderId == orderId && l.OrderItemId == orderItemId && l.CreatedAt >= dayStart)
                .ToListAsync(cancellationToken))
            .Select(l => (l.FactoryId, l.Reason, l.ComponentErpId, l.ProcessId))
            .ToHashSet();

        var newEntries = factories
            .SelectMany(f => f.Skips.Select(s => (f.FactoryId, s.Reason, s.ComponentErpId, s.ProcessId)))
            .Where(skip => !loggedToday.Contains(skip))
            .ToList();

        if (newEntries.Count == 0)
        {
            return;
        }

        foreach (var skip in newEntries)
        {
            db.BalancerSkipLogs.Add(new BalancerSkipLog
            {
                OrderId = orderId,
                OrderItemId = orderItemId,
                SkuUuid = skuUuid,
                FactoryId = skip.FactoryId,
                Reason = skip.Reason,
                ComponentErpId = skip.ComponentErpId,
                ProcessId = skip.ProcessId,
                CreatedAt = now
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
