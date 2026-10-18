using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Payment?> GetByTransactionReferenceAsync(string reference, CancellationToken cancellationToken = default);
    Task<Payment?> GetByReservationIdAsync(Guid reservationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetAllAsync(
        PaymentStatus? status = null,
        PaymentMethod? method = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
