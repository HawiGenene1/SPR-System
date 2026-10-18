using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartParkingSystem.Application.DTOs.Parking;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;
using SmartParkingSystem.Infrastructure.Data;

namespace SmartParkingSystem.Infrastructure.Services;

public class ParkingService : IParkingService
{
    private readonly ApplicationDbContext _context;
    private readonly IParkingHistoryRepository _parkingHistoryRepository;
    private readonly ILogger<ParkingService> _logger;

    public ParkingService(
        ApplicationDbContext context,
        IParkingHistoryRepository parkingHistoryRepository,
        ILogger<ParkingService> logger)
    {
        _context = context;
        _parkingHistoryRepository = parkingHistoryRepository;
        _logger = logger;
    }

    public async Task<CheckInResultDto> CheckInVehicleAsync(
        CheckInRequest request,
        Guid attendantId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        var token = request.QRCode?.Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("QR Code is required.");
        }

        var now = DateTime.UtcNow;

        // 1. Validate QR Code & eager load complete reservation hierarchy
        var qr = await _context.QRCodes
            .Include(q => q.Reservation)
                .ThenInclude(r => r.User)
            .Include(q => q.Reservation)
                .ThenInclude(r => r.Vehicle)
            .Include(q => q.Reservation)
                .ThenInclude(r => r.ParkingSlot)
                    .ThenInclude(s => s.ParkingArea)
            .Include(q => q.Reservation)
                .ThenInclude(r => r.Payment)
            .FirstOrDefaultAsync(q => q.Code == token && !q.IsDeleted, cancellationToken);

        if (qr == null)
        {
            throw new KeyNotFoundException("QR Code was not recognized or is invalid.");
        }

        if (qr.ExpiresAt < now)
        {
            throw new InvalidOperationException("QR code has expired.");
        }

        // 2. Find and verify Reservation
        var res = qr.Reservation;
        if (res == null || res.IsDeleted)
        {
            throw new KeyNotFoundException("Reservation associated with this QR code was not found or has been removed.");
        }

        // 3. Verify Reservation Status (must be Confirmed)
        if (res.Status == ReservationStatus.Cancelled)
        {
            throw new InvalidOperationException("Reservation has been cancelled and cannot be checked in.");
        }

        if (res.Status == ReservationStatus.Completed)
        {
            throw new InvalidOperationException("Reservation has already been completed.");
        }

        if (res.Status == ReservationStatus.Active)
        {
            throw new InvalidOperationException("Vehicle is already checked in for this reservation.");
        }

        if (res.Status != ReservationStatus.Confirmed)
        {
            throw new InvalidOperationException($"Reservation status is '{res.Status}'. Only confirmed reservations can be checked in.");
        }

        // 4. Verify Time Window (early arrival allowed up to 30 mins before; reject if window ended)
        if (now < res.StartTime.AddMinutes(-30))
        {
            throw new InvalidOperationException(
                $"Check-in is too early. Entry is allowed starting 30 minutes before scheduled start time ({res.StartTime:yyyy-MM-dd HH:mm} UTC).");
        }

        if (now > res.EndTime.AddHours(1))
        {
            throw new InvalidOperationException("Reservation time window has already expired.");
        }

        // 5. Verify Payment (must be Paid)
        if (res.Payment == null || res.Payment.Status != PaymentStatus.Paid)
        {
            throw new InvalidOperationException("Check-in denied: Reservation payment has not been completed. Completed payment is required prior to entry.");
        }

        // 6. Prevent Duplicate Check-In
        if (qr.IsUsedForEntry)
        {
            throw new InvalidOperationException($"Duplicate check-in: This QR code was already checked in at {qr.EntryScannedAt:yyyy-MM-dd HH:mm:ss} UTC.");
        }

        var activeHistory = await _parkingHistoryRepository.GetActiveByReservationIdAsync(res.Id, cancellationToken);
        if (activeHistory != null)
        {
            throw new InvalidOperationException("Duplicate check-in: An active parking session already exists for this vehicle.");
        }

        // Facility check (if attendant specified a target facility)
        if (request.ParkingAreaId.HasValue && request.ParkingAreaId.Value != Guid.Empty)
        {
            if (res.ParkingSlot.ParkingAreaId != request.ParkingAreaId.Value)
            {
                throw new InvalidOperationException("This reservation is for a different parking facility.");
            }
        }

        // Fetch attendant info for attribution
        var attendant = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == attendantId && !u.IsDeleted, cancellationToken);

        // 7. Atomic Execution in Database Transaction (guarded for relational providers)
        async Task<CheckInResultDto> ExecuteCheckInCoreAsync()
        {
            // Record entry time and mark QR code used for entry
            qr.IsUsedForEntry = true;
            qr.EntryScannedAt = now;

            // Set Reservation Active
            res.Status = ReservationStatus.Active;
            res.UpdatedAt = now;

            // Set Parking Slot Occupied
            res.ParkingSlot.Status = SlotStatus.Occupied;
            res.ParkingSlot.UpdatedAt = now;

            // Record who performed check-in in ParkingHistory
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

            await _parkingHistoryRepository.AddAsync(history, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Vehicle {Plate} checked into Slot {Slot} at {Facility} by Attendant {AttendantId} (Reservation: {ResNum})",
                res.Vehicle.LicensePlate, res.ParkingSlot.SlotNumber, res.ParkingSlot.ParkingArea.Name, attendantId, res.ReservationNumber);

            return new CheckInResultDto
            {
                Success = true,
                Message = "Vehicle entry check-in processed successfully.",
                ReservationId = res.Id,
                ReservationNumber = res.ReservationNumber,
                ParkingSlotId = res.ParkingSlot.Id,
                SlotNumber = res.ParkingSlot.SlotNumber,
                FloorNumber = res.ParkingSlot.FloorNumber,
                ParkingAreaId = res.ParkingSlot.ParkingArea.Id,
                ParkingAreaName = res.ParkingSlot.ParkingArea.Name,
                VehicleId = res.Vehicle.Id,
                LicensePlate = res.Vehicle.LicensePlate,
                VehicleMakeModel = $"{res.Vehicle.Make} {res.Vehicle.Model}",
                CustomerId = res.User.Id,
                CustomerName = res.User.FullName,
                CustomerPhoneNumber = res.User.PhoneNumber,
                CheckInTime = now,
                ScheduledStartTime = res.StartTime,
                ScheduledEndTime = res.EndTime,
                ReservationStatus = ReservationStatus.Active,
                SlotStatus = SlotStatus.Occupied,
                PaymentStatus = res.Payment.Status,
                AttendantId = attendantId,
                AttendantName = attendant?.FullName ?? "Staff Attendant",
                Notes = request.Notes
            };
        }

        if (_context.Database.IsRelational())
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
                try
                {
                    var result = await ExecuteCheckInCoreAsync();
                    await transaction.CommitAsync(cancellationToken);
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }

        return await ExecuteCheckInCoreAsync();
    }

    public async Task<CheckOutResultDto> CheckOutVehicleAsync(
        CheckOutRequest request,
        Guid attendantId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        var token = request.QRCode?.Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("QR Code is required.");
        }

        var now = DateTime.UtcNow;

        // 1. Scan QR & eager load reservation hierarchy
        var qr = await _context.QRCodes
            .Include(q => q.Reservation)
                .ThenInclude(r => r.User)
            .Include(q => q.Reservation)
                .ThenInclude(r => r.Vehicle)
            .Include(q => q.Reservation)
                .ThenInclude(r => r.ParkingSlot)
                    .ThenInclude(s => s.ParkingArea)
            .Include(q => q.Reservation)
                .ThenInclude(r => r.Payment)
            .FirstOrDefaultAsync(q => q.Code == token && !q.IsDeleted, cancellationToken);

        if (qr == null)
        {
            throw new KeyNotFoundException("QR Code was not recognized or is invalid.");
        }

        // 2. Prevent checkout if the vehicle was never checked in
        if (!qr.IsUsedForEntry)
        {
            throw new InvalidOperationException("Cannot check out: vehicle has not been checked in yet.");
        }

        // 3. Prevent duplicate checkout
        if (qr.IsUsedForExit)
        {
            throw new InvalidOperationException($"Duplicate check-out: This vehicle was already checked out at {qr.ExitScannedAt:yyyy-MM-dd HH:mm:ss} UTC.");
        }

        // 4. Find Active Reservation
        var res = qr.Reservation;
        if (res == null || res.IsDeleted)
        {
            throw new KeyNotFoundException("Reservation associated with this QR code was not found or has been removed.");
        }

        if (res.Status == ReservationStatus.Completed)
        {
            throw new InvalidOperationException("Reservation has already been completed.");
        }

        if (res.Status == ReservationStatus.Cancelled)
        {
            throw new InvalidOperationException("Reservation has been cancelled.");
        }

        if (res.Status != ReservationStatus.Active)
        {
            throw new InvalidOperationException($"Cannot check out: Reservation status is '{res.Status}'. Only active reservations can be checked out.");
        }

        // Optional facility check
        if (request.ParkingAreaId.HasValue && request.ParkingAreaId.Value != Guid.Empty)
        {
            if (res.ParkingSlot.ParkingAreaId != request.ParkingAreaId.Value)
            {
                throw new InvalidOperationException("This reservation belongs to a different parking facility.");
            }
        }

        // Retrieve active parking history record
        var activeHistory = await _parkingHistoryRepository.GetActiveByReservationIdAsync(res.Id, cancellationToken);

        // Record Actual Exit & Calculate actual duration
        var entryTime = activeHistory?.CheckInTime ?? qr.EntryScannedAt ?? res.StartTime;
        var actualDuration = now - entryTime;
        var actualDurationMinutes = Math.Max(1, (int)Math.Ceiling(actualDuration.TotalMinutes));

        // Calculate additional charge if necessary (overstay beyond scheduled EndTime + 15 min grace period)
        int overstayMinutes = 0;
        decimal overstayFee = 0.0m;
        if (now > res.EndTime.AddMinutes(15))
        {
            overstayMinutes = (int)Math.Ceiling((now - res.EndTime).TotalMinutes);
            var extraHours = (decimal)Math.Ceiling(overstayMinutes / 60.0);
            var rate = res.ParkingSlot?.HourlyRate ?? res.ParkingSlot?.ParkingArea?.HourlyRate ?? 5.0m;
            overstayFee = extraHours * rate;
        }

        var attendant = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == attendantId && !u.IsDeleted, cancellationToken);

        // 5. Transactional Execution in Database Transaction (guarded for relational providers)
        async Task<CheckOutResultDto> ExecuteCheckOutCoreAsync()
        {
            // Record Actual Exit
            qr.IsUsedForExit = true;
            qr.ExitScannedAt = now;
            qr.UpdatedAt = now;

            // Complete Reservation
            res.Status = ReservationStatus.Completed;
            res.UpdatedAt = now;

            // Set Slot Available
            if (res.ParkingSlot != null)
            {
                res.ParkingSlot.Status = SlotStatus.Available;
                res.ParkingSlot.UpdatedAt = now;
            }

            // Finalize or Create ParkingHistory
            if (activeHistory != null)
            {
                activeHistory.CheckOutTime = now;
                activeHistory.AttendantCheckOutId = attendantId;
                activeHistory.OverstayDurationMinutes = overstayMinutes;
                activeHistory.OverstayFee = overstayFee;
                activeHistory.UpdatedAt = now;

                if (!string.IsNullOrWhiteSpace(request.Notes))
                {
                    activeHistory.Notes = string.IsNullOrWhiteSpace(activeHistory.Notes)
                        ? request.Notes
                        : $"{activeHistory.Notes}; Exit: {request.Notes}";
                }

                await _parkingHistoryRepository.UpdateAsync(activeHistory, cancellationToken);
            }
            else
            {
                var newHistory = new ParkingHistory
                {
                    Id = Guid.NewGuid(),
                    ReservationId = res.Id,
                    VehicleId = res.VehicleId,
                    ParkingSlotId = res.ParkingSlotId,
                    CheckInTime = entryTime,
                    CheckOutTime = now,
                    AttendantCheckInId = attendantId,
                    AttendantCheckOutId = attendantId,
                    OverstayDurationMinutes = overstayMinutes,
                    OverstayFee = overstayFee,
                    Notes = request.Notes,
                    CreatedAt = now
                };
                await _parkingHistoryRepository.AddAsync(newHistory, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Vehicle {Plate} checked out from Slot {Slot} by Attendant {AttendantId} (Duration: {Duration}m, Overstay: {Overstay}m, Fee: {Fee:C})",
                res.Vehicle.LicensePlate, res.ParkingSlot?.SlotNumber, attendantId, actualDurationMinutes, overstayMinutes, overstayFee);

            var basePrice = res.TotalPrice;
            var totalAmount = basePrice + overstayFee;
            var message = overstayFee > 0
                ? $"Vehicle exit check-out completed with an overstay fee of {overstayFee:F2} USD ({overstayMinutes} mins overstay)."
                : "Vehicle exit check-out processed successfully.";

            return new CheckOutResultDto
            {
                Success = true,
                Message = message,
                ReservationId = res.Id,
                ReservationNumber = res.ReservationNumber,
                ParkingSlotId = res.ParkingSlot?.Id ?? Guid.Empty,
                SlotNumber = res.ParkingSlot?.SlotNumber ?? string.Empty,
                FloorNumber = res.ParkingSlot?.FloorNumber ?? 0,
                ParkingAreaId = res.ParkingSlot?.ParkingArea?.Id ?? Guid.Empty,
                ParkingAreaName = res.ParkingSlot?.ParkingArea?.Name ?? string.Empty,
                VehicleId = res.Vehicle.Id,
                LicensePlate = res.Vehicle.LicensePlate,
                VehicleMakeModel = $"{res.Vehicle.Make} {res.Vehicle.Model}",
                CustomerId = res.User.Id,
                CustomerName = res.User.FullName,
                CustomerPhoneNumber = res.User.PhoneNumber,
                CheckInTime = entryTime,
                CheckOutTime = now,
                ScheduledStartTime = res.StartTime,
                ScheduledEndTime = res.EndTime,
                ActualDurationMinutes = actualDurationMinutes,
                OverstayMinutes = overstayMinutes,
                BasePrice = basePrice,
                OverstayFee = overstayFee,
                TotalAmount = totalAmount,
                ReservationStatus = ReservationStatus.Completed,
                SlotStatus = SlotStatus.Available,
                AttendantCheckInId = activeHistory?.AttendantCheckInId,
                AttendantCheckOutId = attendantId,
                AttendantCheckOutName = attendant?.FullName ?? "Staff Attendant",
                Notes = request.Notes
            };
        }

        if (_context.Database.IsRelational())
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
                try
                {
                    var result = await ExecuteCheckOutCoreAsync();
                    await transaction.CommitAsync(cancellationToken);
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }

        return await ExecuteCheckOutCoreAsync();
    }
}
