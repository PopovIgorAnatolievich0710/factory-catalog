using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Tenengroup.Infrastructure.Common.Web;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        string? incoming = context.Request.Headers[Constants.CorrelationIdHeaderName];
        var correlationId = string.IsNullOrWhiteSpace(incoming) ? Guid.NewGuid().ToString() : incoming;
        context.Response.Headers[Constants.CorrelationIdHeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
