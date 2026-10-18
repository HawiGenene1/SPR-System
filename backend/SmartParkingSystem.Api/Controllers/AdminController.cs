using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartParkingSystem.Application.Common.Security;
using SmartParkingSystem.Application.DTOs.ParkingAreas;
using SmartParkingSystem.Application.DTOs.ParkingSlots;
using SmartParkingSystem.Application.DTOs.Reports;
using SmartParkingSystem.Application.DTOs.Users;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;
using SmartParkingSystem.Infrastructure.Data;

namespace SmartParkingSystem.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IParkingAreaService _parkingAreaService;
    private readonly IParkingSlotService _parkingSlotService;
    private readonly IReportService _reportService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ApplicationDbContext context,
        IParkingAreaService parkingAreaService,
        IParkingSlotService parkingSlotService,
        IReportService reportService,
        ILogger<AdminController> logger)
    {
        _context = context;
        _parkingAreaService = parkingAreaService;
        _parkingSlotService = parkingSlotService;
        _reportService = reportService;
        _logger = logger;
    }

    #region User Management

    /// <summary>
    /// Admin: List all system users with roles and parking area assignments.
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.AssignedParkingArea)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive,
                RoleId = u.RoleId,
                RoleName = u.Role.Name,
                AssignedParkingAreaId = u.AssignedParkingAreaId,
                AssignedParkingAreaName = u.AssignedParkingArea != null ? u.AssignedParkingArea.Name : null,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>
    /// Admin: Activate or deactivate a user account.
    /// </summary>
    [HttpPut("users/{id:guid}/status")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetUserStatus(Guid id, [FromBody] bool isActive)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.AssignedParkingArea)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new { message = $"User with ID '{id}' was not found." });
        }

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Admin updated user {UserId} active status to {IsActive}", id, isActive);

        return Ok(new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            RoleId = user.RoleId,
            RoleName = user.Role.Name,
            AssignedParkingAreaId = user.AssignedParkingAreaId,
            AssignedParkingAreaName = user.AssignedParkingArea?.Name,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        });
    }

    #endregion

    #region Parking Area Management

    /// <summary>
    /// Admin: List all parking facilities with live availability counts.
    /// </summary>
    [HttpGet("parking-areas")]
    [ProducesResponseType(typeof(List<ParkingAreaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllParkingAreas(CancellationToken cancellationToken)
    {
        var areas = await _parkingAreaService.GetAllAsync(includeInactive: true, city: null, cancellationToken);
        return Ok(areas);
    }

    /// <summary>
    /// Admin: Create a new parking facility.
    /// </summary>
    [HttpPost("parking-areas")]
    [ProducesResponseType(typeof(ParkingAreaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateParkingArea([FromBody] CreateParkingAreaRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var created = await _parkingAreaService.CreateAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, created);
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
    /// Admin: Update an existing parking facility.
    /// </summary>
    [HttpPut("parking-areas/{id:guid}")]
    [ProducesResponseType(typeof(ParkingAreaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateParkingArea(Guid id, [FromBody] UpdateParkingAreaRequest request, CancellationToken cancellationToken)
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
    /// </summary>
    [HttpDelete("parking-areas/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteParkingArea(Guid id, CancellationToken cancellationToken)
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

    #endregion

    #region Parking Slot Management

    /// <summary>
    /// Admin: List all parking slots across facilities or filter by parkingAreaId.
    /// </summary>
    [HttpGet("parking-slots")]
    [ProducesResponseType(typeof(List<ParkingSlotDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllParkingSlots([FromQuery] Guid? parkingAreaId, CancellationToken cancellationToken)
    {
        var slots = await _parkingSlotService.GetSlotsAsync(parkingAreaId: parkingAreaId, cancellationToken: cancellationToken);
        return Ok(slots);
    }

    /// <summary>
    /// Admin: Add a parking slot to a facility.
    /// </summary>
    [HttpPost("parking-slots")]
    [ProducesResponseType(typeof(ParkingSlotDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateParkingSlot([FromBody] CreateParkingSlotRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var created = await _parkingSlotService.CreateSlotAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, created);
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
    /// Admin: Update parking slot status or details.
    /// </summary>
    [HttpPut("parking-slots/{id:guid}")]
    [ProducesResponseType(typeof(ParkingSlotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateParkingSlot(Guid id, [FromBody] UpdateParkingSlotRequest request, CancellationToken cancellationToken)
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
    /// Admin: Delete a parking slot.
    /// </summary>
    [HttpDelete("parking-slots/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteParkingSlot(Guid id, CancellationToken cancellationToken)
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

    #endregion

    #region Reports

    /// <summary>
    /// Admin: Financial Revenue Report.
    /// </summary>
    [HttpGet("reports/revenue")]
    [ProducesResponseType(typeof(RevenueReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRevenueReport([FromQuery] ReportFilterRequest filter, CancellationToken cancellationToken)
    {
        try
        {
            var report = await _reportService.GetRevenueReportAsync(filter, cancellationToken);
            return Ok(report);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Admin: Facility Occupancy Report.
    /// </summary>
    [HttpGet("reports/occupancy")]
    [ProducesResponseType(typeof(OccupancyReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOccupancyReport([FromQuery] ReportFilterRequest filter, CancellationToken cancellationToken)
    {
        try
        {
            var report = await _reportService.GetOccupancyReportAsync(filter, cancellationToken);
            return Ok(report);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Admin: Reservation Throughput Report.
    /// </summary>
    [HttpGet("reports/reservations")]
    [ProducesResponseType(typeof(ReservationReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetReservationReport([FromQuery] ReportFilterRequest filter, CancellationToken cancellationToken)
    {
        try
        {
            var report = await _reportService.GetReservationReportAsync(filter, cancellationToken);
            return Ok(report);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Admin: Customer Analytics &amp; Activity Report.
    /// </summary>
    [HttpGet("reports/customers")]
    [ProducesResponseType(typeof(CustomerReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCustomerReport([FromQuery] ReportFilterRequest filter, CancellationToken cancellationToken)
    {
        try
        {
            var report = await _reportService.GetCustomerReportAsync(filter, cancellationToken);
            return Ok(report);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    #endregion
}
