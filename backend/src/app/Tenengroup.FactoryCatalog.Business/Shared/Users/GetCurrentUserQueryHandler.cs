using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Shared.Users;

public class GetCurrentUserQueryHandler(ICurrentUser currentUser) : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        return new CurrentUserDto(
            currentUser.Email,
            currentUser.Name,
            await currentUser.GetRoles(cancellationToken),
            await currentUser.GetFactoryIds(cancellationToken));
    }
}
