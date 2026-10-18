using System.ComponentModel.DataAnnotations;
using SmartParkingSystem.Application.Common.Validation;

namespace SmartParkingSystem.Application.DTOs.Reservations;

[ValidReservationTimeWindow]
public class CreateReservationRequest
{
    [Required(ErrorMessage = "Parking slot ID is required.")]
    public Guid ParkingSlotId { get; set; }

    /// <summary>
    /// Optional parking facility ID. If provided, validates that the slot belongs to this facility.
    /// </summary>
    public Guid? ParkingAreaId { get; set; }

    [Required(ErrorMessage = "Vehicle ID is required.")]
    public Guid VehicleId { get; set; }

    [Required(ErrorMessage = "Start time is required.")]
    public DateTime StartTime { get; set; }

    [Required(ErrorMessage = "End time is required.")]
    public DateTime EndTime { get; set; }

    public Guid? UserId { get; set; }

    /// <summary>
    /// Untrusted client price/amount field. Ignored on the server; total amount is strictly calculated server-side.
    /// </summary>
    public decimal? TotalPrice { get; set; }

    /// <summary>
    /// Untrusted client price/amount field. Ignored on the server; total amount is strictly calculated server-side.
    /// </summary>
    public decimal? Amount { get; set; }
}
