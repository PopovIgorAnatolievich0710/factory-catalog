using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tenengroup.FactoryCatalog.Api.Mappers.Skus;
using Tenengroup.FactoryCatalog.Api.Models.Skus;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Details;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.FactoryShare;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Import;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.List;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.ProcessMappings;
using Tenengroup.Infrastructure.Common.Auth;
using Tenengroup.Infrastructure.Common.Excel;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.FactoryCatalog.Api.Controllers.Skus;

[ApiController]
[Route("v1/skus")]
[ApiExplorerSettings(GroupName = "Skus")]
[AzureAuthorize(Permission.SkusView)]
public class SkusController(IRequestRunner requestRunner) : ControllerBase
{
    [HttpPost("search")]
    public Task<ListResult<SkuListItemDto>> Search([FromBody] ListRequest request, CancellationToken cancellationToken)
    {
        return requestRunner.Run<SearchSkusQuery, ListResult<SkuListItemDto>>(new SearchSkusQuery(request), cancellationToken);
    }

    [HttpPost("export")]
    public async Task<FileResult> Export([FromBody] ExportRequest request, CancellationToken cancellationToken)
    {
        return (await requestRunner.Run<ExportSkusQuery, ExcelFile>(new ExportSkusQuery(request), cancellationToken)).ToFileResult();
    }

    [HttpGet("summary")]
    public Task<SkusSummaryDto> GetSummary(CancellationToken cancellationToken)
    {
        return requestRunner.Run<GetSkusSummaryQuery, SkusSummaryDto>(new GetSkusSummaryQuery(), cancellationToken);
    }

    [HttpGet("{skuUuid:guid}")]
    public Task<SkuDetailsDto> Get(Guid skuUuid, CancellationToken cancellationToken)
    {
        return requestRunner.Run<GetSkuQuery, SkuDetailsDto>(new GetSkuQuery(skuUuid), cancellationToken);
    }

    [HttpPut("{skuUuid:guid}/factories/{factoryId}/producible")]
    [AzureAuthorize(Permission.SkusEditProducible)]
    public Task<SkuFactorySharesDto> SetProducible(
        Guid skuUuid,
        string factoryId,
        [FromBody] SetSkuProducibleModel model,
        CancellationToken cancellationToken)
    {
        return requestRunner.Run<SetSkuProducibleCommand, SkuFactorySharesDto>(
            new SetSkuProducibleCommand(skuUuid, factoryId, model.Producible!.Value), cancellationToken);
    }

    [HttpPut("{skuUuid:guid}/factories/{factoryId}/share-override")]
    [AzureAuthorize(Permission.SkusEditShareOverride)]
    public Task<SkuFactorySharesDto> SetShareOverride(
        Guid skuUuid,
        string factoryId,
        [FromBody] SetSkuShareOverrideModel model,
        CancellationToken cancellationToken)
    {
        return requestRunner.Run<SetSkuShareOverrideCommand, SkuFactorySharesDto>(
            new SetSkuShareOverrideCommand(skuUuid, factoryId, model.SharePct!.Value), cancellationToken);
    }

    [HttpDelete("{skuUuid:guid}/factories/{factoryId}/share-override")]
    [AzureAuthorize(Permission.SkusEditShareOverride)]
    public Task<SkuFactorySharesDto> ResetShareOverride(Guid skuUuid, string factoryId, CancellationToken cancellationToken)
    {
        return requestRunner.Run<ResetSkuShareOverrideCommand, SkuFactorySharesDto>(
            new ResetSkuShareOverrideCommand(skuUuid, factoryId), cancellationToken);
    }

    [HttpPost("process-mappings/import/validate")]
    [AzureAuthorize(Permission.SkusEditMappings)]
    public async Task<ImportValidationResultDto> ValidateProcessMappingImport(IFormFile file, CancellationToken cancellationToken)
    {
        return await requestRunner.Run<ValidateProcessMappingImportQuery, ImportValidationResultDto>(
            new ValidateProcessMappingImportQuery(await file.ToBytes(cancellationToken)), cancellationToken);
    }

    [HttpPost("process-mappings/import")]
    [AzureAuthorize(Permission.SkusEditMappings)]
    public async Task<ImportValidationResultDto> ImportProcessMappings(IFormFile file, CancellationToken cancellationToken)
    {
        return await requestRunner.Run<ImportProcessMappingsCommand, ImportValidationResultDto>(
            new ImportProcessMappingsCommand(await file.ToBytes(cancellationToken)), cancellationToken);
    }
}
