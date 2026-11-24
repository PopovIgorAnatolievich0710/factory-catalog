using Microsoft.AspNetCore.Mvc;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components.Sync;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Sync;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Auth;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.FactoryCatalog.Api.Controllers.Components;

[ApiController]
[Route("v1/sync")]
[ApiExplorerSettings(GroupName = "Sync")]
public class SapComponentsSyncController(IRequestRunner requestRunner) : ControllerBase
{
    [HttpPost(SyncTypes.SapComponents + "/run")]
    [AzureAuthorize(Permission.SyncTrigger)]
    public Task<SyncStatusDto> Run(CancellationToken cancellationToken)
    {
        return requestRunner.Run<RunSapComponentsSyncCommand, SyncStatusDto>(new RunSapComponentsSyncCommand(), cancellationToken);
    }
}
