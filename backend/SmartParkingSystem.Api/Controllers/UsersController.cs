using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartParkingSystem.Application.Common.Exceptions;
using SmartParkingSystem.Application.Common.Security;
using SmartParkingSystem.Application.DTOs.Common;
using SmartParkingSystem.Application.DTOs.Users;
using SmartParkingSystem.Infrastructure.Data;

namespace SmartParkingSystem.Api.Controllers;

/// <summary>
/// User management endpoints for administrative oversight, account activation/deactivation, and profile maintenance.
/// </summary>
[ApiController]
[Route("api/users")]
[Tags("Users")]
[Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(ApplicationDbContext context, ILogger<UsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Admin: List all system users with search, role filtering, status filtering, and server-side pagination.
    /// </summary>
    /// <param name="search">Optional search keyword matching FullName or Email.</param>
    /// <param name="roleName">Optional role filter (e.g., 'Admin', 'ParkingAttendant', 'Customer').</param>
    /// <param name="isActive">Optional active account status filter.</param>
    /// <param name="pageNumber">Page index (default: 1).</param>
    /// <param name="pageSize">Number of items per page (default: 20, max: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Successfully retrieved paginated list of users.</response>
    /// <response code="401">Unauthorized: JWT authentication is required.</response>
    /// <response code="403">Forbidden: Requires Admin role.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? roleName,
        [FromQuery] bool? isActive,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.AssignedParkingArea)
            .Where(u => !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTrimmed = search.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(searchTrimmed) || u.Email.ToLower().Contains(searchTrimmed));
        }

        if (!string.IsNullOrWhiteSpace(roleName))
        {
            query = query.Where(u => u.Role.Name.ToLower() == roleName.Trim().ToLower());
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
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
            .ToListAsync(cancellationToken);

        var result = new PagedResultDto<UserDto>(users, totalCount, pageNumber, pageSize);

        return Ok(result);
    }

    /// <summary>
    /// Admin: Retrieve detailed user account information by user identifier.
    /// </summary>
    /// <param name="id">Unique user identifier (GUID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Successfully retrieved user details.</response>
    /// <response code="401">Unauthorized: JWT authentication is required.</response>
    /// <response code="403">Forbidden: Requires Admin role.</response>
    /// <response code="404">Not Found: User with the specified ID does not exist.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.AssignedParkingArea)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User", id);
        }

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

    /// <summary>
    /// Admin: Update an existing user's profile details or parking area assignment.
    /// </summary>
    /// <param name="id">Unique user identifier (GUID).</param>
    /// <param name="request">Updated user information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Successfully updated user details.</response>
    /// <response code="400">Bad Request: Invalid update payload or non-existent parking area.</response>
    /// <response code="401">Unauthorized: JWT authentication is required.</response>
    /// <response code="403">Forbidden: Requires Admin role.</response>
    /// <response code="404">Not Found: User does not exist.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.AssignedParkingArea)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User", id);
        }

        if (request.AssignedParkingAreaId.HasValue)
        {
            var areaExists = await _context.ParkingAreas.AnyAsync(a => a.Id == request.AssignedParkingAreaId.Value && !a.IsDeleted, cancellationToken);
            if (!areaExists)
            {
                throw new BusinessRuleException($"Parking facility with ID '{request.AssignedParkingAreaId}' was not found.");
            }
            user.AssignedParkingAreaId = request.AssignedParkingAreaId.Value;
        }

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();
        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
        }
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Admin updated user profile for {UserId}", id);

        var updatedAreaName = user.AssignedParkingAreaId.HasValue
            ? await _context.ParkingAreas.Where(a => a.Id == user.AssignedParkingAreaId.Value).Select(a => a.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

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
            AssignedParkingAreaName = updatedAreaName,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        });
    }

    /// <summary>
    /// Admin: Activate or deactivate a user account.
    /// </summary>
    /// <param name="id">Unique user identifier (GUID).</param>
    /// <param name="isActive">Active status flag (true to activate, false to deactivate).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Successfully updated user activation status.</response>
    /// <response code="401">Unauthorized: JWT authentication is required.</response>
    /// <response code="403">Forbidden: Requires Admin role.</response>
    /// <response code="404">Not Found: User with the specified ID does not exist.</response>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetUserStatus(Guid id, [FromBody] bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.AssignedParkingArea)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User", id);
        }

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

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

    /// <summary>
    /// Admin: Soft delete a user account.
    /// </summary>
    /// <param name="id">Unique user identifier (GUID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">User account was successfully deactivated and marked as deleted.</response>
    /// <response code="400">Bad Request: Cannot delete user with active parking reservations.</response>
    /// <response code="401">Unauthorized: JWT authentication is required.</response>
    /// <response code="403">Forbidden: Requires Admin role.</response>
    /// <response code="404">Not Found: User with the specified ID does not exist.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("User", id);
        }

        var hasActiveReservations = await _context.Reservations
            .AnyAsync(r => r.UserId == id && !r.IsDeleted && (r.Status == Domain.Enums.ReservationStatus.Active || r.Status == Domain.Enums.ReservationStatus.Confirmed), cancellationToken);

        if (hasActiveReservations)
        {
            throw new BusinessRuleException("Cannot delete user with active or confirmed parking reservations.");
        }

        user.IsDeleted = true;
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin soft-deleted user {UserId}", id);
        return Ok(new { message = "User deleted successfully." });
    }
}
