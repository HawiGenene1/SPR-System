using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.DTOs.Vehicles;

public class VehicleDto
{
    public Guid Id { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public VehicleType VehicleType { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? Color { get; set; }
    public Guid UserId { get; set; }
    public string? OwnerFullName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
