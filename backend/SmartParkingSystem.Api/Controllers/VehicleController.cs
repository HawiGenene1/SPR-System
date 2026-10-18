using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParkingSystem.Application.Common.Exceptions;
using SmartParkingSystem.Application.Common.Security;
using SmartParkingSystem.Application.DTOs.Vehicles;
using SmartParkingSystem.Application.Interfaces;

namespace SmartParkingSystem.Api.Controllers;

/// <summary>
/// Vehicle fleet management: registration, license plate validation, vehicle inspection, update, and deletion.
/// </summary>
[ApiController]
[Route("api/vehicles")]
[Tags("Vehicles")]
[Authorize]
public class VehicleController : ControllerBase
{
    private readonly IVehicleService _vehicleService;
    private readonly ILogger<VehicleController> _logger;

    public VehicleController(IVehicleService vehicleService, ILogger<VehicleController> logger)
    {
        _vehicleService = vehicleService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new vehicle for the authenticated customer.
    /// </summary>
    /// <param name="request">Vehicle registration details including license plate, vehicle type, make, model, and color.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">Vehicle registered successfully.</response>
    /// <response code="400">Bad Request: Invalid license plate format or duplicate active plate.</response>
    /// <response code="401">Unauthorized: JWT authentication required.</response>
    [HttpPost]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateVehicle([FromBody] CreateVehicleRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetCurrentUserId();
            if (IsAdmin() && request.UserId.HasValue && request.UserId.Value != Guid.Empty)
            {
                userId = request.UserId.Value;
            }
            var created = await _vehicleService.CreateVehicleAsync(userId, request, cancellationToken);
            return CreatedAtAction(nameof(GetVehicleById), new { id = created.Id }, created);
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
    /// Retrieve all registered vehicles belonging to the authenticated customer.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">List of customer's registered vehicles.</response>
    /// <response code="401">Unauthorized: JWT authentication required.</response>
    [HttpGet("my")]
    [ProducesResponseType(typeof(IReadOnlyList<VehicleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyVehicles(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var vehicles = await _vehicleService.GetMyVehiclesAsync(userId, cancellationToken);
        return Ok(vehicles);
    }

    /// <summary>
    /// Retrieve vehicle details by unique ID. Customers can only retrieve their own vehicles.
    /// </summary>
    /// <param name="id">Vehicle unique identifier (GUID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Vehicle details retrieved.</response>
    /// <response code="401">Unauthorized: JWT authentication required.</response>
    /// <response code="403">Forbidden: Attempting to access another user's vehicle.</response>
    /// <response code="404">Not Found: Vehicle does not exist.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVehicleById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id, userId, IsAdmin(), cancellationToken);
            return Ok(vehicle);
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
    /// Update vehicle details (LicensePlate, VehicleType, Make, Model, Color).
    /// </summary>
    /// <param name="id">Vehicle unique identifier (GUID).</param>
    /// <param name="request">Updated vehicle details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Vehicle updated successfully.</response>
    /// <response code="400">Bad Request: Invalid plate format or duplicate active plate.</response>
    /// <response code="401">Unauthorized: JWT authentication required.</response>
    /// <response code="403">Forbidden: Attempting to modify another user's vehicle.</response>
    /// <response code="404">Not Found: Vehicle does not exist.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateVehicle(Guid id, [FromBody] UpdateVehicleRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetCurrentUserId();
            var updated = await _vehicleService.UpdateVehicleAsync(id, userId, request, IsAdmin(), cancellationToken);
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
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Soft delete a vehicle. Only the owner (or Admin) can delete, provided no active/pending reservations exist.
    /// </summary>
    /// <param name="id">Vehicle unique identifier (GUID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Vehicle deleted successfully.</response>
    /// <response code="400">Bad Request: Vehicle has active/confirmed reservations or is parked.</response>
    /// <response code="401">Unauthorized: JWT authentication required.</response>
    /// <response code="403">Forbidden: Attempting to delete another user's vehicle.</response>
    /// <response code="404">Not Found: Vehicle does not exist.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVehicle(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _vehicleService.DeleteVehicleAsync(id, userId, IsAdmin(), cancellationToken);
            return Ok(new { message = "Vehicle deleted successfully." });
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
