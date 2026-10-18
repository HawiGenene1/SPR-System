using Microsoft.EntityFrameworkCore;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;
using SmartParkingSystem.Infrastructure.Data;

namespace SmartParkingSystem.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly ApplicationDbContext _context;

    public PaymentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Include(p => p.User)
            .Include(p => p.Reservation)
                .ThenInclude(r => r.ParkingSlot)
                    .ThenInclude(s => s.ParkingArea)
            .Include(p => p.Reservation)
                .ThenInclude(r => r.Vehicle)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
    }

    public async Task<Payment?> GetByTransactionReferenceAsync(string reference, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Include(p => p.User)
            .Include(p => p.Reservation)
                .ThenInclude(r => r.ParkingSlot)
                    .ThenInclude(s => s.ParkingArea)
            .Include(p => p.Reservation)
                .ThenInclude(r => r.Vehicle)
            .FirstOrDefaultAsync(p => p.TransactionId == reference && !p.IsDeleted, cancellationToken);
    }

    public async Task<Payment?> GetByReservationIdAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Include(p => p.User)
            .Include(p => p.Reservation)
                .ThenInclude(r => r.ParkingSlot)
                    .ThenInclude(s => s.ParkingArea)
            .Include(p => p.Reservation)
                .ThenInclude(r => r.Vehicle)
            .FirstOrDefaultAsync(p => p.ReservationId == reservationId && !p.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Include(p => p.User)
            .Include(p => p.Reservation)
                .ThenInclude(r => r.ParkingSlot)
                    .ThenInclude(s => s.ParkingArea)
            .Include(p => p.Reservation)
                .ThenInclude(r => r.Vehicle)
            .Where(p => p.UserId == userId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetAllAsync(
        PaymentStatus? status = null,
        PaymentMethod? method = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Payments
            .Include(p => p.User)
            .Include(p => p.Reservation)
                .ThenInclude(r => r.ParkingSlot)
                    .ThenInclude(s => s.ParkingArea)
            .Include(p => p.Reservation)
                .ThenInclude(r => r.Vehicle)
            .Where(p => !p.IsDeleted)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        if (method.HasValue)
        {
            query = query.Where(p => p.PaymentMethod == method.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(p => p.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(p => p.CreatedAt <= toDate.Value);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await _context.Payments.AddAsync(payment, cancellationToken);
    }

    public Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _context.Payments.Update(payment);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
