using Microsoft.AspNetCore.Mvc;
using Tenengroup.FactoryCatalog.Api.Models;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.FeatureToggles;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Auth;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Api.Controllers.Shared;

[ApiController]
[Route("v1/feature-toggle")]
[ApiExplorerSettings(GroupName = "Feature toggles")]
[AzureAuthorize]
public class FeatureToggleController(IRequestRunner requestRunner) : ControllerBase
{
    [HttpGet]
    public async Task<ApiDataResult<IReadOnlyCollection<FeatureToggle>>> GetAll(CancellationToken cancellationToken)
    {
        var toggles = await requestRunner.Run<GetFeatureTogglesQuery, IReadOnlyCollection<FeatureToggle>>(
            new GetFeatureTogglesQuery(), cancellationToken);

        return new ApiDataResult<IReadOnlyCollection<FeatureToggle>>(toggles);
    }
}
