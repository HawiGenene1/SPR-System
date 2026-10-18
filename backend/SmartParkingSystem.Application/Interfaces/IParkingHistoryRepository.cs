using SmartParkingSystem.Domain.Entities;

namespace SmartParkingSystem.Application.Interfaces;

public interface IParkingHistoryRepository
{
    Task<ParkingHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ParkingHistory?> GetActiveByReservationIdAsync(Guid reservationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ParkingHistory>> GetByReservationIdAsync(Guid reservationId, CancellationToken cancellationToken = default);
    Task AddAsync(ParkingHistory history, CancellationToken cancellationToken = default);
    Task UpdateAsync(ParkingHistory history, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
