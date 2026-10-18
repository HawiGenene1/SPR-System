using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SmartParkingSystem.Application.Common.Security;
using SmartParkingSystem.Application.DTOs.Reports;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;
using SmartParkingSystem.Infrastructure.Data;
using SmartParkingSystem.Infrastructure.Services;

namespace SmartParkingSystem.UnitTests;

public static class ReportingBusinessLogicTests
{
    private static int _passed = 0;
    private static int _failed = 0;

    public static int Run()
    {
        _passed = 0;
        _failed = 0;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        Console.WriteLine(" Running Step 20: Reporting Analytics Unit Tests         ");
        Console.WriteLine("=========================================================");
        Console.ResetColor();

        TestDateRangeResolvingAndPresets();
        TestInvalidDateRangeValidation();
        TestRevenueReportMetricsAndBreakdown();
        TestRevenueReportFacilityFiltering();
        TestRevenueReportPagination();
        TestReservationReportMetricsAndBreakdown();
        TestOccupancyReportSlotTypeAndHourlyDistribution();
        TestCustomerReportMetricsAndActivityTiers();
        TestCustomerReportSearchAndPagination();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        if (_failed == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" Step 20 Reporting Tests: {_passed} Passed, {_failed} Failed");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" Step 20 Reporting Tests: {_passed} Passed, {_failed} Failed");
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

    private static (ApplicationDbContext context, IReportService service) CreateMockContextAndService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        var service = new ReportService(context, NullLogger<ReportService>.Instance);

        return (context, service);
    }

    private static void TestDateRangeResolvingAndPresets()
    {
        Console.WriteLine("\n[1] Date Range Resolving & Presets Tests:");

        // Daily
        var (startD, endD, periodD, groupD) = ReportService.ResolveDateRange(new ReportFilterRequest { Period = "daily" });
        Assert(periodD == "daily", "Preset 'daily' resolves period as 'daily'");
        Assert(groupD == "hourly", "Preset 'daily' resolves grouping as 'hourly'");
        Assert(startD.Date == DateTime.UtcNow.Date, "Preset 'daily' starts today at 00:00:00 UTC");

        // Weekly
        var (startW, endW, periodW, groupW) = ReportService.ResolveDateRange(new ReportFilterRequest { Period = "weekly" });
        Assert(periodW == "weekly", "Preset 'weekly' resolves period as 'weekly'");
        Assert(groupW == "daily", "Preset 'weekly' resolves grouping as 'daily'");
        Assert((endW.Date - startW.Date).Days == 6, "Preset 'weekly' spans 7 days (6 day diff)");

        // Monthly
        var (startM, endM, periodM, groupM) = ReportService.ResolveDateRange(new ReportFilterRequest { Period = "monthly" });
        Assert(periodM == "monthly", "Preset 'monthly' resolves period as 'monthly'");
        Assert(groupM == "daily", "Preset 'monthly' resolves grouping as 'daily'");
        Assert((endM.Date - startM.Date).Days == 29, "Preset 'monthly' spans 30 days");

        // Custom range with explicit dates
        var customStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var customEnd = new DateTime(2026, 1, 15, 23, 59, 59, DateTimeKind.Utc);
        var (startC, endC, periodC, groupC) = ReportService.ResolveDateRange(new ReportFilterRequest
        {
            Period = "custom",
            StartDate = customStart,
            EndDate = customEnd
        });
        Assert(periodC == "custom", "Explicit range resolves period as 'custom'");
        Assert(startC == customStart, "StartDate matches custom start");
        Assert(endC == customEnd, "EndDate matches custom end");
        Assert(groupC == "daily", "15 days range defaults to daily grouping");
    }

    private static void TestInvalidDateRangeValidation()
    {
        Console.WriteLine("\n[2] Invalid Date Range Validation Tests:");
        var (_, service) = CreateMockContextAndService();

        var invalidFilter = new ReportFilterRequest
        {
            StartDate = DateTime.UtcNow.AddDays(5),
            EndDate = DateTime.UtcNow.AddDays(1)
        };

        bool threwArgumentException = false;
        try
        {
            service.GetRevenueReportAsync(invalidFilter).GetAwaiter().GetResult();
        }
        catch (ArgumentException)
        {
            threwArgumentException = true;
        }

        Assert(threwArgumentException, "GetRevenueReportAsync throws ArgumentException when StartDate > EndDate");
    }

    private static void TestRevenueReportMetricsAndBreakdown()
    {
        Console.WriteLine("\n[3] Revenue Report Metrics & Breakdown Tests:");
        var (context, service) = CreateMockContextAndService();

        var area1 = new ParkingArea { Id = Guid.NewGuid(), Name = "Downtown Hub", Address = "1 Main", City = "Metro", HourlyRate = 5, TotalSlots = 10, IsActive = true };
        context.ParkingAreas.Add(area1);

        var slot1 = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area1.Id, SlotNumber = "D-1", Status = SlotStatus.Occupied };
        context.ParkingSlots.Add(slot1);

        var customer = new User { Id = Guid.NewGuid(), FullName = "Carol Customer", Email = "carol@example.com", PhoneNumber = "1", PasswordHash = "h" };
        context.Users.Add(customer);

        var now = DateTime.UtcNow;
        var res1 = new Reservation { Id = Guid.NewGuid(), ReservationNumber = "RES-REV-1", UserId = customer.Id, ParkingSlotId = slot1.Id, TotalPrice = 30m, Status = ReservationStatus.Completed, StartTime = now.AddDays(-2), EndTime = now.AddDays(-2).AddHours(2) };
        var res2 = new Reservation { Id = Guid.NewGuid(), ReservationNumber = "RES-REV-2", UserId = customer.Id, ParkingSlotId = slot1.Id, TotalPrice = 20m, Status = ReservationStatus.Completed, StartTime = now.AddDays(-1), EndTime = now.AddDays(-1).AddHours(2) };
        var res3 = new Reservation { Id = Guid.NewGuid(), ReservationNumber = "RES-REV-3", UserId = customer.Id, ParkingSlotId = slot1.Id, TotalPrice = 50m, Status = ReservationStatus.Pending, StartTime = now, EndTime = now.AddHours(2) };
        context.Reservations.AddRange(res1, res2, res3);

        // Payments: 2 Paid ($30 CreditCard, $20 MobileMoney), 1 Pending ($50 - should be excluded)
        context.Payments.AddRange(
            new Payment { Id = Guid.NewGuid(), ReservationId = res1.Id, UserId = customer.Id, Amount = 30m, PaymentMethod = PaymentMethod.CreditCard, Status = PaymentStatus.Paid, PaidAt = now.AddDays(-2) },
            new Payment { Id = Guid.NewGuid(), ReservationId = res2.Id, UserId = customer.Id, Amount = 20m, PaymentMethod = PaymentMethod.MobileMoney, Status = PaymentStatus.Paid, PaidAt = now.AddDays(-1) },
            new Payment { Id = Guid.NewGuid(), ReservationId = res3.Id, UserId = customer.Id, Amount = 50m, PaymentMethod = PaymentMethod.CreditCard, Status = PaymentStatus.Pending, PaidAt = now }
        );

        // Overstay fee recorded: $10
        context.ParkingHistories.Add(new ParkingHistory
        {
            Id = Guid.NewGuid(),
            ReservationId = res1.Id,
            VehicleId = Guid.NewGuid(),
            ParkingSlotId = slot1.Id,
            CheckInTime = now.AddDays(-2),
            CheckOutTime = now.AddDays(-2).AddHours(3),
            OverstayDurationMinutes = 60,
            OverstayFee = 10m
        });

        context.SaveChanges();

        var filter = new ReportFilterRequest { Period = "weekly" };
        var report = service.GetRevenueReportAsync(filter).GetAwaiter().GetResult();

        Assert(report.PaidPaymentsRevenue == 50m, "PaidPaymentsRevenue sums only paid payments ($50.00)");
        Assert(report.OverstayFeesRevenue == 10m, "OverstayFeesRevenue is $10.00");
        Assert(report.TotalRevenue == 60m, "TotalRevenue ($60.00) includes regular paid payments + overstay fees");
        Assert(report.TotalTransactions == 3, "TotalTransactions is 3 (2 paid + 1 overstay)");
        Assert(report.AverageTransactionValue == 20m, "AverageTransactionValue is $20.00 (60 / 3)");
        Assert(report.RevenueByPaymentMethod.ContainsKey("CreditCard") && report.RevenueByPaymentMethod["CreditCard"] == 30m, "CreditCard revenue is $30.00");
        Assert(report.RevenueByPaymentMethod.ContainsKey("MobileMoney") && report.RevenueByPaymentMethod["MobileMoney"] == 20m, "MobileMoney revenue is $20.00");
        Assert(report.RevenueByParkingArea.Count == 1, "RevenueByParkingArea contains Downtown Hub");
        Assert(report.RevenueByParkingArea[0].Revenue == 60m, "Downtown Hub generated total $60.00 revenue");
        Assert(report.RevenueByPeriod.Count == 7, "RevenueByPeriod has 7 continuous daily data points for weekly period");
    }

    private static void TestRevenueReportFacilityFiltering()
    {
        Console.WriteLine("\n[4] Revenue Report Facility Filtering Tests:");
        var (context, service) = CreateMockContextAndService();

        var areaA = new ParkingArea { Id = Guid.NewGuid(), Name = "Facility A", Address = "1 A St", City = "Metro", HourlyRate = 5, TotalSlots = 5, IsActive = true };
        var areaB = new ParkingArea { Id = Guid.NewGuid(), Name = "Facility B", Address = "2 B St", City = "Metro", HourlyRate = 5, TotalSlots = 5, IsActive = true };
        context.ParkingAreas.AddRange(areaA, areaB);

        var slotA = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = areaA.Id, SlotNumber = "A-1", Status = SlotStatus.Available };
        var slotB = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = areaB.Id, SlotNumber = "B-1", Status = SlotStatus.Available };
        context.ParkingSlots.AddRange(slotA, slotB);

        var user = new User { Id = Guid.NewGuid(), FullName = "Dave Buyer", Email = "dave@example.com", PhoneNumber = "1", PasswordHash = "h" };
        context.Users.Add(user);

        var resA = new Reservation { Id = Guid.NewGuid(), ReservationNumber = "RES-A", UserId = user.Id, ParkingSlotId = slotA.Id, TotalPrice = 100m, Status = ReservationStatus.Completed, StartTime = DateTime.UtcNow.AddDays(-1), EndTime = DateTime.UtcNow };
        var resB = new Reservation { Id = Guid.NewGuid(), ReservationNumber = "RES-B", UserId = user.Id, ParkingSlotId = slotB.Id, TotalPrice = 50m, Status = ReservationStatus.Completed, StartTime = DateTime.UtcNow.AddDays(-1), EndTime = DateTime.UtcNow };
        context.Reservations.AddRange(resA, resB);

        context.Payments.AddRange(
            new Payment { Id = Guid.NewGuid(), ReservationId = resA.Id, UserId = user.Id, Amount = 100m, Status = PaymentStatus.Paid, PaidAt = DateTime.UtcNow },
            new Payment { Id = Guid.NewGuid(), ReservationId = resB.Id, UserId = user.Id, Amount = 50m, Status = PaymentStatus.Paid, PaidAt = DateTime.UtcNow }
        );

        context.SaveChanges();

        // Query scoped specifically to Facility A
        var filterA = new ReportFilterRequest { ParkingAreaId = areaA.Id, Period = "monthly" };
        var reportA = service.GetRevenueReportAsync(filterA).GetAwaiter().GetResult();

        Assert(reportA.TotalRevenue == 100m, "Facility A scoped query strictly returns $100.00");
        Assert(reportA.RevenueByParkingArea.Count == 1 && reportA.RevenueByParkingArea[0].ParkingAreaName == "Facility A", "RevenueByParkingArea contains only Facility A");
    }

    private static void TestRevenueReportPagination()
    {
        Console.WriteLine("\n[5] Revenue Report Pagination Tests:");
        var (context, service) = CreateMockContextAndService();

        var area = new ParkingArea { Id = Guid.NewGuid(), Name = "Metro Garage", Address = "1 Center", City = "Metro", HourlyRate = 5, TotalSlots = 5, IsActive = true };
        var slot = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "M-01", Status = SlotStatus.Available };
        var user = new User { Id = Guid.NewGuid(), FullName = "Shopper", Email = "shopper@example.com", PhoneNumber = "1", PasswordHash = "h" };
        context.ParkingAreas.Add(area);
        context.ParkingSlots.Add(slot);
        context.Users.Add(user);

        // 5 payments
        for (int i = 1; i <= 5; i++)
        {
            var res = new Reservation { Id = Guid.NewGuid(), ReservationNumber = $"RES-PAGE-{i}", UserId = user.Id, ParkingSlotId = slot.Id, TotalPrice = 10m * i, Status = ReservationStatus.Completed, StartTime = DateTime.UtcNow.AddDays(-i), EndTime = DateTime.UtcNow.AddDays(-i).AddHours(1) };
            context.Reservations.Add(res);
            context.Payments.Add(new Payment { Id = Guid.NewGuid(), ReservationId = res.Id, UserId = user.Id, Amount = 10m * i, Status = PaymentStatus.Paid, PaidAt = DateTime.UtcNow.AddDays(-i) });
        }

        context.SaveChanges();

        // Request Page 1 with PageSize = 2
        var page1 = service.GetRevenueReportAsync(new ReportFilterRequest { Period = "monthly", PageNumber = 1, PageSize = 2 }).GetAwaiter().GetResult();
        Assert(page1.Transactions.Items.Count == 2, "Page 1 contains 2 items");
        Assert(page1.Transactions.TotalCount == 5, "TotalCount is 5");
        Assert(page1.Transactions.TotalPages == 3, "TotalPages is 3");
        Assert(!page1.Transactions.HasPreviousPage, "Page 1 has no previous page");
        Assert(page1.Transactions.HasNextPage, "Page 1 has next page");

        // Request Page 2
        var page2 = service.GetRevenueReportAsync(new ReportFilterRequest { Period = "monthly", PageNumber = 2, PageSize = 2 }).GetAwaiter().GetResult();
        Assert(page2.Transactions.Items.Count == 2, "Page 2 contains 2 items");
        Assert(page2.Transactions.HasPreviousPage, "Page 2 has previous page");
    }

    private static void TestReservationReportMetricsAndBreakdown()
    {
        Console.WriteLine("\n[6] Reservation Report Metrics & Breakdown Tests:");
        var (context, service) = CreateMockContextAndService();

        var area = new ParkingArea { Id = Guid.NewGuid(), Name = "North Station", Address = "90 North", City = "Metro", HourlyRate = 4, TotalSlots = 10, IsActive = true };
        var slot = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "N-01", Status = SlotStatus.Available };
        var user = new User { Id = Guid.NewGuid(), FullName = "Commuter", Email = "commuter@example.com", PhoneNumber = "1", PasswordHash = "h" };
        var vehicle = new Vehicle { Id = Guid.NewGuid(), UserId = user.Id, LicensePlate = "COMM-1", Make = "Honda", Model = "Fit", VehicleType = VehicleType.Car };

        context.ParkingAreas.Add(area);
        context.ParkingSlots.Add(slot);
        context.Users.Add(user);
        context.Vehicles.Add(vehicle);

        var now = DateTime.UtcNow;
        context.Reservations.AddRange(
            new Reservation { Id = Guid.NewGuid(), ReservationNumber = "R-1", UserId = user.Id, ParkingSlotId = slot.Id, VehicleId = vehicle.Id, TotalPrice = 16m, Status = ReservationStatus.Completed, StartTime = now.AddDays(-3), EndTime = now.AddDays(-3).AddHours(2), CreatedAt = now.AddDays(-3) },
            new Reservation { Id = Guid.NewGuid(), ReservationNumber = "R-2", UserId = user.Id, ParkingSlotId = slot.Id, VehicleId = vehicle.Id, TotalPrice = 8m, Status = ReservationStatus.Cancelled, StartTime = now.AddDays(-2), EndTime = now.AddDays(-2).AddHours(1), CreatedAt = now.AddDays(-2) },
            new Reservation { Id = Guid.NewGuid(), ReservationNumber = "R-3", UserId = user.Id, ParkingSlotId = slot.Id, VehicleId = vehicle.Id, TotalPrice = 24m, Status = ReservationStatus.Active, StartTime = now.AddHours(-1), EndTime = now.AddHours(2), CreatedAt = now.AddHours(-1) },
            new Reservation { Id = Guid.NewGuid(), ReservationNumber = "R-4", UserId = user.Id, ParkingSlotId = slot.Id, VehicleId = vehicle.Id, TotalPrice = 12m, Status = ReservationStatus.Pending, StartTime = now.AddDays(1), EndTime = now.AddDays(1).AddHours(1), CreatedAt = now }
        );

        context.SaveChanges();

        var report = service.GetReservationReportAsync(new ReportFilterRequest { Period = "weekly" }).GetAwaiter().GetResult();

        Assert(report.TotalReservations == 4, "TotalReservations is 4");
        Assert(report.CompletedReservations == 1, "CompletedReservations is 1");
        Assert(report.CancelledReservations == 1, "CancelledReservations is 1");
        Assert(report.ActiveReservations == 1, "ActiveReservations is 1");
        Assert(report.PendingReservations == 1, "PendingReservations is 1");
        Assert(report.CancellationRatePercentage == 25.0, "CancellationRatePercentage is 25.0% (1/4)");
        Assert(report.CompletionRatePercentage == 25.0, "CompletionRatePercentage is 25.0% (1/4)");
        Assert(report.ReservationsByStatus.ContainsKey("Completed"), "ReservationsByStatus contains Completed");
        Assert(report.ReservationsByStatus.ContainsKey("Cancelled"), "ReservationsByStatus contains Cancelled");
        Assert(report.Reservations.Items.Count == 4, "Paginated reservations table contains all 4 records");
        Assert(report.Reservations.Items[0].CustomerName == "Commuter", "CustomerName correctly populated in reservation items");
    }

    private static void TestOccupancyReportSlotTypeAndHourlyDistribution()
    {
        Console.WriteLine("\n[7] Occupancy Report Slot Type & Hourly Distribution Tests:");
        var (context, service) = CreateMockContextAndService();

        var area = new ParkingArea { Id = Guid.NewGuid(), Name = "Terminal Deck", Address = "1 Terminal", City = "Metro", HourlyRate = 6, TotalSlots = 4, IsActive = true };
        context.ParkingAreas.Add(area);

        var slotT1 = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "T-1", SlotType = SlotType.Standard, Status = SlotStatus.Occupied };
        var slotT2 = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "T-2", SlotType = SlotType.Standard, Status = SlotStatus.Available };
        var slotE1 = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "E-1", SlotType = SlotType.EVCharging, Status = SlotStatus.Occupied };
        var slotE2 = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "E-2", SlotType = SlotType.EVCharging, Status = SlotStatus.Reserved };
        context.ParkingSlots.AddRange(slotT1, slotT2, slotE1, slotE2);

        var vehicle = new Vehicle { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), LicensePlate = "OCC-1", Make = "Honda", Model = "Civic", VehicleType = VehicleType.Car };
        context.Vehicles.Add(vehicle);

        // Historical check-in at 14:00 (2 PM)
        var today2Pm = DateTime.UtcNow.Date.AddHours(14);
        var res = new Reservation
        {
            Id = Guid.NewGuid(),
            ReservationNumber = "RES-OCC-1",
            UserId = vehicle.UserId,
            ParkingSlotId = slotT1.Id,
            VehicleId = vehicle.Id,
            StartTime = today2Pm.AddHours(-1),
            EndTime = today2Pm.AddHours(2),
            TotalPrice = 12m,
            Status = ReservationStatus.Active
        };
        context.Reservations.Add(res);

        context.ParkingHistories.Add(new ParkingHistory
        {
            Id = Guid.NewGuid(),
            ReservationId = res.Id,
            VehicleId = vehicle.Id,
            ParkingSlotId = slotT1.Id,
            CheckInTime = today2Pm
        });

        context.SaveChanges();

        var report = service.GetOccupancyReportAsync(new ReportFilterRequest { Period = "daily" }).GetAwaiter().GetResult();

        Assert(report.TotalSlots == 4, "TotalSlots is 4");
        Assert(report.OccupiedSlots == 2, "OccupiedSlots is 2");
        Assert(report.AvailableSlots == 1, "AvailableSlots is 1");
        Assert(report.ReservedSlots == 1, "ReservedSlots is 1");
        Assert(report.OccupancyRatePercentage == 50.0, "OccupancyRatePercentage is 50.0% (2/4)");
        Assert(report.OccupancyBySlotType.Count == 2, "OccupancyBySlotType has 2 types (Standard and EVCharging)");

        var std = report.OccupancyBySlotType.First(x => x.SlotType == SlotType.Standard);
        Assert(std.OccupancyRatePercentage == 50.0, "Standard slot occupancy is 50.0%");

        Assert(report.HourlyOccupancy.Count == 24, "HourlyOccupancy has all 24 hour buckets");
        var bucket2Pm = report.HourlyOccupancy.First(h => h.Hour == 14);
        Assert(bucket2Pm.CheckInCount == 1, "14:00 bucket contains 1 check-in");
        Assert(bucket2Pm.IsPeakHour, "14:00 is flagged as peak hour");
        Assert(report.Facilities.Items.Count == 1, "Facilities table contains Terminal Deck");
    }

    private static void TestCustomerReportMetricsAndActivityTiers()
    {
        Console.WriteLine("\n[8] Customer Report Metrics & Activity Tiers Tests:");
        var (context, service) = CreateMockContextAndService();

        var customerRole = new Role { Id = Guid.NewGuid(), Name = AppRoles.Customer, Description = "Customer" };
        context.Roles.Add(customerRole);

        // Customer 1: Frequent (10 bookings, $200 spend)
        var cust1 = new User { Id = Guid.NewGuid(), RoleId = customerRole.Id, FullName = "Alice Frequent", Email = "alice@vip.com", PhoneNumber = "111", PasswordHash = "h", CreatedAt = DateTime.UtcNow.AddDays(-10) };
        // Customer 2: Occasional (2 bookings, $30 spend)
        var cust2 = new User { Id = Guid.NewGuid(), RoleId = customerRole.Id, FullName = "Bob Occasional", Email = "bob@buyer.com", PhoneNumber = "222", PasswordHash = "h", CreatedAt = DateTime.UtcNow.AddDays(-5) };
        // Customer 3: Inactive (0 bookings, $0 spend)
        var cust3 = new User { Id = Guid.NewGuid(), RoleId = customerRole.Id, FullName = "Charlie Inactive", Email = "charlie@new.com", PhoneNumber = "333", PasswordHash = "h", CreatedAt = DateTime.UtcNow.AddDays(-1) };

        context.Users.AddRange(cust1, cust2, cust3);

        // 10 reservations for cust1
        for (int i = 1; i <= 10; i++)
        {
            var r = new Reservation { Id = Guid.NewGuid(), ReservationNumber = $"ALICE-{i}", UserId = cust1.Id, ParkingSlotId = Guid.NewGuid(), TotalPrice = 20m, Status = ReservationStatus.Completed, StartTime = DateTime.UtcNow.AddDays(-i), EndTime = DateTime.UtcNow.AddDays(-i).AddHours(1), CreatedAt = DateTime.UtcNow.AddDays(-i) };
            context.Reservations.Add(r);
            context.Payments.Add(new Payment { Id = Guid.NewGuid(), ReservationId = r.Id, UserId = cust1.Id, Amount = 20m, Status = PaymentStatus.Paid, PaidAt = DateTime.UtcNow.AddDays(-i) });
        }

        // 2 reservations for cust2
        for (int i = 1; i <= 2; i++)
        {
            var r = new Reservation { Id = Guid.NewGuid(), ReservationNumber = $"BOB-{i}", UserId = cust2.Id, ParkingSlotId = Guid.NewGuid(), TotalPrice = 15m, Status = ReservationStatus.Completed, StartTime = DateTime.UtcNow.AddDays(-i), EndTime = DateTime.UtcNow.AddDays(-i).AddHours(1), CreatedAt = DateTime.UtcNow.AddDays(-i) };
            context.Reservations.Add(r);
            context.Payments.Add(new Payment { Id = Guid.NewGuid(), ReservationId = r.Id, UserId = cust2.Id, Amount = 15m, Status = PaymentStatus.Paid, PaidAt = DateTime.UtcNow.AddDays(-i) });
        }

        context.SaveChanges();

        var report = service.GetCustomerReportAsync(new ReportFilterRequest { Period = "monthly" }).GetAwaiter().GetResult();

        Assert(report.TotalCustomers == 3, "TotalCustomers is 3");
        Assert(report.TotalCustomerSpend == 230m, "TotalCustomerSpend is $230.00 (200 + 30)");
        Assert(report.TotalCustomerReservations == 12, "TotalCustomerReservations is 12 (10 + 2)");
        Assert(report.CustomerActivityTiers["Frequent"] == 1, "Frequent tier count is 1");
        Assert(report.CustomerActivityTiers["Occasional"] == 1, "Occasional tier count is 1");
        Assert(report.CustomerActivityTiers["Inactive"] == 1, "Inactive tier count is 1");
        Assert(report.Customers.Items.Count == 3, "Customers table has 3 records");
        Assert(report.Customers.Items[0].FullName == "Alice Frequent", "Top spender (Alice) ordered first");
        Assert(report.Customers.Items[0].ActivityTier == "Frequent", "Alice tier is Frequent");
    }

    private static void TestCustomerReportSearchAndPagination()
    {
        Console.WriteLine("\n[9] Customer Report Search & Pagination Tests:");
        var (context, service) = CreateMockContextAndService();

        var customerRole = new Role { Id = Guid.NewGuid(), Name = AppRoles.Customer, Description = "Customer" };
        context.Roles.Add(customerRole);

        context.Users.AddRange(
            new User { Id = Guid.NewGuid(), RoleId = customerRole.Id, FullName = "George Harrison", Email = "george@beatles.com", PhoneNumber = "1", PasswordHash = "h" },
            new User { Id = Guid.NewGuid(), RoleId = customerRole.Id, FullName = "John Lennon", Email = "john@beatles.com", PhoneNumber = "2", PasswordHash = "h" },
            new User { Id = Guid.NewGuid(), RoleId = customerRole.Id, FullName = "Paul McCartney", Email = "paul@beatles.com", PhoneNumber = "3", PasswordHash = "h" }
        );

        context.SaveChanges();

        // Search for 'george'
        var searchReport = service.GetCustomerReportAsync(new ReportFilterRequest { Search = "george" }).GetAwaiter().GetResult();
        Assert(searchReport.Customers.Items.Count == 1, "Search for 'george' returns exactly 1 customer");
        Assert(searchReport.Customers.Items[0].FullName == "George Harrison", "Matched customer is George Harrison");

        // Pagination: PageSize = 1
        var pagedReport = service.GetCustomerReportAsync(new ReportFilterRequest { PageNumber = 1, PageSize = 1 }).GetAwaiter().GetResult();
        Assert(pagedReport.Customers.Items.Count == 1, "PageSize = 1 returns 1 item");
        Assert(pagedReport.Customers.TotalCount == 3, "TotalCount is 3");
        Assert(pagedReport.Customers.TotalPages == 3, "TotalPages is 3");
    }
}
