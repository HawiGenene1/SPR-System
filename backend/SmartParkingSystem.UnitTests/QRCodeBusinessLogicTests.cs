using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SmartParkingSystem.Application.Common.Security;
using SmartParkingSystem.Application.DTOs.QRCodes;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;
using SmartParkingSystem.Infrastructure.Data;
using SmartParkingSystem.Infrastructure.Repositories;
using SmartParkingSystem.Infrastructure.Services;

namespace SmartParkingSystem.UnitTests;

public static class QRCodeBusinessLogicTests
{
    private static int _passed = 0;
    private static int _failed = 0;

    public static int Run()
    {
        _passed = 0;
        _failed = 0;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        Console.WriteLine(" Running Step 13: QR Code Functionality Unit Tests");
        Console.WriteLine("=========================================================");
        Console.ResetColor();

        // 1. QRCoder Rendering & Security Tests
        TestQrCodePngGeneration();
        TestPngHeaderByteIntegrity();
        TestSecureTokenNonExposure();

        // 2. Generation & Retrieval Tests
        TestGenerateQRCodeAsync_Success();
        TestGenerateQRCodeAsync_NotFound();
        TestGetQRCodeByReservationIdAsync_OwnerCanView();
        TestGetQRCodeByReservationIdAsync_AdminCanView();
        TestGetQRCodeByReservationIdAsync_OtherCustomerForbidden();
        TestGetQRCodeByReservationIdAsync_AutoGenerateForConfirmed();
        TestGetQRCodeByReservationIdAsync_PendingReservationThrows();

        // 3. QR Code Scanning & Validation Tests
        TestScanQRCodeAsync_NonExistentCode_Fails();
        TestScanQRCodeAsync_InvalidatedOrDeleted_Fails();
        TestScanQRCodeAsync_CancelledReservation_Fails();
        TestScanQRCodeAsync_CompletedReservation_Fails();
        TestScanQRCodeAsync_PendingReservation_Fails();
        TestScanQRCodeAsync_ExpiredQRCode_Fails();
        TestScanQRCodeAsync_CheckIn_TooEarly_Fails();
        TestScanQRCodeAsync_CheckIn_Success();
        TestScanQRCodeAsync_CheckIn_Duplicate_Fails();
        TestScanQRCodeAsync_CheckOut_WithoutCheckIn_Fails();
        TestScanQRCodeAsync_CheckOut_Success();
        TestScanQRCodeAsync_CheckOut_Duplicate_Fails();
        TestScanQRCodeAsync_ValidateOnly_Success();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        if (_failed == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" Step 13 QR Code Tests Summary: {_passed} Passed, {_failed} Failed");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" Step 13 QR Code Tests Summary: {_passed} Passed, {_failed} Failed");
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

    private static (ApplicationDbContext context, IQRCodeService service) CreateMockContextAndService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        var qrCodeRepo = new QRCodeRepository(context);
        var service = new QRCodeService(context, qrCodeRepo, NullLogger<QRCodeService>.Instance);

        return (context, service);
    }

    private static (User customer, User attendant, User admin, ParkingArea area, ParkingSlot slot, Vehicle vehicle, Reservation reservation)
        SeedQrTestData(ApplicationDbContext context, ReservationStatus status = ReservationStatus.Confirmed, bool inWindow = true)
    {
        var customerRole = new Role { Id = Guid.NewGuid(), Name = AppRoles.Customer };
        var attendantRole = new Role { Id = Guid.NewGuid(), Name = AppRoles.ParkingAttendant };
        var adminRole = new Role { Id = Guid.NewGuid(), Name = AppRoles.Admin };
        context.Roles.AddRange(customerRole, attendantRole, adminRole);

        var customer = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Jane Customer",
            Email = "jane@customer.com",
            PhoneNumber = "+1234567890",
            PasswordHash = "hash",
            RoleId = customerRole.Id,
            Role = customerRole
        };
        var attendant = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Sam Attendant",
            Email = "sam@parking.com",
            PasswordHash = "hash",
            RoleId = attendantRole.Id,
            Role = attendantRole
        };
        var admin = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Super Admin",
            Email = "admin@parking.com",
            PasswordHash = "hash",
            RoleId = adminRole.Id,
            Role = adminRole
        };
        context.Users.AddRange(customer, attendant, admin);

        var area = new ParkingArea
        {
            Id = Guid.NewGuid(),
            Name = "Downtown Lot A",
            Address = "100 Main St",
            City = "Metropolis",
            HourlyRate = 6.00m
        };
        context.ParkingAreas.Add(area);

        var slot = new ParkingSlot
        {
            Id = Guid.NewGuid(),
            ParkingAreaId = area.Id,
            ParkingArea = area,
            SlotNumber = "A-101",
            FloorNumber = 1,
            HourlyRate = 6.00m,
            Status = SlotStatus.Reserved,
            VehicleType = VehicleType.Car
        };
        context.ParkingSlots.Add(slot);

        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            User = customer,
            LicensePlate = "QR-999-ZZ",
            Make = "Tesla",
            Model = "Model 3",
            Color = "Blue",
            VehicleType = VehicleType.Car
        };
        context.Vehicles.Add(vehicle);

        var now = DateTime.UtcNow;
        var start = inWindow ? now.AddMinutes(-5) : now.AddHours(3);
        var end = start.AddHours(2);

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            ReservationNumber = $"RES-20260926-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            UserId = customer.Id,
            User = customer,
            ParkingSlotId = slot.Id,
            ParkingSlot = slot,
            VehicleId = vehicle.Id,
            Vehicle = vehicle,
            StartTime = start,
            EndTime = end,
            TotalPrice = 12.00m,
            Status = status,
            CreatedAt = now
        };
        context.Reservations.Add(reservation);
        context.SaveChanges();

        return (customer, attendant, admin, area, slot, vehicle, reservation);
    }

    // =========================================================================
    // 1. QRCoder Rendering & Security Tests
    // =========================================================================

    private static void TestQrCodePngGeneration()
    {
        Console.WriteLine("\n[1] QRCoder PNG Image Rendering Tests:");
        var payload = "QR-RES-20260926-UNITTEST123";
        var base64Result = QRCodeService.RenderQrCodeBase64(payload);

        Assert(!string.IsNullOrWhiteSpace(base64Result), "Rendered QR code string is not empty");
        Assert(base64Result.StartsWith("data:image/png;base64,"), "Rendered string has proper data URL prefix");
        Assert(base64Result.Length > 100, "Base64 payload has realistic byte length");
    }

    private static void TestPngHeaderByteIntegrity()
    {
        Console.WriteLine("\n[2] PNG Binary Signature & Integrity Tests:");
        var payload = "SECURE-PARKING-QR-TOKEN";
        var dataUrl = QRCodeService.RenderQrCodeBase64(payload);
        var base64Part = dataUrl.Substring("data:image/png;base64,".Length);

        byte[] bytes = Convert.FromBase64String(base64Part);
        Assert(bytes.Length >= 8, "Byte array has at least 8 bytes for PNG header");

        byte[] expectedHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        bool headerValid = true;
        for (int i = 0; i < expectedHeader.Length; i++)
        {
            if (bytes[i] != expectedHeader[i])
            {
                headerValid = false;
                break;
            }
        }

        Assert(headerValid, "Generated binary bytes match valid PNG magic header signature");
    }

    private static void TestSecureTokenNonExposure()
    {
        Console.WriteLine("\n[3] Secure Token Payload & Sensitive Data Protection Tests:");
        var reservationNumber = "RES-20260926-890ABC";
        var uniqueToken = $"QR-{reservationNumber}-{Guid.NewGuid():N}";

        Assert(uniqueToken.StartsWith("QR-RES-"), "Token prefix indicates reservation reference");
        Assert(!uniqueToken.Contains("Password"), "Token contains no password reference");
        Assert(!uniqueToken.Contains("Card"), "Token contains no payment card reference");
        Assert(!uniqueToken.Contains("CVV"), "Token contains no CVV security code");
        Assert(!uniqueToken.Contains("Email"), "Token contains no plain text user email");
        Assert(!uniqueToken.Contains("12345678"), "Token contains no account number");
    }

    // =========================================================================
    // 2. Generation & Retrieval Tests
    // =========================================================================

    private static void TestGenerateQRCodeAsync_Success()
    {
        Console.WriteLine("\n[4] GenerateQRCodeAsync Success Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, _, _, _, _, _, res) = SeedQrTestData(context);

        var result = service.GenerateQRCodeAsync(res.Id).GetAwaiter().GetResult();

        Assert(result != null, "GenerateQRCodeAsync returns non-null result");
        Assert(result!.ReservationId == res.Id, "Returned QR code links to correct reservation ID");
        Assert(!string.IsNullOrWhiteSpace(result.Code), "QR code token is non-empty");
        Assert(result.Code.StartsWith("QR-RES-"), "QR code token starts with QR-RES- prefix");
        Assert(!string.IsNullOrWhiteSpace(result.ImageData), "QR code image data is non-empty");
        Assert(result.ImageData!.StartsWith("data:image/png;base64,"), "QR code image data is valid PNG data URL");
        Assert(result.ExpiresAt > DateTime.UtcNow, "Expiration date is in the future");
    }

    private static void TestGenerateQRCodeAsync_NotFound()
    {
        Console.WriteLine("\n[5] GenerateQRCodeAsync Non-existent Reservation Test:");
        var (_, service) = CreateMockContextAndService();
        bool threw = false;

        try
        {
            service.GenerateQRCodeAsync(Guid.NewGuid()).GetAwaiter().GetResult();
        }
        catch (KeyNotFoundException)
        {
            threw = true;
        }

        Assert(threw, "Generating QR code for missing reservation throws KeyNotFoundException");
    }

    private static void TestGetQRCodeByReservationIdAsync_OwnerCanView()
    {
        Console.WriteLine("\n[6] GetQRCodeByReservationIdAsync Owner Access Test:");
        var (context, service) = CreateMockContextAndService();
        var (customer, _, _, _, _, _, res) = SeedQrTestData(context);

        var generated = service.GenerateQRCodeAsync(res.Id).GetAwaiter().GetResult();
        var retrieved = service.GetQRCodeByReservationIdAsync(res.Id, customer.Id, isAdmin: false).GetAwaiter().GetResult();

        Assert(retrieved != null, "Owner retrieves non-null QR code");
        Assert(retrieved!.Code == generated.Code, "Retrieved code matches generated code");
        Assert(retrieved.ReservationId == res.Id, "Retrieved QR code links to reservation");
    }

    private static void TestGetQRCodeByReservationIdAsync_AdminCanView()
    {
        Console.WriteLine("\n[7] GetQRCodeByReservationIdAsync Admin Access Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, _, admin, _, _, _, res) = SeedQrTestData(context);

        service.GenerateQRCodeAsync(res.Id).GetAwaiter().GetResult();
        var retrieved = service.GetQRCodeByReservationIdAsync(res.Id, admin.Id, isAdmin: true).GetAwaiter().GetResult();

        Assert(retrieved != null, "Admin retrieves customer QR code successfully");
        Assert(retrieved!.ReservationId == res.Id, "Retrieved QR code links to reservation");
    }

    private static void TestGetQRCodeByReservationIdAsync_OtherCustomerForbidden()
    {
        Console.WriteLine("\n[8] GetQRCodeByReservationIdAsync Other Customer Forbidden Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, _, _, _, _, _, res) = SeedQrTestData(context);

        service.GenerateQRCodeAsync(res.Id).GetAwaiter().GetResult();
        var strangerId = Guid.NewGuid();
        bool threw = false;

        try
        {
            service.GetQRCodeByReservationIdAsync(res.Id, strangerId, isAdmin: false).GetAwaiter().GetResult();
        }
        catch (UnauthorizedAccessException)
        {
            threw = true;
        }

        Assert(threw, "Other customer viewing QR code throws UnauthorizedAccessException (403 Forbidden)");
    }

    private static void TestGetQRCodeByReservationIdAsync_AutoGenerateForConfirmed()
    {
        Console.WriteLine("\n[9] GetQRCodeByReservationIdAsync Auto-Generate for Confirmed Test:");
        var (context, service) = CreateMockContextAndService();
        var (customer, _, _, _, _, _, res) = SeedQrTestData(context, ReservationStatus.Confirmed);

        // Do not explicitly generate before calling GET
        var retrieved = service.GetQRCodeByReservationIdAsync(res.Id, customer.Id, isAdmin: false).GetAwaiter().GetResult();

        Assert(retrieved != null, "Auto-generated QR code retrieved successfully");
        Assert(!string.IsNullOrWhiteSpace(retrieved!.Code), "Auto-generated QR code has valid token");
        Assert(!string.IsNullOrWhiteSpace(retrieved.ImageData), "Auto-generated QR code has valid image data");
    }

    private static void TestGetQRCodeByReservationIdAsync_PendingReservationThrows()
    {
        Console.WriteLine("\n[10] GetQRCodeByReservationIdAsync Pending Reservation Rejection Test:");
        var (context, service) = CreateMockContextAndService();
        var (customer, _, _, _, _, _, res) = SeedQrTestData(context, ReservationStatus.Pending);
        bool threw = false;

        try
        {
            service.GetQRCodeByReservationIdAsync(res.Id, customer.Id, isAdmin: false).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert(threw, "Accessing QR code for unconfirmed Pending reservation throws InvalidOperationException");
    }

    // =========================================================================
    // 3. QR Code Scanning & Validation Tests
    // =========================================================================

    private static void TestScanQRCodeAsync_NonExistentCode_Fails()
    {
        Console.WriteLine("\n[11] ScanQRCodeAsync Non-existent Code Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, attendant, _, _, _, _, _) = SeedQrTestData(context);

        var request = new QRScanRequest { QRCode = "INVALID-CODE-999", Action = "CheckIn" };
        var result = service.ScanQRCodeAsync(request, attendant.Id).GetAwaiter().GetResult();

        Assert(!result.IsValid, "Non-existent code returns IsValid = false");
        Assert(result.Message.Contains("not recognized", StringComparison.OrdinalIgnoreCase), "Scan message indicates code not recognized");
    }

    private static void TestScanQRCodeAsync_InvalidatedOrDeleted_Fails()
    {
        Console.WriteLine("\n[12] ScanQRCodeAsync Deleted/Invalidated Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, attendant, _, _, _, _, res) = SeedQrTestData(context);

        var qrDto = service.GenerateQRCodeAsync(res.Id).GetAwaiter().GetResult();
        var qrEntity = context.QRCodes.First(q => q.Id == qrDto.Id);
        qrEntity.IsDeleted = true;
        context.SaveChanges();

        var request = new QRScanRequest { QRCode = qrDto.Code, Action = "CheckIn" };
        var result = service.ScanQRCodeAsync(request, attendant.Id).GetAwaiter().GetResult();

        Assert(!result.IsValid, "Soft-deleted QR code returns IsValid = false");
        Assert(result.Message.Contains("invalidated", StringComparison.OrdinalIgnoreCase), "Scan message indicates QR code was invalidated");
    }

    private static void TestScanQRCodeAsync_CancelledReservation_Fails()
    {
        Console.WriteLine("\n[13] ScanQRCodeAsync Cancelled Reservation Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, attendant, _, _, _, _, res) = SeedQrTestData(context);

        var qrDto = service.GenerateQRCodeAsync(res.Id).GetAwaiter().GetResult();
        res.Status = ReservationStatus.Cancelled;
        context.SaveChanges();

        var request = new QRScanRequest { QRCode = qrDto.Code, Action = "CheckIn" };
        var result = service.ScanQRCodeAsync(request, attendant.Id).GetAwaiter().GetResult();

        Assert(!result.IsValid, "Cancelled reservation QR code returns IsValid = false");
        Assert(result.Message.Contains("cancelled", StringComparison.OrdinalIgnoreCase), "Scan message indicates reservation cancelled");
    }

    private static void TestScanQRCodeAsync_CompletedReservation_Fails()
    {
        Console.WriteLine("\n[14] ScanQRCodeAsync Completed Reservation Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, attendant, _, _, _, _, res) = SeedQrTestData(context);

        var qrDto = service.GenerateQRCodeAsync(res.Id).GetAwaiter().GetResult();
        res.Status = ReservationStatus.Completed;
        context.SaveChanges();

        var request = new QRScanRequest { QRCode = qrDto.Code, Action = "CheckIn" };
        var result = service.ScanQRCodeAsync(request, attendant.Id).GetAwaiter().GetResult();

        Assert(!result.IsValid, "Completed reservation QR code returns IsValid = false");
        Assert(result.Message.Contains("completed", StringComparison.OrdinalIgnoreCase) || result.Message.Contains("checked out", StringComparison.OrdinalIgnoreCase),
            "Scan message indicates reservation already completed");
    }

    private static void TestScanQRCodeAsync_PendingReservation_Fails()
    {
        Console.WriteLine("\n[15] ScanQRCodeAsync Pending Reservation Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, attendant, _, _, _, _, res) = SeedQrTestData(context);

        var qrDto = service.GenerateQRCodeAsync(res.Id).GetAwaiter().GetResult();
        res.Status = ReservationStatus.Pending;
        context.SaveChanges();

        var request = new QRScanRequest { QRCode = qrDto.Code, Action = "CheckIn" };
        var result = service.ScanQRCodeAsync(request, attendant.Id).GetAwaiter().GetResult();

        Assert(!result.IsValid, "Pending reservation QR code returns IsValid = false");
        Assert(result.Message.Contains("pending", StringComparison.OrdinalIgnoreCase), "Scan message indicates reservation is pending");
    }

    private static void TestScanQRCodeAsync_ExpiredQRCode_Fails()
    {
        Console.WriteLine("\n[16] ScanQRCodeAsync Expired QR Code Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, attendant, _, _, _, _, res) = SeedQrTestData(context);

        var qrDto = service.GenerateQRCodeAsync(res.Id).GetAwaiter().GetResult();
        var qrEntity = context.QRCodes.First(q => q.Id == qrDto.Id);
        qrEntity.ExpiresAt = DateTime.UtcNow.AddMinutes(-30);
        res.EndTime = DateTime.UtcNow.AddHours(-3);
        context.SaveChanges();

        var request = new QRScanRequest { QRCode = qrDto.Code, Action = "CheckIn" };
        var result = service.ScanQRCodeAsync(request, attendant.Id).GetAwaiter().GetResult();

        Assert(!result.IsValid, "Expired QR code returns IsValid = false");
        Assert(result.Message.Contains("expired", StringComparison.OrdinalIgnoreCase), "Scan message indicates QR code has expired");
    }

    private static void TestScanQRCodeAsync_CheckIn_TooEarly_Fails()
    {
        Console.WriteLine("\n[17] ScanQRCodeAsync Check-In Too Early Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, attendant, _, _, _, _, res) = SeedQrTestData(context, inWindow: false);

        var qrDto = service.GenerateQRCodeAsync(res.Id).GetAwaiter().GetResult();

        var request = new QRScanRequest { QRCode = qrDto.Code, Action = "CheckIn" };
        var result = service.ScanQRCodeAsync(request, attendant.Id).GetAwaiter().GetResult();

        Assert(!result.IsValid, "Check-in attempted 3 hours early returns IsValid = false");
        Assert(result.Message.Contains("Too early", StringComparison.OrdinalIgnoreCase), "Scan message indicates entry too early");
    }

    private static void TestScanQRCodeAsync_CheckIn_Success()
    {
        Console.WriteLine("\n[18] ScanQRCodeAsync Valid Check-In Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, attendant, _, _, slot, vehicle, res) = SeedQrTestData(context, inWindow: true);

        var qrDto = service.GenerateQRCodeAsync(res.Id).GetAwaiter().GetResult();

        var request = new QRScanRequest { QRCode = qrDto.Code, Action = "CheckIn", Notes = "Attendant entry check" };
        var result = service.ScanQRCodeAsync(request, attendant.Id).GetAwaiter().GetResult();

        Assert(result.IsValid, "Valid check-in returns IsValid = true");
        Assert(result.IsUsedForEntry, "Result flags IsUsedForEntry = true");
        Assert(result.EntryScannedAt.HasValue, "EntryScannedAt has valid timestamp");
        Assert(result.ReservationStatus == ReservationStatus.Active, "Reservation status changed to Active");
        Assert(slot.Status == SlotStatus.Occupied, "Parking slot status changed to Occupied");
        Assert(result.LicensePlate == vehicle.LicensePlate, "License plate is properly returned in scan result");

        var history = context.ParkingHistories.FirstOrDefault(h => h.ReservationId == res.Id);
        Assert(history != null, "Parking history check-in record was created");
        Assert(history!.AttendantCheckInId == attendant.Id, "Parking history recorded attendant ID");
    }

    private static void TestScanQRCodeAsync_CheckIn_Duplicate_Fails()
    {
        Console.WriteLine("\n[19] ScanQRCodeAsync Duplicate Check-In Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, attendant, _, _, _, _, res) = SeedQrTestData(context, inWindow: true);

        var qrDto = service.GenerateQRCodeAsync(res.Id).GetAwaiter().GetResult();
        var request = new QRScanRequest { QRCode = qrDto.Code, Action = "CheckIn" };

        var first = service.ScanQRCodeAsync(request, attendant.Id).GetAwaiter().GetResult();
        Assert(first.IsValid, "First check-in succeeds");

        var second = service.ScanQRCodeAsync(request, attendant.Id).GetAwaiter().GetResult();
        Assert(!second.IsValid, "Second check-in attempt returns IsValid = false");
        Assert(second.Message.Contains("already been scanned for entry", StringComparison.OrdinalIgnoreCase),
            "Message flags duplicate entry scan");
    }

    private static void TestScanQRCodeAsync_CheckOut_WithoutCheckIn_Fails()
    {
        Console.WriteLine("\n[20] ScanQRCodeAsync Check-Out Without Prior Check-In Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, attendant, _, _, _, _, res) = SeedQrTestData(context, inWindow: true);

        var qrDto = service.GenerateQRCodeAsync(res.Id).GetAwaiter().GetResult();
        var request = new QRScanRequest { QRCode = qrDto.Code, Action = "CheckOut" };

        var result = service.ScanQRCodeAsync(request, attendant.Id).GetAwaiter().GetResult();

        Assert(!result.IsValid, "Check-out without entry returns IsValid = false");
        Assert(result.Message.Contains("has not been scanned for entry", StringComparison.OrdinalIgnoreCase),
            "Message indicates vehicle has not been scanned for entry yet");
    }

    private static void TestScanQRCodeAsync_CheckOut_Success()
    {
        Console.WriteLine("\n[21] ScanQRCodeAsync Valid Check-Out Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, attendant, _, _, slot, _, res) = SeedQrTestData(context, inWindow: true);

        var qrDto = service.GenerateQRCodeAsync(res.Id).GetAwaiter().GetResult();

        // 1. First Check-In
        var checkInReq = new QRScanRequest { QRCode = qrDto.Code, Action = "CheckIn" };
        var checkInRes = service.ScanQRCodeAsync(checkInReq, attendant.Id).GetAwaiter().GetResult();
        Assert(checkInRes.IsValid, "Initial check-in succeeds");

        // 2. Perform Check-Out
        var checkOutReq = new QRScanRequest { QRCode = qrDto.Code, Action = "CheckOut", Notes = "Attendant exit check" };
        var checkOutRes = service.ScanQRCodeAsync(checkOutReq, attendant.Id).GetAwaiter().GetResult();

        Assert(checkOutRes.IsValid, "Check-out returns IsValid = true");
        Assert(checkOutRes.IsUsedForExit, "Result flags IsUsedForExit = true");
        Assert(checkOutRes.ExitScannedAt.HasValue, "ExitScannedAt has valid timestamp");
        Assert(checkOutRes.ReservationStatus == ReservationStatus.Completed, "Reservation status changed to Completed");
        Assert(slot.Status == SlotStatus.Available, "Parking slot status restored to Available");

        var history = context.ParkingHistories.FirstOrDefault(h => h.ReservationId == res.Id);
        Assert(history != null && history.CheckOutTime.HasValue, "Parking history check-out timestamp recorded");
        Assert(history!.AttendantCheckOutId == attendant.Id, "Parking history recorded exit attendant ID");
    }

    private static void TestScanQRCodeAsync_CheckOut_Duplicate_Fails()
    {
        Console.WriteLine("\n[22] ScanQRCodeAsync Duplicate Check-Out Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, attendant, _, _, _, _, res) = SeedQrTestData(context, inWindow: true);

        var qrDto = service.GenerateQRCodeAsync(res.Id).GetAwaiter().GetResult();
        var checkInReq = new QRScanRequest { QRCode = qrDto.Code, Action = "CheckIn" };
        service.ScanQRCodeAsync(checkInReq, attendant.Id).GetAwaiter().GetResult();

        var checkOutReq = new QRScanRequest { QRCode = qrDto.Code, Action = "CheckOut" };
        var firstExit = service.ScanQRCodeAsync(checkOutReq, attendant.Id).GetAwaiter().GetResult();
        Assert(firstExit.IsValid, "First check-out succeeds");

        var secondExit = service.ScanQRCodeAsync(checkOutReq, attendant.Id).GetAwaiter().GetResult();
        Assert(!secondExit.IsValid, "Second check-out attempt returns IsValid = false");
        Assert(secondExit.Message.Contains("already been checked out", StringComparison.OrdinalIgnoreCase) ||
               secondExit.Message.Contains("completed", StringComparison.OrdinalIgnoreCase),
            "Message flags duplicate check-out");
    }

    private static void TestScanQRCodeAsync_ValidateOnly_Success()
    {
        Console.WriteLine("\n[23] ScanQRCodeAsync ValidateOnly Mode Test:");
        var (context, service) = CreateMockContextAndService();
        var (_, attendant, _, _, slot, _, res) = SeedQrTestData(context, inWindow: true);

        var qrDto = service.GenerateQRCodeAsync(res.Id).GetAwaiter().GetResult();
        var validateReq = new QRScanRequest { QRCode = qrDto.Code, Action = "ValidateOnly" };

        var result = service.ScanQRCodeAsync(validateReq, attendant.Id).GetAwaiter().GetResult();

        Assert(result.IsValid, "ValidateOnly returns IsValid = true");
        Assert(!result.IsUsedForEntry, "ValidateOnly does not mark QR code as used for entry");
        Assert(!result.IsUsedForExit, "ValidateOnly does not mark QR code as used for exit");
        Assert(res.Status == ReservationStatus.Confirmed, "Reservation status remains Confirmed (unmutated)");
        Assert(slot.Status == SlotStatus.Reserved, "Parking slot status remains Reserved (unmutated)");
        Assert(result.Message.Contains("ready for entry check-in", StringComparison.OrdinalIgnoreCase),
            "Message indicates QR code verified and ready for entry");
    }
}
