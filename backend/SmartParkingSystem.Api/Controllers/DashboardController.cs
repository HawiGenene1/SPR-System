using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParkingSystem.Application.Common.Exceptions;
using SmartParkingSystem.Application.Common.Security;
using SmartParkingSystem.Application.DTOs.Dashboards;
using SmartParkingSystem.Application.Interfaces;

namespace SmartParkingSystem.Api.Controllers;

/// <summary>
/// Executive, operational, and customer self-service dashboards with live metrics, occupancy, and KPIs.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Tags("Dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardService dashboardService,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Admin: Retrieve system-wide KPIs, inventory breakdown, today &amp; monthly revenue, and charting datasets.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">System-wide admin metrics and analytics datasets.</response>
    /// <response code="401">Unauthorized: Authentication token is missing or invalid.</response>
    /// <response code="403">Forbidden: Requires Admin role.</response>
    [HttpGet("admin")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(AdminDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAdminDashboard(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving Admin Dashboard metrics...");
        var dashboard = await _dashboardService.GetAdminDashboardAsync(cancellationToken);
        return Ok(dashboard);
    }

    /// <summary>
    /// Parking Attendant / Admin: Retrieve facility-scoped operational metrics including today's check-ins,
    /// check-outs, live occupied and available slots, active on-site reservations, and recent vehicle entries.
    /// </summary>
    /// <param name="parkingAreaId">Optional parking area ID filter to inspect a specific facility.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Facility operational metrics and active parking status.</response>
    /// <response code="401">Unauthorized: Authentication token is missing or invalid.</response>
    /// <response code="403">Forbidden: Requires ParkingAttendant or Admin role.</response>
    [HttpGet("attendant")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.ParkingAttendant}")]
    [ProducesResponseType(typeof(AttendantDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAttendantDashboard(
        [FromQuery] Guid? parkingAreaId,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var isAdmin = IsAdmin();

        _logger.LogInformation("Retrieving Attendant Dashboard metrics for User {UserId} (IsAdmin: {IsAdmin}, Area: {AreaId})",
            currentUserId, isAdmin, parkingAreaId);

        var dashboard = await _dashboardService.GetAttendantDashboardAsync(
            currentUserId,
            isAdmin,
            parkingAreaId,
            cancellationToken);

        return Ok(dashboard);
    }

    /// <summary>
    /// Customer: Retrieve personal dashboard overview including upcoming &amp; active reservation,
    /// total and completed reservation counts, total payments, registered vehicles, and recent parking history.
    /// Customer only receives their own data.
    /// </summary>
    /// <param name="customerId">Optional customer ID parameter (only permitted for Admins; ignored for regular customers).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Customer personal dashboard metrics, active reservations, and vehicle records.</response>
    /// <response code="401">Unauthorized: Authentication token is missing or invalid.</response>
    /// <response code="403">Forbidden: Caller is not authorized.</response>
    /// <response code="404">Not Found: Customer record not found.</response>
    [HttpGet("customer")]
    [Authorize(Roles = $"{AppRoles.Customer},{AppRoles.Admin}")]
    [ProducesResponseType(typeof(CustomerDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomerDashboard(
        [FromQuery] Guid? customerId,
        CancellationToken cancellationToken)
    {
        try
        {
            var authenticatedUserId = GetCurrentUserId();
            var isAdmin = IsAdmin();

            // Customer must only receive their own data:
            // Non-admin callers can never query another customer's data.
            var targetCustomerId = (customerId.HasValue && isAdmin) ? customerId.Value : authenticatedUserId;

            _logger.LogInformation("Retrieving Customer Dashboard metrics for User {TargetCustomerId} (Caller: {CallerId}, IsAdmin: {IsAdmin})",
                targetCustomerId, authenticatedUserId, isAdmin);

            var dashboard = await _dashboardService.GetCustomerDashboardAsync(targetCustomerId, cancellationToken);
            return Ok(dashboard);
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
