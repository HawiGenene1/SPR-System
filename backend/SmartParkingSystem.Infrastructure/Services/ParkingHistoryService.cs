using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartParkingSystem.Application.DTOs.Common;
using SmartParkingSystem.Application.DTOs.ParkingHistory;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Infrastructure.Data;

namespace SmartParkingSystem.Infrastructure.Services;

public class ParkingHistoryService : IParkingHistoryService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ParkingHistoryService> _logger;

    public ParkingHistoryService(
        ApplicationDbContext context,
        ILogger<ParkingHistoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResultDto<ParkingHistoryDto>> GetCustomerHistoryAsync(
        Guid customerId,
        ParkingHistoryFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        ValidateFilter(filter);

        var pageNumber = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var query = _context.ParkingHistories
            .AsNoTracking()
            .Include(h => h.Reservation)
                .ThenInclude(r => r.User)
            .Include(h => h.Reservation)
                .ThenInclude(r => r.Payment)
            .Include(h => h.Vehicle)
            .Include(h => h.ParkingSlot)
                .ThenInclude(s => s.ParkingArea)
            .Include(h => h.AttendantCheckIn)
            .Include(h => h.AttendantCheckOut)
            .Where(h => !h.IsDeleted && h.Reservation.UserId == customerId);

        query = ApplyFilters(query, filter);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(h => h.CheckInTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(MapToDto).ToList();

        return new PagedResultDto<ParkingHistoryDto>(dtos, totalCount, pageNumber, pageSize);
    }

    public async Task<PagedResultDto<ParkingHistoryDto>> GetAllHistoryAsync(
        ParkingHistoryFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        ValidateFilter(filter);

        var pageNumber = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var query = _context.ParkingHistories
            .AsNoTracking()
            .Include(h => h.Reservation)
                .ThenInclude(r => r.User)
            .Include(h => h.Reservation)
                .ThenInclude(r => r.Payment)
            .Include(h => h.Vehicle)
            .Include(h => h.ParkingSlot)
                .ThenInclude(s => s.ParkingArea)
            .Include(h => h.AttendantCheckIn)
            .Include(h => h.AttendantCheckOut)
            .Where(h => !h.IsDeleted);

        query = ApplyFilters(query, filter);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(h => h.CheckInTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(MapToDto).ToList();

        return new PagedResultDto<ParkingHistoryDto>(dtos, totalCount, pageNumber, pageSize);
    }

    public async Task<ParkingHistoryDto> GetHistoryByIdAsync(
        Guid id,
        Guid currentUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        var history = await _context.ParkingHistories
            .AsNoTracking()
            .Include(h => h.Reservation)
                .ThenInclude(r => r.User)
            .Include(h => h.Reservation)
                .ThenInclude(r => r.Payment)
            .Include(h => h.Vehicle)
            .Include(h => h.ParkingSlot)
                .ThenInclude(s => s.ParkingArea)
            .Include(h => h.AttendantCheckIn)
            .Include(h => h.AttendantCheckOut)
            .FirstOrDefaultAsync(h => h.Id == id && !h.IsDeleted, cancellationToken);

        if (history == null)
        {
            throw new KeyNotFoundException($"Parking history record with ID '{id}' was not found.");
        }

        if (!isAdmin && history.Reservation.UserId != currentUserId)
        {
            _logger.LogWarning("Unauthorized attempt by User {UserId} to access parking history {HistoryId}", currentUserId, id);
            throw new UnauthorizedAccessException("You are not authorized to view another customer's parking history record.");
        }

        return MapToDto(history);
    }

    private static void ValidateFilter(ParkingHistoryFilterRequest filter)
    {
        if (filter.StartDate.HasValue && filter.EndDate.HasValue && filter.StartDate.Value > filter.EndDate.Value)
        {
            throw new ArgumentException("StartDate cannot be greater than EndDate.");
        }
    }

    private static IQueryable<ParkingHistory> ApplyFilters(IQueryable<ParkingHistory> query, ParkingHistoryFilterRequest filter)
    {
        if (filter.StartDate.HasValue)
        {
            var startUtc = DateTime.SpecifyKind(filter.StartDate.Value, DateTimeKind.Utc);
            query = query.Where(h => h.CheckInTime >= startUtc);
        }

        if (filter.EndDate.HasValue)
        {
            var endUtc = DateTime.SpecifyKind(filter.EndDate.Value, DateTimeKind.Utc);
            query = query.Where(h => h.CheckInTime <= endUtc);
        }

        if (filter.ParkingAreaId.HasValue && filter.ParkingAreaId.Value != Guid.Empty)
        {
            query = query.Where(h => h.ParkingSlot.ParkingAreaId == filter.ParkingAreaId.Value);
        }

        if (filter.VehicleId.HasValue && filter.VehicleId.Value != Guid.Empty)
        {
            query = query.Where(h => h.VehicleId == filter.VehicleId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(h =>
                h.Reservation.ReservationNumber.ToLower().Contains(term) ||
                h.Vehicle.LicensePlate.ToLower().Contains(term) ||
                h.ParkingSlot.SlotNumber.ToLower().Contains(term) ||
                h.ParkingSlot.ParkingArea.Name.ToLower().Contains(term) ||
                (h.Reservation.User != null && h.Reservation.User.FullName.ToLower().Contains(term)));
        }

        return query;
    }

    private static ParkingHistoryDto MapToDto(ParkingHistory h)
    {
        var endOrNow = h.CheckOutTime ?? DateTime.UtcNow;
        var durationMinutes = (int)Math.Max(1, Math.Ceiling((endOrNow - h.CheckInTime).TotalMinutes));
        var baseAmount = h.Reservation?.TotalPrice ?? 0.0m;
        var additionalFee = h.OverstayFee ?? 0.0m;
        var totalAmount = baseAmount + additionalFee;

        return new ParkingHistoryDto
        {
            Id = h.Id,
            ReservationId = h.ReservationId,
            ReservationNumber = h.Reservation?.ReservationNumber ?? "Unknown",
            ReservationStatus = h.Reservation?.Status ?? Domain.Enums.ReservationStatus.Completed,
            ScheduledStartTime = h.Reservation?.StartTime ?? h.CheckInTime,
            ScheduledEndTime = h.Reservation?.EndTime ?? (h.CheckOutTime ?? h.CheckInTime.AddHours(2)),
            ActualEntry = h.CheckInTime,
            ActualExit = h.CheckOutTime,
            DurationMinutes = durationMinutes,
            BaseAmount = baseAmount,
            AdditionalFee = additionalFee,
            TotalAmount = totalAmount,
            VehicleId = h.VehicleId,
            LicensePlate = h.Vehicle?.LicensePlate ?? "Unknown",
            VehicleMakeModel = h.Vehicle != null ? $"{h.Vehicle.Make} {h.Vehicle.Model}" : "Unknown",
            VehicleType = h.Vehicle?.VehicleType ?? Domain.Enums.VehicleType.Car,
            ParkingAreaId = h.ParkingSlot?.ParkingAreaId ?? Guid.Empty,
            ParkingAreaName = h.ParkingSlot?.ParkingArea?.Name ?? "Unknown",
            ParkingAreaAddress = h.ParkingSlot?.ParkingArea?.Address ?? "Unknown",
            ParkingSlotId = h.ParkingSlotId,
            SlotNumber = h.ParkingSlot?.SlotNumber ?? "Unknown",
            FloorNumber = h.ParkingSlot?.FloorNumber ?? 0,
            CustomerId = h.Reservation?.UserId ?? Guid.Empty,
            CustomerName = h.Reservation?.User?.FullName ?? "Unknown",
            CustomerEmail = h.Reservation?.User?.Email ?? "Unknown",
            CustomerPhoneNumber = h.Reservation?.User?.PhoneNumber,
            AttendantCheckInId = h.AttendantCheckInId,
            AttendantCheckInName = h.AttendantCheckIn?.FullName,
            AttendantCheckOutId = h.AttendantCheckOutId,
            AttendantCheckOutName = h.AttendantCheckOut?.FullName,
            Notes = h.Notes,
            CreatedAt = h.CreatedAt
        };
    }
}
