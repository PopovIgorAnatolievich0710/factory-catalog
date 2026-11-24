namespace Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Sync;

public class MasterCatalogOptions
{
    public const string SectionName = "MasterCatalog";
    public const string DefaultCronExpression = "0 3 * * *";

    public string BaseUrl { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string CronExpression { get; set; } = DefaultCronExpression;
}
