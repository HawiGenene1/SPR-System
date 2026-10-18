using System.ComponentModel.DataAnnotations;

namespace SmartParkingSystem.Application.DTOs.Parking;

public class CheckInRequest
{
    [Required(ErrorMessage = "QR Code token is required.")]
    public string QRCode { get; set; } = string.Empty;

    public Guid? ParkingAreaId { get; set; }

    [StringLength(250, ErrorMessage = "Notes cannot exceed 250 characters.")]
    public string? Notes { get; set; }
}
