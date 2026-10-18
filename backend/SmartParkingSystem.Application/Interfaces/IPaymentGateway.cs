using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.Interfaces;

public class PaymentGatewayRequest
{
    public string TransactionReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentMethod PaymentMethod { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    public string? Description { get; set; }
    public string? PaymentToken { get; set; }
}

public class PaymentGatewayResult
{
    public bool IsSuccess { get; set; }
    public string TransactionReference { get; set; } = string.Empty;
    public string? GatewayReference { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? ErrorMessage { get; set; }
    public string? MaskedAccountIdentifier { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Abstraction for payment providers (e.g. Mock, Stripe, PayPal, Chapa, Telebirr).
/// Allows switching or adding real payment providers without changing reservation or payment business logic.
/// </summary>
public interface IPaymentGateway
{
    Task<PaymentGatewayResult> ProcessPaymentAsync(PaymentGatewayRequest request, CancellationToken cancellationToken = default);
    Task<PaymentGatewayResult> RefundPaymentAsync(string transactionReference, decimal amount, CancellationToken cancellationToken = default);
}
