using Amazon.S3;
using Amazon.S3.Model;

namespace Tenengroup.Infrastructure.Common.Aws.S3;

public class S3FileReader(IAmazonS3 s3Client) : IS3FileReader
{
    public async Task<Stream> OpenRead(string bucketName, string key, CancellationToken cancellationToken = default)
    {
        using var response = await s3Client.GetObjectAsync(bucketName, key, cancellationToken);
        var memoryStream = new MemoryStream();
        await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task<string> ReadAllText(string bucketName, string key, CancellationToken cancellationToken = default)
    {
        using var response = await s3Client.GetObjectAsync(bucketName, key, cancellationToken);
        using var reader = new StreamReader(response.ResponseStream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<S3ObjectInfo>> List(string bucketName, string prefix, CancellationToken cancellationToken = default)
    {
        var objects = new List<S3ObjectInfo>();
        var request = new ListObjectsV2Request { BucketName = bucketName, Prefix = prefix };

        ListObjectsV2Response response;
        do
        {
            response = await s3Client.ListObjectsV2Async(request, cancellationToken);
            objects.AddRange((response.S3Objects ?? [])
                .Where(o => o.Size > 0)
                .Select(o => new S3ObjectInfo(o.Key, o.LastModified, o.Size)));
            request.ContinuationToken = response.NextContinuationToken;
        }
        while (response.IsTruncated);

        return objects;
    }
}
