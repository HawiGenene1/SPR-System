using Microsoft.EntityFrameworkCore;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;
using SmartParkingSystem.Infrastructure.Data;

namespace SmartParkingSystem.Infrastructure.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly ApplicationDbContext _context;

    public ReservationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Reservations
            .Include(r => r.User)
            .Include(r => r.ParkingSlot)
                .ThenInclude(s => s.ParkingArea)
            .Include(r => r.Vehicle)
            .Include(r => r.QRCode)
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
    }

    public async Task<Reservation?> GetByReservationNumberAsync(string reservationNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Reservations
            .Include(r => r.User)
            .Include(r => r.ParkingSlot)
                .ThenInclude(s => s.ParkingArea)
            .Include(r => r.Vehicle)
            .Include(r => r.QRCode)
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(r => r.ReservationNumber == reservationNumber && !r.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<Reservation>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Reservations
            .Include(r => r.User)
            .Include(r => r.ParkingSlot)
                .ThenInclude(s => s.ParkingArea)
            .Include(r => r.Vehicle)
            .Include(r => r.QRCode)
            .Include(r => r.Payment)
            .Where(r => r.UserId == userId && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reservation>> GetAllAsync(
        Guid? parkingAreaId = null,
        ReservationStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Reservations
            .Include(r => r.User)
            .Include(r => r.ParkingSlot)
                .ThenInclude(s => s.ParkingArea)
            .Include(r => r.Vehicle)
            .Include(r => r.QRCode)
            .Include(r => r.Payment)
            .Where(r => !r.IsDeleted);

        if (parkingAreaId.HasValue && parkingAreaId.Value != Guid.Empty)
        {
            query = query.Where(r => r.ParkingSlot.ParkingAreaId == parkingAreaId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.StartTime >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => r.EndTime <= toDate.Value);
        }

        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<bool> HasOverlappingReservationAsync(
        Guid slotId,
        DateTime startTime,
        DateTime endTime,
        Guid? excludeReservationId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Reservations
            .Where(r => r.ParkingSlotId == slotId
                        && !r.IsDeleted
                        && (r.Status == ReservationStatus.Pending ||
                            r.Status == ReservationStatus.Confirmed ||
                            r.Status == ReservationStatus.Active)
                        && r.StartTime < endTime
                        && r.EndTime > startTime);

        if (excludeReservationId.HasValue && excludeReservationId.Value != Guid.Empty)
        {
            query = query.Where(r => r.Id != excludeReservationId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        await _context.Reservations.AddAsync(reservation, cancellationToken);
    }

    public Task UpdateAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        _context.Reservations.Update(reservation);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
