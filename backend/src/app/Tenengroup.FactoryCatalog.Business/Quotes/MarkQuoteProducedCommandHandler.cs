using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Quotes;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Quotes;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Json;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Quotes;

public class MarkQuoteProducedCommandHandler(
    IFactoryCatalogDbContext db,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<MarkQuoteProducedCommand, QuoteDetailsDto>
{
    public async Task<QuoteDetailsDto> Handle(MarkQuoteProducedCommand request, CancellationToken cancellationToken)
    {
        ServiceAccountAccess.Ensure(currentUser);

        if (request.ProducedAt == default)
        {
            throw new ValidationException("producedAt", "producedAt is required.");
        }

        var quote = await db.Quotes.FirstOrDefaultAsync(q => q.QuoteId == request.QuoteId, cancellationToken)
                    ?? throw new EntityNotFoundException("Quote", request.QuoteId);
        var set = await db.QuoteSets.AsNoTracking().FirstAsync(s => s.Id == quote.QuoteSetId, cancellationToken);

        var produced = string.IsNullOrWhiteSpace(request.ReproductionOfQuoteId)
            ? await MarkProduced(quote, request, cancellationToken)
            : await RecordReproduction(quote, request, cancellationToken);

        return QuoteMapper.ToDetailsDto(set, produced, await QuoteMapper.LoadOriginalQuoteIds(db, [produced], cancellationToken));
    }

    private async Task<Quote> MarkProduced(Quote quote, MarkQuoteProducedCommand request, CancellationToken cancellationToken)
    {
        if (quote.Status == QuoteStatus.Selected)
        {
            quote.Status = QuoteStatus.Produced;
            quote.ProducedAt = request.ProducedAt;
            quote.ProductionLogJson = ProductionLogJson(request);

            await db.SaveChangesAsync(cancellationToken);

            return quote;
        }

        if (quote.Status != QuoteStatus.Produced)
        {
            throw new ConflictException(
                $"Quote '{quote.QuoteId}' is {quote.Status}; only a selected quote can be marked produced.");
        }

        if (quote.ProducedAt != request.ProducedAt)
        {
            throw new ConflictException(
                $"Quote '{quote.QuoteId}' was already produced at {quote.ProducedAt:O}; send reproductionOfQuoteId to record a reproduction.");
        }

        return quote;
    }

    private async Task<Quote> RecordReproduction(Quote quote, MarkQuoteProducedCommand request, CancellationToken cancellationToken)
    {
        var original = request.ReproductionOfQuoteId == quote.QuoteId
            ? quote
            : await db.Quotes.AsNoTracking().FirstOrDefaultAsync(q => q.QuoteId == request.ReproductionOfQuoteId, cancellationToken)
              ?? throw new EntityNotFoundException("Quote", request.ReproductionOfQuoteId);

        if (original.Status != QuoteStatus.Produced)
        {
            throw new ConflictException(
                $"Quote '{original.QuoteId}' is {original.Status}; only a produced quote can be reproduced.");
        }

        var recorded = await db.Quotes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                q => q.ReproductionOfQuoteId == original.Id
                     && q.ProducedAt == request.ProducedAt
                     && q.FactoryId == quote.FactoryId,
                cancellationToken);

        if (recorded != null)
        {
            return recorded;
        }

        if (quote.Status == QuoteStatus.Selected)
        {
            quote.Status = QuoteStatus.Produced;
            quote.ProducedAt = request.ProducedAt;
            quote.ProductionLogJson = ProductionLogJson(request);
            quote.ReproductionOfQuoteId = original.Id;

            await db.SaveChangesAsync(cancellationToken);

            return quote;
        }

        if (quote.Status != QuoteStatus.Produced)
        {
            throw new ConflictException(
                $"Quote '{quote.QuoteId}' is {quote.Status}; a reproduction needs a selected or produced quote.");
        }

        var reproductionPrefix = $"{quote.QuoteId}-RP";
        var reproductionNumber = await db.Quotes.CountAsync(q => q.QuoteId.StartsWith(reproductionPrefix), cancellationToken) + 1;

        var reproduction = new Quote
        {
            QuoteId = $"{reproductionPrefix}{reproductionNumber}",
            QuoteSetId = quote.QuoteSetId,
            FactoryId = quote.FactoryId,
            Producible = quote.Producible,
            Reasons = quote.Reasons.ToList(),
            MaterialCost = quote.MaterialCost,
            LaborCost = quote.LaborCost,
            PackingCost = quote.PackingCost,
            TotalCost = quote.TotalCost,
            Currency = quote.Currency,
            CapacitySignalPct = quote.CapacitySignalPct,
            SharePct = quote.SharePct,
            ComponentsJson = quote.ComponentsJson,
            ProcessesJson = quote.ProcessesJson,
            Status = QuoteStatus.Produced,
            SelectedAt = quote.SelectedAt,
            ProducedAt = request.ProducedAt,
            ReproductionOfQuoteId = original.Id,
            ProductionLogJson = ProductionLogJson(request),
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };

        db.Quotes.Add(reproduction);
        await db.SaveChangesAsync(cancellationToken);

        return reproduction;
    }

    private static string ProductionLogJson(MarkQuoteProducedCommand request)
    {
        return (request.ProductionLog ?? []).ToJsonString();
    }
}
