using Microsoft.Extensions.Logging;
using SmartParkingSystem.Application.Common.Validation;
using SmartParkingSystem.Application.DTOs.ParkingAreas;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Infrastructure.Services;

public class ParkingAreaService : IParkingAreaService
{
    private readonly IParkingAreaRepository _parkingAreaRepository;
    private readonly ILogger<ParkingAreaService> _logger;

    public ParkingAreaService(
        IParkingAreaRepository parkingAreaRepository,
        ILogger<ParkingAreaService> logger)
    {
        _parkingAreaRepository = parkingAreaRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ParkingAreaDto>> GetAllAsync(bool includeInactive = false, string? city = null, CancellationToken cancellationToken = default)
    {
        var areas = await _parkingAreaRepository.GetAllAsync(includeInactive, city, cancellationToken);
        return areas.Select(MapToDto).ToList();
    }

    public async Task<ParkingAreaDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var area = await _parkingAreaRepository.GetByIdWithSlotsAsync(id, cancellationToken);
        if (area == null)
        {
            throw new KeyNotFoundException($"Parking area with ID '{id}' was not found.");
        }

        return MapToDto(area);
    }

    public async Task<ParkingAreaDto> CreateAsync(CreateParkingAreaRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Validate Operating Hours
        if (!ParkingAreaValidators.IsValidOperatingHours(request.OperatingHours))
        {
            throw new ArgumentException("Operating hours must be a valid schedule (e.g., '24/7' or '06:00 - 23:00').");
        }

        // 2. Validate Capacity
        if (!ParkingAreaValidators.IsValidCapacity(request.TotalSlots))
        {
            throw new ArgumentException("Total capacity must be between 1 and 10,000.");
        }

        // 3. Prevent Duplicate Facility Names
        var nameExists = await _parkingAreaRepository.ExistsByNameAsync(request.Name, null, cancellationToken);
        if (nameExists)
        {
            throw new InvalidOperationException($"A parking area with the name '{request.Name}' already exists.");
        }

        var area = new ParkingArea
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Address = request.Address.Trim(),
            City = request.City.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            TotalSlots = request.TotalSlots,
            HourlyRate = request.HourlyRate,
            OperatingHours = string.IsNullOrWhiteSpace(request.OperatingHours) ? "24/7" : request.OperatingHours.Trim(),
            IsActive = true
        };

        var created = await _parkingAreaRepository.AddAsync(area, cancellationToken);
        _logger.LogInformation("Parking area created successfully: {ParkingAreaId} - {Name}", created.Id, created.Name);

        return MapToDto(created);
    }

    public async Task<ParkingAreaDto> UpdateAsync(Guid id, UpdateParkingAreaRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Validate Operating Hours
        if (!ParkingAreaValidators.IsValidOperatingHours(request.OperatingHours))
        {
            throw new ArgumentException("Operating hours must be a valid schedule (e.g., '24/7' or '06:00 - 23:00').");
        }

        // 2. Validate Capacity
        if (!ParkingAreaValidators.IsValidCapacity(request.TotalSlots))
        {
            throw new ArgumentException("Total capacity must be between 1 and 10,000.");
        }

        var area = await _parkingAreaRepository.GetByIdWithSlotsAsync(id, cancellationToken);
        if (area == null)
        {
            throw new KeyNotFoundException($"Parking area with ID '{id}' was not found.");
        }

        // 3. Prevent Duplicate Facility Names (excluding current entity)
        var nameExists = await _parkingAreaRepository.ExistsByNameAsync(request.Name, id, cancellationToken);
        if (nameExists)
        {
            throw new InvalidOperationException($"A parking area with the name '{request.Name}' already exists.");
        }

        area.Name = request.Name.Trim();
        area.Description = request.Description?.Trim();
        area.Address = request.Address.Trim();
        area.City = request.City.Trim();
        area.Latitude = request.Latitude;
        area.Longitude = request.Longitude;
        area.TotalSlots = request.TotalSlots;
        area.HourlyRate = request.HourlyRate;
        area.OperatingHours = string.IsNullOrWhiteSpace(request.OperatingHours) ? "24/7" : request.OperatingHours.Trim();
        area.IsActive = request.IsActive;

        await _parkingAreaRepository.UpdateAsync(area, cancellationToken);
        _logger.LogInformation("Parking area {ParkingAreaId} ({Name}) updated successfully.", area.Id, area.Name);

        return MapToDto(area);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var area = await _parkingAreaRepository.GetByIdWithSlotsAsync(id, cancellationToken);
        if (area == null)
        {
            throw new KeyNotFoundException($"Parking area with ID '{id}' was not found.");
        }

        // Rule: Prevent deletion if parking area has active, confirmed, or pending reservations
        var hasActiveReservations = await _parkingAreaRepository.HasActiveReservationsAsync(id, cancellationToken);
        if (hasActiveReservations)
        {
            _logger.LogWarning("Deletion rejected: Parking area {ParkingAreaId} has active reservations.", id);
            throw new InvalidOperationException("Cannot delete parking area because it has active, confirmed, or pending reservations.");
        }

        await _parkingAreaRepository.DeleteAsync(area, cancellationToken);
        _logger.LogInformation("Parking area {ParkingAreaId} and associated slots soft-deleted successfully.", id);

        return true;
    }

    private static ParkingAreaDto MapToDto(ParkingArea area)
    {
        var slots = area.ParkingSlots?.Where(s => !s.IsDeleted).ToList() ?? new List<ParkingSlot>();
        var occupied = slots.Count(s => s.Status == SlotStatus.Occupied || s.Status == SlotStatus.Reserved);
        var available = slots.Count > 0 
            ? slots.Count(s => s.IsActive && s.Status == SlotStatus.Available)
            : area.TotalSlots;

        return new ParkingAreaDto
        {
            Id = area.Id,
            Name = area.Name,
            Description = area.Description,
            Address = area.Address,
            City = area.City,
            Latitude = area.Latitude,
            Longitude = area.Longitude,
            TotalSlots = area.TotalSlots,
            AvailableSlots = available,
            OccupiedSlots = occupied,
            HourlyRate = area.HourlyRate,
            OperatingHours = area.OperatingHours,
            IsActive = area.IsActive,
            CreatedAt = area.CreatedAt,
            UpdatedAt = area.UpdatedAt
        };
    }
}
