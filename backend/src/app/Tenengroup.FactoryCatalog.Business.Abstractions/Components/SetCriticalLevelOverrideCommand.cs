using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Components;

public record SetCriticalLevelOverrideCommand(string ErpId, string FactoryId, decimal? Level) : IRequest;
