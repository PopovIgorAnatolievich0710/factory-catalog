using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Components;

public class SetManualOosCommandHandler(
    IFactoryCatalogDbContext db,
    ICurrentUser currentUser,
    IHistoryWriter historyWriter,
    TimeProvider timeProvider) : IRequestHandler<SetManualOosCommand>
{
    public const int MaxNoteLength = 1000;

    public async Task Handle(SetManualOosCommand request, CancellationToken cancellationToken)
    {
        await currentUser.EnsureCanEditFactory(request.FactoryId, cancellationToken);

        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        if (note is { Length: > MaxNoteLength })
        {
            throw new ValidationException("note", $"The note can be at most {MaxNoteLength} characters long.");
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
            if (!request.Active)
            {
                return;
            }

            row = new ComponentFactory { ComponentErpId = request.ErpId, FactoryId = request.FactoryId };
            db.ComponentFactories.Add(row);
        }

        var newNote = request.Active ? note : null;

        historyWriter.Record(HistoryEntityTypes.Component, request.ErpId, "manualOos",
            row.ManualOosActive, request.Active, factoryId: request.FactoryId);
        historyWriter.Record(HistoryEntityTypes.Component, request.ErpId, "manualOosNote",
            row.ManualOosNote, newNote, factoryId: request.FactoryId);

        if (request.Active && !row.ManualOosActive)
        {
            row.ManualOosSetBy = string.IsNullOrEmpty(currentUser.Name) ? currentUser.Email : currentUser.Name;
            row.ManualOosSetAt = timeProvider.GetUtcNow().UtcDateTime;
        }

        if (!request.Active)
        {
            row.ManualOosSetBy = null;
            row.ManualOosSetAt = null;
        }

        row.ManualOosActive = request.Active;
        row.ManualOosNote = newNote;

        await db.SaveChangesAsync(cancellationToken);
    }
}
