using SmartParkingSystem.Domain.Common;

namespace SmartParkingSystem.Domain.Entities;

public class ParkingArea : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int TotalSlots { get; set; }
    public decimal HourlyRate { get; set; }
    public string OperatingHours { get; set; } = "24/7";
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<ParkingSlot> ParkingSlots { get; set; } = new List<ParkingSlot>();
    public ICollection<User> Attendants { get; set; } = new List<User>();
}
