using Microsoft.EntityFrameworkCore;
using Tenengroup.Balancer.Business.Abstractions;
using Tenengroup.Balancer.Business.Abstractions.Settings;
using Tenengroup.Balancer.Business.Abstractions.Users;
using Tenengroup.Balancer.DataAccess.Abstractions;
using Tenengroup.Balancer.Domains;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.Balancer.Business.Settings;

public class BalancerSettingsProvider(IBalancerDbContext db, BalancerOptions options) : IBalancerSettingsProvider
{
    public async Task<BalancerSettings> GetCurrent(CancellationToken cancellationToken)
    {
        var stored = await db.Settings
            .AsNoTracking()
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(cancellationToken);

        return stored ?? new BalancerSettings
        {
            Version = 0,
            Mode = options.Mode,
            LatePenaltyPerDay = options.LatePenaltyPerDay,
            SplitPenalty = options.SplitPenalty,
            ShareWeight = options.ShareWeight,
            SuccessRateEnabled = options.SuccessRateEnabled,
            SuccessRateWeight = options.SuccessRateWeight,
            ShareWindowDays = options.ShareWindowDays,
            ShareGapThresholdPct = options.ShareGapThresholdPct,
            DefaultProductionDays = options.DefaultProductionDays,
            UpdatedBy = "configuration"
        };
    }
}

internal static class SettingsMapper
{
    public static BalancerSettingsDto ToDto(this BalancerSettings settings)
    {
        return new BalancerSettingsDto(
            settings.Version,
            settings.Mode,
            settings.LatePenaltyPerDay,
            settings.SplitPenalty,
            settings.ShareWeight,
            settings.SuccessRateEnabled,
            settings.SuccessRateWeight,
            settings.ShareWindowDays,
            settings.ShareGapThresholdPct,
            settings.DefaultProductionDays,
            settings.UpdatedBy,
            settings.Version == 0 ? null : settings.UpdatedAt);
    }

    public static CapacityReservationDto ToDto(this CapacityReservation reservation)
    {
        return new CapacityReservationDto(
            reservation.Id,
            reservation.FactoryId,
            reservation.DateFrom,
            reservation.DateTo,
            reservation.ReservedPct,
            reservation.EventName,
            reservation.UpdatedBy,
            reservation.UpdatedAt);
    }
}

public class GetSettingsQueryHandler(IBalancerSettingsProvider settingsProvider)
    : IRequestHandler<GetSettingsQuery, BalancerSettingsDto>
{
    public async Task<BalancerSettingsDto> Handle(GetSettingsQuery request, CancellationToken cancellationToken)
    {
        return (await settingsProvider.GetCurrent(cancellationToken)).ToDto();
    }
}

public class UpdateSettingsCommandHandler(
    IBalancerDbContext db,
    IBalancerSettingsProvider settingsProvider,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<UpdateSettingsCommand, BalancerSettingsDto>
{
    public async Task<BalancerSettingsDto> Handle(UpdateSettingsCommand request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, List<string>>();
        Require(errors, "latePenaltyPerDay", request.LatePenaltyPerDay >= 0);
        Require(errors, "splitPenalty", request.SplitPenalty >= 0);
        Require(errors, "shareWeight", request.ShareWeight is >= 0 and <= 1);
        Require(errors, "successRateWeight", request.SuccessRateWeight >= 0);
        Require(errors, "shareWindowDays", request.ShareWindowDays is >= 1 and <= 90);
        Require(errors, "shareGapThresholdPct", request.ShareGapThresholdPct is >= 0 and <= 100);
        Require(errors, "defaultProductionDays", request.DefaultProductionDays is >= 0 and <= 60);

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        var current = await settingsProvider.GetCurrent(cancellationToken);
        var settings = new BalancerSettings
        {
            Version = current.Version + 1,
            Mode = request.Mode,
            LatePenaltyPerDay = request.LatePenaltyPerDay,
            SplitPenalty = request.SplitPenalty,
            ShareWeight = request.ShareWeight,
            SuccessRateEnabled = request.SuccessRateEnabled,
            SuccessRateWeight = request.SuccessRateWeight,
            ShareWindowDays = request.ShareWindowDays,
            ShareGapThresholdPct = request.ShareGapThresholdPct,
            DefaultProductionDays = request.DefaultProductionDays,
            UpdatedBy = currentUser.Actor,
            UpdatedAt = timeProvider.GetUtcNow().UtcDateTime
        };

        db.Settings.Add(settings);
        await db.SaveChangesAsync(cancellationToken);

        return settings.ToDto();
    }

    private static void Require(Dictionary<string, List<string>> errors, string field, bool isValid)
    {
        if (!isValid)
        {
            errors[field] = ["Value is out of range."];
        }
    }
}

public class GetCapacityReservationsQueryHandler(IBalancerDbContext db)
    : IRequestHandler<GetCapacityReservationsQuery, IReadOnlyCollection<CapacityReservationDto>>
{
    public async Task<IReadOnlyCollection<CapacityReservationDto>> Handle(
        GetCapacityReservationsQuery request,
        CancellationToken cancellationToken)
    {
        var reservations = await db.CapacityReservations
            .AsNoTracking()
            .OrderByDescending(r => r.DateFrom)
            .ThenBy(r => r.FactoryId)
            .ToListAsync(cancellationToken);

        return reservations.Select(r => r.ToDto()).ToList();
    }
}

public class SaveCapacityReservationCommandHandler(
    IBalancerDbContext db,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<SaveCapacityReservationCommand, CapacityReservationDto>
{
    public async Task<CapacityReservationDto> Handle(SaveCapacityReservationCommand request, CancellationToken cancellationToken)
    {
        if (request.DateTo < request.DateFrom)
        {
            throw new ValidationException("dateTo", "The end date is before the start date.");
        }

        if (request.ReservedPct is < 1 or > 100)
        {
            throw new ValidationException("reservedPct", "Reserved capacity must be between 1 and 100 %.");
        }

        CapacityReservation reservation;
        if (request.Id.HasValue)
        {
            reservation = await db.CapacityReservations.FirstOrDefaultAsync(r => r.Id == request.Id.Value, cancellationToken)
                          ?? throw new EntityNotFoundException("Capacity reservation", request.Id.Value);
        }
        else
        {
            reservation = new CapacityReservation();
            db.CapacityReservations.Add(reservation);
        }

        reservation.FactoryId = request.FactoryId;
        reservation.DateFrom = request.DateFrom;
        reservation.DateTo = request.DateTo;
        reservation.ReservedPct = request.ReservedPct;
        reservation.EventName = request.EventName;
        reservation.UpdatedBy = currentUser.Actor;
        reservation.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;

        await db.SaveChangesAsync(cancellationToken);

        return reservation.ToDto();
    }
}

public class DeleteCapacityReservationCommandHandler(IBalancerDbContext db)
    : IRequestHandler<DeleteCapacityReservationCommand>
{
    public async Task Handle(DeleteCapacityReservationCommand request, CancellationToken cancellationToken)
    {
        var reservation = await db.CapacityReservations.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
                          ?? throw new EntityNotFoundException("Capacity reservation", request.Id);

        db.CapacityReservations.Remove(reservation);
        await db.SaveChangesAsync(cancellationToken);
    }
}
