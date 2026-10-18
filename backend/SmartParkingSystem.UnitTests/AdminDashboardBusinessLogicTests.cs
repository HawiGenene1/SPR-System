using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SmartParkingSystem.Application.Common.Security;
using SmartParkingSystem.Application.DTOs.Dashboards;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;
using SmartParkingSystem.Infrastructure.Data;
using SmartParkingSystem.Infrastructure.Services;

namespace SmartParkingSystem.UnitTests;

public static class AdminDashboardBusinessLogicTests
{
    private static int _passed = 0;
    private static int _failed = 0;

    public static int Run()
    {
        _passed = 0;
        _failed = 0;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        Console.WriteLine(" Running Step 17: Admin Dashboard Business Logic Tests    ");
        Console.WriteLine("=========================================================");
        Console.ResetColor();

        TestUserBreakdownMetrics();
        TestParkingAreaAndSlotMetrics();
        TestReservationStatusAndTodayMetrics();
        TestRevenueAggregations();
        TestRevenueByDayChartContinuity();
        TestReservationsByDayChart();
        TestOccupancyStatisticsBreakdown();
        TestPeakParkingHoursDistribution();
        TestRecentReservationsOrdering();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        if (_failed == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" Step 17 Admin Dashboard Tests: {_passed} Passed, {_failed} Failed");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" Step 17 Admin Dashboard Tests: {_passed} Passed, {_failed} Failed");
        }
        Console.ResetColor();
        Console.WriteLine("=========================================================");

        return _failed == 0 ? 0 : 1;
    }

    private static void Assert(bool condition, string testName)
    {
        if (condition)
        {
            _passed++;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  [PASS] {testName}");
            Console.ResetColor();
        }
        else
        {
            _failed++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [FAIL] {testName}");
            Console.ResetColor();
        }
    }

    private static (ApplicationDbContext context, IDashboardService service) CreateMockContextAndService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        var service = new DashboardService(context, NullLogger<DashboardService>.Instance);

        return (context, service);
    }

    private static void TestUserBreakdownMetrics()
    {
        Console.WriteLine("\n[1] User Breakdown Metrics Tests:");
        var (context, service) = CreateMockContextAndService();

        var custRole = new Role { Id = Guid.NewGuid(), Name = AppRoles.Customer };
        var staffRole = new Role { Id = Guid.NewGuid(), Name = AppRoles.ParkingAttendant };
        var adminRole = new Role { Id = Guid.NewGuid(), Name = AppRoles.Admin };
        context.Roles.AddRange(custRole, staffRole, adminRole);

        // 3 Customers, 2 Attendants, 1 Admin
        for (int i = 0; i < 3; i++)
        {
            context.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                FullName = $"Customer {i}",
                Email = $"cust{i}@test.com",
                PhoneNumber = "+12345",
                PasswordHash = "hash",
                RoleId = custRole.Id,
                Role = custRole
            });
        }
        for (int i = 0; i < 2; i++)
        {
            context.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                FullName = $"Attendant {i}",
                Email = $"attendant{i}@test.com",
                PhoneNumber = "+12345",
                PasswordHash = "hash",
                RoleId = staffRole.Id,
                Role = staffRole
            });
        }
        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            FullName = "Administrator",
            Email = "admin@test.com",
            PhoneNumber = "+12345",
            PasswordHash = "hash",
            RoleId = adminRole.Id,
            Role = adminRole
        });

        // 1 Soft-deleted customer (should be excluded)
        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            FullName = "Deleted User",
            Email = "deleted@test.com",
            PhoneNumber = "+12345",
            PasswordHash = "hash",
            RoleId = custRole.Id,
            Role = custRole,
            IsDeleted = true
        });

        context.SaveChanges();

        var dashboard = service.GetAdminDashboardAsync().GetAwaiter().GetResult();

        Assert(dashboard.TotalUsers == 6, "TotalUsers correctly counts 6 non-deleted users");
        Assert(dashboard.TotalCustomers == 3, "TotalCustomers correctly counts 3 active customers");
        Assert(dashboard.TotalAttendants == 2, "TotalAttendants correctly counts 2 parking attendants");
    }

    private static void TestParkingAreaAndSlotMetrics()
    {
        Console.WriteLine("\n[2] Parking Area & Slot Inventory Tests:");
        var (context, service) = CreateMockContextAndService();

        var area1 = new ParkingArea
        {
            Id = Guid.NewGuid(),
            Name = "North Lot",
            Address = "100 North Rd",
            City = "Metropolis",
            HourlyRate = 5.0m,
            TotalSlots = 10,
            IsActive = true
        };
        var area2 = new ParkingArea
        {
            Id = Guid.NewGuid(),
            Name = "South Deck",
            Address = "200 South Rd",
            City = "Metropolis",
            HourlyRate = 6.0m,
            TotalSlots = 5,
            IsActive = true
        };
        var inactiveArea = new ParkingArea
        {
            Id = Guid.NewGuid(),
            Name = "Old Garage",
            Address = "Old Way",
            City = "Metropolis",
            HourlyRate = 2.0m,
            TotalSlots = 2,
            IsActive = false
        };
        context.ParkingAreas.AddRange(area1, area2, inactiveArea);

        // Add slots with varying statuses:
        // 3 Available, 2 Reserved, 2 Occupied, 1 Maintenance
        context.ParkingSlots.AddRange(
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area1.Id, SlotNumber = "A1", FloorNumber = 1, Status = SlotStatus.Available },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area1.Id, SlotNumber = "A2", FloorNumber = 1, Status = SlotStatus.Available },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area1.Id, SlotNumber = "A3", FloorNumber = 1, Status = SlotStatus.Available },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area1.Id, SlotNumber = "B1", FloorNumber = 1, Status = SlotStatus.Reserved },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area1.Id, SlotNumber = "B2", FloorNumber = 1, Status = SlotStatus.Reserved },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area2.Id, SlotNumber = "C1", FloorNumber = 1, Status = SlotStatus.Occupied },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area2.Id, SlotNumber = "C2", FloorNumber = 1, Status = SlotStatus.Occupied },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area2.Id, SlotNumber = "D1", FloorNumber = 1, Status = SlotStatus.Maintenance }
        );

        context.SaveChanges();

        var dashboard = service.GetAdminDashboardAsync().GetAwaiter().GetResult();

        Assert(dashboard.TotalParkingAreas == 2, "TotalParkingAreas counts 2 active facilities");
        Assert(dashboard.TotalParkingSlots == 8, "TotalParkingSlots accurately reflects 8 total slots");
        Assert(dashboard.TotalSlots == 8, "TotalSlots alias matches TotalParkingSlots");
        Assert(dashboard.AvailableSlots == 3, "AvailableSlots count is 3");
        Assert(dashboard.ReservedSlots == 2, "ReservedSlots count is 2");
        Assert(dashboard.OccupiedSlots == 2, "OccupiedSlots count is 2");
        Assert(dashboard.MaintenanceSlots == 1, "MaintenanceSlots count is 1");
        Assert(dashboard.OccupancyRatePercentage == 25.0, "OccupancyRatePercentage is 25.0% (2 occupied / 8 total)");
    }

    private static void TestReservationStatusAndTodayMetrics()
    {
        Console.WriteLine("\n[3] Reservation Status & Today Metrics Tests:");
        var (context, service) = CreateMockContextAndService();
        var now = DateTime.UtcNow;

        var user = new User { Id = Guid.NewGuid(), FullName = "John Doe", Email = "j@d.com", PhoneNumber = "1", PasswordHash = "h" };
        var area = new ParkingArea { Id = Guid.NewGuid(), Name = "Central Parking", Address = "Main St", City = "Metro", HourlyRate = 5, TotalSlots = 10, IsActive = true };
        var slot = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "A1", FloorNumber = 1, Status = SlotStatus.Available };
        var vehicle = new Vehicle { Id = Guid.NewGuid(), UserId = user.Id, LicensePlate = "ABC-123", Make = "Toyota", Model = "Corolla", VehicleType = VehicleType.Car };

        context.Users.Add(user);
        context.ParkingAreas.Add(area);
        context.ParkingSlots.Add(slot);
        context.Vehicles.Add(vehicle);

        // 1 Active reservation today
        context.Reservations.Add(new Reservation
        {
            Id = Guid.NewGuid(),
            ReservationNumber = "RES-ACT-1",
            UserId = user.Id,
            VehicleId = vehicle.Id,
            ParkingSlotId = slot.Id,
            StartTime = now.Date.AddHours(2),
            EndTime = now.Date.AddHours(4),
            TotalPrice = 10.0m,
            Status = ReservationStatus.Active,
            CreatedAt = now.Date.AddHours(1)
        });

        // 2 Completed reservations (1 today, 1 yesterday)
        context.Reservations.Add(new Reservation
        {
            Id = Guid.NewGuid(),
            ReservationNumber = "RES-CMP-1",
            UserId = user.Id,
            VehicleId = vehicle.Id,
            ParkingSlotId = slot.Id,
            StartTime = now.Date.AddHours(5),
            EndTime = now.Date.AddHours(7),
            TotalPrice = 10.0m,
            Status = ReservationStatus.Completed,
            CreatedAt = now.Date.AddHours(4)
        });
        context.Reservations.Add(new Reservation
        {
            Id = Guid.NewGuid(),
            ReservationNumber = "RES-CMP-OLD",
            UserId = user.Id,
            VehicleId = vehicle.Id,
            ParkingSlotId = slot.Id,
            StartTime = now.Date.AddDays(-2),
            EndTime = now.Date.AddDays(-2).AddHours(2),
            TotalPrice = 10.0m,
            Status = ReservationStatus.Completed,
            CreatedAt = now.Date.AddDays(-2)
        });

        // 1 Cancelled reservation
        context.Reservations.Add(new Reservation
        {
            Id = Guid.NewGuid(),
            ReservationNumber = "RES-CNC-1",
            UserId = user.Id,
            VehicleId = vehicle.Id,
            ParkingSlotId = slot.Id,
            StartTime = now.Date.AddDays(1),
            EndTime = now.Date.AddDays(1).AddHours(2),
            TotalPrice = 10.0m,
            Status = ReservationStatus.Cancelled,
            CreatedAt = now.Date.AddHours(6)
        });

        context.SaveChanges();

        var dashboard = service.GetAdminDashboardAsync().GetAwaiter().GetResult();

        Assert(dashboard.ActiveReservations == 1, "ActiveReservations count is 1");
        Assert(dashboard.ActiveReservationsCount == 1, "ActiveReservationsCount alias matches 1");
        Assert(dashboard.CompletedReservations == 2, "CompletedReservations count is 2");
        Assert(dashboard.CompletedReservationsCount == 2, "CompletedReservationsCount alias matches 2");
        Assert(dashboard.CancelledReservations == 1, "CancelledReservations count is 1");
        Assert(dashboard.CancelledReservationsCount == 1, "CancelledReservationsCount alias matches 1");
        Assert(dashboard.TodaysReservations == 3, "TodaysReservations counts 3 reservations created or scheduled today");
        Assert(dashboard.TodayReservations == 3, "TodayReservations alias matches 3");
    }

    private static void TestRevenueAggregations()
    {
        Console.WriteLine("\n[4] Revenue Aggregations Tests (Today, Monthly, Total):");
        var (context, service) = CreateMockContextAndService();
        var now = DateTime.UtcNow;
        var today = now.Date;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var user = new User { Id = Guid.NewGuid(), FullName = "U", Email = "u@u.com", PhoneNumber = "1", PasswordHash = "h" };
        var area = new ParkingArea { Id = Guid.NewGuid(), Name = "A", Address = "A", City = "C", HourlyRate = 5, TotalSlots = 10, IsActive = true };
        var slot = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "S1", FloorNumber = 1, Status = SlotStatus.Available };
        var vehicle = new Vehicle { Id = Guid.NewGuid(), UserId = user.Id, LicensePlate = "ABC-123", Make = "T", Model = "C", VehicleType = VehicleType.Car };
        var res1 = new Reservation { Id = Guid.NewGuid(), ReservationNumber = "R1", UserId = user.Id, VehicleId = vehicle.Id, ParkingSlotId = slot.Id, StartTime = now, EndTime = now.AddHours(2), TotalPrice = 25, Status = ReservationStatus.Completed };
        var res2 = new Reservation { Id = Guid.NewGuid(), ReservationNumber = "R2", UserId = user.Id, VehicleId = vehicle.Id, ParkingSlotId = slot.Id, StartTime = now, EndTime = now.AddHours(2), TotalPrice = 50, Status = ReservationStatus.Completed };
        var res3 = new Reservation { Id = Guid.NewGuid(), ReservationNumber = "R3", UserId = user.Id, VehicleId = vehicle.Id, ParkingSlotId = slot.Id, StartTime = now, EndTime = now.AddHours(2), TotalPrice = 100, Status = ReservationStatus.Completed };
        var res4 = new Reservation { Id = Guid.NewGuid(), ReservationNumber = "R4", UserId = user.Id, VehicleId = vehicle.Id, ParkingSlotId = slot.Id, StartTime = now, EndTime = now.AddHours(2), TotalPrice = 999, Status = ReservationStatus.Cancelled };
        var res5 = new Reservation { Id = Guid.NewGuid(), ReservationNumber = "R5", UserId = user.Id, VehicleId = vehicle.Id, ParkingSlotId = slot.Id, StartTime = now, EndTime = now.AddHours(2), TotalPrice = 10, Status = ReservationStatus.Completed };

        context.Users.Add(user);
        context.ParkingAreas.Add(area);
        context.ParkingSlots.Add(slot);
        context.Vehicles.Add(vehicle);
        context.Reservations.AddRange(res1, res2, res3, res4, res5);

        // Payments:
        // Today paid: $25.00
        context.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            ReservationId = res1.Id,
            UserId = user.Id,
            Amount = 25.00m,
            Status = PaymentStatus.Paid,
            TransactionId = "TXN-TODAY",
            CreatedAt = today.AddHours(2),
            PaidAt = today.AddHours(2)
        });

        // Current month paid (earlier this month if possible, or today + 4h within month):
        var earlierInMonth = currentMonthStart.AddHours(1);
        var dateForMonthPayment = earlierInMonth < today ? earlierInMonth : today.AddHours(4);
        context.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            ReservationId = res2.Id,
            UserId = user.Id,
            Amount = 50.00m,
            Status = PaymentStatus.Paid,
            TransactionId = "TXN-MONTH",
            CreatedAt = dateForMonthPayment,
            PaidAt = dateForMonthPayment
        });

        // Previous month paid: $100.00
        var prevMonthDate = currentMonthStart.AddMonths(-1).AddDays(5);
        context.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            ReservationId = res3.Id,
            UserId = user.Id,
            Amount = 100.00m,
            Status = PaymentStatus.Paid,
            TransactionId = "TXN-OLD",
            CreatedAt = prevMonthDate,
            PaidAt = prevMonthDate
        });

        // Failed payment (should NOT be included in revenue)
        context.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            ReservationId = res4.Id,
            UserId = user.Id,
            Amount = 999.00m,
            Status = PaymentStatus.Failed,
            TransactionId = "TXN-FAIL",
            CreatedAt = today.AddHours(1)
        });

        // Overstay fee today: $5.00
        context.ParkingHistories.Add(new ParkingHistory
        {
            Id = Guid.NewGuid(),
            ReservationId = res5.Id,
            VehicleId = vehicle.Id,
            ParkingSlotId = slot.Id,
            CheckInTime = today.AddHours(1),
            CheckOutTime = today.AddHours(3),
            OverstayDurationMinutes = 30,
            OverstayFee = 5.00m,
            CreatedAt = today
        });

        context.SaveChanges();

        var dashboard = service.GetAdminDashboardAsync().GetAwaiter().GetResult();

        Assert(dashboard.TodaysRevenue >= 30.00m, $"TodaysRevenue includes paid today + overstay ($30.00): {dashboard.TodaysRevenue}");
        Assert(dashboard.TodayRevenue == dashboard.TodaysRevenue, "TodayRevenue alias matches TodaysRevenue");
        Assert(dashboard.MonthlyRevenue >= dashboard.TodaysRevenue, $"MonthlyRevenue ({dashboard.MonthlyRevenue}) is at least TodaysRevenue");
        Assert(dashboard.TotalRevenue >= 180.00m, $"TotalRevenue ({dashboard.TotalRevenue}) includes all historical paid amounts + overstay ($180.00)");
    }

    private static void TestRevenueByDayChartContinuity()
    {
        Console.WriteLine("\n[5] Revenue By Day Chart Continuity Tests:");
        var (context, service) = CreateMockContextAndService();
        var now = DateTime.UtcNow;
        var today = now.Date;

        var user = new User { Id = Guid.NewGuid(), FullName = "U", Email = "u@u.com", PhoneNumber = "1", PasswordHash = "h" };
        var area = new ParkingArea { Id = Guid.NewGuid(), Name = "A", Address = "A", City = "C", HourlyRate = 5, TotalSlots = 10, IsActive = true };
        var slot = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "S1", FloorNumber = 1, Status = SlotStatus.Available };
        var vehicle = new Vehicle { Id = Guid.NewGuid(), UserId = user.Id, LicensePlate = "ABC-123", Make = "T", Model = "C", VehicleType = VehicleType.Car };
        var res = new Reservation { Id = Guid.NewGuid(), ReservationNumber = "R1", UserId = user.Id, VehicleId = vehicle.Id, ParkingSlotId = slot.Id, StartTime = now, EndTime = now.AddHours(2), TotalPrice = 10, Status = ReservationStatus.Completed };

        context.Users.Add(user);
        context.ParkingAreas.Add(area);
        context.ParkingSlots.Add(slot);
        context.Vehicles.Add(vehicle);
        context.Reservations.Add(res);

        // 1 Payment 3 days ago of $40.00
        var threeDaysAgo = today.AddDays(-3).AddHours(10);
        context.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            ReservationId = res.Id,
            UserId = user.Id,
            Amount = 40.00m,
            Status = PaymentStatus.Paid,
            TransactionId = "TXN-CHART-1",
            CreatedAt = threeDaysAgo,
            PaidAt = threeDaysAgo
        });

        context.SaveChanges();

        var dashboard = service.GetAdminDashboardAsync().GetAwaiter().GetResult();

        Assert(dashboard.RevenueByDay.Count == 14, "RevenueByDay contains exactly 14 contiguous daily data points");
        Assert(!string.IsNullOrWhiteSpace(dashboard.RevenueByDay[0].DateLabel), "DateLabel is formatted properly");

        var dayMinus3 = dashboard.RevenueByDay.FirstOrDefault(d => d.Date.Date == today.AddDays(-3));
        Assert(dayMinus3 != null && dayMinus3.Revenue == 40.00m, "Revenue for 3 days ago is accurately calculated as $40.00");

        var yesterday = dashboard.RevenueByDay.FirstOrDefault(d => d.Date.Date == today.AddDays(-1));
        Assert(yesterday != null && yesterday.Revenue == 0.00m, "Zero-revenue day is properly preserved as 0.00m without gaps");
    }

    private static void TestReservationsByDayChart()
    {
        Console.WriteLine("\n[6] Reservations By Day Chart Tests:");
        var (context, service) = CreateMockContextAndService();
        var now = DateTime.UtcNow;
        var today = now.Date;

        var user = new User { Id = Guid.NewGuid(), FullName = "U", Email = "u@u.com", PhoneNumber = "1", PasswordHash = "h" };
        var area = new ParkingArea { Id = Guid.NewGuid(), Name = "A", Address = "A", City = "C", HourlyRate = 5, TotalSlots = 10, IsActive = true };
        var slot = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "S1", FloorNumber = 1, Status = SlotStatus.Available };
        var vehicle = new Vehicle { Id = Guid.NewGuid(), UserId = user.Id, LicensePlate = "ABC-123", Make = "T", Model = "C", VehicleType = VehicleType.Car };

        context.Users.Add(user);
        context.ParkingAreas.Add(area);
        context.ParkingSlots.Add(slot);
        context.Vehicles.Add(vehicle);

        // 2 Reservations 2 days ago (1 completed, 1 cancelled)
        context.Reservations.Add(new Reservation
        {
            Id = Guid.NewGuid(),
            ReservationNumber = "R-2A",
            UserId = user.Id,
            VehicleId = vehicle.Id,
            ParkingSlotId = slot.Id,
            StartTime = today.AddDays(-2).AddHours(8),
            EndTime = today.AddDays(-2).AddHours(10),
            TotalPrice = 10,
            Status = ReservationStatus.Completed,
            CreatedAt = today.AddDays(-2)
        });
        context.Reservations.Add(new Reservation
        {
            Id = Guid.NewGuid(),
            ReservationNumber = "R-2B",
            UserId = user.Id,
            VehicleId = vehicle.Id,
            ParkingSlotId = slot.Id,
            StartTime = today.AddDays(-2).AddHours(12),
            EndTime = today.AddDays(-2).AddHours(14),
            TotalPrice = 10,
            Status = ReservationStatus.Cancelled,
            CreatedAt = today.AddDays(-2)
        });

        context.SaveChanges();

        var dashboard = service.GetAdminDashboardAsync().GetAwaiter().GetResult();

        Assert(dashboard.ReservationsByDay.Count == 14, "ReservationsByDay contains exactly 14 continuous days");
        var dayMinus2 = dashboard.ReservationsByDay.FirstOrDefault(d => d.Date.Date == today.AddDays(-2));
        Assert(dayMinus2 != null && dayMinus2.TotalReservations == 2, "Day minus 2 total reservations count is 2");
        Assert(dayMinus2 != null && dayMinus2.CompletedCount == 1, "Day minus 2 completed count is 1");
        Assert(dayMinus2 != null && dayMinus2.CancelledCount == 1, "Day minus 2 cancelled count is 1");
    }

    private static void TestOccupancyStatisticsBreakdown()
    {
        Console.WriteLine("\n[7] Occupancy Statistics Breakdown Tests:");
        var (context, service) = CreateMockContextAndService();

        var area1 = new ParkingArea { Id = Guid.NewGuid(), Name = "Terminal 1", Address = "Airport Rd", City = "Metro", HourlyRate = 8, TotalSlots = 4, IsActive = true };
        var area2 = new ParkingArea { Id = Guid.NewGuid(), Name = "Terminal 2", Address = "Airport Rd", City = "Metro", HourlyRate = 6, TotalSlots = 2, IsActive = true };
        context.ParkingAreas.AddRange(area1, area2);

        // Terminal 1: 2 Occupied, 1 Reserved, 1 Available (all Standard)
        // Terminal 2: 1 Occupied (EV), 1 Available (EV)
        context.ParkingSlots.AddRange(
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area1.Id, SlotNumber = "T1-1", SlotType = SlotType.Standard, Status = SlotStatus.Occupied },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area1.Id, SlotNumber = "T1-2", SlotType = SlotType.Standard, Status = SlotStatus.Occupied },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area1.Id, SlotNumber = "T1-3", SlotType = SlotType.Standard, Status = SlotStatus.Reserved },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area1.Id, SlotNumber = "T1-4", SlotType = SlotType.Standard, Status = SlotStatus.Available },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area2.Id, SlotNumber = "T2-1", SlotType = SlotType.EVCharging, Status = SlotStatus.Occupied },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area2.Id, SlotNumber = "T2-2", SlotType = SlotType.EVCharging, Status = SlotStatus.Available }
        );

        context.SaveChanges();

        var dashboard = service.GetAdminDashboardAsync().GetAwaiter().GetResult();
        var occ = dashboard.OccupancyStatistics;

        Assert(occ != null, "OccupancyStatistics object is populated");
        Assert(occ!.TotalSlots == 6, "OccupancyStatistics TotalSlots is 6");
        Assert(occ.OccupiedSlots == 3, "OccupancyStatistics OccupiedSlots is 3");
        Assert(occ.OverallOccupancyRatePercentage == 50.0, "OverallOccupancyRatePercentage is 50.0%");

        Assert(occ.ByParkingArea.Count == 2, "ByParkingArea contains 2 facilities");
        var t1 = occ.ByParkingArea.FirstOrDefault(a => a.ParkingAreaId == area1.Id);
        Assert(t1 != null && t1.OccupancyRatePercentage == 50.0, "Terminal 1 has 50.0% occupancy");

        var standardSlots = occ.BySlotType.FirstOrDefault(s => s.SlotType == SlotType.Standard);
        Assert(standardSlots != null && standardSlots.TotalSlots == 4 && standardSlots.OccupiedSlots == 2, "Slot type Standard has 4 total, 2 occupied");
    }

    private static void TestPeakParkingHoursDistribution()
    {
        Console.WriteLine("\n[8] Peak Parking Hours Distribution Tests:");
        var (context, service) = CreateMockContextAndService();
        var baseDate = DateTime.UtcNow.Date.AddDays(-2);

        var user = new User { Id = Guid.NewGuid(), FullName = "U", Email = "u@u.com", PhoneNumber = "1", PasswordHash = "h" };
        var area = new ParkingArea { Id = Guid.NewGuid(), Name = "A", Address = "A", City = "C", HourlyRate = 5, TotalSlots = 10, IsActive = true };
        var slot = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "S1", FloorNumber = 1, Status = SlotStatus.Available };
        var vehicle = new Vehicle { Id = Guid.NewGuid(), UserId = user.Id, LicensePlate = "ABC-123", Make = "T", Model = "C", VehicleType = VehicleType.Car };

        context.Users.Add(user);
        context.ParkingAreas.Add(area);
        context.ParkingSlots.Add(slot);
        context.Vehicles.Add(vehicle);

        // Add 5 reservations starting at 9 AM, 3 starting at 2 PM (14:00), 1 starting at 8 PM (20:00)
        for (int i = 0; i < 5; i++)
        {
            context.Reservations.Add(new Reservation
            {
                Id = Guid.NewGuid(),
                ReservationNumber = $"R-9AM-{i}",
                UserId = user.Id,
                VehicleId = vehicle.Id,
                ParkingSlotId = slot.Id,
                StartTime = baseDate.AddHours(9),
                EndTime = baseDate.AddHours(11),
                TotalPrice = 10,
                Status = ReservationStatus.Completed,
                CreatedAt = baseDate
            });
        }
        for (int i = 0; i < 3; i++)
        {
            context.Reservations.Add(new Reservation
            {
                Id = Guid.NewGuid(),
                ReservationNumber = $"R-2PM-{i}",
                UserId = user.Id,
                VehicleId = vehicle.Id,
                ParkingSlotId = slot.Id,
                StartTime = baseDate.AddHours(14),
                EndTime = baseDate.AddHours(16),
                TotalPrice = 10,
                Status = ReservationStatus.Completed,
                CreatedAt = baseDate
            });
        }
        context.Reservations.Add(new Reservation
        {
            Id = Guid.NewGuid(),
            ReservationNumber = "R-8PM-0",
            UserId = user.Id,
            VehicleId = vehicle.Id,
            ParkingSlotId = slot.Id,
            StartTime = baseDate.AddHours(20),
            EndTime = baseDate.AddHours(22),
            TotalPrice = 10,
            Status = ReservationStatus.Completed,
            CreatedAt = baseDate
        });

        context.SaveChanges();

        var dashboard = service.GetAdminDashboardAsync().GetAwaiter().GetResult();
        var hours = dashboard.PeakParkingHours;

        Assert(hours.Count == 24, "PeakParkingHours contains all 24 hour buckets (0 to 23)");
        var nineAm = hours.FirstOrDefault(h => h.Hour == 9);
        Assert(nineAm != null && nineAm.ReservationCount == 5, "9 AM bucket has 5 reservations");
        Assert(nineAm != null && nineAm.HourLabel == "9 AM", "Hour label formatted as '9 AM'");
        Assert(nineAm != null && nineAm.IsPeakHour, "9 AM is flagged as a peak hour");

        var twoPm = hours.FirstOrDefault(h => h.Hour == 14);
        Assert(twoPm != null && twoPm.HourLabel == "2 PM", "14:00 label formatted as '2 PM'");
        Assert(twoPm != null && twoPm.IsPeakHour, "2 PM is flagged as a peak hour");

        var midnight = hours.FirstOrDefault(h => h.Hour == 0);
        Assert(midnight != null && midnight.HourLabel == "12 AM", "Hour 0 label formatted as '12 AM'");
    }

    private static void TestRecentReservationsOrdering()
    {
        Console.WriteLine("\n[9] Recent Reservations Ordering Tests:");
        var (context, service) = CreateMockContextAndService();
        var now = DateTime.UtcNow;

        var user = new User { Id = Guid.NewGuid(), FullName = "Customer Jane", Email = "jane@u.com", PhoneNumber = "1", PasswordHash = "h" };
        var area = new ParkingArea { Id = Guid.NewGuid(), Name = "Downtown Hub", Address = "Hub Way", City = "C", HourlyRate = 5, TotalSlots = 10, IsActive = true };
        var slot = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "HUB-1", FloorNumber = 1, Status = SlotStatus.Available };
        var vehicle = new Vehicle { Id = Guid.NewGuid(), UserId = user.Id, LicensePlate = "JANE-777", Make = "Honda", Model = "Civic", VehicleType = VehicleType.Car };

        context.Users.Add(user);
        context.ParkingAreas.Add(area);
        context.ParkingSlots.Add(slot);
        context.Vehicles.Add(vehicle);

        for (int i = 1; i <= 8; i++)
        {
            context.Reservations.Add(new Reservation
            {
                Id = Guid.NewGuid(),
                ReservationNumber = $"REC-{i}",
                UserId = user.Id,
                VehicleId = vehicle.Id,
                ParkingSlotId = slot.Id,
                StartTime = now.AddDays(i),
                EndTime = now.AddDays(i).AddHours(2),
                TotalPrice = 10,
                Status = ReservationStatus.Confirmed,
                CreatedAt = now.AddMinutes(i * 10)
            });
        }

        context.SaveChanges();

        var dashboard = service.GetAdminDashboardAsync().GetAwaiter().GetResult();

        Assert(dashboard.RecentReservations.Count == 5, "RecentReservations capped at the 5 most recent records");
        Assert(dashboard.RecentReservations[0].ReservationNumber == "REC-8", "Most recent reservation REC-8 is first");
        Assert(dashboard.RecentReservations[0].CustomerName == "Customer Jane", "CustomerName is mapped correctly");
        Assert(dashboard.RecentReservations[0].LicensePlate == "JANE-777", "LicensePlate is mapped correctly");
    }
}
