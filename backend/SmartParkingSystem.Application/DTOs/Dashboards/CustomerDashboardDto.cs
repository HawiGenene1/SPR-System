using SmartParkingSystem.Application.DTOs.ParkingHistory;
using SmartParkingSystem.Application.DTOs.Reservations;
using SmartParkingSystem.Application.DTOs.Vehicles;

namespace SmartParkingSystem.Application.DTOs.Dashboards;

/// <summary>
/// Data transfer object representing the Customer Dashboard metrics, active/upcoming bookings,
/// registered vehicle fleet, financial totals, and recent parking history.
/// </summary>
public class CustomerDashboardDto
{
    // 1. Upcoming & Active Bookings
    public ReservationDto? UpcomingReservation { get; set; }
    public ReservationDto? ActiveReservation { get; set; }

    // 2. Reservation Metrics
    public int TotalReservations { get; set; }
    public int CompletedReservations { get; set; }
    public int ActiveReservationsCount { get; set; }
    public int UpcomingReservationsCount { get; set; }
    public int CancelledReservationsCount { get; set; }
    public int PastReservationsCount { get; set; }

    // 3. Financial Metrics (Total Payments & Count)
    public decimal TotalPayments { get; set; }
    public decimal TotalSpent
    {
        get => TotalPayments;
        set => TotalPayments = value;
    }
    public int TotalPaymentsCount { get; set; }

    // 4. Registered Vehicles
    public List<VehicleDto> RegisteredVehicles { get; set; } = new();
    public List<VehicleDto> Vehicles
    {
        get => RegisteredVehicles;
        set => RegisteredVehicles = value;
    }
    public int RegisteredVehiclesCount => RegisteredVehicles.Count;

    // 5. Recent Parking History
    public List<ParkingHistoryDto> RecentParkingHistory { get; set; } = new();

    // 6. Recent Reservations Feed (for quick card displays)
    public List<ReservationSummaryDto> RecentReservations { get; set; } = new();
}
