using System.ComponentModel.DataAnnotations;

namespace SmartParkingSystem.Application.DTOs.Parking;

public class CheckOutRequest
{
    /// <summary>
    /// The unique QR code token presented by the customer at the exit gate.
    /// </summary>
    [Required(ErrorMessage = "QR Code is required for check-out.")]
    public string QRCode { get; set; } = string.Empty;

    /// <summary>
    /// Optional parking facility identifier if the attendant is assigned to a specific gate/facility.
    /// </summary>
    public Guid? ParkingAreaId { get; set; }

    /// <summary>
    /// Optional notes or comments recorded during vehicle exit (e.g., vehicle condition, overstay remarks).
    /// </summary>
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }
}
