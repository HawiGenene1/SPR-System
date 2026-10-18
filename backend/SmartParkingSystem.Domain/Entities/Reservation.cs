using SmartParkingSystem.Domain.Common;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Domain.Entities;

public class Reservation : BaseEntity
{
    public string ReservationNumber { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal TotalPrice { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    // Foreign Keys
    public Guid UserId { get; set; }
    public Guid ParkingSlotId { get; set; }
    public Guid VehicleId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ParkingSlot ParkingSlot { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;

    public Payment? Payment { get; set; }
    public QRCode? QRCode { get; set; }
    public ParkingHistory? ParkingHistory { get; set; }
}
