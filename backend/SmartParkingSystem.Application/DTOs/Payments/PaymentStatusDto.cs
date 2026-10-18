using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.DTOs.Payments;

public class PaymentStatusDto
{
    public Guid PaymentId { get; set; }
    public Guid ReservationId { get; set; }
    public string? ReservationNumber { get; set; }
    public ReservationStatus? ReservationStatus { get; set; }
    public string? ReservationStatusName => ReservationStatus?.ToString();
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public PaymentMethod PaymentMethod { get; set; }
    public string PaymentMethodName => PaymentMethod.ToString();
    public string TransactionReference { get; set; } = string.Empty;
    public string? MaskedAccount { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
}
