using Microsoft.Extensions.Logging;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Infrastructure.Services;

/// <summary>
/// Mock payment gateway providing safe payment simulation for development and testing.
/// Easily replaceable by a real gateway (e.g. Stripe, PayPal, Chapa, Telebirr) via IPaymentGateway.
/// </summary>
public class MockPaymentGateway : IPaymentGateway
{
    private readonly ILogger<MockPaymentGateway> _logger;

    public MockPaymentGateway(ILogger<MockPaymentGateway> logger)
    {
        _logger = logger;
    }

    public Task<PaymentGatewayResult> ProcessPaymentAsync(
        PaymentGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing mock payment for Reference: {Reference}, Method: {Method}, Amount: {Amount} {Currency}",
            request.TransactionReference, request.PaymentMethod, request.Amount, request.Currency);

        // Allow deliberate failure simulation for testing edge cases
        if (!string.IsNullOrWhiteSpace(request.PaymentToken) &&
            (request.PaymentToken.Equals("FAIL", StringComparison.OrdinalIgnoreCase) ||
             request.PaymentToken.Equals("DECLINED", StringComparison.OrdinalIgnoreCase)))
        {
            return Task.FromResult(new PaymentGatewayResult
            {
                IsSuccess = false,
                Status = PaymentStatus.Failed,
                TransactionReference = request.TransactionReference,
                GatewayReference = $"GW-FAIL-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                ErrorMessage = "Payment was declined by payment provider.",
                ProcessedAt = DateTime.UtcNow
            });
        }

        // Generate safe masked identifiers depending on payment method - never expose raw card/account numbers
        var maskedAccount = request.PaymentMethod switch
        {
            PaymentMethod.Card or PaymentMethod.CreditCard or PaymentMethod.DebitCard => "**** **** **** 4242",
            PaymentMethod.MobileMoney => "+251***789",
            PaymentMethod.Online => "ONLINE-ACCT-***56",
            PaymentMethod.Cash => "Cash Payment (Receipt Pending)",
            _ => "****-****"
        };

        var result = new PaymentGatewayResult
        {
            IsSuccess = true,
            Status = PaymentStatus.Paid,
            TransactionReference = request.TransactionReference,
            GatewayReference = $"GW-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}",
            MaskedAccountIdentifier = maskedAccount,
            ProcessedAt = DateTime.UtcNow
        };

        return Task.FromResult(result);
    }

    public Task<PaymentGatewayResult> RefundPaymentAsync(
        string transactionReference,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing mock refund for Reference: {Reference}, Amount: {Amount}", transactionReference, amount);

        return Task.FromResult(new PaymentGatewayResult
        {
            IsSuccess = true,
            Status = PaymentStatus.Refunded,
            TransactionReference = transactionReference,
            GatewayReference = $"RF-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}",
            ProcessedAt = DateTime.UtcNow
        });
    }
}
