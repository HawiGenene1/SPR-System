using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParkingSystem.Application.Common.Exceptions;
using SmartParkingSystem.Application.Common.Security;
using SmartParkingSystem.Application.DTOs.Reservations;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Api.Controllers;

/// <summary>
/// Reservation management: booking creation, server-side duration/pricing calculation, customer booking history, cancellations, and status transitions.
/// </summary>
[ApiController]
[Route("api/reservations")]
[Tags("Reservations")]
[Authorize]
public class ReservationController : ControllerBase
{
    private readonly IReservationService _reservationService;
    private readonly ILogger<ReservationController> _logger;

    public ReservationController(
        IReservationService reservationService,
        ILogger<ReservationController> logger)
    {
        _reservationService = reservationService;
        _logger = logger;
    }

    /// <summary>
    /// Customer: Create a new slot reservation.
    /// Duration and pricing are strictly calculated on the server.
    /// Concurrency protection prevents overlapping bookings for the same slot.
    /// </summary>
    /// <param name="request">Reservation parameters including slot ID, vehicle ID, scheduled entry time, and scheduled exit time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">Reservation created successfully; slot tentatively reserved and entry QR generated.</response>
    /// <response code="400">Bad Request: Slot already reserved, vehicle mismatch, past arrival time, or duration under 15 minutes.</response>
    /// <response code="401">Unauthorized: JWT authentication required.</response>
    /// <response code="404">Not Found: Parking slot or vehicle does not exist.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateReservation(
        [FromBody] CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetCurrentUserId();
            var created = await _reservationService.CreateReservationAsync(request, userId, cancellationToken);
            return CreatedAtAction(nameof(GetReservationById), new { id = created.Id }, created);
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
    }

    /// <summary>
    /// Customer: Retrieve all reservations made by the authenticated customer.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Customer's reservation history.</response>
    /// <response code="401">Unauthorized: JWT authentication required.</response>
    [HttpGet("my")]
    [ProducesResponseType(typeof(IReadOnlyList<ReservationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyReservations(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var reservations = await _reservationService.GetCustomerReservationsAsync(userId, cancellationToken);
        return Ok(reservations);
    }

    /// <summary>
    /// Retrieve reservation details by ID.
    /// Customers may only view their own reservations; Admins may view any.
    /// </summary>
    /// <param name="id">Reservation unique identifier (GUID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Reservation details.</response>
    /// <response code="401">Unauthorized: JWT authentication required.</response>
    /// <response code="403">Forbidden: Attempting to access another customer's reservation.</response>
    /// <response code="404">Not Found: Reservation does not exist.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReservationById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var isAdmin = IsAdmin();
            var reservation = await _reservationService.GetReservationByIdAsync(id, userId, isAdmin, cancellationToken);
            return Ok(reservation);
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
    /// Cancel an upcoming reservation.
    /// Customers may cancel their own upcoming reservations; Admins may cancel any.
    /// Releases the parking slot immediately back to Available status.
    /// </summary>
    /// <param name="id">Reservation unique identifier (GUID).</param>
    /// <param name="request">Optional cancellation reason note.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Reservation cancelled and slot released.</response>
    /// <response code="400">Bad Request: Cannot cancel an active or completed reservation.</response>
    /// <response code="401">Unauthorized: JWT authentication required.</response>
    /// <response code="403">Forbidden: Attempting to cancel another customer's reservation.</response>
    /// <response code="404">Not Found: Reservation does not exist.</response>
    [HttpPut("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelReservation(
        Guid id,
        [FromBody] CancelReservationRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var isAdmin = IsAdmin();
            var cancelled = await _reservationService.CancelReservationAsync(
                id, userId, request?.Reason, isAdmin, cancellationToken);
            return Ok(cancelled);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Admin: List all reservations with optional filtering by parkingAreaId, status, and date range.
    /// </summary>
    /// <param name="parkingAreaId">Optional facility filter.</param>
    /// <param name="status">Optional reservation status filter.</param>
    /// <param name="fromDate">Optional start of date window (UTC).</param>
    /// <param name="toDate">Optional end of date window (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">List of reservations matching query filters.</response>
    /// <response code="401">Unauthorized: JWT authentication required.</response>
    /// <response code="403">Forbidden: Requires Admin role.</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(IReadOnlyList<ReservationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllReservations(
        [FromQuery] Guid? parkingAreaId = null,
        [FromQuery] ReservationStatus? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var reservations = await _reservationService.GetAllReservationsAsync(
            parkingAreaId, status, fromDate, toDate, cancellationToken);
        return Ok(reservations);
    }

    /// <summary>
    /// Admin: Update reservation status (Confirmed, Active, Completed, Cancelled, Expired).
    /// Slot status is automatically synchronized.
    /// </summary>
    /// <param name="id">Reservation unique identifier (GUID).</param>
    /// <param name="request">New reservation status and administrative notes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Reservation status updated and slot synchronized.</response>
    /// <response code="400">Bad Request: Invalid state transition.</response>
    /// <response code="401">Unauthorized: JWT authentication required.</response>
    /// <response code="403">Forbidden: Requires Admin role.</response>
    /// <response code="404">Not Found: Reservation does not exist.</response>
    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateReservationStatus(
        Guid id,
        [FromBody] UpdateReservationStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var updated = await _reservationService.UpdateReservationStatusAsync(
                id, request.Status, request.Note, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
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
