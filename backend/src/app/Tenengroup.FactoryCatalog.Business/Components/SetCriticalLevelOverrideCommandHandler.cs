using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Components;

public class SetCriticalLevelOverrideCommandHandler(IFactoryCatalogDbContext db, IHistoryWriter historyWriter)
    : IRequestHandler<SetCriticalLevelOverrideCommand>
{
    public async Task Handle(SetCriticalLevelOverrideCommand request, CancellationToken cancellationToken)
    {
        if (request.Level < 0)
        {
            throw new ValidationException("level", "The critical level cannot be negative.");
        }

        if (!await db.Components.AnyAsync(c => c.ErpId == request.ErpId, cancellationToken))
        {
            throw new EntityNotFoundException(nameof(Component), request.ErpId);
        }

        if (!await db.Factories.AnyAsync(f => f.Id == request.FactoryId, cancellationToken))
        {
            throw new EntityNotFoundException(nameof(Factory), request.FactoryId);
        }

        var row = await db.ComponentFactories.FirstOrDefaultAsync(
            f => f.ComponentErpId == request.ErpId && f.FactoryId == request.FactoryId, cancellationToken);

        if (row == null)
        {
            if (request.Level == null)
            {
                return;
            }

            row = new ComponentFactory { ComponentErpId = request.ErpId, FactoryId = request.FactoryId };
            db.ComponentFactories.Add(row);
        }

        historyWriter.Record(HistoryEntityTypes.Component, request.ErpId, "criticalStockLevelOverride",
            row.CriticalStockLevelOverride, request.Level, factoryId: request.FactoryId);

        row.CriticalStockLevelOverride = request.Level;

        await db.SaveChangesAsync(cancellationToken);
    }
}
