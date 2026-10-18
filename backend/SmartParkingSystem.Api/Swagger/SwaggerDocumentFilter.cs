using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SmartParkingSystem.Api.Swagger;

/// <summary>
/// OpenAPI Document Filter that defines tag descriptions and guarantees the requested
/// grouping order across all endpoints in the Swagger UI.
/// </summary>
public class SwaggerDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Tags = new List<OpenApiTag>
        {
            new OpenApiTag
            {
                Name = "Authentication",
                Description = "User account registration, JWT credential login, access token refresh, logout token revocation, profile retrieval, and password change."
            },
            new OpenApiTag
            {
                Name = "Users",
                Description = "User directory management, role-based filtering, account activation/deactivation, and administrator profile updates."
            },
            new OpenApiTag
            {
                Name = "Vehicles",
                Description = "Customer vehicle fleet registration, vehicle type classification, license plate normalization, vehicle updates, and safe deletion."
            },
            new OpenApiTag
            {
                Name = "Parking Areas",
                Description = "Parking facility discovery, geographic search, real-time slot availability, operating capacity, and administrative facility setup."
            },
            new OpenApiTag
            {
                Name = "Parking Slots",
                Description = "Slot inventory management, floor numbers, vehicle type compatibility, hourly rate overrides, and maintenance status toggles."
            },
            new OpenApiTag
            {
                Name = "Reservations",
                Description = "End-to-end reservation booking lifecycle, concurrency locking, server-side duration & price computation, customer history, and cancellations."
            },
            new OpenApiTag
            {
                Name = "Payments",
                Description = "Payment gateway processing, card & mobile money methods, payment verification, transaction receipts, and customer billing histories."
            },
            new OpenApiTag
            {
                Name = "QR Codes",
                Description = "Contactless QR code generation for confirmed reservations, Base64 PNG image rendering, and staff validation scanning."
            },
            new OpenApiTag
            {
                Name = "Parking",
                Description = "Gate operations: attendant vehicle check-in, entry scanning, exit check-out, overstay calculations, and historical session logs."
            },
            new OpenApiTag
            {
                Name = "Dashboard",
                Description = "Role-optimized analytics portals: Executive Admin KPIs and charts, Parking Attendant gate feeds, and Customer personal portal."
            },
            new OpenApiTag
            {
                Name = "Reports",
                Description = "Administrative business intelligence: Revenue analytics, reservation throughput, facility occupancy curves, and customer analytics."
            }
        };
    }
}
