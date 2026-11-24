using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Import;
using Tenengroup.Infrastructure.Common.Exceptions;

namespace Tenengroup.FactoryCatalog.Business.Skus.Import;

internal static class ImportErrors
{
    public static ImportValidationResultDto ToResult(int totalRows, IReadOnlyCollection<ImportRowErrorDto> errors)
    {
        var invalidRows = errors.Select(e => e.Row).Distinct().Count(row => row > 0);
        return new ImportValidationResultDto(totalRows, totalRows - invalidRows, errors);
    }

    public static void ThrowIfAny(IReadOnlyCollection<ImportRowErrorDto> errors)
    {
        if (errors.Count == 0)
        {
            return;
        }

        throw new ValidationException(errors
            .GroupBy(e => e.Row > 0 ? $"row {e.Row}" : "file")
            .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToList()));
    }
}
