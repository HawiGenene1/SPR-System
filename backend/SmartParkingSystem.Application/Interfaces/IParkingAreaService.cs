using SmartParkingSystem.Application.DTOs.ParkingAreas;

namespace SmartParkingSystem.Application.Interfaces;

public interface IParkingAreaService
{
    /// <summary>
    /// Retrieve all parking facilities with live availability metrics.
    /// </summary>
    Task<IReadOnlyList<ParkingAreaDto>> GetAllAsync(bool includeInactive = false, string? city = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve details of a specific parking facility by ID.
    /// </summary>
    Task<ParkingAreaDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin: Create and register a new parking facility.
    /// </summary>
    Task<ParkingAreaDto> CreateAsync(CreateParkingAreaRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin: Update an existing parking facility's details, operating hours, capacity, and rates.
    /// </summary>
    Task<ParkingAreaDto> UpdateAsync(Guid id, UpdateParkingAreaRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin: Soft delete a parking facility, preventing deletion if active reservations exist.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
