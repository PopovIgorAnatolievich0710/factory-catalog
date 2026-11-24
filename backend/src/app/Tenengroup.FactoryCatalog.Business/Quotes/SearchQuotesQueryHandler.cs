using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Quotes;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Quotes;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.Business.Quotes;

public class SearchQuotesQueryHandler(IFactoryCatalogDbContext db)
    : IRequestHandler<SearchQuotesQuery, ListResult<QuoteDetailsDto>>
{
    private const string DefaultSortBy = "createdAt";

    private static readonly Dictionary<string, Expression<Func<QuoteRow, object>>> SortableFields = new()
    {
        [DefaultSortBy] = r => r.Quote.CreatedAt,
        ["selectedAt"] = r => r.Quote.SelectedAt,
        ["producedAt"] = r => r.Quote.ProducedAt,
        ["transferredAt"] = r => r.Quote.TransferredAt,
        ["orderId"] = r => r.Set.OrderId,
        ["factoryId"] = r => r.Quote.FactoryId,
        ["status"] = r => r.Quote.Status,
        ["quoteId"] = r => r.Quote.QuoteId
    };

    public async Task<ListResult<QuoteDetailsDto>> Handle(SearchQuotesQuery query, CancellationToken cancellationToken)
    {
        var request = query.Request;

        var rows = db.Quotes
            .AsNoTracking()
            .Join(db.QuoteSets, q => q.QuoteSetId, s => s.Id, (q, s) => new QuoteRow { Quote = q, Set = s });

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            rows = rows.Where(r => r.Set.OrderId.ToLower().Contains(search)
                                   || r.Set.OrderItemId.ToLower().Contains(search)
                                   || r.Quote.QuoteId.ToLower().Contains(search));
        }

        var page = await rows
            .ApplyFilter(request, "factoryId", r => r.Quote.FactoryId)
            .ApplyFilter(request, "status", r => r.Quote.Status)
            .ApplyFilter(request, "producible", r => r.Quote.Producible)
            .ApplyFilter(request, "quoteId", r => r.Quote.QuoteId)
            .ApplyFilter(request, "quoteKey", r => r.Set.QuoteKey)
            .ApplyFilter(request, "orderId", r => r.Set.OrderId)
            .ApplyFilter(request, "orderItemId", r => r.Set.OrderItemId)
            .ApplyFilter(request, "skuUuid", r => r.Set.SkuUuid)
            .ApplyFilter(request, "mode", r => r.Set.Mode)
            .ApplyFilter(request, "createdAt", r => r.Quote.CreatedAt)
            .ApplyFilter(request, "selectedAt", r => r.Quote.SelectedAt)
            .ApplyFilter(request, "producedAt", r => r.Quote.ProducedAt)
            .ApplyFilter(request, "transferredAt", r => r.Quote.TransferredAt)
            .ApplySorting(request, SortableFields, DefaultSortBy)
            .ToListResult(request, cancellationToken);

        var originalQuoteIds = await QuoteMapper.LoadOriginalQuoteIds(db, page.Items.Select(r => r.Quote), cancellationToken);

        return new ListResult<QuoteDetailsDto>(
            page.Items.Select(r => QuoteMapper.ToDetailsDto(r.Set, r.Quote, originalQuoteIds)).ToList(),
            page.TotalCount);
    }

    private class QuoteRow
    {
        public Quote Quote { get; init; }

        public QuoteSet Set { get; init; }
    }
}
