using Microsoft.AspNetCore.Mvc;
using Tenengroup.FactoryCatalog.Api.Models.LegacyBalancer;
using Tenengroup.FactoryCatalog.Business.Abstractions.LegacyBalancer;
using Tenengroup.Infrastructure.Common.Auth;
using Tenengroup.Infrastructure.Common.Excel;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.FactoryCatalog.Api.Controllers.LegacyBalancer;

[ApiController]
[Route("v1/legacy-balancer")]
[ApiExplorerSettings(GroupName = "LegacyBalancer")]
public class LegacyBalancerController(IRequestRunner requestRunner) : ControllerBase
{
    [HttpPost("eligibility")]
    [AzureAuthorize]
    public Task<LegacyEligibilityDto> CheckEligibility(
        [FromBody] LegacyEligibilityRequestModel model,
        CancellationToken cancellationToken)
    {
        return requestRunner.Run<CheckLegacyEligibilityCommand, LegacyEligibilityDto>(
            new CheckLegacyEligibilityCommand(
                model.OrderId,
                model.OrderItemId,
                model.Sku,
                (model.Cbd ?? []).Select(c => c.ErpId).ToList(),
                model.FactoryIds),
            cancellationToken);
    }

    [HttpPost("skip-log/search")]
    [AzureAuthorize(Permission.BalancerView)]
    public Task<ListResult<SkipLogEntryDto>> SearchSkipLog([FromBody] ListRequest request, CancellationToken cancellationToken)
    {
        return requestRunner.Run<SearchSkipLogQuery, ListResult<SkipLogEntryDto>>(new SearchSkipLogQuery(request), cancellationToken);
    }

    [HttpPost("skip-log/export")]
    [AzureAuthorize(Permission.BalancerView)]
    public async Task<FileResult> ExportSkipLog([FromBody] ExportRequest request, CancellationToken cancellationToken)
    {
        return (await requestRunner.Run<ExportSkipLogQuery, ExcelFile>(new ExportSkipLogQuery(request), cancellationToken))
            .ToFileResult();
    }

    [HttpGet("country-sku-lists")]
    [AzureAuthorize(Permission.BalancerView)]
    public Task<IReadOnlyList<CountrySkuListDto>> GetCountrySkuLists([FromQuery] string factoryId, CancellationToken cancellationToken)
    {
        return requestRunner.Run<GetCountrySkuListsQuery, IReadOnlyList<CountrySkuListDto>>(
            new GetCountrySkuListsQuery(factoryId), cancellationToken);
    }
}
