using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Tenengroup.FactoryCatalog.Api.Mappers.Quotes;
using Tenengroup.FactoryCatalog.Api.Models.Quotes;
using Tenengroup.FactoryCatalog.Business.Abstractions.Quotes;
using Tenengroup.Infrastructure.Common.Auth;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.FactoryCatalog.Api.Controllers.Quotes;

[ApiController]
[Route("v1/quotes")]
[ApiExplorerSettings(GroupName = "Quotes")]
public class QuotesController(IRequestRunner requestRunner) : ControllerBase
{
    [HttpPost]
    [AzureAuthorize]
    public Task<QuoteSetDto> RequestQuotes([FromBody] QuoteRequestModel model, CancellationToken cancellationToken)
    {
        return requestRunner.Run<RequestQuotesCommand, QuoteSetDto>(model.ToCommand(), cancellationToken);
    }

    [HttpGet("{quoteKey}")]
    [AzureAuthorize(Permission.BalancerView)]
    public Task<QuoteSetDto> GetQuoteSet(string quoteKey, CancellationToken cancellationToken)
    {
        return requestRunner.Run<GetQuoteSetQuery, QuoteSetDto>(new GetQuoteSetQuery(quoteKey), cancellationToken);
    }

    [HttpPost("search")]
    [AzureAuthorize(Permission.BalancerView)]
    public Task<ListResult<QuoteDetailsDto>> Search([FromBody] ListRequest request, CancellationToken cancellationToken)
    {
        return requestRunner.Run<SearchQuotesQuery, ListResult<QuoteDetailsDto>>(new SearchQuotesQuery(request), cancellationToken);
    }

    [HttpGet("dropshipper-cost")]
    [AzureAuthorize(Permission.BalancerView)]
    public Task<DropshipperCostDto> GetDropshipperCost([FromQuery, Required] string sku, CancellationToken cancellationToken)
    {
        return requestRunner.Run<GetDropshipperCostQuery, DropshipperCostDto>(new GetDropshipperCostQuery(sku), cancellationToken);
    }

    [HttpPost("{quoteId}/select")]
    [AzureAuthorize]
    public Task<QuoteDetailsDto> Select(string quoteId, CancellationToken cancellationToken)
    {
        return requestRunner.Run<SelectQuoteCommand, QuoteDetailsDto>(new SelectQuoteCommand(quoteId), cancellationToken);
    }

    [HttpPost("{quoteId}/produced")]
    [AzureAuthorize]
    public Task<QuoteDetailsDto> MarkProduced(
        string quoteId,
        [FromBody] MarkQuoteProducedModel model,
        CancellationToken cancellationToken)
    {
        return requestRunner.Run<MarkQuoteProducedCommand, QuoteDetailsDto>(model.ToCommand(quoteId), cancellationToken);
    }

    [HttpPost("{quoteId}/transferred")]
    [AzureAuthorize]
    public Task<QuoteDetailsDto> MarkTransferred(string quoteId, CancellationToken cancellationToken)
    {
        return requestRunner.Run<MarkQuoteTransferredCommand, QuoteDetailsDto>(
            new MarkQuoteTransferredCommand(quoteId), cancellationToken);
    }
}
