using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace Tenengroup.Infrastructure.Common.Web;

public static class WebExtensions
{
    public const string CorsPolicyName = "corsapp";

    public static IMvcBuilder AddTenengroupControllers(this IServiceCollection services)
    {
        return services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(e => e.Value?.Errors.Count > 0)
                        .ToDictionary(e => e.Key, e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray());

                    return new UnprocessableEntityObjectResult(new ErrorResponse("Validation failed", errors));
                };
            });
    }

    public static IServiceCollection AddTenengroupCors(this IServiceCollection services, IConfiguration configuration)
    {
        var configured = configuration[Constants.ParameterNames.CorsAllowedHosts];
        var origins = string.IsNullOrWhiteSpace(configured)
            ? ["*"]
            : configured.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return services.AddCors(options => options.AddPolicy(CorsPolicyName, builder => builder
            .WithOrigins(origins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders(HeaderNames.ContentDisposition, Constants.CorrelationIdHeaderName)));
    }

    public static IApplicationBuilder UsePing(this IApplicationBuilder app)
    {
        return app.Map("/ping", x => x.Run(context => context.Response.WriteAsync("pong")));
    }

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }

    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionMiddleware>();
    }
}
