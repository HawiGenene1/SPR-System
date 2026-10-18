using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParkingSystem.Application.Common.Exceptions;
using SmartParkingSystem.Application.Common.Security;
using SmartParkingSystem.Application.DTOs.Common;
using SmartParkingSystem.Application.DTOs.ParkingHistory;
using SmartParkingSystem.Application.Interfaces;

namespace SmartParkingSystem.Api.Controllers;

/// <summary>
/// Historical records of completed and active vehicle parking sessions, timestamps, fees, and attendant audits.
/// </summary>
[ApiController]
[Route("api/parking-history")]
[Tags("Parking")]
[Authorize]
public class ParkingHistoryController : ControllerBase
{
    private readonly IParkingHistoryService _parkingHistoryService;
    private readonly ILogger<ParkingHistoryController> _logger;

    public ParkingHistoryController(
        IParkingHistoryService parkingHistoryService,
        ILogger<ParkingHistoryController> logger)
    {
        _parkingHistoryService = parkingHistoryService;
        _logger = logger;
    }

    /// <summary>
    /// Customer: Retrieve personal parking history check-ins and check-outs with pagination and date filtering.
    /// </summary>
    /// <param name="filter">Pagination and date filtering criteria (page number, page size, start date, end date).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Paginated list of customer parking history records.</response>
    /// <response code="400">Bad Request: Invalid date ranges or invalid pagination parameters.</response>
    /// <response code="401">Unauthorized: Authentication token is missing or invalid.</response>
    [HttpGet("my")]
    [ProducesResponseType(typeof(PagedResultDto<ParkingHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyParkingHistory(
        [FromQuery] ParkingHistoryFilterRequest filter,
        CancellationToken cancellationToken)
    {
        try
        {
            var customerId = GetCurrentUserId();
            var result = await _parkingHistoryService.GetCustomerHistoryAsync(customerId, filter, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Admin: Retrieve all system-wide parking history check-in and check-out logs with pagination, date filtering, and search.
    /// </summary>
    /// <param name="filter">Pagination, keyword search, and date interval criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Paginated list of all system parking history records.</response>
    /// <response code="400">Bad Request: Invalid filter or pagination parameters.</response>
    /// <response code="401">Unauthorized: Authentication token is missing or invalid.</response>
    /// <response code="403">Forbidden: Requires Admin role.</response>
    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(PagedResultDto<ParkingHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllParkingHistory(
        [FromQuery] ParkingHistoryFilterRequest filter,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _parkingHistoryService.GetAllHistoryAsync(filter, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Customer / Admin: Retrieve a single parking history record by ID.
    /// Customers can only access their own parking records; Admins can access any record.
    /// </summary>
    /// <param name="id">Unique identifier of the parking history record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Parking history session found and returned.</response>
    /// <response code="401">Unauthorized: Authentication token is missing or invalid.</response>
    /// <response code="403">Forbidden: Caller is not authorized to inspect this history session.</response>
    /// <response code="404">Not Found: Parking history record does not exist.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ParkingHistoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetParkingHistoryById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var isAdmin = IsAdmin();
            var result = await _parkingHistoryService.GetHistoryByIdAsync(id, currentUserId, isAdmin, cancellationToken);
            return Ok(result);
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
