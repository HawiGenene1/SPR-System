namespace SmartParkingSystem.Application.DTOs.QRCodes;

public class QRCodeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? ImageData { get; set; }
    public bool IsUsedForEntry { get; set; }
    public DateTime? EntryScannedAt { get; set; }
    public bool IsUsedForExit { get; set; }
    public DateTime? ExitScannedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public Guid ReservationId { get; set; }
    public string? ReservationNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}
