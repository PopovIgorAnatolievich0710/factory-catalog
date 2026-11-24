using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Import;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Skus.ProcessMappings;

public record ValidateProcessMappingImportQuery(byte[] File) : IRequest<ImportValidationResultDto>;

public record ImportProcessMappingsCommand(byte[] File) : IRequest<ImportValidationResultDto>;
