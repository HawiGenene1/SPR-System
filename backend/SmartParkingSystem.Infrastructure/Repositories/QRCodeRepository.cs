using Microsoft.EntityFrameworkCore;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Infrastructure.Data;

namespace SmartParkingSystem.Infrastructure.Repositories;

public class QRCodeRepository : IQRCodeRepository
{
    private readonly ApplicationDbContext _context;

    public QRCodeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<QRCode?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.QRCodes
            .Include(q => q.Reservation)
                .ThenInclude(r => r.User)
            .Include(q => q.Reservation)
                .ThenInclude(r => r.Vehicle)
            .Include(q => q.Reservation)
                .ThenInclude(r => r.ParkingSlot)
                    .ThenInclude(s => s.ParkingArea)
            .FirstOrDefaultAsync(q => q.Id == id && !q.IsDeleted, cancellationToken);
    }

    public async Task<QRCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim();
        return await _context.QRCodes
            .IgnoreQueryFilters()
            .Include(q => q.Reservation)
                .ThenInclude(r => r.User)
            .Include(q => q.Reservation)
                .ThenInclude(r => r.Vehicle)
            .Include(q => q.Reservation)
                .ThenInclude(r => r.ParkingSlot)
                    .ThenInclude(s => s.ParkingArea)
            .FirstOrDefaultAsync(q => q.Code == normalized, cancellationToken);
    }

    public async Task<QRCode?> GetByReservationIdAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        return await _context.QRCodes
            .Include(q => q.Reservation)
                .ThenInclude(r => r.User)
            .Include(q => q.Reservation)
                .ThenInclude(r => r.Vehicle)
            .Include(q => q.Reservation)
                .ThenInclude(r => r.ParkingSlot)
                    .ThenInclude(s => s.ParkingArea)
            .FirstOrDefaultAsync(q => q.ReservationId == reservationId && !q.IsDeleted, cancellationToken);
    }

    public async Task AddAsync(QRCode qrCode, CancellationToken cancellationToken = default)
    {
        await _context.QRCodes.AddAsync(qrCode, cancellationToken);
    }

    public Task UpdateAsync(QRCode qrCode, CancellationToken cancellationToken = default)
    {
        _context.QRCodes.Update(qrCode);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
