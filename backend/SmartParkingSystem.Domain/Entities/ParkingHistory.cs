using SmartParkingSystem.Domain.Common;

namespace SmartParkingSystem.Domain.Entities;

public class ParkingHistory : BaseEntity
{
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public int? OverstayDurationMinutes { get; set; }
    public decimal? OverstayFee { get; set; }
    public string? Notes { get; set; }

    // Foreign Keys
    public Guid ReservationId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid ParkingSlotId { get; set; }
    public Guid? AttendantCheckInId { get; set; }
    public Guid? AttendantCheckOutId { get; set; }

    // Navigation properties
    public Reservation Reservation { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
    public ParkingSlot ParkingSlot { get; set; } = null!;
    public User? AttendantCheckIn { get; set; }
    public User? AttendantCheckOut { get; set; }
}
