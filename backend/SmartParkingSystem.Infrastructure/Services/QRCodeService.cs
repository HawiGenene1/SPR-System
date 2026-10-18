using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QRCoder;
using SmartParkingSystem.Application.DTOs.QRCodes;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;
using SmartParkingSystem.Infrastructure.Data;

namespace SmartParkingSystem.Infrastructure.Services;

public class QRCodeService : IQRCodeService
{
    private readonly ApplicationDbContext _context;
    private readonly IQRCodeRepository _qrCodeRepository;
    private readonly ILogger<QRCodeService> _logger;

    public QRCodeService(
        ApplicationDbContext context,
        IQRCodeRepository qrCodeRepository,
        ILogger<QRCodeService> logger)
    {
        _context = context;
        _qrCodeRepository = qrCodeRepository;
        _logger = logger;
    }

    public async Task<QRCodeDto> GenerateQRCodeAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        var reservation = await _context.Reservations
            .Include(r => r.QRCode)
            .FirstOrDefaultAsync(r => r.Id == reservationId && !r.IsDeleted, cancellationToken);

        if (reservation == null)
        {
            throw new KeyNotFoundException($"Reservation with ID '{reservationId}' was not found.");
        }

        // Generate a secure, unique, non-sensitive reservation token
        var secureToken = $"QR-{reservation.ReservationNumber}-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}";
        var base64Image = RenderQrCodeBase64(secureToken);

        var existingQr = reservation.QRCode ?? await _qrCodeRepository.GetByReservationIdAsync(reservationId, cancellationToken);

        if (existingQr != null)
        {
            existingQr.Code = secureToken;
            existingQr.ImageData = base64Image;
            existingQr.ExpiresAt = reservation.EndTime.AddHours(2);
            await _qrCodeRepository.UpdateAsync(existingQr, cancellationToken);
            await _qrCodeRepository.SaveChangesAsync(cancellationToken);
            return MapToDto(existingQr, reservation.ReservationNumber);
        }

        var newQr = new QRCode
        {
            Id = Guid.NewGuid(),
            ReservationId = reservation.Id,
            Code = secureToken,
            ImageData = base64Image,
            IsUsedForEntry = false,
            IsUsedForExit = false,
            ExpiresAt = reservation.EndTime.AddHours(2),
            CreatedAt = DateTime.UtcNow
        };

        await _qrCodeRepository.AddAsync(newQr, cancellationToken);
        await _qrCodeRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Generated new QR code {Code} for reservation {ReservationNumber}", newQr.Code, reservation.ReservationNumber);

        return MapToDto(newQr, reservation.ReservationNumber);
    }

    public async Task<QRCodeDto> GetQRCodeByReservationIdAsync(
        Guid reservationId,
        Guid currentUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        var reservation = await _context.Reservations
            .Include(r => r.QRCode)
            .FirstOrDefaultAsync(r => r.Id == reservationId && !r.IsDeleted, cancellationToken);

        if (reservation == null)
        {
            throw new KeyNotFoundException($"Reservation with ID '{reservationId}' was not found.");
        }

        // Ownership enforcement: Only reservation owner or an Admin can access the QR code
        if (!isAdmin && reservation.UserId != currentUserId)
        {
            _logger.LogWarning("User {UserId} attempted unauthorized access to QR code for reservation {ReservationId}", currentUserId, reservationId);
            throw new UnauthorizedAccessException("You are not authorized to view the QR code for another user's reservation.");
        }

        var qr = reservation.QRCode ?? await _qrCodeRepository.GetByReservationIdAsync(reservationId, cancellationToken);

        if (qr == null)
        {
            // Auto-generate if reservation is confirmed or active
            if (reservation.Status is ReservationStatus.Confirmed or ReservationStatus.Active)
            {
                return await GenerateQRCodeAsync(reservationId, cancellationToken);
            }

            throw new InvalidOperationException("QR code has not been generated for this reservation.");
        }

        // Ensure image data exists (generate if empty)
        if (string.IsNullOrWhiteSpace(qr.ImageData))
        {
            qr.ImageData = RenderQrCodeBase64(qr.Code);
            await _qrCodeRepository.UpdateAsync(qr, cancellationToken);
            await _qrCodeRepository.SaveChangesAsync(cancellationToken);
        }

        return MapToDto(qr, reservation.ReservationNumber);
    }

    public async Task<QRScanResultDto> ScanQRCodeAsync(
        QRScanRequest request,
        Guid attendantId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var code = request.QRCode?.Trim() ?? string.Empty;
        var action = string.IsNullOrWhiteSpace(request.Action) ? "CheckIn" : request.Action.Trim();

        // 1. Validate QR Code & Reservation existence
        var qr = await _qrCodeRepository.GetByCodeAsync(code, cancellationToken);
        if (qr == null || qr.Reservation == null)
        {
            return new QRScanResultDto
            {
                IsValid = false,
                Message = "QR code not recognized or invalid.",
                Action = action,
                QRCode = code,
                ScannedAt = now
            };
        }

        var res = qr.Reservation;
        var baseResult = new QRScanResultDto
        {
            QRCode = qr.Code,
            Action = action,
            ReservationId = res.Id,
            ReservationNumber = res.ReservationNumber,
            CustomerName = res.User?.FullName ?? "Unknown",
            CustomerPhoneNumber = res.User?.PhoneNumber,
            LicensePlate = res.Vehicle?.LicensePlate ?? "Unknown",
            VehicleModel = res.Vehicle != null ? $"{res.Vehicle.Make} {res.Vehicle.Model}" : "Unknown",
            ParkingAreaName = res.ParkingSlot?.ParkingArea?.Name ?? "Unknown",
            SlotNumber = res.ParkingSlot?.SlotNumber ?? "Unknown",
            FloorNumber = res.ParkingSlot?.FloorNumber ?? 1,
            StartTime = res.StartTime,
            EndTime = res.EndTime,
            ReservationStatus = res.Status,
            IsUsedForEntry = qr.IsUsedForEntry,
            EntryScannedAt = qr.EntryScannedAt,
            IsUsedForExit = qr.IsUsedForExit,
            ExitScannedAt = qr.ExitScannedAt,
            ExpiresAt = qr.ExpiresAt,
            ScannedAt = now
        };

        // 2. Validate QR code has not been deleted or invalidated
        if (qr.IsDeleted || res.IsDeleted)
        {
            baseResult.IsValid = false;
            baseResult.Message = "QR code has been invalidated.";
            return baseResult;
        }

        // 3. Validate Reservation Status (must be Confirmed or Active)
        if (res.Status != ReservationStatus.Confirmed && res.Status != ReservationStatus.Active)
        {
            baseResult.IsValid = false;
            baseResult.Message = res.Status switch
            {
                ReservationStatus.Cancelled => "Reservation has been cancelled.",
                ReservationStatus.Completed => qr.IsUsedForExit
                    ? $"QR code has already been checked out at {qr.ExitScannedAt:yyyy-MM-dd HH:mm:ss} UTC."
                    : "Reservation has already been completed.",
                ReservationStatus.Pending => "Reservation is pending payment or confirmation.",
                _ => $"Reservation is not in an active or confirmed state (current status: {res.Status})."
            };
            return baseResult;
        }

        // 4. Validate Time Window
        if (now > qr.ExpiresAt || now > res.EndTime.AddHours(2))
        {
            baseResult.IsValid = false;
            baseResult.Message = "Reservation QR code has expired.";
            return baseResult;
        }

        // Prevent check-in if too early (allowed up to 30 mins before start)
        if (action.Equals("CheckIn", StringComparison.OrdinalIgnoreCase) && now < res.StartTime.AddMinutes(-30))
        {
            baseResult.IsValid = false;
            baseResult.Message = $"Too early for entry check-in. Entry permitted starting 30 minutes before reservation ({res.StartTime:yyyy-MM-dd HH:mm} UTC).";
            return baseResult;
        }

        // 5. Validate and Process Sequential Action (CheckIn, CheckOut, or ValidateOnly)
        if (action.Equals("CheckIn", StringComparison.OrdinalIgnoreCase))
        {
            if (qr.IsUsedForEntry)
            {
                baseResult.IsValid = false;
                baseResult.Message = $"QR code has already been scanned for entry at {qr.EntryScannedAt:yyyy-MM-dd HH:mm:ss} UTC.";
                return baseResult;
            }

            // Process Check-In
            qr.IsUsedForEntry = true;
            qr.EntryScannedAt = now;
            res.Status = ReservationStatus.Active;

            if (res.ParkingSlot != null)
            {
                res.ParkingSlot.Status = SlotStatus.Occupied;
            }

            var history = new ParkingHistory
            {
                Id = Guid.NewGuid(),
                ReservationId = res.Id,
                VehicleId = res.VehicleId,
                ParkingSlotId = res.ParkingSlotId,
                CheckInTime = now,
                AttendantCheckInId = attendantId,
                Notes = request.Notes,
                CreatedAt = now
            };

            await _context.ParkingHistories.AddAsync(history, cancellationToken);
            await _qrCodeRepository.UpdateAsync(qr, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            baseResult.IsValid = true;
            baseResult.Message = "Vehicle entry check-in validated and processed successfully.";
            baseResult.IsUsedForEntry = true;
            baseResult.EntryScannedAt = now;
            baseResult.ReservationStatus = ReservationStatus.Active;

            _logger.LogInformation("Attendant {AttendantId} checked in Reservation {ReservationNumber}", attendantId, res.ReservationNumber);
            return baseResult;
        }

        if (action.Equals("CheckOut", StringComparison.OrdinalIgnoreCase))
        {
            if (!qr.IsUsedForEntry)
            {
                baseResult.IsValid = false;
                baseResult.Message = "Cannot check out: vehicle has not been scanned for entry yet.";
                return baseResult;
            }

            if (qr.IsUsedForExit)
            {
                baseResult.IsValid = false;
                baseResult.Message = $"QR code has already been checked out at {qr.ExitScannedAt:yyyy-MM-dd HH:mm:ss} UTC.";
                return baseResult;
            }

            // Process Check-Out
            qr.IsUsedForExit = true;
            qr.ExitScannedAt = now;
            res.Status = ReservationStatus.Completed;

            if (res.ParkingSlot != null)
            {
                res.ParkingSlot.Status = SlotStatus.Available;
            }

            var history = await _context.ParkingHistories
                .FirstOrDefaultAsync(h => h.ReservationId == res.Id && h.CheckOutTime == null, cancellationToken);

            int? overstayMinutes = null;
            decimal? overstayFee = null;

            if (now > res.EndTime.AddMinutes(15)) // 15-minute grace period
            {
                overstayMinutes = (int)(now - res.EndTime).TotalMinutes;
                var extraHours = Math.Ceiling(overstayMinutes.Value / 60.0);
                var rate = res.ParkingSlot?.HourlyRate ?? res.ParkingSlot?.ParkingArea?.HourlyRate ?? 5.0m;
                overstayFee = (decimal)extraHours * rate;
            }

            if (history != null)
            {
                history.CheckOutTime = now;
                history.AttendantCheckOutId = attendantId;
                history.OverstayDurationMinutes = overstayMinutes;
                history.OverstayFee = overstayFee;
                if (!string.IsNullOrWhiteSpace(request.Notes))
                {
                    history.Notes = string.IsNullOrWhiteSpace(history.Notes)
                        ? request.Notes
                        : $"{history.Notes}; {request.Notes}";
                }
            }

            await _qrCodeRepository.UpdateAsync(qr, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            baseResult.IsValid = true;
            baseResult.Message = overstayFee.HasValue && overstayFee.Value > 0
                ? $"Check-out processed. Overstay fee of {overstayFee.Value:F2} USD incurred ({overstayMinutes} minutes)."
                : "Vehicle exit check-out processed successfully.";
            baseResult.IsUsedForExit = true;
            baseResult.ExitScannedAt = now;
            baseResult.ReservationStatus = ReservationStatus.Completed;
            baseResult.OverstayFee = overstayFee;
            baseResult.OverstayMinutes = overstayMinutes;

            _logger.LogInformation("Attendant {AttendantId} checked out Reservation {ReservationNumber}", attendantId, res.ReservationNumber);
            return baseResult;
        }

        // Default: ValidateOnly
        baseResult.IsValid = true;
        baseResult.Message = qr.IsUsedForEntry
            ? "QR code verified: vehicle currently parked (awaiting exit)."
            : "QR code verified: ready for entry check-in.";
        return baseResult;
    }

    /// <summary>
    /// Uses QRCoder to render pure PNG byte graphics and format as a Base64 data URL.
    /// Pure C# implementation (PngByteQRCode) requiring no GDI+ or libgdiplus native libraries.
    /// </summary>
    public static string RenderQrCodeBase64(string payload)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        using var pngQrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = pngQrCode.GetGraphic(20);
        return $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
    }

    private static QRCodeDto MapToDto(QRCode qr, string? reservationNumber = null)
    {
        return new QRCodeDto
        {
            Id = qr.Id,
            Code = qr.Code,
            ImageData = qr.ImageData,
            IsUsedForEntry = qr.IsUsedForEntry,
            EntryScannedAt = qr.EntryScannedAt,
            IsUsedForExit = qr.IsUsedForExit,
            ExitScannedAt = qr.ExitScannedAt,
            ExpiresAt = qr.ExpiresAt,
            ReservationId = qr.ReservationId,
            ReservationNumber = reservationNumber ?? qr.Reservation?.ReservationNumber,
            CreatedAt = qr.CreatedAt
        };
    }
}
