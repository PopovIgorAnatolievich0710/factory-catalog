using Tenengroup.Balancer.Domains;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.Balancer.Business.Abstractions.Settings;

public record BalancerSettingsDto(
    int Version,
    BalancerMode Mode,
    decimal LatePenaltyPerDay,
    decimal SplitPenalty,
    decimal ShareWeight,
    bool SuccessRateEnabled,
    decimal SuccessRateWeight,
    int ShareWindowDays,
    decimal ShareGapThresholdPct,
    int DefaultProductionDays,
    string UpdatedBy,
    DateTime? UpdatedAt);

public record GetSettingsQuery : IRequest<BalancerSettingsDto>;

public record UpdateSettingsCommand(
    BalancerMode Mode,
    decimal LatePenaltyPerDay,
    decimal SplitPenalty,
    decimal ShareWeight,
    bool SuccessRateEnabled,
    decimal SuccessRateWeight,
    int ShareWindowDays,
    decimal ShareGapThresholdPct,
    int DefaultProductionDays) : IRequest<BalancerSettingsDto>;

public interface IBalancerSettingsProvider
{
    Task<BalancerSettings> GetCurrent(CancellationToken cancellationToken);
}

public record CapacityReservationDto(
    long Id,
    string FactoryId,
    DateOnly DateFrom,
    DateOnly DateTo,
    int ReservedPct,
    string EventName,
    string UpdatedBy,
    DateTime UpdatedAt);

public record GetCapacityReservationsQuery : IRequest<IReadOnlyCollection<CapacityReservationDto>>;

public record SaveCapacityReservationCommand(
    long? Id,
    string FactoryId,
    DateOnly DateFrom,
    DateOnly DateTo,
    int ReservedPct,
    string EventName) : IRequest<CapacityReservationDto>;

public record DeleteCapacityReservationCommand(long Id) : IRequest;
