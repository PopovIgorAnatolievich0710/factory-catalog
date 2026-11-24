using Microsoft.AspNetCore.Mvc;
using Tenengroup.Balancer.Api.Models;
using Tenengroup.Balancer.Business.Abstractions.Catalog;
using Tenengroup.Balancer.Business.Abstractions.Settings;
using Tenengroup.Infrastructure.Common.Auth;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.Balancer.Api.Controllers;

[ApiController]
[Route("v1")]
[ApiExplorerSettings(GroupName = "Settings")]
public class SettingsController(IRequestRunner requestRunner) : ControllerBase
{
    [HttpGet("settings")]
    [AzureAuthorize(Permission.BalancerView)]
    public Task<BalancerSettingsDto> GetSettings(CancellationToken cancellationToken)
    {
        return requestRunner.Run<GetSettingsQuery, BalancerSettingsDto>(new GetSettingsQuery(), cancellationToken);
    }

    [HttpPut("settings")]
    [AzureAuthorize(Permission.BalancerEditReservations)]
    public Task<BalancerSettingsDto> UpdateSettings([FromBody] UpdateSettingsCommand command, CancellationToken cancellationToken)
    {
        return requestRunner.Run<UpdateSettingsCommand, BalancerSettingsDto>(command, cancellationToken);
    }

    [HttpGet("capacity-reservations")]
    [AzureAuthorize(Permission.BalancerView)]
    public Task<IReadOnlyCollection<CapacityReservationDto>> GetReservations(CancellationToken cancellationToken)
    {
        return requestRunner.Run<GetCapacityReservationsQuery, IReadOnlyCollection<CapacityReservationDto>>(
            new GetCapacityReservationsQuery(), cancellationToken);
    }

    [HttpPost("capacity-reservations")]
    [AzureAuthorize(Permission.BalancerEditReservations)]
    public Task<CapacityReservationDto> CreateReservation([FromBody] CapacityReservationModel model, CancellationToken cancellationToken)
    {
        return SaveReservation(null, model, cancellationToken);
    }

    [HttpPut("capacity-reservations/{id:long}")]
    [AzureAuthorize(Permission.BalancerEditReservations)]
    public Task<CapacityReservationDto> UpdateReservation(long id, [FromBody] CapacityReservationModel model, CancellationToken cancellationToken)
    {
        return SaveReservation(id, model, cancellationToken);
    }

    [HttpDelete("capacity-reservations/{id:long}")]
    [AzureAuthorize(Permission.BalancerEditReservations)]
    public Task DeleteReservation(long id, CancellationToken cancellationToken)
    {
        return requestRunner.Run(new DeleteCapacityReservationCommand(id), cancellationToken);
    }

    [HttpGet("quotes/{quoteKey}")]
    [AzureAuthorize(Permission.BalancerView)]
    public Task<QuoteSet> GetQuoteSet(string quoteKey, CancellationToken cancellationToken)
    {
        return requestRunner.Run<GetQuoteSetQuery, QuoteSet>(new GetQuoteSetQuery(quoteKey), cancellationToken);
    }

    [HttpPost("quotes/search")]
    [AzureAuthorize(Permission.BalancerView)]
    public Task<QuoteSearchResult> SearchQuotes([FromBody] ListRequest request, CancellationToken cancellationToken)
    {
        return requestRunner.Run<SearchQuotesQuery, QuoteSearchResult>(new SearchQuotesQuery(request), cancellationToken);
    }

    private Task<CapacityReservationDto> SaveReservation(long? id, CapacityReservationModel model, CancellationToken cancellationToken)
    {
        return requestRunner.Run<SaveCapacityReservationCommand, CapacityReservationDto>(
            new SaveCapacityReservationCommand(id, model.FactoryId, model.DateFrom.Value, model.DateTo.Value, model.ReservedPct, model.EventName),
            cancellationToken);
    }
}
