using Tenengroup.FactoryCatalog.Business.Abstractions.Components.Sync;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Sync;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Components.Sync;

public class RunSapComponentsSyncCommandHandler(
    ISapComponentsSyncService syncService,
    ISyncRunTracker syncRunTracker,
    ICurrentUser currentUser) : IRequestHandler<RunSapComponentsSyncCommand, SyncStatusDto>
{
    public async Task<SyncStatusDto> Handle(RunSapComponentsSyncCommand request, CancellationToken cancellationToken)
    {
        await syncService.Run(string.IsNullOrEmpty(currentUser.Email) ? currentUser.Name : currentUser.Email, cancellationToken);

        return await syncRunTracker.GetStatus(SyncTypes.SapComponents, cancellationToken);
    }
}
