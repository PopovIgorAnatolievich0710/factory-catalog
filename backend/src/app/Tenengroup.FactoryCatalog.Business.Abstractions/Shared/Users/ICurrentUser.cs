using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;

public interface ICurrentUser
{
    string Email { get; }

    string Name { get; }

    bool IsServiceAccount { get; }

    Task<IReadOnlyCollection<string>> GetRoles(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> GetFactoryIds(CancellationToken cancellationToken = default);

    Task<bool> HasPermission(Permission permission, CancellationToken cancellationToken = default);

    Task<bool> CanEditFactory(string factoryId, CancellationToken cancellationToken = default);

    Task EnsureCanEditFactory(string factoryId, CancellationToken cancellationToken = default);
}
