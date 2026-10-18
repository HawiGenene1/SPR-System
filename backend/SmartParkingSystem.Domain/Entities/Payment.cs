using SmartParkingSystem.Domain.Common;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Domain.Entities;

public class Payment : BaseEntity
{
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CreditCard;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string TransactionId { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public string? FailureReason { get; set; }

    // Foreign Keys
    public Guid ReservationId { get; set; }
    public Guid UserId { get; set; }

    // Navigation properties
    public Reservation Reservation { get; set; } = null!;
    public User User { get; set; } = null!;
}
