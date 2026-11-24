using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.FactoryShare;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Skus.FactoryShare;

public class UpdateGlobalFactoryShareCommandHandler(
    IFactoryCatalogDbContext db,
    SkuFactoryShareService shareService,
    IHistoryWriter historyWriter,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<UpdateGlobalFactoryShareCommand, GlobalFactoryShareDto>
{
    public const string HistoryEntityId = "global";
    public const string PctField = "pct";

    public async Task<GlobalFactoryShareDto> Handle(UpdateGlobalFactoryShareCommand request, CancellationToken cancellationToken)
    {
        var factoryIds = await shareService.GetActiveFactoryIds(cancellationToken);
        Validate(request, factoryIds);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var updatedBy = string.IsNullOrEmpty(currentUser.Email) ? currentUser.Name : currentUser.Email;
        var rows = await db.GlobalFactoryShares.ToListAsync(cancellationToken);
        foreach (var share in request.FactoryShares)
        {
            var row = rows.FirstOrDefault(r => r.FactoryId == share.FactoryId);
            var oldPct = row?.Pct;
            if (oldPct == share.Pct)
            {
                continue;
            }

            if (row == null)
            {
                row = new GlobalFactoryShare { FactoryId = share.FactoryId };
                db.GlobalFactoryShares.Add(row);
            }

            historyWriter.Record(
                HistoryEntityTypes.GlobalFactoryShare, HistoryEntityId, PctField, oldPct, share.Pct, factoryId: share.FactoryId);
            row.Pct = share.Pct;
            row.UpdatedBy = updatedBy;
            row.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);

        var latest = request.FactoryShares
            .Select(s => new FactorySharePctDto(s.FactoryId, s.Pct))
            .OrderBy(s => s.FactoryId)
            .ToList();

        return new GlobalFactoryShareDto(latest, latest.Sum(s => s.Pct), updatedBy, now);
    }

    private static void Validate(UpdateGlobalFactoryShareCommand request, IReadOnlyList<string> factoryIds)
    {
        var errors = new Dictionary<string, List<string>>();
        void Add(string message)
        {
            if (!errors.TryGetValue("factoryShares", out var messages))
            {
                errors["factoryShares"] = messages = [];
            }

            messages.Add(message);
        }

        var requestedIds = request.FactoryShares.Select(s => s.FactoryId).ToList();
        foreach (var unknown in requestedIds.Except(factoryIds))
        {
            Add($"Factory {unknown} is not an active factory.");
        }

        foreach (var missing in factoryIds.Except(requestedIds))
        {
            Add($"Share of factory {missing} is missing.");
        }

        if (requestedIds.Distinct().Count() != requestedIds.Count)
        {
            Add("Each factory can be listed only once.");
        }

        if (request.FactoryShares.Any(s => s.Pct < 0 || s.Pct > FactoryShareCalculator.TotalPct
                                           || decimal.Round(s.Pct, FactoryShareCalculator.Decimals) != s.Pct))
        {
            Add("Each share must be between 0 and 100 with at most 2 decimals.");
        }

        var total = request.FactoryShares.Sum(s => s.Pct);
        if (total != FactoryShareCalculator.TotalPct)
        {
            Add($"Shares must total 100%, the total is {total:0.##}%.");
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}
