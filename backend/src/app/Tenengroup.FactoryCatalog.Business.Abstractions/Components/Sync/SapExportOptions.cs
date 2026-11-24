namespace Tenengroup.FactoryCatalog.Business.Abstractions.Components.Sync;

public class SapExportOptions
{
    public const string SectionName = "SapExport";
    public const string DefaultCronExpression = "0 2 * * *";

    public string Bucket { get; set; } = string.Empty;

    public string Prefix { get; set; } = string.Empty;

    public string CronExpression { get; set; } = DefaultCronExpression;
}
