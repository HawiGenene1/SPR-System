using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.DTOs.Parking;

public class CheckOutResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    // Reservation Details
    public Guid ReservationId { get; set; }
    public string ReservationNumber { get; set; } = string.Empty;

    // Slot & Area Details
    public Guid ParkingSlotId { get; set; }
    public string SlotNumber { get; set; } = string.Empty;
    public int FloorNumber { get; set; }
    public Guid ParkingAreaId { get; set; }
    public string ParkingAreaName { get; set; } = string.Empty;

    // Vehicle Details
    public Guid VehicleId { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string VehicleMakeModel { get; set; } = string.Empty;

    // Customer Details
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhoneNumber { get; set; }

    // Timing Details
    public DateTime CheckInTime { get; set; }
    public DateTime CheckOutTime { get; set; }
    public DateTime ScheduledStartTime { get; set; }
    public DateTime ScheduledEndTime { get; set; }
    public int ActualDurationMinutes { get; set; }
    public int OverstayMinutes { get; set; }

    // Pricing & Fees
    public decimal BasePrice { get; set; }
    public decimal OverstayFee { get; set; }
    public decimal TotalAmount { get; set; }

    // Final Statuses
    public ReservationStatus ReservationStatus { get; set; } = ReservationStatus.Completed;
    public string ReservationStatusName => ReservationStatus.ToString();
    public SlotStatus SlotStatus { get; set; } = SlotStatus.Available;
    public string SlotStatusName => SlotStatus.ToString();

    // Attendant Attribution
    public Guid? AttendantCheckInId { get; set; }
    public Guid AttendantCheckOutId { get; set; }
    public string AttendantCheckOutName { get; set; } = string.Empty;

    // Exit Remarks
    public string? Notes { get; set; }
}
