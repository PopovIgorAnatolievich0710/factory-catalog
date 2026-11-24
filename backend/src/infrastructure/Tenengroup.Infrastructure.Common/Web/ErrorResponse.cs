namespace Tenengroup.Infrastructure.Common.Web;

public record ErrorResponse(string Message, IReadOnlyDictionary<string, string[]>? Errors = null);
