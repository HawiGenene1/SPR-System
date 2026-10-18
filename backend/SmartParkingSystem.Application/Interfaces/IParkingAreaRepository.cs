using SmartParkingSystem.Domain.Entities;

namespace SmartParkingSystem.Application.Interfaces;

public interface IParkingAreaRepository
{
    /// <summary>
    /// Retrieve a parking area by ID.
    /// </summary>
    Task<ParkingArea?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve a parking area by ID including its associated slots.
    /// </summary>
    Task<ParkingArea?> GetByIdWithSlotsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve all parking facilities (optionally filtered by city or active state).
    /// </summary>
    Task<IReadOnlyList<ParkingArea>> GetAllAsync(bool includeInactive = false, string? city = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check whether a facility name is already registered (case-insensitive, optionally excluding an existing ID during updates).
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check whether any slot within the parking facility has active, confirmed, or pending reservations.
    /// </summary>
    Task<bool> HasActiveReservationsAsync(Guid parkingAreaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new parking facility to the database.
    /// </summary>
    Task<ParkingArea> AddAsync(ParkingArea parkingArea, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing parking facility.
    /// </summary>
    Task UpdateAsync(ParkingArea parkingArea, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft delete a parking facility and its slots.
    /// </summary>
    Task DeleteAsync(ParkingArea parkingArea, CancellationToken cancellationToken = default);
}
