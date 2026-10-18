using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartParkingSystem.Application.Common.Validation;
using SmartParkingSystem.Application.DTOs.Vehicles;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Infrastructure.Data;

namespace SmartParkingSystem.Infrastructure.Services;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VehicleService> _logger;

    public VehicleService(
        IVehicleRepository vehicleRepository,
        ApplicationDbContext context,
        ILogger<VehicleService> logger)
    {
        _vehicleRepository = vehicleRepository;
        _context = context;
        _logger = logger;
    }

    public async Task<VehicleDto> CreateVehicleAsync(Guid userId, CreateVehicleRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Validate Vehicle Type
        if (!VehicleValidators.IsValidVehicleType(request.VehicleType))
        {
            throw new ArgumentException("Invalid vehicle type specified.");
        }

        // 2. Validate and Normalize Plate
        if (!VehicleValidators.IsValidPlate(request.LicensePlate))
        {
            throw new ArgumentException("License plate must contain 2 to 20 valid alphanumeric characters, spaces, or hyphens.");
        }

        var normalizedPlate = VehicleValidators.NormalizePlate(request.LicensePlate);

        // 3. Verify User exists
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID '{userId}' was not found.");
        }

        // 4. Check for duplicate plate across active vehicles
        var exists = await _vehicleRepository.ExistsByPlateAsync(normalizedPlate, null, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Vehicle with license plate '{request.LicensePlate}' is already registered.");
        }

        // 5. Create Entity
        var vehicle = new Vehicle
        {
            LicensePlate = normalizedPlate,
            VehicleType = request.VehicleType,
            Make = request.Make.Trim(),
            Model = request.Model.Trim(),
            Color = request.Color?.Trim(),
            UserId = userId,
            IsDeleted = false
        };

        var createdVehicle = await _vehicleRepository.AddAsync(vehicle, cancellationToken);
        _logger.LogInformation("Vehicle {VehicleId} ({Plate}) registered successfully for User {UserId}.", createdVehicle.Id, createdVehicle.LicensePlate, userId);

        return MapToDto(createdVehicle, user.FullName);
    }

    public async Task<IReadOnlyList<VehicleDto>> GetMyVehiclesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var vehicles = await _vehicleRepository.GetByUserIdAsync(userId, cancellationToken);
        return vehicles.Select(v => MapToDto(v, v.User?.FullName)).ToList();
    }

    public async Task<VehicleDto> GetVehicleByIdAsync(Guid vehicleId, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId, cancellationToken);
        if (vehicle == null)
        {
            throw new KeyNotFoundException($"Vehicle with ID '{vehicleId}' was not found.");
        }

        // Rule: Customer can only access their own vehicles
        if (vehicle.UserId != currentUserId && !isAdmin)
        {
            _logger.LogWarning("Unauthorized vehicle access attempt: User {UserId} tried to access Vehicle {VehicleId} owned by User {OwnerId}.",
                currentUserId, vehicleId, vehicle.UserId);
            throw new UnauthorizedAccessException("Access denied: You can only access your own vehicles.");
        }

        return MapToDto(vehicle, vehicle.User?.FullName);
    }

    public async Task<VehicleDto> UpdateVehicleAsync(Guid vehicleId, Guid currentUserId, UpdateVehicleRequest request, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        // 1. Validate Vehicle Type
        if (!VehicleValidators.IsValidVehicleType(request.VehicleType))
        {
            throw new ArgumentException("Invalid vehicle type specified.");
        }

        // 2. Validate and Normalize Plate
        if (!VehicleValidators.IsValidPlate(request.LicensePlate))
        {
            throw new ArgumentException("License plate must contain 2 to 20 valid alphanumeric characters, spaces, or hyphens.");
        }

        var normalizedPlate = VehicleValidators.NormalizePlate(request.LicensePlate);

        // 3. Find existing vehicle
        var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId, cancellationToken);
        if (vehicle == null)
        {
            throw new KeyNotFoundException($"Vehicle with ID '{vehicleId}' was not found.");
        }

        // Rule: Customer can only modify their own vehicles
        if (vehicle.UserId != currentUserId && !isAdmin)
        {
            _logger.LogWarning("Unauthorized vehicle update attempt: User {UserId} tried to modify Vehicle {VehicleId} owned by User {OwnerId}.",
                currentUserId, vehicleId, vehicle.UserId);
            throw new UnauthorizedAccessException("Access denied: You can only modify your own vehicles.");
        }

        // 4. Check for duplicate plate or active reservations if plate changed
        if (!string.Equals(vehicle.LicensePlate, normalizedPlate, StringComparison.OrdinalIgnoreCase))
        {
            var hasActiveReservations = await _vehicleRepository.HasActiveReservationsAsync(vehicleId, cancellationToken);
            if (hasActiveReservations)
            {
                throw new InvalidOperationException("Cannot change license plate while vehicle is assigned to active or pending reservations.");
            }

            var exists = await _vehicleRepository.ExistsByPlateAsync(normalizedPlate, vehicleId, cancellationToken);
            if (exists)
            {
                throw new InvalidOperationException($"Vehicle with license plate '{request.LicensePlate}' is already registered.");
            }
        }

        // 5. Update vehicle fields
        vehicle.LicensePlate = normalizedPlate;
        vehicle.VehicleType = request.VehicleType;
        vehicle.Make = request.Make.Trim();
        vehicle.Model = request.Model.Trim();
        vehicle.Color = request.Color?.Trim();

        await _vehicleRepository.UpdateAsync(vehicle, cancellationToken);
        _logger.LogInformation("Vehicle {VehicleId} ({Plate}) updated successfully by User {UserId}.", vehicleId, vehicle.LicensePlate, currentUserId);

        return MapToDto(vehicle, vehicle.User?.FullName);
    }

    public async Task<bool> DeleteVehicleAsync(Guid vehicleId, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId, cancellationToken);
        if (vehicle == null)
        {
            throw new KeyNotFoundException($"Vehicle with ID '{vehicleId}' was not found.");
        }

        // Rule: Customer can only delete their own vehicles
        if (vehicle.UserId != currentUserId && !isAdmin)
        {
            _logger.LogWarning("Unauthorized vehicle deletion attempt: User {UserId} tried to delete Vehicle {VehicleId} owned by User {OwnerId}.",
                currentUserId, vehicleId, vehicle.UserId);
            throw new UnauthorizedAccessException("Access denied: You can only delete your own vehicles.");
        }

        // Rule: A vehicle cannot be deleted if assigned to active/pending reservations
        var hasActiveReservations = await _vehicleRepository.HasActiveReservationsAsync(vehicleId, cancellationToken);
        if (hasActiveReservations)
        {
            _logger.LogWarning("Attempted to delete Vehicle {VehicleId} which has active or pending reservations.", vehicleId);
            throw new InvalidOperationException("Cannot delete vehicle because it is currently assigned to active or pending reservations.");
        }

        await _vehicleRepository.DeleteAsync(vehicle, cancellationToken);
        _logger.LogInformation("Vehicle {VehicleId} soft-deleted successfully by User {UserId}.", vehicleId, currentUserId);

        return true;
    }

    public async Task<bool> ValidateVehicleOwnershipForReservationAsync(Guid vehicleId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId, cancellationToken);
        if (vehicle == null)
        {
            return false;
        }

        // Vehicle must belong to the user making the reservation
        return vehicle.UserId == currentUserId;
    }

    private static VehicleDto MapToDto(Vehicle vehicle, string? ownerFullName = null)
    {
        return new VehicleDto
        {
            Id = vehicle.Id,
            LicensePlate = vehicle.LicensePlate,
            VehicleType = vehicle.VehicleType,
            Make = vehicle.Make,
            Model = vehicle.Model,
            Color = vehicle.Color,
            UserId = vehicle.UserId,
            OwnerFullName = ownerFullName ?? vehicle.User?.FullName,
            CreatedAt = vehicle.CreatedAt,
            UpdatedAt = vehicle.UpdatedAt
        };
    }
}
