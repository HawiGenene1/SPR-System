using Microsoft.EntityFrameworkCore;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;
using SmartParkingSystem.Infrastructure.Data;

namespace SmartParkingSystem.Infrastructure.Repositories;

public class ParkingAreaRepository : IParkingAreaRepository
{
    private readonly ApplicationDbContext _context;

    public ParkingAreaRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ParkingArea?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ParkingAreas
            .Include(p => p.ParkingSlots)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
    }

    public async Task<ParkingArea?> GetByIdWithSlotsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ParkingAreas
            .Include(p => p.ParkingSlots)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<ParkingArea>> GetAllAsync(bool includeInactive = false, string? city = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ParkingAreas
            .Include(p => p.ParkingSlots)
            .Where(p => !p.IsDeleted);

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var normalizedCity = city.Trim().ToLower();
            query = query.Where(p => p.City.ToLower() == normalizedCity);
        }

        return await query
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLower();

        var query = _context.ParkingAreas
            .Where(p => !p.IsDeleted && p.Name.ToLower() == normalizedName);

        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> HasActiveReservationsAsync(Guid parkingAreaId, CancellationToken cancellationToken = default)
    {
        var hasReservations = await _context.Reservations
            .AnyAsync(r => r.ParkingSlot.ParkingAreaId == parkingAreaId
                           && !r.IsDeleted
                           && (r.Status == ReservationStatus.Pending
                               || r.Status == ReservationStatus.Confirmed
                               || r.Status == ReservationStatus.Active),
                      cancellationToken);

        if (hasReservations)
        {
            return true;
        }

        var hasActiveSessions = await _context.ParkingHistories
            .AnyAsync(h => h.ParkingSlot.ParkingAreaId == parkingAreaId
                           && !h.IsDeleted
                           && h.CheckOutTime == null,
                      cancellationToken);

        return hasActiveSessions;
    }

    public async Task<ParkingArea> AddAsync(ParkingArea parkingArea, CancellationToken cancellationToken = default)
    {
        await _context.ParkingAreas.AddAsync(parkingArea, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return parkingArea;
    }

    public async Task UpdateAsync(ParkingArea parkingArea, CancellationToken cancellationToken = default)
    {
        parkingArea.UpdatedAt = DateTime.UtcNow;
        _context.ParkingAreas.Update(parkingArea);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ParkingArea parkingArea, CancellationToken cancellationToken = default)
    {
        parkingArea.IsDeleted = true;
        parkingArea.UpdatedAt = DateTime.UtcNow;

        // Cascade soft delete to associated slots
        if (parkingArea.ParkingSlots != null)
        {
            foreach (var slot in parkingArea.ParkingSlots)
            {
                slot.IsDeleted = true;
                slot.UpdatedAt = DateTime.UtcNow;
            }
        }

        _context.ParkingAreas.Update(parkingArea);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
