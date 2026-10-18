using SmartParkingSystem.Application.DTOs.Dashboards;

namespace SmartParkingSystem.Application.Interfaces;

public interface IDashboardService
{
    /// <summary>
    /// Computes high-level KPIs, reservation and parking area breakdown, and chart series (revenue, reservations, occupancy, peak hours)
    /// using optimized database queries.
    /// </summary>
    Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes facility-scoped operational metrics for the parking attendant (today's check-ins/check-outs,
    /// live occupied/available slot inventory, active reservations, and recent vehicle entries).
    /// </summary>
    Task<AttendantDashboardDto> GetAttendantDashboardAsync(
        Guid currentUserId,
        bool isAdmin,
        Guid? parkingAreaId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes customer-specific dashboard metrics including active reservation, upcoming reservation,
    /// total and completed reservations count, total payments/spent, registered vehicles, and recent parking history.
    /// Strictly isolated to the given customer ID.
    /// </summary>
    Task<CustomerDashboardDto> GetCustomerDashboardAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
}
