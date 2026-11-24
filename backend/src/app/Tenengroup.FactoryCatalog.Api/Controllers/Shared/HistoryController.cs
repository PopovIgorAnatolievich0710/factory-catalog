using Microsoft.AspNetCore.Mvc;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.Infrastructure.Common.Auth;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.Api.Controllers.Shared;

[ApiController]
[Route("v1/history")]
[ApiExplorerSettings(GroupName = "History")]
[AzureAuthorize]
public class HistoryController(IRequestRunner requestRunner) : ControllerBase
{
    [HttpGet("{entityType}/{entityId}")]
    public Task<ListResult<HistoryEntryDto>> Get(
        string entityType,
        string entityId,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = ListRequest.DefaultPageSize)
    {
        return requestRunner.Run<GetHistoryQuery, ListResult<HistoryEntryDto>>(
            new GetHistoryQuery(entityType, entityId, page, pageSize), cancellationToken);
    }
}
