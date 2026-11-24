using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tenengroup.Balancer.Business.Abstractions;
using Tenengroup.Balancer.Business.Abstractions.Catalog;
using Tenengroup.Balancer.Business.Abstractions.Events;
using Tenengroup.Balancer.Business.Abstractions.Production;
using Tenengroup.Balancer.DataAccess.Abstractions;
using Tenengroup.Balancer.Domains;
using Tenengroup.Infrastructure.Common.Aws.Sns;
using Tenengroup.Infrastructure.Common.Json;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.Balancer.Business.Production;

public class RecordProductionCommandHandler(
    IBalancerDbContext db,
    ICatalogClient catalogClient,
    IEventPublisher eventPublisher,
    BalancerOptions options,
    TimeProvider timeProvider,
    ILogger<RecordProductionCommandHandler> logger) : IRequestHandler<RecordProductionCommand, bool>
{
    private const string ProducedStatus = "produced";

    public async Task<bool> Handle(RecordProductionCommand request, CancellationToken cancellationToken)
    {
        if (await db.ProductionRecords.AnyAsync(r => r.SourceEventId == request.SourceEventId, cancellationToken))
        {
            return true;
        }

        var allocation = await db.Allocations
            .Where(a => a.OrderId == request.OrderId
                        && a.OrderItemId == request.OrderItemId
                        && a.Mode != AllocationMode.Shadow
                        && a.FactoryId != null)
            .OrderByDescending(a => a.DecidedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (allocation == null)
        {
            logger.LogWarning("Production event {EventId}: no live allocation for order {OrderId} item {OrderItemId}",
                request.SourceEventId, request.OrderId, request.OrderItemId);
            return true;
        }

        var original = await db.ProductionRecords
            .FirstOrDefaultAsync(r => r.AllocationId == allocation.Id && !r.IsReproduction, cancellationToken);

        if (request.IsReproduction && original == null)
        {
            logger.LogWarning("Reproduction event {EventId} arrived before the sent event of order {OrderId} item {OrderItemId}",
                request.SourceEventId, request.OrderId, request.OrderItemId);
            return false;
        }

        if (!request.IsReproduction && original != null)
        {
            return true;
        }

        var quoteId = allocation.SelectedQuoteId;
        if (quoteId != null)
        {
            var produced = await catalogClient.MarkQuoteProduced(
                request.IsReproduction ? original.QuoteId : quoteId,
                new MarkQuoteProducedRequest(request.SentAt, request.ProductionLog, request.IsReproduction ? original.QuoteId : null),
                cancellationToken);
            quoteId = produced.Quote.QuoteId;
        }

        var orderedAt = allocation.PaidAt ?? allocation.DecidedAt;
        var record = new ProductionRecord
        {
            AllocationId = allocation.Id,
            SourceEventId = request.SourceEventId,
            OrderId = allocation.OrderId,
            OrderItemId = allocation.OrderItemId,
            SkuUuid = allocation.SkuUuid,
            QuoteKey = allocation.QuoteKey ?? string.Empty,
            QuoteId = quoteId ?? string.Empty,
            FactoryId = allocation.FactoryId,
            PackingFacilityId = allocation.PackingFacilityId ?? allocation.FactoryId,
            SentAt = request.SentAt,
            ProductionDays = Math.Max(0, (request.SentAt.Date - orderedAt.Date).Days),
            IsReproduction = request.IsReproduction,
            ReproductionOfQuoteId = original?.QuoteId,
            ReproductionOfRecordId = original?.Id,
            ProductionLogJson = request.ProductionLog.ToJsonString(),
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };
        db.ProductionRecords.Add(record);

        if (!request.IsReproduction)
        {
            allocation.Status = AllocationStatus.Produced;
            allocation.ProducedAt = request.SentAt;
            await ApplyActualCost(allocation, request.ProductionLog, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(options.EventsTopicArn))
        {
            logger.LogWarning("Balancer:EventsTopicArn is empty; produced event for {OrderId} {OrderItemId} is not published",
                allocation.OrderId, allocation.OrderItemId);
        }
        else
        {
            await eventPublisher.Publish(options.EventsTopicArn, BalancerEventTypes.QuoteProduced, ToEvent(allocation, record), cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task ApplyActualCost(
        Allocation allocation,
        IReadOnlyCollection<ProductionLogEntry> productionLog,
        CancellationToken cancellationToken)
    {
        var measured = productionLog
            .Where(e => e.Quantity.HasValue)
            .GroupBy(e => e.ProcessId)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Quantity.Value));

        if (measured.Count == 0)
        {
            return;
        }

        var lines = await db.AllocationCostLines
            .Where(l => l.AllocationId == allocation.Id && l.Kind != CostLineKind.Component)
            .ToListAsync(cancellationToken);

        foreach (var line in lines.Where(l => measured.ContainsKey(l.Code) && l.QuotedUnitCost.HasValue))
        {
            line.ActualQuantity = measured[line.Code];
            line.ActualCost = Math.Round(line.QuotedUnitCost.Value * line.ActualQuantity.Value, 4);
        }

        var laborLines = lines.Where(l => l.Kind == CostLineKind.Process).ToList();
        if (laborLines.Any(l => l.ActualCost.HasValue))
        {
            allocation.ActualLaborCost = laborLines.Sum(l => l.ActualCost ?? l.QuotedCost);
        }
    }

    private static QuoteProducedEvent ToEvent(Allocation allocation, ProductionRecord record)
    {
        var quotedCost = allocation.ProductionCost.HasValue && allocation.Currency != null
            ? new QuoteCost(
                allocation.MaterialCost ?? 0m,
                allocation.LaborCost ?? 0m,
                allocation.PackingCost ?? 0m,
                allocation.ProductionCost.Value,
                allocation.Currency)
            : null;

        return new QuoteProducedEvent(
            record.OrderId,
            record.OrderItemId,
            record.QuoteKey,
            record.QuoteId,
            ProducedStatus,
            record.FactoryId,
            record.PackingFacilityId,
            quotedCost,
            record.SentAt,
            record.ProductionDays,
            record.ReproductionOfQuoteId);
    }
}
