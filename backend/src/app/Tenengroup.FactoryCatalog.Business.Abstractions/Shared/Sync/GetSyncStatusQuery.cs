using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Sync;

public record GetSyncStatusQuery(string SyncType) : IRequest<SyncStatusDto>;
