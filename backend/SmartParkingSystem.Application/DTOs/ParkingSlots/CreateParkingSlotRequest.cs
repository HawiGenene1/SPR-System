using System.ComponentModel.DataAnnotations;
using SmartParkingSystem.Application.Common.Validation;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.DTOs.ParkingSlots;

public class CreateParkingSlotRequest
{
    [Required(ErrorMessage = "Parking area ID is required.")]
    public Guid ParkingAreaId { get; set; }

    [Required(ErrorMessage = "Slot number is required.")]
    [ValidSlotNumber]
    public string SlotNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Floor number is required.")]
    [Range(-5, 50, ErrorMessage = "Floor number must be between -5 and 50.")]
    public int FloorNumber { get; set; } = 1;

    [Required(ErrorMessage = "Slot type is required.")]
    public SlotType SlotType { get; set; } = SlotType.Standard;

    /// <summary>
    /// Vehicle type permitted for this slot.
    /// </summary>
    [Required(ErrorMessage = "Vehicle type is required.")]
    public VehicleType VehicleType { get; set; } = VehicleType.Car;

    /// <summary>
    /// Optional price per hour override. If null, the parking area's default hourly rate applies.
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
}
