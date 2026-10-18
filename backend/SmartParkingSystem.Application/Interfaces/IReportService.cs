using SmartParkingSystem.Application.DTOs.Reports;

namespace SmartParkingSystem.Application.Interfaces;

/// <summary>
/// Service interface for generating executive analytics reports across revenue,
/// reservations, facility occupancy, and customer activity.
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Computes financial revenue report supporting presets (daily, weekly, monthly, custom),
    /// time-series chart data, breakdown by payment method and area, and paginated transactions.
    /// </summary>
    Task<RevenueReportDto> GetRevenueReportAsync(ReportFilterRequest filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes reservation throughput report supporting presets, cancellation and completion rates,
    /// time-series chart data, status breakdown, and paginated reservation records.
    /// </summary>
    Task<ReservationReportDto> GetReservationReportAsync(ReportFilterRequest filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes facility occupancy report supporting presets, slot type and hourly distributions,
    /// period occupancy rates, and paginated facility inventory tables.
    /// </summary>
    Task<OccupancyReportDto> GetOccupancyReportAsync(ReportFilterRequest filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes customer analytics report supporting presets, registration trends,
    /// activity tiers, search criteria, and paginated customer activity tables.
    /// </summary>
    Task<CustomerReportDto> GetCustomerReportAsync(ReportFilterRequest filter, CancellationToken cancellationToken = default);
}
