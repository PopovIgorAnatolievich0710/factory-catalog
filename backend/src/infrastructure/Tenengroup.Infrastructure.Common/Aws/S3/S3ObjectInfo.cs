namespace Tenengroup.Infrastructure.Common.Aws.S3;

public record S3ObjectInfo(string Key, DateTime LastModified, long Size);
