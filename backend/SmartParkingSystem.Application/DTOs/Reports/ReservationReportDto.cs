using SmartParkingSystem.Application.DTOs.Common;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.DTOs.Reports;

/// <summary>
/// Reservation throughput analytics report suitable for Angular charts and tables.
/// Includes KPI summaries, cancellation and completion rates, period time-series,
/// area breakdown, and paginated reservation records.
/// </summary>
public class ReservationReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Period { get; set; } = string.Empty;

    // Summary KPIs
    public int TotalReservations { get; set; }
    public int CompletedReservations { get; set; }
    public int CancelledReservations { get; set; }
    public int ActiveReservations { get; set; }
    public int PendingReservations { get; set; }
    public int ExpiredReservations { get; set; }
    public double CancellationRatePercentage { get; set; }
    public double CompletionRatePercentage { get; set; }
    public double AverageDurationMinutes { get; set; }
    public decimal TotalRevenue { get; set; }

    // Chart Series
    public Dictionary<string, int> ReservationsByStatus { get; set; } = new();
    public List<ParkingAreaReservationStatDto> ReservationsByArea { get; set; } = new();
    public List<PeriodReservationDto> ReservationsByPeriod { get; set; } = new();

    // Tabular Paginated Items
    public PagedResultDto<ReservationReportItemDto> Reservations { get; set; } = new();
}

public class ParkingAreaReservationStatDto
{
    public Guid ParkingAreaId { get; set; }
    public string ParkingAreaName { get; set; } = string.Empty;
    public int TotalReservations { get; set; }
    public decimal TotalRevenue { get; set; }
    public double Percentage { get; set; }
}

public class PeriodReservationDto
{
    public DateTime Date { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public int TotalReservations { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
    public int ActiveCount { get; set; }
    public decimal Revenue { get; set; }
}

public class ReservationReportItemDto
{
    public Guid Id { get; set; }
    public string ReservationNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ParkingAreaName { get; set; } = string.Empty;
    public string SlotNumber { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public decimal TotalPrice { get; set; }
    public ReservationStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public DateTime CreatedAt { get; set; }
}
