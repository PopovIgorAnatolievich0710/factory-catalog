using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Factories;

public record GetFactoriesQuery : IRequest<IReadOnlyCollection<FactoryDto>>;
