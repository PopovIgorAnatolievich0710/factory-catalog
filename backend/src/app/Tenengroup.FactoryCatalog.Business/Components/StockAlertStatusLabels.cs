using Tenengroup.FactoryCatalog.Domains.Components;

namespace Tenengroup.FactoryCatalog.Business.Components;

public static class StockAlertStatusLabels
{
    public static string ToLabel(this StockAlertStatus status)
    {
        return status switch
        {
            StockAlertStatus.Ok => "OK",
            StockAlertStatus.Low => "Low",
            StockAlertStatus.OutOfStock => "OOS",
            _ => "OOS conflict"
        };
    }
}
