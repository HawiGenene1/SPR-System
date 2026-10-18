using SmartParkingSystem.Application.DTOs.Reservations;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.DTOs.Dashboards;

public class AttendantDashboardDto
{
    // Facility Context
    public Guid? AssignedParkingAreaId { get; set; }
    public string? AssignedParkingAreaName { get; set; }
    public string? FacilityAddress { get; set; }

    // Today's Operational Activity
    public int TodaysCheckIns { get; set; }
    public int TodayCheckIns => TodaysCheckIns;
    public int CheckInsToday
    {
        get => TodaysCheckIns;
        set => TodaysCheckIns = value;
    }

    public int TodaysCheckOuts { get; set; }
    public int TodayCheckOuts => TodaysCheckOuts;
    public int CheckOutsToday
    {
        get => TodaysCheckOuts;
        set => TodaysCheckOuts = value;
    }

    // Live Slot Inventory
    public int TotalSlots { get; set; }
    public int AvailableSlots { get; set; }
    public int CurrentOccupiedSlots { get; set; }
    public int OccupiedSlots
    {
        get => CurrentOccupiedSlots;
        set => CurrentOccupiedSlots = value;
    }
    public int ReservedSlots { get; set; }
    public int MaintenanceSlots { get; set; }
    public double OccupancyRatePercentage { get; set; }

    // Reservation Context
    public int ActiveReservations { get; set; }
    public int ActiveReservationsCount
    {
        get => ActiveReservations;
        set => ActiveReservations = value;
    }
    public int ConfirmedArrivalsToday { get; set; }
    public int PendingArrivalsToday { get; set; }

    // Recent Vehicle Entries List
    public List<RecentVehicleEntryDto> RecentVehicleEntries { get; set; } = new();

    // Backward-compatible Recent Activity alias
    public List<ReservationSummaryDto> RecentActivity { get; set; } = new();
}

public class RecentVehicleEntryDto
{
    public Guid ParkingHistoryId { get; set; }
    public Guid ReservationId { get; set; }
    public string ReservationNumber { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public string VehicleMakeModel { get; set; } = string.Empty;
    public VehicleType VehicleType { get; set; }
    public string VehicleTypeName => VehicleType.ToString();
    public string SlotNumber { get; set; } = string.Empty;
    public int FloorNumber { get; set; }
    public string ParkingAreaName { get; set; } = string.Empty;
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public bool IsCurrentlyParked => CheckOutTime == null;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhoneNumber { get; set; }
    public string? AttendantName { get; set; }
}
