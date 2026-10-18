using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartParkingSystem.Application.Common.Security;
using SmartParkingSystem.Application.DTOs.Dashboards;
using SmartParkingSystem.Application.DTOs.ParkingAreas;
using SmartParkingSystem.Application.DTOs.QRCodes;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;
using SmartParkingSystem.Infrastructure.Data;

namespace SmartParkingSystem.Api.Controllers;

[ApiController]
[Route("api/attendant")]
[Authorize(Policy = AuthorizationPolicies.RequireAttendant)]
public class AttendantController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IParkingAreaService _parkingAreaService;
    private readonly ILogger<AttendantController> _logger;

    public AttendantController(
        ApplicationDbContext context,
        IParkingAreaService parkingAreaService,
        ILogger<AttendantController> logger)
    {
        _context = context;
        _parkingAreaService = parkingAreaService;
        _logger = logger;
    }

    #region Parking Areas

    /// <summary>
    /// Attendant: List all active parking facilities with live availability metrics.
    /// </summary>
    [HttpGet("parking-areas")]
    [ProducesResponseType(typeof(IReadOnlyList<ParkingAreaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParkingAreas([FromQuery] string? city, CancellationToken cancellationToken)
    {
        var areas = await _parkingAreaService.GetAllAsync(includeInactive: false, city: city, cancellationToken);
        return Ok(areas);
    }

    /// <summary>
    /// Attendant: Retrieve parking facility details by ID.
    /// </summary>
    [HttpGet("parking-areas/{id:guid}")]
    [ProducesResponseType(typeof(ParkingAreaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetParkingAreaById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var area = await _parkingAreaService.GetByIdAsync(id, cancellationToken);
            return Ok(area);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    #endregion

    #region QR Scanning

    /// <summary>
    /// Attendant: Scan and validate customer QR code.
    /// </summary>
    [HttpPost("scan-qr")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ScanQRCode([FromBody] QRScanRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var qr = await _context.QRCodes
            .Include(q => q.Reservation)
                .ThenInclude(r => r.User)
            .Include(q => q.Reservation)
                .ThenInclude(r => r.Vehicle)
            .Include(q => q.Reservation)
                .ThenInclude(r => r.ParkingSlot)
                    .ThenInclude(s => s.ParkingArea)
            .FirstOrDefaultAsync(q => q.Code == request.QRCode.Trim());

        if (qr == null)
        {
            return NotFound(new { message = "QR code not recognized or invalid." });
        }

        var isExpired = qr.ExpiresAt < DateTime.UtcNow;

        return Ok(new
        {
            QRCode = qr.Code,
            IsValid = !isExpired,
            IsExpired = isExpired,
            IsUsedForEntry = qr.IsUsedForEntry,
            EntryScannedAt = qr.EntryScannedAt,
            IsUsedForExit = qr.IsUsedForExit,
            ExitScannedAt = qr.ExitScannedAt,
            ExpiresAt = qr.ExpiresAt,
            Reservation = new
            {
                qr.Reservation.Id,
                qr.Reservation.ReservationNumber,
                CustomerName = qr.Reservation.User.FullName,
                CustomerPhone = qr.Reservation.User.PhoneNumber,
                LicensePlate = qr.Reservation.Vehicle.LicensePlate,
                Vehicle = $"{qr.Reservation.Vehicle.Make} {qr.Reservation.Vehicle.Model}",
                ParkingArea = qr.Reservation.ParkingSlot.ParkingArea.Name,
                SlotNumber = qr.Reservation.ParkingSlot.SlotNumber,
                FloorNumber = qr.Reservation.ParkingSlot.FloorNumber,
                qr.Reservation.StartTime,
                qr.Reservation.EndTime,
                Status = qr.Reservation.Status.ToString()
            }
        });
    }

    #endregion

    #region Check-in

    /// <summary>
    /// Attendant: Process vehicle entry check-in via validated reservation.
    /// </summary>
    [HttpPost("check-in")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessCheckIn([FromBody] QRScanRequest request)
    {
        var attendantId = GetCurrentAttendantId();

        var qr = await _context.QRCodes
            .Include(q => q.Reservation)
                .ThenInclude(r => r.ParkingSlot)
            .Include(q => q.Reservation)
                .ThenInclude(r => r.Vehicle)
            .FirstOrDefaultAsync(q => q.Code == request.QRCode.Trim());

        if (qr == null)
        {
            return NotFound(new { message = "Invalid QR code." });
        }

        if (qr.IsUsedForEntry)
        {
            return BadRequest(new { message = "This QR code has already been scanned for entry." });
        }

        if (qr.ExpiresAt < DateTime.UtcNow)
        {
            return BadRequest(new { message = "Reservation QR code has expired." });
        }

        var res = qr.Reservation;

        // Mark QR code used for entry
        qr.IsUsedForEntry = true;
        qr.EntryScannedAt = DateTime.UtcNow;

        // Update reservation status to Active
        res.Status = ReservationStatus.Active;

        // Update slot status to Occupied
        res.ParkingSlot.Status = SlotStatus.Occupied;

        // Create parking history
        var history = new ParkingHistory
        {
            ReservationId = res.Id,
            VehicleId = res.VehicleId,
            ParkingSlotId = res.ParkingSlotId,
            CheckInTime = DateTime.UtcNow,
            AttendantCheckInId = attendantId,
            Notes = request.Notes
        };

        await _context.ParkingHistories.AddAsync(history);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Attendant {AttendantId} checked in reservation {ReservationNumber}", attendantId, res.ReservationNumber);

        return Ok(new
        {
            message = "Check-in processed successfully.",
            reservationNumber = res.ReservationNumber,
            checkInTime = history.CheckInTime,
            slotNumber = res.ParkingSlot.SlotNumber,
            licensePlate = res.Vehicle.LicensePlate
        });
    }

    #endregion

    #region Check-out

    /// <summary>
    /// Attendant: Process vehicle exit check-out and calculate any overstay fees.
    /// </summary>
    [HttpPost("check-out")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessCheckOut([FromBody] QRScanRequest request)
    {
        var attendantId = GetCurrentAttendantId();

        var qr = await _context.QRCodes
            .Include(q => q.Reservation)
                .ThenInclude(r => r.ParkingSlot)
                    .ThenInclude(s => s.ParkingArea)
            .Include(q => q.Reservation)
                .ThenInclude(r => r.Vehicle)
            .FirstOrDefaultAsync(q => q.Code == request.QRCode.Trim());

        if (qr == null)
        {
            return NotFound(new { message = "Invalid QR code." });
        }

        if (!qr.IsUsedForEntry)
        {
            return BadRequest(new { message = "Cannot check out: vehicle has not been scanned for entry yet." });
        }

        if (qr.IsUsedForExit)
        {
            return BadRequest(new { message = "This QR code has already been checked out." });
        }

        var res = qr.Reservation;

        var history = await _context.ParkingHistories
            .FirstOrDefaultAsync(h => h.ReservationId == res.Id && h.CheckOutTime == null);

        var checkOutTime = DateTime.UtcNow;

        int? overstayMinutes = null;
        decimal? overstayFee = null;

        if (checkOutTime > res.EndTime)
        {
            var extraMinutes = (int)(checkOutTime - res.EndTime).TotalMinutes;
            if (extraMinutes > 15) // 15-minute grace period
            {
                overstayMinutes = extraMinutes;
                var extraHours = Math.Ceiling(extraMinutes / 60.0);
                overstayFee = (decimal)extraHours * res.ParkingSlot.ParkingArea.HourlyRate;
            }
        }

        qr.IsUsedForExit = true;
        qr.ExitScannedAt = checkOutTime;

        res.Status = ReservationStatus.Completed;
        res.ParkingSlot.Status = SlotStatus.Available;

        if (history != null)
        {
            history.CheckOutTime = checkOutTime;
            history.AttendantCheckOutId = attendantId;
            history.OverstayDurationMinutes = overstayMinutes;
            history.OverstayFee = overstayFee;
            if (!string.IsNullOrWhiteSpace(request.Notes))
                history.Notes = $"{history.Notes} | Checkout: {request.Notes}".Trim(' ', '|');
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Attendant {AttendantId} checked out reservation {ReservationNumber}", attendantId, res.ReservationNumber);

        return Ok(new
        {
            message = "Check-out processed successfully.",
            reservationNumber = res.ReservationNumber,
            checkOutTime,
            overstayMinutes,
            overstayFee,
            slotNumber = res.ParkingSlot.SlotNumber
        });
    }

    #endregion

    #region Parking Occupancy

    /// <summary>
    /// Attendant: Get live slot occupancy metrics for the attendant's assigned parking facility.
    /// </summary>
    [HttpGet("occupancy")]
    [ProducesResponseType(typeof(AttendantDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFacilityOccupancy()
    {
        var attendantId = GetCurrentAttendantId();
        var attendant = await _context.Users
            .Include(u => u.AssignedParkingArea!)
                .ThenInclude(a => a.ParkingSlots)
            .FirstOrDefaultAsync(u => u.Id == attendantId);

        var area = attendant?.AssignedParkingArea 
                   ?? await _context.ParkingAreas.Include(a => a.ParkingSlots).FirstOrDefaultAsync();

        if (area == null)
        {
            return Ok(new AttendantDashboardDto
            {
                TotalSlots = 0,
                AvailableSlots = 0,
                OccupiedSlots = 0
            });
        }

        var slots = area.ParkingSlots.ToList();
        var total = slots.Count;
        var occupied = slots.Count(s => s.Status == SlotStatus.Occupied);
        var reserved = slots.Count(s => s.Status == SlotStatus.Reserved);
        var available = slots.Count(s => s.Status == SlotStatus.Available);

        var todayStart = DateTime.UtcNow.Date;
        var checkInsToday = await _context.ParkingHistories
            .CountAsync(h => h.ParkingSlot.ParkingAreaId == area.Id && h.CheckInTime >= todayStart);

        var checkOutsToday = await _context.ParkingHistories
            .CountAsync(h => h.ParkingSlot.ParkingAreaId == area.Id && h.CheckOutTime >= todayStart);

        var pendingArrivals = await _context.Reservations
            .CountAsync(r => r.ParkingSlot.ParkingAreaId == area.Id 
                             && r.Status == ReservationStatus.Pending 
                             && r.StartTime >= todayStart);

        return Ok(new AttendantDashboardDto
        {
            AssignedParkingAreaId = area.Id,
            AssignedParkingAreaName = area.Name,
            TotalSlots = total,
            AvailableSlots = available,
            OccupiedSlots = occupied,
            ReservedSlots = reserved,
            OccupancyRatePercentage = total > 0 ? Math.Round((double)occupied / total * 100, 2) : 0,
            CheckInsToday = checkInsToday,
            CheckOutsToday = checkOutsToday,
            PendingArrivalsToday = pendingArrivals
        });
    }

    #endregion

    private Guid GetCurrentAttendantId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;

        if (Guid.TryParse(claim, out var id))
        {
            return id;
        }

        throw new UnauthorizedAccessException("Attendant ID not found in claims.");
    }
}
