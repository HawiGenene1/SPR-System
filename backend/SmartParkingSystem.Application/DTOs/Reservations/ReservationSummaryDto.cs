using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.DTOs.Reservations;

public class ReservationSummaryDto
{
    public Guid Id { get; set; }
    public string ReservationNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ParkingAreaName { get; set; } = string.Empty;
    public string SlotNumber { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal TotalPrice { get; set; }
    public ReservationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
