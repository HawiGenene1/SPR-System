using SmartParkingSystem.Application.DTOs.Common;
using SmartParkingSystem.Application.DTOs.ParkingHistory;

namespace SmartParkingSystem.Application.Interfaces;

public interface IParkingHistoryService
{
    /// <summary>
    /// Retrieves paginated parking history for the currently authenticated customer with optional date and vehicle filters.
    /// </summary>
    Task<PagedResultDto<ParkingHistoryDto>> GetCustomerHistoryAsync(
        Guid customerId,
        ParkingHistoryFilterRequest filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin: Retrieves system-wide paginated parking history with date, facility, vehicle, and search filters.
    /// </summary>
    Task<PagedResultDto<ParkingHistoryDto>> GetAllHistoryAsync(
        ParkingHistoryFilterRequest filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific parking history record by ID, enforcing customer ownership isolation unless accessed by an Admin.
    /// </summary>
    Task<ParkingHistoryDto> GetHistoryByIdAsync(
        Guid id,
        Guid currentUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default);
}
