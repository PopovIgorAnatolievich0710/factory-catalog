using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Components;

public record SearchComponentsQuery(ListRequest Request) : IRequest<ListResult<ComponentListItemDto>>;
