using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Components;

public record GetComponentFilterOptionsQuery : IRequest<ComponentFilterOptionsDto>;
