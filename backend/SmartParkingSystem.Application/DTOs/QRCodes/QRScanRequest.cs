using System.ComponentModel.DataAnnotations;

namespace SmartParkingSystem.Application.DTOs.QRCodes;

public class QRScanRequest
{
    [Required(ErrorMessage = "QR Code token is required.")]
    public string QRCode { get; set; } = string.Empty;

    /// <summary>
    /// Scan action: 'CheckIn', 'CheckOut', or 'ValidateOnly' (default: 'CheckIn').
    /// </summary>
    [RegularExpression("^(CheckIn|CheckOut|ValidateOnly)$", ErrorMessage = "Action must be 'CheckIn', 'CheckOut', or 'ValidateOnly'.")]
    public string Action { get; set; } = "CheckIn";

    public Guid? AttendantId { get; set; }

    [StringLength(250, ErrorMessage = "Notes cannot exceed 250 characters.")]
    public string? Notes { get; set; }
}
