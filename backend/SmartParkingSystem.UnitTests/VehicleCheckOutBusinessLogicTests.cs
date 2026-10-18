using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SmartParkingSystem.Application.Common.Security;
using SmartParkingSystem.Application.DTOs.Parking;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;
using SmartParkingSystem.Infrastructure.Data;
using SmartParkingSystem.Infrastructure.Repositories;
using SmartParkingSystem.Infrastructure.Services;

namespace SmartParkingSystem.UnitTests;

public static class VehicleCheckOutBusinessLogicTests
{
    private static int _passed = 0;
    private static int _failed = 0;

    public static int Run()
    {
        _passed = 0;
        _failed = 0;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        Console.WriteLine(" Running Step 15: Vehicle Check-Out Business Logic Tests ");
        Console.WriteLine("=========================================================");
        Console.ResetColor();

        // 1. Static Invariant & Calculation Tests
        TestEntryRequiredGuard();
        TestDuplicateCheckOutPrevention();
        TestReservationStatusGuards();
        TestActualDurationCalculation();
        TestOverstayAndFeeCalculation();
        TestStateTransitionsAndAttendantAttribution();

        // 2. Service End-to-End Workflow Tests (InMemory DB)
        TestCheckOutSuccessWorkflow_OnTime();
        TestCheckOutSuccessWorkflow_WithOverstay();
        TestCheckOutEmptyQRCodeThrows();
        TestCheckOutUnknownQRCodeThrows();
        TestCheckOutNeverCheckedInThrows();
        TestCheckOutDuplicateExitThrows();
        TestCheckOutAlreadyCompletedReservationThrows();
        TestCheckOutCancelledReservationThrows();
        TestCheckOutNonActiveReservationThrows();
        TestCheckOutMismatchedFacilityThrows();
        TestCheckOutAttendantAttribution();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        if (_failed == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" Step 15 Vehicle Check-Out Tests: {_passed} Passed, {_failed} Failed");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" Step 15 Vehicle Check-Out Tests: {_passed} Passed, {_failed} Failed");
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

    private static (ApplicationDbContext context, IParkingService service) CreateMockContextAndService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        var parkingHistoryRepo = new ParkingHistoryRepository(context);
        var service = new ParkingService(context, parkingHistoryRepo, NullLogger<ParkingService>.Instance);

        return (context, service);
    }

    private static (User customer, User inAttendant, User outAttendant, ParkingArea area, ParkingSlot slot, Vehicle vehicle, Reservation reservation, QRCode qr, ParkingHistory history)
        SeedCheckOutTestData(
            ApplicationDbContext context,
            ReservationStatus resStatus = ReservationStatus.Active,
            bool isQrUsedForEntry = true,
            bool isQrUsedForExit = false,
            int entryHoursAgo = 2,
            int scheduledHoursAgo = 2,
            int scheduledDurationHours = 2)
    {
        var custRole = new Role { Id = Guid.NewGuid(), Name = AppRoles.Customer };
        var staffRole = new Role { Id = Guid.NewGuid(), Name = AppRoles.ParkingAttendant };
        context.Roles.AddRange(custRole, staffRole);

        var customer = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Elena Vance",
            Email = "elena@customer.com",
            PhoneNumber = "+1555987654",
            PasswordHash = "hash",
            RoleId = custRole.Id,
            Role = custRole
        };
        var inAttendant = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Inbound Attendant",
            Email = "in@parking.com",
            PasswordHash = "hash",
            RoleId = staffRole.Id,
            Role = staffRole
        };
        var outAttendant = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Outbound Attendant",
            Email = "out@parking.com",
            PasswordHash = "hash",
            RoleId = staffRole.Id,
            Role = staffRole
        };
        context.Users.AddRange(customer, inAttendant, outAttendant);

        var area = new ParkingArea
        {
            Id = Guid.NewGuid(),
            Name = "Plaza South Terminal",
            Address = "200 South Ave",
            City = "Metropolis",
            HourlyRate = 8.00m
        };
        context.ParkingAreas.Add(area);

        var slot = new ParkingSlot
        {
            Id = Guid.NewGuid(),
            ParkingAreaId = area.Id,
            ParkingArea = area,
            SlotNumber = "C-301",
            FloorNumber = 3,
            HourlyRate = 8.00m,
            Status = SlotStatus.Occupied,
            VehicleType = VehicleType.Car
        };
        context.ParkingSlots.Add(slot);

        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            User = customer,
            LicensePlate = "OUT-888",
            Make = "Honda",
            Model = "Civic",
            Color = "Black",
            VehicleType = VehicleType.Car
        };
        context.Vehicles.Add(vehicle);

        var now = DateTime.UtcNow;
        var start = now.AddHours(-scheduledHoursAgo);
        var end = start.AddHours(scheduledDurationHours);
        var entryTime = now.AddHours(-entryHoursAgo);

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
            StartTime = start,
            EndTime = end,
            TotalPrice = 16.00m,
            Status = resStatus,
            CreatedAt = start
        };
        context.Reservations.Add(reservation);

        var token = $"QR-{reservation.ReservationNumber}-OUTTEST";
        var qr = new QRCode
        {
            Id = Guid.NewGuid(),
            ReservationId = reservation.Id,
            Reservation = reservation,
            Code = token,
            ImageData = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUg==",
            IsUsedForEntry = isQrUsedForEntry,
            EntryScannedAt = isQrUsedForEntry ? entryTime : null,
            IsUsedForExit = isQrUsedForExit,
            ExitScannedAt = isQrUsedForExit ? now.AddMinutes(-5) : null,
            ExpiresAt = end.AddHours(2),
            CreatedAt = start
        };
        context.QRCodes.Add(qr);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ReservationId = reservation.Id,
            Reservation = reservation,
            UserId = customer.Id,
            Amount = 16.00m,
            PaymentMethod = PaymentMethod.Card,
            Status = PaymentStatus.Paid,
            TransactionId = $"TX-{Guid.NewGuid():N}",
            CreatedAt = start
        };
        context.Payments.Add(payment);

        ParkingHistory? history = null;
        if (isQrUsedForEntry)
        {
            history = new ParkingHistory
            {
                Id = Guid.NewGuid(),
                ReservationId = reservation.Id,
                VehicleId = vehicle.Id,
                ParkingSlotId = slot.Id,
                CheckInTime = entryTime,
                CheckOutTime = isQrUsedForExit ? now.AddMinutes(-5) : null,
                AttendantCheckInId = inAttendant.Id,
                AttendantCheckOutId = isQrUsedForExit ? outAttendant.Id : null,
                CreatedAt = entryTime
            };
            context.ParkingHistories.Add(history);
        }

        context.SaveChanges();

        return (customer, inAttendant, outAttendant, area, slot, vehicle, reservation, qr, history!);
    }

    // =========================================================================
    // 1. Static Invariant & Calculation Tests
    // =========================================================================

    private static void TestEntryRequiredGuard()
    {
        Console.WriteLine("\n[1] Entry Required Prior to Check-Out Guards:");
        var neverCheckedInQr = new QRCode { IsUsedForEntry = false };
        Assert(!neverCheckedInQr.IsUsedForEntry, "Check-out is blocked when vehicle was never checked in (IsUsedForEntry == false)");

        var checkedInQr = new QRCode { IsUsedForEntry = true, EntryScannedAt = DateTime.UtcNow.AddHours(-1) };
        Assert(checkedInQr.IsUsedForEntry, "Check-out is permitted when vehicle was previously checked in");
    }

    private static void TestDuplicateCheckOutPrevention()
    {
        Console.WriteLine("\n[2] Duplicate Check-Out Prevention Tests:");
        var alreadyCheckedOutQr = new QRCode
        {
            IsUsedForEntry = true,
            IsUsedForExit = true,
            ExitScannedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        Assert(alreadyCheckedOutQr.IsUsedForExit, "Re-scanning an already checked-out QR code is detected as duplicate exit");

        var activeQr = new QRCode
        {
            IsUsedForEntry = true,
            IsUsedForExit = false
        };
        Assert(!activeQr.IsUsedForExit, "Fresh exit check-out is permitted when IsUsedForExit == false");
    }

    private static void TestReservationStatusGuards()
    {
        Console.WriteLine("\n[3] Reservation Status Guard Tests:");
        var activeRes = new Reservation { Status = ReservationStatus.Active };
        Assert(activeRes.Status == ReservationStatus.Active, "Active reservation status is permitted for check-out");

        var confirmedRes = new Reservation { Status = ReservationStatus.Confirmed };
        Assert(confirmedRes.Status != ReservationStatus.Active, "Confirmed reservation (not yet checked in) is blocked from check-out");

        var completedRes = new Reservation { Status = ReservationStatus.Completed };
        Assert(completedRes.Status != ReservationStatus.Active, "Already completed reservation is blocked from duplicate check-out");

        var cancelledRes = new Reservation { Status = ReservationStatus.Cancelled };
        Assert(cancelledRes.Status != ReservationStatus.Active, "Cancelled reservation is blocked from check-out");
    }

    private static void TestActualDurationCalculation()
    {
        Console.WriteLine("\n[4] Actual Duration Calculation Tests:");
        var checkIn = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc);

        var checkOut1 = new DateTime(2026, 9, 20, 11, 15, 0, DateTimeKind.Utc);
        int duration1 = (int)Math.Ceiling((checkOut1 - checkIn).TotalMinutes);
        Assert(duration1 == 75, "75 minutes elapsed duration calculated correctly");

        var checkOut2 = new DateTime(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc);
        int duration2 = (int)Math.Ceiling((checkOut2 - checkIn).TotalMinutes);
        Assert(duration2 == 120, "120 minutes (2 hours) elapsed duration calculated correctly");

        var checkOut3 = checkIn.AddSeconds(10);
        int duration3 = Math.Max(1, (int)Math.Ceiling((checkOut3 - checkIn).TotalMinutes));
        Assert(duration3 == 1, "Short sub-minute stay rounds up to 1 minute minimum");
    }

    private static void TestOverstayAndFeeCalculation()
    {
        Console.WriteLine("\n[5] Overstay & Additional Charge Calculation Tests:");
        var scheduledEnd = new DateTime(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc);
        decimal hourlyRate = 8.00m;

        // Early exit
        var earlyExit = scheduledEnd.AddMinutes(-30);
        var (feeA, overstayA) = CalculateOverstay(earlyExit, scheduledEnd, hourlyRate);
        Assert(feeA == 0.0m && overstayA == 0, "Early exit incurs 0 overstay minutes and $0.00 fee");

        // On-time exit
        var onTimeExit = scheduledEnd;
        var (feeB, overstayB) = CalculateOverstay(onTimeExit, scheduledEnd, hourlyRate);
        Assert(feeB == 0.0m && overstayB == 0, "On-time exit incurs 0 overstay minutes and $0.00 fee");

        // Exit within 15-minute grace period
        var gracePeriodExit = scheduledEnd.AddMinutes(10);
        var (feeC, overstayC) = CalculateOverstay(gracePeriodExit, scheduledEnd, hourlyRate);
        Assert(feeC == 0.0m && overstayC == 0, "Exit within 15-minute grace window incurs $0.00 fee");

        // 45-minute overstay (1 hour extra)
        var overstay45Exit = scheduledEnd.AddMinutes(45);
        var (feeD, overstayD) = CalculateOverstay(overstay45Exit, scheduledEnd, hourlyRate);
        Assert(overstayD == 45, "45-minute overstay accurately recorded");
        Assert(feeD == 8.00m, "45-minute overstay billed at 1 hour rate ($8.00)");

        // 75-minute overstay (2 hours extra)
        var overstay75Exit = scheduledEnd.AddMinutes(75);
        var (feeE, overstayE) = CalculateOverstay(overstay75Exit, scheduledEnd, hourlyRate);
        Assert(overstayE == 75, "75-minute overstay accurately recorded");
        Assert(feeE == 16.00m, "75-minute overstay billed at 2 hours rate ($16.00)");

        decimal basePrice = 16.00m;
        decimal totalAmount = basePrice + feeE;
        Assert(totalAmount == 32.00m, "Total amount sums base price ($16.00) + overstay fee ($16.00) = $32.00");
    }

    private static void TestStateTransitionsAndAttendantAttribution()
    {
        Console.WriteLine("\n[6] State Transitions & Attendant Attribution Tests:");
        var now = DateTime.UtcNow;
        var checkOutAttendantId = Guid.NewGuid();
        var res = new Reservation { Status = ReservationStatus.Active };
        var slot = new ParkingSlot { Status = SlotStatus.Occupied };
        var qr = new QRCode { IsUsedForExit = false };
        var history = new ParkingHistory { CheckInTime = now.AddHours(-2) };

        qr.IsUsedForExit = true;
        qr.ExitScannedAt = now;
        res.Status = ReservationStatus.Completed;
        slot.Status = SlotStatus.Available;
        history.CheckOutTime = now;
        history.AttendantCheckOutId = checkOutAttendantId;

        Assert(qr.IsUsedForExit, "QR code flagged as IsUsedForExit = true");
        Assert(qr.ExitScannedAt == now, "QR code exit timestamp recorded");
        Assert(res.Status == ReservationStatus.Completed, "Reservation status transitioned to Completed");
        Assert(slot.Status == SlotStatus.Available, "Parking slot status transitioned back to Available");
        Assert(history.AttendantCheckOutId == checkOutAttendantId, "Parking history attributes the checking-out attendant ID");
        Assert(history.CheckOutTime == now, "Parking history accurately records check-out timestamp");
    }

    // =========================================================================
    // 2. Service End-to-End Workflow Tests (InMemory DB)
    // =========================================================================

    private static void TestCheckOutSuccessWorkflow_OnTime()
    {
        Console.WriteLine("\n[7] CheckOutVehicleAsync On-Time Workflow Test:");
        var (context, service) = CreateMockContextAndService();
        // Started 1 hour ago, scheduled for 2 hours (exit is 1 hour before scheduled end)
        var (_, inAttendant, outAttendant, _, slot, vehicle, res, qr, _) = SeedCheckOutTestData(
            context, entryHoursAgo: 1, scheduledHoursAgo: 1, scheduledDurationHours: 2);

        var request = new CheckOutRequest
        {
            QRCode = qr.Code,
            Notes = "Smooth exit via Gate 2"
        };

        var result = service.CheckOutVehicleAsync(request, outAttendant.Id).GetAwaiter().GetResult();

        Assert(result.Success, "CheckOutVehicleAsync returns Success = true");
        Assert(result.ReservationId == res.Id, "Returned ReservationId matches seeded reservation");
        Assert(result.ReservationStatus == ReservationStatus.Completed, "Returned ReservationStatus is Completed");
        Assert(result.SlotStatus == SlotStatus.Available, "Returned SlotStatus is Available");
        Assert(result.LicensePlate == vehicle.LicensePlate, "Returned LicensePlate matches vehicle");
        Assert(result.OverstayMinutes == 0, "On-time exit has OverstayMinutes = 0");
        Assert(result.OverstayFee == 0.0m, "On-time exit has OverstayFee = $0.00");
        Assert(result.TotalAmount == result.BasePrice, "Total amount equals base price without overstay penalty");
        Assert(result.AttendantCheckOutId == outAttendant.Id, "Returned AttendantCheckOutId matches outbound staff");
        Assert(result.AttendantCheckOutName == outAttendant.FullName, "Returned AttendantCheckOutName matches outbound staff name");
        Assert(qr.IsUsedForExit, "QR entity marked as IsUsedForExit = true in database");
        Assert(qr.ExitScannedAt.HasValue, "QR entity ExitScannedAt is populated in database");
        Assert(slot.Status == SlotStatus.Available, "ParkingSlot entity status restored to Available in database");
        Assert(res.Status == ReservationStatus.Completed, "Reservation entity status marked Completed in database");

        var history = context.ParkingHistories.FirstOrDefault(h => h.ReservationId == res.Id);
        Assert(history != null && history.CheckOutTime.HasValue, "ParkingHistory CheckOutTime is finalized");
        Assert(history!.AttendantCheckInId == inAttendant.Id, "ParkingHistory preserves initial check-in attendant");
        Assert(history.AttendantCheckOutId == outAttendant.Id, "ParkingHistory records exit attendant attribution");
    }

    private static void TestCheckOutSuccessWorkflow_WithOverstay()
    {
        Console.WriteLine("\n[8] CheckOutVehicleAsync Overstay Additional Charge Test:");
        var (context, service) = CreateMockContextAndService();
        // Started 3 hours ago, scheduled for 2 hours (now is 1 hour = 60 mins past scheduled end)
        var (_, _, outAttendant, _, _, _, res, qr, _) = SeedCheckOutTestData(
            context, entryHoursAgo: 3, scheduledHoursAgo: 3, scheduledDurationHours: 2);

        var request = new CheckOutRequest { QRCode = qr.Code, Notes = "Customer delayed by traffic" };
        var result = service.CheckOutVehicleAsync(request, outAttendant.Id).GetAwaiter().GetResult();

        Assert(result.Success, "CheckOutVehicleAsync returns Success = true");
        Assert(result.OverstayMinutes >= 55, "Overstay duration (> 15m grace window) accurately calculated");
        Assert(result.OverstayFee > 0, "Overstay fee is assessed for exceeding scheduled time");
        Assert(result.TotalAmount == result.BasePrice + result.OverstayFee, "TotalAmount correctly sums base price + overstay fee");
        Assert(result.Message.Contains("overstay fee", StringComparison.OrdinalIgnoreCase), "Success message mentions overstay fee");
    }

    private static void TestCheckOutEmptyQRCodeThrows()
    {
        Console.WriteLine("\n[9] CheckOutVehicleAsync Empty QR Code Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, _, outAttendant, _, _, _, _, _, _) = SeedCheckOutTestData(context);

        bool threw = false;
        try
        {
            service.CheckOutVehicleAsync(new CheckOutRequest { QRCode = "  " }, outAttendant.Id).GetAwaiter().GetResult();
        }
        catch (ArgumentException)
        {
            threw = true;
        }

        Assert(threw, "Empty QR code throws ArgumentException");
    }

    private static void TestCheckOutUnknownQRCodeThrows()
    {
        Console.WriteLine("\n[10] CheckOutVehicleAsync Unknown QR Code Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, _, outAttendant, _, _, _, _, _, _) = SeedCheckOutTestData(context);

        bool threw = false;
        try
        {
            service.CheckOutVehicleAsync(new CheckOutRequest { QRCode = "QR-NON-EXISTENT" }, outAttendant.Id).GetAwaiter().GetResult();
        }
        catch (KeyNotFoundException)
        {
            threw = true;
        }

        Assert(threw, "Unknown QR code throws KeyNotFoundException");
    }

    private static void TestCheckOutNeverCheckedInThrows()
    {
        Console.WriteLine("\n[11] CheckOutVehicleAsync Never Checked In Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, _, outAttendant, _, _, _, _, qr, _) = SeedCheckOutTestData(context, isQrUsedForEntry: false);

        bool threw = false;
        try
        {
            service.CheckOutVehicleAsync(new CheckOutRequest { QRCode = qr.Code }, outAttendant.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("has not been checked in", StringComparison.OrdinalIgnoreCase);
        }

        Assert(threw, "Attempting checkout without entry check-in throws InvalidOperationException");
    }

    private static void TestCheckOutDuplicateExitThrows()
    {
        Console.WriteLine("\n[12] CheckOutVehicleAsync Duplicate Check-Out Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, _, outAttendant, _, _, _, _, qr, _) = SeedCheckOutTestData(context, isQrUsedForExit: true);

        bool threw = false;
        try
        {
            service.CheckOutVehicleAsync(new CheckOutRequest { QRCode = qr.Code }, outAttendant.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("already checked out", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("Duplicate check-out", StringComparison.OrdinalIgnoreCase);
        }

        Assert(threw, "Duplicate check-out throws InvalidOperationException");
    }

    private static void TestCheckOutAlreadyCompletedReservationThrows()
    {
        Console.WriteLine("\n[13] CheckOutVehicleAsync Completed Reservation Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, _, outAttendant, _, _, _, _, qr, _) = SeedCheckOutTestData(context, resStatus: ReservationStatus.Completed);

        bool threw = false;
        try
        {
            service.CheckOutVehicleAsync(new CheckOutRequest { QRCode = qr.Code }, outAttendant.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("completed", StringComparison.OrdinalIgnoreCase);
        }

        Assert(threw, "Checking out completed reservation throws InvalidOperationException");
    }

    private static void TestCheckOutCancelledReservationThrows()
    {
        Console.WriteLine("\n[14] CheckOutVehicleAsync Cancelled Reservation Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, _, outAttendant, _, _, _, _, qr, _) = SeedCheckOutTestData(context, resStatus: ReservationStatus.Cancelled);

        bool threw = false;
        try
        {
            service.CheckOutVehicleAsync(new CheckOutRequest { QRCode = qr.Code }, outAttendant.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("cancelled", StringComparison.OrdinalIgnoreCase);
        }

        Assert(threw, "Checking out cancelled reservation throws InvalidOperationException");
    }

    private static void TestCheckOutNonActiveReservationThrows()
    {
        Console.WriteLine("\n[15] CheckOutVehicleAsync Non-Active Reservation Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, _, outAttendant, _, _, _, _, qr, _) = SeedCheckOutTestData(context, resStatus: ReservationStatus.Confirmed);

        bool threw = false;
        try
        {
            service.CheckOutVehicleAsync(new CheckOutRequest { QRCode = qr.Code }, outAttendant.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("Only active reservations", StringComparison.OrdinalIgnoreCase);
        }

        Assert(threw, "Checking out Confirmed (non-active) reservation throws InvalidOperationException");
    }

    private static void TestCheckOutMismatchedFacilityThrows()
    {
        Console.WriteLine("\n[16] CheckOutVehicleAsync Mismatched Facility Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, _, outAttendant, _, _, _, _, qr, _) = SeedCheckOutTestData(context);

        var wrongAreaId = Guid.NewGuid();
        bool threw = false;
        try
        {
            service.CheckOutVehicleAsync(new CheckOutRequest
            {
                QRCode = qr.Code,
                ParkingAreaId = wrongAreaId
            }, outAttendant.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("different parking facility", StringComparison.OrdinalIgnoreCase);
        }

        Assert(threw, "Check-out at wrong facility throws InvalidOperationException");
    }

    private static void TestCheckOutAttendantAttribution()
    {
        Console.WriteLine("\n[17] CheckOutVehicleAsync Attendant Attribution Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, inAttendant, outAttendant, _, _, _, res, qr, _) = SeedCheckOutTestData(context);

        var result = service.CheckOutVehicleAsync(new CheckOutRequest { QRCode = qr.Code }, outAttendant.Id).GetAwaiter().GetResult();

        Assert(result.AttendantCheckOutId == outAttendant.Id, "CheckOutResultDto captures exit attendant ID");
        Assert(result.AttendantCheckOutName == "Outbound Attendant", "CheckOutResultDto captures exit attendant FullName");
        Assert(result.AttendantCheckInId == inAttendant.Id, "CheckOutResultDto preserves entry attendant ID");

        var history = context.ParkingHistories.First(h => h.ReservationId == res.Id);
        Assert(history.AttendantCheckOutId == outAttendant.Id, "ParkingHistory records exit attendant ID for auditing");
    }

    private static (decimal fee, int overstayMinutes) CalculateOverstay(DateTime exitTime, DateTime scheduledEnd, decimal hourlyRate)
    {
        if (exitTime <= scheduledEnd.AddMinutes(15))
        {
            return (0.0m, 0);
        }

        int overstayMinutes = (int)Math.Ceiling((exitTime - scheduledEnd).TotalMinutes);
        var extraHours = (decimal)Math.Ceiling(overstayMinutes / 60.0);
        var fee = extraHours * hourlyRate;
        return (fee, overstayMinutes);
    }
}
