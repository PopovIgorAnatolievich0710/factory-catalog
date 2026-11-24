using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components.Alternatives;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Components.Alternatives;

public class RemoveAlternativeCommandHandler(IFactoryCatalogDbContext db, IHistoryWriter historyWriter)
    : IRequestHandler<RemoveAlternativeCommand>
{
    public async Task Handle(RemoveAlternativeCommand request, CancellationToken cancellationToken)
    {
        var alternative = await db.ComponentAlternatives.FirstOrDefaultAsync(
                              a => a.ComponentErpId == request.ErpId && a.AlternativeErpId == request.AlternativeErpId,
                              cancellationToken)
                          ?? throw new EntityNotFoundException(nameof(ComponentAlternative), request.AlternativeErpId);

        db.ComponentAlternatives.Remove(alternative);
        historyWriter.Record(HistoryEntityTypes.Component, request.ErpId, "alternative", alternative.AlternativeErpId, null);

        await db.SaveChangesAsync(cancellationToken);
    }
}
