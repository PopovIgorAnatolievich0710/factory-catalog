using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tenengroup.FactoryCatalog.Api.Mappers.Skus;
using Tenengroup.FactoryCatalog.Api.Models.Skus;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.FactoryShare;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Import;
using Tenengroup.Infrastructure.Common.Auth;
using Tenengroup.Infrastructure.Common.Excel;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.FactoryCatalog.Api.Controllers.Skus;

[ApiController]
[Route("v1/factory-share")]
[ApiExplorerSettings(GroupName = "FactoryShare")]
[AzureAuthorize(Permission.SkusView)]
public class FactoryShareController(IRequestRunner requestRunner) : ControllerBase
{
    [HttpPost("search")]
    public Task<ListResult<FactoryShareRowDto>> Search([FromBody] ListRequest request, CancellationToken cancellationToken)
    {
        return requestRunner.Run<SearchFactoryShareQuery, ListResult<FactoryShareRowDto>>(
            new SearchFactoryShareQuery(request), cancellationToken);
    }

    [HttpPost("export")]
    public async Task<FileResult> Export([FromBody] ExportRequest request, CancellationToken cancellationToken)
    {
        return (await requestRunner.Run<ExportFactoryShareQuery, ExcelFile>(new ExportFactoryShareQuery(request), cancellationToken))
            .ToFileResult();
    }

    [HttpPost("producible/bulk")]
    [AzureAuthorize(Permission.SkusEditProducible)]
    public Task<BulkSetSkuProducibleResultDto> BulkSetProducible(
        [FromBody] BulkSetSkuProducibleModel model,
        CancellationToken cancellationToken)
    {
        return requestRunner.Run<BulkSetSkuProducibleCommand, BulkSetSkuProducibleResultDto>(
            new BulkSetSkuProducibleCommand(model.FactoryId, model.SkuUuids, model.Producible!.Value), cancellationToken);
    }

    [HttpPost("producible/import/validate")]
    [AzureAuthorize(Permission.SkusEditProducible)]
    public async Task<ImportValidationResultDto> ValidateProducibleImport(IFormFile file, CancellationToken cancellationToken)
    {
        return await requestRunner.Run<ValidateProducibleImportQuery, ImportValidationResultDto>(
            new ValidateProducibleImportQuery(await file.ToBytes(cancellationToken)), cancellationToken);
    }

    [HttpPost("producible/import")]
    [AzureAuthorize(Permission.SkusEditProducible)]
    public async Task<ImportValidationResultDto> ImportProducible(IFormFile file, CancellationToken cancellationToken)
    {
        return await requestRunner.Run<ImportProducibleCommand, ImportValidationResultDto>(
            new ImportProducibleCommand(await file.ToBytes(cancellationToken)), cancellationToken);
    }
}
