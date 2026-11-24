using Microsoft.AspNetCore.Mvc;
using Tenengroup.FactoryCatalog.Api.Models.Components;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components.Alternatives;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components.Replacements;
using Tenengroup.Infrastructure.Common.Auth;
using Tenengroup.Infrastructure.Common.Excel;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.FactoryCatalog.Api.Controllers.Components;

[ApiController]
[Route("v1/components")]
[ApiExplorerSettings(GroupName = "Components")]
[AzureAuthorize(Permission.ComponentsView)]
public class ComponentsController(IRequestRunner requestRunner) : ControllerBase
{
    [HttpPost("search")]
    public Task<ListResult<ComponentListItemDto>> Search([FromBody] ListRequest request, CancellationToken cancellationToken)
    {
        return requestRunner.Run<SearchComponentsQuery, ListResult<ComponentListItemDto>>(
            new SearchComponentsQuery(request), cancellationToken);
    }

    [HttpPost("export")]
    public async Task<FileResult> Export([FromBody] ExportRequest request, CancellationToken cancellationToken)
    {
        return (await requestRunner.Run<ExportComponentsQuery, ExcelFile>(new ExportComponentsQuery(request), cancellationToken))
            .ToFileResult();
    }

    [HttpGet("filter-options")]
    public Task<ComponentFilterOptionsDto> GetFilterOptions(CancellationToken cancellationToken)
    {
        return requestRunner.Run<GetComponentFilterOptionsQuery, ComponentFilterOptionsDto>(
            new GetComponentFilterOptionsQuery(), cancellationToken);
    }

    [HttpGet("{erpId}")]
    public Task<ComponentDetailsDto> Get(string erpId, CancellationToken cancellationToken)
    {
        return requestRunner.Run<GetComponentQuery, ComponentDetailsDto>(new GetComponentQuery(erpId), cancellationToken);
    }

    [HttpPut("{erpId}/factories/{factoryId}/manual-oos")]
    [AzureAuthorize(Permission.ComponentsEditManualOos)]
    public Task SetManualOos(
        string erpId,
        string factoryId,
        [FromBody] SetManualOosModel model,
        CancellationToken cancellationToken)
    {
        return requestRunner.Run(new SetManualOosCommand(erpId, factoryId, model.Active, model.Note), cancellationToken);
    }

    [HttpPut("{erpId}/critical-level")]
    [AzureAuthorize(Permission.ComponentsEditCriticalLevels)]
    public Task SetCriticalLevel(string erpId, [FromBody] SetCriticalLevelModel model, CancellationToken cancellationToken)
    {
        return requestRunner.Run(new SetCriticalLevelCommand(erpId, model.Level), cancellationToken);
    }

    [HttpPut("{erpId}/factories/{factoryId}/critical-level-override")]
    [AzureAuthorize(Permission.ComponentsEditCriticalLevels)]
    public Task SetCriticalLevelOverride(
        string erpId,
        string factoryId,
        [FromBody] SetCriticalLevelModel model,
        CancellationToken cancellationToken)
    {
        return requestRunner.Run(new SetCriticalLevelOverrideCommand(erpId, factoryId, model.Level), cancellationToken);
    }

    [HttpGet("{erpId}/alternatives")]
    public Task<IReadOnlyCollection<ReplacementAlternativeDto>> GetReplacementAlternatives(
        string erpId,
        [FromQuery] string factoryId,
        CancellationToken cancellationToken)
    {
        return requestRunner.Run<GetReplacementAlternativesQuery, IReadOnlyCollection<ReplacementAlternativeDto>>(
            new GetReplacementAlternativesQuery(erpId, factoryId), cancellationToken);
    }

    [HttpPost("{erpId}/alternatives")]
    [AzureAuthorize(Permission.ComponentsEditAlternatives)]
    public Task AddAlternative(string erpId, [FromBody] AddAlternativeModel model, CancellationToken cancellationToken)
    {
        return requestRunner.Run(
            new AddAlternativeCommand(erpId, model.AlternativeErpId, model.ForBalancer, model.ForPlanner), cancellationToken);
    }

    [HttpPut("{erpId}/alternatives/{alternativeErpId}")]
    [AzureAuthorize(Permission.ComponentsEditAlternatives)]
    public Task SetAlternativeUse(
        string erpId,
        string alternativeErpId,
        [FromBody] SetAlternativeUseModel model,
        CancellationToken cancellationToken)
    {
        return requestRunner.Run(
            new SetAlternativeUseCommand(erpId, alternativeErpId, model.ForBalancer, model.ForPlanner), cancellationToken);
    }

    [HttpDelete("{erpId}/alternatives/{alternativeErpId}")]
    [AzureAuthorize(Permission.ComponentsEditAlternatives)]
    public Task RemoveAlternative(string erpId, string alternativeErpId, CancellationToken cancellationToken)
    {
        return requestRunner.Run(new RemoveAlternativeCommand(erpId, alternativeErpId), cancellationToken);
    }

    [HttpPost("replacements")]
    public Task<ComponentReplacementDto> RecordReplacement(
        [FromBody] RecordComponentReplacementModel model,
        CancellationToken cancellationToken)
    {
        return requestRunner.Run<RecordComponentReplacementCommand, ComponentReplacementDto>(
            new RecordComponentReplacementCommand(
                model.OrderId,
                model.OrderItemId,
                model.FactoryId,
                model.OriginalErpId,
                model.ReplacementErpId,
                model.Quantity,
                model.ReplacedBy,
                model.ReplacedAt),
            cancellationToken);
    }
}
