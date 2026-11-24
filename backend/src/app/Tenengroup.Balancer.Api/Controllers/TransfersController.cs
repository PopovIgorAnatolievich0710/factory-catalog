using Microsoft.AspNetCore.Mvc;
using Tenengroup.Balancer.Business.Abstractions.Transfers;
using Tenengroup.Infrastructure.Common.Auth;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.Balancer.Api.Controllers;

[ApiController]
[Route("v1/transfers")]
[ApiExplorerSettings(GroupName = "Transfers")]
public class TransfersController(IRequestRunner requestRunner) : ControllerBase
{
    [HttpPost("open-items/search")]
    [AzureAuthorize(Permission.BalancerView)]
    public Task<ListResult<OpenItemDto>> SearchOpenItems([FromBody] ListRequest request, CancellationToken cancellationToken)
    {
        return requestRunner.Run<SearchOpenItemsQuery, ListResult<OpenItemDto>>(new SearchOpenItemsQuery(request), cancellationToken);
    }

    [HttpPost("open-items/ids")]
    [AzureAuthorize(Permission.BalancerTransfer)]
    public Task<IReadOnlyCollection<Guid>> GetOpenItemIds([FromBody] ListRequest request, CancellationToken cancellationToken)
    {
        return requestRunner.Run<GetOpenItemIdsQuery, IReadOnlyCollection<Guid>>(new GetOpenItemIdsQuery(request), cancellationToken);
    }

    [HttpGet("pages")]
    [AzureAuthorize(Permission.BalancerView)]
    public Task<IReadOnlyCollection<string>> GetPages([FromQuery] string factoryId, CancellationToken cancellationToken)
    {
        return requestRunner.Run<GetProductionPagesQuery, IReadOnlyCollection<string>>(new GetProductionPagesQuery(factoryId), cancellationToken);
    }

    [HttpPost("preview")]
    [AzureAuthorize(Permission.BalancerTransfer)]
    public Task<IReadOnlyCollection<TransferItemDto>> Preview([FromBody] TransferRequest request, CancellationToken cancellationToken)
    {
        return requestRunner.Run<PreviewTransferQuery, IReadOnlyCollection<TransferItemDto>>(new PreviewTransferQuery(request), cancellationToken);
    }

    [HttpPost]
    [AzureAuthorize(Permission.BalancerTransfer)]
    public Task<TransferResultDto> Transfer([FromBody] TransferRequest request, CancellationToken cancellationToken)
    {
        return requestRunner.Run<TransferItemsCommand, TransferResultDto>(new TransferItemsCommand(request), cancellationToken);
    }

    [HttpPost("search")]
    [AzureAuthorize(Permission.BalancerView)]
    public Task<ListResult<TransferLogDto>> SearchLog([FromBody] ListRequest request, CancellationToken cancellationToken)
    {
        return requestRunner.Run<SearchTransfersQuery, ListResult<TransferLogDto>>(new SearchTransfersQuery(request), cancellationToken);
    }
}
