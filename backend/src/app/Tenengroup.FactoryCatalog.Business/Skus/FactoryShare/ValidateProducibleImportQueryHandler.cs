using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.FactoryShare;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Import;
using Tenengroup.FactoryCatalog.Business.Skus.Import;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Skus.FactoryShare;

public class ValidateProducibleImportQueryHandler(ProducibleImportParser parser)
    : IRequestHandler<ValidateProducibleImportQuery, ImportValidationResultDto>
{
    public async Task<ImportValidationResultDto> Handle(ValidateProducibleImportQuery request, CancellationToken cancellationToken)
    {
        var import = await parser.Parse(request.File, cancellationToken);
        return ImportErrors.ToResult(import.TotalRows, import.Errors);
    }
}
