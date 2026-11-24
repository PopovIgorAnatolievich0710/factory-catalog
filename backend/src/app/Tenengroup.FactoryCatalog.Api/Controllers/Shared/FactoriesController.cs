using Microsoft.AspNetCore.Mvc;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Factories;
using Tenengroup.Infrastructure.Common.Auth;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Api.Controllers.Shared;

[ApiController]
[Route("v1/factories")]
[ApiExplorerSettings(GroupName = "Factories")]
[AzureAuthorize]
public class FactoriesController(IRequestRunner requestRunner) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<FactoryDto>> GetAll(CancellationToken cancellationToken)
    {
        return requestRunner.Run<GetFactoriesQuery, IReadOnlyCollection<FactoryDto>>(new GetFactoriesQuery(), cancellationToken);
    }
}
