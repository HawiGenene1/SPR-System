using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.DTOs.ParkingSlots;

public class ParkingSlotDto
{
    public Guid Id { get; set; }
    public string SlotNumber { get; set; } = string.Empty;
    public int FloorNumber { get; set; }
    public SlotType SlotType { get; set; }
    public VehicleType VehicleType { get; set; }
    public SlotStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public bool IsActive { get; set; }
    public bool IsAvailable => IsActive && Status == SlotStatus.Available;
    public Guid ParkingAreaId { get; set; }
    public string? ParkingAreaName { get; set; }
    public decimal? HourlyRate { get; set; }
    public decimal PricePerHour { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
