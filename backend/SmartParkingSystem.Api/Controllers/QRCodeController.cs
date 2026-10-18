using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParkingSystem.Application.Common.Exceptions;
using SmartParkingSystem.Application.Common.Security;
using SmartParkingSystem.Application.DTOs.QRCodes;
using SmartParkingSystem.Application.Interfaces;

namespace SmartParkingSystem.Api.Controllers;

/// <summary>
/// Dynamic QR code generation, Base64 image delivery, and attendant gate scanning validation.
/// </summary>
[ApiController]
[Route("api/qr-codes")]
[Tags("QR Codes")]
[Authorize]
public class QRCodeController : ControllerBase
{
    private readonly IQRCodeService _qrCodeService;
    private readonly ILogger<QRCodeController> _logger;

    public QRCodeController(
        IQRCodeService qrCodeService,
        ILogger<QRCodeController> logger)
    {
        _qrCodeService = qrCodeService;
        _logger = logger;
    }

    /// <summary>
    /// Customer/Admin: Retrieve the QR code and Base64 image for a specific reservation.
    /// Only the customer who owns the reservation or an Admin can access it.
    /// </summary>
    /// <param name="reservationId">Unique identifier of the reservation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">QR code details retrieved successfully, including token string and Base64 image.</response>
    /// <response code="401">Unauthorized: Authentication token is missing or invalid.</response>
    /// <response code="403">Forbidden: Caller is neither the reservation owner nor an Admin.</response>
    /// <response code="404">Not Found: Reservation does not exist or has no active QR code.</response>
    [HttpGet("{reservationId:guid}")]
    [ProducesResponseType(typeof(QRCodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQRCodeByReservationId(
        [FromRoute] Guid reservationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var isAdmin = IsAdmin();
            var qrCode = await _qrCodeService.GetQRCodeByReservationIdAsync(reservationId, currentUserId, isAdmin, cancellationToken);
            return Ok(qrCode);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
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

    /// <summary>
    /// ParkingAttendant/Admin: Scan and validate customer QR code for parking entry or exit.
    /// Restricted strictly to authorized staff (ParkingAttendant and Admin).
    /// </summary>
    /// <param name="request">QR scan payload containing raw token string and gate identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">QR code validated successfully, returning customer, slot, and vehicle details.</response>
    /// <response code="400">Bad Request: Invalid, tampered, or expired QR code token.</response>
    /// <response code="401">Unauthorized: Authentication token is missing or invalid.</response>
    /// <response code="403">Forbidden: Requires ParkingAttendant or Admin role.</response>
    /// <response code="404">Not Found: Corresponding reservation or slot not found.</response>
    [HttpPost("scan")]
    [Authorize(Roles = $"{AppRoles.ParkingAttendant},{AppRoles.Admin}")]
    [ProducesResponseType(typeof(QRScanResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ScanQRCode(
        [FromBody] QRScanRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var staffUserId = GetCurrentUserId();
        var isAdmin = IsAdmin();

        var result = await _qrCodeService.ScanQRCodeAsync(request, staffUserId, isAdmin, cancellationToken);
        return Ok(result);
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
