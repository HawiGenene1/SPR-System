using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.DTOs.Payments;

public class PaymentDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string PaymentMethodName => PaymentMethod.ToString();
    public PaymentStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string TransactionId { get; set; } = string.Empty;
    public string TransactionReference => TransactionId;
    public string? MaskedAccount { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? FailureReason { get; set; }
    public Guid ReservationId { get; set; }
    public string? ReservationNumber { get; set; }
    public Guid UserId { get; set; }
    public string? CustomerName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
