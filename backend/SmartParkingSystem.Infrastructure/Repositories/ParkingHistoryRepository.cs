using Microsoft.EntityFrameworkCore;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Infrastructure.Data;

namespace SmartParkingSystem.Infrastructure.Repositories;

public class ParkingHistoryRepository : IParkingHistoryRepository
{
    private readonly ApplicationDbContext _context;

    public ParkingHistoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ParkingHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ParkingHistories
            .Include(h => h.Reservation)
                .ThenInclude(r => r.User)
            .Include(h => h.Vehicle)
            .Include(h => h.ParkingSlot)
                .ThenInclude(s => s.ParkingArea)
            .Include(h => h.AttendantCheckIn)
            .Include(h => h.AttendantCheckOut)
            .FirstOrDefaultAsync(h => h.Id == id && !h.IsDeleted, cancellationToken);
    }

    public async Task<ParkingHistory?> GetActiveByReservationIdAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        return await _context.ParkingHistories
            .Include(h => h.Reservation)
                .ThenInclude(r => r.User)
            .Include(h => h.Vehicle)
            .Include(h => h.ParkingSlot)
                .ThenInclude(s => s.ParkingArea)
            .Include(h => h.AttendantCheckIn)
            .Include(h => h.AttendantCheckOut)
            .FirstOrDefaultAsync(h => h.ReservationId == reservationId && h.CheckOutTime == null && !h.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<ParkingHistory>> GetByReservationIdAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        return await _context.ParkingHistories
            .Include(h => h.Reservation)
            .Include(h => h.Vehicle)
            .Include(h => h.ParkingSlot)
            .Include(h => h.AttendantCheckIn)
            .Include(h => h.AttendantCheckOut)
            .Where(h => h.ReservationId == reservationId && !h.IsDeleted)
            .OrderByDescending(h => h.CheckInTime)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ParkingHistory history, CancellationToken cancellationToken = default)
    {
        await _context.ParkingHistories.AddAsync(history, cancellationToken);
    }

    public Task UpdateAsync(ParkingHistory history, CancellationToken cancellationToken = default)
    {
        _context.ParkingHistories.Update(history);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
