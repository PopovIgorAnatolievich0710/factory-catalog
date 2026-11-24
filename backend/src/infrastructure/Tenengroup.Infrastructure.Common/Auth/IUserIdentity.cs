namespace Tenengroup.Infrastructure.Common.Auth;

public interface IUserIdentity
{
    bool IsServiceAccount { get; }

    string Email { get; }

    string Name { get; }

    IReadOnlyCollection<string> Groups { get; }
}
