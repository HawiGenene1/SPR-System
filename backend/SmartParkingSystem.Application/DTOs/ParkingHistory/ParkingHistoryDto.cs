using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.DTOs.ParkingHistory;

public class ParkingHistoryDto
{
    public Guid Id { get; set; }

    // Reservation Details
    public Guid ReservationId { get; set; }
    public string ReservationNumber { get; set; } = string.Empty;
    public ReservationStatus ReservationStatus { get; set; }
    public string ReservationStatusName => ReservationStatus.ToString();
    public DateTime ScheduledStartTime { get; set; }
    public DateTime ScheduledEndTime { get; set; }

    // Actual Entry & Exit Timestamps
    public DateTime ActualEntry { get; set; }
    public DateTime CheckInTime => ActualEntry;
    public DateTime? ActualExit { get; set; }
    public DateTime? CheckOutTime => ActualExit;

    // Duration in Minutes (actual elapsed stay)
    public int DurationMinutes { get; set; }
    public int Duration => DurationMinutes;

    // Pricing & Fees
    public decimal BaseAmount { get; set; }
    public decimal Amount => BaseAmount;
    public decimal AdditionalFee { get; set; }
    public decimal? OverstayFee => AdditionalFee > 0 ? AdditionalFee : null;
    public decimal TotalAmount { get; set; }

    // Vehicle Details
    public Guid VehicleId { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string VehicleMakeModel { get; set; } = string.Empty;
    public VehicleType VehicleType { get; set; }
    public string VehicleTypeName => VehicleType.ToString();

    // Parking Area Details
    public Guid ParkingAreaId { get; set; }
    public string ParkingAreaName { get; set; } = string.Empty;
    public string ParkingAreaAddress { get; set; } = string.Empty;

    // Slot Details
    public Guid ParkingSlotId { get; set; }
    public string SlotNumber { get; set; } = string.Empty;
    public int FloorNumber { get; set; }

    // Customer Attribution
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerPhoneNumber { get; set; }

    // Staff Attribution
    public Guid? AttendantCheckInId { get; set; }
    public string? AttendantCheckInName { get; set; }
    public Guid? AttendantCheckOutId { get; set; }
    public string? AttendantCheckOutName { get; set; }

    // Notes & Metadata
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
