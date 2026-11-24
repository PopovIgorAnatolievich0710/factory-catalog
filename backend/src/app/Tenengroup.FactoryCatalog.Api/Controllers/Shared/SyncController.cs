using Microsoft.AspNetCore.Mvc;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Sync;
using Tenengroup.Infrastructure.Common.Auth;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Api.Controllers.Shared;

[ApiController]
[Route("v1/sync")]
[ApiExplorerSettings(GroupName = "Sync")]
[AzureAuthorize]
public class SyncController(IRequestRunner requestRunner) : ControllerBase
{
    [HttpGet("{syncType}/status")]
    public Task<SyncStatusDto> GetStatus(string syncType, CancellationToken cancellationToken)
    {
        return requestRunner.Run<GetSyncStatusQuery, SyncStatusDto>(new GetSyncStatusQuery(syncType), cancellationToken);
    }
}
