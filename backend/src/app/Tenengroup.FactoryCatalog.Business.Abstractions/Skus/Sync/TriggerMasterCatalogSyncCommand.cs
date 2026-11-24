using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Sync;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Sync;

public record TriggerMasterCatalogSyncCommand : IRequest<SyncStatusDto>;
