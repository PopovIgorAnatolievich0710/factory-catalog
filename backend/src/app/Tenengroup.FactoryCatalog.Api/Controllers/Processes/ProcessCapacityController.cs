using Microsoft.AspNetCore.Mvc;
using Tenengroup.FactoryCatalog.Api.Mappers.Processes;
using Tenengroup.FactoryCatalog.Api.Models.Processes;
using Tenengroup.FactoryCatalog.Business.Abstractions.Processes;
using Tenengroup.Infrastructure.Common.Auth;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.FactoryCatalog.Api.Controllers.Processes;

[ApiController]
[Route("v1/process-capacity")]
[ApiExplorerSettings(GroupName = "Process capacity")]
[AzureAuthorize(Permission.ProcessesView)]
public class ProcessCapacityController(IRequestRunner requestRunner) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<ProcessCapacityRowDto>> GetOverview(CancellationToken cancellationToken)
    {
        return requestRunner.Run<GetProcessCapacityOverviewQuery, IReadOnlyCollection<ProcessCapacityRowDto>>(
            new GetProcessCapacityOverviewQuery(), cancellationToken);
    }

    [HttpGet("factories/{factoryId}")]
    public Task<IReadOnlyCollection<ProcessCapacityRowDto>> GetFactory(string factoryId, CancellationToken cancellationToken)
    {
        return requestRunner.Run<GetFactoryProcessCapacityQuery, IReadOnlyCollection<ProcessCapacityRowDto>>(
            new GetFactoryProcessCapacityQuery(factoryId), cancellationToken);
    }

    [HttpPut("factories/{factoryId}/processes/{processId}")]
    [AzureAuthorize(Permission.ProcessesEditCapacity)]
    public async Task<ProcessCapacityRowDto> SetCapacity(
        string factoryId,
        string processId,
        [FromBody] SetProcessCapacityModel model,
        CancellationToken cancellationToken)
    {
        await requestRunner.Run(model.ToCommand(processId, factoryId), cancellationToken);

        return (await GetFactory(factoryId, cancellationToken)).FirstOrDefault(row => row.ProcessId == processId);
    }
}
