using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParkingSystem.Application.Common.Exceptions;
using SmartParkingSystem.Application.Common.Security;
using SmartParkingSystem.Application.DTOs.Payments;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Api.Controllers;

/// <summary>
/// Payment processing, transaction verification, customer payment history, and financial audit logs.
/// </summary>
[ApiController]
[Route("api/payments")]
[Tags("Payments")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IPaymentService paymentService,
        ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Customer: Process payment for a reservation.
    /// Amount is validated server-side. Customer cannot pay for another user's reservation.
    /// When payment succeeds, reservation status transitions to Confirmed and entry QR is activated.
    /// </summary>
    /// <param name="request">Payment processing request with reservation ID, exact amount, and selected payment method.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Payment processed successfully; reservation confirmed.</response>
    /// <response code="400">Bad Request: Invalid payment amount, currency mismatch, or invalid reservation status.</response>
    /// <response code="401">Unauthorized: Authentication token is missing or invalid.</response>
    /// <response code="403">Forbidden: Caller is not the reservation owner.</response>
    /// <response code="404">Not Found: Reservation does not exist.</response>
    [HttpPost]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessPayment(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _paymentService.ProcessPaymentAsync(request, currentUserId, cancellationToken);
            return Ok(result);
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
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Customer/Admin: Confirm payment transaction.
    /// Updates status from Pending to Completed and links transaction reference.
    /// </summary>
    /// <param name="id">Unique identifier of the payment transaction.</param>
    /// <param name="request">Optional confirmation details including gateway transaction ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Payment confirmed successfully.</response>
    /// <response code="400">Bad Request: Payment is already finalized or in invalid state.</response>
    /// <response code="401">Unauthorized: Authentication token is missing or invalid.</response>
    /// <response code="403">Forbidden: Caller is not authorized to confirm this transaction.</response>
    /// <response code="404">Not Found: Payment record does not exist.</response>
    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmPayment(
        [FromRoute] Guid id,
        [FromBody] ConfirmPaymentRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var isAdmin = IsAdmin();
            var result = await _paymentService.ConfirmPaymentAsync(id, currentUserId, isAdmin, cancellationToken);
            return Ok(result);
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
    /// Customer: View all payments and transaction history belonging to the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">List of customer payment transactions.</response>
    /// <response code="401">Unauthorized: Authentication token is missing or invalid.</response>
    [HttpGet("my")]
    [ProducesResponseType(typeof(IReadOnlyList<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyPayments(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var payments = await _paymentService.GetCustomerPaymentsAsync(currentUserId, cancellationToken);
        return Ok(payments);
    }

    /// <summary>
    /// Customer/Admin: Retrieve payment details and transaction reference by payment ID.
    /// Customer can only access their own payment details; Admin can view any transaction.
    /// </summary>
    /// <param name="id">Unique identifier of the payment transaction.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Payment details found and returned.</response>
    /// <response code="401">Unauthorized: Authentication token is missing or invalid.</response>
    /// <response code="403">Forbidden: Customer attempting to inspect another user's payment.</response>
    /// <response code="404">Not Found: Payment record does not exist.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var isAdmin = IsAdmin();
            var payment = await _paymentService.GetPaymentByIdAsync(id, currentUserId, isAdmin, cancellationToken);
            return Ok(payment);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Customer/Admin: Quick status check for a payment transaction.
    /// </summary>
    /// <param name="id">Unique identifier of the payment transaction.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Payment status information.</response>
    /// <response code="401">Unauthorized: Authentication token is missing or invalid.</response>
    /// <response code="403">Forbidden: Caller is not authorized to view this payment.</response>
    /// <response code="404">Not Found: Payment record does not exist.</response>
    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(typeof(PaymentStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentStatus(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var isAdmin = IsAdmin();
            var status = await _paymentService.GetPaymentStatusAsync(id, currentUserId, isAdmin, cancellationToken);
            return Ok(status);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Admin: List all payments system-wide with optional status, payment method, and date filters.
    /// </summary>
    /// <param name="status">Optional status filter (e.g. Pending, Completed, Failed, Refunded).</param>
    /// <param name="method">Optional payment method filter (e.g. CreditCard, DebitCard, Cash, MobilePayment, BankTransfer).</param>
    /// <param name="fromDate">Optional start date (UTC).</param>
    /// <param name="toDate">Optional end date (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">List of all system transactions matching filters.</response>
    /// <response code="401">Unauthorized: Authentication token is missing or invalid.</response>
    /// <response code="403">Forbidden: Requires Admin role.</response>
    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(IReadOnlyList<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllPayments(
        [FromQuery] PaymentStatus? status,
        [FromQuery] PaymentMethod? method,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var payments = await _paymentService.GetAllPaymentsAsync(status, method, fromDate, toDate, cancellationToken);
        return Ok(payments);
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
