using SmartParkingSystem.Application.DTOs.Vehicles;

namespace SmartParkingSystem.Application.Interfaces;

public interface IVehicleService
{
    /// <summary>
    /// Register a new vehicle for the authenticated customer.
    /// </summary>
    Task<VehicleDto> CreateVehicleAsync(Guid userId, CreateVehicleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve all registered vehicles belonging to the specified user.
    /// </summary>
    Task<IReadOnlyList<VehicleDto>> GetMyVehiclesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve details of a specific vehicle, verifying that the caller is the owner or an administrator.
    /// </summary>
    Task<VehicleDto> GetVehicleByIdAsync(Guid vehicleId, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update details of an existing vehicle, verifying ownership and ensuring license plate uniqueness.
    /// </summary>
    Task<VehicleDto> UpdateVehicleAsync(Guid vehicleId, Guid currentUserId, UpdateVehicleRequest request, bool isAdmin = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft delete a vehicle, verifying ownership and ensuring no active or pending reservations exist.
    /// </summary>
    Task<bool> DeleteVehicleAsync(Guid vehicleId, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate that a vehicle belongs to the given user and has not been deleted before assigning to a reservation.
    /// </summary>
    Task<bool> ValidateVehicleOwnershipForReservationAsync(Guid vehicleId, Guid currentUserId, CancellationToken cancellationToken = default);
}
