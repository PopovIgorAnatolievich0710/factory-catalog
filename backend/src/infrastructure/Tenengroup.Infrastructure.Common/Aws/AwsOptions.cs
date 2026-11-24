namespace Tenengroup.Infrastructure.Common.Aws;

public class AwsOptions
{
    public const string SectionName = "Aws";

    public string? AccessKey { get; set; }

    public string? SecretKey { get; set; }

    public string? DefaultRegion { get; set; }

    public string? ServiceUrl { get; set; }

    public string? SqsServiceUrl { get; set; }

    public string? SnsServiceUrl { get; set; }
}
