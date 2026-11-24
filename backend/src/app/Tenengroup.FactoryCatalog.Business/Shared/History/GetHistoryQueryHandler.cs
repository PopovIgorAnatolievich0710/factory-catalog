using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;
using Microsoft.EntityFrameworkCore;

namespace Tenengroup.FactoryCatalog.Business.Shared.History;

public class GetHistoryQueryHandler(IFactoryCatalogDbContext db)
    : IRequestHandler<GetHistoryQuery, ListResult<HistoryEntryDto>>
{
    public Task<ListResult<HistoryEntryDto>> Handle(GetHistoryQuery request, CancellationToken cancellationToken)
    {
        return db.HistoryEntries
            .AsNoTracking()
            .Where(h => h.EntityType == request.EntityType && h.EntityId == request.EntityId)
            .OrderByDescending(h => h.ChangedAt)
            .ThenByDescending(h => h.Id)
            .Select(h => new HistoryEntryDto(h.ChangedBy, h.ChangedAt, h.Source, h.FactoryId, h.Field, h.OldValue, h.NewValue))
            .ToListResult(new ListRequest { Page = request.Page, PageSize = request.PageSize }, cancellationToken);
    }
}
