using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.Interfaces;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Reservation?> GetByReservationNumberAsync(string reservationNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reservation>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reservation>> GetAllAsync(
        Guid? parkingAreaId = null,
        ReservationStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingReservationAsync(
        Guid slotId,
        DateTime startTime,
        DateTime endTime,
        Guid? excludeReservationId = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default);
    Task UpdateAsync(Reservation reservation, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
