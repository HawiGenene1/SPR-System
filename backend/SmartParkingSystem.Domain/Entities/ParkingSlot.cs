using SmartParkingSystem.Domain.Common;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Domain.Entities;

public class ParkingSlot : BaseEntity
{
    public string SlotNumber { get; set; } = string.Empty;
    public int FloorNumber { get; set; } = 1;
    public SlotType SlotType { get; set; } = SlotType.Standard;
    public VehicleType VehicleType { get; set; } = VehicleType.Car;
    public SlotStatus Status { get; set; } = SlotStatus.Available;
    public decimal? HourlyRate { get; set; }
    public bool IsActive { get; set; } = true;

    // Foreign Key
    public Guid ParkingAreaId { get; set; }

    // Navigation properties
    public ParkingArea ParkingArea { get; set; } = null!;
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<ParkingHistory> ParkingHistories { get; set; } = new List<ParkingHistory>();
}
