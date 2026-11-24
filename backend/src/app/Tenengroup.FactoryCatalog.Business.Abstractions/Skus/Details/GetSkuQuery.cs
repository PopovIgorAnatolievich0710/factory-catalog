using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Details;

public record GetSkuQuery(Guid SkuUuid) : IRequest<SkuDetailsDto>;
