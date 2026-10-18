using System.ComponentModel.DataAnnotations;

namespace SmartParkingSystem.Application.DTOs.Payments;

public class ConfirmPaymentRequest
{
    [StringLength(100, ErrorMessage = "Transaction reference cannot exceed 100 characters.")]
    public string? TransactionReference { get; set; }

    [StringLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
    public string? Note { get; set; }
}
