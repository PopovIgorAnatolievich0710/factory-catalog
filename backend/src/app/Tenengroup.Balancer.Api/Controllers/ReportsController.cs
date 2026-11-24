using Microsoft.AspNetCore.Mvc;
using Tenengroup.Balancer.Api.Models;
using Tenengroup.Balancer.Business.Abstractions.DailyPlan;
using Tenengroup.Balancer.Business.Abstractions.Reports;
using Tenengroup.Infrastructure.Common.Auth;
using Tenengroup.Infrastructure.Common.Excel;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.Balancer.Api.Controllers;

[ApiController]
[Route("v1")]
[ApiExplorerSettings(GroupName = "Reports")]
public class ReportsController(IRequestRunner requestRunner) : ControllerBase
{
    [HttpGet("dashboard/routing")]
    [AzureAuthorize(Permission.BalancerViewDashboards)]
    public Task<RoutingDashboardDto> GetRoutingDashboard([FromQuery] DashboardFilterModel filter, CancellationToken cancellationToken)
    {
        return requestRunner.Run<GetRoutingDashboardQuery, RoutingDashboardDto>(
            new GetRoutingDashboardQuery(ToFilter(filter)), cancellationToken);
    }

    [HttpGet("dashboard/shadow-comparison")]
    [AzureAuthorize(Permission.BalancerViewDashboards)]
    public Task<ShadowComparisonDto> GetShadowComparison([FromQuery] DashboardFilterModel filter, CancellationToken cancellationToken)
    {
        return requestRunner.Run<GetShadowComparisonQuery, ShadowComparisonDto>(
            new GetShadowComparisonQuery(ToFilter(filter)), cancellationToken);
    }

    [HttpPost("dim-cost/search")]
    [AzureAuthorize(Permission.BalancerViewDimCost)]
    public Task<ListResult<DimCostRowDto>> SearchDimCost([FromBody] ListRequest request, CancellationToken cancellationToken)
    {
        return requestRunner.Run<SearchDimCostQuery, ListResult<DimCostRowDto>>(new SearchDimCostQuery(request), cancellationToken);
    }

    [HttpPost("dim-cost/export")]
    [AzureAuthorize(Permission.BalancerViewDimCost)]
    public async Task<FileResult> ExportDimCost([FromBody] ExportRequest request, CancellationToken cancellationToken)
    {
        return (await requestRunner.Run<ExportDimCostQuery, ExcelFile>(new ExportDimCostQuery(request), cancellationToken)).ToFileResult();
    }

    [HttpGet("dim-cost/{allocationId:guid}")]
    [AzureAuthorize(Permission.BalancerViewDimCost)]
    public Task<DimCostDetailsDto> GetDimCostDetails(Guid allocationId, CancellationToken cancellationToken)
    {
        return requestRunner.Run<GetDimCostDetailsQuery, DimCostDetailsDto>(new GetDimCostDetailsQuery(allocationId), cancellationToken);
    }

    [HttpGet("daily-plan")]
    [AzureAuthorize(Permission.BalancerViewDailyPlan)]
    public Task<DailyPlanDto> GetDailyPlan([FromQuery] string factoryId, [FromQuery] DateOnly date, CancellationToken cancellationToken)
    {
        return requestRunner.Run<GetDailyPlanQuery, DailyPlanDto>(new GetDailyPlanQuery(factoryId, date), cancellationToken);
    }

    private static DashboardFilter ToFilter(DashboardFilterModel model)
    {
        return new DashboardFilter(model.FactoryId, model.Country, model.From.Value, model.To.Value);
    }
}
