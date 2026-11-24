using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Shared.Users;

public class GetListSettingsQueryHandler(IFactoryCatalogDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetListSettingsQuery, ListSettingsDto>
{
    public async Task<ListSettingsDto> Handle(GetListSettingsQuery request, CancellationToken cancellationToken)
    {
        var email = currentUser.Email;
        var settings = await db.UserListSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserEmail == email && s.ListKey == request.ListKey, cancellationToken);

        return new ListSettingsDto(settings?.Columns ?? []);
    }
}
