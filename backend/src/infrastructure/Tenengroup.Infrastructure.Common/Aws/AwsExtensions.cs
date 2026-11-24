using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tenengroup.Infrastructure.Common.Aws.S3;
using Tenengroup.Infrastructure.Common.Aws.Sns;
using Tenengroup.Infrastructure.Common.Aws.Sqs;

namespace Tenengroup.Infrastructure.Common.Aws;

public static class AwsExtensions
{
    public static IServiceCollection AddAwsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(AwsOptions.SectionName).Get<AwsOptions>() ?? new AwsOptions();

        services.AddSingleton(_ => AwsClientFactory.CreateS3Client(options));
        services.AddSingleton(_ => AwsClientFactory.CreateSqsClient(options));
        services.AddSingleton(_ => AwsClientFactory.CreateSnsClient(options));

        services.AddScoped<IS3FileReader, S3FileReader>();
        services.AddScoped<IMessageReader, MessageReader>();
        services.AddScoped<IQueueReader, QueueReader>();
        services.AddScoped<IEventPublisher, SnsEventPublisher>();

        return services;
    }
}
