using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SmartParkingSystem.Api.Middleware;
using SmartParkingSystem.Application;
using SmartParkingSystem.Application.Common.Exceptions;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Infrastructure;
using SmartParkingSystem.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Register Layer Services (Clean Architecture DI)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 2. Controller Configuration with String Enum Conversion & Standard Error Contract
builder.Services.AddControllers(options =>
{
    options.Filters.Add<StandardErrorResultFilter>();
})
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => !string.IsNullOrEmpty(e.ErrorMessage) ? e.ErrorMessage : e.Exception?.Message ?? "Invalid value.")
                .Where(msg => !string.IsNullOrWhiteSpace(msg))
                .ToList();

            var errorResponse = new ErrorResponse
            {
                Success = false,
                Message = "One or more validation errors occurred.",
                Errors = errors
            };

            return new BadRequestObjectResult(errorResponse);
        };
    });

// 3. CORS Configuration for Angular Frontend
const string corsPolicyName = "AllowAngularApp";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                     ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicyName, policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 4. JWT Authentication Configuration
var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"] 
    ?? throw new InvalidOperationException("JwtSettings:SecretKey is required.");
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "SmartParkingSystem";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "SmartParkingSystemUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSecretKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        NameClaimType = System.Security.Claims.ClaimTypes.Name
    };
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            var payload = new ErrorResponse
            {
                Success = false,
                Message = "You are not authorized to access this resource. Authentication is required.",
                Errors = new List<string>()
            };
            await context.Response.WriteAsJsonAsync(payload);
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            var payload = new ErrorResponse
            {
                Success = false,
                Message = "You do not have permission to access this resource.",
                Errors = new List<string>()
            };
            await context.Response.WriteAsJsonAsync(payload);
        }
    };
});

// 5. Role-Based Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(SmartParkingSystem.Application.Common.Security.AuthorizationPolicies.RequireAdmin, policy =>
        policy.RequireRole(SmartParkingSystem.Application.Common.Security.AppRoles.Admin));

    options.AddPolicy(SmartParkingSystem.Application.Common.Security.AuthorizationPolicies.RequireAttendant, policy =>
        policy.RequireRole(SmartParkingSystem.Application.Common.Security.AppRoles.ParkingAttendant));

    options.AddPolicy(SmartParkingSystem.Application.Common.Security.AuthorizationPolicies.RequireCustomer, policy =>
        policy.RequireRole(SmartParkingSystem.Application.Common.Security.AppRoles.Customer));

    options.AddPolicy(SmartParkingSystem.Application.Common.Security.AuthorizationPolicies.RequireStaff, policy =>
        policy.RequireRole(
            SmartParkingSystem.Application.Common.Security.AppRoles.Admin,
            SmartParkingSystem.Application.Common.Security.AppRoles.ParkingAttendant));
});

// 6. Swagger / OpenAPI Documentation with JWT Support & Rich Frontend Developer Experience
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Smart Parking Reservation System API",
        Version = "v1",
        Description = @"## Production-Grade RESTful Web API for Smart Parking Reservation System

### 🔐 Role-Based Access Control (RBAC) & Authentication
- **Public**: Registration, Login, Token Refresh, Health Checks.
- **Customer**: Manage personal vehicles, book parking reservations, process payments, view QR codes, and personal dashboard.
- **ParkingAttendant**: Scan QR codes, process vehicle check-in and check-out, and view attendant operational dashboard.
- **Admin**: Full system administration, user management, facility/slot configuration, and comprehensive analytics reports.

### 🚀 Quick Start for Frontend Developers
1. Obtain an access token via `POST /api/auth/login` using pre-seeded accounts:
   - **Admin**: `admin@smartparking.com` / `Admin@123456`
   - **Attendant**: `attendant@smartparking.com` / `Attendant@123456`
   - **Customer**: `customer@smartparking.com` / `Customer@123456`
2. Click the green **Authorize** button (top right) and enter your token as:
   `Bearer <your_jwt_token>`
3. All error responses conform strictly to the consistent contract:
   `{ ""success"": false, ""message"": ""..."", ""errors"": [] }`
",
        Contact = new OpenApiContact
        {
            Name = "Smart Parking System Team",
            Email = "support@smartparking.com"
        }
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token format: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Custom Filters for Role Badging and Tag Descriptions
    options.OperationFilter<SmartParkingSystem.Api.Swagger.SwaggerAuthorizeOperationFilter>();
    options.DocumentFilter<SmartParkingSystem.Api.Swagger.SwaggerDocumentFilter>();

    // Include XML Documentation Comments if available
    var apiXmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var apiXmlPath = Path.Combine(AppContext.BaseDirectory, apiXmlFile);
    if (File.Exists(apiXmlPath))
    {
        options.IncludeXmlComments(apiXmlPath);
    }

    var appXmlFile = "SmartParkingSystem.Application.xml";
    var appXmlPath = Path.Combine(AppContext.BaseDirectory, appXmlFile);
    if (File.Exists(appXmlPath))
    {
        options.IncludeXmlComments(appXmlPath);
    }

    // Grouping APIs strictly by the 11 required categories
    options.TagActionsBy(apiDesc =>
    {
        var tagsAttr = apiDesc.ActionDescriptor.EndpointMetadata.OfType<TagsAttribute>().FirstOrDefault();
        if (tagsAttr != null && tagsAttr.Tags.Count > 0)
        {
            return tagsAttr.Tags.ToList();
        }

        var route = apiDesc.RelativePath?.ToLowerInvariant() ?? string.Empty;

        if (route.StartsWith("api/auth") || route.StartsWith("api/health"))
            return new[] { "Authentication" };

        if (route.StartsWith("api/users") || route.StartsWith("api/admin/users"))
            return new[] { "Users" };

        if (route.StartsWith("api/vehicles") || route.StartsWith("api/customer/vehicles") || route.StartsWith("api/customer/my-vehicles"))
            return new[] { "Vehicles" };

        if (route.StartsWith("api/parking-areas") || route.StartsWith("api/admin/parking-areas") || route.StartsWith("api/customer/parking-areas") || route.StartsWith("api/attendant/facility") || route.StartsWith("api/attendant/parking-areas") || route.StartsWith("api/attendant/occupancy"))
            return new[] { "Parking Areas" };

        if (route.StartsWith("api/parking-slots") || route.StartsWith("api/admin/parking-slots"))
            return new[] { "Parking Slots" };

        if (route.StartsWith("api/reservations") || route.StartsWith("api/customer/my-reservations") || route.StartsWith("api/attendant/active-reservations"))
            return new[] { "Reservations" };

        if (route.StartsWith("api/payments") || route.StartsWith("api/customer/my-payments"))
            return new[] { "Payments" };

        if (route.StartsWith("api/qr-codes") || route.StartsWith("api/attendant/scan-qr"))
            return new[] { "QR Codes" };

        if (route.StartsWith("api/parking") || route.StartsWith("api/parking-history") || route.StartsWith("api/customer/my-history") || route.StartsWith("api/customer/my-parking-history") || route.StartsWith("api/attendant/parking-history") || route.StartsWith("api/attendant/check-in") || route.StartsWith("api/attendant/check-out"))
            return new[] { "Parking" };

        if (route.StartsWith("api/dashboard"))
            return new[] { "Dashboard" };

        if (route.StartsWith("api/reports") || route.StartsWith("api/admin/reports"))
            return new[] { "Reports" };

        return new[] { "Authentication" };
    });

    options.OrderActionsBy(apiDesc => $"{apiDesc.ActionDescriptor.RouteValues["controller"]}_{apiDesc.HttpMethod}");
});

var app = builder.Build();

// 5. Automatic Database Migration and Idempotent Seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();

        logger.LogInformation("Applying pending database migrations...");
        await dbContext.Database.MigrateAsync();

        logger.LogInformation("Seeding initial data (Roles, Admin, Attendant, Customer, Parking Area, Slots)...");
        await DatabaseSeeder.SeedAsync(dbContext, passwordHasher, app.Configuration);
        logger.LogInformation("Database migration and seeding completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
        throw;
    }
}

// 5.1 CLI Seed Verification Flag Support
if (args.Contains("--verify-seed"))
{
    using (var verifyScope = app.Services.CreateScope())
    {
        var verifyServices = verifyScope.ServiceProvider;
        var verifyDb = verifyServices.GetRequiredService<ApplicationDbContext>();
        var verifyHasher = verifyServices.GetRequiredService<IPasswordHasher>();
        await SeedVerificationRunner.RunAsync(verifyDb, verifyHasher, app.Configuration);
    }
    return;
}

// 6. HTTP Request Pipeline Configuration
app.UseMiddleware<ExceptionMiddleware>();

// Enable Swagger in Development and Staging for frontend accessibility
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Parking System API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "Smart Parking API - Documentation";
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    c.EnableFilter();
    c.DisplayRequestDuration();
    c.EnableTryItOutByDefault();
    c.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Example);
});

app.UseHttpsRedirection();

app.UseCors(corsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
