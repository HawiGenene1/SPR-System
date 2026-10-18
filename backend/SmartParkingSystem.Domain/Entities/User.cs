using SmartParkingSystem.Domain.Common;

namespace SmartParkingSystem.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

    // Foreign Keys
    public Guid RoleId { get; set; }
    public Guid? AssignedParkingAreaId { get; set; }

    // Navigation properties
    public Role Role { get; set; } = null!;
    public ParkingArea? AssignedParkingArea { get; set; }

    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<ParkingHistory> CheckInsHandled { get; set; } = new List<ParkingHistory>();
    public ICollection<ParkingHistory> CheckOutsHandled { get; set; } = new List<ParkingHistory>();
}
