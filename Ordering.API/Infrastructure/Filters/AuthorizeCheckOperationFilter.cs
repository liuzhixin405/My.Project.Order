using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Ordering.API.Infrastructure.Filters
{
    public class AuthorizeCheckOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var hasAuthorize = context.MethodInfo.DeclaringType.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() ||
                context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any();
            if (!hasAuthorize) return;
            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "未授权" });
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "禁止" });
            var oAuthScheme = new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
            };
            operation.Security = new List<OpenApiSecurityRequirement>
            {

                new OpenApiSecurityRequirement
                {
                    [oAuthScheme] = new []{"orderingapi"}
                }
            };
        }
    }
}