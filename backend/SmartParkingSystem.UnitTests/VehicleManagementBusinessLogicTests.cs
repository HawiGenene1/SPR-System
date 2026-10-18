using SmartParkingSystem.Application.Common.Validation;
using SmartParkingSystem.Application.DTOs.Vehicles;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;
using SmartParkingSystem.Infrastructure.Data;
using SmartParkingSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace SmartParkingSystem.UnitTests;

public static class VehicleManagementBusinessLogicTests
{
    private static int _passed;
    private static int _failed;

    public static int Run()
    {
        _passed = 0;
        _failed = 0;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        Console.WriteLine(" Running Vehicle Management Business Logic Unit Tests");
        Console.WriteLine("=========================================================");
        Console.ResetColor();

        RunPlateValidationTests();
        RunVehicleTypeValidationTests();
        RunPlateNormalizationTests();
        RunOwnershipAndAccessTests();
        RunReservationProtectionTests();
        RunDuplicatePlatePreventionTests();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        if (_failed == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" Vehicle Management Tests Summary: {_passed} Passed, {_failed} Failed");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" Vehicle Management Tests Summary: {_passed} Passed, {_failed} Failed");
        }
        Console.WriteLine("=========================================================");
        Console.ResetColor();

        return _failed == 0 ? 0 : 1;
    }

    private static void RunPlateValidationTests()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[1] License Plate Validation Tests:");
        Console.ResetColor();

        // Valid plate formats
        Assert(VehicleValidators.IsValidPlate("ABC-123"), "Standard hyphenated plate 'ABC-123' is valid");
        Assert(VehicleValidators.IsValidPlate("NY 1234"), "Spaced plate 'NY 1234' is valid");
        Assert(VehicleValidators.IsValidPlate("7ABC123"), "Compact alphanumeric '7ABC123' is valid");
        Assert(VehicleValidators.IsValidPlate("2-12345 AA"), "International format '2-12345 AA' is valid");
        Assert(VehicleValidators.IsValidPlate("AB"), "Minimum 2-character plate 'AB' is valid");

        // Invalid plate formats
        Assert(!VehicleValidators.IsValidPlate(""), "Empty plate is invalid");
        Assert(!VehicleValidators.IsValidPlate("   "), "Whitespace plate is invalid");
        Assert(!VehicleValidators.IsValidPlate("A"), "Single character plate 'A' is invalid");
        Assert(!VehicleValidators.IsValidPlate("---"), "Punctuation-only plate '---' is invalid");
        Assert(!VehicleValidators.IsValidPlate("- -"), "Spaces and dashes '- -' is invalid");
        Assert(!VehicleValidators.IsValidPlate("ABC@123"), "Special character plate 'ABC@123' is invalid");
        Assert(!VehicleValidators.IsValidPlate("ABC_123"), "Underscore plate 'ABC_123' is invalid");
        Assert(!VehicleValidators.IsValidPlate("-ABC123"), "Leading hyphen '-ABC123' is invalid");
        Assert(!VehicleValidators.IsValidPlate("ABC123-"), "Trailing hyphen 'ABC123-' is invalid");
    }

    private static void RunVehicleTypeValidationTests()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[2] Vehicle Type Enum Validation Tests:");
        Console.ResetColor();

        Assert(VehicleValidators.IsValidVehicleType(VehicleType.Car), "VehicleType.Car is valid");
        Assert(VehicleValidators.IsValidVehicleType(VehicleType.Motorcycle), "VehicleType.Motorcycle is valid");
        Assert(VehicleValidators.IsValidVehicleType(VehicleType.SUV), "VehicleType.SUV is valid");
        Assert(VehicleValidators.IsValidVehicleType(VehicleType.Truck), "VehicleType.Truck is valid");
        Assert(VehicleValidators.IsValidVehicleType(VehicleType.ElectricVehicle), "VehicleType.ElectricVehicle is valid");

        Assert(!VehicleValidators.IsValidVehicleType((VehicleType)0), "VehicleType 0 is invalid");
        Assert(!VehicleValidators.IsValidVehicleType((VehicleType)99), "VehicleType 99 is invalid");
    }

    private static void RunPlateNormalizationTests()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[3] Plate Normalization Tests:");
        Console.ResetColor();

        Assert(VehicleValidators.NormalizePlate("abc-123") == "ABC-123", "Lower case normalized to uppercase: 'abc-123' -> 'ABC-123'");
        Assert(VehicleValidators.NormalizePlate("abc 123") == "ABC-123", "Spaces converted to single hyphen: 'abc 123' -> 'ABC-123'");
        Assert(VehicleValidators.NormalizePlate("  CA  123-xyz  ") == "CA-123-XYZ", "Trim and consecutive spaces normalized to hyphen: 'CA-123-XYZ'");
        Assert(VehicleValidators.NormalizePlate("") == string.Empty, "Empty plate returns empty string");
    }

    private static void RunOwnershipAndAccessTests()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[4] Customer Ownership and Access Control Isolation Tests:");
        Console.ResetColor();

        var customerAId = Guid.NewGuid();
        var customerBId = Guid.NewGuid();
        var vehicleAId = Guid.NewGuid();

        var (context, service) = CreateMockContextAndService();

        // Seed customer A's vehicle
        var vehicleA = new Vehicle
        {
            Id = vehicleAId,
            UserId = customerAId,
            LicensePlate = "OWNER-01",
            VehicleType = VehicleType.Car,
            Make = "Honda",
            Model = "Civic",
            IsDeleted = false
        };
        context.Vehicles.Add(vehicleA);
        context.Users.Add(new User { Id = customerAId, FullName = "Customer A", Email = "a@test.com", PasswordHash = "hash" });
        context.Users.Add(new User { Id = customerBId, FullName = "Customer B", Email = "b@test.com", PasswordHash = "hash" });
        context.SaveChanges();

        // Customer A accessing their own vehicle succeeds
        var retrievedA = service.GetVehicleByIdAsync(vehicleAId, customerAId, isAdmin: false).GetAwaiter().GetResult();
        Assert(retrievedA.Id == vehicleAId, "Customer A successfully accesses their own vehicle");

        // Customer B attempting to access Customer A's vehicle throws UnauthorizedAccessException
        bool unauthorizedAccessBlocked = false;
        try
        {
            service.GetVehicleByIdAsync(vehicleAId, customerBId, isAdmin: false).GetAwaiter().GetResult();
        }
        catch (UnauthorizedAccessException)
        {
            unauthorizedAccessBlocked = true;
        }
        Assert(unauthorizedAccessBlocked, "Customer B is blocked with UnauthorizedAccessException when accessing Customer A's vehicle");

        // Customer B attempting to update Customer A's vehicle throws UnauthorizedAccessException
        bool unauthorizedUpdateBlocked = false;
        try
        {
            service.UpdateVehicleAsync(vehicleAId, customerBId, new UpdateVehicleRequest
            {
                LicensePlate = "HACKED-01",
                VehicleType = VehicleType.Car,
                Make = "Honda",
                Model = "Civic"
            }, isAdmin: false).GetAwaiter().GetResult();
        }
        catch (UnauthorizedAccessException)
        {
            unauthorizedUpdateBlocked = true;
        }
        Assert(unauthorizedUpdateBlocked, "Customer B is blocked with UnauthorizedAccessException when attempting to update Customer A's vehicle");

        // Customer B attempting to delete Customer A's vehicle throws UnauthorizedAccessException
        bool unauthorizedDeleteBlocked = false;
        try
        {
            service.DeleteVehicleAsync(vehicleAId, customerBId, isAdmin: false).GetAwaiter().GetResult();
        }
        catch (UnauthorizedAccessException)
        {
            unauthorizedDeleteBlocked = true;
        }
        Assert(unauthorizedDeleteBlocked, "Customer B is blocked with UnauthorizedAccessException when attempting to delete Customer A's vehicle");

        // Admin can access vehicle
        var adminRetrieved = service.GetVehicleByIdAsync(vehicleAId, Guid.NewGuid(), isAdmin: true).GetAwaiter().GetResult();
        Assert(adminRetrieved.Id == vehicleAId, "Admin can access any customer's vehicle");
    }

    private static void RunReservationProtectionTests()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[5] Reservation Assignment & Active Reservation Guards Tests:");
        Console.ResetColor();

        var customerAId = Guid.NewGuid();
        var customerBId = Guid.NewGuid();
        var vehicleAId = Guid.NewGuid();

        var (context, service) = CreateMockContextAndService();

        var vehicleA = new Vehicle
        {
            Id = vehicleAId,
            UserId = customerAId,
            LicensePlate = "RES-001",
            VehicleType = VehicleType.SUV,
            Make = "Ford",
            Model = "Explorer",
            IsDeleted = false
        };
        context.Vehicles.Add(vehicleA);
        context.Users.Add(new User { Id = customerAId, FullName = "Customer A", Email = "a2@test.com", PasswordHash = "hash" });
        context.SaveChanges();

        // 1. Vehicle ownership validation for reservation
        bool validForOwner = service.ValidateVehicleOwnershipForReservationAsync(vehicleAId, customerAId).GetAwaiter().GetResult();
        Assert(validForOwner, "Vehicle ownership validation succeeds for the real owner");

        bool validForOther = service.ValidateVehicleOwnershipForReservationAsync(vehicleAId, customerBId).GetAwaiter().GetResult();
        Assert(!validForOther, "Vehicle cannot be assigned to another user's reservation (returns false)");

        // 2. Active reservation prevents deletion
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            UserId = customerAId,
            VehicleId = vehicleAId,
            ParkingSlotId = Guid.NewGuid(),
            ReservationNumber = "RES-TEST-001",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(2),
            TotalPrice = 10.00m,
            Status = ReservationStatus.Confirmed,
            IsDeleted = false
        };
        context.Reservations.Add(reservation);
        context.SaveChanges();

        bool deleteBlockedWithActiveReservation = false;
        try
        {
            service.DeleteVehicleAsync(vehicleAId, customerAId, isAdmin: false).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException)
        {
            deleteBlockedWithActiveReservation = true;
        }
        Assert(deleteBlockedWithActiveReservation, "Vehicle deletion is blocked when active/confirmed reservation exists");

        // 3. Plate update blocked when active reservation exists
        bool plateUpdateBlocked = false;
        try
        {
            service.UpdateVehicleAsync(vehicleAId, customerAId, new UpdateVehicleRequest
            {
                LicensePlate = "RES-NEW-PLATE",
                VehicleType = VehicleType.SUV,
                Make = "Ford",
                Model = "Explorer"
            }, isAdmin: false).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException)
        {
            plateUpdateBlocked = true;
        }
        Assert(plateUpdateBlocked, "License plate modification is blocked while vehicle is assigned to an active reservation");

        // 4. Once reservation is completed, deletion succeeds
        reservation.Status = ReservationStatus.Completed;
        context.SaveChanges();

        bool deleteSuccess = service.DeleteVehicleAsync(vehicleAId, customerAId, isAdmin: false).GetAwaiter().GetResult();
        Assert(deleteSuccess, "Vehicle deletion succeeds once reservations are completed");

        // Check vehicle is soft-deleted and filtered by global query filter
        var deletedVehicle = context.Vehicles.IgnoreQueryFilters().First(v => v.Id == vehicleAId);
        Assert(deletedVehicle.IsDeleted, "Vehicle record is flagged as soft-deleted (IsDeleted = true)");

        var activeQueryHidesDeleted = context.Vehicles.FirstOrDefault(v => v.Id == vehicleAId) == null;
        Assert(activeQueryHidesDeleted, "Global query filter successfully hides soft-deleted vehicle from active queries");
    }

    private static void RunDuplicatePlatePreventionTests()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[6] Duplicate License Plate Prevention Tests:");
        Console.ResetColor();

        var customerId = Guid.NewGuid();
        var (context, service) = CreateMockContextAndService();

        context.Users.Add(new User { Id = customerId, FullName = "Duplicate Test User", Email = "dup@test.com", PasswordHash = "hash" });
        context.SaveChanges();

        // Register initial vehicle
        var first = service.CreateVehicleAsync(customerId, new CreateVehicleRequest
        {
            LicensePlate = "DUP-999",
            VehicleType = VehicleType.Car,
            Make = "Toyota",
            Model = "Corolla"
        }).GetAwaiter().GetResult();

        Assert(first.LicensePlate == "DUP-999", "First vehicle registered with plate 'DUP-999'");

        // Register second vehicle with identical plate throws InvalidOperationException
        bool duplicateRejected = false;
        try
        {
            service.CreateVehicleAsync(customerId, new CreateVehicleRequest
            {
                LicensePlate = "dup 999", // different case and space
                VehicleType = VehicleType.ElectricVehicle,
                Make = "Tesla",
                Model = "Model 3"
            }).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException)
        {
            duplicateRejected = true;
        }
        Assert(duplicateRejected, "Registering duplicate plate 'dup 999' is rejected by uniqueness check");
    }

    private static (ApplicationDbContext context, IVehicleService service) CreateMockContextAndService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        var repo = new SmartParkingSystem.Infrastructure.Repositories.VehicleRepository(context);
        var service = new VehicleService(repo, context, NullLogger<VehicleService>.Instance);

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
