using System.ComponentModel.DataAnnotations;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.DTOs.Payments;

public class CreatePaymentRequest
{
    [Required(ErrorMessage = "Reservation ID is required.")]
    public Guid ReservationId { get; set; }

    /// <summary>
    /// Optional payment amount. If provided, server strictly validates against reservation TotalPrice.
    /// If omitted or null, server automatically charges the exact reservation TotalPrice.
    /// </summary>
    [Range(0.01, 100000.0, ErrorMessage = "Payment amount must be greater than zero.")]
    public decimal? Amount { get; set; }

    [Required(ErrorMessage = "Payment method is required.")]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Card;

    [StringLength(100, ErrorMessage = "Payment token/reference cannot exceed 100 characters.")]
    public string? PaymentToken { get; set; }
}
