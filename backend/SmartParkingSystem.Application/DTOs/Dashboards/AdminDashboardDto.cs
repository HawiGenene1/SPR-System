using SmartParkingSystem.Application.DTOs.Reservations;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.DTOs.Dashboards;

public class AdminDashboardDto
{
    // User metrics
    public int TotalUsers { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalAttendants { get; set; }

    // Parking area and slot metrics
    public int TotalParkingAreas { get; set; }
    public int TotalParkingSlots { get; set; }
    public int TotalSlots
    {
        get => TotalParkingSlots;
        set => TotalParkingSlots = value;
    }
    public int AvailableSlots { get; set; }
    public int ReservedSlots { get; set; }
    public int OccupiedSlots { get; set; }
    public int MaintenanceSlots { get; set; }
    public double OccupancyRatePercentage { get; set; }

    // Reservation metrics
    public int TodaysReservations { get; set; }
    public int TodayReservations
    {
        get => TodaysReservations;
        set => TodaysReservations = value;
    }
    public int ActiveReservations { get; set; }
    public int ActiveReservationsCount
    {
        get => ActiveReservations;
        set => ActiveReservations = value;
    }
    public int CompletedReservations { get; set; }
    public int CompletedReservationsCount
    {
        get => CompletedReservations;
        set => CompletedReservations = value;
    }
    public int CancelledReservations { get; set; }
    public int CancelledReservationsCount
    {
        get => CancelledReservations;
        set => CancelledReservations = value;
    }

    // Revenue metrics
    public decimal TodaysRevenue { get; set; }
    public decimal TodayRevenue
    {
        get => TodaysRevenue;
        set => TodaysRevenue = value;
    }
    public decimal MonthlyRevenue { get; set; }
    public decimal TotalRevenue { get; set; }

    // Chart Data
    public List<DailyRevenueChartDto> RevenueByDay { get; set; } = new();
    public List<DailyReservationChartDto> ReservationsByDay { get; set; } = new();
    public OccupancyStatisticsDto OccupancyStatistics { get; set; } = new();
    public List<HourlyPeakChartDto> PeakParkingHours { get; set; } = new();

    // Recent activity list
    public List<ReservationSummaryDto> RecentReservations { get; set; } = new();
}

public class DailyRevenueChartDto
{
    public DateTime Date { get; set; }
    public string DateLabel { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int TransactionsCount { get; set; }
}

public class DailyReservationChartDto
{
    public DateTime Date { get; set; }
    public string DateLabel { get; set; } = string.Empty;
    public int TotalReservations { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
    public int ActiveCount { get; set; }
}

public class OccupancyStatisticsDto
{
    public double OverallOccupancyRatePercentage { get; set; }
    public int TotalSlots { get; set; }
    public int OccupiedSlots { get; set; }
    public int ReservedSlots { get; set; }
    public int AvailableSlots { get; set; }
    public int MaintenanceSlots { get; set; }
    public List<AreaOccupancyChartDto> ByParkingArea { get; set; } = new();
    public List<SlotTypeOccupancyChartDto> BySlotType { get; set; } = new();
}

public class AreaOccupancyChartDto
{
    public Guid ParkingAreaId { get; set; }
    public string ParkingAreaName { get; set; } = string.Empty;
    public int TotalSlots { get; set; }
    public int OccupiedSlots { get; set; }
    public int ReservedSlots { get; set; }
    public int AvailableSlots { get; set; }
    public int MaintenanceSlots { get; set; }
    public double OccupancyRatePercentage { get; set; }
}

public class SlotTypeOccupancyChartDto
{
    public SlotType SlotType { get; set; }
    public string SlotTypeName => SlotType.ToString();
    public int TotalSlots { get; set; }
    public int OccupiedSlots { get; set; }
    public int ReservedSlots { get; set; }
    public int AvailableSlots { get; set; }
    public double OccupancyRatePercentage { get; set; }
}

public class HourlyPeakChartDto
{
    public int Hour { get; set; }
    public string HourLabel { get; set; } = string.Empty;
    public int ReservationCount { get; set; }
    public double Percentage { get; set; }
    public bool IsPeakHour { get; set; }
}
