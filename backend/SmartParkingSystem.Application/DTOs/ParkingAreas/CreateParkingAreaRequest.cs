using System.ComponentModel.DataAnnotations;
using SmartParkingSystem.Application.Common.Validation;

namespace SmartParkingSystem.Application.DTOs.ParkingAreas;

public class CreateParkingAreaRequest
{
    private int _totalSlots;

    [Required(ErrorMessage = "Parking area name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(250, ErrorMessage = "Description cannot exceed 250 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Address or Location is required.")]
    [StringLength(250, ErrorMessage = "Address cannot exceed 250 characters.")]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Unified Location string (auto-syncs with Address if Address is not provided).
    /// </summary>
    public string? Location
    {
        get => Address;
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(Address))
            {
                Address = value.Trim();
            }
        }
    }

    [Required(ErrorMessage = "City is required.")]
    [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
    public string City { get; set; } = string.Empty;

    [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90.")]
    public decimal? Latitude { get; set; }

    [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180.")]
    public decimal? Longitude { get; set; }

    [Range(1, 10000, ErrorMessage = "Total capacity must be between 1 and 10,000.")]
    public int TotalSlots
    {
        get => _totalSlots;
        set => _totalSlots = value;
    }

    /// <summary>
    /// Facility capacity alias for TotalSlots.
    /// </summary>
    public int Capacity
    {
        get => _totalSlots;
        set => _totalSlots = value;
    }

    [Required(ErrorMessage = "Hourly rate is required.")]
    [Range(0.01, 10000.0, ErrorMessage = "Hourly rate must be greater than zero.")]
    public decimal HourlyRate { get; set; }

    /// <summary>
    /// Operating schedule (e.g. "24/7" or "06:00 - 23:00").
    /// </summary>
    [ValidOperatingHours]
    public string OperatingHours { get; set; } = "24/7";
}
