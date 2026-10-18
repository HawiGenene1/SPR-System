using System.ComponentModel.DataAnnotations;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.DTOs.ParkingSlots;

public class UpdateParkingSlotRequest
{
    [StringLength(20, MinimumLength = 1, ErrorMessage = "Slot number must be between 1 and 20 characters.")]
    public string? SlotNumber { get; set; }

    [Range(-5, 50, ErrorMessage = "Floor number must be between -5 and 50.")]
    public int? FloorNumber { get; set; }

    public SlotType? SlotType { get; set; }

    /// <summary>
    /// Permitted vehicle type for this slot.
    /// </summary>
    public VehicleType? VehicleType { get; set; }

    /// <summary>
    /// Slot status: Available, Reserved, Occupied, Maintenance.
    /// </summary>
    public SlotStatus? Status { get; set; }

    /// <summary>
    /// Optional price per hour override.
    /// </summary>
    [Range(0.0, 10000.0, ErrorMessage = "Hourly rate must be greater than or equal to 0.")]
    public decimal? HourlyRate { get; set; }

    /// <summary>
    /// Price per hour alias for HourlyRate.
    /// </summary>
    public decimal? PricePerHour
    {
        get => HourlyRate;
        set => HourlyRate = value;
    }

    public bool? IsActive { get; set; }
}
