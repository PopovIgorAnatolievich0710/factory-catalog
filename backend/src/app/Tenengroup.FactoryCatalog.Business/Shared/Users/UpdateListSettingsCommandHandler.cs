using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Shared.Users;

public class UpdateListSettingsCommandHandler(
    IFactoryCatalogDbContext db,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<UpdateListSettingsCommand, ListSettingsDto>
{
    public async Task<ListSettingsDto> Handle(UpdateListSettingsCommand request, CancellationToken cancellationToken)
    {
        var columns = (request.Columns ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (columns.Count > UserListSettings.MaxColumns)
        {
            throw new ValidationException("columns", $"At most {UserListSettings.MaxColumns} columns can be added.");
        }

        var email = currentUser.Email;
        if (string.IsNullOrEmpty(email))
        {
            throw new ValidationException("user", "List settings are stored per user and need a user email.");
        }

        var settings = await db.UserListSettings
            .FirstOrDefaultAsync(s => s.UserEmail == email && s.ListKey == request.ListKey, cancellationToken);

        if (settings == null)
        {
            settings = new UserListSettings { UserEmail = email, ListKey = request.ListKey };
            db.UserListSettings.Add(settings);
        }

        settings.Columns = columns;
        settings.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;

        await db.SaveChangesAsync(cancellationToken);

        return new ListSettingsDto(columns);
    }
}
