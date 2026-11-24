using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Components.Alternatives;

public record SetAlternativeUseCommand(string ErpId, string AlternativeErpId, bool ForBalancer, bool ForPlanner) : IRequest;
