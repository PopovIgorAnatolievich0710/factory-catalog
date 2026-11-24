using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components.Alternatives;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Components.Alternatives;

public class AddAlternativeCommandHandler(
    IFactoryCatalogDbContext db,
    ICurrentUser currentUser,
    IHistoryWriter historyWriter,
    TimeProvider timeProvider) : IRequestHandler<AddAlternativeCommand>
{
    public async Task Handle(AddAlternativeCommand request, CancellationToken cancellationToken)
    {
        if (!await db.Components.AnyAsync(c => c.ErpId == request.ErpId, cancellationToken))
        {
            throw new EntityNotFoundException(nameof(Component), request.ErpId);
        }

        if (string.Equals(request.ErpId, request.AlternativeErpId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("alternativeErpId", "A component cannot be its own alternative.");
        }

        var alternative = await db.Components.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ErpId == request.AlternativeErpId, cancellationToken);
        if (alternative is not { Status: ComponentStatus.Active })
        {
            throw new ValidationException("alternativeErpId", "The alternative must be an active ERP ID in SAP.");
        }

        if (await db.ComponentAlternatives.AnyAsync(
                a => a.ComponentErpId == request.ErpId && a.AlternativeErpId == alternative.ErpId, cancellationToken))
        {
            throw new ConflictException($"'{alternative.ErpId}' is already an alternative of '{request.ErpId}'");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        db.ComponentAlternatives.Add(new ComponentAlternative
        {
            ComponentErpId = request.ErpId,
            AlternativeErpId = alternative.ErpId,
            ForBalancer = request.ForBalancer,
            ForPlanner = request.ForPlanner,
            CreatedBy = string.IsNullOrEmpty(currentUser.Email) ? currentUser.Name : currentUser.Email,
            CreatedAt = now,
            UpdatedAt = now
        });

        historyWriter.Record(HistoryEntityTypes.Component, request.ErpId, "alternative", null, alternative.ErpId);
        historyWriter.Record(HistoryEntityTypes.Component, request.ErpId,
            AlternativeHistoryFields.ForBalancer(alternative.ErpId), null, request.ForBalancer);
        historyWriter.Record(HistoryEntityTypes.Component, request.ErpId,
            AlternativeHistoryFields.ForPlanner(alternative.ErpId), null, request.ForPlanner);

        await db.SaveChangesAsync(cancellationToken);
    }
}
