using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParkingSystem.Application.Common.Exceptions;
using SmartParkingSystem.Application.Common.Security;
using SmartParkingSystem.Application.DTOs.Reports;
using SmartParkingSystem.Application.Interfaces;

namespace SmartParkingSystem.Api.Controllers;

/// <summary>
/// Business intelligence, executive reporting, revenue analytics, reservation throughput, occupancy heatmaps, and customer segmentation.
/// </summary>
[ApiController]
[Route("api/reports")]
[Tags("Reports")]
[Authorize(Roles = AppRoles.Admin)]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportService reportService,
        ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// Admin: Financial Revenue Report.
    /// Supports date range, presets (daily, weekly, monthly, custom), breakdowns, and paginated transactions suitable for Angular charts and tables.
    /// </summary>
    /// <param name="filter">Reporting filters including timeframe presets (daily, weekly, monthly, custom), date range, and pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Financial revenue analysis with chart series and transaction breakdown.</response>
    /// <response code="400">Bad Request: Invalid date ranges or period specification.</response>
    /// <response code="401">Unauthorized: Authentication token is missing or invalid.</response>
    /// <response code="403">Forbidden: Requires Admin role.</response>
    [HttpGet("revenue")]
    [ProducesResponseType(typeof(RevenueReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRevenueReport(
        [FromQuery] ReportFilterRequest filter,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Admin requested Revenue Report (Period: {Period}, Start: {Start}, End: {End})",
                filter.Period, filter.StartDate, filter.EndDate);

            var report = await _reportService.GetRevenueReportAsync(filter, cancellationToken);
            return Ok(report);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Admin: Reservation Throughput Report.
    /// Supports date range, presets, cancellation and completion rates, and paginated reservations.
    /// </summary>
    /// <param name="filter">Reporting filters including timeframe presets, date ranges, and pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Reservation volume, trends, completion rates, and details.</response>
    /// <response code="400">Bad Request: Invalid filter parameters.</response>
    /// <response code="401">Unauthorized: Authentication token is missing or invalid.</response>
    /// <response code="403">Forbidden: Requires Admin role.</response>
    [HttpGet("reservations")]
    [ProducesResponseType(typeof(ReservationReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetReservationReport(
        [FromQuery] ReportFilterRequest filter,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Admin requested Reservation Report (Period: {Period}, Start: {Start}, End: {End})",
                filter.Period, filter.StartDate, filter.EndDate);

            var report = await _reportService.GetReservationReportAsync(filter, cancellationToken);
            return Ok(report);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Admin: Facility Occupancy Report.
    /// Supports date range, presets, slot types, hourly peaks, and paginated facility inventories.
    /// </summary>
    /// <param name="filter">Occupancy filter parameters including parking area ID, date range, and presets.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Occupancy rates, peak hours, and slot utilization metrics.</response>
    /// <response code="400">Bad Request: Invalid area or date parameters.</response>
    /// <response code="401">Unauthorized: Authentication token is missing or invalid.</response>
    /// <response code="403">Forbidden: Requires Admin role.</response>
    [HttpGet("occupancy")]
    [ProducesResponseType(typeof(OccupancyReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOccupancyReport(
        [FromQuery] ReportFilterRequest filter,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Admin requested Occupancy Report (Period: {Period}, Area: {Area})",
                filter.Period, filter.ParkingAreaId);

            var report = await _reportService.GetOccupancyReportAsync(filter, cancellationToken);
            return Ok(report);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Admin: Customer Analytics &amp; Activity Report.
    /// Supports date range, presets, new signups, activity tier breakdowns, search, and paginated customer table.
    /// </summary>
    /// <param name="filter">Customer analytics filters including keyword search, activity period, and pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Customer activity trends, top spenders, and paginated list.</response>
    /// <response code="400">Bad Request: Invalid filter criteria.</response>
    /// <response code="401">Unauthorized: Authentication token is missing or invalid.</response>
    /// <response code="403">Forbidden: Requires Admin role.</response>
    [HttpGet("customers")]
    [ProducesResponseType(typeof(CustomerReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCustomerReport(
        [FromQuery] ReportFilterRequest filter,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Admin requested Customer Report (Period: {Period}, Search: {Search})",
                filter.Period, filter.Search);

            var report = await _reportService.GetCustomerReportAsync(filter, cancellationToken);
            return Ok(report);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
