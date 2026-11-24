using Microsoft.AspNetCore.Mvc;
using Tenengroup.FactoryCatalog.Api.Mappers.Processes;
using Tenengroup.FactoryCatalog.Api.Models.Processes;
using Tenengroup.FactoryCatalog.Business.Abstractions.Processes;
using Tenengroup.Infrastructure.Common.Auth;
using Tenengroup.Infrastructure.Common.Excel;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.FactoryCatalog.Api.Controllers.Processes;

[ApiController]
[Route("v1/processes")]
[ApiExplorerSettings(GroupName = "Processes")]
[AzureAuthorize(Permission.ProcessesView)]
public class ProcessesController(IRequestRunner requestRunner) : ControllerBase
{
    [HttpPost("search")]
    public Task<ListResult<ProcessListItemDto>> Search([FromBody] ListRequest request, CancellationToken cancellationToken)
    {
        return requestRunner.Run<SearchProcessesQuery, ListResult<ProcessListItemDto>>(
            new SearchProcessesQuery(request), cancellationToken);
    }

    [HttpPost("export")]
    public async Task<FileResult> Export([FromBody] ExportRequest request, CancellationToken cancellationToken)
    {
        return (await requestRunner.Run<ExportProcessesQuery, ExcelFile>(new ExportProcessesQuery(request), cancellationToken))
            .ToFileResult();
    }

    [HttpGet("{processId}")]
    public Task<ProcessDto> Get(string processId, CancellationToken cancellationToken)
    {
        return requestRunner.Run<GetProcessQuery, ProcessDto>(new GetProcessQuery(processId), cancellationToken);
    }

    [HttpPost]
    [AzureAuthorize(Permission.ProcessesEdit)]
    public async Task<ProcessDto> Create([FromBody] CreateProcessModel model, CancellationToken cancellationToken)
    {
        var processId = await requestRunner.Run<CreateProcessCommand, string>(model.ToCommand(), cancellationToken);

        return await Get(processId, cancellationToken);
    }

    [HttpPut("{processId}")]
    [AzureAuthorize(Permission.ProcessesEdit)]
    public async Task<ProcessDto> Update(string processId, [FromBody] UpdateProcessModel model, CancellationToken cancellationToken)
    {
        await requestRunner.Run(model.ToCommand(processId), cancellationToken);

        return await Get(processId, cancellationToken);
    }

    [HttpPut("{processId}/factories/{factoryId}")]
    [AzureAuthorize(Permission.ProcessesEditFactorySetup)]
    public async Task<ProcessDto> UpdateFactorySetup(
        string processId,
        string factoryId,
        [FromBody] UpdateProcessFactorySetupModel model,
        CancellationToken cancellationToken)
    {
        await requestRunner.Run(model.ToCommand(processId, factoryId), cancellationToken);

        return await Get(processId, cancellationToken);
    }

    [HttpPost("import/validate")]
    [AzureAuthorize(Permission.ProcessesEdit)]
    public async Task<ProcessImportValidationDto> ValidateImport(IFormFile file, CancellationToken cancellationToken)
    {
        return await requestRunner.Run<ValidateProcessImportQuery, ProcessImportValidationDto>(
            new ValidateProcessImportQuery(await file.ToContent(cancellationToken)), cancellationToken);
    }

    [HttpPost("import")]
    [AzureAuthorize(Permission.ProcessesEdit)]
    public async Task<ProcessImportResultDto> Import(IFormFile file, CancellationToken cancellationToken)
    {
        return await requestRunner.Run<ImportProcessesCommand, ProcessImportResultDto>(
            new ImportProcessesCommand(await file.ToContent(cancellationToken)), cancellationToken);
    }
}
