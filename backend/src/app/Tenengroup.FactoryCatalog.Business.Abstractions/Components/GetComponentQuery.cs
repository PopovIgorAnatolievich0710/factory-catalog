using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Components;

public record GetComponentQuery(string ErpId) : IRequest<ComponentDetailsDto>;
