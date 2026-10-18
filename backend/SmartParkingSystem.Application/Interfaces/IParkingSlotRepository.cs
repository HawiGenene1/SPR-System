using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.Interfaces;

public interface IParkingSlotRepository
{
    /// <summary>
    /// Retrieve a parking slot by ID.
    /// </summary>
    Task<ParkingSlot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve a parking slot by ID including parent parking area.
    /// </summary>
    Task<ParkingSlot?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Query parking slots across facilities or within a specific area, with optional filters.
    /// </summary>
    Task<IReadOnlyList<ParkingSlot>> GetSlotsAsync(
        Guid? parkingAreaId = null,
        int? floorNumber = null,
        SlotStatus? status = null,
        VehicleType? vehicleType = null,
        bool availableOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check whether a slot number is already registered within a given parking area.
    /// </summary>
    Task<bool> ExistsSlotNumberAsync(Guid parkingAreaId, string slotNumber, Guid? excludeSlotId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check whether a parking slot has any pending, confirmed, or active reservations.
    /// </summary>
    Task<bool> HasActiveReservationsAsync(Guid slotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new parking slot to the database.
    /// </summary>
    Task<ParkingSlot> AddAsync(ParkingSlot slot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing parking slot.
    /// </summary>
    Task UpdateAsync(ParkingSlot slot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft delete a parking slot.
    /// </summary>
    Task DeleteAsync(ParkingSlot slot, CancellationToken cancellationToken = default);
}
