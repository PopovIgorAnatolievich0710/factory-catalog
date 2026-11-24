using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Components;

public record SetCriticalLevelCommand(string ErpId, decimal? Level) : IRequest;
