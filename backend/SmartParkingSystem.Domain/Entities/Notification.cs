using SmartParkingSystem.Domain.Common;

namespace SmartParkingSystem.Domain.Entities;

public class Notification : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public string? Type { get; set; }

    // Foreign Key
    public Guid UserId { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}
