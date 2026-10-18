using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using SmartParkingSystem.Application.Common.Exceptions;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SmartParkingSystem.Api.Swagger;

/// <summary>
/// OpenAPI Operation Filter that inspects authorization metadata on controllers and actions,
/// applies JWT Bearer security requirements to Swagger, decorates endpoint descriptions
/// with role badges, and documents standard 401/403 ErrorResponse contracts.
/// </summary>
public class SwaggerAuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var authAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>()
            .Concat(context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>())
            .ToList() ?? new List<AuthorizeAttribute>();

        var allowAnonymous = context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any() == true
            || context.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any();

        if (allowAnonymous || authAttributes.Count == 0)
        {
            // Public endpoint
            operation.Security.Clear();
            operation.Summary = $"[Public] {operation.Summary}";
            operation.Description = $"{operation.Description}\n\n> 🔓 **Authentication:** Public endpoint. No JWT Bearer token required.";
            return;
        }

        // Endpoint requires authentication
        var roles = authAttributes
            .Where(a => !string.IsNullOrWhiteSpace(a.Roles))
            .Select(a => a.Roles)
            .ToList();

        var policies = authAttributes
            .Where(a => !string.IsNullOrWhiteSpace(a.Policy))
            .Select(a => a.Policy)
            .ToList();

        string requiredAuthBadge;
        if (roles.Count > 0)
        {
            requiredAuthBadge = $"Requires Role: `{string.Join(", ", roles)}`";
        }
        else if (policies.Count > 0)
        {
            requiredAuthBadge = $"Requires Policy: `{string.Join(", ", policies)}`";
        }
        else
        {
            requiredAuthBadge = "Requires Authenticated User (Any Role)";
        }

        operation.Summary = $"[🔒 {requiredAuthBadge}] {operation.Summary}";
        operation.Description = $"{operation.Description}\n\n> 🔒 **Authorization:** {requiredAuthBadge}. Enter JWT in format: `Bearer {{token}}`.";

        // Attach Bearer security requirement so Swagger UI displays lock and includes header
        var securityScheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        };

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            { securityScheme, Array.Empty<string>() }
        });

        // Ensure 401 and 403 response documentation exists
        if (!operation.Responses.ContainsKey("401"))
        {
            operation.Responses.Add("401", new OpenApiResponse
            {
                Description = "Unauthorized: Missing, invalid, or expired JWT Bearer token.",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = context.SchemaGenerator.GenerateSchema(typeof(ErrorResponse), context.SchemaRepository)
                    }
                }
            });
        }

        if (!operation.Responses.ContainsKey("403"))
        {
            operation.Responses.Add("403", new OpenApiResponse
            {
                Description = "Forbidden: The authenticated user does not have permission to access this resource.",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = context.SchemaGenerator.GenerateSchema(typeof(ErrorResponse), context.SchemaRepository)
                    }
                }
            });
        }
    }
}
