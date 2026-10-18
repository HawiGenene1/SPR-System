using SmartParkingSystem.Domain.Common;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Domain.Entities;

public class Vehicle : BaseEntity
{
    public string LicensePlate { get; set; } = string.Empty;
    public VehicleType VehicleType { get; set; } = VehicleType.Car;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? Color { get; set; }

    // Foreign Key
    public Guid UserId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<ParkingHistory> ParkingHistories { get; set; } = new List<ParkingHistory>();
}
