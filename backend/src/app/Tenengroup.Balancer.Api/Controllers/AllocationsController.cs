using Microsoft.AspNetCore.Mvc;
using Tenengroup.Balancer.Api.Filters;
using Tenengroup.Balancer.Api.Models;
using Tenengroup.Balancer.Business.Abstractions.Allocations;
using Tenengroup.Infrastructure.Common.Auth;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.Balancer.Api.Controllers;

[ApiController]
[Route("v1")]
[ApiExplorerSettings(GroupName = "Allocations")]
public class AllocationsController(IRequestRunner requestRunner) : ControllerBase
{
    [HttpPost("allocations")]
    [AzureAuthorize]
    [ServiceAccountOnly]
    public Task<IReadOnlyCollection<AllocationDto>> Allocate([FromBody] AllocateOrderModel model, CancellationToken cancellationToken)
    {
        return requestRunner.Run<AllocateOrderCommand, IReadOnlyCollection<AllocationDto>>(
            new AllocateOrderCommand(
                model.OrderId,
                model.DestinationCountry,
                model.PromisedDeliveryDate,
                model.SlaDeliveryDate,
                model.IsEventOrder,
                model.EventName,
                model.Site,
                model.PaidAt,
                model.Items
                    .Select(i => new AllocationItemRequest(i.OrderItemId, i.SkuUuid, i.Quantity, i.Cbd, i.FulfillmentType))
                    .ToList()),
            cancellationToken);
    }

    [HttpPost("allocations/search")]
    [AzureAuthorize(Permission.BalancerView)]
    public Task<ListResult<AllocationDto>> Search([FromBody] ListRequest request, CancellationToken cancellationToken)
    {
        return requestRunner.Run<SearchAllocationsQuery, ListResult<AllocationDto>>(new SearchAllocationsQuery(request), cancellationToken);
    }

    [HttpGet("allocations/{id:guid}")]
    [AzureAuthorize(Permission.BalancerView)]
    public Task<AllocationDto> Get(Guid id, CancellationToken cancellationToken)
    {
        return requestRunner.Run<GetAllocationQuery, AllocationDto>(new GetAllocationQuery(id), cancellationToken);
    }

    [HttpPost("estimates")]
    [AzureAuthorize]
    public Task<EstimateDto> Estimate([FromBody] EstimateModel model, CancellationToken cancellationToken)
    {
        return requestRunner.Run<EstimateAllocationQuery, EstimateDto>(
            new EstimateAllocationQuery(model.SkuUuid, model.Quantity, model.Cbd, model.DestinationCountry, model.PromisedDeliveryDate),
            cancellationToken);
    }

    [HttpPost("legacy-decisions")]
    [AzureAuthorize]
    [ServiceAccountOnly]
    public Task RecordLegacyDecision([FromBody] LegacyDecisionModel model, CancellationToken cancellationToken)
    {
        return requestRunner.Run(
            new RecordLegacyDecisionCommand(model.OrderId, model.OrderItemId, model.FactoryId, model.TotalCost, model.Eta),
            cancellationToken);
    }
}
