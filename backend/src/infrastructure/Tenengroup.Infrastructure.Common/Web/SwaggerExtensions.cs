using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Tenengroup.Infrastructure.Common.Web;

public static class SwaggerExtensions
{
    private const string BasicSchemeId = "basic";
    private const string BearerSchemeId = "bearer";

    public static IServiceCollection AddTenengroupSwagger(this IServiceCollection services, string title, string version, string? xmlDocumentationPath = null)
    {
        return services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc(version, new OpenApiInfo { Title = title, Version = version });

            if (xmlDocumentationPath != null && File.Exists(xmlDocumentationPath))
            {
                c.IncludeXmlComments(xmlDocumentationPath);
            }

            c.TagActionsBy(api =>
                api.GroupName != null
                    ? [api.GroupName]
                    : [((ControllerActionDescriptor)api.ActionDescriptor).ControllerName]);
            c.DocInclusionPredicate((_, _) => true);
            c.CustomSchemaIds(type => type.FullName?.Replace('+', '.'));

            c.AddSecurityDefinition(BasicSchemeId, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "basic",
                In = ParameterLocation.Header,
                Description = "Basic authorization for machine-to-machine callers."
            });

            c.AddSecurityDefinition(BearerSchemeId, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Azure AD access token."
            });

            c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                { new OpenApiSecuritySchemeReference(BasicSchemeId, document), new List<string>() },
                { new OpenApiSecuritySchemeReference(BearerSchemeId, document), new List<string>() }
            });
        });
    }

    public static IApplicationBuilder UseTenengroupSwagger(this IApplicationBuilder app, string title, string version)
    {
        app.UseSwagger();
        return app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint($"/swagger/{version}/swagger.json", title);
            c.RoutePrefix = string.Empty;
            c.DisplayRequestDuration();
        });
    }
}
