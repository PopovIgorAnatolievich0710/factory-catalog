using Microsoft.AspNetCore.Http;
using Tenengroup.Infrastructure.Common.Exceptions;

namespace Tenengroup.FactoryCatalog.Api.Mappers.Skus;

public static class ImportFileMapper
{
    public static async Task<byte[]> ToBytes(this IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            throw new ValidationException("file", "Upload an .xlsx file in the 'file' form field.");
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        return stream.ToArray();
    }
}
