using Microsoft.AspNetCore.Mvc;
using Tenengroup.FactoryCatalog.Api.Models.Skus;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.FactoryShare;
using Tenengroup.Infrastructure.Common.Auth;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.FactoryCatalog.Api.Controllers.Skus;

[ApiController]
[Route("v1/settings/factory-share")]
[ApiExplorerSettings(GroupName = "Settings")]
[AzureAuthorize(Permission.SkusView)]
public class FactoryShareSettingsController(IRequestRunner requestRunner) : ControllerBase
{
    [HttpGet]
    public Task<GlobalFactoryShareDto> Get(CancellationToken cancellationToken)
    {
        return requestRunner.Run<GetGlobalFactoryShareQuery, GlobalFactoryShareDto>(new GetGlobalFactoryShareQuery(), cancellationToken);
    }

    [HttpPut]
    [AzureAuthorize(Permission.SkusEditGlobalShare)]
    public Task<GlobalFactoryShareDto> Update([FromBody] UpdateGlobalFactoryShareModel model, CancellationToken cancellationToken)
    {
        return requestRunner.Run<UpdateGlobalFactoryShareCommand, GlobalFactoryShareDto>(
            new UpdateGlobalFactoryShareCommand(
                model.FactoryShares.Select(s => new FactorySharePctDto(s.FactoryId, s.Pct!.Value)).ToList()),
            cancellationToken);
    }
}
