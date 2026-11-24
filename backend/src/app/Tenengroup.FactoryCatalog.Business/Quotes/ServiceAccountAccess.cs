using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.Infrastructure.Common.Exceptions;

namespace Tenengroup.FactoryCatalog.Business.Quotes;

internal static class ServiceAccountAccess
{
    public static void Ensure(ICurrentUser currentUser)
    {
        if (!currentUser.IsServiceAccount)
        {
            throw new ForbiddenException("This endpoint is available to service accounts only.");
        }
    }
}
