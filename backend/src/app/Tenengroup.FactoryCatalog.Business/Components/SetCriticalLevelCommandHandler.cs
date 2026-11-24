using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Components;

public class SetCriticalLevelCommandHandler(
    IFactoryCatalogDbContext db,
    IHistoryWriter historyWriter,
    TimeProvider timeProvider) : IRequestHandler<SetCriticalLevelCommand>
{
    public async Task Handle(SetCriticalLevelCommand request, CancellationToken cancellationToken)
    {
        if (request.Level < 0)
        {
            throw new ValidationException("level", "The critical level cannot be negative.");
        }

        var component = await db.Components.FirstOrDefaultAsync(c => c.ErpId == request.ErpId, cancellationToken)
                        ?? throw new EntityNotFoundException(nameof(Component), request.ErpId);

        historyWriter.Record(HistoryEntityTypes.Component, component.ErpId, "criticalStockLevel",
            component.CriticalStockLevel, request.Level);

        component.CriticalStockLevel = request.Level;
        component.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;

        await db.SaveChangesAsync(cancellationToken);
    }
}
