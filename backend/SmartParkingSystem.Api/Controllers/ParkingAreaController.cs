using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParkingSystem.Application.Common.Exceptions;
using SmartParkingSystem.Application.Common.Security;
using SmartParkingSystem.Application.DTOs.ParkingAreas;
using SmartParkingSystem.Application.Interfaces;

namespace SmartParkingSystem.Api.Controllers;

/// <summary>
/// Parking facility management: facility discovery, real-time slot occupancy, operating hours, and administrative setup.
/// </summary>
[ApiController]
[Route("api/parking-areas")]
[Tags("Parking Areas")]
[Authorize]
public class ParkingAreaController : ControllerBase
{
    private readonly IParkingAreaService _parkingAreaService;
    private readonly ILogger<ParkingAreaController> _logger;

    public ParkingAreaController(
        IParkingAreaService parkingAreaService,
        ILogger<ParkingAreaController> logger)
    {
        _parkingAreaService = parkingAreaService;
        _logger = logger;
    }

    /// <summary>
    /// List all parking facilities. Accessible by Customers, Attendants, and Admins.
    /// Non-admin callers only receive active facilities.
    /// </summary>
    /// <param name="city">Optional city filter.</param>
    /// <param name="includeInactive">Whether to include inactive facilities (Admins only).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">List of parking facilities matching query filters.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<ParkingAreaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllParkingAreas(
        [FromQuery] string? city = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        // Only admins can see inactive facilities
        var canViewInactive = User?.Identity?.IsAuthenticated == true && User.IsInRole(AppRoles.Admin) && includeInactive;
        var areas = await _parkingAreaService.GetAllAsync(canViewInactive, city, cancellationToken);
        return Ok(areas);
    }

    /// <summary>
    /// Retrieve details of a specific parking facility by ID.
    /// Accessible by Customers, Attendants, and Admins.
    /// </summary>
    /// <param name="id">Parking facility unique identifier (GUID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Parking facility details.</response>
    /// <response code="404">Not Found: Parking facility does not exist.</response>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ParkingAreaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetParkingAreaById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var area = await _parkingAreaService.GetByIdAsync(id, cancellationToken);
            return Ok(area);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Admin: Create and register a new parking facility.
    /// </summary>
    /// <param name="request">Parking facility creation parameters including name, address, coordinates, total slots, and base hourly rate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">Facility created successfully.</response>
    /// <response code="400">Bad Request: Invalid facility configuration or duplicate name.</response>
    /// <response code="401">Unauthorized: JWT authentication required.</response>
    /// <response code="403">Forbidden: Requires Admin role.</response>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(ParkingAreaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateParkingArea(
        [FromBody] CreateParkingAreaRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var created = await _parkingAreaService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetParkingAreaById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Admin: Update an existing parking facility's details, operating hours, capacity, and rates.
    /// </summary>
    /// <param name="id">Parking facility unique identifier (GUID).</param>
    /// <param name="request">Updated parking facility attributes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Facility updated successfully.</response>
    /// <response code="400">Bad Request: Invalid facility attributes.</response>
    /// <response code="401">Unauthorized: JWT authentication required.</response>
    /// <response code="403">Forbidden: Requires Admin role.</response>
    /// <response code="404">Not Found: Facility does not exist.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(ParkingAreaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateParkingArea(
        Guid id,
        [FromBody] UpdateParkingAreaRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var updated = await _parkingAreaService.UpdateAsync(id, request, cancellationToken);
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
    /// Admin: Soft delete a parking facility.
    /// Prevented if the facility has active or pending reservations.
    /// </summary>
    /// <param name="id">Parking facility unique identifier (GUID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Facility deleted successfully.</response>
    /// <response code="400">Bad Request: Facility has active/pending reservations.</response>
    /// <response code="401">Unauthorized: JWT authentication required.</response>
    /// <response code="403">Forbidden: Requires Admin role.</response>
    /// <response code="404">Not Found: Facility does not exist.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteParkingArea(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _parkingAreaService.DeleteAsync(id, cancellationToken);
            return Ok(new { message = "Parking area deleted successfully." });
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
