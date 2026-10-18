using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartParkingSystem.Application.Common.Validation;
using SmartParkingSystem.Application.DTOs.ParkingSlots;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;
using SmartParkingSystem.Infrastructure.Data;

namespace SmartParkingSystem.Infrastructure.Services;

public class ParkingSlotService : IParkingSlotService
{
    private readonly IParkingSlotRepository _parkingSlotRepository;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ParkingSlotService> _logger;

    public ParkingSlotService(
        IParkingSlotRepository parkingSlotRepository,
        ApplicationDbContext context,
        ILogger<ParkingSlotService> logger)
    {
        _parkingSlotRepository = parkingSlotRepository;
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ParkingSlotDto>> GetSlotsAsync(
        Guid? parkingAreaId = null,
        int? floorNumber = null,
        SlotStatus? status = null,
        VehicleType? vehicleType = null,
        bool availableOnly = false,
        CancellationToken cancellationToken = default)
    {
        var slots = await _parkingSlotRepository.GetSlotsAsync(
            parkingAreaId, floorNumber, status, vehicleType, availableOnly, cancellationToken);

        return slots.Select(MapToDto).ToList();
    }

    public async Task<ParkingSlotDto> GetSlotByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var slot = await _parkingSlotRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (slot == null)
        {
            throw new KeyNotFoundException($"Parking slot with ID '{id}' was not found.");
        }

        return MapToDto(slot);
    }

    public async Task<ParkingSlotDto> CreateSlotAsync(CreateParkingSlotRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Verify parent parking area exists
        var area = await _context.ParkingAreas.FirstOrDefaultAsync(p => p.Id == request.ParkingAreaId && !p.IsDeleted, cancellationToken);
        if (area == null)
        {
            throw new KeyNotFoundException($"Parking area with ID '{request.ParkingAreaId}' was not found.");
        }

        // 2. Validate Slot Number format
        if (!ParkingSlotValidators.IsValidSlotNumber(request.SlotNumber))
        {
            throw new ArgumentException("Slot number must contain between 1 and 20 alphanumeric characters or hyphens.");
        }

        // 3. Validate Floor
        if (!ParkingSlotValidators.IsValidFloor(request.FloorNumber))
        {
            throw new ArgumentException("Floor number must be between -5 and 50.");
        }

        // 4. Rule: Slot number must be unique within a parking area
        var normalizedSlot = request.SlotNumber.Trim().ToUpperInvariant();
        var exists = await _parkingSlotRepository.ExistsSlotNumberAsync(request.ParkingAreaId, normalizedSlot, null, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Slot number '{request.SlotNumber}' already exists in parking facility '{area.Name}'.");
        }

        var slot = new ParkingSlot
        {
            ParkingAreaId = request.ParkingAreaId,
            SlotNumber = normalizedSlot,
            FloorNumber = request.FloorNumber,
            SlotType = request.SlotType,
            VehicleType = request.VehicleType,
            HourlyRate = request.HourlyRate,
            Status = SlotStatus.Available,
            IsActive = true
        };

        var created = await _parkingSlotRepository.AddAsync(slot, cancellationToken);
        _logger.LogInformation("Parking slot {SlotId} ({SlotNumber}) created in area {AreaId}.", created.Id, created.SlotNumber, area.Id);

        created.ParkingArea = area;
        return MapToDto(created);
    }

    public async Task<ParkingSlotDto> UpdateSlotAsync(Guid id, UpdateParkingSlotRequest request, CancellationToken cancellationToken = default)
    {
        var slot = await _parkingSlotRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (slot == null)
        {
            throw new KeyNotFoundException($"Parking slot with ID '{id}' was not found.");
        }

        // 1. Slot Number update & uniqueness check
        if (!string.IsNullOrWhiteSpace(request.SlotNumber))
        {
            var normalizedSlot = request.SlotNumber.Trim().ToUpperInvariant();
            if (!string.Equals(slot.SlotNumber, normalizedSlot, StringComparison.OrdinalIgnoreCase))
            {
                if (!ParkingSlotValidators.IsValidSlotNumber(normalizedSlot))
                {
                    throw new ArgumentException("Slot number must contain between 1 and 20 alphanumeric characters or hyphens.");
                }

                var exists = await _parkingSlotRepository.ExistsSlotNumberAsync(slot.ParkingAreaId, normalizedSlot, id, cancellationToken);
                if (exists)
                {
                    throw new InvalidOperationException($"Slot number '{request.SlotNumber}' already exists in parking facility '{slot.ParkingArea?.Name}'.");
                }

                slot.SlotNumber = normalizedSlot;
            }
        }

        // 2. Floor Number
        if (request.FloorNumber.HasValue)
        {
            if (!ParkingSlotValidators.IsValidFloor(request.FloorNumber.Value))
            {
                throw new ArgumentException("Floor number must be between -5 and 50.");
            }
            slot.FloorNumber = request.FloorNumber.Value;
        }

        // 3. Slot Type & Vehicle Type
        if (request.SlotType.HasValue)
        {
            slot.SlotType = request.SlotType.Value;
        }

        if (request.VehicleType.HasValue)
        {
            slot.VehicleType = request.VehicleType.Value;
        }

        // 4. Hourly Rate (Price per hour)
        if (request.HourlyRate.HasValue)
        {
            slot.HourlyRate = request.HourlyRate.Value;
        }

        // 5. Status Transition with reservation check
        if (request.Status.HasValue)
        {
            if (request.Status.Value == SlotStatus.Maintenance && slot.Status != SlotStatus.Maintenance)
            {
                var hasActive = await _parkingSlotRepository.HasActiveReservationsAsync(id, cancellationToken);
                if (hasActive)
                {
                    throw new InvalidOperationException("Cannot put parking slot into maintenance because active or pending reservations exist for it.");
                }
            }

            slot.Status = request.Status.Value;
        }

        // 6. Active state
        if (request.IsActive.HasValue)
        {
            slot.IsActive = request.IsActive.Value;
        }

        await _parkingSlotRepository.UpdateAsync(slot, cancellationToken);
        _logger.LogInformation("Parking slot {SlotId} ({SlotNumber}) updated successfully.", slot.Id, slot.SlotNumber);

        return MapToDto(slot);
    }

    public async Task<ParkingSlotDto> SetMaintenanceStatusAsync(Guid id, bool isMaintenance, CancellationToken cancellationToken = default)
    {
        var slot = await _parkingSlotRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (slot == null)
        {
            throw new KeyNotFoundException($"Parking slot with ID '{id}' was not found.");
        }

        if (isMaintenance)
        {
            var hasActive = await _parkingSlotRepository.HasActiveReservationsAsync(id, cancellationToken);
            if (hasActive)
            {
                throw new InvalidOperationException("Cannot put parking slot into maintenance because active or pending reservations exist for it.");
            }

            slot.Status = SlotStatus.Maintenance;
            _logger.LogInformation("Parking slot {SlotId} ({SlotNumber}) placed into MAINTENANCE.", slot.Id, slot.SlotNumber);
        }
        else
        {
            slot.Status = SlotStatus.Available;
            _logger.LogInformation("Parking slot {SlotId} ({SlotNumber}) returned to AVAILABLE.", slot.Id, slot.SlotNumber);
        }

        await _parkingSlotRepository.UpdateAsync(slot, cancellationToken);
        return MapToDto(slot);
    }

    public async Task<bool> DeleteSlotAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var slot = await _parkingSlotRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (slot == null)
        {
            throw new KeyNotFoundException($"Parking slot with ID '{id}' was not found.");
        }

        // Rule: Prevent deletion if parking slot has active reservations
        var hasActive = await _parkingSlotRepository.HasActiveReservationsAsync(id, cancellationToken);
        if (hasActive)
        {
            _logger.LogWarning("Deletion rejected: Parking slot {SlotId} has active reservations.", id);
            throw new InvalidOperationException("Cannot delete parking slot because it has active, confirmed, or pending reservations.");
        }

        await _parkingSlotRepository.DeleteAsync(slot, cancellationToken);
        _logger.LogInformation("Parking slot {SlotId} ({SlotNumber}) soft-deleted successfully.", id, slot.SlotNumber);

        return true;
    }

    private static ParkingSlotDto MapToDto(ParkingSlot slot)
    {
        var effectivePrice = slot.HourlyRate ?? slot.ParkingArea?.HourlyRate ?? 0m;

        return new ParkingSlotDto
        {
            Id = slot.Id,
            SlotNumber = slot.SlotNumber,
            FloorNumber = slot.FloorNumber,
            SlotType = slot.SlotType,
            VehicleType = slot.VehicleType,
            Status = slot.Status,
            IsActive = slot.IsActive,
            ParkingAreaId = slot.ParkingAreaId,
            ParkingAreaName = slot.ParkingArea?.Name,
            HourlyRate = slot.HourlyRate,
            PricePerHour = effectivePrice,
            CreatedAt = slot.CreatedAt,
            UpdatedAt = slot.UpdatedAt
        };
    }
}
