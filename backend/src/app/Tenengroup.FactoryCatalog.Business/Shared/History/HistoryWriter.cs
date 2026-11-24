using System.Globalization;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Auth;

namespace Tenengroup.FactoryCatalog.Business.Shared.History;

public class HistoryWriter(IFactoryCatalogDbContext db, IUserIdentity identity, TimeProvider timeProvider) : IHistoryWriter
{
    public void Record(
        string entityType,
        string entityId,
        string field,
        object oldValue,
        object newValue,
        HistorySource source = HistorySource.User,
        string factoryId = null,
        string changedBy = null)
    {
        var oldText = Format(oldValue);
        var newText = Format(newValue);

        if (oldText == newText)
        {
            return;
        }

        db.HistoryEntries.Add(new HistoryEntry
        {
            EntityType = entityType,
            EntityId = entityId,
            FactoryId = factoryId,
            Field = field,
            OldValue = oldText,
            NewValue = newText,
            ChangedBy = changedBy ?? (string.IsNullOrEmpty(identity.Email) ? identity.Name : identity.Email),
            ChangedAt = timeProvider.GetUtcNow().UtcDateTime,
            Source = source
        });
    }

    private static string Format(object value)
    {
        return value switch
        {
            null => null,
            string text => text,
            bool flag => flag ? "true" : "false",
            DateTime dateTime => dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            decimal number => number.ToString("0.####", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }
}
