using System.ComponentModel.DataAnnotations;

namespace SmartParkingSystem.Application.DTOs.Reservations;

public class CancelReservationRequest
{
    [StringLength(500, ErrorMessage = "Cancellation reason cannot exceed 500 characters.")]
    public string? Reason { get; set; }
}
