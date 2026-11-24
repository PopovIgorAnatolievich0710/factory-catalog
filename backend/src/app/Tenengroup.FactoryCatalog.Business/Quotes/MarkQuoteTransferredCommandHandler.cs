using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Quotes;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Quotes;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Quotes;

public class MarkQuoteTransferredCommandHandler(
    IFactoryCatalogDbContext db,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<MarkQuoteTransferredCommand, QuoteDetailsDto>
{
    public async Task<QuoteDetailsDto> Handle(MarkQuoteTransferredCommand request, CancellationToken cancellationToken)
    {
        ServiceAccountAccess.Ensure(currentUser);

        var quote = await db.Quotes.FirstOrDefaultAsync(q => q.QuoteId == request.QuoteId, cancellationToken)
                    ?? throw new EntityNotFoundException("Quote", request.QuoteId);
        var set = await db.QuoteSets.AsNoTracking().FirstAsync(s => s.Id == quote.QuoteSetId, cancellationToken);

        if (quote.Status == QuoteStatus.Selected)
        {
            quote.Status = QuoteStatus.Transferred;
            quote.TransferredAt = timeProvider.GetUtcNow().UtcDateTime;

            await db.SaveChangesAsync(cancellationToken);
        }
        else if (quote.Status != QuoteStatus.Transferred)
        {
            throw new ConflictException(
                $"Quote '{quote.QuoteId}' is {quote.Status}; only a selected quote can be marked transferred.");
        }

        return QuoteMapper.ToDetailsDto(set, quote, await QuoteMapper.LoadOriginalQuoteIds(db, [quote], cancellationToken));
    }
}
