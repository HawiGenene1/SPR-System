using SmartParkingSystem.Domain.Common;

namespace SmartParkingSystem.Domain.Entities;

public class QRCode : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string? ImageData { get; set; }
    public bool IsUsedForEntry { get; set; } = false;
    public DateTime? EntryScannedAt { get; set; }
    public bool IsUsedForExit { get; set; } = false;
    public DateTime? ExitScannedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    // Foreign Key
    public Guid ReservationId { get; set; }

    // Navigation property
    public Reservation Reservation { get; set; } = null!;
}
