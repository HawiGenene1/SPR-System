using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParkingSystem.Application.Common.Exceptions;
using SmartParkingSystem.Application.Common.Security;
using SmartParkingSystem.Application.DTOs.ParkingSlots;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Api.Controllers;

/// <summary>
/// Parking slot inventory: slot availability, floor classification, vehicle compatibility, rate overrides, and maintenance control.
/// </summary>
[ApiController]
[Route("api/parking-slots")]
[Tags("Parking Slots")]
[Authorize]
public class ParkingSlotController : ControllerBase
{
    private readonly IParkingSlotService _parkingSlotService;
    private readonly ILogger<ParkingSlotController> _logger;

    public ParkingSlotController(
        IParkingSlotService parkingSlotService,
        ILogger<ParkingSlotController> logger)
    {
        _parkingSlotService = parkingSlotService;
        _logger = logger;
    }

    /// <summary>
    /// View slot availability and details. Accessible by Customers, Attendants, and Admins.
    /// Supports filtering by parking facility, floor, vehicle type, status, and availability.
    /// </summary>
    /// <param name="parkingAreaId">Optional facility identifier filter.</param>
    /// <param name="floorNumber">Optional floor level filter.</param>
    /// <param name="status">Optional slot status filter (Available, Reserved, Occupied, Maintenance).</param>
    /// <param name="vehicleType">Optional vehicle type compatibility filter.</param>
    /// <param name="availableOnly">If true, returns only vacant, non-maintenance slots.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">List of parking slots matching query parameters.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<ParkingSlotDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSlots(
        [FromQuery] Guid? parkingAreaId = null,
        [FromQuery] int? floorNumber = null,
        [FromQuery] SlotStatus? status = null,
        [FromQuery] VehicleType? vehicleType = null,
        [FromQuery] bool availableOnly = false,
        CancellationToken cancellationToken = default)
    {
        var slots = await _parkingSlotService.GetSlotsAsync(
            parkingAreaId, floorNumber, status, vehicleType, availableOnly, cancellationToken);
        return Ok(slots);
    }

    /// <summary>
    /// Retrieve details of a specific parking slot by ID. Accessible by Customers, Attendants, and Admins.
    /// </summary>
    /// <param name="id">Parking slot unique identifier (GUID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Parking slot details.</response>
    /// <response code="404">Not Found: Parking slot does not exist.</response>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ParkingSlotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSlotById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var slot = await _parkingSlotService.GetSlotByIdAsync(id, cancellationToken);
            return Ok(slot);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Admin: Create and register a new parking slot within a facility.
    /// Enforces slot number uniqueness within the parking facility.
    /// </summary>
    /// <param name="request">Parking slot parameters including facility ID, slot number, floor number, vehicle type, and rate override.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">Parking slot created successfully.</response>
    /// <response code="400">Bad Request: Duplicate slot number or invalid attributes.</response>
    /// <response code="401">Unauthorized: JWT authentication required.</response>
    /// <response code="403">Forbidden: Requires Admin role.</response>
    /// <response code="404">Not Found: Parent parking facility does not exist.</response>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(ParkingSlotDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateSlot(
        [FromBody] CreateParkingSlotRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var created = await _parkingSlotService.CreateSlotAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetSlotById), new { id = created.Id }, created);
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
    /// Admin: Update parking slot properties (SlotNumber, FloorNumber, SlotType, VehicleType, PricePerHour, Status, IsActive).
    /// </summary>
    /// <param name="id">Parking slot unique identifier (GUID).</param>
    /// <param name="request">Updated slot configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Parking slot updated successfully.</response>
    /// <response code="400">Bad Request: Invalid slot attributes or duplicate slot number.</response>
    /// <response code="401">Unauthorized: JWT authentication required.</response>
    /// <response code="403">Forbidden: Requires Admin role.</response>
    /// <response code="404">Not Found: Parking slot does not exist.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(ParkingSlotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSlot(
        Guid id,
        [FromBody] UpdateParkingSlotRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var updated = await _parkingSlotService.UpdateSlotAsync(id, request, cancellationToken);
            return Ok(updated);
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
    /// Admin: Put a parking slot into or out of Maintenance status.
    /// Blocked if the slot currently has active or pending reservations.
    /// </summary>
    /// <param name="id">Parking slot unique identifier (GUID).</param>
    /// <param name="isMaintenance">Set true for Maintenance; false to return to Available.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Slot maintenance status successfully modified.</response>
    /// <response code="400">Bad Request: Slot has active/pending reservations.</response>
    /// <response code="401">Unauthorized: JWT authentication required.</response>
    /// <response code="403">Forbidden: Requires Admin role.</response>
    /// <response code="404">Not Found: Parking slot does not exist.</response>
    [HttpPut("{id:guid}/maintenance")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(ParkingSlotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetMaintenanceStatus(
        Guid id,
        [FromQuery] bool isMaintenance = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var updated = await _parkingSlotService.SetMaintenanceStatusAsync(id, isMaintenance, cancellationToken);
            return Ok(updated);
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
    /// Admin: Soft delete a parking slot.
    /// Blocked if the slot has active or pending reservations.
    /// </summary>
    /// <param name="id">Parking slot unique identifier (GUID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Parking slot deleted successfully.</response>
    /// <response code="400">Bad Request: Slot has active/pending reservations.</response>
    /// <response code="401">Unauthorized: JWT authentication required.</response>
    /// <response code="403">Forbidden: Requires Admin role.</response>
    /// <response code="404">Not Found: Parking slot does not exist.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSlot(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _parkingSlotService.DeleteSlotAsync(id, cancellationToken);
            return Ok(new { message = "Parking slot deleted successfully." });
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
}
