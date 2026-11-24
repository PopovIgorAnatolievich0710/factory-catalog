using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Skus.List;

public record SearchSkusQuery(ListRequest Request) : IRequest<ListResult<SkuListItemDto>>;
