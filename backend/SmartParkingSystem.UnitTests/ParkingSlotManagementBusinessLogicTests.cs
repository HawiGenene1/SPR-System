using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SmartParkingSystem.Application.Common.Validation;
using SmartParkingSystem.Application.DTOs.ParkingSlots;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;
using SmartParkingSystem.Infrastructure.Data;
using SmartParkingSystem.Infrastructure.Repositories;
using SmartParkingSystem.Infrastructure.Services;

namespace SmartParkingSystem.UnitTests;

public static class ParkingSlotManagementBusinessLogicTests
{
    private static int _passed;
    private static int _failed;

    public static int Run()
    {
        _passed = 0;
        _failed = 0;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        Console.WriteLine(" Running Parking Slot Management Business Logic Unit Tests");
        Console.WriteLine("=========================================================");
        Console.ResetColor();

        TestSlotStatusEnumValues();
        TestSlotValidationRules();
        TestSlotNumberUniquenessWithinParkingArea();
        TestSameSlotNumberInDifferentParkingAreasAllowed();
        TestMaintenanceModeTransitions();
        TestMaintenanceBlockedWithActiveReservations();
        TestActiveReservationDeletionGuard();
        TestHourlyRateAndPricePerHourFallback();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        if (_failed == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" Parking Slot Tests Summary: {_passed} Passed, {_failed} Failed");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" Parking Slot Tests Summary: {_passed} Passed, {_failed} Failed");
        }
        Console.WriteLine("=========================================================");
        Console.ResetColor();

        return _failed == 0 ? 0 : 1;
    }

    private static void TestSlotStatusEnumValues()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[1] Slot Status Enum Values Tests:");
        Console.ResetColor();

        Assert(ParkingSlotValidators.IsValidStatus(SlotStatus.Available), "SlotStatus.Available is defined");
        Assert(ParkingSlotValidators.IsValidStatus(SlotStatus.Reserved), "SlotStatus.Reserved is defined");
        Assert(ParkingSlotValidators.IsValidStatus(SlotStatus.Occupied), "SlotStatus.Occupied is defined");
        Assert(ParkingSlotValidators.IsValidStatus(SlotStatus.Maintenance), "SlotStatus.Maintenance is defined");

        Assert(!ParkingSlotValidators.IsValidStatus((SlotStatus)0), "SlotStatus 0 is invalid");
        Assert(!ParkingSlotValidators.IsValidStatus((SlotStatus)99), "SlotStatus 99 is invalid");
    }

    private static void TestSlotValidationRules()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[2] Slot Number and Floor Validation Tests:");
        Console.ResetColor();

        Assert(ParkingSlotValidators.IsValidSlotNumber("A-01"), "Standard 'A-01' is valid");
        Assert(ParkingSlotValidators.IsValidSlotNumber("EV-100"), "'EV-100' is valid");
        Assert(ParkingSlotValidators.IsValidSlotNumber("B2#15"), "'B2#15' is valid");
        Assert(!ParkingSlotValidators.IsValidSlotNumber(""), "Empty slot number is invalid");
        Assert(!ParkingSlotValidators.IsValidSlotNumber("   "), "Whitespace slot number is invalid");
        Assert(!ParkingSlotValidators.IsValidSlotNumber("THIS_SLOT_NUMBER_IS_WAY_TOO_LONG_12345"), "Over 20 characters is invalid");

        Assert(ParkingSlotValidators.IsValidFloor(1), "Floor 1 is valid");
        Assert(ParkingSlotValidators.IsValidFloor(-2), "Basement Floor -2 is valid");
        Assert(ParkingSlotValidators.IsValidFloor(50), "Floor 50 is valid (maximum)");
        Assert(!ParkingSlotValidators.IsValidFloor(-6), "Floor -6 is invalid (below -5)");
        Assert(!ParkingSlotValidators.IsValidFloor(51), "Floor 51 is invalid (above 50)");
    }

    private static void TestSlotNumberUniquenessWithinParkingArea()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[3] Slot Number Uniqueness within a Parking Area Tests:");
        Console.ResetColor();

        var (context, service) = CreateMockContextAndService();
        var area = new ParkingArea
        {
            Id = Guid.NewGuid(),
            Name = "Plaza Central",
            Address = "100 Main St",
            City = "Metropolis",
            TotalSlots = 50,
            HourlyRate = 5.00m
        };
        context.ParkingAreas.Add(area);
        context.SaveChanges();

        // 1. Create first slot
        var slot1 = service.CreateSlotAsync(new CreateParkingSlotRequest
        {
            ParkingAreaId = area.Id,
            SlotNumber = "A-01",
            FloorNumber = 1,
            SlotType = SlotType.Standard,
            VehicleType = VehicleType.Car
        }).GetAwaiter().GetResult();

        Assert(slot1.SlotNumber == "A-01", "Slot 'A-01' registered in 'Plaza Central'");

        // 2. Attempt duplicate slot number in same area (case-insensitive)
        bool duplicateRejected = false;
        try
        {
            service.CreateSlotAsync(new CreateParkingSlotRequest
            {
                ParkingAreaId = area.Id,
                SlotNumber = "a-01", // lower case duplicate
                FloorNumber = 2,
                SlotType = SlotType.Standard,
                VehicleType = VehicleType.Car
            }).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException)
        {
            duplicateRejected = true;
        }
        Assert(duplicateRejected, "Duplicate slot number 'a-01' in same parking area rejected with InvalidOperationException");
    }

    private static void TestSameSlotNumberInDifferentParkingAreasAllowed()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[4] Same Slot Number in Different Parking Areas Allowed Tests:");
        Console.ResetColor();

        var (context, service) = CreateMockContextAndService();

        var area1 = new ParkingArea { Id = Guid.NewGuid(), Name = "Facility East", Address = "East St", City = "Metropolis", HourlyRate = 5.00m };
        var area2 = new ParkingArea { Id = Guid.NewGuid(), Name = "Facility West", Address = "West St", City = "Metropolis", HourlyRate = 5.00m };
        context.ParkingAreas.AddRange(area1, area2);
        context.SaveChanges();

        var slotArea1 = service.CreateSlotAsync(new CreateParkingSlotRequest
        {
            ParkingAreaId = area1.Id,
            SlotNumber = "VIP-01",
            FloorNumber = 1,
            SlotType = SlotType.VIP,
            VehicleType = VehicleType.Car
        }).GetAwaiter().GetResult();

        var slotArea2 = service.CreateSlotAsync(new CreateParkingSlotRequest
        {
            ParkingAreaId = area2.Id,
            SlotNumber = "VIP-01", // Same slot number, different area
            FloorNumber = 1,
            SlotType = SlotType.VIP,
            VehicleType = VehicleType.Car
        }).GetAwaiter().GetResult();

        Assert(slotArea1.SlotNumber == "VIP-01" && slotArea2.SlotNumber == "VIP-01", 
            "Identical slot number 'VIP-01' is correctly allowed in two distinct parking areas");
    }

    private static void TestMaintenanceModeTransitions()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[5] Maintenance Mode State Transition Tests:");
        Console.ResetColor();

        var (context, service) = CreateMockContextAndService();
        var area = new ParkingArea { Id = Guid.NewGuid(), Name = "South Garage", Address = "South Ave", City = "Metropolis", HourlyRate = 4.00m };
        context.ParkingAreas.Add(area);
        context.SaveChanges();

        var slot = service.CreateSlotAsync(new CreateParkingSlotRequest
        {
            ParkingAreaId = area.Id,
            SlotNumber = "M-01",
            FloorNumber = 1,
            SlotType = SlotType.Standard,
            VehicleType = VehicleType.Car
        }).GetAwaiter().GetResult();

        Assert(slot.Status == SlotStatus.Available, "Initial slot status is Available");

        // Put into maintenance
        var inMaint = service.SetMaintenanceStatusAsync(slot.Id, isMaintenance: true).GetAwaiter().GetResult();
        Assert(inMaint.Status == SlotStatus.Maintenance, "Slot transitioned to Maintenance status");
        Assert(!inMaint.IsAvailable, "Slot in maintenance has IsAvailable = false");

        // Return from maintenance
        var outMaint = service.SetMaintenanceStatusAsync(slot.Id, isMaintenance: false).GetAwaiter().GetResult();
        Assert(outMaint.Status == SlotStatus.Available, "Slot returned to Available status");
        Assert(outMaint.IsAvailable, "Slot returned to IsAvailable = true");
    }

    private static void TestMaintenanceBlockedWithActiveReservations()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[6] Maintenance Blocked with Active Reservations Tests:");
        Console.ResetColor();

        var (context, service) = CreateMockContextAndService();
        var area = new ParkingArea { Id = Guid.NewGuid(), Name = "Metro Garage", Address = "Metro St", City = "Metropolis", HourlyRate = 4.00m };
        var slot = new ParkingSlot
        {
            Id = Guid.NewGuid(),
            ParkingAreaId = area.Id,
            SlotNumber = "BLK-01",
            FloorNumber = 1,
            SlotType = SlotType.Standard,
            VehicleType = VehicleType.Car,
            Status = SlotStatus.Reserved
        };
        var res = new Reservation
        {
            Id = Guid.NewGuid(),
            ParkingSlotId = slot.Id,
            UserId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            ReservationNumber = "RES-BLK-01",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(2),
            TotalPrice = 8.00m,
            Status = ReservationStatus.Confirmed,
            IsDeleted = false
        };

        context.ParkingAreas.Add(area);
        context.ParkingSlots.Add(slot);
        context.Reservations.Add(res);
        context.SaveChanges();

        bool maintBlocked = false;
        try
        {
            service.SetMaintenanceStatusAsync(slot.Id, isMaintenance: true).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException)
        {
            maintBlocked = true;
        }
        Assert(maintBlocked, "Putting slot into maintenance is blocked when active/confirmed reservation exists");
    }

    private static void TestActiveReservationDeletionGuard()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[7] Active Reservation Deletion Guard Tests:");
        Console.ResetColor();

        var (context, service) = CreateMockContextAndService();
        var area = new ParkingArea { Id = Guid.NewGuid(), Name = "Terminal Deck", Address = "Terminal St", City = "Metropolis", HourlyRate = 4.00m };
        var slot = new ParkingSlot
        {
            Id = Guid.NewGuid(),
            ParkingAreaId = area.Id,
            SlotNumber = "DEL-01",
            FloorNumber = 1,
            SlotType = SlotType.Standard,
            VehicleType = VehicleType.Car,
            Status = SlotStatus.Reserved
        };
        var res = new Reservation
        {
            Id = Guid.NewGuid(),
            ParkingSlotId = slot.Id,
            UserId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            ReservationNumber = "RES-DEL-01",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(2),
            TotalPrice = 8.00m,
            Status = ReservationStatus.Confirmed,
            IsDeleted = false
        };

        context.ParkingAreas.Add(area);
        context.ParkingSlots.Add(slot);
        context.Reservations.Add(res);
        context.SaveChanges();

        // 1. Delete blocked while reservation is active
        bool deleteBlocked = false;
        try
        {
            service.DeleteSlotAsync(slot.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException)
        {
            deleteBlocked = true;
        }
        Assert(deleteBlocked, "Deleting slot blocked when active reservation exists");

        // 2. Complete reservation -> Deletion allowed
        res.Status = ReservationStatus.Completed;
        context.SaveChanges();

        bool deleteSuccess = service.DeleteSlotAsync(slot.Id).GetAwaiter().GetResult();
        Assert(deleteSuccess, "Deleting slot succeeds once reservations are completed");

        var softDeleted = context.ParkingSlots.IgnoreQueryFilters().First(s => s.Id == slot.Id);
        Assert(softDeleted.IsDeleted, "Slot record is flagged as soft-deleted (IsDeleted = true)");
    }

    private static void TestHourlyRateAndPricePerHourFallback()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[8] Hourly Rate and PricePerHour Fallback Tests:");
        Console.ResetColor();

        var (context, service) = CreateMockContextAndService();
        var area = new ParkingArea { Id = Guid.NewGuid(), Name = "Pricing Deck", Address = "Price St", City = "Metropolis", HourlyRate = 5.00m };
        context.ParkingAreas.Add(area);
        context.SaveChanges();

        // Slot without rate override -> falls back to facility rate ($5.00)
        var defaultRateSlot = service.CreateSlotAsync(new CreateParkingSlotRequest
        {
            ParkingAreaId = area.Id,
            SlotNumber = "DEF-01",
            FloorNumber = 1,
            HourlyRate = null
        }).GetAwaiter().GetResult();

        Assert(defaultRateSlot.PricePerHour == 5.00m, "Slot without override uses facility rate ($5.00)");

        // Slot with rate override ($8.50)
        var customRateSlot = service.CreateSlotAsync(new CreateParkingSlotRequest
        {
            ParkingAreaId = area.Id,
            SlotNumber = "OVR-01",
            FloorNumber = 1,
            HourlyRate = 8.50m
        }).GetAwaiter().GetResult();

        Assert(customRateSlot.PricePerHour == 8.50m, "Slot with override uses custom rate ($8.50)");
    }

    private static (ApplicationDbContext context, IParkingSlotService service) CreateMockContextAndService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        var repo = new ParkingSlotRepository(context);
        var service = new ParkingSlotService(repo, context, NullLogger<ParkingSlotService>.Instance);

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
