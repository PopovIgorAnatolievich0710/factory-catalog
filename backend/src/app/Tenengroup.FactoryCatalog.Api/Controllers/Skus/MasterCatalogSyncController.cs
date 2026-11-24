using Microsoft.AspNetCore.Mvc;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Sync;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Auth;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.FactoryCatalog.Api.Controllers.Skus;

[ApiController]
[Route("v1/sync/" + SyncTypes.MasterCatalogSkus)]
[ApiExplorerSettings(GroupName = "Sync")]
public class MasterCatalogSyncController(IRequestRunner requestRunner) : ControllerBase
{
    [HttpPost("run")]
    [AzureAuthorize(Permission.SyncTrigger)]
    public async Task<IActionResult> Run(CancellationToken cancellationToken)
    {
        await requestRunner.Run(new TriggerMasterCatalogSyncCommand(), cancellationToken);
        return Accepted();
    }
}
