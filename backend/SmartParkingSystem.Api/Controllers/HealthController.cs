using Microsoft.AspNetCore.Mvc;

namespace SmartParkingSystem.Api.Controllers;

/// <summary>
/// Service health check and system heartbeat monitoring.
/// </summary>
[ApiController]
[Route("api/health")]
[Tags("Authentication")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Public: Retrieve service health, system timestamp, and runtime environment status.
    /// </summary>
    /// <response code="200">System is healthy and accepting requests.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            Status = "Healthy",
            System = "Smart Parking Reservation System API",
            Timestamp = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
        });
    }
}

