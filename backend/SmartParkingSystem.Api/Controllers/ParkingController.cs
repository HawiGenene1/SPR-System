using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParkingSystem.Application.Common.Exceptions;
using SmartParkingSystem.Application.Common.Security;
using SmartParkingSystem.Application.DTOs.Parking;
using SmartParkingSystem.Application.Interfaces;

namespace SmartParkingSystem.Api.Controllers;

/// <summary>
/// On-site vehicle gate operations: entry check-in verification, exit check-out processing, overstay calculations, and slot state transitions.
/// </summary>
[ApiController]
[Route("api/parking")]
[Tags("Parking")]
[Authorize]
public class ParkingController : ControllerBase
{
    private readonly IParkingService _parkingService;
    private readonly ILogger<ParkingController> _logger;

    public ParkingController(
        IParkingService parkingService,
        ILogger<ParkingController> logger)
    {
        _parkingService = parkingService;
        _logger = logger;
    }

    /// <summary>
    /// Staff (ParkingAttendant or Admin): Process vehicle entry check-in via customer QR code scan.
    /// Workflow verifies QR validity, reservation existence, confirmed status, time window, and completed payment.
    /// Records entry time, attendant attribution, sets reservation to Active, and slot to Occupied.
    /// </summary>
    /// <param name="request">Check-in details including scanned QR code token and optional gate identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Vehicle successfully checked in; slot marked Occupied and entry recorded.</response>
    /// <response code="400">Bad Request: Reservation not confirmed, payment incomplete, or entry outside permissible window.</response>
    /// <response code="401">Unauthorized: Authentication token is missing or invalid.</response>
    /// <response code="403">Forbidden: Requires ParkingAttendant or Admin role.</response>
    /// <response code="404">Not Found: Reservation or slot not found for the provided QR token.</response>
    [HttpPost("check-in")]
    [Authorize(Roles = $"{AppRoles.ParkingAttendant},{AppRoles.Admin}")]
    [ProducesResponseType(typeof(CheckInResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckInVehicle(
        [FromBody] CheckInRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var attendantId = GetCurrentUserId();
            var isAdmin = IsAdmin();
            var result = await _parkingService.CheckInVehicleAsync(request, attendantId, isAdmin, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Staff (ParkingAttendant or Admin): Process vehicle exit check-out via customer QR code scan.
    /// Strictly verifies vehicle was previously checked in, reservation is Active, calculates actual duration
    /// and any overstay fees (with 15m grace window), sets reservation to Completed, slot to Available,
    /// and finalizes ParkingHistory record with exit timestamp and attendant attribution.
    /// </summary>
    /// <param name="request">Check-out details including scanned QR code token and optional gate identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Vehicle checked out successfully; slot released to Available and session finalized.</response>
    /// <response code="400">Bad Request: Vehicle has not checked in or reservation is in invalid state.</response>
    /// <response code="401">Unauthorized: Authentication token is missing or invalid.</response>
    /// <response code="403">Forbidden: Requires ParkingAttendant or Admin role.</response>
    /// <response code="404">Not Found: Active parking session not found for the provided QR token.</response>
    [HttpPost("check-out")]
    [Authorize(Roles = $"{AppRoles.ParkingAttendant},{AppRoles.Admin}")]
    [ProducesResponseType(typeof(CheckOutResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckOutVehicle(
        [FromBody] CheckOutRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var attendantId = GetCurrentUserId();
            var isAdmin = IsAdmin();
            var result = await _parkingService.CheckOutVehicleAsync(request, attendantId, isAdmin, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;

        if (Guid.TryParse(claim, out var id))
        {
            return id;
        }

        throw new UnauthorizedAccessException("User identification claim is missing or invalid.");
    }

    private bool IsAdmin()
    {
        return User.IsInRole(AppRoles.Admin);
    }
}
