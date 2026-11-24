namespace Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;

public record CurrentUserDto(
    string Email,
    string Name,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> FactoryIds);
