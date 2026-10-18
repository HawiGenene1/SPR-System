using SmartParkingSystem.Domain.Entities;

namespace SmartParkingSystem.Application.Interfaces;

public interface IVehicleRepository
{
    /// <summary>
    /// Retrieve a vehicle by its unique identifier.
    /// </summary>
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve all active vehicles belonging to a specific user.
    /// </summary>
    Task<IReadOnlyList<Vehicle>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check whether a license plate already exists in the system (optionally excluding a specific vehicle ID during updates).
    /// </summary>
    Task<bool> ExistsByPlateAsync(string licensePlate, Guid? excludeVehicleId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a vehicle has any pending, confirmed, or active reservations that prevent deletion or reassignment.
    /// </summary>
    Task<bool> HasActiveReservationsAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new vehicle to the database.
    /// </summary>
    Task<Vehicle> AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing vehicle's attributes.
    /// </summary>
    Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft delete a vehicle from the system.
    /// </summary>
    Task DeleteAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
}
