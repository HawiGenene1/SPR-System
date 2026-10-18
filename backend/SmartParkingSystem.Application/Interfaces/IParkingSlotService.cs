using SmartParkingSystem.Application.DTOs.ParkingSlots;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.Interfaces;

public interface IParkingSlotService
{
    /// <summary>
    /// Retrieve parking slots with optional filtering by area, floor, status, vehicle type, and availability.
    /// </summary>
    Task<IReadOnlyList<ParkingSlotDto>> GetSlotsAsync(
        Guid? parkingAreaId = null,
        int? floorNumber = null,
        SlotStatus? status = null,
        VehicleType? vehicleType = null,
        bool availableOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve details of a specific parking slot by ID.
    /// </summary>
    Task<ParkingSlotDto> GetSlotByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin: Create and register a new parking slot within a facility.
    /// </summary>
    Task<ParkingSlotDto> CreateSlotAsync(CreateParkingSlotRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin: Update an existing parking slot.
    /// </summary>
    Task<ParkingSlotDto> UpdateSlotAsync(Guid id, UpdateParkingSlotRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin: Set a parking slot into or out of Maintenance status.
    /// </summary>
    Task<ParkingSlotDto> SetMaintenanceStatusAsync(Guid id, bool isMaintenance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin: Soft delete a parking slot (blocked if active reservations exist).
    /// </summary>
    Task<bool> DeleteSlotAsync(Guid id, CancellationToken cancellationToken = default);
}
