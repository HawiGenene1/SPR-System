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

public static class CustomerDashboardBusinessLogicTests
{
    private static int _passed = 0;
    private static int _failed = 0;

    public static int Run()
    {
        _passed = 0;
        _failed = 0;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        Console.WriteLine(" Running Step 19: Customer Dashboard Unit Tests          ");
        Console.WriteLine("=========================================================");
        Console.ResetColor();

        TestCustomerDataIsolation();
        TestActiveAndUpcomingReservationResolution();
        TestNoActiveOrUpcomingReservations();
        TestFinancialMetricsCalculation();
        TestRegisteredVehiclesListAndSoftDelete();
        TestRecentParkingHistoryDetails();
        TestNonExistentCustomer();
        TestSoftDeletedReservationsExcluded();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        if (_failed == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" Step 19 Customer Dashboard Tests: {_passed} Passed, {_failed} Failed");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" Step 19 Customer Dashboard Tests: {_passed} Passed, {_failed} Failed");
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

    private static void TestCustomerDataIsolation()
    {
        Console.WriteLine("\n[1] Customer Data Isolation Tests:");
        var (context, service) = CreateMockContextAndService();

        var area = new ParkingArea { Id = Guid.NewGuid(), Name = "Central Plaza", Address = "100 Market St", City = "Metro", HourlyRate = 5, TotalSlots = 10, IsActive = true };
        context.ParkingAreas.Add(area);

        var slotA = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "A-01", Status = SlotStatus.Occupied };
        var slotB = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "B-01", Status = SlotStatus.Available };
        context.ParkingSlots.AddRange(slotA, slotB);

        var customerA = new User { Id = Guid.NewGuid(), FullName = "Alice Driver", Email = "alice@example.com", PhoneNumber = "111", PasswordHash = "h" };
        var customerB = new User { Id = Guid.NewGuid(), FullName = "Bob Commuter", Email = "bob@example.com", PhoneNumber = "222", PasswordHash = "h" };
        context.Users.AddRange(customerA, customerB);

        var vehicleA = new Vehicle { Id = Guid.NewGuid(), UserId = customerA.Id, LicensePlate = "ALICE-1", Make = "Tesla", Model = "Model 3", VehicleType = VehicleType.ElectricVehicle };
        var vehicleB = new Vehicle { Id = Guid.NewGuid(), UserId = customerB.Id, LicensePlate = "BOB-99", Make = "Ford", Model = "F-150", VehicleType = VehicleType.Truck };
        context.Vehicles.AddRange(vehicleA, vehicleB);

        // Customer A: 1 active reservation, 1 upcoming reservation
        var resA1 = new Reservation
        {
            Id = Guid.NewGuid(),
            ReservationNumber = "RES-A1",
            UserId = customerA.Id,
            ParkingSlotId = slotA.Id,
            VehicleId = vehicleA.Id,
            StartTime = DateTime.UtcNow.AddMinutes(-30),
            EndTime = DateTime.UtcNow.AddHours(2),
            TotalPrice = 15m,
            Status = ReservationStatus.Active
        };
        var resA2 = new Reservation
        {
            Id = Guid.NewGuid(),
            ReservationNumber = "RES-A2",
            UserId = customerA.Id,
            ParkingSlotId = slotB.Id,
            VehicleId = vehicleA.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalPrice = 20m,
            Status = ReservationStatus.Confirmed
        };

        // Customer B: 1 completed reservation
        var resB1 = new Reservation
        {
            Id = Guid.NewGuid(),
            ReservationNumber = "RES-B1",
            UserId = customerB.Id,
            ParkingSlotId = slotB.Id,
            VehicleId = vehicleB.Id,
            StartTime = DateTime.UtcNow.AddDays(-2),
            EndTime = DateTime.UtcNow.AddDays(-2).AddHours(2),
            TotalPrice = 30m,
            Status = ReservationStatus.Completed
        };
        context.Reservations.AddRange(resA1, resA2, resB1);

        // Payments
        context.Payments.AddRange(
            new Payment { Id = Guid.NewGuid(), ReservationId = resA1.Id, UserId = customerA.Id, Amount = 15m, Status = PaymentStatus.Paid },
            new Payment { Id = Guid.NewGuid(), ReservationId = resA2.Id, UserId = customerA.Id, Amount = 20m, Status = PaymentStatus.Paid },
            new Payment { Id = Guid.NewGuid(), ReservationId = resB1.Id, UserId = customerB.Id, Amount = 30m, Status = PaymentStatus.Paid }
        );

        // Parking History
        context.ParkingHistories.AddRange(
            new ParkingHistory { Id = Guid.NewGuid(), ReservationId = resA1.Id, VehicleId = vehicleA.Id, ParkingSlotId = slotA.Id, CheckInTime = DateTime.UtcNow.AddMinutes(-30) },
            new ParkingHistory { Id = Guid.NewGuid(), ReservationId = resB1.Id, VehicleId = vehicleB.Id, ParkingSlotId = slotB.Id, CheckInTime = DateTime.UtcNow.AddDays(-2), CheckOutTime = DateTime.UtcNow.AddDays(-2).AddHours(2) }
        );

        context.SaveChanges();

        // Customer A Dashboard
        var dashA = service.GetCustomerDashboardAsync(customerA.Id).GetAwaiter().GetResult();
        Assert(dashA.TotalReservations == 2, "Customer A TotalReservations is strictly 2");
        Assert(dashA.ActiveReservation != null && dashA.ActiveReservation.ReservationNumber == "RES-A1", "Customer A ActiveReservation matches RES-A1");
        Assert(dashA.UpcomingReservation != null && dashA.UpcomingReservation.ReservationNumber == "RES-A2", "Customer A UpcomingReservation matches RES-A2");
        Assert(dashA.RegisteredVehicles.Count == 1 && dashA.RegisteredVehicles[0].LicensePlate == "ALICE-1", "Customer A only receives their own vehicle");
        Assert(dashA.TotalPayments == 35m, "Customer A TotalPayments strictly sums Alice's payments ($35.00)");
        Assert(dashA.RecentParkingHistory.Count == 1 && dashA.RecentParkingHistory[0].ReservationNumber == "RES-A1", "Customer A only receives their own parking history");

        // Customer B Dashboard
        var dashB = service.GetCustomerDashboardAsync(customerB.Id).GetAwaiter().GetResult();
        Assert(dashB.TotalReservations == 1, "Customer B TotalReservations is strictly 1");
        Assert(dashB.CompletedReservations == 1, "Customer B CompletedReservations is strictly 1");
        Assert(dashB.ActiveReservation == null, "Customer B has no active reservation");
        Assert(dashB.UpcomingReservation == null, "Customer B has no upcoming reservation");
        Assert(dashB.RegisteredVehicles.Count == 1 && dashB.RegisteredVehicles[0].LicensePlate == "BOB-99", "Customer B only receives Bob's vehicle");
        Assert(dashB.TotalPayments == 30m, "Customer B TotalPayments strictly sums Bob's payments ($30.00)");
        Assert(dashB.RecentParkingHistory.Count == 1 && dashB.RecentParkingHistory[0].ReservationNumber == "RES-B1", "Customer B only receives their own parking history");
    }

    private static void TestActiveAndUpcomingReservationResolution()
    {
        Console.WriteLine("\n[2] Active & Upcoming Reservation Resolution Tests:");
        var (context, service) = CreateMockContextAndService();

        var area = new ParkingArea { Id = Guid.NewGuid(), Name = "Airport Express", Address = "1 Runway Blvd", City = "Metro", HourlyRate = 8, TotalSlots = 5, IsActive = true };
        context.ParkingAreas.Add(area);

        var slot = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "EXP-10", Status = SlotStatus.Occupied };
        context.ParkingSlots.Add(slot);

        var customer = new User { Id = Guid.NewGuid(), FullName = "Claire Traveler", Email = "claire@example.com", PhoneNumber = "333", PasswordHash = "h" };
        context.Users.Add(customer);

        var vehicle = new Vehicle { Id = Guid.NewGuid(), UserId = customer.Id, LicensePlate = "FLY-777", Make = "Audi", Model = "A4", VehicleType = VehicleType.Car };
        context.Vehicles.Add(vehicle);

        // 1 Active reservation
        var activeRes = new Reservation
        {
            Id = Guid.NewGuid(),
            ReservationNumber = "ACTIVE-001",
            UserId = customer.Id,
            ParkingSlotId = slot.Id,
            VehicleId = vehicle.Id,
            StartTime = DateTime.UtcNow.AddMinutes(-45),
            EndTime = DateTime.UtcNow.AddHours(3),
            TotalPrice = 24m,
            Status = ReservationStatus.Active
        };

        // 2 Upcoming reservations: one starts tomorrow, one starts in 3 days
        var upcomingRes1 = new Reservation
        {
            Id = Guid.NewGuid(),
            ReservationNumber = "UPCOMING-EARLY",
            UserId = customer.Id,
            ParkingSlotId = slot.Id,
            VehicleId = vehicle.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalPrice = 16m,
            Status = ReservationStatus.Confirmed
        };
        var upcomingRes2 = new Reservation
        {
            Id = Guid.NewGuid(),
            ReservationNumber = "UPCOMING-LATER",
            UserId = customer.Id,
            ParkingSlotId = slot.Id,
            VehicleId = vehicle.Id,
            StartTime = DateTime.UtcNow.AddDays(3),
            EndTime = DateTime.UtcNow.AddDays(3).AddHours(2),
            TotalPrice = 16m,
            Status = ReservationStatus.Confirmed
        };

        context.Reservations.AddRange(activeRes, upcomingRes1, upcomingRes2);

        var qr = new QRCode { Id = Guid.NewGuid(), ReservationId = activeRes.Id, Code = "QR-ACT-001", ImageData = "data:image/png;base64,abc", ExpiresAt = activeRes.EndTime };
        var pay = new Payment { Id = Guid.NewGuid(), ReservationId = activeRes.Id, UserId = customer.Id, Amount = 24m, Status = PaymentStatus.Paid, TransactionId = "TXN-101" };
        context.QRCodes.Add(qr);
        context.Payments.Add(pay);

        context.SaveChanges();

        var dashboard = service.GetCustomerDashboardAsync(customer.Id).GetAwaiter().GetResult();

        Assert(dashboard.ActiveReservation != null, "ActiveReservation is resolved");
        Assert(dashboard.ActiveReservation!.ReservationNumber == "ACTIVE-001", "ActiveReservation matches ACTIVE-001");
        Assert(dashboard.ActiveReservation.SlotNumber == "EXP-10", "ActiveReservation includes SlotNumber EXP-10");
        Assert(dashboard.ActiveReservation.ParkingAreaName == "Airport Express", "ActiveReservation includes ParkingAreaName");
        Assert(dashboard.ActiveReservation.LicensePlate == "FLY-777", "ActiveReservation includes LicensePlate");
        Assert(dashboard.ActiveReservation.QRCode != null && dashboard.ActiveReservation.QRCode.Code == "QR-ACT-001", "ActiveReservation includes QRCode");
        Assert(dashboard.ActiveReservation.Payment != null && dashboard.ActiveReservation.Payment.TransactionId == "TXN-101", "ActiveReservation includes Payment details");

        Assert(dashboard.UpcomingReservation != null, "UpcomingReservation is resolved");
        Assert(dashboard.UpcomingReservation!.ReservationNumber == "UPCOMING-EARLY", "UpcomingReservation selects the earliest upcoming booking (StartTime ASC)");
        Assert(dashboard.UpcomingReservationsCount == 2, "UpcomingReservationsCount is 2");
        Assert(dashboard.ActiveReservationsCount == 1, "ActiveReservationsCount is 1");
    }

    private static void TestNoActiveOrUpcomingReservations()
    {
        Console.WriteLine("\n[3] No Active or Upcoming Reservations Tests:");
        var (context, service) = CreateMockContextAndService();

        var customer = new User { Id = Guid.NewGuid(), FullName = "Dave Inactive", Email = "dave@example.com", PhoneNumber = "444", PasswordHash = "h" };
        context.Users.Add(customer);

        var area = new ParkingArea { Id = Guid.NewGuid(), Name = "Mall Garage", Address = "2 Center St", City = "Metro", HourlyRate = 4, TotalSlots = 10, IsActive = true };
        var slot = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "M-1", Status = SlotStatus.Available };
        var vehicle = new Vehicle { Id = Guid.NewGuid(), UserId = customer.Id, LicensePlate = "DAVE-10", Make = "Honda", Model = "Civic", VehicleType = VehicleType.Car };
        context.ParkingAreas.Add(area);
        context.ParkingSlots.Add(slot);
        context.Vehicles.Add(vehicle);

        // 1 Completed and 1 Cancelled reservation
        context.Reservations.AddRange(
            new Reservation { Id = Guid.NewGuid(), ReservationNumber = "RES-COMP", UserId = customer.Id, ParkingSlotId = slot.Id, VehicleId = vehicle.Id, StartTime = DateTime.UtcNow.AddDays(-5), EndTime = DateTime.UtcNow.AddDays(-5).AddHours(2), TotalPrice = 8m, Status = ReservationStatus.Completed },
            new Reservation { Id = Guid.NewGuid(), ReservationNumber = "RES-CANC", UserId = customer.Id, ParkingSlotId = slot.Id, VehicleId = vehicle.Id, StartTime = DateTime.UtcNow.AddDays(-3), EndTime = DateTime.UtcNow.AddDays(-3).AddHours(1), TotalPrice = 4m, Status = ReservationStatus.Cancelled }
        );

        context.SaveChanges();

        var dashboard = service.GetCustomerDashboardAsync(customer.Id).GetAwaiter().GetResult();

        Assert(dashboard.ActiveReservation == null, "ActiveReservation is null when no active parking");
        Assert(dashboard.UpcomingReservation == null, "UpcomingReservation is null when no future bookings");
        Assert(dashboard.TotalReservations == 2, "TotalReservations is 2");
        Assert(dashboard.CompletedReservations == 1, "CompletedReservations is 1");
        Assert(dashboard.CancelledReservationsCount == 1, "CancelledReservationsCount is 1");
        Assert(dashboard.ActiveReservationsCount == 0, "ActiveReservationsCount is 0");
        Assert(dashboard.UpcomingReservationsCount == 0, "UpcomingReservationsCount is 0");
    }

    private static void TestFinancialMetricsCalculation()
    {
        Console.WriteLine("\n[4] Financial Metrics Calculation Tests:");
        var (context, service) = CreateMockContextAndService();

        var customer = new User { Id = Guid.NewGuid(), FullName = "Elena Spender", Email = "elena@example.com", PhoneNumber = "555", PasswordHash = "h" };
        context.Users.Add(customer);

        var area = new ParkingArea { Id = Guid.NewGuid(), Name = "City Garage", Address = "10 Main", City = "Metro", HourlyRate = 5, TotalSlots = 5, IsActive = true };
        var slot = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "C-1", Status = SlotStatus.Available };
        var vehicle = new Vehicle { Id = Guid.NewGuid(), UserId = customer.Id, LicensePlate = "ELENA-1", Make = "Nissan", Model = "Leaf", VehicleType = VehicleType.ElectricVehicle };
        context.ParkingAreas.Add(area);
        context.ParkingSlots.Add(slot);
        context.Vehicles.Add(vehicle);

        var res1 = new Reservation { Id = Guid.NewGuid(), ReservationNumber = "RES-PAY-1", UserId = customer.Id, ParkingSlotId = slot.Id, VehicleId = vehicle.Id, StartTime = DateTime.UtcNow.AddDays(-3), EndTime = DateTime.UtcNow.AddDays(-3).AddHours(2), TotalPrice = 25m, Status = ReservationStatus.Completed };
        var res2 = new Reservation { Id = Guid.NewGuid(), ReservationNumber = "RES-PAY-2", UserId = customer.Id, ParkingSlotId = slot.Id, VehicleId = vehicle.Id, StartTime = DateTime.UtcNow.AddDays(-2), EndTime = DateTime.UtcNow.AddDays(-2).AddHours(1), TotalPrice = 15.5m, Status = ReservationStatus.Completed };
        var res3 = new Reservation { Id = Guid.NewGuid(), ReservationNumber = "RES-PAY-3", UserId = customer.Id, ParkingSlotId = slot.Id, VehicleId = vehicle.Id, StartTime = DateTime.UtcNow.AddDays(-1), EndTime = DateTime.UtcNow.AddDays(-1).AddHours(1), TotalPrice = 40m, Status = ReservationStatus.Pending };
        var res4 = new Reservation { Id = Guid.NewGuid(), ReservationNumber = "RES-PAY-4", UserId = customer.Id, ParkingSlotId = slot.Id, VehicleId = vehicle.Id, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddHours(1), TotalPrice = 10m, Status = ReservationStatus.Cancelled };
        context.Reservations.AddRange(res1, res2, res3, res4);

        // Payments: 2 Paid, 1 Pending, 1 Failed
        context.Payments.AddRange(
            new Payment { Id = Guid.NewGuid(), ReservationId = res1.Id, UserId = customer.Id, Amount = 25.00m, Status = PaymentStatus.Paid },
            new Payment { Id = Guid.NewGuid(), ReservationId = res2.Id, UserId = customer.Id, Amount = 15.50m, Status = PaymentStatus.Paid },
            new Payment { Id = Guid.NewGuid(), ReservationId = res3.Id, UserId = customer.Id, Amount = 40.00m, Status = PaymentStatus.Pending },
            new Payment { Id = Guid.NewGuid(), ReservationId = res4.Id, UserId = customer.Id, Amount = 10.00m, Status = PaymentStatus.Failed }
        );

        // Overstay fee recorded in parking history: $8.50
        context.ParkingHistories.Add(new ParkingHistory
        {
            Id = Guid.NewGuid(),
            ReservationId = res1.Id,
            VehicleId = vehicle.Id,
            ParkingSlotId = slot.Id,
            CheckInTime = DateTime.UtcNow.AddDays(-3),
            CheckOutTime = DateTime.UtcNow.AddDays(-3).AddHours(3),
            OverstayDurationMinutes = 60,
            OverstayFee = 8.50m
        });

        context.SaveChanges();

        var dashboard = service.GetCustomerDashboardAsync(customer.Id).GetAwaiter().GetResult();

        // Expected: 25.00 + 15.50 + 8.50 = 49.00
        Assert(dashboard.TotalPayments == 49.00m, $"TotalPayments ({dashboard.TotalPayments}) matches sum of paid payments + overstay fee ($49.00)");
        Assert(dashboard.TotalSpent == 49.00m, "TotalSpent alias matches TotalPayments");
        Assert(dashboard.TotalPaymentsCount == 2, "TotalPaymentsCount reflects only paid payment transactions (2)");
    }

    private static void TestRegisteredVehiclesListAndSoftDelete()
    {
        Console.WriteLine("\n[5] Registered Vehicles List & Soft-Delete Tests:");
        var (context, service) = CreateMockContextAndService();

        var customer = new User { Id = Guid.NewGuid(), FullName = "Frank Fleet", Email = "frank@example.com", PhoneNumber = "666", PasswordHash = "h" };
        context.Users.Add(customer);

        var veh1 = new Vehicle { Id = Guid.NewGuid(), UserId = customer.Id, LicensePlate = "FLEET-01", Make = "Toyota", Model = "RAV4", VehicleType = VehicleType.SUV };
        var veh2 = new Vehicle { Id = Guid.NewGuid(), UserId = customer.Id, LicensePlate = "FLEET-02", Make = "Honda", Model = "CR-V", VehicleType = VehicleType.SUV };
        var vehDeleted = new Vehicle { Id = Guid.NewGuid(), UserId = customer.Id, LicensePlate = "FLEET-OLD", Make = "Dodge", Model = "Neon", VehicleType = VehicleType.Car, IsDeleted = true };
        context.Vehicles.AddRange(veh1, veh2, vehDeleted);

        context.SaveChanges();

        var dashboard = service.GetCustomerDashboardAsync(customer.Id).GetAwaiter().GetResult();

        Assert(dashboard.RegisteredVehicles.Count == 2, "RegisteredVehicles excludes soft-deleted vehicles");
        Assert(dashboard.Vehicles.Count == 2, "Vehicles alias matches RegisteredVehicles count");
        Assert(dashboard.RegisteredVehiclesCount == 2, "RegisteredVehiclesCount is 2");
        Assert(dashboard.RegisteredVehicles.Any(v => v.LicensePlate == "FLEET-01"), "RegisteredVehicles contains FLEET-01");
        Assert(dashboard.RegisteredVehicles.Any(v => v.LicensePlate == "FLEET-02"), "RegisteredVehicles contains FLEET-02");
        Assert(!dashboard.RegisteredVehicles.Any(v => v.LicensePlate == "FLEET-OLD"), "Soft-deleted FLEET-OLD is excluded");
    }

    private static void TestRecentParkingHistoryDetails()
    {
        Console.WriteLine("\n[6] Recent Parking History Details Tests:");
        var (context, service) = CreateMockContextAndService();

        var customer = new User { Id = Guid.NewGuid(), FullName = "Grace Regular", Email = "grace@example.com", PhoneNumber = "777", PasswordHash = "h" };
        var attendant = new User { Id = Guid.NewGuid(), FullName = "Officer Sam", Email = "sam@p.com", PhoneNumber = "888", PasswordHash = "h" };
        context.Users.AddRange(customer, attendant);

        var area = new ParkingArea { Id = Guid.NewGuid(), Name = "Harbor Lot", Address = "5 Bay St", City = "Metro", HourlyRate = 6, TotalSlots = 10, IsActive = true };
        var slot = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "H-05", FloorNumber = 2, Status = SlotStatus.Available };
        var vehicle = new Vehicle { Id = Guid.NewGuid(), UserId = customer.Id, LicensePlate = "GRACE-77", Make = "BMW", Model = "330i", VehicleType = VehicleType.Car };
        context.ParkingAreas.Add(area);
        context.ParkingSlots.Add(slot);
        context.Vehicles.Add(vehicle);

        // Create 6 parking history records at different times
        for (int i = 1; i <= 6; i++)
        {
            var res = new Reservation
            {
                Id = Guid.NewGuid(),
                ReservationNumber = $"RES-HIST-{i}",
                UserId = customer.Id,
                ParkingSlotId = slot.Id,
                VehicleId = vehicle.Id,
                StartTime = DateTime.UtcNow.AddDays(-i),
                EndTime = DateTime.UtcNow.AddDays(-i).AddHours(2),
                TotalPrice = 12m,
                Status = ReservationStatus.Completed
            };
            context.Reservations.Add(res);

            context.ParkingHistories.Add(new ParkingHistory
            {
                Id = Guid.NewGuid(),
                ReservationId = res.Id,
                VehicleId = vehicle.Id,
                ParkingSlotId = slot.Id,
                AttendantCheckInId = attendant.Id,
                AttendantCheckOutId = attendant.Id,
                CheckInTime = DateTime.UtcNow.AddDays(-i),
                CheckOutTime = DateTime.UtcNow.AddDays(-i).AddHours(2),
                OverstayDurationMinutes = i == 1 ? 30 : 0,
                OverstayFee = i == 1 ? 5m : 0m,
                Notes = $"Routine exit #{i}"
            });
        }

        context.SaveChanges();

        var dashboard = service.GetCustomerDashboardAsync(customer.Id).GetAwaiter().GetResult();

        Assert(dashboard.RecentParkingHistory.Count == 5, "RecentParkingHistory is capped at the 5 most recent records");
        Assert(dashboard.RecentParkingHistory[0].ReservationNumber == "RES-HIST-1", "Most recent parking history (RES-HIST-1) is ordered first");
        Assert(dashboard.RecentParkingHistory[0].LicensePlate == "GRACE-77", "LicensePlate matches 'GRACE-77'");
        Assert(dashboard.RecentParkingHistory[0].VehicleMakeModel == "BMW 330i", "VehicleMakeModel matches 'BMW 330i'");
        Assert(dashboard.RecentParkingHistory[0].SlotNumber == "H-05", "SlotNumber matches 'H-05'");
        Assert(dashboard.RecentParkingHistory[0].ParkingAreaName == "Harbor Lot", "ParkingAreaName matches 'Harbor Lot'");
        Assert(dashboard.RecentParkingHistory[0].CustomerName == "Grace Regular", "CustomerName matches 'Grace Regular'");
        Assert(dashboard.RecentParkingHistory[0].AttendantCheckInName == "Officer Sam", "AttendantCheckInName matches 'Officer Sam'");
        Assert(dashboard.RecentParkingHistory[0].AdditionalFee == 5m, "AdditionalFee (overstay) matches 5m");
        Assert(dashboard.RecentParkingHistory[0].TotalAmount == 17m, "TotalAmount is 17m (12 base + 5 overstay)");
    }

    private static void TestNonExistentCustomer()
    {
        Console.WriteLine("\n[7] Non-Existent Customer Exception Tests:");
        var (_, service) = CreateMockContextAndService();

        var invalidId = Guid.NewGuid();
        bool threwKeyNotFound = false;

        try
        {
            service.GetCustomerDashboardAsync(invalidId).GetAwaiter().GetResult();
        }
        catch (KeyNotFoundException)
        {
            threwKeyNotFound = true;
        }

        Assert(threwKeyNotFound, "GetCustomerDashboardAsync throws KeyNotFoundException for non-existent customer ID");
    }

    private static void TestSoftDeletedReservationsExcluded()
    {
        Console.WriteLine("\n[8] Soft-Deleted Reservations Excluded Tests:");
        var (context, service) = CreateMockContextAndService();

        var customer = new User { Id = Guid.NewGuid(), FullName = "Harry Driver", Email = "harry@example.com", PhoneNumber = "999", PasswordHash = "h" };
        context.Users.Add(customer);

        var area = new ParkingArea { Id = Guid.NewGuid(), Name = "South Garage", Address = "1 South", City = "Metro", HourlyRate = 5, TotalSlots = 5, IsActive = true };
        var slot = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "S-01", Status = SlotStatus.Available };
        var vehicle = new Vehicle { Id = Guid.NewGuid(), UserId = customer.Id, LicensePlate = "HARRY-1", Make = "Kia", Model = "Soul", VehicleType = VehicleType.Car };
        context.ParkingAreas.Add(area);
        context.ParkingSlots.Add(slot);
        context.Vehicles.Add(vehicle);

        var resActive = new Reservation
        {
            Id = Guid.NewGuid(),
            ReservationNumber = "RES-ACTIVE-OK",
            UserId = customer.Id,
            ParkingSlotId = slot.Id,
            VehicleId = vehicle.Id,
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            EndTime = DateTime.UtcNow.AddHours(2),
            TotalPrice = 10m,
            Status = ReservationStatus.Active
        };
        var resDeleted = new Reservation
        {
            Id = Guid.NewGuid(),
            ReservationNumber = "RES-DELETED",
            UserId = customer.Id,
            ParkingSlotId = slot.Id,
            VehicleId = vehicle.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalPrice = 10m,
            Status = ReservationStatus.Confirmed,
            IsDeleted = true
        };
        context.Reservations.AddRange(resActive, resDeleted);

        context.SaveChanges();

        var dashboard = service.GetCustomerDashboardAsync(customer.Id).GetAwaiter().GetResult();

        Assert(dashboard.TotalReservations == 1, "TotalReservations excludes soft-deleted reservation");
        Assert(dashboard.UpcomingReservation == null, "Soft-deleted upcoming reservation is not selected");
        Assert(dashboard.ActiveReservation != null && dashboard.ActiveReservation.ReservationNumber == "RES-ACTIVE-OK", "Active reservation remains intact");
    }
}
