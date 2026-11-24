namespace Tenengroup.FactoryCatalog.Business.Abstractions.Components;

public record ComponentFilterOptionsDto(IReadOnlyCollection<string> Families, IReadOnlyCollection<string> Materials);
