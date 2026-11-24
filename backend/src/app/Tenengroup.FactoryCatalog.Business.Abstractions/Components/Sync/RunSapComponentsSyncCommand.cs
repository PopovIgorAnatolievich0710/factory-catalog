using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Sync;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Components.Sync;

public record RunSapComponentsSyncCommand : IRequest<SyncStatusDto>;
