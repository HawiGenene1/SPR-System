namespace SmartParkingSystem.Application.DTOs.ParkingAreas;

public class ParkingAreaDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Formatted physical location (Address, City).
    /// </summary>
    public string Location => string.IsNullOrWhiteSpace(City) ? Address : $"{Address}, {City}";

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    /// <summary>
    /// Total parking slot capacity of the facility.
    /// </summary>
    public int TotalSlots { get; set; }
    public int Capacity => TotalSlots;

    public int AvailableSlots { get; set; }
    public int OccupiedSlots { get; set; }
    public decimal HourlyRate { get; set; }

    /// <summary>
    /// Operating schedule of the facility (e.g. "24/7" or "06:00 - 23:00").
    /// </summary>
    public string OperatingHours { get; set; } = "24/7";

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
