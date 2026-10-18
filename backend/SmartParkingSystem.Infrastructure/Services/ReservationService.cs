using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartParkingSystem.Application.Common.BusinessLogic;
using SmartParkingSystem.Application.DTOs.Payments;
using SmartParkingSystem.Application.DTOs.QRCodes;
using SmartParkingSystem.Application.DTOs.Reservations;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;
using SmartParkingSystem.Infrastructure.Data;

namespace SmartParkingSystem.Infrastructure.Services;

public class ReservationService : IReservationService
{
    private readonly ApplicationDbContext _context;
    private readonly IReservationRepository _reservationRepository;
    private readonly IVehicleService _vehicleService;
    private readonly ILogger<ReservationService> _logger;

    public ReservationService(
        ApplicationDbContext context,
        IReservationRepository reservationRepository,
        IVehicleService vehicleService,
        ILogger<ReservationService> logger)
    {
        _context = context;
        _reservationRepository = reservationRepository;
        _vehicleService = vehicleService;
        _logger = logger;
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, SemaphoreSlim> _slotLocks = new();

    public async Task<ReservationDto> CreateReservationAsync(
        CreateReservationRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        // 1. Time validation (Entry time < Exit time, not in past, minimum 15 minutes)
        var (isValidTime, timeErrorMessage) = ReservationPricingCalculator.ValidateReservationTimes(
            request.StartTime, request.EndTime, DateTime.UtcNow);

        if (!isValidTime)
        {
            throw new ArgumentException(timeErrorMessage);
        }

        // 2. Slot must exist and be active
        var slot = await _context.ParkingSlots
            .Include(s => s.ParkingArea)
            .FirstOrDefaultAsync(s => s.Id == request.ParkingSlotId && !s.IsDeleted, cancellationToken);

        if (slot == null || !slot.IsActive)
        {
            throw new KeyNotFoundException($"Parking slot with ID '{request.ParkingSlotId}' was not found or is inactive.");
        }

        if (slot.Status == SlotStatus.Maintenance)
        {
            throw new InvalidOperationException($"Parking slot '{slot.SlotNumber}' is currently under maintenance and cannot be reserved.");
        }

        // 3. Slot must belong to selected parking area (if specified)
        if (request.ParkingAreaId.HasValue && request.ParkingAreaId.Value != Guid.Empty)
        {
            if (slot.ParkingAreaId != request.ParkingAreaId.Value)
            {
                throw new ArgumentException($"Parking slot '{slot.SlotNumber}' does not belong to the selected parking area.");
            }
        }

        // 4. Vehicle must belong to current customer and not be deleted
        var targetUserId = request.UserId.HasValue && request.UserId.Value != Guid.Empty
            ? request.UserId.Value
            : currentUserId;

        var isVehicleValid = await _vehicleService.ValidateVehicleOwnershipForReservationAsync(request.VehicleId, targetUserId, cancellationToken);
        if (!isVehicleValid)
        {
            throw new ArgumentException("Vehicle does not belong to the current customer or does not exist.");
        }

        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId && !v.IsDeleted, cancellationToken);

        if (vehicle == null)
        {
            throw new KeyNotFoundException($"Vehicle with ID '{request.VehicleId}' was not found.");
        }

        // Vehicle-type compatibility check if slot is specialized
        if (slot.VehicleType == VehicleType.ElectricVehicle && vehicle.VehicleType != VehicleType.ElectricVehicle)
        {
            throw new InvalidOperationException("This parking slot is designated specifically for Electric Vehicles.");
        }
        if (slot.VehicleType == VehicleType.Motorcycle && vehicle.VehicleType != VehicleType.Motorcycle)
        {
            throw new InvalidOperationException("This parking slot is designated specifically for Motorcycles.");
        }

        // 5. Server-side duration and price calculation (Never trust amount or duration sent by client)
        var durationHours = ReservationPricingCalculator.CalculateDurationHours(request.StartTime, request.EndTime);
        var hourlyRate = slot.HourlyRate ?? slot.ParkingArea.HourlyRate;
        var totalPrice = ReservationPricingCalculator.CalculateTotalPrice(hourlyRate, request.StartTime, request.EndTime);

        if (request.TotalPrice.HasValue || request.Amount.HasValue)
        {
            _logger.LogInformation(
                "Client sent price ({ClientPrice:C}), but server calculated price as {ServerPrice:C} for {Hours}h at {Rate:C}/hr.",
                request.TotalPrice ?? request.Amount, totalPrice, durationHours, hourlyRate);
        }

        // 6. Concurrency prevention: Application-level lock per slot ID + database transaction with row-level lock
        var slotLock = _slotLocks.GetOrAdd(slot.Id, _ => new SemaphoreSlim(1, 1));
        await slotLock.WaitAsync(cancellationToken);
        try
        {
            if (_context.Database.IsRelational())
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
                    try
                    {
                        if (_context.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            await _context.Database.ExecuteSqlInterpolatedAsync(
                                $"SELECT \"Id\" FROM \"ParkingSlots\" WHERE \"Id\" = {slot.Id} FOR UPDATE",
                                cancellationToken);
                        }

                        var reservation = await ExecuteReservationCreationAsync(slot, vehicle, targetUserId, request.StartTime, request.EndTime, totalPrice, cancellationToken);
                        await _context.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);

                        var created = await _reservationRepository.GetByIdAsync(reservation.Id, cancellationToken);
                        return MapToDto(created ?? reservation);
                    }
                    catch
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        throw;
                    }
                });
            }
            else
            {
                // In-memory or non-relational test environments
                var reservation = await ExecuteReservationCreationAsync(slot, vehicle, targetUserId, request.StartTime, request.EndTime, totalPrice, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                var created = await _reservationRepository.GetByIdAsync(reservation.Id, cancellationToken);
                return MapToDto(created ?? reservation);
            }
        }
        finally
        {
            slotLock.Release();
        }
    }

    private async Task<Reservation> ExecuteReservationCreationAsync(
        ParkingSlot slot,
        Vehicle vehicle,
        Guid targetUserId,
        DateTime startTime,
        DateTime endTime,
        decimal totalPrice,
        CancellationToken cancellationToken)
    {
        // Check for overlapping reservations
        var hasConflict = await _reservationRepository.HasOverlappingReservationAsync(
            slot.Id, startTime, endTime, null, cancellationToken);

        if (hasConflict)
        {
            throw new InvalidOperationException("The selected parking slot is already reserved for the requested time frame.");
        }

        var reservationNumber = $"RES-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            ReservationNumber = reservationNumber,
            StartTime = startTime,
            EndTime = endTime,
            TotalPrice = totalPrice,
            Status = ReservationStatus.Confirmed,
            UserId = targetUserId,
            ParkingSlotId = slot.Id,
            VehicleId = vehicle.Id,
            CreatedAt = DateTime.UtcNow
        };

        await _reservationRepository.AddAsync(reservation, cancellationToken);

        // Generate entrance QR code
        var qrToken = $"QR-{reservationNumber}-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}";
        var qrCode = new QRCode
        {
            Id = Guid.NewGuid(),
            ReservationId = reservation.Id,
            Code = qrToken,
            ImageData = QRCodeService.RenderQrCodeBase64(qrToken),
            ExpiresAt = endTime.AddHours(2),
            CreatedAt = DateTime.UtcNow
        };

        await _context.QRCodes.AddAsync(qrCode, cancellationToken);

        // Update slot state if reservation starts within 15 minutes
        if (startTime <= DateTime.UtcNow.AddMinutes(15))
        {
            slot.Status = SlotStatus.Reserved;
            slot.UpdatedAt = DateTime.UtcNow;
        }

        _logger.LogInformation(
            "Created reservation {ReservationNumber} for user {UserId} on slot {SlotNumber} ({TotalPrice:C})",
            reservation.ReservationNumber, targetUserId, slot.SlotNumber, totalPrice);

        return reservation;
    }

    public async Task<IReadOnlyList<ReservationDto>> GetCustomerReservationsAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var reservations = await _reservationRepository.GetByUserIdAsync(currentUserId, cancellationToken);
        return reservations.Select(MapToDto).ToList();
    }

    public async Task<ReservationDto> GetReservationByIdAsync(
        Guid id,
        Guid? currentUserId = null,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        var reservation = await _reservationRepository.GetByIdAsync(id, cancellationToken);
        if (reservation == null)
        {
            throw new KeyNotFoundException($"Reservation with ID '{id}' was not found.");
        }

        if (!isAdmin && currentUserId.HasValue && reservation.UserId != currentUserId.Value)
        {
            throw new UnauthorizedAccessException("You are not authorized to view this reservation.");
        }

        return MapToDto(reservation);
    }

    public async Task<ReservationDto> CancelReservationAsync(
        Guid id,
        Guid currentUserId,
        string? reason = null,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        var reservation = await _reservationRepository.GetByIdAsync(id, cancellationToken);
        if (reservation == null)
        {
            throw new KeyNotFoundException($"Reservation with ID '{id}' was not found.");
        }

        if (!isAdmin && reservation.UserId != currentUserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to cancel this reservation.");
        }

        if (reservation.Status == ReservationStatus.Cancelled)
        {
            throw new InvalidOperationException("Reservation is already cancelled.");
        }

        if (reservation.Status == ReservationStatus.Completed)
        {
            throw new InvalidOperationException("Cannot cancel a completed reservation.");
        }

        if (reservation.Status == ReservationStatus.Active)
        {
            throw new InvalidOperationException("Cannot cancel an active parking session. Please check out instead.");
        }

        if (reservation.Status == ReservationStatus.Expired)
        {
            throw new InvalidOperationException("Cannot cancel an expired reservation.");
        }

        reservation.Status = ReservationStatus.Cancelled;
        reservation.CancelledAt = DateTime.UtcNow;
        reservation.CancellationReason = reason ?? (isAdmin ? "Cancelled by Administrator" : "Cancelled by customer");
        reservation.UpdatedAt = DateTime.UtcNow;

        // Release slot if it was in Reserved status
        if (reservation.ParkingSlot.Status == SlotStatus.Reserved)
        {
            var hasOtherActive = await _reservationRepository.HasOverlappingReservationAsync(
                reservation.ParkingSlotId,
                DateTime.UtcNow,
                DateTime.UtcNow.AddMinutes(15),
                reservation.Id,
                cancellationToken);

            if (!hasOtherActive)
            {
                reservation.ParkingSlot.Status = SlotStatus.Available;
                reservation.ParkingSlot.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _reservationRepository.UpdateAsync(reservation, cancellationToken);
        await _reservationRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Reservation {ReservationNumber} cancelled by user {UserId}",
            reservation.ReservationNumber, currentUserId);

        return MapToDto(reservation);
    }

    public async Task<IReadOnlyList<ReservationDto>> GetAllReservationsAsync(
        Guid? parkingAreaId = null,
        ReservationStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var reservations = await _reservationRepository.GetAllAsync(parkingAreaId, status, fromDate, toDate, cancellationToken);
        return reservations.Select(MapToDto).ToList();
    }

    public async Task<ReservationDto> UpdateReservationStatusAsync(
        Guid id,
        ReservationStatus newStatus,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var reservation = await _reservationRepository.GetByIdAsync(id, cancellationToken);
        if (reservation == null)
        {
            throw new KeyNotFoundException($"Reservation with ID '{id}' was not found.");
        }

        var oldStatus = reservation.Status;
        reservation.Status = newStatus;
        reservation.UpdatedAt = DateTime.UtcNow;

        if (newStatus == ReservationStatus.Cancelled)
        {
            reservation.CancelledAt = DateTime.UtcNow;
            reservation.CancellationReason = note ?? "Cancelled by administrator";
            if (reservation.ParkingSlot.Status == SlotStatus.Reserved)
            {
                reservation.ParkingSlot.Status = SlotStatus.Available;
                reservation.ParkingSlot.UpdatedAt = DateTime.UtcNow;
            }
        }
        else if (newStatus == ReservationStatus.Active)
        {
            reservation.ParkingSlot.Status = SlotStatus.Occupied;
            reservation.ParkingSlot.UpdatedAt = DateTime.UtcNow;
        }
        else if (newStatus == ReservationStatus.Completed)
        {
            reservation.ParkingSlot.Status = SlotStatus.Available;
            reservation.ParkingSlot.UpdatedAt = DateTime.UtcNow;
        }
        else if (newStatus == ReservationStatus.Confirmed)
        {
            if (reservation.StartTime <= DateTime.UtcNow.AddMinutes(15))
            {
                reservation.ParkingSlot.Status = SlotStatus.Reserved;
                reservation.ParkingSlot.UpdatedAt = DateTime.UtcNow;
            }

            var existingQr = await _context.QRCodes.FirstOrDefaultAsync(q => q.ReservationId == reservation.Id && !q.IsDeleted, cancellationToken);
            if (existingQr == null)
            {
                var qrToken = $"QR-{reservation.ReservationNumber}-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}";
                var qr = new QRCode
                {
                    Id = Guid.NewGuid(),
                    ReservationId = reservation.Id,
                    Code = qrToken,
                    ImageData = QRCodeService.RenderQrCodeBase64(qrToken),
                    ExpiresAt = reservation.EndTime.AddHours(2),
                    CreatedAt = DateTime.UtcNow
                };
                await _context.QRCodes.AddAsync(qr, cancellationToken);
            }
        }

        await _reservationRepository.UpdateAsync(reservation, cancellationToken);
        await _reservationRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Reservation {ReservationNumber} status changed from {OldStatus} to {NewStatus}",
            reservation.ReservationNumber, oldStatus, newStatus);

        return MapToDto(reservation);
    }

    private static ReservationDto MapToDto(Reservation r)
    {
        return new ReservationDto
        {
            Id = r.Id,
            ReservationNumber = r.ReservationNumber,
            StartTime = r.StartTime,
            EndTime = r.EndTime,
            TotalPrice = r.TotalPrice,
            Status = r.Status,
            CancelledAt = r.CancelledAt,
            CancellationReason = r.CancellationReason,
            UserId = r.UserId,
            CustomerName = r.User?.FullName,
            CustomerEmail = r.User?.Email,
            CustomerPhoneNumber = r.User?.PhoneNumber,
            ParkingSlotId = r.ParkingSlotId,
            SlotNumber = r.ParkingSlot?.SlotNumber,
            FloorNumber = r.ParkingSlot?.FloorNumber,
            SlotType = r.ParkingSlot?.SlotType,
            ParkingAreaId = r.ParkingSlot?.ParkingAreaId,
            ParkingAreaName = r.ParkingSlot?.ParkingArea?.Name,
            ParkingAreaAddress = r.ParkingSlot?.ParkingArea?.Address,
            VehicleId = r.VehicleId,
            LicensePlate = r.Vehicle?.LicensePlate,
            VehicleMake = r.Vehicle?.Make,
            VehicleModel = r.Vehicle?.Model,
            QRCode = r.QRCode != null ? new QRCodeDto
            {
                Id = r.QRCode.Id,
                Code = r.QRCode.Code,
                ImageData = r.QRCode.ImageData,
                IsUsedForEntry = r.QRCode.IsUsedForEntry,
                EntryScannedAt = r.QRCode.EntryScannedAt,
                IsUsedForExit = r.QRCode.IsUsedForExit,
                ExitScannedAt = r.QRCode.ExitScannedAt,
                ExpiresAt = r.QRCode.ExpiresAt,
                ReservationId = r.QRCode.ReservationId,
                ReservationNumber = r.ReservationNumber,
                CreatedAt = r.QRCode.CreatedAt
            } : null,
            Payment = r.Payment != null ? new PaymentDto
            {
                Id = r.Payment.Id,
                Amount = r.Payment.Amount,
                PaymentMethod = r.Payment.PaymentMethod,
                Status = r.Payment.Status,
                TransactionId = r.Payment.TransactionId,
                PaidAt = r.Payment.PaidAt,
                FailureReason = r.Payment.FailureReason,
                ReservationId = r.Payment.ReservationId,
                ReservationNumber = r.ReservationNumber,
                UserId = r.UserId,
                CustomerName = r.User?.FullName,
                CreatedAt = r.Payment.CreatedAt
            } : null,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };
    }
}
