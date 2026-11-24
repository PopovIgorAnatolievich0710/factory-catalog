namespace Tenengroup.FactoryCatalog.Business.Abstractions.Processes;

public record ProcessImportRowErrorDto(int Row, string? Field, string Message);

public record ProcessImportValidationDto(int TotalRows, int ValidRows, IReadOnlyCollection<ProcessImportRowErrorDto> Errors);

public record ProcessImportResultDto(int TotalRows, int ProcessesCreated, int ProcessesUpdated, int FactoryRowsChanged);
