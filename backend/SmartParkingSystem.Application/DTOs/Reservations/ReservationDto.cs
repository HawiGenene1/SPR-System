using SmartParkingSystem.Application.DTOs.Payments;
using SmartParkingSystem.Application.DTOs.QRCodes;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.DTOs.Reservations;

public class ReservationDto
{
    public Guid Id { get; set; }
    public string ReservationNumber { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal DurationHours => (EndTime > StartTime) ? Math.Round((decimal)(EndTime - StartTime).TotalHours, 2) : 0m;
    public int DurationMinutes => (EndTime > StartTime) ? (int)(EndTime - StartTime).TotalMinutes : 0;
    public decimal TotalPrice { get; set; }
    public ReservationStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    // User / Customer details
    public Guid UserId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhoneNumber { get; set; }

    // Parking Slot & Area details
    public Guid ParkingSlotId { get; set; }
    public string? SlotNumber { get; set; }
    public int? FloorNumber { get; set; }
    public SlotType? SlotType { get; set; }
    public Guid? ParkingAreaId { get; set; }
    public string? ParkingAreaName { get; set; }
    public string? ParkingAreaAddress { get; set; }

    // Vehicle details
    public Guid VehicleId { get; set; }
    public string? LicensePlate { get; set; }
    public string? VehicleMake { get; set; }
    public string? VehicleModel { get; set; }

    // Linked DTOs
    public QRCodeDto? QRCode { get; set; }
    public PaymentDto? Payment { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
