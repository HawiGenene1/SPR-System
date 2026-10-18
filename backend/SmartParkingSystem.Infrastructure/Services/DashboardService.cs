using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartParkingSystem.Application.Common.Security;
using SmartParkingSystem.Application.DTOs.Dashboards;
using SmartParkingSystem.Application.DTOs.ParkingHistory;
using SmartParkingSystem.Application.DTOs.Payments;
using SmartParkingSystem.Application.DTOs.QRCodes;
using SmartParkingSystem.Application.DTOs.Reservations;
using SmartParkingSystem.Application.DTOs.Vehicles;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;
using SmartParkingSystem.Infrastructure.Data;

namespace SmartParkingSystem.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        ApplicationDbContext context,
        ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var todayUtcStart = now.Date;
        var todayUtcEnd = todayUtcStart.AddDays(1);
        var monthUtcStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        const int chartDays = 14;
        var chartWindowStart = todayUtcStart.AddDays(-(chartDays - 1));

        // 1. Optimized User Counts (loads role names in a single projection)
        var usersWithRole = await _context.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted)
            .Include(u => u.Role)
            .Select(u => new { RoleName = u.Role != null ? u.Role.Name : string.Empty })
            .ToListAsync(cancellationToken);

        var totalUsers = usersWithRole.Count;
        var totalCustomers = usersWithRole.Count(u => u.RoleName == AppRoles.Customer);
        var totalAttendants = usersWithRole.Count(u => u.RoleName == AppRoles.ParkingAttendant);

        // 2. Optimized Slot Breakdown (single query covering total, available, reserved, occupied, maintenance)
        var allSlots = await _context.ParkingSlots
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .ToListAsync(cancellationToken);

        var totalSlots = allSlots.Count;
        var availableSlots = allSlots.Count(s => s.Status == SlotStatus.Available);
        var reservedSlots = allSlots.Count(s => s.Status == SlotStatus.Reserved);
        var occupiedSlots = allSlots.Count(s => s.Status == SlotStatus.Occupied);
        var maintenanceSlots = allSlots.Count(s => s.Status == SlotStatus.Maintenance);
        var overallOccupancyRate = totalSlots > 0 ? Math.Round((double)occupiedSlots / totalSlots * 100, 2) : 0;

        // 3. Optimized Parking Areas Count
        var activeAreas = await _context.ParkingAreas
            .AsNoTracking()
            .Where(a => !a.IsDeleted && a.IsActive)
            .ToListAsync(cancellationToken);

        var totalParkingAreas = activeAreas.Count;

        // 4. Optimized Reservation Breakdown (single query with lightweight projection)
        var allReservations = await _context.Reservations
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .Select(r => new { r.Status, r.CreatedAt, r.StartTime })
            .ToListAsync(cancellationToken);

        var activeReservations = allReservations.Count(r => r.Status == ReservationStatus.Active);
        var completedReservations = allReservations.Count(r => r.Status == ReservationStatus.Completed);
        var cancelledReservations = allReservations.Count(r => r.Status == ReservationStatus.Cancelled);

        var todaysReservations = allReservations.Count(r =>
            (r.CreatedAt.Date == todayUtcStart) || (r.StartTime.Date == todayUtcStart));

        // 5. Optimized Revenue Aggregations
        var paidPayments = await _context.Payments
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.Status == PaymentStatus.Paid)
            .Select(p => new { p.Amount, PaymentDate = p.PaidAt ?? p.CreatedAt })
            .ToListAsync(cancellationToken);

        var monthlyOverstay = await _context.ParkingHistories
            .AsNoTracking()
            .Where(h => !h.IsDeleted && h.OverstayFee.HasValue && h.CheckOutTime.HasValue)
            .Select(h => new { Fee = h.OverstayFee ?? 0m, OverstayDate = h.CheckOutTime ?? DateTime.MinValue })
            .ToListAsync(cancellationToken);

        var todayPaymentsSum = paidPayments.Where(p => p.PaymentDate.Date == todayUtcStart).Sum(p => p.Amount);
        var todayOverstaySum = monthlyOverstay.Where(h => h.OverstayDate.Date == todayUtcStart).Sum(h => h.Fee);
        var todaysRevenue = todayPaymentsSum + todayOverstaySum;

        var monthlyPaymentsSum = paidPayments.Where(p => p.PaymentDate >= monthUtcStart).Sum(p => p.Amount);
        var monthlyOverstaySum = monthlyOverstay.Where(h => h.OverstayDate >= monthUtcStart).Sum(h => h.Fee);
        var monthlyRevenue = monthlyPaymentsSum + monthlyOverstaySum;

        var totalPaymentsSum = paidPayments.Sum(p => p.Amount);
        var totalOverstaySum = monthlyOverstay.Sum(h => h.Fee);
        var totalRevenue = totalPaymentsSum + totalOverstaySum;

        // 6. Chart: Revenue By Day (continuous 14-day time series)
        var revenueByDayList = new List<DailyRevenueChartDto>(chartDays);
        for (int i = 0; i < chartDays; i++)
        {
            var d = chartWindowStart.AddDays(i);
            var dayPayments = paidPayments.Where(p => p.PaymentDate.Date == d).ToList();
            var dayOverstay = monthlyOverstay.Where(h => h.OverstayDate.Date == d).ToList();
            var dayRev = dayPayments.Sum(p => p.Amount) + dayOverstay.Sum(h => h.Fee);
            var txCount = dayPayments.Count + dayOverstay.Count;

            revenueByDayList.Add(new DailyRevenueChartDto
            {
                Date = d,
                DateLabel = d.ToString("MMM dd"),
                Revenue = dayRev,
                TransactionsCount = txCount
            });
        }

        // 7. Chart: Reservations By Day (continuous 14-day time series)
        var reservationsByDayList = new List<DailyReservationChartDto>(chartDays);
        for (int i = 0; i < chartDays; i++)
        {
            var d = chartWindowStart.AddDays(i);
            var dayRes = allReservations.Where(r => r.StartTime.Date == d || (r.StartTime == default && r.CreatedAt.Date == d)).ToList();

            reservationsByDayList.Add(new DailyReservationChartDto
            {
                Date = d,
                DateLabel = d.ToString("MMM dd"),
                TotalReservations = dayRes.Count,
                CompletedCount = dayRes.Count(r => r.Status == ReservationStatus.Completed),
                CancelledCount = dayRes.Count(r => r.Status == ReservationStatus.Cancelled),
                ActiveCount = dayRes.Count(r => r.Status == ReservationStatus.Active)
            });
        }

        // 8. Chart: Occupancy Statistics (Facility & Slot Type breakdown)
        var areaStats = activeAreas
            .Select(a =>
            {
                var areaSlots = allSlots.Where(s => s.ParkingAreaId == a.Id).ToList();
                var occCount = areaSlots.Count(s => s.Status == SlotStatus.Occupied);
                return new AreaOccupancyChartDto
                {
                    ParkingAreaId = a.Id,
                    ParkingAreaName = a.Name,
                    TotalSlots = areaSlots.Count,
                    OccupiedSlots = occCount,
                    ReservedSlots = areaSlots.Count(s => s.Status == SlotStatus.Reserved),
                    AvailableSlots = areaSlots.Count(s => s.Status == SlotStatus.Available),
                    MaintenanceSlots = areaSlots.Count(s => s.Status == SlotStatus.Maintenance),
                    OccupancyRatePercentage = areaSlots.Count > 0
                        ? Math.Round((double)occCount / areaSlots.Count * 100, 2)
                        : 0
                };
            })
            .OrderBy(a => a.ParkingAreaName)
            .ToList();

        var slotTypeStats = allSlots
            .GroupBy(s => s.SlotType)
            .Select(g =>
            {
                var count = g.Count();
                var occCount = g.Count(s => s.Status == SlotStatus.Occupied);
                return new SlotTypeOccupancyChartDto
                {
                    SlotType = g.Key,
                    TotalSlots = count,
                    OccupiedSlots = occCount,
                    ReservedSlots = g.Count(s => s.Status == SlotStatus.Reserved),
                    AvailableSlots = g.Count(s => s.Status == SlotStatus.Available),
                    OccupancyRatePercentage = count > 0
                        ? Math.Round((double)occCount / count * 100, 2)
                        : 0
                };
            })
            .OrderBy(s => s.SlotType)
            .ToList();

        var occupancyStatistics = new OccupancyStatisticsDto
        {
            OverallOccupancyRatePercentage = overallOccupancyRate,
            TotalSlots = totalSlots,
            OccupiedSlots = occupiedSlots,
            ReservedSlots = reservedSlots,
            AvailableSlots = availableSlots,
            MaintenanceSlots = maintenanceSlots,
            ByParkingArea = areaStats,
            BySlotType = slotTypeStats
        };

        // 9. Chart: Peak Parking Hours (24-hour distribution)
        var recentHoursStart = todayUtcStart.AddDays(-30);
        var recentReservationHours = await _context.Reservations
            .AsNoTracking()
            .Where(r => !r.IsDeleted && r.StartTime >= recentHoursStart)
            .Select(r => r.StartTime.Hour)
            .ToListAsync(cancellationToken);

        var totalHourlyEntries = recentReservationHours.Count;
        var hourlyGroupCounts = recentReservationHours
            .GroupBy(h => h)
            .ToDictionary(g => g.Key, g => g.Count());

        var peakHoursList = new List<HourlyPeakChartDto>(24);
        for (int h = 0; h < 24; h++)
        {
            var count = hourlyGroupCounts.TryGetValue(h, out var c) ? c : 0;
            var pct = totalHourlyEntries > 0 ? Math.Round((double)count / totalHourlyEntries * 100, 2) : 0;
            var hourLabel = FormatHourLabel(h);

            peakHoursList.Add(new HourlyPeakChartDto
            {
                Hour = h,
                HourLabel = hourLabel,
                ReservationCount = count,
                Percentage = pct,
                IsPeakHour = false
            });
        }

        // Flag top 3 peak hours
        var top3Hours = peakHoursList
            .Where(x => x.ReservationCount > 0)
            .OrderByDescending(x => x.ReservationCount)
            .Take(3)
            .Select(x => x.Hour)
            .ToHashSet();

        foreach (var item in peakHoursList)
        {
            if (top3Hours.Contains(item.Hour))
            {
                item.IsPeakHour = true;
            }
        }

        // 10. Recent Activity List
        var recentReservations = await _context.Reservations
            .AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.ParkingSlot)
                .ThenInclude(s => s.ParkingArea)
            .Include(r => r.Vehicle)
            .Where(r => !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Select(r => new ReservationSummaryDto
            {
                Id = r.Id,
                ReservationNumber = r.ReservationNumber,
                CustomerName = r.User.FullName,
                ParkingAreaName = r.ParkingSlot.ParkingArea.Name,
                SlotNumber = r.ParkingSlot.SlotNumber,
                LicensePlate = r.Vehicle.LicensePlate,
                StartTime = r.StartTime,
                EndTime = r.EndTime,
                TotalPrice = r.TotalPrice,
                Status = r.Status,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Admin dashboard metrics computed successfully. Total Users: {TotalUsers}, Total Slots: {TotalSlots}",
            totalUsers, totalSlots);

        return new AdminDashboardDto
        {
            TotalUsers = totalUsers,
            TotalCustomers = totalCustomers,
            TotalAttendants = totalAttendants,
            TotalParkingAreas = totalParkingAreas,
            TotalParkingSlots = totalSlots,
            AvailableSlots = availableSlots,
            ReservedSlots = reservedSlots,
            OccupiedSlots = occupiedSlots,
            MaintenanceSlots = maintenanceSlots,
            OccupancyRatePercentage = overallOccupancyRate,
            TodaysReservations = todaysReservations,
            ActiveReservations = activeReservations,
            CompletedReservations = completedReservations,
            CancelledReservations = cancelledReservations,
            TodaysRevenue = todaysRevenue,
            MonthlyRevenue = monthlyRevenue,
            TotalRevenue = totalRevenue,
            RevenueByDay = revenueByDayList,
            ReservationsByDay = reservationsByDayList,
            OccupancyStatistics = occupancyStatistics,
            PeakParkingHours = peakHoursList,
            RecentReservations = recentReservations
        };
    }

    public async Task<AttendantDashboardDto> GetAttendantDashboardAsync(
        Guid currentUserId,
        bool isAdmin,
        Guid? parkingAreaId = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var todayUtcStart = now.Date;
        var todayUtcEnd = todayUtcStart.AddDays(1);

        Guid? targetAreaId = parkingAreaId;
        string? assignedAreaName = null;
        string? facilityAddress = null;

        // Resolve attendant's assigned parking facility if not an explicit admin query
        if (!isAdmin || targetAreaId == null)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.AssignedParkingArea)
                .FirstOrDefaultAsync(u => u.Id == currentUserId && !u.IsDeleted, cancellationToken);

            if (user?.AssignedParkingAreaId != null)
            {
                targetAreaId = user.AssignedParkingAreaId;
                assignedAreaName = user.AssignedParkingArea?.Name;
                facilityAddress = user.AssignedParkingArea?.Address;
            }
        }

        // If targetAreaId was explicitly requested or resolved, load its details
        if (targetAreaId != null && assignedAreaName == null)
        {
            var area = await _context.ParkingAreas
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == targetAreaId.Value && !a.IsDeleted, cancellationToken);

            if (area != null)
            {
                assignedAreaName = area.Name;
                facilityAddress = area.Address;
            }
        }

        if (targetAreaId == null && isAdmin)
        {
            assignedAreaName = "All Facilities (System-Wide)";
        }
        else if (targetAreaId == null)
        {
            // Fallback for attendants without explicit facility assignment: pick the first active parking facility
            var defaultArea = await _context.ParkingAreas
                .AsNoTracking()
                .FirstOrDefaultAsync(a => !a.IsDeleted && a.IsActive, cancellationToken);

            if (defaultArea != null)
            {
                targetAreaId = defaultArea.Id;
                assignedAreaName = defaultArea.Name;
                facilityAddress = defaultArea.Address;
            }
            else
            {
                assignedAreaName = "No Facility Assigned";
            }
        }

        // 1. Live Slots Inventory for the target facility
        var slotsQuery = _context.ParkingSlots
            .AsNoTracking()
            .Where(s => !s.IsDeleted);

        if (targetAreaId != null)
        {
            slotsQuery = slotsQuery.Where(s => s.ParkingAreaId == targetAreaId.Value);
        }

        var slots = await slotsQuery.ToListAsync(cancellationToken);
        var totalSlots = slots.Count;
        var availableSlots = slots.Count(s => s.Status == SlotStatus.Available);
        var occupiedSlots = slots.Count(s => s.Status == SlotStatus.Occupied);
        var reservedSlots = slots.Count(s => s.Status == SlotStatus.Reserved);
        var maintenanceSlots = slots.Count(s => s.Status == SlotStatus.Maintenance);
        var occupancyRate = totalSlots > 0 ? Math.Round((double)occupiedSlots / totalSlots * 100, 2) : 0;

        // 2. Today's Check-Ins (entered facility today)
        var checkInsQuery = _context.ParkingHistories
            .AsNoTracking()
            .Where(h => !h.IsDeleted && h.CheckInTime >= todayUtcStart && h.CheckInTime < todayUtcEnd);

        if (targetAreaId != null)
        {
            checkInsQuery = checkInsQuery.Where(h => h.ParkingSlot.ParkingAreaId == targetAreaId.Value);
        }
        var todaysCheckIns = await checkInsQuery.CountAsync(cancellationToken);

        // 3. Today's Check-Outs (exited facility today)
        var checkOutsQuery = _context.ParkingHistories
            .AsNoTracking()
            .Where(h => !h.IsDeleted && h.CheckOutTime.HasValue && h.CheckOutTime >= todayUtcStart && h.CheckOutTime < todayUtcEnd);

        if (targetAreaId != null)
        {
            checkOutsQuery = checkOutsQuery.Where(h => h.ParkingSlot.ParkingAreaId == targetAreaId.Value);
        }
        var todaysCheckOuts = await checkOutsQuery.CountAsync(cancellationToken);

        // 4. Active Reservations (vehicles currently parked on-site)
        var activeResQuery = _context.Reservations
            .AsNoTracking()
            .Where(r => !r.IsDeleted && r.Status == ReservationStatus.Active);

        if (targetAreaId != null)
        {
            activeResQuery = activeResQuery.Where(r => r.ParkingSlot.ParkingAreaId == targetAreaId.Value);
        }
        var activeReservations = await activeResQuery.CountAsync(cancellationToken);

        // 5. Scheduled Arrivals Today
        var arrivalsQuery = _context.Reservations
            .AsNoTracking()
            .Where(r => !r.IsDeleted && r.StartTime >= todayUtcStart && r.StartTime < todayUtcEnd);

        if (targetAreaId != null)
        {
            arrivalsQuery = arrivalsQuery.Where(r => r.ParkingSlot.ParkingAreaId == targetAreaId.Value);
        }
        var confirmedArrivalsToday = await arrivalsQuery.CountAsync(r => r.Status == ReservationStatus.Confirmed, cancellationToken);
        var pendingArrivalsToday = await arrivalsQuery.CountAsync(r => r.Status == ReservationStatus.Pending, cancellationToken);

        // 6. Recent Vehicle Entries (latest check-ins)
        var recentEntriesQuery = _context.ParkingHistories
            .AsNoTracking()
            .Include(h => h.Reservation)
                .ThenInclude(r => r.User)
            .Include(h => h.Vehicle)
            .Include(h => h.ParkingSlot)
                .ThenInclude(s => s.ParkingArea)
            .Include(h => h.AttendantCheckIn)
            .Where(h => !h.IsDeleted);

        if (targetAreaId != null)
        {
            recentEntriesQuery = recentEntriesQuery.Where(h => h.ParkingSlot.ParkingAreaId == targetAreaId.Value);
        }

        var recentVehicleEntries = await recentEntriesQuery
            .OrderByDescending(h => h.CheckInTime)
            .Take(10)
            .Select(h => new RecentVehicleEntryDto
            {
                ParkingHistoryId = h.Id,
                ReservationId = h.ReservationId,
                ReservationNumber = h.Reservation != null ? h.Reservation.ReservationNumber : "Unknown",
                LicensePlate = h.Vehicle != null ? h.Vehicle.LicensePlate : "Unknown",
                VehicleMakeModel = h.Vehicle != null ? $"{h.Vehicle.Make} {h.Vehicle.Model}" : "Unknown",
                VehicleType = h.Vehicle != null ? h.Vehicle.VehicleType : VehicleType.Car,
                SlotNumber = h.ParkingSlot != null ? h.ParkingSlot.SlotNumber : "Unknown",
                FloorNumber = h.ParkingSlot != null ? h.ParkingSlot.FloorNumber : 0,
                ParkingAreaName = h.ParkingSlot != null && h.ParkingSlot.ParkingArea != null ? h.ParkingSlot.ParkingArea.Name : "Unknown",
                CheckInTime = h.CheckInTime,
                CheckOutTime = h.CheckOutTime,
                CustomerName = h.Reservation != null && h.Reservation.User != null ? h.Reservation.User.FullName : "Unknown",
                CustomerPhoneNumber = h.Reservation != null && h.Reservation.User != null ? h.Reservation.User.PhoneNumber : null,
                AttendantName = h.AttendantCheckIn != null ? h.AttendantCheckIn.FullName : null
            })
            .ToListAsync(cancellationToken);

        var recentActivityList = recentVehicleEntries.Select(e => new ReservationSummaryDto
        {
            Id = e.ReservationId,
            ReservationNumber = e.ReservationNumber,
            CustomerName = e.CustomerName,
            ParkingAreaName = e.ParkingAreaName,
            SlotNumber = e.SlotNumber,
            LicensePlate = e.LicensePlate,
            StartTime = e.CheckInTime,
            EndTime = e.CheckOutTime ?? e.CheckInTime.AddHours(2),
            TotalPrice = 0m,
            Status = e.IsCurrentlyParked ? ReservationStatus.Active : ReservationStatus.Completed,
            CreatedAt = e.CheckInTime
        }).ToList();

        _logger.LogInformation("Attendant dashboard retrieved for User {UserId} (Facility: {Facility}, Occupied: {Occupied}/{Total})",
            currentUserId, assignedAreaName, occupiedSlots, totalSlots);

        return new AttendantDashboardDto
        {
            AssignedParkingAreaId = targetAreaId,
            AssignedParkingAreaName = assignedAreaName,
            FacilityAddress = facilityAddress,
            TodaysCheckIns = todaysCheckIns,
            TodaysCheckOuts = todaysCheckOuts,
            TotalSlots = totalSlots,
            AvailableSlots = availableSlots,
            CurrentOccupiedSlots = occupiedSlots,
            ReservedSlots = reservedSlots,
            MaintenanceSlots = maintenanceSlots,
            OccupancyRatePercentage = occupancyRate,
            ActiveReservations = activeReservations,
            ConfirmedArrivalsToday = confirmedArrivalsToday,
            PendingArrivalsToday = pendingArrivalsToday,
            RecentVehicleEntries = recentVehicleEntries,
            RecentActivity = recentActivityList
        };
    }

    public async Task<CustomerDashboardDto> GetCustomerDashboardAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Verify customer exists
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == customerId && !u.IsDeleted, cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID '{customerId}' was not found.");
        }

        // 1. Fetch user's reservations (eager-loaded for DTO projection)
        var allReservations = await _context.Reservations
            .AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.ParkingSlot)
                .ThenInclude(s => s!.ParkingArea)
            .Include(r => r.Vehicle)
            .Include(r => r.QRCode)
            .Include(r => r.Payment)
            .Where(r => r.UserId == customerId && !r.IsDeleted)
            .ToListAsync(cancellationToken);

        // Counts
        var totalReservations = allReservations.Count;
        var completedReservations = allReservations.Count(r => r.Status == ReservationStatus.Completed);
        var activeReservationsCount = allReservations.Count(r => r.Status == ReservationStatus.Active);
        var upcomingReservationsCount = allReservations.Count(r =>
            (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Pending) && r.EndTime >= now);
        var cancelledReservationsCount = allReservations.Count(r => r.Status == ReservationStatus.Cancelled);
        var pastReservationsCount = allReservations.Count(r =>
            r.Status == ReservationStatus.Completed || r.EndTime < now);

        // Active Reservation (status == Active, newest first)
        var activeReservationEntity = allReservations
            .Where(r => r.Status == ReservationStatus.Active)
            .OrderByDescending(r => r.StartTime)
            .FirstOrDefault();
        var activeReservationDto = activeReservationEntity != null ? MapReservationToDto(activeReservationEntity) : null;

        // Upcoming Reservation ((Confirmed or Pending) and EndTime >= now, sorted by StartTime ASC)
        var upcomingReservationEntity = allReservations
            .Where(r => (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Pending) && r.EndTime >= now)
            .OrderBy(r => r.StartTime)
            .FirstOrDefault();
        var upcomingReservationDto = upcomingReservationEntity != null ? MapReservationToDto(upcomingReservationEntity) : null;

        // Recent Reservations (top 5 by CreatedAt DESC)
        var recentReservations = allReservations
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Select(r => new ReservationSummaryDto
            {
                Id = r.Id,
                ReservationNumber = r.ReservationNumber,
                CustomerName = r.User != null ? r.User.FullName : user.FullName,
                ParkingAreaName = r.ParkingSlot?.ParkingArea?.Name ?? "Unknown Area",
                SlotNumber = r.ParkingSlot?.SlotNumber ?? "Unknown Slot",
                LicensePlate = r.Vehicle?.LicensePlate ?? "Unknown",
                StartTime = r.StartTime,
                EndTime = r.EndTime,
                TotalPrice = r.TotalPrice,
                Status = r.Status,
                CreatedAt = r.CreatedAt
            })
            .ToList();

        // 2. Fetch Customer's Registered Vehicles
        var vehicles = await _context.Vehicles
            .AsNoTracking()
            .Include(v => v.User)
            .Where(v => v.UserId == customerId && !v.IsDeleted)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);

        var registeredVehicles = vehicles.Select(v => new VehicleDto
        {
            Id = v.Id,
            LicensePlate = v.LicensePlate,
            VehicleType = v.VehicleType,
            Make = v.Make,
            Model = v.Model,
            Color = v.Color,
            UserId = v.UserId,
            OwnerFullName = v.User?.FullName ?? user.FullName,
            CreatedAt = v.CreatedAt,
            UpdatedAt = v.UpdatedAt
        }).ToList();

        // 3. Fetch Financial Totals (Payments and Overstay Fees)
        var paidPayments = await _context.Payments
            .AsNoTracking()
            .Where(p => p.UserId == customerId && !p.IsDeleted && p.Status == PaymentStatus.Paid)
            .Select(p => p.Amount)
            .ToListAsync(cancellationToken);

        var totalPaidPayments = paidPayments.Sum();
        var totalPaymentsCount = paidPayments.Count;

        var overstayFees = await _context.ParkingHistories
            .AsNoTracking()
            .Where(h => !h.IsDeleted && h.Reservation != null && h.Reservation.UserId == customerId && h.OverstayFee.HasValue)
            .Select(h => h.OverstayFee ?? 0m)
            .ToListAsync(cancellationToken);

        var totalOverstay = overstayFees.Sum();
        var totalPayments = totalPaidPayments + totalOverstay;

        // 4. Fetch Recent Parking History (top 5 by CheckInTime DESC)
        var recentHistories = await _context.ParkingHistories
            .AsNoTracking()
            .Include(h => h.Reservation)
                .ThenInclude(r => r!.User)
            .Include(h => h.ParkingSlot)
                .ThenInclude(s => s!.ParkingArea)
            .Include(h => h.Vehicle)
            .Include(h => h.AttendantCheckIn)
            .Include(h => h.AttendantCheckOut)
            .Where(h => !h.IsDeleted && h.Reservation != null && h.Reservation.UserId == customerId)
            .OrderByDescending(h => h.CheckInTime)
            .Take(5)
            .ToListAsync(cancellationToken);

        var recentParkingHistory = recentHistories.Select(MapParkingHistoryToDto).ToList();

        _logger.LogInformation("Customer dashboard retrieved for user {UserId}. Total reservations: {Total}, Registered vehicles: {VehiclesCount}, Total payments: {Payments}",
            customerId, totalReservations, registeredVehicles.Count, totalPayments);

        return new CustomerDashboardDto
        {
            UpcomingReservation = upcomingReservationDto,
            ActiveReservation = activeReservationDto,
            TotalReservations = totalReservations,
            CompletedReservations = completedReservations,
            ActiveReservationsCount = activeReservationsCount,
            UpcomingReservationsCount = upcomingReservationsCount,
            CancelledReservationsCount = cancelledReservationsCount,
            PastReservationsCount = pastReservationsCount,
            TotalPayments = totalPayments,
            TotalPaymentsCount = totalPaymentsCount,
            RegisteredVehicles = registeredVehicles,
            RecentParkingHistory = recentParkingHistory,
            RecentReservations = recentReservations
        };
    }

    private static ReservationDto MapReservationToDto(Reservation r)
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

    private static ParkingHistoryDto MapParkingHistoryToDto(ParkingHistory h)
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
            ReservationStatus = h.Reservation?.Status ?? ReservationStatus.Completed,
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
            VehicleType = h.Vehicle?.VehicleType ?? VehicleType.Car,
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

    private static string FormatHourLabel(int hour)
    {
        if (hour == 0) return "12 AM";
        if (hour < 12) return $"{hour} AM";
        if (hour == 12) return "12 PM";
        return $"{hour - 12} PM";
    }
}
