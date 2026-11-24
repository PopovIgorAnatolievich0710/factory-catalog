using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Components.Alternatives;

public record RemoveAlternativeCommand(string ErpId, string AlternativeErpId) : IRequest;
