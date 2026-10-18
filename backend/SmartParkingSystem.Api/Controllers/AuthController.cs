using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParkingSystem.Application.Common.Exceptions;
using SmartParkingSystem.Application.DTOs.Auth;
using SmartParkingSystem.Application.DTOs.Users;
using SmartParkingSystem.Application.Interfaces;

namespace SmartParkingSystem.Api.Controllers;

/// <summary>
/// Authentication operations: Registration, Login, Token Refresh, Revocation, and Profile inspection.
/// </summary>
[ApiController]
[Route("api/auth")]
[Tags("Authentication")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new customer user account.
    /// </summary>
    /// <param name="request">New customer account credentials and contact details.</param>
    /// <response code="201">Customer registered successfully; returns JWT and refresh token.</response>
    /// <response code="400">Validation failure: Email already in use or missing required fields.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _authService.RegisterAsync(request);
            _logger.LogInformation("New user registered successfully: {Email}", request.Email);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Registration failed for {Email}: {Message}", request.Email, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration for {Email}", request.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while processing registration." });
        }
    }

    /// <summary>
    /// Authenticate a user and receive JWT &amp; Refresh tokens.
    /// </summary>
    /// <param name="request">User login credentials (Email and Password).</param>
    /// <response code="200">Authentication successful: Returns JWT access token, expiration, and refresh token.</response>
    /// <response code="401">Unauthorized: Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _authService.LoginAsync(request);
            _logger.LogInformation("User logged in successfully: {Email}", request.Email);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Failed login attempt for {Email}", request.Email);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for {Email}", request.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred during authentication." });
        }
    }

    /// <summary>
    /// Refresh an expired JWT token using a valid Refresh token.
    /// </summary>
    /// <param name="request">Expired JWT token and active Refresh token.</param>
    /// <response code="200">Token refreshed: Returns renewed JWT access token and refresh token.</response>
    /// <response code="401">Unauthorized: Invalid, expired, or revoked refresh token.</response>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _authService.RefreshTokenAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while refreshing the token." });
        }
    }

    /// <summary>
    /// Revoke active refresh token and log out the user.
    /// </summary>
    /// <response code="200">User successfully logged out and refresh token revoked.</response>
    /// <response code="401">Unauthorized: Missing or invalid JWT Bearer token.</response>
    [Authorize]
    [HttpPost("logout")]
    [HttpPost("revoke-token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User not identified." });
        }

        await _authService.LogoutAsync(userId.Value);
        _logger.LogInformation("User {UserId} logged out", userId);
        return Ok(new { message = "Logged out successfully." });
    }

    /// <summary>
    /// Retrieve profile information of the currently authenticated user.
    /// </summary>
    /// <response code="200">Current authenticated user profile details.</response>
    /// <response code="401">Unauthorized: Missing or invalid JWT Bearer token.</response>
    /// <response code="404">Not Found: User profile does not exist.</response>
    [Authorize]
    [HttpGet("me")]
    [HttpGet("profile")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User not identified." });
        }

        try
        {
            var user = await _authService.GetCurrentUserAsync(userId.Value);
            return Ok(user);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "User profile not found." });
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("sub")?.Value;

        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }
}
