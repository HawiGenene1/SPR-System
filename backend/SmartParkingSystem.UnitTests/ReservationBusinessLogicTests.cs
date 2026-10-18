using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SmartParkingSystem.Application.Common.BusinessLogic;
using SmartParkingSystem.Application.Common.Security;
using SmartParkingSystem.Application.DTOs.Reservations;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;
using SmartParkingSystem.Infrastructure.Data;
using SmartParkingSystem.Infrastructure.Repositories;
using SmartParkingSystem.Infrastructure.Services;

namespace SmartParkingSystem.UnitTests;

public static class ReservationBusinessLogicTests
{
    private static int _passed = 0;
    private static int _failed = 0;

    public static int Run()
    {
        _passed = 0;
        _failed = 0;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        Console.WriteLine(" Running Reservation Business Logic Unit Tests");
        Console.WriteLine("=========================================================");
        Console.ResetColor();

        // 1. Calculator & Pricing Math
        TestDurationCalculation();
        TestPricingCalculation();
        TestPricingMinimumOneHourRule();
        TestOverlapDetectionMatrix();
        TestTimeValidationRules();

        // 2. Step 11 Business Rules
        TestSlotMustExistRule();                                        // Rule 1
        TestSlotMustBelongToSelectedParkingAreaRule();                  // Rule 2
        TestVehicleMustBelongToCurrentCustomerRule();                   // Rule 3
        TestEntryTimeBeforeExitTimeAndNotPastRules();                   // Rule 4 & 5
        TestReservationCannotOverlapExistingActiveReservation();        // Rule 6
        TestServerSideDurationAndPriceCalculationIgnoringAngularAmount(); // Rule 7, 8, 9
        TestConcurrencyPreventionOnSameSlot();                          // Rule 10 & 11
        TestReservationStatusAndSlotStateTransitions();                 // Rule 12 & 13

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        if (_failed == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" Reservation Tests Summary: {_passed} Passed, {_failed} Failed");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" Reservation Tests Summary: {_passed} Passed, {_failed} Failed");
        }
        Console.WriteLine("=========================================================");
        Console.ResetColor();

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

    private static (ApplicationDbContext context, IReservationService service) CreateMockContextAndService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        var resRepo = new ReservationRepository(context);
        var vehRepo = new VehicleRepository(context);
        var vehService = new VehicleService(vehRepo, context, NullLogger<VehicleService>.Instance);
        var resService = new ReservationService(context, resRepo, vehService, NullLogger<ReservationService>.Instance);

        return (context, resService);
    }

    private static (ParkingArea area, ParkingSlot slot, User customer, Vehicle vehicle) SeedBaseData(ApplicationDbContext context)
    {
        var area = new ParkingArea
        {
            Id = Guid.NewGuid(),
            Name = "Plaza One",
            Address = "100 Main St",
            City = "Metropolis",
            TotalSlots = 20,
            HourlyRate = 5.00m,
            IsActive = true
        };
        context.ParkingAreas.Add(area);

        var slot = new ParkingSlot
        {
            Id = Guid.NewGuid(),
            ParkingAreaId = area.Id,
            SlotNumber = "A-01",
            FloorNumber = 1,
            HourlyRate = 6.00m,
            Status = SlotStatus.Available,
            VehicleType = VehicleType.Car,
            IsActive = true
        };
        context.ParkingSlots.Add(slot);

        var customerRole = new Role { Id = Guid.NewGuid(), Name = AppRoles.Customer };
        context.Roles.Add(customerRole);

        var customer = new User
        {
            Id = Guid.NewGuid(),
            FullName = "John Tester",
            Email = "john@tester.com",
            PasswordHash = "hashed",
            RoleId = customerRole.Id,
            Role = customerRole
        };
        context.Users.Add(customer);

        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            LicensePlate = "ABC-1234",
            Make = "Toyota",
            Model = "Corolla",
            VehicleType = VehicleType.Car,
            IsDeleted = false
        };
        context.Vehicles.Add(vehicle);

        context.SaveChanges();
        return (area, slot, customer, vehicle);
    }

    private static void TestDurationCalculation()
    {
        Console.WriteLine("\n[1] Duration Calculation Tests (Rule 7):");
        var start = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc);
        
        var duration2h = ReservationPricingCalculator.CalculateDurationHours(start, start.AddHours(2));
        Assert(duration2h == 2.0m, "Exact 2 hours duration equals 2.0m");

        var duration90m = ReservationPricingCalculator.CalculateDurationHours(start, start.AddMinutes(90));
        Assert(duration90m == 1.5m, "90 minutes duration equals 1.5m");

        var invalidDuration = ReservationPricingCalculator.CalculateDurationHours(start, start.AddHours(-1));
        Assert(invalidDuration == 0m, "End time before start time yields 0 duration");
    }

    private static void TestPricingCalculation()
    {
        Console.WriteLine("\n[2] Pricing Calculation Tests (Rule 8):");
        var start = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc);
        decimal hourlyRate = 6.00m;

        var price2h = ReservationPricingCalculator.CalculateTotalPrice(hourlyRate, start, start.AddHours(2));
        Assert(price2h == 12.00m, "2 hours at $6.00/hr yields $12.00");

        var price2_5h = ReservationPricingCalculator.CalculateTotalPrice(hourlyRate, start, start.AddMinutes(150));
        Assert(price2_5h == 15.00m, "2.5 hours at $6.00/hr yields $15.00");

        var priceZeroRate = ReservationPricingCalculator.CalculateTotalPrice(0m, start, start.AddHours(2));
        Assert(priceZeroRate == 0m, "Zero rate yields $0.00");
    }

    private static void TestPricingMinimumOneHourRule()
    {
        Console.WriteLine("\n[3] Minimum 1-Hour Billing Rule Tests (Rule 8 & 9):");
        var start = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc);
        decimal hourlyRate = 5.00m;

        var price20m = ReservationPricingCalculator.CalculateTotalPrice(hourlyRate, start, start.AddMinutes(20));
        Assert(price20m == 5.00m, "20-minute reservation is billed at 1 hour minimum ($5.00)");

        var price45m = ReservationPricingCalculator.CalculateTotalPrice(hourlyRate, start, start.AddMinutes(45));
        Assert(price45m == 5.00m, "45-minute reservation is billed at 1 hour minimum ($5.00)");
    }

    private static void TestOverlapDetectionMatrix()
    {
        Console.WriteLine("\n[4] Overlap Detection Matrix Tests (Rule 6):");
        var bStart = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc);
        var bEnd = new DateTime(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc);

        Assert(ReservationPricingCalculator.IsOverlapping(bStart, bEnd, bStart, bEnd),
            "Exact match interval [10:00, 12:00] overlaps");
        Assert(ReservationPricingCalculator.IsOverlapping(bStart.AddMinutes(30), bEnd.AddMinutes(-30), bStart, bEnd),
            "Contained interval [10:30, 11:30] overlaps");
        Assert(ReservationPricingCalculator.IsOverlapping(bStart.AddHours(-1), bStart.AddMinutes(30), bStart, bEnd),
            "Interval overlapping front [09:00, 10:30] overlaps");
        Assert(ReservationPricingCalculator.IsOverlapping(bEnd.AddMinutes(-30), bEnd.AddHours(1), bStart, bEnd),
            "Interval overlapping rear [11:30, 13:00] overlaps");
        Assert(ReservationPricingCalculator.IsOverlapping(bStart.AddHours(-1), bEnd.AddHours(1), bStart, bEnd),
            "Enclosing interval [09:00, 13:00] overlaps");
        Assert(!ReservationPricingCalculator.IsOverlapping(bStart.AddHours(-2), bStart, bStart, bEnd),
            "Adjacent interval before [08:00, 10:00] does NOT overlap");
        Assert(!ReservationPricingCalculator.IsOverlapping(bEnd, bEnd.AddHours(2), bStart, bEnd),
            "Adjacent interval after [12:00, 14:00] does NOT overlap");
        Assert(!ReservationPricingCalculator.IsOverlapping(bEnd.AddHours(2), bEnd.AddHours(4), bStart, bEnd),
            "Completely disjoint interval [14:00, 16:00] does NOT overlap");
    }

    private static void TestTimeValidationRules()
    {
        Console.WriteLine("\n[5] Time Validation Rules Tests (Rule 4 & 5):");
        var now = DateTime.UtcNow;

        var valid = ReservationPricingCalculator.ValidateReservationTimes(now.AddHours(1), now.AddHours(3), now);
        Assert(valid.IsValid, "Valid future reservation time passes validation");

        var past = ReservationPricingCalculator.ValidateReservationTimes(now.AddMinutes(-20), now.AddHours(1), now);
        Assert(!past.IsValid && past.ErrorMessage!.Contains("past"), "Past start time (> 5m ago) is rejected");

        var endBeforeStart = ReservationPricingCalculator.ValidateReservationTimes(now.AddHours(2), now.AddHours(1), now);
        Assert(!endBeforeStart.IsValid && endBeforeStart.ErrorMessage!.Contains("after start time"), "End time before start time is rejected");

        var equalTimes = ReservationPricingCalculator.ValidateReservationTimes(now.AddHours(1), now.AddHours(1), now);
        Assert(!equalTimes.IsValid, "Equal start and end time is rejected");

        var tooShort = ReservationPricingCalculator.ValidateReservationTimes(now.AddHours(1), now.AddHours(1).AddMinutes(10), now);
        Assert(!tooShort.IsValid && tooShort.ErrorMessage!.Contains("15 minutes"), "Duration under 15 minutes is rejected");
    }

    private static void TestSlotMustExistRule()
    {
        Console.WriteLine("\n[6] Rule 1: Slot Must Exist & Not Under Maintenance:");
        var (context, service) = CreateMockContextAndService();
        var (_, slot, customer, vehicle) = SeedBaseData(context);

        // 1. Non-existent slot ID throws KeyNotFoundException
        bool nonExistentSlotCaught = false;
        try
        {
            service.CreateReservationAsync(new CreateReservationRequest
            {
                ParkingSlotId = Guid.NewGuid(),
                VehicleId = vehicle.Id,
                StartTime = DateTime.UtcNow.AddHours(1),
                EndTime = DateTime.UtcNow.AddHours(3)
            }, customer.Id).GetAwaiter().GetResult();
        }
        catch (KeyNotFoundException)
        {
            nonExistentSlotCaught = true;
        }
        Assert(nonExistentSlotCaught, "Non-existent slot ID throws KeyNotFoundException");

        // 2. Slot in Maintenance status cannot be reserved
        slot.Status = SlotStatus.Maintenance;
        context.SaveChanges();

        bool maintenanceSlotCaught = false;
        try
        {
            service.CreateReservationAsync(new CreateReservationRequest
            {
                ParkingSlotId = slot.Id,
                VehicleId = vehicle.Id,
                StartTime = DateTime.UtcNow.AddHours(1),
                EndTime = DateTime.UtcNow.AddHours(3)
            }, customer.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            maintenanceSlotCaught = ex.Message.Contains("maintenance");
        }
        Assert(maintenanceSlotCaught, "Slot in Maintenance throws InvalidOperationException");
    }

    private static void TestSlotMustBelongToSelectedParkingAreaRule()
    {
        Console.WriteLine("\n[7] Rule 2: Slot Must Belong to Selected Parking Area:");
        var (context, service) = CreateMockContextAndService();
        var (_, slot, customer, vehicle) = SeedBaseData(context);

        var differentArea = new ParkingArea
        {
            Id = Guid.NewGuid(),
            Name = "Plaza Two",
            Address = "200 South St",
            City = "Metropolis",
            HourlyRate = 8.00m
        };
        context.ParkingAreas.Add(differentArea);
        context.SaveChanges();

        bool mismatchedAreaCaught = false;
        try
        {
            service.CreateReservationAsync(new CreateReservationRequest
            {
                ParkingSlotId = slot.Id,
                ParkingAreaId = differentArea.Id, // Mismatched area ID
                VehicleId = vehicle.Id,
                StartTime = DateTime.UtcNow.AddHours(1),
                EndTime = DateTime.UtcNow.AddHours(3)
            }, customer.Id).GetAwaiter().GetResult();
        }
        catch (ArgumentException ex)
        {
            mismatchedAreaCaught = ex.Message.Contains("does not belong to the selected parking area");
        }
        Assert(mismatchedAreaCaught, "Slot belonging to different area throws ArgumentException");
    }

    private static void TestVehicleMustBelongToCurrentCustomerRule()
    {
        Console.WriteLine("\n[8] Rule 3: Vehicle Must Belong to Current Customer:");
        var (context, service) = CreateMockContextAndService();
        var (_, slot, customer, vehicle) = SeedBaseData(context);

        var otherRole = context.Roles.FirstOrDefault(r => r.Name == AppRoles.Customer)
            ?? new Role { Id = Guid.NewGuid(), Name = AppRoles.Customer };
        if (!context.Roles.Any(r => r.Id == otherRole.Id)) context.Roles.Add(otherRole);

        var otherCustomer = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Other User",
            Email = "other@user.com",
            RoleId = otherRole.Id,
            Role = otherRole
        };
        context.Users.Add(otherCustomer);
        context.SaveChanges();

        // Customer attempts to reserve with other customer's vehicle
        bool invalidOwnershipCaught = false;
        try
        {
            service.CreateReservationAsync(new CreateReservationRequest
            {
                ParkingSlotId = slot.Id,
                VehicleId = vehicle.Id, // Owned by 'customer', not 'otherCustomer'
                StartTime = DateTime.UtcNow.AddHours(1),
                EndTime = DateTime.UtcNow.AddHours(3)
            }, otherCustomer.Id).GetAwaiter().GetResult();
        }
        catch (ArgumentException ex)
        {
            invalidOwnershipCaught = ex.Message.Contains("Vehicle does not belong to the current customer");
        }
        Assert(invalidOwnershipCaught, "Reserving with another user's vehicle rejected with ArgumentException");
    }

    private static void TestEntryTimeBeforeExitTimeAndNotPastRules()
    {
        Console.WriteLine("\n[9] Rule 4 & 5: Entry Time Validation (Before Exit & Not In Past):");
        var (context, service) = CreateMockContextAndService();
        var (_, slot, customer, vehicle) = SeedBaseData(context);

        // 1. Entry time in past (> 5 min ago)
        bool pastTimeCaught = false;
        try
        {
            service.CreateReservationAsync(new CreateReservationRequest
            {
                ParkingSlotId = slot.Id,
                VehicleId = vehicle.Id,
                StartTime = DateTime.UtcNow.AddMinutes(-30),
                EndTime = DateTime.UtcNow.AddHours(2)
            }, customer.Id).GetAwaiter().GetResult();
        }
        catch (ArgumentException ex)
        {
            pastTimeCaught = ex.Message.Contains("past");
        }
        Assert(pastTimeCaught, "Start time in past rejected with ArgumentException");

        // 2. Exit time before entry time
        bool exitBeforeEntryCaught = false;
        try
        {
            service.CreateReservationAsync(new CreateReservationRequest
            {
                ParkingSlotId = slot.Id,
                VehicleId = vehicle.Id,
                StartTime = DateTime.UtcNow.AddHours(3),
                EndTime = DateTime.UtcNow.AddHours(1)
            }, customer.Id).GetAwaiter().GetResult();
        }
        catch (ArgumentException ex)
        {
            exitBeforeEntryCaught = ex.Message.Contains("before exit time") || ex.Message.Contains("after start time");
        }
        Assert(exitBeforeEntryCaught, "Exit time before entry time rejected with ArgumentException");
    }

    private static void TestReservationCannotOverlapExistingActiveReservation()
    {
        Console.WriteLine("\n[10] Rule 6: Overlap Guard Against Existing Active Bookings:");
        var (context, service) = CreateMockContextAndService();
        var (_, slot, customer, vehicle) = SeedBaseData(context);

        var start = DateTime.UtcNow.AddHours(2);
        var end = DateTime.UtcNow.AddHours(4);

        // 1. Create first valid reservation [2h to 4h]
        var res1 = service.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = slot.Id,
            VehicleId = vehicle.Id,
            StartTime = start,
            EndTime = end
        }, customer.Id).GetAwaiter().GetResult();
        Assert(res1.Status == ReservationStatus.Confirmed, "Initial reservation Confirmed [2h to 4h]");

        // 2. Overlapping attempt [2.5h to 3.5h] inside existing window
        bool overlapInsideCaught = false;
        try
        {
            service.CreateReservationAsync(new CreateReservationRequest
            {
                ParkingSlotId = slot.Id,
                VehicleId = vehicle.Id,
                StartTime = start.AddMinutes(30),
                EndTime = end.AddMinutes(-30)
            }, customer.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            overlapInsideCaught = ex.Message.Contains("already reserved");
        }
        Assert(overlapInsideCaught, "Overlapping reservation inside window rejected with InvalidOperationException");

        // 3. Adjacent window [4h to 6h] must SUCCEED (end1 == start2 does not overlap)
        var resAdjacent = service.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = slot.Id,
            VehicleId = vehicle.Id,
            StartTime = end,
            EndTime = end.AddHours(2)
        }, customer.Id).GetAwaiter().GetResult();
        Assert(resAdjacent.Status == ReservationStatus.Confirmed, "Adjacent reservation [4h to 6h] successfully created");
    }

    private static void TestServerSideDurationAndPriceCalculationIgnoringAngularAmount()
    {
        Console.WriteLine("\n[11] Rule 7, 8, 9: Server-Side Calculation & Untrusted Client Amount:");
        var (context, service) = CreateMockContextAndService();
        var (_, slot, customer, vehicle) = SeedBaseData(context);

        // Slot rate is $6.00/hr. 2 hours reservation = $12.00
        var start = DateTime.UtcNow.AddHours(1);
        var end = DateTime.UtcNow.AddHours(3);

        // Angular sends fraudulent amount $0.50 and TotalPrice $1.00
        var res = service.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = slot.Id,
            VehicleId = vehicle.Id,
            StartTime = start,
            EndTime = end,
            Amount = 0.50m,
            TotalPrice = 1.00m
        }, customer.Id).GetAwaiter().GetResult();

        Assert(res.DurationHours == 2.0m, "Server calculated DurationHours = 2.0h");
        Assert(res.DurationMinutes == 120, "Server calculated DurationMinutes = 120m");
        Assert(res.TotalPrice == 12.00m, "Server strictly enforced TotalPrice = $12.00 (ignored client $0.50 / $1.00)");
    }

    private static void TestConcurrencyPreventionOnSameSlot()
    {
        Console.WriteLine("\n[12] Rule 10 & 11: Concurrency & Race Condition Prevention:");
        var (context, service) = CreateMockContextAndService();
        var (_, slot, customer, vehicle) = SeedBaseData(context);

        var roleB = context.Roles.FirstOrDefault(r => r.Name == AppRoles.Customer)
            ?? new Role { Id = Guid.NewGuid(), Name = AppRoles.Customer };
        if (!context.Roles.Any(r => r.Id == roleB.Id)) context.Roles.Add(roleB);

        var customerB = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Jane Rival",
            Email = "jane@rival.com",
            RoleId = roleB.Id,
            Role = roleB
        };
        context.Users.Add(customerB);
        var vehicleB = new Vehicle
        {
            Id = Guid.NewGuid(),
            UserId = customerB.Id,
            LicensePlate = "XYZ-9999",
            Make = "Honda",
            Model = "Civic",
            VehicleType = VehicleType.Car
        };
        context.Vehicles.Add(vehicleB);
        context.SaveChanges();

        var startTime = DateTime.UtcNow.AddHours(5);
        var endTime = DateTime.UtcNow.AddHours(7);

        // Two concurrent reservation requests targeting the EXACT same slot and time window
        var task1 = Task.Run(() =>
        {
            try
            {
                return (Success: true, Res: service.CreateReservationAsync(new CreateReservationRequest
                {
                    ParkingSlotId = slot.Id,
                    VehicleId = vehicle.Id,
                    StartTime = startTime,
                    EndTime = endTime
                }, customer.Id).GetAwaiter().GetResult(), Error: (string?)null);
            }
            catch (Exception ex)
            {
                return (Success: false, Res: (ReservationDto?)null, Error: ex.Message);
            }
        });

        var task2 = Task.Run(() =>
        {
            try
            {
                return (Success: true, Res: service.CreateReservationAsync(new CreateReservationRequest
                {
                    ParkingSlotId = slot.Id,
                    VehicleId = vehicleB.Id,
                    StartTime = startTime,
                    EndTime = endTime
                }, customerB.Id).GetAwaiter().GetResult(), Error: (string?)null);
            }
            catch (Exception ex)
            {
                return (Success: false, Res: (ReservationDto?)null, Error: ex.Message);
            }
        });

        Task.WaitAll(task1, task2);
        var result1 = task1.Result;
        var result2 = task2.Result;

        int successCount = (result1.Success ? 1 : 0) + (result2.Success ? 1 : 0);
        int failureCount = (!result1.Success ? 1 : 0) + (!result2.Success ? 1 : 0);

        Assert(successCount == 1, "Exactly one concurrent reservation succeeded");
        Assert(failureCount == 1, "The competing concurrent reservation was rejected with overlap conflict");
    }

    private static void TestReservationStatusAndSlotStateTransitions()
    {
        Console.WriteLine("\n[13] Rule 12 & 13: Reservation Status & Slot State Transitions:");
        var (context, service) = CreateMockContextAndService();
        var (_, slot, customer, vehicle) = SeedBaseData(context);

        // 1. Immediate reservation (starts within 5 minutes)
        var startImmediate = DateTime.UtcNow.AddMinutes(5);
        var endImmediate = DateTime.UtcNow.AddHours(2);

        var immediateRes = service.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = slot.Id,
            VehicleId = vehicle.Id,
            StartTime = startImmediate,
            EndTime = endImmediate
        }, customer.Id).GetAwaiter().GetResult();

        Assert(immediateRes.Status == ReservationStatus.Confirmed, "Reservation status set to Confirmed");
        var updatedSlot = context.ParkingSlots.Find(slot.Id)!;
        Assert(updatedSlot.Status == SlotStatus.Reserved, "Slot status transitioned to Reserved for upcoming session");

        // 2. Customer cancels reservation
        var cancelledRes = service.CancelReservationAsync(
            immediateRes.Id, customer.Id, "Changed plans", false).GetAwaiter().GetResult();
        Assert(cancelledRes.Status == ReservationStatus.Cancelled, "Reservation status updated to Cancelled");
        context.Entry(updatedSlot).Reload();
        Assert(updatedSlot.Status == SlotStatus.Available, "Slot released back to Available upon cancellation");

        // 3. Admin status updates (Active -> Occupied, Completed -> Available)
        var futureRes = service.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = slot.Id,
            VehicleId = vehicle.Id,
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(3)
        }, customer.Id).GetAwaiter().GetResult();

        // Admin marks Active (Vehicle arrived)
        var activeRes = service.UpdateReservationStatusAsync(
            futureRes.Id, ReservationStatus.Active).GetAwaiter().GetResult();
        Assert(activeRes.Status == ReservationStatus.Active, "Admin marked reservation Active");
        context.Entry(updatedSlot).Reload();
        Assert(updatedSlot.Status == SlotStatus.Occupied, "Slot automatically transitioned to Occupied");

        // Admin marks Completed (Vehicle departed)
        var completedRes = service.UpdateReservationStatusAsync(
            futureRes.Id, ReservationStatus.Completed).GetAwaiter().GetResult();
        Assert(completedRes.Status == ReservationStatus.Completed, "Admin marked reservation Completed");
        context.Entry(updatedSlot).Reload();
        Assert(updatedSlot.Status == SlotStatus.Available, "Slot automatically transitioned back to Available");
    }
}
