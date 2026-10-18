using Microsoft.EntityFrameworkCore;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;
using SmartParkingSystem.Infrastructure.Data;

namespace SmartParkingSystem.Infrastructure.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly ApplicationDbContext _context;

    public VehicleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Vehicles
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<Vehicle>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Vehicles
            .Include(v => v.User)
            .Where(v => v.UserId == userId && !v.IsDeleted)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByPlateAsync(string licensePlate, Guid? excludeVehicleId = null, CancellationToken cancellationToken = default)
    {
        var normalizedPlate = SmartParkingSystem.Application.Common.Validation.VehicleValidators.NormalizePlate(licensePlate);

        var query = _context.Vehicles
            .Where(v => !v.IsDeleted && v.LicensePlate.ToUpper() == normalizedPlate);

        if (excludeVehicleId.HasValue)
        {
            query = query.Where(v => v.Id != excludeVehicleId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> HasActiveReservationsAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        return await _context.Reservations
            .AnyAsync(r => r.VehicleId == vehicleId
                           && !r.IsDeleted
                           && (r.Status == ReservationStatus.Pending
                               || r.Status == ReservationStatus.Confirmed
                               || r.Status == ReservationStatus.Active),
                      cancellationToken);
    }

    public async Task<Vehicle> AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        await _context.Vehicles.AddAsync(vehicle, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return vehicle;
    }

    public async Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        vehicle.UpdatedAt = DateTime.UtcNow;
        _context.Vehicles.Update(vehicle);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        vehicle.IsDeleted = true;
        vehicle.UpdatedAt = DateTime.UtcNow;
        _context.Vehicles.Update(vehicle);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
