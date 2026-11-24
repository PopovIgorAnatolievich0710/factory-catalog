using Tenengroup.FactoryCatalog.Business.Abstractions.Components;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.Business.Components;

public class SearchComponentsQueryHandler(ComponentListReader reader)
    : IRequestHandler<SearchComponentsQuery, ListResult<ComponentListItemDto>>
{
    public async Task<ListResult<ComponentListItemDto>> Handle(SearchComponentsQuery request, CancellationToken cancellationToken)
    {
        var rows = await reader.Read(request.Request, cancellationToken);

        return new ListResult<ComponentListItemDto>(rows.AsQueryable().ApplyPaging(request.Request).ToList(), rows.Count);
    }
}
