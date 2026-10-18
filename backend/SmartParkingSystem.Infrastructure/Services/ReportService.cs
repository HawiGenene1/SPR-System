using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartParkingSystem.Application.Common.Security;
using SmartParkingSystem.Application.DTOs.Common;
using SmartParkingSystem.Application.DTOs.Reports;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Enums;
using SmartParkingSystem.Infrastructure.Data;

namespace SmartParkingSystem.Infrastructure.Services;

/// <summary>
/// High-performance reporting and analytics engine for executive dashboards, Angular charts, and tables.
/// Supports date range presets (daily, weekly, monthly, custom), continuous time-series zero-filling,
/// and server-side pagination.
/// </summary>
public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        ApplicationDbContext context,
        ILogger<ReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region 1. Revenue Report

    public async Task<RevenueReportDto> GetRevenueReportAsync(
        ReportFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var (startDate, endDate, period, grouping) = ResolveDateRange(filter);
        var pageNumber = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        _logger.LogInformation("Generating Revenue Report for {Period} ({Start:u} to {End:u}), Area: {AreaId}",
            period, startDate, endDate, filter.ParkingAreaId);

        // 1. Query paid payments within the window
        var paymentsQuery = _context.Payments
            .AsNoTracking()
            .Include(p => p.Reservation)
                .ThenInclude(r => r!.ParkingSlot)
                    .ThenInclude(s => s!.ParkingArea)
            .Include(p => p.User)
            .Where(p => !p.IsDeleted && p.Status == PaymentStatus.Paid);

        // Filter by date (PaidAt or CreatedAt)
        paymentsQuery = paymentsQuery.Where(p => (p.PaidAt ?? p.CreatedAt) >= startDate && (p.PaidAt ?? p.CreatedAt) <= endDate);

        if (filter.ParkingAreaId.HasValue)
        {
            paymentsQuery = paymentsQuery.Where(p => p.Reservation != null &&
                                                     p.Reservation.ParkingSlot != null &&
                                                     p.Reservation.ParkingSlot.ParkingAreaId == filter.ParkingAreaId.Value);
        }

        // Total payments count and sum
        var allPayments = await paymentsQuery
            .Select(p => new
            {
                p.Id,
                p.ReservationId,
                ReservationNumber = p.Reservation != null ? p.Reservation.ReservationNumber : "Unknown",
                CustomerName = p.User != null ? p.User.FullName : "Unknown",
                CustomerEmail = p.User != null ? p.User.Email : "Unknown",
                ParkingAreaId = (p.Reservation != null && p.Reservation.ParkingSlot != null) ? p.Reservation.ParkingSlot.ParkingAreaId : Guid.Empty,
                ParkingAreaName = (p.Reservation != null && p.Reservation.ParkingSlot != null && p.Reservation.ParkingSlot.ParkingArea != null) ? p.Reservation.ParkingSlot.ParkingArea.Name : "General",
                p.Amount,
                p.PaymentMethod,
                p.Status,
                p.TransactionId,
                PaidAt = p.PaidAt ?? p.CreatedAt
            })
            .ToListAsync(cancellationToken);

        // 2. Query Overstay Fees from ParkingHistory
        var overstayQuery = _context.ParkingHistories
            .AsNoTracking()
            .Include(h => h.ParkingSlot)
                .ThenInclude(s => s!.ParkingArea)
            .Where(h => !h.IsDeleted && h.OverstayFee.HasValue && h.OverstayFee.Value > 0 && h.CheckOutTime.HasValue);

        overstayQuery = overstayQuery.Where(h => h.CheckOutTime >= startDate && h.CheckOutTime <= endDate);

        if (filter.ParkingAreaId.HasValue)
        {
            overstayQuery = overstayQuery.Where(h => h.ParkingSlot != null && h.ParkingSlot.ParkingAreaId == filter.ParkingAreaId.Value);
        }

        var allOverstays = await overstayQuery
            .Select(h => new
            {
                h.Id,
                ParkingAreaId = h.ParkingSlot != null ? h.ParkingSlot.ParkingAreaId : Guid.Empty,
                ParkingAreaName = h.ParkingSlot != null && h.ParkingSlot.ParkingArea != null ? h.ParkingSlot.ParkingArea.Name : "General",
                Fee = h.OverstayFee!.Value,
                CheckOutTime = h.CheckOutTime!.Value
            })
            .ToListAsync(cancellationToken);

        var paidPaymentsRevenue = allPayments.Sum(p => p.Amount);
        var overstayFeesRevenue = allOverstays.Sum(h => h.Fee);
        var totalRevenue = paidPaymentsRevenue + overstayFeesRevenue;
        var totalTransactions = allPayments.Count + allOverstays.Count;
        var avgTransactionValue = totalTransactions > 0 ? Math.Round(totalRevenue / totalTransactions, 2) : 0m;

        // Breakdown by Payment Method
        var revenueByPaymentMethod = allPayments
            .GroupBy(p => p.PaymentMethod.ToString())
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        if (allOverstays.Count > 0)
        {
            if (revenueByPaymentMethod.ContainsKey("OverstayAdjustment"))
            {
                revenueByPaymentMethod["OverstayAdjustment"] += overstayFeesRevenue;
            }
            else
            {
                revenueByPaymentMethod["OverstayAdjustment"] = overstayFeesRevenue;
            }
        }

        // Breakdown by Parking Area
        var areaRevenueMap = new Dictionary<Guid, (string Name, decimal Revenue, int Count)>();
        foreach (var p in allPayments)
        {
            if (!areaRevenueMap.ContainsKey(p.ParkingAreaId))
            {
                areaRevenueMap[p.ParkingAreaId] = (p.ParkingAreaName, 0m, 0);
            }
            var current = areaRevenueMap[p.ParkingAreaId];
            areaRevenueMap[p.ParkingAreaId] = (current.Name, current.Revenue + p.Amount, current.Count + 1);
        }
        foreach (var h in allOverstays)
        {
            if (!areaRevenueMap.ContainsKey(h.ParkingAreaId))
            {
                areaRevenueMap[h.ParkingAreaId] = (h.ParkingAreaName, 0m, 0);
            }
            var current = areaRevenueMap[h.ParkingAreaId];
            areaRevenueMap[h.ParkingAreaId] = (current.Name, current.Revenue + h.Fee, current.Count + 1);
        }

        var revenueByAreaList = areaRevenueMap.Select(kv => new ParkingAreaRevenueDto
        {
            ParkingAreaId = kv.Key,
            ParkingAreaName = kv.Value.Name,
            Revenue = kv.Value.Revenue,
            TotalTransactions = kv.Value.Count,
            Percentage = totalRevenue > 0 ? Math.Round((double)kv.Value.Revenue / (double)totalRevenue * 100, 2) : 0
        }).OrderByDescending(x => x.Revenue).ToList();

        // Time Series (continuous buckets)
        var timeSeriesBuckets = BuildDateBuckets(startDate, endDate, grouping);
        foreach (var p in allPayments)
        {
            var bucket = FindMatchingBucket(timeSeriesBuckets, p.PaidAt, grouping);
            if (bucket != null)
            {
                bucket.Revenue += p.Amount;
                bucket.TransactionsCount++;
            }
        }
        foreach (var h in allOverstays)
        {
            var bucket = FindMatchingBucket(timeSeriesBuckets, h.CheckOutTime, grouping);
            if (bucket != null)
            {
                bucket.Revenue += h.Fee;
                bucket.TransactionsCount++;
            }
        }

        var revenueByPeriod = timeSeriesBuckets.Select(b => new DailyRevenueDto
        {
            Date = b.Date,
            PeriodLabel = b.PeriodLabel,
            Revenue = b.Revenue,
            TransactionsCount = b.TransactionsCount
        }).ToList();

        // Paginated Transactions for Table
        var totalRecords = allPayments.Count;
        var pagedItems = allPayments
            .OrderByDescending(p => p.PaidAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new RevenueTransactionDto
            {
                PaymentId = p.Id,
                ReservationId = p.ReservationId,
                ReservationNumber = p.ReservationNumber,
                CustomerName = p.CustomerName,
                CustomerEmail = p.CustomerEmail,
                ParkingAreaName = p.ParkingAreaName,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                Status = p.Status,
                TransactionId = p.TransactionId,
                PaidAt = p.PaidAt
            })
            .ToList();

        var paginatedTransactions = new PagedResultDto<RevenueTransactionDto>(pagedItems, totalRecords, pageNumber, pageSize);

        return new RevenueReportDto
        {
            StartDate = startDate,
            EndDate = endDate,
            Period = period,
            TotalRevenue = totalRevenue,
            PaidPaymentsRevenue = paidPaymentsRevenue,
            OverstayFeesRevenue = overstayFeesRevenue,
            TotalTransactions = totalTransactions,
            AverageTransactionValue = avgTransactionValue,
            RevenueByPaymentMethod = revenueByPaymentMethod,
            RevenueByParkingArea = revenueByAreaList,
            RevenueByPeriod = revenueByPeriod,
            Transactions = paginatedTransactions
        };
    }

    #endregion

    #region 2. Reservations Report

    public async Task<ReservationReportDto> GetReservationReportAsync(
        ReportFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var (startDate, endDate, period, grouping) = ResolveDateRange(filter);
        var pageNumber = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        _logger.LogInformation("Generating Reservation Report for {Period} ({Start:u} to {End:u}), Area: {AreaId}",
            period, startDate, endDate, filter.ParkingAreaId);

        var query = _context.Reservations
            .AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.ParkingSlot)
                .ThenInclude(s => s!.ParkingArea)
            .Include(r => r.Vehicle)
            .Where(r => !r.IsDeleted && r.CreatedAt >= startDate && r.CreatedAt <= endDate);

        if (filter.ParkingAreaId.HasValue)
        {
            query = query.Where(r => r.ParkingSlot != null && r.ParkingSlot.ParkingAreaId == filter.ParkingAreaId.Value);
        }

        var allReservations = await query
            .Select(r => new
            {
                r.Id,
                r.ReservationNumber,
                CustomerName = r.User != null ? r.User.FullName : "Unknown",
                ParkingAreaId = r.ParkingSlot != null ? r.ParkingSlot.ParkingAreaId : Guid.Empty,
                ParkingAreaName = (r.ParkingSlot != null && r.ParkingSlot.ParkingArea != null) ? r.ParkingSlot.ParkingArea.Name : "General",
                SlotNumber = r.ParkingSlot != null ? r.ParkingSlot.SlotNumber : "N/A",
                LicensePlate = r.Vehicle != null ? r.Vehicle.LicensePlate : "N/A",
                r.StartTime,
                r.EndTime,
                r.TotalPrice,
                r.Status,
                r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var total = allReservations.Count;
        var completed = allReservations.Count(r => r.Status == ReservationStatus.Completed);
        var cancelled = allReservations.Count(r => r.Status == ReservationStatus.Cancelled);
        var active = allReservations.Count(r => r.Status == ReservationStatus.Active);
        var pending = allReservations.Count(r => r.Status == ReservationStatus.Pending);
        var expired = allReservations.Count(r => r.Status == ReservationStatus.Expired);
        var totalRevenue = allReservations.Where(r => r.Status == ReservationStatus.Completed || r.Status == ReservationStatus.Active).Sum(r => r.TotalPrice);

        var cancellationRate = total > 0 ? Math.Round((double)cancelled / total * 100, 2) : 0;
        var completionRate = total > 0 ? Math.Round((double)completed / total * 100, 2) : 0;
        var avgDuration = total > 0 ? Math.Round(allReservations.Average(r => Math.Max(0, (r.EndTime - r.StartTime).TotalMinutes)), 1) : 0;

        // Breakdown by status
        var statusBreakdown = allReservations
            .GroupBy(r => r.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // Breakdown by area
        var areaBreakdown = allReservations
            .GroupBy(r => new { r.ParkingAreaId, r.ParkingAreaName })
            .Select(g => new ParkingAreaReservationStatDto
            {
                ParkingAreaId = g.Key.ParkingAreaId,
                ParkingAreaName = g.Key.ParkingAreaName,
                TotalReservations = g.Count(),
                TotalRevenue = g.Sum(x => x.TotalPrice),
                Percentage = total > 0 ? Math.Round((double)g.Count() / total * 100, 2) : 0
            })
            .OrderByDescending(x => x.TotalReservations)
            .ToList();

        // Time Series (continuous buckets)
        var timeSeriesBuckets = BuildDateBuckets(startDate, endDate, grouping);
        var reservationPeriodBuckets = timeSeriesBuckets.Select(b => new PeriodReservationDto
        {
            Date = b.Date,
            PeriodLabel = b.PeriodLabel,
            TotalReservations = 0,
            CompletedCount = 0,
            CancelledCount = 0,
            ActiveCount = 0,
            Revenue = 0m
        }).ToList();

        foreach (var r in allReservations)
        {
            var match = FindMatchingReservationBucket(reservationPeriodBuckets, r.CreatedAt, grouping);
            if (match != null)
            {
                match.TotalReservations++;
                if (r.Status == ReservationStatus.Completed) match.CompletedCount++;
                if (r.Status == ReservationStatus.Cancelled) match.CancelledCount++;
                if (r.Status == ReservationStatus.Active) match.ActiveCount++;
                match.Revenue += r.TotalPrice;
            }
        }

        // Paginated items table
        var paginatedReservations = allReservations
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReservationReportItemDto
            {
                Id = r.Id,
                ReservationNumber = r.ReservationNumber,
                CustomerName = r.CustomerName,
                ParkingAreaName = r.ParkingAreaName,
                SlotNumber = r.SlotNumber,
                LicensePlate = r.LicensePlate,
                StartTime = r.StartTime,
                EndTime = r.EndTime,
                DurationMinutes = (int)Math.Max(0, (r.EndTime - r.StartTime).TotalMinutes),
                TotalPrice = r.TotalPrice,
                Status = r.Status,
                CreatedAt = r.CreatedAt
            })
            .ToList();

        var pagedResult = new PagedResultDto<ReservationReportItemDto>(paginatedReservations, total, pageNumber, pageSize);

        return new ReservationReportDto
        {
            StartDate = startDate,
            EndDate = endDate,
            Period = period,
            TotalReservations = total,
            CompletedReservations = completed,
            CancelledReservations = cancelled,
            ActiveReservations = active,
            PendingReservations = pending,
            ExpiredReservations = expired,
            CancellationRatePercentage = cancellationRate,
            CompletionRatePercentage = completionRate,
            AverageDurationMinutes = avgDuration,
            TotalRevenue = totalRevenue,
            ReservationsByStatus = statusBreakdown,
            ReservationsByArea = areaBreakdown,
            ReservationsByPeriod = reservationPeriodBuckets,
            Reservations = pagedResult
        };
    }

    #endregion

    #region 3. Occupancy Report

    public async Task<OccupancyReportDto> GetOccupancyReportAsync(
        ReportFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var (startDate, endDate, period, grouping) = ResolveDateRange(filter);
        var pageNumber = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        _logger.LogInformation("Generating Occupancy Report for {Period} ({Start:u} to {End:u}), Area: {AreaId}",
            period, startDate, endDate, filter.ParkingAreaId);

        // 1. Facilities and Slots
        var areasQuery = _context.ParkingAreas
            .AsNoTracking()
            .Include(a => a.ParkingSlots)
            .Where(a => !a.IsDeleted);

        if (filter.ParkingAreaId.HasValue)
        {
            areasQuery = areasQuery.Where(a => a.Id == filter.ParkingAreaId.Value);
        }

        var areas = await areasQuery.ToListAsync(cancellationToken);

        string? singleAreaName = null;
        if (filter.ParkingAreaId.HasValue && areas.Count > 0)
        {
            singleAreaName = areas[0].Name;
        }

        var allSlots = areas.SelectMany(a => a.ParkingSlots.Where(s => !s.IsDeleted)).ToList();
        var totalSlots = allSlots.Count;
        var occupiedSlots = allSlots.Count(s => s.Status == SlotStatus.Occupied);
        var availableSlots = allSlots.Count(s => s.Status == SlotStatus.Available);
        var reservedSlots = allSlots.Count(s => s.Status == SlotStatus.Reserved);
        var maintenanceSlots = allSlots.Count(s => s.Status == SlotStatus.Maintenance);
        var currentOccupancyRate = totalSlots > 0 ? Math.Round((double)occupiedSlots / totalSlots * 100, 2) : 0;

        // 2. Historical check-ins from ParkingHistory within period
        var historyQuery = _context.ParkingHistories
            .AsNoTracking()
            .Where(h => !h.IsDeleted && h.CheckInTime >= startDate && h.CheckInTime <= endDate);

        if (filter.ParkingAreaId.HasValue)
        {
            historyQuery = historyQuery.Where(h => h.ParkingSlot != null && h.ParkingSlot.ParkingAreaId == filter.ParkingAreaId.Value);
        }

        var histories = await historyQuery
            .Select(h => new
            {
                h.Id,
                h.CheckInTime,
                ParkingAreaId = h.ParkingSlot != null ? h.ParkingSlot.ParkingAreaId : Guid.Empty
            })
            .ToListAsync(cancellationToken);

        var totalDays = Math.Max(1, (endDate.Date - startDate.Date).Days + 1);
        var averageTurnoverRate = totalSlots > 0 ? Math.Round((double)histories.Count / (totalSlots * totalDays), 2) : 0;

        // Slot Type Breakdown
        var slotTypeBreakdown = allSlots
            .GroupBy(s => s.SlotType)
            .Select(g => new SlotTypeOccupancyDto
            {
                SlotType = g.Key,
                TotalSlots = g.Count(),
                OccupiedSlots = g.Count(s => s.Status == SlotStatus.Occupied),
                OccupancyRatePercentage = g.Any() ? Math.Round((double)g.Count(s => s.Status == SlotStatus.Occupied) / g.Count() * 100, 2) : 0
            })
            .OrderBy(x => x.SlotType)
            .ToList();

        // 24 Hour Distribution
        var hourlyGroupCounts = histories
            .GroupBy(h => h.CheckInTime.Hour)
            .ToDictionary(g => g.Key, g => g.Count());

        var hourlyOccupancyList = new List<HourlyOccupancyDto>(24);
        var maxHourlyCount = hourlyGroupCounts.Values.DefaultIfEmpty(0).Max();

        for (int h = 0; h < 24; h++)
        {
            var count = hourlyGroupCounts.TryGetValue(h, out var c) ? c : 0;
            var avgRate = totalSlots > 0 ? Math.Round((double)count / (totalSlots * totalDays) * 100, 2) : 0;

            hourlyOccupancyList.Add(new HourlyOccupancyDto
            {
                Hour = h,
                HourLabel = FormatHour(h),
                AverageOccupancyRate = avgRate,
                CheckInCount = count,
                IsPeakHour = maxHourlyCount > 0 && count == maxHourlyCount
            });
        }

        // Time Series (continuous buckets)
        var timeSeriesBuckets = BuildDateBuckets(startDate, endDate, grouping);
        var occupancyPeriodBuckets = timeSeriesBuckets.Select(b => new PeriodOccupancyDto
        {
            Date = b.Date,
            PeriodLabel = b.PeriodLabel,
            OccupancyRatePercentage = 0,
            PeakOccupiedSlots = 0,
            TotalCheckIns = 0
        }).ToList();

        foreach (var h in histories)
        {
            var match = FindMatchingOccupancyBucket(occupancyPeriodBuckets, h.CheckInTime, grouping);
            if (match != null)
            {
                match.TotalCheckIns++;
            }
        }

        foreach (var b in occupancyPeriodBuckets)
        {
            b.OccupancyRatePercentage = totalSlots > 0 ? Math.Round((double)b.TotalCheckIns / totalSlots * 100, 2) : 0;
        }

        // Parking Area Breakdown
        var areaOccupancyList = areas.Select(a =>
        {
            var aSlots = a.ParkingSlots.Where(s => !s.IsDeleted).ToList();
            var aTotal = aSlots.Count;
            var aOccupied = aSlots.Count(s => s.Status == SlotStatus.Occupied);
            var aAvailable = aSlots.Count(s => s.Status == SlotStatus.Available);
            var aRate = aTotal > 0 ? Math.Round((double)aOccupied / aTotal * 100, 2) : 0;

            return new ParkingAreaOccupancyDto
            {
                ParkingAreaId = a.Id,
                ParkingAreaName = a.Name,
                TotalSlots = aTotal,
                OccupiedSlots = aOccupied,
                AvailableSlots = aAvailable,
                OccupancyRatePercentage = aRate
            };
        }).OrderByDescending(x => x.OccupancyRatePercentage).ToList();

        // Paginated Facility Inventory Table
        var facilityTableRows = areas.Select(a =>
        {
            var aSlots = a.ParkingSlots.Where(s => !s.IsDeleted).ToList();
            var aTotal = aSlots.Count;
            var aOccupied = aSlots.Count(s => s.Status == SlotStatus.Occupied);
            var aReserved = aSlots.Count(s => s.Status == SlotStatus.Reserved);
            var aAvailable = aSlots.Count(s => s.Status == SlotStatus.Available);
            var aMaintenance = aSlots.Count(s => s.Status == SlotStatus.Maintenance);
            var aRate = aTotal > 0 ? Math.Round((double)aOccupied / aTotal * 100, 2) : 0;
            var checkInsInPeriod = histories.Count(h => h.ParkingAreaId == a.Id);

            return new FacilityOccupancyTableDto
            {
                ParkingAreaId = a.Id,
                ParkingAreaName = a.Name,
                Address = a.Address,
                City = a.City,
                HourlyRate = a.HourlyRate,
                TotalSlots = aTotal,
                OccupiedSlots = aOccupied,
                ReservedSlots = aReserved,
                AvailableSlots = aAvailable,
                MaintenanceSlots = aMaintenance,
                OccupancyRatePercentage = aRate,
                TotalCheckInsInPeriod = checkInsInPeriod,
                IsActive = a.IsActive
            };
        }).ToList();

        var pagedFacilities = new PagedResultDto<FacilityOccupancyTableDto>(
            facilityTableRows.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList(),
            facilityTableRows.Count,
            pageNumber,
            pageSize);

        return new OccupancyReportDto
        {
            GeneratedAt = DateTime.UtcNow,
            StartDate = startDate,
            EndDate = endDate,
            Period = period,
            ParkingAreaId = filter.ParkingAreaId,
            ParkingAreaName = singleAreaName,
            TotalSlots = totalSlots,
            OccupiedSlots = occupiedSlots,
            AvailableSlots = availableSlots,
            ReservedSlots = reservedSlots,
            MaintenanceSlots = maintenanceSlots,
            OccupancyRatePercentage = currentOccupancyRate,
            AverageTurnoverRate = averageTurnoverRate,
            OccupancyBySlotType = slotTypeBreakdown,
            HourlyOccupancy = hourlyOccupancyList,
            OccupancyByPeriod = occupancyPeriodBuckets,
            OccupancyByParkingArea = areaOccupancyList,
            Facilities = pagedFacilities
        };
    }

    #endregion

    #region 4. Customers Report

    public async Task<CustomerReportDto> GetCustomerReportAsync(
        ReportFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var (startDate, endDate, period, grouping) = ResolveDateRange(filter);
        var pageNumber = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        _logger.LogInformation("Generating Customer Analytics Report for {Period} ({Start:u} to {End:u}), Search: '{Search}'",
            period, startDate, endDate, filter.Search);

        // Fetch all non-deleted customers
        var customerQuery = _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.Vehicles)
            .Include(u => u.Reservations)
            .Include(u => u.Payments)
            .Where(u => !u.IsDeleted && u.Role != null && u.Role.Name == AppRoles.Customer);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            customerQuery = customerQuery.Where(u =>
                u.FullName.ToLower().Contains(search) ||
                u.Email.ToLower().Contains(search) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));
        }

        var allCustomers = await customerQuery.ToListAsync(cancellationToken);

        var totalCustomers = allCustomers.Count;
        var newCustomersInPeriod = allCustomers.Count(c => c.CreatedAt >= startDate && c.CreatedAt <= endDate);

        // Active customers: users who created reservations in the period
        var activeCustomersInPeriod = allCustomers.Count(c =>
            c.Reservations.Any(r => !r.IsDeleted && r.CreatedAt >= startDate && r.CreatedAt <= endDate));

        // Customer activity list with aggregations
        var customerActivityList = allCustomers.Select(c =>
        {
            var nonDeletedReservations = c.Reservations.Where(r => !r.IsDeleted).ToList();
            var completedCount = nonDeletedReservations.Count(r => r.Status == ReservationStatus.Completed);
            var cancelledCount = nonDeletedReservations.Count(r => r.Status == ReservationStatus.Cancelled);
            var paidPayments = c.Payments.Where(p => !p.IsDeleted && p.Status == PaymentStatus.Paid).Sum(p => p.Amount);
            var vehicleCount = c.Vehicles.Count(v => !v.IsDeleted);
            var lastRes = nonDeletedReservations.OrderByDescending(r => r.CreatedAt).FirstOrDefault()?.CreatedAt;

            string tier = nonDeletedReservations.Count switch
            {
                >= 10 => "Frequent",
                >= 4 => "Regular",
                >= 1 => "Occasional",
                _ => "Inactive"
            };

            return new CustomerActivityDto
            {
                CustomerId = c.Id,
                FullName = c.FullName,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                RegisteredAt = c.CreatedAt,
                TotalReservations = nonDeletedReservations.Count,
                CompletedReservations = completedCount,
                CancelledReservations = cancelledCount,
                TotalSpend = paidPayments,
                RegisteredVehiclesCount = vehicleCount,
                LastReservationDate = lastRes,
                ActivityTier = tier
            };
        }).OrderByDescending(x => x.TotalSpend).ToList();

        var totalCustomerSpend = customerActivityList.Sum(c => c.TotalSpend);
        var totalCustomerReservations = customerActivityList.Sum(c => c.TotalReservations);
        var avgSpend = totalCustomers > 0 ? Math.Round(totalCustomerSpend / totalCustomers, 2) : 0m;

        // Activity Tier Breakdown
        var activityTiers = new Dictionary<string, int>
        {
            ["Frequent"] = customerActivityList.Count(c => c.ActivityTier == "Frequent"),
            ["Regular"] = customerActivityList.Count(c => c.ActivityTier == "Regular"),
            ["Occasional"] = customerActivityList.Count(c => c.ActivityTier == "Occasional"),
            ["Inactive"] = customerActivityList.Count(c => c.ActivityTier == "Inactive")
        };

        // Time Series (continuous buckets for registrations)
        var timeSeriesBuckets = BuildDateBuckets(startDate, endDate, grouping);
        var registrationPeriodBuckets = timeSeriesBuckets.Select(b => new PeriodCustomerDto
        {
            Date = b.Date,
            PeriodLabel = b.PeriodLabel,
            NewRegistrationsCount = 0
        }).ToList();

        foreach (var c in allCustomers.Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate))
        {
            var match = FindMatchingCustomerBucket(registrationPeriodBuckets, c.CreatedAt, grouping);
            if (match != null)
            {
                match.NewRegistrationsCount++;
            }
        }

        // Paginated Table
        var paginatedCustomers = new PagedResultDto<CustomerActivityDto>(
            customerActivityList.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList(),
            customerActivityList.Count,
            pageNumber,
            pageSize);

        return new CustomerReportDto
        {
            StartDate = startDate,
            EndDate = endDate,
            Period = period,
            TotalCustomers = totalCustomers,
            NewCustomersInPeriod = newCustomersInPeriod,
            ActiveCustomersInPeriod = activeCustomersInPeriod,
            TotalCustomerSpend = totalCustomerSpend,
            AverageSpendPerCustomer = avgSpend,
            TotalCustomerReservations = totalCustomerReservations,
            RegistrationsByPeriod = registrationPeriodBuckets,
            CustomerActivityTiers = activityTiers,
            Customers = paginatedCustomers
        };
    }

    #endregion

    #region Helper Methods (Date Resolving, Formatting & Bucketing)

    public static (DateTime StartDate, DateTime EndDate, string Period, string Grouping) ResolveDateRange(ReportFilterRequest filter)
    {
        var now = DateTime.UtcNow;
        var period = filter.Period?.Trim().ToLowerInvariant();

        if (filter.StartDate.HasValue && filter.EndDate.HasValue)
        {
            var start = DateTime.SpecifyKind(filter.StartDate.Value, DateTimeKind.Utc);
            var end = DateTime.SpecifyKind(filter.EndDate.Value, DateTimeKind.Utc);

            if (start > end)
            {
                throw new ArgumentException("StartDate cannot be greater than EndDate.");
            }

            var days = (end.Date - start.Date).TotalDays;
            string grouping = period switch
            {
                "hourly" or "hour" => "hourly",
                "daily" or "day" => "daily",
                "weekly" or "week" => "weekly",
                "monthly" or "month" => "monthly",
                _ => days <= 2 ? "hourly" : days <= 45 ? "daily" : days <= 180 ? "weekly" : "monthly"
            };

            return (start, end, "custom", grouping);
        }

        switch (period)
        {
            case "daily":
            case "today":
            {
                var start = now.Date;
                var end = start.AddDays(1).AddTicks(-1);
                return (start, end, "daily", "hourly");
            }
            case "weekly":
            case "week":
            {
                // Last 7 days
                var start = now.Date.AddDays(-6);
                var end = now.Date.AddDays(1).AddTicks(-1);
                return (start, end, "weekly", "daily");
            }
            case "monthly":
            case "month":
            {
                // Last 30 days
                var start = now.Date.AddDays(-29);
                var end = now.Date.AddDays(1).AddTicks(-1);
                return (start, end, "monthly", "daily");
            }
            default:
            {
                // Default: last 30 days
                var start = now.Date.AddDays(-29);
                var end = now.Date.AddDays(1).AddTicks(-1);
                return (start, end, "custom", "daily");
            }
        }
    }

    private class RawBucket
    {
        public DateTime Date { get; set; }
        public string PeriodLabel { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int TransactionsCount { get; set; }
    }

    private static List<RawBucket> BuildDateBuckets(DateTime start, DateTime end, string grouping)
    {
        var buckets = new List<RawBucket>();

        if (grouping == "hourly")
        {
            var current = new DateTime(start.Year, start.Month, start.Day, 0, 0, 0, DateTimeKind.Utc);
            for (int h = 0; h < 24; h++)
            {
                var hourDt = current.AddHours(h);
                buckets.Add(new RawBucket
                {
                    Date = hourDt,
                    PeriodLabel = FormatHour(h)
                });
            }
        }
        else if (grouping == "weekly")
        {
            var cur = start.Date;
            while (cur <= end.Date)
            {
                var weekEnd = cur.AddDays(6);
                buckets.Add(new RawBucket
                {
                    Date = cur,
                    PeriodLabel = $"{cur:MMM dd} - {weekEnd:MMM dd}"
                });
                cur = cur.AddDays(7);
            }
        }
        else if (grouping == "monthly")
        {
            var cur = new DateTime(start.Year, start.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endMonth = new DateTime(end.Year, end.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            while (cur <= endMonth)
            {
                buckets.Add(new RawBucket
                {
                    Date = cur,
                    PeriodLabel = cur.ToString("MMM yyyy")
                });
                cur = cur.AddMonths(1);
            }
        }
        else // daily
        {
            var cur = start.Date;
            while (cur <= end.Date)
            {
                buckets.Add(new RawBucket
                {
                    Date = cur,
                    PeriodLabel = cur.ToString("MMM dd")
                });
                cur = cur.AddDays(1);
            }
        }

        return buckets;
    }

    private static RawBucket? FindMatchingBucket(List<RawBucket> buckets, DateTime timestamp, string grouping)
    {
        if (grouping == "hourly")
        {
            return buckets.FirstOrDefault(b => b.Date.Date == timestamp.Date && b.Date.Hour == timestamp.Hour);
        }
        if (grouping == "weekly")
        {
            return buckets.FirstOrDefault(b => timestamp.Date >= b.Date && timestamp.Date < b.Date.AddDays(7));
        }
        if (grouping == "monthly")
        {
            return buckets.FirstOrDefault(b => b.Date.Year == timestamp.Year && b.Date.Month == timestamp.Month);
        }
        return buckets.FirstOrDefault(b => b.Date.Date == timestamp.Date);
    }

    private static PeriodReservationDto? FindMatchingReservationBucket(List<PeriodReservationDto> buckets, DateTime timestamp, string grouping)
    {
        if (grouping == "hourly")
        {
            return buckets.FirstOrDefault(b => b.Date.Date == timestamp.Date && b.Date.Hour == timestamp.Hour);
        }
        if (grouping == "weekly")
        {
            return buckets.FirstOrDefault(b => timestamp.Date >= b.Date && timestamp.Date < b.Date.AddDays(7));
        }
        if (grouping == "monthly")
        {
            return buckets.FirstOrDefault(b => b.Date.Year == timestamp.Year && b.Date.Month == timestamp.Month);
        }
        return buckets.FirstOrDefault(b => b.Date.Date == timestamp.Date);
    }

    private static PeriodOccupancyDto? FindMatchingOccupancyBucket(List<PeriodOccupancyDto> buckets, DateTime timestamp, string grouping)
    {
        if (grouping == "hourly")
        {
            return buckets.FirstOrDefault(b => b.Date.Date == timestamp.Date && b.Date.Hour == timestamp.Hour);
        }
        if (grouping == "weekly")
        {
            return buckets.FirstOrDefault(b => timestamp.Date >= b.Date && timestamp.Date < b.Date.AddDays(7));
        }
        if (grouping == "monthly")
        {
            return buckets.FirstOrDefault(b => b.Date.Year == timestamp.Year && b.Date.Month == timestamp.Month);
        }
        return buckets.FirstOrDefault(b => b.Date.Date == timestamp.Date);
    }

    private static PeriodCustomerDto? FindMatchingCustomerBucket(List<PeriodCustomerDto> buckets, DateTime timestamp, string grouping)
    {
        if (grouping == "hourly")
        {
            return buckets.FirstOrDefault(b => b.Date.Date == timestamp.Date && b.Date.Hour == timestamp.Hour);
        }
        if (grouping == "weekly")
        {
            return buckets.FirstOrDefault(b => timestamp.Date >= b.Date && timestamp.Date < b.Date.AddDays(7));
        }
        if (grouping == "monthly")
        {
            return buckets.FirstOrDefault(b => b.Date.Year == timestamp.Year && b.Date.Month == timestamp.Month);
        }
        return buckets.FirstOrDefault(b => b.Date.Date == timestamp.Date);
    }

    private static string FormatHour(int hour)
    {
        if (hour == 0) return "12 AM";
        if (hour < 12) return $"{hour} AM";
        if (hour == 12) return "12 PM";
        return $"{hour - 12} PM";
    }

    #endregion
}
