using Microsoft.AspNetCore.Authorization;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.Infrastructure.Common.Auth;

public class AzureAuthenticatedRequirement(Permission? permission = null) : IAuthorizationRequirement
{
    public Permission? Permission { get; } = permission;
}
