namespace Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Import;

public record ImportValidationResultDto(int TotalRows, int ValidRows, IReadOnlyCollection<ImportRowErrorDto> Errors);

public record ImportRowErrorDto(int Row, string? Field, string Message);
