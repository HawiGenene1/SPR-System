using System.ComponentModel.DataAnnotations;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.DTOs.Reservations;

public class UpdateReservationStatusRequest
{
    [Required(ErrorMessage = "Reservation status is required.")]
    public ReservationStatus Status { get; set; }

    [StringLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
    public string? Note { get; set; }
}
