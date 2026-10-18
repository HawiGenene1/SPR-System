using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.DTOs.Parking;

public class CheckInResultDto
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public Guid ReservationId { get; set; }
    public string ReservationNumber { get; set; } = string.Empty;
    public Guid ParkingSlotId { get; set; }
    public string SlotNumber { get; set; } = string.Empty;
    public int FloorNumber { get; set; }
    public Guid ParkingAreaId { get; set; }
    public string ParkingAreaName { get; set; } = string.Empty;
    public Guid VehicleId { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string VehicleMakeModel { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhoneNumber { get; set; }
    public DateTime CheckInTime { get; set; }
    public DateTime ScheduledStartTime { get; set; }
    public DateTime ScheduledEndTime { get; set; }
    public ReservationStatus ReservationStatus { get; set; } = ReservationStatus.Active;
    public string ReservationStatusName => ReservationStatus.ToString();
    public SlotStatus SlotStatus { get; set; } = SlotStatus.Occupied;
    public string SlotStatusName => SlotStatus.ToString();
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Paid;
    public string PaymentStatusName => PaymentStatus.ToString();
    public Guid? AttendantId { get; set; }
    public string? AttendantName { get; set; }
    public string? Notes { get; set; }
}
