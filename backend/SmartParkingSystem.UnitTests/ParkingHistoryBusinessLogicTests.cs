using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SmartParkingSystem.Application.Common.Security;
using SmartParkingSystem.Application.DTOs.ParkingHistory;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;
using SmartParkingSystem.Infrastructure.Data;
using SmartParkingSystem.Infrastructure.Services;

namespace SmartParkingSystem.UnitTests;

public static class ParkingHistoryBusinessLogicTests
{
    private static int _passed = 0;
    private static int _failed = 0;

    public static int Run()
    {
        _passed = 0;
        _failed = 0;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        Console.WriteLine(" Running Step 16: Parking History Business Logic Tests   ");
        Console.WriteLine("=========================================================");
        Console.ResetColor();

        TestCustomerHistoryIsolation();
        TestAdminRetrievesAllHistory();
        TestPagination();
        TestDateFiltering();
        TestInvalidDateRangeValidation();
        TestSearchFilter();
        TestFacilityFilter();
        TestDurationAndAmountCalculations();
        TestGetHistoryByIdOwnershipEnforcement();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        if (_failed == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" Step 16 Parking History Tests: {_passed} Passed, {_failed} Failed");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" Step 16 Parking History Tests: {_passed} Passed, {_failed} Failed");
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

    private static (ApplicationDbContext context, IParkingHistoryService service) CreateMockContextAndService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        var service = new ParkingHistoryService(context, NullLogger<ParkingHistoryService>.Instance);

        return (context, service);
    }

    private static (User customerA, User customerB, User attendant, ParkingArea area1, ParkingArea area2) SeedBaseUsersAndAreas(ApplicationDbContext context)
    {
        var custRole = new Role { Id = Guid.NewGuid(), Name = AppRoles.Customer };
        var staffRole = new Role { Id = Guid.NewGuid(), Name = AppRoles.ParkingAttendant };
        var adminRole = new Role { Id = Guid.NewGuid(), Name = AppRoles.Admin };
        context.Roles.AddRange(custRole, staffRole, adminRole);

        var customerA = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Customer Alpha",
            Email = "alpha@customer.com",
            PhoneNumber = "+1111111111",
            PasswordHash = "hash",
            RoleId = custRole.Id,
            Role = custRole
        };
        var customerB = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Customer Beta",
            Email = "beta@customer.com",
            PhoneNumber = "+2222222222",
            PasswordHash = "hash",
            RoleId = custRole.Id,
            Role = custRole
        };
        var attendant = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Staff Attendant",
            Email = "attendant@parking.com",
            PasswordHash = "hash",
            RoleId = staffRole.Id,
            Role = staffRole
        };
        context.Users.AddRange(customerA, customerB, attendant);

        var area1 = new ParkingArea
        {
            Id = Guid.NewGuid(),
            Name = "Plaza Central Garage",
            Address = "100 Main St",
            City = "Metropolis",
            HourlyRate = 6.00m
        };
        var area2 = new ParkingArea
        {
            Id = Guid.NewGuid(),
            Name = "Airport Terminal West",
            Address = "500 Airport Rd",
            City = "Metropolis",
            HourlyRate = 12.00m
        };
        context.ParkingAreas.AddRange(area1, area2);
        context.SaveChanges();

        return (customerA, customerB, attendant, area1, area2);
    }

    private static ParkingHistory CreateParkingHistoryRecord(
        ApplicationDbContext context,
        User customer,
        ParkingArea area,
        string slotNumber,
        string licensePlate,
        DateTime checkIn,
        DateTime? checkOut,
        decimal basePrice,
        decimal? overstayFee,
        User? attendant)
    {
        var slot = new ParkingSlot
        {
            Id = Guid.NewGuid(),
            ParkingAreaId = area.Id,
            ParkingArea = area,
            SlotNumber = slotNumber,
            FloorNumber = 1,
            HourlyRate = area.HourlyRate,
            Status = checkOut.HasValue ? SlotStatus.Available : SlotStatus.Occupied,
            VehicleType = VehicleType.Car
        };
        context.ParkingSlots.Add(slot);

        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            User = customer,
            LicensePlate = licensePlate,
            Make = "Ford",
            Model = "Focus",
            Color = "Blue",
            VehicleType = VehicleType.Car
        };
        context.Vehicles.Add(vehicle);

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            ReservationNumber = $"RES-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            UserId = customer.Id,
            User = customer,
            ParkingSlotId = slot.Id,
            ParkingSlot = slot,
            VehicleId = vehicle.Id,
            Vehicle = vehicle,
            StartTime = checkIn,
            EndTime = checkOut ?? checkIn.AddHours(2),
            TotalPrice = basePrice,
            Status = checkOut.HasValue ? ReservationStatus.Completed : ReservationStatus.Active,
            CreatedAt = checkIn
        };
        context.Reservations.Add(reservation);

        var history = new ParkingHistory
        {
            Id = Guid.NewGuid(),
            ReservationId = reservation.Id,
            Reservation = reservation,
            VehicleId = vehicle.Id,
            Vehicle = vehicle,
            ParkingSlotId = slot.Id,
            ParkingSlot = slot,
            CheckInTime = checkIn,
            CheckOutTime = checkOut,
            OverstayDurationMinutes = overstayFee.HasValue && overstayFee.Value > 0 ? 30 : 0,
            OverstayFee = overstayFee,
            AttendantCheckInId = attendant?.Id,
            AttendantCheckIn = attendant,
            AttendantCheckOutId = checkOut.HasValue ? attendant?.Id : null,
            AttendantCheckOut = checkOut.HasValue ? attendant : null,
            CreatedAt = checkIn
        };
        context.ParkingHistories.Add(history);
        context.SaveChanges();

        return history;
    }

    private static void TestCustomerHistoryIsolation()
    {
        Console.WriteLine("\n[1] Customer Parking History Isolation Tests:");
        var (context, service) = CreateMockContextAndService();
        var (custA, custB, attendant, area1, _) = SeedBaseUsersAndAreas(context);

        var now = DateTime.UtcNow;
        CreateParkingHistoryRecord(context, custA, area1, "A-1", "AAA-111", now.AddDays(-2), now.AddDays(-2).AddHours(2), 12.00m, null, attendant);
        CreateParkingHistoryRecord(context, custA, area1, "A-2", "AAA-111", now.AddDays(-1), now.AddDays(-1).AddHours(1), 6.00m, null, attendant);
        CreateParkingHistoryRecord(context, custB, area1, "B-1", "BBB-222", now.AddDays(-1), now.AddDays(-1).AddHours(3), 18.00m, null, attendant);

        var resultA = service.GetCustomerHistoryAsync(custA.Id, new ParkingHistoryFilterRequest()).GetAwaiter().GetResult();
        var resultB = service.GetCustomerHistoryAsync(custB.Id, new ParkingHistoryFilterRequest()).GetAwaiter().GetResult();

        Assert(resultA.TotalCount == 2, "Customer A retrieves exactly 2 parking history records");
        Assert(resultB.TotalCount == 1, "Customer B retrieves exactly 1 parking history record");
        Assert(resultA.Items.All(i => i.CustomerId == custA.Id), "All records returned to Customer A belong to Customer A");
        Assert(resultB.Items.All(i => i.CustomerId == custB.Id), "All records returned to Customer B belong to Customer B");
    }

    private static void TestAdminRetrievesAllHistory()
    {
        Console.WriteLine("\n[2] Admin System-wide History Retrieval Tests:");
        var (context, service) = CreateMockContextAndService();
        var (custA, custB, attendant, area1, area2) = SeedBaseUsersAndAreas(context);

        var now = DateTime.UtcNow;
        CreateParkingHistoryRecord(context, custA, area1, "A-1", "AAA-111", now.AddHours(-10), now.AddHours(-8), 12.00m, null, attendant);
        CreateParkingHistoryRecord(context, custB, area2, "W-1", "BBB-222", now.AddHours(-5), now.AddHours(-4), 12.00m, null, attendant);

        var adminResult = service.GetAllHistoryAsync(new ParkingHistoryFilterRequest()).GetAwaiter().GetResult();

        Assert(adminResult.TotalCount == 2, "Admin retrieves all parking history records across all customers");
        Assert(adminResult.Items.Any(i => i.CustomerId == custA.Id), "Contains Customer A record");
        Assert(adminResult.Items.Any(i => i.CustomerId == custB.Id), "Contains Customer B record");
    }

    private static void TestPagination()
    {
        Console.WriteLine("\n[3] Pagination & Navigation Tests:");
        var (context, service) = CreateMockContextAndService();
        var (custA, _, attendant, area1, _) = SeedBaseUsersAndAreas(context);

        var now = DateTime.UtcNow;
        for (int i = 1; i <= 5; i++)
        {
            CreateParkingHistoryRecord(context, custA, area1, $"P-{i}", $"PAG-{i}", now.AddHours(-i * 5), now.AddHours(-i * 5 + 1), 6.00m, null, attendant);
        }

        // Page 1: 2 items
        var page1 = service.GetCustomerHistoryAsync(custA.Id, new ParkingHistoryFilterRequest { PageNumber = 1, PageSize = 2 }).GetAwaiter().GetResult();
        Assert(page1.Items.Count == 2, "Page 1 returns 2 items");
        Assert(page1.TotalCount == 5, "TotalCount is 5");
        Assert(page1.TotalPages == 3, "TotalPages is 3");
        Assert(!page1.HasPreviousPage, "Page 1 has no previous page");
        Assert(page1.HasNextPage, "Page 1 has next page");

        // Page 2: 2 items
        var page2 = service.GetCustomerHistoryAsync(custA.Id, new ParkingHistoryFilterRequest { PageNumber = 2, PageSize = 2 }).GetAwaiter().GetResult();
        Assert(page2.Items.Count == 2, "Page 2 returns 2 items");
        Assert(page2.HasPreviousPage, "Page 2 has previous page");
        Assert(page2.HasNextPage, "Page 2 has next page");

        // Page 3: 1 item
        var page3 = service.GetCustomerHistoryAsync(custA.Id, new ParkingHistoryFilterRequest { PageNumber = 3, PageSize = 2 }).GetAwaiter().GetResult();
        Assert(page3.Items.Count == 1, "Page 3 returns 1 item");
        Assert(page3.HasPreviousPage, "Page 3 has previous page");
        Assert(!page3.HasNextPage, "Page 3 has no next page");
    }

    private static void TestDateFiltering()
    {
        Console.WriteLine("\n[4] Date Filtering Tests:");
        var (context, service) = CreateMockContextAndService();
        var (custA, _, attendant, area1, _) = SeedBaseUsersAndAreas(context);

        var baseTime = new DateTime(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc);
        CreateParkingHistoryRecord(context, custA, area1, "D-1", "DAT-1", baseTime.AddDays(-10), baseTime.AddDays(-10).AddHours(2), 12.00m, null, attendant);
        CreateParkingHistoryRecord(context, custA, area1, "D-2", "DAT-2", baseTime.AddDays(-3), baseTime.AddDays(-3).AddHours(2), 12.00m, null, attendant);
        CreateParkingHistoryRecord(context, custA, area1, "D-3", "DAT-3", baseTime, baseTime.AddHours(2), 12.00m, null, attendant);

        // Filter: Last 5 days
        var filterRecent = new ParkingHistoryFilterRequest
        {
            StartDate = baseTime.AddDays(-5),
            EndDate = baseTime.AddDays(1)
        };
        var recentResult = service.GetCustomerHistoryAsync(custA.Id, filterRecent).GetAwaiter().GetResult();

        Assert(recentResult.TotalCount == 2, "Date filter returns 2 records within the 5-day window");
        Assert(recentResult.Items.All(i => i.CheckInTime >= baseTime.AddDays(-5)), "All returned records satisfy StartDate");
    }

    private static void TestInvalidDateRangeValidation()
    {
        Console.WriteLine("\n[5] Invalid Date Range Validation Test:");
        var (context, service) = CreateMockContextAndService();
        var (custA, _, _, _, _) = SeedBaseUsersAndAreas(context);

        var invalidFilter = new ParkingHistoryFilterRequest
        {
            StartDate = DateTime.UtcNow.AddDays(5),
            EndDate = DateTime.UtcNow.AddDays(2) // EndDate before StartDate
        };

        bool threw = false;
        try
        {
            service.GetCustomerHistoryAsync(custA.Id, invalidFilter).GetAwaiter().GetResult();
        }
        catch (ArgumentException ex)
        {
            threw = ex.Message.Contains("StartDate cannot be greater than EndDate");
        }

        Assert(threw, "StartDate > EndDate rejected with ArgumentException");
    }

    private static void TestSearchFilter()
    {
        Console.WriteLine("\n[6] Search Filter Tests:");
        var (context, service) = CreateMockContextAndService();
        var (custA, _, attendant, area1, _) = SeedBaseUsersAndAreas(context);

        var now = DateTime.UtcNow;
        CreateParkingHistoryRecord(context, custA, area1, "SR-1", "SEARCH-999", now.AddHours(-3), now.AddHours(-1), 12.00m, null, attendant);
        CreateParkingHistoryRecord(context, custA, area1, "SR-2", "OTHER-111", now.AddHours(-2), now.AddHours(-1), 6.00m, null, attendant);

        var searchResult = service.GetCustomerHistoryAsync(custA.Id, new ParkingHistoryFilterRequest { Search = "search-999" }).GetAwaiter().GetResult();

        Assert(searchResult.TotalCount == 1, "Search returns 1 record matching license plate");
        Assert(searchResult.Items[0].LicensePlate == "SEARCH-999", "Matched item has searched license plate");
    }

    private static void TestFacilityFilter()
    {
        Console.WriteLine("\n[7] Facility Filter Tests:");
        var (context, service) = CreateMockContextAndService();
        var (custA, _, attendant, area1, area2) = SeedBaseUsersAndAreas(context);

        var now = DateTime.UtcNow;
        CreateParkingHistoryRecord(context, custA, area1, "F-1", "FAC-1", now.AddHours(-4), now.AddHours(-2), 12.00m, null, attendant);
        CreateParkingHistoryRecord(context, custA, area2, "F-2", "FAC-2", now.AddHours(-3), now.AddHours(-1), 24.00m, null, attendant);

        var facilityResult = service.GetCustomerHistoryAsync(custA.Id, new ParkingHistoryFilterRequest { ParkingAreaId = area2.Id }).GetAwaiter().GetResult();

        Assert(facilityResult.TotalCount == 1, "Facility filter returns 1 record for area2");
        Assert(facilityResult.Items[0].ParkingAreaId == area2.Id, "Matched record belongs to specified area");
    }

    private static void TestDurationAndAmountCalculations()
    {
        Console.WriteLine("\n[8] Duration & Amount Calculations Tests:");
        var (context, service) = CreateMockContextAndService();
        var (custA, _, attendant, area1, _) = SeedBaseUsersAndAreas(context);

        var entryTime = DateTime.UtcNow.AddMinutes(-90);
        var exitTime = entryTime.AddMinutes(90);

        CreateParkingHistoryRecord(context, custA, area1, "C-1", "CALC-1", entryTime, exitTime, 15.00m, 5.00m, attendant);

        var result = service.GetCustomerHistoryAsync(custA.Id, new ParkingHistoryFilterRequest()).GetAwaiter().GetResult();
        var item = result.Items[0];

        Assert(item.DurationMinutes == 90, "Actual duration calculated as 90 minutes");
        Assert(item.BaseAmount == 15.00m, "BaseAmount is $15.00");
        Assert(item.AdditionalFee == 5.00m, "AdditionalFee is $5.00");
        Assert(item.TotalAmount == 20.00m, "TotalAmount is $20.00 (Base + Overstay)");
        Assert(item.OverstayFee == 5.00m, "OverstayFee property matches AdditionalFee");
    }

    private static void TestGetHistoryByIdOwnershipEnforcement()
    {
        Console.WriteLine("\n[9] GetHistoryById Ownership Isolation Tests:");
        var (context, service) = CreateMockContextAndService();
        var (custA, custB, attendant, area1, _) = SeedBaseUsersAndAreas(context);

        var now = DateTime.UtcNow;
        var recordA = CreateParkingHistoryRecord(context, custA, area1, "ISO-1", "ISO-111", now.AddHours(-2), now.AddHours(-1), 6.00m, null, attendant);

        // 1. Owner can access
        var retrievedByOwner = service.GetHistoryByIdAsync(recordA.Id, custA.Id, isAdmin: false).GetAwaiter().GetResult();
        Assert(retrievedByOwner != null && retrievedByOwner.Id == recordA.Id, "Owner successfully retrieves their own parking record");

        // 2. Stranger customer blocked
        bool strangerBlocked = false;
        try
        {
            service.GetHistoryByIdAsync(recordA.Id, custB.Id, isAdmin: false).GetAwaiter().GetResult();
        }
        catch (UnauthorizedAccessException)
        {
            strangerBlocked = true;
        }
        Assert(strangerBlocked, "Stranger customer blocked with UnauthorizedAccessException");

        // 3. Admin can access any record
        var adminId = Guid.NewGuid();
        var retrievedByAdmin = service.GetHistoryByIdAsync(recordA.Id, adminId, isAdmin: true).GetAwaiter().GetResult();
        Assert(retrievedByAdmin != null && retrievedByAdmin.Id == recordA.Id, "Admin successfully retrieves customer's parking record");

        // 4. Missing ID throws KeyNotFoundException
        bool missingThrew = false;
        try
        {
            service.GetHistoryByIdAsync(Guid.NewGuid(), custA.Id, isAdmin: false).GetAwaiter().GetResult();
        }
        catch (KeyNotFoundException)
        {
            missingThrew = true;
        }
        Assert(missingThrew, "Non-existent record ID throws KeyNotFoundException");
    }
}
