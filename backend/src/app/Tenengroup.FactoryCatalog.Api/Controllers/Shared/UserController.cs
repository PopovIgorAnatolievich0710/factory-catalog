using Microsoft.AspNetCore.Mvc;
using Tenengroup.FactoryCatalog.Api.Models.Shared;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.Infrastructure.Common.Auth;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Api.Controllers.Shared;

[ApiController]
[Route("v1/user")]
[ApiExplorerSettings(GroupName = "User")]
[AzureAuthorize]
public class UserController(IRequestRunner requestRunner) : ControllerBase
{
    [HttpGet("me")]
    public Task<CurrentUserDto> GetMe(CancellationToken cancellationToken)
    {
        return requestRunner.Run<GetCurrentUserQuery, CurrentUserDto>(new GetCurrentUserQuery(), cancellationToken);
    }

    [HttpGet("list-settings/{listKey}")]
    public Task<ListSettingsDto> GetListSettings(string listKey, CancellationToken cancellationToken)
    {
        return requestRunner.Run<GetListSettingsQuery, ListSettingsDto>(new GetListSettingsQuery(listKey), cancellationToken);
    }

    [HttpPut("list-settings/{listKey}")]
    public Task<ListSettingsDto> UpdateListSettings(string listKey, [FromBody] UpdateListSettingsModel model, CancellationToken cancellationToken)
    {
        return requestRunner.Run<UpdateListSettingsCommand, ListSettingsDto>(
            new UpdateListSettingsCommand(listKey, model.Columns), cancellationToken);
    }
}
