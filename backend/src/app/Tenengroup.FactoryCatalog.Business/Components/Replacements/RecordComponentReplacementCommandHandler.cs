using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components.Replacements;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Components.Replacements;

public class RecordComponentReplacementCommandHandler(
    IFactoryCatalogDbContext db,
    IHistoryWriter historyWriter,
    TimeProvider timeProvider) : IRequestHandler<RecordComponentReplacementCommand, ComponentReplacementDto>
{
    public async Task<ComponentReplacementDto> Handle(
        RecordComponentReplacementCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
        {
            throw new ValidationException("quantity", "The quantity must be greater than zero.");
        }

        if (!await db.Factories.AnyAsync(f => f.Id == request.FactoryId, cancellationToken))
        {
            throw new ValidationException("factoryId", $"Factory '{request.FactoryId}' does not exist.");
        }

        var isApproved = await db.ComponentAlternatives.AnyAsync(
            a => a.ComponentErpId == request.OriginalErpId
                 && a.AlternativeErpId == request.ReplacementErpId
                 && a.ForPlanner,
            cancellationToken);
        if (!isApproved)
        {
            throw new ValidationException(
                "replacementErpId",
                $"'{request.ReplacementErpId}' is not an approved S.C. planner alternative of '{request.OriginalErpId}'.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var replacement = new ComponentReplacement
        {
            OrderId = request.OrderId,
            OrderItemId = request.OrderItemId,
            FactoryId = request.FactoryId,
            OriginalErpId = request.OriginalErpId,
            ReplacementErpId = request.ReplacementErpId,
            Quantity = request.Quantity,
            ReplacedBy = request.ReplacedBy,
            ReplacedAt = request.ReplacedAt?.ToUniversalTime() ?? now,
            CreatedAt = now
        };
        db.ComponentReplacements.Add(replacement);

        historyWriter.Record(HistoryEntityTypes.Component, request.OriginalErpId,
            $"replacement in order {request.OrderId} item {request.OrderItemId}", null, request.ReplacementErpId,
            HistorySource.System, request.FactoryId, request.ReplacedBy);

        await db.SaveChangesAsync(cancellationToken);

        return new ComponentReplacementDto(
            replacement.Id,
            replacement.OrderId,
            replacement.OrderItemId,
            replacement.FactoryId,
            replacement.OriginalErpId,
            replacement.ReplacementErpId,
            replacement.Quantity,
            replacement.ReplacedBy,
            replacement.ReplacedAt);
    }
}
