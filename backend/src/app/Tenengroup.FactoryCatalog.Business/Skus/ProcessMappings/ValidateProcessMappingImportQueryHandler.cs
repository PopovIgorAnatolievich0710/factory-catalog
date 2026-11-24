using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Import;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.ProcessMappings;
using Tenengroup.FactoryCatalog.Business.Skus.Import;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Skus.ProcessMappings;

public class ValidateProcessMappingImportQueryHandler(ProcessMappingImportParser parser)
    : IRequestHandler<ValidateProcessMappingImportQuery, ImportValidationResultDto>
{
    public async Task<ImportValidationResultDto> Handle(ValidateProcessMappingImportQuery request, CancellationToken cancellationToken)
    {
        var import = await parser.Parse(request.File, cancellationToken);
        return ImportErrors.ToResult(import.TotalRows, import.Errors);
    }
}
