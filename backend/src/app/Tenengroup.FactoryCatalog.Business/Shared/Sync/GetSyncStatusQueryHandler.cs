using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Sync;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Shared.Sync;

public class GetSyncStatusQueryHandler(ISyncRunTracker tracker) : IRequestHandler<GetSyncStatusQuery, SyncStatusDto>
{
    public Task<SyncStatusDto> Handle(GetSyncStatusQuery request, CancellationToken cancellationToken)
    {
        return tracker.GetStatus(request.SyncType, cancellationToken);
    }
}
