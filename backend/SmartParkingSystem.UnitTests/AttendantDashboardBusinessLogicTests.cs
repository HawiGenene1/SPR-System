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

public static class AttendantDashboardBusinessLogicTests
{
    private static int _passed = 0;
    private static int _failed = 0;

    public static int Run()
    {
        _passed = 0;
        _failed = 0;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        Console.WriteLine(" Running Step 18: Parking Attendant Dashboard Unit Tests ");
        Console.WriteLine("=========================================================");
        Console.ResetColor();

        TestFacilityScopingAndIsolation();
        TestTodaysCheckInsAndCheckOuts();
        TestCurrentOccupiedAndAvailableSlots();
        TestActiveReservationsCount();
        TestRecentVehicleEntriesDetails();
        TestAdminCrossFacilityDashboard();
        TestUnassignedAttendantFallback();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        if (_failed == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" Step 18 Attendant Dashboard Tests: {_passed} Passed, {_failed} Failed");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" Step 18 Attendant Dashboard Tests: {_passed} Passed, {_failed} Failed");
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

    private static void TestFacilityScopingAndIsolation()
    {
        Console.WriteLine("\n[1] Facility Scoping & Isolation Tests:");
        var (context, service) = CreateMockContextAndService();

        var area1 = new ParkingArea { Id = Guid.NewGuid(), Name = "North Facility", Address = "100 North Rd", City = "Metro", HourlyRate = 5, TotalSlots = 10, IsActive = true };
        var area2 = new ParkingArea { Id = Guid.NewGuid(), Name = "South Facility", Address = "200 South Rd", City = "Metro", HourlyRate = 6, TotalSlots = 5, IsActive = true };
        context.ParkingAreas.AddRange(area1, area2);

        var attendant1 = new User { Id = Guid.NewGuid(), FullName = "Attendant One", Email = "att1@p.com", PhoneNumber = "1", PasswordHash = "h", AssignedParkingAreaId = area1.Id };
        var attendant2 = new User { Id = Guid.NewGuid(), FullName = "Attendant Two", Email = "att2@p.com", PhoneNumber = "2", PasswordHash = "h", AssignedParkingAreaId = area2.Id };
        context.Users.AddRange(attendant1, attendant2);

        // Slots: 3 in area1 (2 occupied), 2 in area2 (1 occupied)
        context.ParkingSlots.AddRange(
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area1.Id, SlotNumber = "N-1", Status = SlotStatus.Occupied },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area1.Id, SlotNumber = "N-2", Status = SlotStatus.Occupied },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area1.Id, SlotNumber = "N-3", Status = SlotStatus.Available },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area2.Id, SlotNumber = "S-1", Status = SlotStatus.Occupied },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area2.Id, SlotNumber = "S-2", Status = SlotStatus.Available }
        );

        context.SaveChanges();

        var dash1 = service.GetAttendantDashboardAsync(attendant1.Id, isAdmin: false).GetAwaiter().GetResult();
        var dash2 = service.GetAttendantDashboardAsync(attendant2.Id, isAdmin: false).GetAwaiter().GetResult();

        Assert(dash1.AssignedParkingAreaId == area1.Id, "Attendant 1 dashboard is scoped to North Facility");
        Assert(dash1.AssignedParkingAreaName == "North Facility", "AssignedParkingAreaName matches 'North Facility'");
        Assert(dash1.TotalSlots == 3, "North Facility total slots is 3");
        Assert(dash1.CurrentOccupiedSlots == 2, "North Facility current occupied slots is 2");

        Assert(dash2.AssignedParkingAreaId == area2.Id, "Attendant 2 dashboard is scoped to South Facility");
        Assert(dash2.AssignedParkingAreaName == "South Facility", "AssignedParkingAreaName matches 'South Facility'");
        Assert(dash2.TotalSlots == 2, "South Facility total slots is 2");
        Assert(dash2.CurrentOccupiedSlots == 1, "South Facility current occupied slots is 1");
    }

    private static void TestTodaysCheckInsAndCheckOuts()
    {
        Console.WriteLine("\n[2] Today's Check-Ins and Check-Outs Tests:");
        var (context, service) = CreateMockContextAndService();
        var now = DateTime.UtcNow;
        var today = now.Date;

        var area = new ParkingArea { Id = Guid.NewGuid(), Name = "Terminal Gate", Address = "Gate 1", City = "Metro", HourlyRate = 5, TotalSlots = 10, IsActive = true };
        var otherArea = new ParkingArea { Id = Guid.NewGuid(), Name = "Other Gate", Address = "Gate 2", City = "Metro", HourlyRate = 5, TotalSlots = 10, IsActive = true };
        context.ParkingAreas.AddRange(area, otherArea);

        var attendant = new User { Id = Guid.NewGuid(), FullName = "Gate Guard", Email = "guard@p.com", PhoneNumber = "1", PasswordHash = "h", AssignedParkingAreaId = area.Id };
        var customer = new User { Id = Guid.NewGuid(), FullName = "Cust", Email = "c@c.com", PhoneNumber = "1", PasswordHash = "h" };
        context.Users.AddRange(attendant, customer);

        var slot1 = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "G-1", Status = SlotStatus.Occupied };
        var slot2 = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "G-2", Status = SlotStatus.Available };
        var slotOther = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = otherArea.Id, SlotNumber = "O-1", Status = SlotStatus.Occupied };
        context.ParkingSlots.AddRange(slot1, slot2, slotOther);

        var vehicle = new Vehicle { Id = Guid.NewGuid(), UserId = customer.Id, LicensePlate = "ABC-123", Make = "Ford", Model = "Focus", VehicleType = VehicleType.Car };
        context.Vehicles.Add(vehicle);

        var res1 = new Reservation { Id = Guid.NewGuid(), ReservationNumber = "RES-T1", UserId = customer.Id, VehicleId = vehicle.Id, ParkingSlotId = slot1.Id, StartTime = today.AddHours(8), EndTime = today.AddHours(12), TotalPrice = 20, Status = ReservationStatus.Active };
        var res2 = new Reservation { Id = Guid.NewGuid(), ReservationNumber = "RES-T2", UserId = customer.Id, VehicleId = vehicle.Id, ParkingSlotId = slot2.Id, StartTime = today.AddHours(9), EndTime = today.AddHours(11), TotalPrice = 10, Status = ReservationStatus.Completed };
        var resOld = new Reservation { Id = Guid.NewGuid(), ReservationNumber = "RES-OLD", UserId = customer.Id, VehicleId = vehicle.Id, ParkingSlotId = slot1.Id, StartTime = today.AddDays(-2), EndTime = today.AddDays(-2).AddHours(2), TotalPrice = 10, Status = ReservationStatus.Completed };
        var resOther = new Reservation { Id = Guid.NewGuid(), ReservationNumber = "RES-OTH", UserId = customer.Id, VehicleId = vehicle.Id, ParkingSlotId = slotOther.Id, StartTime = today.AddHours(10), EndTime = today.AddHours(12), TotalPrice = 10, Status = ReservationStatus.Active };
        context.Reservations.AddRange(res1, res2, resOld, resOther);

        // Check-in today, still parked (CheckOutTime = null)
        context.ParkingHistories.Add(new ParkingHistory
        {
            Id = Guid.NewGuid(),
            ReservationId = res1.Id,
            VehicleId = vehicle.Id,
            ParkingSlotId = slot1.Id,
            CheckInTime = today.AddHours(8).AddMinutes(10),
            CheckOutTime = null,
            AttendantCheckInId = attendant.Id
        });

        // Check-in today AND checked out today
        context.ParkingHistories.Add(new ParkingHistory
        {
            Id = Guid.NewGuid(),
            ReservationId = res2.Id,
            VehicleId = vehicle.Id,
            ParkingSlotId = slot2.Id,
            CheckInTime = today.AddHours(9).AddMinutes(5),
            CheckOutTime = today.AddHours(10).AddMinutes(55),
            AttendantCheckInId = attendant.Id,
            AttendantCheckOutId = attendant.Id
        });

        // Check-in yesterday (should not count for today)
        context.ParkingHistories.Add(new ParkingHistory
        {
            Id = Guid.NewGuid(),
            ReservationId = resOld.Id,
            VehicleId = vehicle.Id,
            ParkingSlotId = slot1.Id,
            CheckInTime = today.AddDays(-2),
            CheckOutTime = today.AddDays(-2).AddHours(2)
        });

        // Check-in today at OTHER facility (should not count for this attendant)
        context.ParkingHistories.Add(new ParkingHistory
        {
            Id = Guid.NewGuid(),
            ReservationId = resOther.Id,
            VehicleId = vehicle.Id,
            ParkingSlotId = slotOther.Id,
            CheckInTime = today.AddHours(10),
            CheckOutTime = null
        });

        context.SaveChanges();

        var dashboard = service.GetAttendantDashboardAsync(attendant.Id, isAdmin: false).GetAwaiter().GetResult();

        Assert(dashboard.TodaysCheckIns == 2, "TodaysCheckIns correctly counts 2 vehicle check-ins at this facility today");
        Assert(dashboard.CheckInsToday == 2, "CheckInsToday alias matches 2");
        Assert(dashboard.TodaysCheckOuts == 1, "TodaysCheckOuts correctly counts 1 vehicle exit checkout today");
        Assert(dashboard.CheckOutsToday == 1, "CheckOutsToday alias matches 1");
    }

    private static void TestCurrentOccupiedAndAvailableSlots()
    {
        Console.WriteLine("\n[3] Current Occupied & Available Slots Tests:");
        var (context, service) = CreateMockContextAndService();

        var area = new ParkingArea { Id = Guid.NewGuid(), Name = "Plaza Deck", Address = "Plaza Way", City = "Metro", HourlyRate = 5, TotalSlots = 8, IsActive = true };
        context.ParkingAreas.Add(area);

        var attendant = new User { Id = Guid.NewGuid(), FullName = "Plaza Attendant", Email = "p@p.com", PhoneNumber = "1", PasswordHash = "h", AssignedParkingAreaId = area.Id };
        context.Users.Add(attendant);

        // 3 Occupied, 4 Available, 1 Reserved, 1 Maintenance
        context.ParkingSlots.AddRange(
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "P-1", Status = SlotStatus.Occupied },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "P-2", Status = SlotStatus.Occupied },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "P-3", Status = SlotStatus.Occupied },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "P-4", Status = SlotStatus.Available },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "P-5", Status = SlotStatus.Available },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "P-6", Status = SlotStatus.Available },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "P-7", Status = SlotStatus.Available },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "P-8", Status = SlotStatus.Reserved },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "P-9", Status = SlotStatus.Maintenance }
        );

        context.SaveChanges();

        var dashboard = service.GetAttendantDashboardAsync(attendant.Id, isAdmin: false).GetAwaiter().GetResult();

        Assert(dashboard.TotalSlots == 9, "TotalSlots is 9");
        Assert(dashboard.CurrentOccupiedSlots == 3, "CurrentOccupiedSlots is 3");
        Assert(dashboard.OccupiedSlots == 3, "OccupiedSlots alias matches 3");
        Assert(dashboard.AvailableSlots == 4, "AvailableSlots is 4");
        Assert(dashboard.ReservedSlots == 1, "ReservedSlots is 1");
        Assert(dashboard.MaintenanceSlots == 1, "MaintenanceSlots is 1");
        Assert(dashboard.OccupancyRatePercentage == 33.33, "OccupancyRatePercentage is 33.33% (3 / 9)");
    }

    private static void TestActiveReservationsCount()
    {
        Console.WriteLine("\n[4] Active Reservations Count Tests:");
        var (context, service) = CreateMockContextAndService();
        var now = DateTime.UtcNow;

        var area = new ParkingArea { Id = Guid.NewGuid(), Name = "Mall Deck", Address = "Mall Rd", City = "Metro", HourlyRate = 5, TotalSlots = 10, IsActive = true };
        context.ParkingAreas.Add(area);

        var attendant = new User { Id = Guid.NewGuid(), FullName = "Mall Staff", Email = "m@p.com", PhoneNumber = "1", PasswordHash = "h", AssignedParkingAreaId = area.Id };
        var customer = new User { Id = Guid.NewGuid(), FullName = "Cust", Email = "c@m.com", PhoneNumber = "1", PasswordHash = "h" };
        context.Users.AddRange(attendant, customer);

        var slot = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "M-1", Status = SlotStatus.Occupied };
        context.ParkingSlots.Add(slot);

        var vehicle = new Vehicle { Id = Guid.NewGuid(), UserId = customer.Id, LicensePlate = "MALL-101", Make = "Tesla", Model = "Y", VehicleType = VehicleType.Car };
        context.Vehicles.Add(vehicle);

        // 2 Active reservations, 1 Completed, 1 Cancelled
        context.Reservations.AddRange(
            new Reservation { Id = Guid.NewGuid(), ReservationNumber = "R-ACT-1", UserId = customer.Id, VehicleId = vehicle.Id, ParkingSlotId = slot.Id, StartTime = now.AddHours(-1), EndTime = now.AddHours(2), TotalPrice = 15, Status = ReservationStatus.Active },
            new Reservation { Id = Guid.NewGuid(), ReservationNumber = "R-ACT-2", UserId = customer.Id, VehicleId = vehicle.Id, ParkingSlotId = slot.Id, StartTime = now.AddMinutes(-30), EndTime = now.AddHours(1), TotalPrice = 10, Status = ReservationStatus.Active },
            new Reservation { Id = Guid.NewGuid(), ReservationNumber = "R-CMP-1", UserId = customer.Id, VehicleId = vehicle.Id, ParkingSlotId = slot.Id, StartTime = now.AddHours(-3), EndTime = now.AddHours(-1), TotalPrice = 10, Status = ReservationStatus.Completed },
            new Reservation { Id = Guid.NewGuid(), ReservationNumber = "R-CNC-1", UserId = customer.Id, VehicleId = vehicle.Id, ParkingSlotId = slot.Id, StartTime = now.AddHours(2), EndTime = now.AddHours(4), TotalPrice = 10, Status = ReservationStatus.Cancelled }
        );

        context.SaveChanges();

        var dashboard = service.GetAttendantDashboardAsync(attendant.Id, isAdmin: false).GetAwaiter().GetResult();

        Assert(dashboard.ActiveReservations == 2, "ActiveReservations accurately counts 2 active on-site bookings");
        Assert(dashboard.ActiveReservationsCount == 2, "ActiveReservationsCount alias matches 2");
    }

    private static void TestRecentVehicleEntriesDetails()
    {
        Console.WriteLine("\n[5] Recent Vehicle Entries List Details Tests:");
        var (context, service) = CreateMockContextAndService();
        var now = DateTime.UtcNow;

        var area = new ParkingArea { Id = Guid.NewGuid(), Name = "Harbor Lot", Address = "Harbor Way", City = "Metro", HourlyRate = 5, TotalSlots = 10, IsActive = true };
        context.ParkingAreas.Add(area);

        var attendant = new User { Id = Guid.NewGuid(), FullName = "Officer Bob", Email = "bob@p.com", PhoneNumber = "1", PasswordHash = "h", AssignedParkingAreaId = area.Id };
        var customer = new User { Id = Guid.NewGuid(), FullName = "Alice Driver", Email = "alice@c.com", PhoneNumber = "+1-555-9876", PasswordHash = "h" };
        context.Users.AddRange(attendant, customer);

        var slot = new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area.Id, SlotNumber = "H-05", FloorNumber = 2, Status = SlotStatus.Occupied };
        context.ParkingSlots.Add(slot);

        var vehicle = new Vehicle { Id = Guid.NewGuid(), UserId = customer.Id, LicensePlate = "ALICE-1", Make = "BMW", Model = "330i", VehicleType = VehicleType.Car };
        context.Vehicles.Add(vehicle);

        var res = new Reservation { Id = Guid.NewGuid(), ReservationNumber = "RES-ALICE", UserId = customer.Id, VehicleId = vehicle.Id, ParkingSlotId = slot.Id, StartTime = now.AddHours(-1), EndTime = now.AddHours(1), TotalPrice = 10, Status = ReservationStatus.Active };
        context.Reservations.Add(res);

        // Add active entry (no checkout time)
        var entryTime = now.AddMinutes(-45);
        context.ParkingHistories.Add(new ParkingHistory
        {
            Id = Guid.NewGuid(),
            ReservationId = res.Id,
            VehicleId = vehicle.Id,
            ParkingSlotId = slot.Id,
            CheckInTime = entryTime,
            CheckOutTime = null,
            AttendantCheckInId = attendant.Id
        });

        context.SaveChanges();

        var dashboard = service.GetAttendantDashboardAsync(attendant.Id, isAdmin: false).GetAwaiter().GetResult();
        var entries = dashboard.RecentVehicleEntries;

        Assert(entries.Count == 1, "RecentVehicleEntries contains 1 entry");
        var first = entries[0];
        Assert(first.LicensePlate == "ALICE-1", "LicensePlate matches 'ALICE-1'");
        Assert(first.VehicleMakeModel == "BMW 330i", "VehicleMakeModel is 'BMW 330i'");
        Assert(first.SlotNumber == "H-05", "SlotNumber matches 'H-05'");
        Assert(first.FloorNumber == 2, "FloorNumber matches 2");
        Assert(first.ParkingAreaName == "Harbor Lot", "ParkingAreaName matches 'Harbor Lot'");
        Assert(first.CustomerName == "Alice Driver", "CustomerName matches 'Alice Driver'");
        Assert(first.CustomerPhoneNumber == "+1-555-9876", "CustomerPhoneNumber matches '+1-555-9876'");
        Assert(first.AttendantName == "Officer Bob", "AttendantName matches 'Officer Bob'");
        Assert(first.IsCurrentlyParked, "IsCurrentlyParked is true for vehicle still inside facility");
        Assert(first.CheckOutTime == null, "CheckOutTime is null");
    }

    private static void TestAdminCrossFacilityDashboard()
    {
        Console.WriteLine("\n[6] Admin Cross-Facility Access Tests:");
        var (context, service) = CreateMockContextAndService();

        var area1 = new ParkingArea { Id = Guid.NewGuid(), Name = "Facility A", Address = "A St", City = "Metro", HourlyRate = 5, TotalSlots = 4, IsActive = true };
        var area2 = new ParkingArea { Id = Guid.NewGuid(), Name = "Facility B", Address = "B St", City = "Metro", HourlyRate = 5, TotalSlots = 6, IsActive = true };
        context.ParkingAreas.AddRange(area1, area2);

        var adminUser = new User { Id = Guid.NewGuid(), FullName = "Super Admin", Email = "admin@root.com", PhoneNumber = "1", PasswordHash = "h" };
        context.Users.Add(adminUser);

        context.ParkingSlots.AddRange(
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area1.Id, SlotNumber = "A-1", Status = SlotStatus.Occupied },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area1.Id, SlotNumber = "A-2", Status = SlotStatus.Available },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area2.Id, SlotNumber = "B-1", Status = SlotStatus.Occupied },
            new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = area2.Id, SlotNumber = "B-2", Status = SlotStatus.Occupied }
        );

        context.SaveChanges();

        // 1. Admin querying Facility B explicitly via parkingAreaId
        var adminQueryB = service.GetAttendantDashboardAsync(adminUser.Id, isAdmin: true, parkingAreaId: area2.Id).GetAwaiter().GetResult();
        Assert(adminQueryB.AssignedParkingAreaId == area2.Id, "Admin scoped query successfully targets Facility B");
        Assert(adminQueryB.AssignedParkingAreaName == "Facility B", "AssignedParkingAreaName matches Facility B");
        Assert(adminQueryB.CurrentOccupiedSlots == 2, "Facility B has 2 occupied slots");

        // 2. Admin querying without specific facility parameter (System-wide aggregation)
        var adminQueryAll = service.GetAttendantDashboardAsync(adminUser.Id, isAdmin: true, parkingAreaId: null).GetAwaiter().GetResult();
        Assert(adminQueryAll.AssignedParkingAreaName == "All Facilities (System-Wide)", "Admin default targets All Facilities (System-Wide)");
        Assert(adminQueryAll.CurrentOccupiedSlots == 3, "System-wide current occupied slots is 3 (1 from A + 2 from B)");
        Assert(adminQueryAll.TotalSlots == 4, "System-wide total slots counts all 4 slots");
    }

    private static void TestUnassignedAttendantFallback()
    {
        Console.WriteLine("\n[7] Unassigned Attendant Fallback Tests:");
        var (context, service) = CreateMockContextAndService();

        var activeArea = new ParkingArea { Id = Guid.NewGuid(), Name = "Primary Facility", Address = "Main Ave", City = "Metro", HourlyRate = 5, TotalSlots = 5, IsActive = true };
        context.ParkingAreas.Add(activeArea);

        // Attendant with AssignedParkingAreaId = null
        var unassignedAttendant = new User { Id = Guid.NewGuid(), FullName = "New Employee", Email = "new@p.com", PhoneNumber = "1", PasswordHash = "h", AssignedParkingAreaId = null };
        context.Users.Add(unassignedAttendant);

        context.ParkingSlots.Add(new ParkingSlot { Id = Guid.NewGuid(), ParkingAreaId = activeArea.Id, SlotNumber = "PR-1", Status = SlotStatus.Available });
        context.SaveChanges();

        var dashboard = service.GetAttendantDashboardAsync(unassignedAttendant.Id, isAdmin: false).GetAwaiter().GetResult();

        Assert(dashboard.AssignedParkingAreaId == activeArea.Id, "Unassigned attendant safely defaults to the active facility");
        Assert(dashboard.AssignedParkingAreaName == "Primary Facility", "AssignedParkingAreaName matches 'Primary Facility'");
        Assert(dashboard.AvailableSlots == 1, "AvailableSlots count is 1");
    }
}
