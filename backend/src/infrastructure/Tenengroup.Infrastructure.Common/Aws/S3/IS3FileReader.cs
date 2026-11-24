namespace Tenengroup.Infrastructure.Common.Aws.S3;

public interface IS3FileReader
{
    Task<Stream> OpenRead(string bucketName, string key, CancellationToken cancellationToken = default);

    Task<string> ReadAllText(string bucketName, string key, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<S3ObjectInfo>> List(string bucketName, string prefix, CancellationToken cancellationToken = default);
}
