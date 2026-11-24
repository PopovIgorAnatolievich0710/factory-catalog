using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SQS;

namespace Tenengroup.Infrastructure.Common.Aws;

public static class AwsClientFactory
{
    public static IAmazonS3 CreateS3Client(AwsOptions options)
    {
        var config = new AmazonS3Config();
        if (string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            config.RegionEndpoint = Region(options);
        }
        else
        {
            config.ServiceURL = options.ServiceUrl;
            config.AuthenticationRegion = options.DefaultRegion;
            config.ForcePathStyle = true;
        }

        var credentials = Credentials(options);
        return credentials == null ? new AmazonS3Client(config) : new AmazonS3Client(credentials, config);
    }

    public static IAmazonSQS CreateSqsClient(AwsOptions options)
    {
        var config = new AmazonSQSConfig { RegionEndpoint = Region(options) };
        if (!string.IsNullOrWhiteSpace(options.SqsServiceUrl))
        {
            config.ServiceURL = options.SqsServiceUrl;
        }

        var credentials = Credentials(options);
        return credentials == null ? new AmazonSQSClient(config) : new AmazonSQSClient(credentials, config);
    }

    public static IAmazonSimpleNotificationService CreateSnsClient(AwsOptions options)
    {
        var config = new AmazonSimpleNotificationServiceConfig { RegionEndpoint = Region(options) };
        if (!string.IsNullOrWhiteSpace(options.SnsServiceUrl))
        {
            config.ServiceURL = options.SnsServiceUrl;
        }

        var credentials = Credentials(options);
        return credentials == null
            ? new AmazonSimpleNotificationServiceClient(config)
            : new AmazonSimpleNotificationServiceClient(credentials, config);
    }

    private static RegionEndpoint Region(AwsOptions options)
    {
        return string.IsNullOrWhiteSpace(options.DefaultRegion)
            ? RegionEndpoint.EUWest1
            : RegionEndpoint.GetBySystemName(options.DefaultRegion);
    }

    private static BasicAWSCredentials? Credentials(AwsOptions options)
    {
        return string.IsNullOrWhiteSpace(options.AccessKey) || string.IsNullOrWhiteSpace(options.SecretKey)
            ? null
            : new BasicAWSCredentials(options.AccessKey, options.SecretKey);
    }
}
