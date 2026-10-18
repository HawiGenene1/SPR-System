using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartParkingSystem.Application.DTOs.Payments;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;
using SmartParkingSystem.Infrastructure.Data;

namespace SmartParkingSystem.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        ApplicationDbContext context,
        IPaymentRepository paymentRepository,
        IPaymentGateway paymentGateway,
        ILogger<PaymentService> logger)
    {
        _context = context;
        _paymentRepository = paymentRepository;
        _paymentGateway = paymentGateway;
        _logger = logger;
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, SemaphoreSlim> _reservationPaymentLocks = new();

    public async Task<PaymentDto> ProcessPaymentAsync(
        CreatePaymentRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        // 1. Fetch reservation with related user, slot, and existing payment
        var reservation = await _context.Reservations
            .Include(r => r.User)
            .Include(r => r.ParkingSlot)
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(r => r.Id == request.ReservationId && !r.IsDeleted, cancellationToken);

        if (reservation == null)
        {
            throw new KeyNotFoundException($"Reservation with ID '{request.ReservationId}' was not found.");
        }

        // 2. Customer Isolation: verify ownership
        if (reservation.UserId != currentUserId)
        {
            _logger.LogWarning(
                "Security alert: User {UserId} attempted to pay for Reservation {ReservationId} owned by User {OwnerId}.",
                currentUserId, reservation.Id, reservation.UserId);
            throw new UnauthorizedAccessException("You are not authorized to pay for another customer's reservation.");
        }

        // 3. Status validation
        if (reservation.Status == ReservationStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot process payment for a cancelled reservation.");
        }

        if (reservation.Status == ReservationStatus.Completed)
        {
            throw new InvalidOperationException("Cannot process payment for a completed reservation.");
        }

        if (reservation.Payment != null && reservation.Payment.Status == PaymentStatus.Paid)
        {
            throw new InvalidOperationException("This reservation has already been paid for.");
        }

        // 4. Validate amount against server-computed total price
        if (request.Amount.HasValue)
        {
            if (Math.Round(request.Amount.Value, 2) != Math.Round(reservation.TotalPrice, 2))
            {
                throw new InvalidOperationException(
                    $"Payment amount ({request.Amount.Value:F2}) does not match the server-calculated reservation total ({reservation.TotalPrice:F2}).");
            }
        }

        var amountToCharge = reservation.TotalPrice;
        var transactionRef = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        // 5. Concurrency prevention: Application-level lock per reservation + database transaction
        var paymentLock = _reservationPaymentLocks.GetOrAdd(reservation.Id, _ => new SemaphoreSlim(1, 1));
        await paymentLock.WaitAsync(cancellationToken);
        try
        {
            if (_context.Database.IsRelational())
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
                    try
                    {
                        if (_context.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            await _context.Database.ExecuteSqlInterpolatedAsync(
                                $"SELECT \"Id\" FROM \"Reservations\" WHERE \"Id\" = {reservation.Id} FOR UPDATE",
                                cancellationToken);
                        }

                        var result = await ExecuteProcessPaymentInternalAsync(
                            reservation, request, amountToCharge, transactionRef, currentUserId, cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        return result;
                    }
                    catch
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        throw;
                    }
                });
            }
            else
            {
                return await ExecuteProcessPaymentInternalAsync(
                    reservation, request, amountToCharge, transactionRef, currentUserId, cancellationToken);
            }
        }
        finally
        {
            paymentLock.Release();
        }
    }

    private async Task<PaymentDto> ExecuteProcessPaymentInternalAsync(
        Reservation reservation,
        CreatePaymentRequest request,
        decimal amountToCharge,
        string transactionRef,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        // Re-verify payment hasn't been completed concurrently
        var existingPayment = await _context.Payments
            .FirstOrDefaultAsync(p => p.ReservationId == reservation.Id && !p.IsDeleted, cancellationToken);

        if (existingPayment != null && existingPayment.Status == PaymentStatus.Paid)
        {
            throw new InvalidOperationException("This reservation has already been paid for.");
        }

        Payment payment;
        if (existingPayment != null)
        {
            payment = existingPayment;
            payment.Amount = amountToCharge;
            payment.PaymentMethod = request.PaymentMethod;
            payment.TransactionId = transactionRef;
            payment.Status = PaymentStatus.Pending;
            payment.FailureReason = null;
        }
        else
        {
            payment = new Payment
            {
                Id = Guid.NewGuid(),
                ReservationId = reservation.Id,
                UserId = currentUserId,
                Amount = amountToCharge,
                PaymentMethod = request.PaymentMethod,
                TransactionId = transactionRef,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            await _paymentRepository.AddAsync(payment, cancellationToken);
        }

        await _paymentRepository.SaveChangesAsync(cancellationToken);

        // 6. Call payment gateway abstraction
        var gatewayRequest = new PaymentGatewayRequest
        {
            TransactionReference = transactionRef,
            Amount = amountToCharge,
            Currency = "USD",
            PaymentMethod = request.PaymentMethod,
            CustomerEmail = reservation.User.Email,
            CustomerName = reservation.User.FullName,
            Description = $"Payment for Reservation #{reservation.ReservationNumber}",
            PaymentToken = request.PaymentToken
        };

        var gatewayResult = await _paymentGateway.ProcessPaymentAsync(gatewayRequest, cancellationToken);

        if (gatewayResult.IsSuccess)
        {
            payment.Status = PaymentStatus.Paid;
            payment.PaidAt = gatewayResult.ProcessedAt;
            payment.FailureReason = null;

            // When payment succeeds: Reservation becomes Confirmed
            reservation.Status = ReservationStatus.Confirmed;

            // Ensure entrance QR code exists and has rendered image data
            var qr = await _context.QRCodes.FirstOrDefaultAsync(q => q.ReservationId == reservation.Id && !q.IsDeleted, cancellationToken);
            if (qr == null)
            {
                var qrToken = $"QR-{reservation.ReservationNumber}-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}";
                qr = new QRCode
                {
                    Id = Guid.NewGuid(),
                    ReservationId = reservation.Id,
                    Code = qrToken,
                    ImageData = QRCodeService.RenderQrCodeBase64(qrToken),
                    ExpiresAt = reservation.EndTime.AddHours(2),
                    CreatedAt = DateTime.UtcNow
                };
                await _context.QRCodes.AddAsync(qr, cancellationToken);
            }
            else if (string.IsNullOrWhiteSpace(qr.ImageData))
            {
                qr.ImageData = QRCodeService.RenderQrCodeBase64(qr.Code);
            }

            // Update slot status if reservation is scheduled to begin soon
            var now = DateTime.UtcNow;
            if (reservation.StartTime <= now.AddMinutes(15) && reservation.EndTime > now &&
                reservation.ParkingSlot.Status == SlotStatus.Available)
            {
                reservation.ParkingSlot.Status = SlotStatus.Reserved;
            }

            _logger.LogInformation("Payment {PaymentId} succeeded for Reservation {ReservationNumber}.",
                payment.Id, reservation.ReservationNumber);
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = gatewayResult.ErrorMessage ?? "Payment was declined by provider.";
            _logger.LogWarning("Payment {PaymentId} failed for Reservation {ReservationNumber}. Reason: {Reason}",
                payment.Id, reservation.ReservationNumber, payment.FailureReason);
        }

        await _paymentRepository.UpdateAsync(payment, cancellationToken);
        await _paymentRepository.SaveChangesAsync(cancellationToken);

        payment.Reservation = reservation;
        payment.User = reservation.User;

        return MapToDto(payment, gatewayResult.MaskedAccountIdentifier);
    }

    public async Task<PaymentDto> ConfirmPaymentAsync(
        Guid paymentId,
        Guid currentUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        if (payment == null)
        {
            throw new KeyNotFoundException($"Payment with ID '{paymentId}' was not found.");
        }

        if (!isAdmin && payment.UserId != currentUserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to confirm this payment.");
        }

        if (payment.Status == PaymentStatus.Paid)
        {
            return MapToDto(payment);
        }

        if (payment.Status == PaymentStatus.Refunded)
        {
            throw new InvalidOperationException("Cannot confirm a refunded payment.");
        }

        if (_context.Database.IsRelational())
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
                try
                {
                    var res = await ExecuteConfirmPaymentInternalAsync(payment, cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return res;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }
        else
        {
            return await ExecuteConfirmPaymentInternalAsync(payment, cancellationToken);
        }
    }

    private async Task<PaymentDto> ExecuteConfirmPaymentInternalAsync(Payment payment, CancellationToken cancellationToken)
    {
        payment.Status = PaymentStatus.Paid;
        payment.PaidAt = DateTime.UtcNow;
        payment.FailureReason = null;

        if (payment.Reservation != null)
        {
            payment.Reservation.Status = ReservationStatus.Confirmed;
            var now = DateTime.UtcNow;
            if (payment.Reservation.StartTime <= now.AddMinutes(15) &&
                payment.Reservation.EndTime > now &&
                payment.Reservation.ParkingSlot != null &&
                payment.Reservation.ParkingSlot.Status == SlotStatus.Available)
            {
                payment.Reservation.ParkingSlot.Status = SlotStatus.Reserved;
            }

            var qr = await _context.QRCodes.FirstOrDefaultAsync(q => q.ReservationId == payment.Reservation.Id && !q.IsDeleted, cancellationToken);
            if (qr == null)
            {
                var qrToken = $"QR-{payment.Reservation.ReservationNumber}-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}";
                qr = new QRCode
                {
                    Id = Guid.NewGuid(),
                    ReservationId = payment.Reservation.Id,
                    Code = qrToken,
                    ImageData = QRCodeService.RenderQrCodeBase64(qrToken),
                    ExpiresAt = payment.Reservation.EndTime.AddHours(2),
                    CreatedAt = DateTime.UtcNow
                };
                await _context.QRCodes.AddAsync(qr, cancellationToken);
            }
        }

        await _paymentRepository.UpdateAsync(payment, cancellationToken);
        await _paymentRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(payment);
    }

    public async Task<PaymentDto> GetPaymentByIdAsync(
        Guid paymentId,
        Guid currentUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        if (payment == null)
        {
            throw new KeyNotFoundException($"Payment with ID '{paymentId}' was not found.");
        }

        if (!isAdmin && payment.UserId != currentUserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to access this payment.");
        }

        return MapToDto(payment);
    }

    public async Task<PaymentStatusDto> GetPaymentStatusAsync(
        Guid paymentId,
        Guid currentUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        if (payment == null)
        {
            throw new KeyNotFoundException($"Payment with ID '{paymentId}' was not found.");
        }

        if (!isAdmin && payment.UserId != currentUserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to view the status of this payment.");
        }

        return new PaymentStatusDto
        {
            PaymentId = payment.Id,
            TransactionReference = payment.TransactionId,
            Status = payment.Status,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            PaidAt = payment.PaidAt,
            FailureReason = payment.FailureReason,
            ReservationId = payment.ReservationId,
            ReservationNumber = payment.Reservation?.ReservationNumber,
            ReservationStatus = payment.Reservation?.Status,
            MaskedAccount = payment.PaymentMethod switch
            {
                PaymentMethod.Card or PaymentMethod.CreditCard or PaymentMethod.DebitCard => "**** **** **** 4242",
                PaymentMethod.MobileMoney => "+251***789",
                PaymentMethod.Cash => "Cash Payment",
                PaymentMethod.Online => "ONLINE-ACCT-***56",
                _ => "****-****"
            },
            CreatedAt = payment.CreatedAt
        };
    }

    public async Task<IReadOnlyList<PaymentDto>> GetCustomerPaymentsAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var payments = await _paymentRepository.GetByUserIdAsync(currentUserId, cancellationToken);
        return payments.Select(p => MapToDto(p)).ToList();
    }

    public async Task<IReadOnlyList<PaymentDto>> GetAllPaymentsAsync(
        PaymentStatus? status = null,
        PaymentMethod? method = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var payments = await _paymentRepository.GetAllAsync(status, method, fromDate, toDate, cancellationToken);
        return payments.Select(p => MapToDto(p)).ToList();
    }

    private static PaymentDto MapToDto(Payment payment, string? customMaskedIdentifier = null)
    {
        var masked = customMaskedIdentifier ?? payment.PaymentMethod switch
        {
            PaymentMethod.Card or PaymentMethod.CreditCard or PaymentMethod.DebitCard => "**** **** **** 4242",
            PaymentMethod.MobileMoney => "+251***789",
            PaymentMethod.Cash => "Cash Payment",
            PaymentMethod.Online => "ONLINE-ACCT-***56",
            _ => "****-****"
        };

        return new PaymentDto
        {
            Id = payment.Id,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            Status = payment.Status,
            TransactionId = payment.TransactionId,
            MaskedAccount = masked,
            PaidAt = payment.PaidAt,
            FailureReason = payment.FailureReason,
            ReservationId = payment.ReservationId,
            ReservationNumber = payment.Reservation?.ReservationNumber,
            UserId = payment.UserId,
            CustomerName = payment.User?.FullName,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        };
    }
}
