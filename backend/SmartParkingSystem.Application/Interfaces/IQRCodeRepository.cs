using SmartParkingSystem.Domain.Entities;

namespace SmartParkingSystem.Application.Interfaces;

public interface IQRCodeRepository
{
    Task<QRCode?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<QRCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<QRCode?> GetByReservationIdAsync(Guid reservationId, CancellationToken cancellationToken = default);
    Task AddAsync(QRCode qrCode, CancellationToken cancellationToken = default);
    Task UpdateAsync(QRCode qrCode, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
