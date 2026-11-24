using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components.Alternatives;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Components.Alternatives;

public class SetAlternativeUseCommandHandler(
    IFactoryCatalogDbContext db,
    IHistoryWriter historyWriter,
    TimeProvider timeProvider) : IRequestHandler<SetAlternativeUseCommand>
{
    public async Task Handle(SetAlternativeUseCommand request, CancellationToken cancellationToken)
    {
        var alternative = await db.ComponentAlternatives.FirstOrDefaultAsync(
                              a => a.ComponentErpId == request.ErpId && a.AlternativeErpId == request.AlternativeErpId,
                              cancellationToken)
                          ?? throw new EntityNotFoundException(nameof(ComponentAlternative), request.AlternativeErpId);

        historyWriter.Record(HistoryEntityTypes.Component, request.ErpId,
            AlternativeHistoryFields.ForBalancer(alternative.AlternativeErpId), alternative.ForBalancer, request.ForBalancer);
        historyWriter.Record(HistoryEntityTypes.Component, request.ErpId,
            AlternativeHistoryFields.ForPlanner(alternative.AlternativeErpId), alternative.ForPlanner, request.ForPlanner);

        alternative.ForBalancer = request.ForBalancer;
        alternative.ForPlanner = request.ForPlanner;
        alternative.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;

        await db.SaveChangesAsync(cancellationToken);
    }
}
