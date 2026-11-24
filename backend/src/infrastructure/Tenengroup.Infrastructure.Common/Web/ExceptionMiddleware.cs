using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Json;

namespace Tenengroup.Infrastructure.Common.Web;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (EntityNotFoundException ex)
        {
            logger.LogWarning(ex, "EntityNotFoundException on {Method} {Path}", context.Request.Method, context.Request.Path);
            await Write(context, HttpStatusCode.NotFound, new ErrorResponse(ex.Message));
        }
        catch (ConflictException ex)
        {
            await Write(context, HttpStatusCode.Conflict, new ErrorResponse(ex.Message));
        }
        catch (ForbiddenException ex)
        {
            logger.LogWarning(ex, "ForbiddenException on {Method} {Path}", context.Request.Method, context.Request.Path);
            await Write(context, HttpStatusCode.Forbidden, new ErrorResponse(ex.Message));
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "ValidationException on {Method} {Path}", context.Request.Method, context.Request.Path);
            var errors = ex.ValidationMessages.ToDictionary(e => e.Key, e => e.Value.ToArray());
            await Write(context, HttpStatusCode.UnprocessableEntity, new ErrorResponse(ex.Message, errors));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            await Write(context, HttpStatusCode.InternalServerError, new ErrorResponse($"HTTP 500 - Internal Server Error. {ex.Message}"));
        }
    }

    private static Task Write(HttpContext context, HttpStatusCode statusCode, ErrorResponse response)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(response.ToJsonString());
    }
}
