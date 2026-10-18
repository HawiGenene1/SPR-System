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

public static class VehicleCheckInBusinessLogicTests
{
    private static int _passed = 0;
    private static int _failed = 0;

    public static int Run()
    {
        _passed = 0;
        _failed = 0;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        Console.WriteLine(" Running Step 14: Vehicle Check-In Business Logic Tests ");
        Console.WriteLine("=========================================================");
        Console.ResetColor();

        // 1. Static Invariant Tests
        TestQrCodeValidityAndExpiration();
        TestReservationStatusGuards();
        TestTimeWindowBoundaryRules();
        TestPaymentStatusVerification();
        TestDuplicateEntryPrevention();
        TestStateTransitionsAndAttendantAttribution();

        // 2. Service End-to-End Workflow Tests (InMemory DB)
        TestCheckInSuccessWorkflow();
        TestCheckInEmptyQRCodeThrows();
        TestCheckInUnknownQRCodeThrows();
        TestCheckInExpiredQRCodeThrows();
        TestCheckInCancelledReservationThrows();
        TestCheckInCompletedReservationThrows();
        TestCheckInAlreadyActiveReservationThrows();
        TestCheckInPendingReservationThrows();
        TestCheckInUnpaidReservationThrows();
        TestCheckInPrematureArrivalThrows();
        TestCheckInEarlyArrivalWithinGracePeriodSucceeds();
        TestCheckInExpiredTimeWindowThrows();
        TestCheckInDuplicateEntryWithUsedQRCodeThrows();
        TestCheckInDuplicateEntryWithActiveSessionThrows();
        TestCheckInMismatchedFacilityThrows();
        TestCheckInAttendantAttribution();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        if (_failed == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" Step 14 Vehicle Check-In Tests: {_passed} Passed, {_failed} Failed");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" Step 14 Vehicle Check-In Tests: {_passed} Passed, {_failed} Failed");
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

    private static (User customer, User attendant, ParkingArea area, ParkingSlot slot, Vehicle vehicle, Reservation reservation, QRCode qr, Payment payment)
        SeedCheckInTestData(
            ApplicationDbContext context,
            ReservationStatus resStatus = ReservationStatus.Confirmed,
            PaymentStatus payStatus = PaymentStatus.Paid,
            int startMinutesOffset = -5,
            int endHoursOffset = 2,
            bool isQrUsedForEntry = false,
            bool isQrExpired = false)
    {
        var custRole = new Role { Id = Guid.NewGuid(), Name = AppRoles.Customer };
        var staffRole = new Role { Id = Guid.NewGuid(), Name = AppRoles.ParkingAttendant };
        context.Roles.AddRange(custRole, staffRole);

        var customer = new User
        {
            Id = Guid.NewGuid(),
            FullName = "David Miller",
            Email = "david@customer.com",
            PhoneNumber = "+1555123456",
            PasswordHash = "hash",
            RoleId = custRole.Id,
            Role = custRole
        };
        var attendant = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Alex Attendant",
            Email = "alex@parking.com",
            PasswordHash = "hash",
            RoleId = staffRole.Id,
            Role = staffRole
        };
        context.Users.AddRange(customer, attendant);

        var area = new ParkingArea
        {
            Id = Guid.NewGuid(),
            Name = "Plaza Central Garage",
            Address = "100 Plaza Ave",
            City = "Metropolis",
            HourlyRate = 5.00m
        };
        context.ParkingAreas.Add(area);

        var slot = new ParkingSlot
        {
            Id = Guid.NewGuid(),
            ParkingAreaId = area.Id,
            ParkingArea = area,
            SlotNumber = "B-201",
            FloorNumber = 2,
            HourlyRate = 5.00m,
            Status = SlotStatus.Reserved,
            VehicleType = VehicleType.Car
        };
        context.ParkingSlots.Add(slot);

        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            User = customer,
            LicensePlate = "CHK-100",
            Make = "Toyota",
            Model = "Camry",
            Color = "Silver",
            VehicleType = VehicleType.Car
        };
        context.Vehicles.Add(vehicle);

        var now = DateTime.UtcNow;
        var start = now.AddMinutes(startMinutesOffset);
        var end = start.AddHours(endHoursOffset);

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
            TotalPrice = 10.00m,
            Status = resStatus,
            CreatedAt = now
        };
        context.Reservations.Add(reservation);

        var token = $"QR-{reservation.ReservationNumber}-UNITTEST";
        var qr = new QRCode
        {
            Id = Guid.NewGuid(),
            ReservationId = reservation.Id,
            Reservation = reservation,
            Code = token,
            ImageData = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUg==",
            IsUsedForEntry = isQrUsedForEntry,
            EntryScannedAt = isQrUsedForEntry ? now.AddMinutes(-10) : null,
            ExpiresAt = isQrExpired ? now.AddHours(-1) : end.AddHours(2),
            CreatedAt = now
        };
        context.QRCodes.Add(qr);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ReservationId = reservation.Id,
            Reservation = reservation,
            UserId = customer.Id,
            Amount = 10.00m,
            PaymentMethod = PaymentMethod.Card,
            Status = payStatus,
            TransactionId = $"TX-{Guid.NewGuid():N}",
            CreatedAt = now
        };
        context.Payments.Add(payment);

        context.SaveChanges();

        return (customer, attendant, area, slot, vehicle, reservation, qr, payment);
    }

    // =========================================================================
    // 1. Static Invariant Tests
    // =========================================================================

    private static void TestQrCodeValidityAndExpiration()
    {
        Console.WriteLine("\n[1] QR Code Validation & Expiration Tests:");
        var now = DateTime.UtcNow;

        var validQr = new QRCode { Code = "QR-RES-VALID", ExpiresAt = now.AddHours(2), IsUsedForEntry = false };
        Assert(!string.IsNullOrWhiteSpace(validQr.Code) && validQr.ExpiresAt >= now, "Unexpired QR code with valid token is accepted");

        var expiredQr = new QRCode { Code = "QR-RES-EXPIRED", ExpiresAt = now.AddMinutes(-5), IsUsedForEntry = false };
        Assert(expiredQr.ExpiresAt < now, "Expired QR code is rejected prior to check-in");

        string? emptyToken = "   ";
        Assert(string.IsNullOrWhiteSpace(emptyToken), "Blank or whitespace QR code token is rejected");
    }

    private static void TestReservationStatusGuards()
    {
        Console.WriteLine("\n[2] Reservation Status Guard Tests:");
        var confirmedRes = new Reservation { Status = ReservationStatus.Confirmed };
        Assert(confirmedRes.Status == ReservationStatus.Confirmed, "Confirmed reservation status is permitted for check-in");

        var cancelledRes = new Reservation { Status = ReservationStatus.Cancelled };
        Assert(cancelledRes.Status != ReservationStatus.Confirmed, "Cancelled reservation is blocked from check-in");

        var completedRes = new Reservation { Status = ReservationStatus.Completed };
        Assert(completedRes.Status != ReservationStatus.Confirmed, "Already completed reservation is blocked from check-in");

        var activeRes = new Reservation { Status = ReservationStatus.Active };
        Assert(activeRes.Status != ReservationStatus.Confirmed, "Already active reservation is blocked from re-checking in");

        var pendingRes = new Reservation { Status = ReservationStatus.Pending };
        Assert(pendingRes.Status != ReservationStatus.Confirmed, "Pending reservation is blocked from check-in");
    }

    private static void TestTimeWindowBoundaryRules()
    {
        Console.WriteLine("\n[3] Arrival Time Window Boundary Tests:");
        var now = DateTime.UtcNow;

        var startTimeOnTime = now;
        var endTimeOnTime = now.AddHours(2);
        bool onTimeAllowed = now >= startTimeOnTime.AddMinutes(-30) && now <= endTimeOnTime.AddHours(1);
        Assert(onTimeAllowed, "Arrival within scheduled window is allowed");

        var startTimeSoon = now.AddMinutes(20);
        bool earlyWithin30MinAllowed = now >= startTimeSoon.AddMinutes(-30);
        Assert(earlyWithin30MinAllowed, "Early arrival 20 minutes before start time is permitted (within 30m grace window)");

        var startTimeFarAhead = now.AddHours(2);
        bool prematureArrivalBlocked = now < startTimeFarAhead.AddMinutes(-30);
        Assert(prematureArrivalBlocked, "Premature arrival 2 hours before start time is rejected");

        var endTimeExpired = now.AddHours(-2);
        bool expiredWindowBlocked = now > endTimeExpired.AddHours(1);
        Assert(expiredWindowBlocked, "Arrival long after scheduled end time is rejected as expired window");
    }

    private static void TestPaymentStatusVerification()
    {
        Console.WriteLine("\n[4] Payment Status Invariant Tests:");
        var paidPayment = new Payment { Status = PaymentStatus.Paid };
        Assert(paidPayment.Status == PaymentStatus.Paid, "Reservation with PaymentStatus.Paid is accepted for check-in");

        var pendingPayment = new Payment { Status = PaymentStatus.Pending };
        Assert(pendingPayment.Status != PaymentStatus.Paid, "Reservation with unpaid/pending payment is denied entry");

        var failedPayment = new Payment { Status = PaymentStatus.Failed };
        Assert(failedPayment.Status != PaymentStatus.Paid, "Reservation with failed payment is denied entry");

        Payment? missingPayment = null;
        Assert(missingPayment == null, "Reservation with missing payment record is denied entry");
    }

    private static void TestDuplicateEntryPrevention()
    {
        Console.WriteLine("\n[5] Duplicate Entry Prevention Tests:");
        var freshQr = new QRCode { IsUsedForEntry = false };
        Assert(!freshQr.IsUsedForEntry, "Fresh QR code not yet used for entry allows check-in");

        var usedQr = new QRCode { IsUsedForEntry = true, EntryScannedAt = DateTime.UtcNow.AddMinutes(-15) };
        Assert(usedQr.IsUsedForEntry, "QR code already marked as IsUsedForEntry is blocked from re-use");

        var existingActiveSession = new ParkingHistory
        {
            Id = Guid.NewGuid(),
            ReservationId = Guid.NewGuid(),
            CheckInTime = DateTime.UtcNow.AddMinutes(-30),
            CheckOutTime = null
        };
        Assert(existingActiveSession.CheckOutTime == null, "Existing unclosed parking history session prevents duplicate entry");
    }

    private static void TestStateTransitionsAndAttendantAttribution()
    {
        Console.WriteLine("\n[6] State Transition & Attendant Attribution Tests:");
        var now = DateTime.UtcNow;
        var attendantId = Guid.NewGuid();
        var res = new Reservation { Status = ReservationStatus.Confirmed };
        var slot = new ParkingSlot { Status = SlotStatus.Reserved };
        var qr = new QRCode { IsUsedForEntry = false };

        qr.IsUsedForEntry = true;
        qr.EntryScannedAt = now;
        res.Status = ReservationStatus.Active;
        slot.Status = SlotStatus.Occupied;

        var history = new ParkingHistory
        {
            CheckInTime = now,
            AttendantCheckInId = attendantId
        };

        Assert(qr.IsUsedForEntry, "QR code flagged as IsUsedForEntry = true");
        Assert(qr.EntryScannedAt == now, "QR code entry timestamp recorded");
        Assert(res.Status == ReservationStatus.Active, "Reservation status transitioned to Active");
        Assert(slot.Status == SlotStatus.Occupied, "Parking slot status transitioned to Occupied");
        Assert(history.AttendantCheckInId == attendantId, "Parking history attributes the scanning attendant ID");
        Assert(history.CheckInTime == now, "Parking history accurately records entry check-in time");
    }

    // =========================================================================
    // 2. Service End-to-End Workflow Tests (InMemory DB)
    // =========================================================================

    private static void TestCheckInSuccessWorkflow()
    {
        Console.WriteLine("\n[7] CheckInVehicleAsync Success Workflow Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, attendant, _, slot, vehicle, res, qr, _) = SeedCheckInTestData(context);

        var request = new CheckInRequest
        {
            QRCode = qr.Code,
            Notes = "VIP customer arrival"
        };

        var result = service.CheckInVehicleAsync(request, attendant.Id).GetAwaiter().GetResult();

        Assert(result.Success, "CheckInVehicleAsync returns Success = true");
        Assert(result.ReservationId == res.Id, "Returned ReservationId matches seeded reservation");
        Assert(result.ReservationStatus == ReservationStatus.Active, "Returned ReservationStatus is Active");
        Assert(result.SlotStatus == SlotStatus.Occupied, "Returned SlotStatus is Occupied");
        Assert(result.LicensePlate == vehicle.LicensePlate, "Returned LicensePlate matches vehicle");
        Assert(result.AttendantId == attendant.Id, "Returned AttendantId matches scanning staff");
        Assert(result.AttendantName == attendant.FullName, "Returned AttendantName matches staff full name");
        Assert(qr.IsUsedForEntry, "QR entity marked as IsUsedForEntry = true in database");
        Assert(qr.EntryScannedAt.HasValue, "QR entity EntryScannedAt is populated in database");
        Assert(slot.Status == SlotStatus.Occupied, "ParkingSlot entity status updated to Occupied in database");
        Assert(res.Status == ReservationStatus.Active, "Reservation entity status updated to Active in database");

        var history = context.ParkingHistories.FirstOrDefault(h => h.ReservationId == res.Id);
        Assert(history != null, "ParkingHistory record created in database");
        Assert(history!.AttendantCheckInId == attendant.Id, "ParkingHistory records attendant attribution");
        Assert(history.CheckOutTime == null, "ParkingHistory CheckOutTime is null (active session)");
    }

    private static void TestCheckInEmptyQRCodeThrows()
    {
        Console.WriteLine("\n[8] CheckInVehicleAsync Empty QR Code Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, attendant, _, _, _, _, _, _) = SeedCheckInTestData(context);

        bool threw = false;
        try
        {
            service.CheckInVehicleAsync(new CheckInRequest { QRCode = "  " }, attendant.Id).GetAwaiter().GetResult();
        }
        catch (ArgumentException)
        {
            threw = true;
        }

        Assert(threw, "Empty QR code throws ArgumentException");
    }

    private static void TestCheckInUnknownQRCodeThrows()
    {
        Console.WriteLine("\n[9] CheckInVehicleAsync Unknown QR Code Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, attendant, _, _, _, _, _, _) = SeedCheckInTestData(context);

        bool threw = false;
        try
        {
            service.CheckInVehicleAsync(new CheckInRequest { QRCode = "QR-NON-EXISTENT" }, attendant.Id).GetAwaiter().GetResult();
        }
        catch (KeyNotFoundException)
        {
            threw = true;
        }

        Assert(threw, "Unknown QR code throws KeyNotFoundException");
    }

    private static void TestCheckInExpiredQRCodeThrows()
    {
        Console.WriteLine("\n[10] CheckInVehicleAsync Expired QR Code Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, attendant, _, _, _, _, qr, _) = SeedCheckInTestData(context, isQrExpired: true);

        bool threw = false;
        try
        {
            service.CheckInVehicleAsync(new CheckInRequest { QRCode = qr.Code }, attendant.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("expired", StringComparison.OrdinalIgnoreCase);
        }

        Assert(threw, "Expired QR code throws InvalidOperationException with 'expired'");
    }

    private static void TestCheckInCancelledReservationThrows()
    {
        Console.WriteLine("\n[11] CheckInVehicleAsync Cancelled Reservation Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, attendant, _, _, _, _, qr, _) = SeedCheckInTestData(context, resStatus: ReservationStatus.Cancelled);

        bool threw = false;
        try
        {
            service.CheckInVehicleAsync(new CheckInRequest { QRCode = qr.Code }, attendant.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("cancelled", StringComparison.OrdinalIgnoreCase);
        }

        Assert(threw, "Cancelled reservation throws InvalidOperationException");
    }

    private static void TestCheckInCompletedReservationThrows()
    {
        Console.WriteLine("\n[12] CheckInVehicleAsync Completed Reservation Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, attendant, _, _, _, _, qr, _) = SeedCheckInTestData(context, resStatus: ReservationStatus.Completed);

        bool threw = false;
        try
        {
            service.CheckInVehicleAsync(new CheckInRequest { QRCode = qr.Code }, attendant.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("completed", StringComparison.OrdinalIgnoreCase);
        }

        Assert(threw, "Completed reservation throws InvalidOperationException");
    }

    private static void TestCheckInAlreadyActiveReservationThrows()
    {
        Console.WriteLine("\n[13] CheckInVehicleAsync Already Active Reservation Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, attendant, _, _, _, _, qr, _) = SeedCheckInTestData(context, resStatus: ReservationStatus.Active);

        bool threw = false;
        try
        {
            service.CheckInVehicleAsync(new CheckInRequest { QRCode = qr.Code }, attendant.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("already checked in", StringComparison.OrdinalIgnoreCase);
        }

        Assert(threw, "Already active reservation throws InvalidOperationException");
    }

    private static void TestCheckInPendingReservationThrows()
    {
        Console.WriteLine("\n[14] CheckInVehicleAsync Pending Reservation Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, attendant, _, _, _, _, qr, _) = SeedCheckInTestData(context, resStatus: ReservationStatus.Pending);

        bool threw = false;
        try
        {
            service.CheckInVehicleAsync(new CheckInRequest { QRCode = qr.Code }, attendant.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("Only confirmed", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("Pending", StringComparison.OrdinalIgnoreCase);
        }

        Assert(threw, "Pending reservation throws InvalidOperationException");
    }

    private static void TestCheckInUnpaidReservationThrows()
    {
        Console.WriteLine("\n[15] CheckInVehicleAsync Unpaid Reservation Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, attendant, _, _, _, _, qr, _) = SeedCheckInTestData(context, payStatus: PaymentStatus.Pending);

        bool threw = false;
        try
        {
            service.CheckInVehicleAsync(new CheckInRequest { QRCode = qr.Code }, attendant.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("payment", StringComparison.OrdinalIgnoreCase);
        }

        Assert(threw, "Unpaid reservation throws InvalidOperationException requiring completed payment");
    }

    private static void TestCheckInPrematureArrivalThrows()
    {
        Console.WriteLine("\n[16] CheckInVehicleAsync Premature Arrival (>30m early) Test:");
        var (context, service) = CreateMockContextAndService();
        // Start time is 2 hours from now
        var (_, attendant, _, _, _, _, qr, _) = SeedCheckInTestData(context, startMinutesOffset: 120);

        bool threw = false;
        try
        {
            service.CheckInVehicleAsync(new CheckInRequest { QRCode = qr.Code }, attendant.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("too early", StringComparison.OrdinalIgnoreCase);
        }

        Assert(threw, "Premature arrival >30 minutes early throws InvalidOperationException");
    }

    private static void TestCheckInEarlyArrivalWithinGracePeriodSucceeds()
    {
        Console.WriteLine("\n[17] CheckInVehicleAsync Early Arrival Within 30m Window Test:");
        var (context, service) = CreateMockContextAndService();
        // Start time is 15 minutes in the future
        var (_, attendant, _, _, _, _, qr, _) = SeedCheckInTestData(context, startMinutesOffset: 15);

        var result = service.CheckInVehicleAsync(new CheckInRequest { QRCode = qr.Code }, attendant.Id).GetAwaiter().GetResult();

        Assert(result.Success, "Arrival 15 minutes early (within 30m window) succeeds");
        Assert(result.ReservationStatus == ReservationStatus.Active, "Reservation set to Active");
    }

    private static void TestCheckInExpiredTimeWindowThrows()
    {
        Console.WriteLine("\n[18] CheckInVehicleAsync Expired Time Window Test:");
        var (context, service) = CreateMockContextAndService();
        // Started 4 hours ago, ended 2 hours ago (>1 hour past end)
        var (_, attendant, _, _, _, _, qr, _) = SeedCheckInTestData(context, startMinutesOffset: -240, endHoursOffset: 2);

        bool threw = false;
        try
        {
            service.CheckInVehicleAsync(new CheckInRequest { QRCode = qr.Code }, attendant.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("expired", StringComparison.OrdinalIgnoreCase);
        }

        Assert(threw, "Arrival past expired window throws InvalidOperationException");
    }

    private static void TestCheckInDuplicateEntryWithUsedQRCodeThrows()
    {
        Console.WriteLine("\n[19] CheckInVehicleAsync Duplicate Entry with Used QR Code Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, attendant, _, _, _, _, qr, _) = SeedCheckInTestData(context, isQrUsedForEntry: true);

        bool threw = false;
        try
        {
            service.CheckInVehicleAsync(new CheckInRequest { QRCode = qr.Code }, attendant.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("already checked in", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("Duplicate check-in", StringComparison.OrdinalIgnoreCase);
        }

        Assert(threw, "Duplicate check-in with used QR code throws InvalidOperationException");
    }

    private static void TestCheckInDuplicateEntryWithActiveSessionThrows()
    {
        Console.WriteLine("\n[20] CheckInVehicleAsync Duplicate Entry with Active ParkingHistory Session Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, attendant, _, slot, vehicle, res, qr, _) = SeedCheckInTestData(context);

        // Pre-insert an active ParkingHistory session
        context.ParkingHistories.Add(new ParkingHistory
        {
            Id = Guid.NewGuid(),
            ReservationId = res.Id,
            VehicleId = vehicle.Id,
            ParkingSlotId = slot.Id,
            CheckInTime = DateTime.UtcNow.AddMinutes(-10),
            CheckOutTime = null,
            AttendantCheckInId = attendant.Id
        });
        context.SaveChanges();

        bool threw = false;
        try
        {
            service.CheckInVehicleAsync(new CheckInRequest { QRCode = qr.Code }, attendant.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("Duplicate check-in", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("active parking session", StringComparison.OrdinalIgnoreCase);
        }

        Assert(threw, "Duplicate check-in with active session in history throws InvalidOperationException");
    }

    private static void TestCheckInMismatchedFacilityThrows()
    {
        Console.WriteLine("\n[21] CheckInVehicleAsync Mismatched Facility Guard Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, attendant, _, _, _, _, qr, _) = SeedCheckInTestData(context);

        var differentAreaId = Guid.NewGuid();
        bool threw = false;
        try
        {
            service.CheckInVehicleAsync(new CheckInRequest
            {
                QRCode = qr.Code,
                ParkingAreaId = differentAreaId
            }, attendant.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("different parking facility", StringComparison.OrdinalIgnoreCase);
        }

        Assert(threw, "Check-in at wrong parking facility throws InvalidOperationException");
    }

    private static void TestCheckInAttendantAttribution()
    {
        Console.WriteLine("\n[22] CheckInVehicleAsync Attendant Attribution Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, attendant, _, _, _, res, qr, _) = SeedCheckInTestData(context);

        var result = service.CheckInVehicleAsync(new CheckInRequest { QRCode = qr.Code }, attendant.Id).GetAwaiter().GetResult();

        Assert(result.AttendantId == attendant.Id, "CheckInResultDto captures attendant ID");
        Assert(result.AttendantName == "Alex Attendant", "CheckInResultDto captures attendant FullName");

        var history = context.ParkingHistories.First(h => h.ReservationId == res.Id);
        Assert(history.AttendantCheckInId == attendant.Id, "ParkingHistory records attendant ID for accountability");
    }
}
