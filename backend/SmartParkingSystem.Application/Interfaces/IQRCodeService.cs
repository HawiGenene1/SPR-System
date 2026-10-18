using SmartParkingSystem.Application.DTOs.QRCodes;

namespace SmartParkingSystem.Application.Interfaces;

public interface IQRCodeService
{
    /// <summary>
    /// Generates a unique, secure QR code (with Base64 PNG image via QRCoder) for a confirmed reservation.
    /// Does not expose any sensitive credentials.
    /// </summary>
    Task<QRCodeDto> GenerateQRCodeAsync(Guid reservationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the QR code for a reservation. Only the reservation owner or an Admin can access it.
    /// </summary>
    Task<QRCodeDto> GetQRCodeByReservationIdAsync(
        Guid reservationId,
        Guid currentUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a QR code and optionally processes check-in or check-out.
    /// Strictly restricted to ParkingAttendants and Admins.
    /// </summary>
    Task<QRScanResultDto> ScanQRCodeAsync(
        QRScanRequest request,
        Guid attendantId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default);
}
