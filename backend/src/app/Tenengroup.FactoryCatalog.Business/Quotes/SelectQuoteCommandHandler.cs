using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Quotes;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Quotes;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Quotes;

public class SelectQuoteCommandHandler(
    IFactoryCatalogDbContext db,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<SelectQuoteCommand, QuoteDetailsDto>
{
    public async Task<QuoteDetailsDto> Handle(SelectQuoteCommand request, CancellationToken cancellationToken)
    {
        ServiceAccountAccess.Ensure(currentUser);

        var quote = await db.Quotes.FirstOrDefaultAsync(q => q.QuoteId == request.QuoteId, cancellationToken)
                    ?? throw new EntityNotFoundException("Quote", request.QuoteId);
        var set = await db.QuoteSets.AsNoTracking().FirstAsync(s => s.Id == quote.QuoteSetId, cancellationToken);

        if (quote.Status == QuoteStatus.Quoted)
        {
            if (!quote.Producible)
            {
                throw new ConflictException(
                    $"Quote '{quote.QuoteId}' is not producible ({string.Join(", ", quote.Reasons)}) and cannot be selected.");
            }

            var winner = await db.Quotes
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    q => q.QuoteSetId == quote.QuoteSetId
                         && q.Id != quote.Id
                         && (q.Status == QuoteStatus.Selected || q.Status == QuoteStatus.Produced),
                    cancellationToken);

            if (winner != null)
            {
                throw new ConflictException(
                    $"Quote set '{set.QuoteKey}' already has the {winner.Status} quote '{winner.QuoteId}'.");
            }

            quote.Status = QuoteStatus.Selected;
            quote.SelectedAt = timeProvider.GetUtcNow().UtcDateTime;

            await db.SaveChangesAsync(cancellationToken);
        }
        else if (quote.Status != QuoteStatus.Selected)
        {
            throw new ConflictException($"Quote '{quote.QuoteId}' is {quote.Status} and cannot be selected.");
        }

        return QuoteMapper.ToDetailsDto(set, quote, await QuoteMapper.LoadOriginalQuoteIds(db, [quote], cancellationToken));
    }
}
