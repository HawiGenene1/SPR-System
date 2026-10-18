using System.ComponentModel.DataAnnotations;
using SmartParkingSystem.Application.Common.Validation;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.DTOs.Vehicles;

public class CreateVehicleRequest
{
    [Required(ErrorMessage = "License plate is required.")]
    [ValidLicensePlate]
    public string LicensePlate { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vehicle type is required.")]
    [ValidVehicleType]
    public VehicleType VehicleType { get; set; } = VehicleType.Car;

    [Required(ErrorMessage = "Vehicle make is required.")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Make cannot exceed 50 characters.")]
    public string Make { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vehicle model is required.")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Model cannot exceed 50 characters.")]
    public string Model { get; set; } = string.Empty;

    [StringLength(30, ErrorMessage = "Color cannot exceed 30 characters.")]
    public string? Color { get; set; }

    public Guid? UserId { get; set; }
}
