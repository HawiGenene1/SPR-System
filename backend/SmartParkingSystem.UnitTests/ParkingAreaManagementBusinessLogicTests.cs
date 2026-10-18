using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SmartParkingSystem.Application.Common.Validation;
using SmartParkingSystem.Application.DTOs.ParkingAreas;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;
using SmartParkingSystem.Infrastructure.Data;
using SmartParkingSystem.Infrastructure.Repositories;
using SmartParkingSystem.Infrastructure.Services;

namespace SmartParkingSystem.UnitTests;

public static class ParkingAreaManagementBusinessLogicTests
{
    private static int _passed;
    private static int _failed;

    public static int Run()
    {
        _passed = 0;
        _failed = 0;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        Console.WriteLine(" Running Parking Area Management Business Logic Unit Tests");
        Console.WriteLine("=========================================================");
        Console.ResetColor();

        TestOperatingHoursValidation();
        TestCapacityValidation();
        TestDuplicateNamePrevention();
        TestActiveReservationDeletionGuard();
        TestActiveParkingSessionDeletionGuard();
        TestCascadingSoftDelete();
        TestDtoLocationAndCapacityAliases();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        if (_failed == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" Parking Area Tests Summary: {_passed} Passed, {_failed} Failed");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" Parking Area Tests Summary: {_passed} Passed, {_failed} Failed");
        }
        Console.WriteLine("=========================================================");
        Console.ResetColor();

        return _failed == 0 ? 0 : 1;
    }

    private static void TestOperatingHoursValidation()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[1] Operating Hours Validation Tests:");
        Console.ResetColor();

        // Valid Operating Hours
        Assert(ParkingAreaValidators.IsValidOperatingHours("24/7"), "'24/7' schedule is valid");
        Assert(ParkingAreaValidators.IsValidOperatingHours("24 Hours"), "'24 Hours' schedule is valid");
        Assert(ParkingAreaValidators.IsValidOperatingHours("06:00 - 23:00"), "'06:00 - 23:00' 24h schedule is valid");
        Assert(ParkingAreaValidators.IsValidOperatingHours("08:00 AM - 10:00 PM"), "'08:00 AM - 10:00 PM' 12h schedule is valid");
        Assert(ParkingAreaValidators.IsValidOperatingHours("Mon-Fri 08:00 - 18:00"), "'Mon-Fri 08:00 - 18:00' weekday schedule is valid");
        Assert(ParkingAreaValidators.IsValidOperatingHours("Daily: 24/7"), "'Daily: 24/7' schedule is valid");

        // Invalid Operating Hours
        Assert(!ParkingAreaValidators.IsValidOperatingHours(""), "Empty string is invalid");
        Assert(!ParkingAreaValidators.IsValidOperatingHours("   "), "Whitespace is invalid");
        Assert(!ParkingAreaValidators.IsValidOperatingHours("abc"), "Random text 'abc' is invalid");
        Assert(!ParkingAreaValidators.IsValidOperatingHours("invalid_schedule"), "Arbitrary text 'invalid_schedule' is invalid");
        Assert(!ParkingAreaValidators.IsValidOperatingHours("99:99 - 88:88"), "Out of range time '99:99 - 88:88' is invalid");
    }

    private static void TestCapacityValidation()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[2] Parking Area Capacity Validation Tests:");
        Console.ResetColor();

        Assert(ParkingAreaValidators.IsValidCapacity(1), "Capacity 1 is valid (minimum)");
        Assert(ParkingAreaValidators.IsValidCapacity(50), "Capacity 50 is valid");
        Assert(ParkingAreaValidators.IsValidCapacity(500), "Capacity 500 is valid");
        Assert(ParkingAreaValidators.IsValidCapacity(10000), "Capacity 10,000 is valid (maximum)");

        Assert(!ParkingAreaValidators.IsValidCapacity(0), "Capacity 0 is invalid");
        Assert(!ParkingAreaValidators.IsValidCapacity(-10), "Negative capacity is invalid");
        Assert(!ParkingAreaValidators.IsValidCapacity(10001), "Capacity over 10,000 is invalid");
    }

    private static void TestDuplicateNamePrevention()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[3] Duplicate Facility Name Prevention Tests:");
        Console.ResetColor();

        var (context, service) = CreateMockContextAndService();

        var first = service.CreateAsync(new CreateParkingAreaRequest
        {
            Name = "Downtown Grand Plaza",
            Address = "100 Main Street",
            City = "Metropolis",
            TotalSlots = 100,
            HourlyRate = 5.00m,
            OperatingHours = "24/7"
        }).GetAwaiter().GetResult();

        Assert(first.Name == "Downtown Grand Plaza", "First parking facility registered successfully");

        // Duplicate name (case-insensitive check)
        bool duplicateRejected = false;
        try
        {
            service.CreateAsync(new CreateParkingAreaRequest
            {
                Name = "downtown grand plaza", // lower case duplicate
                Address = "200 Second Street",
                City = "Metropolis",
                TotalSlots = 50,
                HourlyRate = 4.00m,
                OperatingHours = "24/7"
            }).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException)
        {
            duplicateRejected = true;
        }
        Assert(duplicateRejected, "Duplicate facility name is rejected with InvalidOperationException");
    }

    private static void TestActiveReservationDeletionGuard()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[4] Active Reservation Deletion Guard Tests (Rule: Prevent deletion if active reservations exist):");
        Console.ResetColor();

        var (context, service) = CreateMockContextAndService();

        var area = new ParkingArea
        {
            Id = Guid.NewGuid(),
            Name = "Airport Terminal Parking",
            Address = "Terminal 1",
            City = "Aero City",
            TotalSlots = 20,
            HourlyRate = 8.00m,
            OperatingHours = "24/7",
            IsActive = true
        };
        var slot = new ParkingSlot
        {
            Id = Guid.NewGuid(),
            ParkingAreaId = area.Id,
            SlotNumber = "A-01",
            FloorNumber = 1,
            SlotType = SlotType.Standard,
            Status = SlotStatus.Reserved
        };
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            ParkingSlotId = slot.Id,
            UserId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            ReservationNumber = "RES-AIR-01",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(3),
            TotalPrice = 24.00m,
            Status = ReservationStatus.Confirmed,
            IsDeleted = false
        };

        context.ParkingAreas.Add(area);
        context.ParkingSlots.Add(slot);
        context.Reservations.Add(reservation);
        context.SaveChanges();

        // 1. Attempt delete while reservation is Confirmed
        bool deleteBlocked = false;
        try
        {
            service.DeleteAsync(area.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException)
        {
            deleteBlocked = true;
        }
        Assert(deleteBlocked, "Deletion blocked when facility slot has a Confirmed reservation");

        // 2. Transition reservation to Active -> Deletion still blocked
        reservation.Status = ReservationStatus.Active;
        context.SaveChanges();

        bool deleteBlockedActive = false;
        try
        {
            service.DeleteAsync(area.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException)
        {
            deleteBlockedActive = true;
        }
        Assert(deleteBlockedActive, "Deletion blocked when facility slot has an Active reservation");

        // 3. Complete reservation -> Deletion allowed
        reservation.Status = ReservationStatus.Completed;
        context.SaveChanges();

        bool deleteSuccess = service.DeleteAsync(area.Id).GetAwaiter().GetResult();
        Assert(deleteSuccess, "Deletion succeeds after reservations are Completed");
    }

    private static void TestActiveParkingSessionDeletionGuard()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[5] Active Parking Session Deletion Guard Tests:");
        Console.ResetColor();

        var (context, service) = CreateMockContextAndService();

        var area = new ParkingArea
        {
            Id = Guid.NewGuid(),
            Name = "Harbor Bay Parking",
            Address = "Pier 4",
            City = "Harbor City",
            TotalSlots = 10,
            HourlyRate = 6.00m,
            OperatingHours = "24/7",
            IsActive = true
        };
        var slot = new ParkingSlot
        {
            Id = Guid.NewGuid(),
            ParkingAreaId = area.Id,
            SlotNumber = "H-01",
            FloorNumber = 1,
            SlotType = SlotType.Standard,
            Status = SlotStatus.Occupied
        };
        var activeHistory = new ParkingHistory
        {
            Id = Guid.NewGuid(),
            ParkingSlotId = slot.Id,
            ReservationId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            AttendantCheckInId = Guid.NewGuid(),
            CheckInTime = DateTime.UtcNow,
            CheckOutTime = null // Currently parked!
        };

        context.ParkingAreas.Add(area);
        context.ParkingSlots.Add(slot);
        context.ParkingHistories.Add(activeHistory);
        context.SaveChanges();

        // Active session blocks deletion
        bool deleteBlockedBySession = false;
        try
        {
            service.DeleteAsync(area.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException)
        {
            deleteBlockedBySession = true;
        }
        Assert(deleteBlockedBySession, "Deletion blocked when a vehicle is actively parked in the facility (CheckOutTime == null)");

        // Check out vehicle
        activeHistory.CheckOutTime = DateTime.UtcNow;
        context.SaveChanges();

        bool deleteAllowed = service.DeleteAsync(area.Id).GetAwaiter().GetResult();
        Assert(deleteAllowed, "Deletion succeeds once active parking session has completed check-out");
    }

    private static void TestCascadingSoftDelete()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[6] Cascading Soft-Delete Tests:");
        Console.ResetColor();

        var (context, service) = CreateMockContextAndService();

        var area = new ParkingArea
        {
            Id = Guid.NewGuid(),
            Name = "North Hills Parking",
            Address = "500 Hilltop Way",
            City = "North Hills",
            TotalSlots = 5,
            HourlyRate = 4.00m,
            OperatingHours = "24/7",
            IsActive = true
        };
        var slot1 = new ParkingSlot
        {
            Id = Guid.NewGuid(),
            ParkingAreaId = area.Id,
            SlotNumber = "N-01",
            FloorNumber = 1,
            SlotType = SlotType.Standard,
            Status = SlotStatus.Available
        };
        var slot2 = new ParkingSlot
        {
            Id = Guid.NewGuid(),
            ParkingAreaId = area.Id,
            SlotNumber = "N-02",
            FloorNumber = 1,
            SlotType = SlotType.Standard,
            Status = SlotStatus.Available
        };

        context.ParkingAreas.Add(area);
        context.ParkingSlots.AddRange(slot1, slot2);
        context.SaveChanges();

        service.DeleteAsync(area.Id).GetAwaiter().GetResult();

        // Check facility is flagged as soft-deleted
        var softDeletedArea = context.ParkingAreas.IgnoreQueryFilters().First(a => a.Id == area.Id);
        Assert(softDeletedArea.IsDeleted, "Parking area entity is flagged as soft-deleted (IsDeleted = true)");

        // Check slots are cascaded soft-deleted
        var softDeletedSlot1 = context.ParkingSlots.IgnoreQueryFilters().First(s => s.Id == slot1.Id);
        var softDeletedSlot2 = context.ParkingSlots.IgnoreQueryFilters().First(s => s.Id == slot2.Id);
        Assert(softDeletedSlot1.IsDeleted && softDeletedSlot2.IsDeleted, "Associated parking slots cascaded to soft-deleted state");

        // Normal query ignores soft-deleted facility
        var activeArea = context.ParkingAreas.FirstOrDefault(a => a.Id == area.Id);
        Assert(activeArea == null, "Global query filter hides soft-deleted parking facility from standard queries");
    }

    private static void TestDtoLocationAndCapacityAliases()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[7] DTO Location and Capacity Alias Consistency Tests:");
        Console.ResetColor();

        var dto = new ParkingAreaDto
        {
            Id = Guid.NewGuid(),
            Name = "City Center Deck",
            Address = "123 Broadway",
            City = "Metropolis",
            TotalSlots = 250,
            HourlyRate = 6.00m,
            OperatingHours = "24/7",
            IsActive = true
        };

        Assert(dto.Capacity == 250, "DTO Capacity alias matches TotalSlots (250)");
        Assert(dto.Location == "123 Broadway, Metropolis", "DTO Location formatted properly: '123 Broadway, Metropolis'");

        var createReq = new CreateParkingAreaRequest
        {
            Name = "Westside Garage",
            Capacity = 120, // Uses Capacity alias
            Location = "456 West Blvd", // Uses Location alias
            City = "Metropolis",
            HourlyRate = 5.00m,
            OperatingHours = "06:00 - 22:00"
        };

        Assert(createReq.TotalSlots == 120, "Create request TotalSlots matches Capacity (120)");
        Assert(createReq.Address == "456 West Blvd", "Create request Address matches Location ('456 West Blvd')");
    }

    private static (ApplicationDbContext context, IParkingAreaService service) CreateMockContextAndService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        var repo = new ParkingAreaRepository(context);
        var service = new ParkingAreaService(repo, NullLogger<ParkingAreaService>.Instance);

        return (context, service);
    }

    private static void Assert(bool condition, string message)
    {
        if (condition)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  [PASS] {message}");
            _passed++;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [FAIL] {message}");
            _failed++;
        }
        Console.ResetColor();
    }
}
