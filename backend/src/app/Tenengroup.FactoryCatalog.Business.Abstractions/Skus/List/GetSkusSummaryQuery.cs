using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Skus.List;

public record GetSkusSummaryQuery : IRequest<SkusSummaryDto>;

public record SkusSummaryDto(int Total, int Dropshipper, int Invalid);
