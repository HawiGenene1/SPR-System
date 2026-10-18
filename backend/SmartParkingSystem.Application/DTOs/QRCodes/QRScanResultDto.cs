using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.DTOs.QRCodes;

public class QRScanResultDto
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Action { get; set; } = "ValidateOnly";
    public string QRCode { get; set; } = string.Empty;
    public Guid ReservationId { get; set; }
    public string ReservationNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhoneNumber { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public string ParkingAreaName { get; set; } = string.Empty;
    public string SlotNumber { get; set; } = string.Empty;
    public int FloorNumber { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public ReservationStatus ReservationStatus { get; set; }
    public string ReservationStatusName => ReservationStatus.ToString();
    public bool IsUsedForEntry { get; set; }
    public DateTime? EntryScannedAt { get; set; }
    public bool IsUsedForExit { get; set; }
    public DateTime? ExitScannedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    public decimal? OverstayFee { get; set; }
    public int? OverstayMinutes { get; set; }
}
