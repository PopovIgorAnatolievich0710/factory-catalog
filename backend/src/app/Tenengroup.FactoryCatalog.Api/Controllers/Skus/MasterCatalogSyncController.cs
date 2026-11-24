using Microsoft.AspNetCore.Mvc;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Sync;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Sync;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Auth;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.FactoryCatalog.Api.Controllers.Skus;

[ApiController]
[Route("v1/sync")]
[ApiExplorerSettings(GroupName = "Sync")]
public class MasterCatalogSyncController(IRequestRunner requestRunner) : ControllerBase
{
    [HttpPost(SyncTypes.MasterCatalogSkus + "/run")]
    [AzureAuthorize(Permission.SyncTrigger)]
    public Task<SyncStatusDto> Run(CancellationToken cancellationToken)
    {
        return requestRunner.Run<TriggerMasterCatalogSyncCommand, SyncStatusDto>(new TriggerMasterCatalogSyncCommand(), cancellationToken);
    }
}
