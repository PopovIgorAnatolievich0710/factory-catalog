namespace Tenengroup.Balancer.Business.Abstractions.Users;

public interface ICurrentUser
{
    string Actor { get; }

    Task<bool> CanAccessAnyFactory(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<string>> GetFactoryIds(CancellationToken cancellationToken);

    Task<bool> CanAccessFactory(string factoryId, CancellationToken cancellationToken);

    Task EnsureCanAccessFactory(string factoryId, CancellationToken cancellationToken);
}
