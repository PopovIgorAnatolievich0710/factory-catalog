using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Quotes;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Quotes;

public class GetQuoteSetQueryHandler(IFactoryCatalogDbContext db) : IRequestHandler<GetQuoteSetQuery, QuoteSetDto>
{
    public async Task<QuoteSetDto> Handle(GetQuoteSetQuery request, CancellationToken cancellationToken)
    {
        var set = await db.QuoteSets
                      .AsNoTracking()
                      .FirstOrDefaultAsync(s => s.QuoteKey == request.QuoteKey, cancellationToken)
                  ?? throw new EntityNotFoundException("Quote set", request.QuoteKey);

        var quotes = await db.Quotes
            .AsNoTracking()
            .Where(q => q.QuoteSetId == set.Id)
            .OrderBy(q => q.Id)
            .ToListAsync(cancellationToken);

        return QuoteMapper.ToSetDto(set, quotes, await QuoteMapper.LoadOriginalQuoteIds(db, quotes, cancellationToken));
    }
}
