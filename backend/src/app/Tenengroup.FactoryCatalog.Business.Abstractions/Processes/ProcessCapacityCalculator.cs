using Tenengroup.FactoryCatalog.Domains.Processes;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Processes;

public static class ProcessCapacityCalculator
{
    public static bool IsValidCapacity(int capacityPct)
    {
        return capacityPct is >= 0 and <= ProcessFactory.FullCapacityPct
               && capacityPct % ProcessFactory.CapacityStepPct == 0;
    }

    public static CapacityStatus Status(int capacityPct)
    {
        return capacityPct switch
        {
            <= 0 => CapacityStatus.Offline,
            >= ProcessFactory.FullCapacityPct => CapacityStatus.Full,
            _ => CapacityStatus.Reduced
        };
    }

    public static ProcessTodayDto? Today(decimal? maxPerDay, int capacityPct, decimal used)
    {
        if (maxPerDay is not > 0)
        {
            return null;
        }

        var available = maxPerDay.Value * capacityPct / ProcessFactory.FullCapacityPct;
        var remainingPct = Math.Max(0, (available - used) / maxPerDay.Value * 100);

        return new ProcessTodayDto(available, used, (int)Math.Round(remainingPct, MidpointRounding.AwayFromZero));
    }
}
