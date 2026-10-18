using Microsoft.EntityFrameworkCore;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;
using SmartParkingSystem.Infrastructure.Data;

namespace SmartParkingSystem.Infrastructure.Repositories;

public class ParkingSlotRepository : IParkingSlotRepository
{
    private readonly ApplicationDbContext _context;

    public ParkingSlotRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ParkingSlot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ParkingSlots
            .Include(s => s.ParkingArea)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);
    }

    public async Task<ParkingSlot?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ParkingSlots
            .Include(s => s.ParkingArea)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<ParkingSlot>> GetSlotsAsync(
        Guid? parkingAreaId = null,
        int? floorNumber = null,
        SlotStatus? status = null,
        VehicleType? vehicleType = null,
        bool availableOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ParkingSlots
            .Include(s => s.ParkingArea)
            .Where(s => !s.IsDeleted);

        if (parkingAreaId.HasValue)
        {
            query = query.Where(s => s.ParkingAreaId == parkingAreaId.Value);
        }

        if (floorNumber.HasValue)
        {
            query = query.Where(s => s.FloorNumber == floorNumber.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        if (vehicleType.HasValue)
        {
            query = query.Where(s => s.VehicleType == vehicleType.Value);
        }

        if (availableOnly)
        {
            query = query.Where(s => s.IsActive && s.Status == SlotStatus.Available);
        }

        return await query
            .OrderBy(s => s.ParkingArea.Name)
            .ThenBy(s => s.FloorNumber)
            .ThenBy(s => s.SlotNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsSlotNumberAsync(Guid parkingAreaId, string slotNumber, Guid? excludeSlotId = null, CancellationToken cancellationToken = default)
    {
        var normalizedSlot = slotNumber.Trim().ToUpperInvariant();

        var query = _context.ParkingSlots
            .Where(s => !s.IsDeleted && s.ParkingAreaId == parkingAreaId && s.SlotNumber.ToUpper() == normalizedSlot);

        if (excludeSlotId.HasValue)
        {
            query = query.Where(s => s.Id != excludeSlotId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> HasActiveReservationsAsync(Guid slotId, CancellationToken cancellationToken = default)
    {
        return await _context.Reservations
            .AnyAsync(r => r.ParkingSlotId == slotId
                           && !r.IsDeleted
                           && (r.Status == ReservationStatus.Pending
                               || r.Status == ReservationStatus.Confirmed
                               || r.Status == ReservationStatus.Active),
                      cancellationToken);
    }

    public async Task<ParkingSlot> AddAsync(ParkingSlot slot, CancellationToken cancellationToken = default)
    {
        await _context.ParkingSlots.AddAsync(slot, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return slot;
    }

    public async Task UpdateAsync(ParkingSlot slot, CancellationToken cancellationToken = default)
    {
        slot.UpdatedAt = DateTime.UtcNow;
        _context.ParkingSlots.Update(slot);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ParkingSlot slot, CancellationToken cancellationToken = default)
    {
        slot.IsDeleted = true;
        slot.UpdatedAt = DateTime.UtcNow;
        _context.ParkingSlots.Update(slot);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
