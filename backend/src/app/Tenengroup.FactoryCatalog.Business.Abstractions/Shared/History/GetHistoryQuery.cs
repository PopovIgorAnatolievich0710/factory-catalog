using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;

public record GetHistoryQuery(string EntityType, string EntityId, int Page, int PageSize)
    : IRequest<ListResult<HistoryEntryDto>>;
