using SmartParkingSystem.Domain.Common;

namespace SmartParkingSystem.Domain.Entities;

public class AuditLog : BaseEntity
{
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // Foreign Key
    public Guid? UserId { get; set; }

    // Navigation property
    public User? User { get; set; }
}
