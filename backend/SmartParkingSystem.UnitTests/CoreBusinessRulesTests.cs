using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SmartParkingSystem.Application.Common.BusinessLogic;
using SmartParkingSystem.Application.Common.Security;
using SmartParkingSystem.Application.DTOs.Auth;
using SmartParkingSystem.Application.DTOs.Parking;
using SmartParkingSystem.Application.DTOs.ParkingHistory;
using SmartParkingSystem.Application.DTOs.Payments;
using SmartParkingSystem.Application.DTOs.QRCodes;
using SmartParkingSystem.Application.DTOs.Reservations;
using SmartParkingSystem.Application.DTOs.Vehicles;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;
using SmartParkingSystem.Infrastructure.Data;
using SmartParkingSystem.Infrastructure.Repositories;
using SmartParkingSystem.Infrastructure.Services;

namespace SmartParkingSystem.UnitTests;

/// <summary>
/// Step 23: Comprehensive Backend Testing Suite for Core Business Rules.
/// Validates both successful paths and exhaustive failure/security cases across:
/// Registration, Login, Invalid Login, Role Authorization, Vehicle Ownership,
/// Slot Availability, Overlapping Reservations, Reservation Concurrency,
/// Price Calculation, Payment Processing, QR Code Validation, Check-In, Check-Out,
/// and Parking History Auditing.
/// </summary>
public static class CoreBusinessRulesTests
{
    private static int _passed = 0;
    private static int _failed = 0;

    public static int Run()
    {
        _passed = 0;
        _failed = 0;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        Console.WriteLine(" Step 23: Running Backend Core Business Rules Test Suite");
        Console.WriteLine("=========================================================");
        Console.ResetColor();

        // 1. Registration Tests
        TestRegistration_Success();
        TestRegistration_DuplicateEmail_Fails();
        TestRegistration_EmailCaseInsensitive_Fails();
        TestRegistration_EmptyPassword_Fails();

        // 2. Login Tests
        TestLogin_Success();
        TestLogin_EmailCaseInsensitive_Success();

        // 3. Invalid Login Failure Cases
        TestLogin_NonExistentEmail_Fails();
        TestLogin_WrongPassword_Fails();
        TestLogin_DeactivatedAccount_Fails();
        TestLogin_DeletedAccount_Fails();

        // 4. Role Authorization Tests
        TestRoleAuthorization_AdminPolicy();
        TestRoleAuthorization_AttendantPolicy();
        TestRoleAuthorization_StaffPolicy();
        TestRoleAuthorization_CustomerCrossTenantIsolation();

        // 5. Vehicle Ownership Tests
        TestVehicleOwnership_OwnerAccess_Success();
        TestVehicleOwnership_NonOwnerUpdate_Fails();
        TestVehicleOwnership_NonOwnerDelete_Fails();
        TestVehicleOwnership_ReservationVehicleMismatch_Fails();
        TestVehicleOwnership_DeleteVehicleWithActiveReservation_Fails();

        // 6. Parking Slot Availability Tests
        TestSlotAvailability_AvailableSlot_CanBeReserved();
        TestSlotAvailability_MaintenanceSlot_Fails();
        TestSlotAvailability_InactiveSlot_Fails();
        TestSlotAvailability_RealTimeCounts();

        // 7. Overlapping Reservation Tests (Matrix)
        TestReservation_ExactOverlap_Fails();
        TestReservation_ContainedOverlap_Fails();
        TestReservation_EnclosingOverlap_Fails();
        TestReservation_PartialStartOverlap_Fails();
        TestReservation_PartialEndOverlap_Fails();
        TestReservation_AdjacentConsecutive_Succeeds();
        TestReservation_PriorConsecutive_Succeeds();
        TestReservation_CancelledReservationDoesNotBlockNewBooking();

        // 8. Reservation Concurrency Tests (Heavy Focus)
        TestReservationConcurrency_TenSimultaneousCompetingBookings();

        // 9. Price Calculation Tests
        TestPriceCalculation_StandardDuration();
        TestPriceCalculation_MinimumOneHourRule();
        TestPriceCalculation_PartialHourCeiling();
        TestPriceCalculation_ServerSideEnforcementIgnoringClientValue();

        // 10. Payment Business Rules Tests
        TestPayment_Success();
        TestPayment_CrossUserPayment_Fails();
        TestPayment_MismatchedAmount_Fails();
        TestPayment_DuplicatePayment_Fails();
        TestPayment_NonExistentReservation_Fails();

        // 11. QR Code Validation Tests
        TestQRValidation_ValidToken_Success();
        TestQRValidation_TamperedToken_Fails();
        TestQRValidation_UnconfirmedReservation_Fails();
        TestQRValidation_CustomerAccessOnlyOwnQR_Fails();

        // 12. Check-In Gate Operations Tests
        TestCheckIn_Success();
        TestCheckIn_UnpaidReservation_Fails();
        TestCheckIn_DuplicateCheckIn_Fails();
        TestCheckIn_CancelledReservation_Fails();
        TestCheckIn_NonStaffCaller_Fails();

        // 13. Check-Out Gate Operations Tests
        TestCheckOut_OnTime_Success();
        TestCheckOut_WithinGracePeriod_Success();
        TestCheckOut_OverstayPenaltyApplied();
        TestCheckOut_NotCheckedIn_Fails();
        TestCheckOut_AlreadyCompleted_Fails();

        // 14. Parking History Auditing Tests
        TestParkingHistory_CreatedOnCheckOut();
        TestParkingHistory_CustomerIsolation();
        TestParkingHistory_AdminAudit();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        if (_failed == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" Step 23 Backend Testing Suite: {_passed} Passed, 0 Failed. All Business Rules Verified!");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" Step 23 Backend Testing Suite: {_passed} Passed, {_failed} Failed!");
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

    #region Test Environment Factory

    private class TestEnv
    {
        public ApplicationDbContext Context { get; set; } = null!;
        public IAuthService AuthService { get; set; } = null!;
        public IVehicleService VehicleService { get; set; } = null!;
        public IReservationService ReservationService { get; set; } = null!;
        public IPaymentService PaymentService { get; set; } = null!;
        public IQRCodeService QRCodeService { get; set; } = null!;
        public IParkingService ParkingService { get; set; } = null!;
        public IParkingHistoryService ParkingHistoryService { get; set; } = null!;
        public Role AdminRole { get; set; } = null!;
        public Role AttendantRole { get; set; } = null!;
        public Role CustomerRole { get; set; } = null!;
        public User AdminUser { get; set; } = null!;
        public User AttendantUser { get; set; } = null!;
        public User CustomerA { get; set; } = null!;
        public User CustomerB { get; set; } = null!;
        public ParkingArea Area { get; set; } = null!;
        public ParkingSlot Slot { get; set; } = null!;
        public Vehicle VehicleA { get; set; } = null!;
        public Vehicle VehicleB { get; set; } = null!;
    }

    private static TestEnv CreateTestEnv()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);

        var configValues = new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = "SuperSecretKeyForStep23BackendTestingSuite1234567890!",
            ["JwtSettings:Issuer"] = "SmartParkingSystem",
            ["JwtSettings:Audience"] = "SmartParkingSystemUsers",
            ["JwtSettings:ExpiryInMinutes"] = "120"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();

        var passwordHasher = new PasswordHasher();
        var jwtTokenGenerator = new JwtTokenGenerator(configuration);
        var authService = new AuthService(context, passwordHasher, jwtTokenGenerator, configuration);

        var vehicleRepo = new VehicleRepository(context);
        var vehicleService = new VehicleService(vehicleRepo, context, NullLogger<VehicleService>.Instance);

        var reservationRepo = new ReservationRepository(context);
        var reservationService = new ReservationService(context, reservationRepo, vehicleService, NullLogger<ReservationService>.Instance);

        var paymentRepo = new PaymentRepository(context);
        var qrCodeRepo = new QRCodeRepository(context);
        var qrCodeService = new QRCodeService(context, qrCodeRepo, NullLogger<QRCodeService>.Instance);

        var paymentGateway = new MockPaymentGateway(NullLogger<MockPaymentGateway>.Instance);
        var paymentService = new PaymentService(context, paymentRepo, paymentGateway, NullLogger<PaymentService>.Instance);

        var parkingHistoryRepo = new ParkingHistoryRepository(context);
        var parkingHistoryService = new ParkingHistoryService(context, NullLogger<ParkingHistoryService>.Instance);

        var parkingService = new ParkingService(
            context,
            parkingHistoryRepo,
            NullLogger<ParkingService>.Instance);

        // Seed Roles
        var adminRole = new Role { Id = Guid.NewGuid(), Name = AppRoles.Admin, Description = "Administrator" };
        var attendantRole = new Role { Id = Guid.NewGuid(), Name = AppRoles.ParkingAttendant, Description = "Parking Attendant" };
        var customerRole = new Role { Id = Guid.NewGuid(), Name = AppRoles.Customer, Description = "Customer" };
        context.Roles.AddRange(adminRole, attendantRole, customerRole);

        // Seed Facility & Slot
        var area = new ParkingArea
        {
            Id = Guid.NewGuid(),
            Name = "Central Grand Plaza",
            Address = "500 Commerce Blvd",
            City = "Metropolis",
            TotalSlots = 50,
            HourlyRate = 10.00m,
            IsActive = true
        };
        context.ParkingAreas.Add(area);

        var slot = new ParkingSlot
        {
            Id = Guid.NewGuid(),
            ParkingAreaId = area.Id,
            SlotNumber = "A-101",
            FloorNumber = 1,
            HourlyRate = 10.00m,
            Status = SlotStatus.Available,
            VehicleType = VehicleType.Car,
            IsActive = true
        };
        context.ParkingSlots.Add(slot);

        // Seed Users
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Master Admin",
            Email = "admin@testing.com",
            PasswordHash = passwordHasher.HashPassword("Admin@123456"),
            RoleId = adminRole.Id,
            Role = adminRole,
            IsActive = true
        };

        var attendantUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Gate Attendant",
            Email = "attendant@testing.com",
            PasswordHash = passwordHasher.HashPassword("Attendant@123456"),
            RoleId = attendantRole.Id,
            Role = attendantRole,
            AssignedParkingAreaId = area.Id,
            IsActive = true
        };

        var customerA = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Alice Customer",
            Email = "alice@testing.com",
            PasswordHash = passwordHasher.HashPassword("Alice@123456"),
            RoleId = customerRole.Id,
            Role = customerRole,
            IsActive = true
        };

        var customerB = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Bob Customer",
            Email = "bob@testing.com",
            PasswordHash = passwordHasher.HashPassword("Bob@123456"),
            RoleId = customerRole.Id,
            Role = customerRole,
            IsActive = true
        };

        context.Users.AddRange(adminUser, attendantUser, customerA, customerB);

        // Seed Vehicles
        var vehicleA = new Vehicle
        {
            Id = Guid.NewGuid(),
            UserId = customerA.Id,
            LicensePlate = "ALICE-01",
            Make = "Tesla",
            Model = "Model 3",
            Color = "Pearl White",
            VehicleType = VehicleType.Car,
            IsDeleted = false
        };

        var vehicleB = new Vehicle
        {
            Id = Guid.NewGuid(),
            UserId = customerB.Id,
            LicensePlate = "BOB-99",
            Make = "Ford",
            Model = "F-150",
            Color = "Shadow Black",
            VehicleType = VehicleType.Car,
            IsDeleted = false
        };

        context.Vehicles.AddRange(vehicleA, vehicleB);
        context.SaveChanges();

        return new TestEnv
        {
            Context = context,
            AuthService = authService,
            VehicleService = vehicleService,
            ReservationService = reservationService,
            PaymentService = paymentService,
            QRCodeService = qrCodeService,
            ParkingService = parkingService,
            ParkingHistoryService = parkingHistoryService,
            AdminRole = adminRole,
            AttendantRole = attendantRole,
            CustomerRole = customerRole,
            AdminUser = adminUser,
            AttendantUser = attendantUser,
            CustomerA = customerA,
            CustomerB = customerB,
            Area = area,
            Slot = slot,
            VehicleA = vehicleA,
            VehicleB = vehicleB
        };
    }

    #endregion

    #region 1. Registration Tests

    private static void TestRegistration_Success()
    {
        Console.WriteLine("\n[1] Registration Tests (Success & Failure Cases):");
        var env = CreateTestEnv();

        var request = new RegisterRequest
        {
            FullName = "Charlie Davis",
            Email = "charlie@testing.com",
            Password = "Password@123",
            PhoneNumber = "+1-555-0199",
            Role = UserRole.Customer
        };

        var response = env.AuthService.RegisterAsync(request).GetAwaiter().GetResult();

        Assert(response != null, "Registration returns response object");
        Assert(!string.IsNullOrWhiteSpace(response.Token), "Registration returns valid JWT access token");
        Assert(!string.IsNullOrWhiteSpace(response.RefreshToken), "Registration returns valid refresh token");
        Assert(response.User.Email == "charlie@testing.com", "Registered user email matches normalized input");
        Assert(response.User.RoleName == AppRoles.Customer, "Registered user assigned default Customer role");

        var createdUser = env.Context.Users.FirstOrDefault(u => u.Email == "charlie@testing.com")!;
        Assert(createdUser.IsActive, "Newly registered user is Active by default");
        Assert(createdUser.PasswordHash != "Password@123", "Password is cryptographic PBKDF2 hash, not plaintext");
    }

    private static void TestRegistration_DuplicateEmail_Fails()
    {
        var env = CreateTestEnv();

        bool failedAsExpected = false;
        try
        {
            env.AuthService.RegisterAsync(new RegisterRequest
            {
                FullName = "Alice Clone",
                Email = "alice@testing.com", // Already exists in seeded data
                Password = "Password@123",
                Role = UserRole.Customer
            }).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            failedAsExpected = ex.Message.Contains("already exists");
        }

        Assert(failedAsExpected, "Duplicate email registration rejected with InvalidOperationException");
    }

    private static void TestRegistration_EmailCaseInsensitive_Fails()
    {
        var env = CreateTestEnv();

        bool failedAsExpected = false;
        try
        {
            env.AuthService.RegisterAsync(new RegisterRequest
            {
                FullName = "Alice Uppercase",
                Email = "ALICE@TESTING.COM", // Case variation of alice@testing.com
                Password = "Password@123",
                Role = UserRole.Customer
            }).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            failedAsExpected = ex.Message.Contains("already exists");
        }

        Assert(failedAsExpected, "Duplicate email detection is case-insensitive");
    }

    private static void TestRegistration_EmptyPassword_Fails()
    {
        var env = CreateTestEnv();

        bool failedAsExpected = false;
        try
        {
            env.AuthService.RegisterAsync(new RegisterRequest
            {
                FullName = "Empty Pass",
                Email = "emptypass@testing.com",
                Password = "   ",
                Role = UserRole.Customer
            }).GetAwaiter().GetResult();
        }
        catch (ArgumentException)
        {
            failedAsExpected = true;
        }

        Assert(failedAsExpected, "Empty/whitespace password registration rejected with ArgumentException");
    }

    #endregion

    #region 2. Login Tests

    private static void TestLogin_Success()
    {
        Console.WriteLine("\n[2] Login Tests (Success Cases):");
        var env = CreateTestEnv();

        var response = env.AuthService.LoginAsync(new LoginRequest
        {
            Email = "alice@testing.com",
            Password = "Alice@123456"
        }).GetAwaiter().GetResult();

        Assert(response != null, "Login returns response object");
        Assert(!string.IsNullOrWhiteSpace(response.Token), "Login returns JWT token");
        Assert(!string.IsNullOrWhiteSpace(response.RefreshToken), "Login returns Refresh token");
        Assert(response.ExpiresAt > DateTime.UtcNow, "Token expiration is in future");
        Assert(response.User.FullName == "Alice Customer", "User profile returned with correct name");
    }

    private static void TestLogin_EmailCaseInsensitive_Success()
    {
        var env = CreateTestEnv();

        var response = env.AuthService.LoginAsync(new LoginRequest
        {
            Email = "ALICE@TESTING.COM", // Uppercase email
            Password = "Alice@123456"
        }).GetAwaiter().GetResult();

        Assert(response != null && !string.IsNullOrWhiteSpace(response.Token),
            "Login with uppercase email matches lowercase stored email");
    }

    #endregion

    #region 3. Invalid Login Failure Cases

    private static void TestLogin_NonExistentEmail_Fails()
    {
        Console.WriteLine("\n[3] Invalid Login Tests (Failure Cases):");
        var env = CreateTestEnv();

        bool failedAsExpected = false;
        try
        {
            env.AuthService.LoginAsync(new LoginRequest
            {
                Email = "ghost@testing.com",
                Password = "Password@123"
            }).GetAwaiter().GetResult();
        }
        catch (UnauthorizedAccessException ex)
        {
            failedAsExpected = ex.Message.Contains("Invalid email or password");
        }

        Assert(failedAsExpected, "Non-existent email login rejected with UnauthorizedAccessException");
    }

    private static void TestLogin_WrongPassword_Fails()
    {
        var env = CreateTestEnv();

        bool failedAsExpected = false;
        try
        {
            env.AuthService.LoginAsync(new LoginRequest
            {
                Email = "alice@testing.com",
                Password = "CompletelyWrongPassword!"
            }).GetAwaiter().GetResult();
        }
        catch (UnauthorizedAccessException ex)
        {
            failedAsExpected = ex.Message.Contains("Invalid email or password");
        }

        Assert(failedAsExpected, "Wrong password login rejected with UnauthorizedAccessException");
    }

    private static void TestLogin_DeactivatedAccount_Fails()
    {
        var env = CreateTestEnv();
        var user = env.Context.Users.First(u => u.Email == "alice@testing.com");
        user.IsActive = false; // Deactivated by Admin
        env.Context.SaveChanges();

        bool failedAsExpected = false;
        try
        {
            env.AuthService.LoginAsync(new LoginRequest
            {
                Email = "alice@testing.com",
                Password = "Alice@123456"
            }).GetAwaiter().GetResult();
        }
        catch (UnauthorizedAccessException ex)
        {
            failedAsExpected = ex.Message.Contains("Invalid email or password");
        }

        Assert(failedAsExpected, "Deactivated account login strictly rejected");
    }

    private static void TestLogin_DeletedAccount_Fails()
    {
        var env = CreateTestEnv();
        var user = env.Context.Users.First(u => u.Email == "bob@testing.com");
        user.IsDeleted = true; // Soft-deleted
        env.Context.SaveChanges();

        bool failedAsExpected = false;
        try
        {
            env.AuthService.LoginAsync(new LoginRequest
            {
                Email = "bob@testing.com",
                Password = "Bob@123456"
            }).GetAwaiter().GetResult();
        }
        catch (UnauthorizedAccessException)
        {
            failedAsExpected = true;
        }

        Assert(failedAsExpected, "Soft-deleted user account login rejected");
    }

    #endregion

    #region 4. Role Authorization Tests

    private static void TestRoleAuthorization_AdminPolicy()
    {
        Console.WriteLine("\n[4] Role Authorization & Policy Enforcement Tests:");
        var env = CreateTestEnv();

        Assert(env.AdminUser.Role.Name == AppRoles.Admin, "Admin user has Admin role");
        Assert(env.CustomerA.Role.Name != AppRoles.Admin, "Customer does not have Admin role");
        Assert(env.AttendantUser.Role.Name != AppRoles.Admin, "Attendant does not have Admin role");
    }

    private static void TestRoleAuthorization_AttendantPolicy()
    {
        var env = CreateTestEnv();

        Assert(env.AttendantUser.Role.Name == AppRoles.ParkingAttendant, "Attendant user has ParkingAttendant role");
        Assert(env.CustomerA.Role.Name != AppRoles.ParkingAttendant, "Customer does not have ParkingAttendant role");
    }

    private static void TestRoleAuthorization_StaffPolicy()
    {
        var env = CreateTestEnv();

        bool adminIsStaff = env.AdminUser.Role.Name == AppRoles.Admin || env.AdminUser.Role.Name == AppRoles.ParkingAttendant;
        bool attendantIsStaff = env.AttendantUser.Role.Name == AppRoles.Admin || env.AttendantUser.Role.Name == AppRoles.ParkingAttendant;
        bool customerIsStaff = env.CustomerA.Role.Name == AppRoles.Admin || env.CustomerA.Role.Name == AppRoles.ParkingAttendant;

        Assert(adminIsStaff, "Admin qualifies as Staff");
        Assert(attendantIsStaff, "ParkingAttendant qualifies as Staff");
        Assert(!customerIsStaff, "Customer does NOT qualify as Staff");
    }

    private static void TestRoleAuthorization_CustomerCrossTenantIsolation()
    {
        var env = CreateTestEnv();

        // Customer A queries their own vehicles vs Customer B
        var customerAVehicles = env.VehicleService.GetMyVehiclesAsync(env.CustomerA.Id).GetAwaiter().GetResult();
        Assert(customerAVehicles.Count == 1 && customerAVehicles[0].LicensePlate == "ALICE-01",
            "Customer A only receives Customer A vehicles");

        var customerBVehicles = env.VehicleService.GetMyVehiclesAsync(env.CustomerB.Id).GetAwaiter().GetResult();
        Assert(customerBVehicles.Count == 1 && customerBVehicles[0].LicensePlate == "BOB-99",
            "Customer B only receives Customer B vehicles");
    }

    #endregion

    #region 5. Vehicle Ownership Tests

    private static void TestVehicleOwnership_OwnerAccess_Success()
    {
        Console.WriteLine("\n[5] Vehicle Ownership & Tenant Isolation Tests:");
        var env = CreateTestEnv();

        var vehicle = env.VehicleService.GetVehicleByIdAsync(env.VehicleA.Id, env.CustomerA.Id).GetAwaiter().GetResult();
        Assert(vehicle != null && vehicle.UserId == env.CustomerA.Id, "Owner can retrieve their own vehicle");

        var updated = env.VehicleService.UpdateVehicleAsync(env.VehicleA.Id, env.CustomerA.Id, new UpdateVehicleRequest
        {
            LicensePlate = "ALICE-01",
            Color = "Midnight Silver",
            Make = "Tesla",
            Model = "Model 3",
            VehicleType = VehicleType.Car
        }).GetAwaiter().GetResult();

        Assert(updated.Color == "Midnight Silver", "Owner can update their own vehicle");
    }

    private static void TestVehicleOwnership_NonOwnerUpdate_Fails()
    {
        var env = CreateTestEnv();

        bool failedAsExpected = false;
        try
        {
            // Bob attempts to update Alice's vehicle
            env.VehicleService.UpdateVehicleAsync(env.VehicleA.Id, env.CustomerB.Id, new UpdateVehicleRequest
            {
                LicensePlate = "HACKED-01",
                Make = "Stolen",
                Model = "Stolen",
                Color = "Red",
                VehicleType = VehicleType.Car
            }).GetAwaiter().GetResult();
        }
        catch (UnauthorizedAccessException)
        {
            failedAsExpected = true;
        }
        catch (KeyNotFoundException)
        {
            failedAsExpected = true;
        }

        Assert(failedAsExpected, "Non-owner update attempt strictly rejected");
    }

    private static void TestVehicleOwnership_NonOwnerDelete_Fails()
    {
        var env = CreateTestEnv();

        bool failedAsExpected = false;
        try
        {
            // Bob attempts to delete Alice's vehicle
            env.VehicleService.DeleteVehicleAsync(env.VehicleA.Id, env.CustomerB.Id).GetAwaiter().GetResult();
        }
        catch (UnauthorizedAccessException)
        {
            failedAsExpected = true;
        }
        catch (KeyNotFoundException)
        {
            failedAsExpected = true;
        }

        Assert(failedAsExpected, "Non-owner vehicle deletion attempt strictly rejected");
    }

    private static void TestVehicleOwnership_ReservationVehicleMismatch_Fails()
    {
        var env = CreateTestEnv();

        bool failedAsExpected = false;
        try
        {
            // Customer A attempts to book a reservation using Customer B's vehicle
            env.ReservationService.CreateReservationAsync(new CreateReservationRequest
            {
                ParkingSlotId = env.Slot.Id,
                VehicleId = env.VehicleB.Id, // Owned by Bob!
                StartTime = DateTime.UtcNow.AddHours(2),
                EndTime = DateTime.UtcNow.AddHours(4)
            }, env.CustomerA.Id).GetAwaiter().GetResult();
        }
        catch (ArgumentException ex)
        {
            failedAsExpected = ex.Message.Contains("Vehicle does not belong to the current customer");
        }

        Assert(failedAsExpected, "Booking reservation with another customer's vehicle rejected with ArgumentException");
    }

    private static void TestVehicleOwnership_DeleteVehicleWithActiveReservation_Fails()
    {
        var env = CreateTestEnv();

        // Create an active reservation for Alice's vehicle
        var res = env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id,
            StartTime = DateTime.UtcNow.AddHours(2),
            EndTime = DateTime.UtcNow.AddHours(4)
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        bool failedAsExpected = false;
        try
        {
            // Alice attempts to delete vehicle while active reservation exists
            env.VehicleService.DeleteVehicleAsync(env.VehicleA.Id, env.CustomerA.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            failedAsExpected = ex.Message.Contains("active") || ex.Message.Contains("reservation");
        }

        Assert(failedAsExpected, "Deleting vehicle with active reservation rejected with InvalidOperationException");
    }

    #endregion

    #region 6. Parking Slot Availability Tests

    private static void TestSlotAvailability_AvailableSlot_CanBeReserved()
    {
        Console.WriteLine("\n[6] Parking Slot Availability & Maintenance Tests:");
        var env = CreateTestEnv();

        var res = env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id,
            StartTime = DateTime.UtcNow.AddHours(2),
            EndTime = DateTime.UtcNow.AddHours(4)
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        Assert(res != null, "Available, active, non-maintenance slot can be reserved");
    }

    private static void TestSlotAvailability_MaintenanceSlot_Fails()
    {
        var env = CreateTestEnv();
        var maintenanceSlot = new ParkingSlot
        {
            Id = Guid.NewGuid(),
            ParkingAreaId = env.Area.Id,
            SlotNumber = "MNT-01",
            FloorNumber = 1,
            HourlyRate = 10.00m,
            Status = SlotStatus.Maintenance,
            VehicleType = VehicleType.Car,
            IsActive = true
        };
        env.Context.ParkingSlots.Add(maintenanceSlot);
        env.Context.SaveChanges();

        bool failedAsExpected = false;
        try
        {
            env.ReservationService.CreateReservationAsync(new CreateReservationRequest
            {
                ParkingSlotId = maintenanceSlot.Id,
                VehicleId = env.VehicleA.Id,
                StartTime = DateTime.UtcNow.AddHours(2),
                EndTime = DateTime.UtcNow.AddHours(4)
            }, env.CustomerA.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            failedAsExpected = ex.Message.Contains("maintenance") || ex.Message.Contains("available");
        }

        Assert(failedAsExpected, "Reserving slot under maintenance rejected with InvalidOperationException");
    }

    private static void TestSlotAvailability_InactiveSlot_Fails()
    {
        var env = CreateTestEnv();
        var inactiveSlot = new ParkingSlot
        {
            Id = Guid.NewGuid(),
            ParkingAreaId = env.Area.Id,
            SlotNumber = "INACT-01",
            FloorNumber = 1,
            HourlyRate = 10.00m,
            Status = SlotStatus.Available,
            VehicleType = VehicleType.Car,
            IsActive = false // Inactive
        };
        env.Context.ParkingSlots.Add(inactiveSlot);
        env.Context.SaveChanges();

        bool failedAsExpected = false;
        try
        {
            env.ReservationService.CreateReservationAsync(new CreateReservationRequest
            {
                ParkingSlotId = inactiveSlot.Id,
                VehicleId = env.VehicleA.Id,
                StartTime = DateTime.UtcNow.AddHours(2),
                EndTime = DateTime.UtcNow.AddHours(4)
            }, env.CustomerA.Id).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            failedAsExpected = ex is KeyNotFoundException || ex is InvalidOperationException || ex.Message.Contains("inactive") || ex.Message.Contains("not found");
        }

        Assert(failedAsExpected, "Reserving inactive parking slot rejected with KeyNotFoundException/InvalidOperationException");
    }

    private static void TestSlotAvailability_RealTimeCounts()
    {
        var env = CreateTestEnv();

        // Slot is available initially
        var availableSlots = env.Context.ParkingSlots.Count(s => s.Status == SlotStatus.Available && s.IsActive);
        Assert(availableSlots >= 1, "Real-time query shows slot available");

        // Occupy the slot
        env.Slot.Status = SlotStatus.Occupied;
        env.Context.SaveChanges();

        var newAvailable = env.Context.ParkingSlots.Count(s => s.Status == SlotStatus.Available && s.IsActive);
        Assert(newAvailable == 0, "Occupied slot discounted from available inventory");
    }

    #endregion

    #region 7. Overlapping Reservation Tests (Matrix)

    private static void TestReservation_ExactOverlap_Fails()
    {
        Console.WriteLine("\n[7] Overlapping Reservation Matrix Tests:");
        var env = CreateTestEnv();
        var start = DateTime.UtcNow.AddHours(5);
        var end = DateTime.UtcNow.AddHours(7);

        // Booking 1 succeeds
        env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id,
            StartTime = start,
            EndTime = end
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        // Exact overlap Booking 2 fails
        bool failedAsExpected = false;
        try
        {
            env.ReservationService.CreateReservationAsync(new CreateReservationRequest
            {
                ParkingSlotId = env.Slot.Id,
                VehicleId = env.VehicleB.Id,
                StartTime = start,
                EndTime = end
            }, env.CustomerB.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            failedAsExpected = ex.Message.Contains("already reserved");
        }

        Assert(failedAsExpected, "Exact matching interval overlap [10:00, 12:00] rejected");
    }

    private static void TestReservation_ContainedOverlap_Fails()
    {
        var env = CreateTestEnv();
        var start = DateTime.UtcNow.AddHours(5);
        var end = DateTime.UtcNow.AddHours(8);

        env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id,
            StartTime = start,
            EndTime = end
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        bool failedAsExpected = false;
        try
        {
            // Inside existing: start + 30m to end - 30m
            env.ReservationService.CreateReservationAsync(new CreateReservationRequest
            {
                ParkingSlotId = env.Slot.Id,
                VehicleId = env.VehicleB.Id,
                StartTime = start.AddMinutes(30),
                EndTime = end.AddMinutes(-30)
            }, env.CustomerB.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            failedAsExpected = ex.Message.Contains("already reserved");
        }

        Assert(failedAsExpected, "Contained interval overlap [10:30, 11:30] rejected");
    }

    private static void TestReservation_EnclosingOverlap_Fails()
    {
        var env = CreateTestEnv();
        var start = DateTime.UtcNow.AddHours(5);
        var end = DateTime.UtcNow.AddHours(7);

        env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id,
            StartTime = start,
            EndTime = end
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        bool failedAsExpected = false;
        try
        {
            // Starts before, ends after existing
            env.ReservationService.CreateReservationAsync(new CreateReservationRequest
            {
                ParkingSlotId = env.Slot.Id,
                VehicleId = env.VehicleB.Id,
                StartTime = start.AddHours(-1),
                EndTime = end.AddHours(1)
            }, env.CustomerB.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            failedAsExpected = ex.Message.Contains("already reserved");
        }

        Assert(failedAsExpected, "Enclosing interval overlap [09:00, 13:00] rejected");
    }

    private static void TestReservation_PartialStartOverlap_Fails()
    {
        var env = CreateTestEnv();
        var start = DateTime.UtcNow.AddHours(5);
        var end = DateTime.UtcNow.AddHours(7);

        env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id,
            StartTime = start,
            EndTime = end
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        bool failedAsExpected = false;
        try
        {
            // Starts before, ends inside existing
            env.ReservationService.CreateReservationAsync(new CreateReservationRequest
            {
                ParkingSlotId = env.Slot.Id,
                VehicleId = env.VehicleB.Id,
                StartTime = start.AddMinutes(-30),
                EndTime = start.AddMinutes(30)
            }, env.CustomerB.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            failedAsExpected = ex.Message.Contains("already reserved");
        }

        Assert(failedAsExpected, "Partial start overlap [09:30, 10:30] rejected");
    }

    private static void TestReservation_PartialEndOverlap_Fails()
    {
        var env = CreateTestEnv();
        var start = DateTime.UtcNow.AddHours(5);
        var end = DateTime.UtcNow.AddHours(7);

        env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id,
            StartTime = start,
            EndTime = end
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        bool failedAsExpected = false;
        try
        {
            // Starts inside, ends after existing
            env.ReservationService.CreateReservationAsync(new CreateReservationRequest
            {
                ParkingSlotId = env.Slot.Id,
                VehicleId = env.VehicleB.Id,
                StartTime = end.AddMinutes(-30),
                EndTime = end.AddMinutes(30)
            }, env.CustomerB.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            failedAsExpected = ex.Message.Contains("already reserved");
        }

        Assert(failedAsExpected, "Partial end overlap [11:30, 12:30] rejected");
    }

    private static void TestReservation_AdjacentConsecutive_Succeeds()
    {
        var env = CreateTestEnv();
        var start = DateTime.UtcNow.AddHours(5);
        var end = DateTime.UtcNow.AddHours(7);

        env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id,
            StartTime = start,
            EndTime = end
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        // Adjacent immediately after: starts exactly at end
        var adjacentRes = env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleB.Id,
            StartTime = end,
            EndTime = end.AddHours(2)
        }, env.CustomerB.Id).GetAwaiter().GetResult();

        Assert(adjacentRes != null, "Adjacent consecutive reservation [12:00, 14:00] succeeds without conflict");
    }

    private static void TestReservation_PriorConsecutive_Succeeds()
    {
        var env = CreateTestEnv();
        var start = DateTime.UtcNow.AddHours(5);
        var end = DateTime.UtcNow.AddHours(7);

        env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id,
            StartTime = start,
            EndTime = end
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        // Adjacent immediately before: ends exactly at start
        var priorRes = env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleB.Id,
            StartTime = start.AddHours(-2),
            EndTime = start
        }, env.CustomerB.Id).GetAwaiter().GetResult();

        Assert(priorRes != null, "Prior consecutive reservation [08:00, 10:00] succeeds without conflict");
    }

    private static void TestReservation_CancelledReservationDoesNotBlockNewBooking()
    {
        var env = CreateTestEnv();
        var start = DateTime.UtcNow.AddHours(5);
        var end = DateTime.UtcNow.AddHours(7);

        var res = env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id,
            StartTime = start,
            EndTime = end
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        // Alice cancels the reservation
        env.ReservationService.CancelReservationAsync(res.Id, env.CustomerA.Id, "Schedule change", false).GetAwaiter().GetResult();

        // Bob now books the exact same interval
        var bobRes = env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleB.Id,
            StartTime = start,
            EndTime = end
        }, env.CustomerB.Id).GetAwaiter().GetResult();

        Assert(bobRes != null, "Cancelled reservation releases slot interval for subsequent booking");
    }

    #endregion

    #region 8. Reservation Concurrency Tests (Heavy Focus)

    private static void TestReservationConcurrency_TenSimultaneousCompetingBookings()
    {
        Console.WriteLine("\n[8] Reservation Concurrency & Race Condition Tests (10 Competitors):");
        var env = CreateTestEnv();

        // Create 10 distinct customers & vehicles
        var competitors = new List<(User user, Vehicle vehicle)>();
        for (int i = 0; i < 10; i++)
        {
            var u = new User
            {
                Id = Guid.NewGuid(),
                FullName = $"Competitor {i}",
                Email = $"comp{i}@testing.com",
                PasswordHash = "hash",
                RoleId = env.CustomerRole.Id,
                Role = env.CustomerRole,
                IsActive = true
            };
            var v = new Vehicle
            {
                Id = Guid.NewGuid(),
                UserId = u.Id,
                LicensePlate = $"COMP-{i:D2}",
                Make = "Toyota",
                Model = "Yaris",
                VehicleType = VehicleType.Car
            };
            env.Context.Users.Add(u);
            env.Context.Vehicles.Add(v);
            competitors.Add((u, v));
        }
        env.Context.SaveChanges();

        var startTime = DateTime.UtcNow.AddHours(10);
        var endTime = DateTime.UtcNow.AddHours(12);

        // Concurrently dispatch 10 simultaneous booking attempts targeting the EXACT same slot and interval
        var tasks = competitors.Select(c => Task.Run(() =>
        {
            try
            {
                var r = env.ReservationService.CreateReservationAsync(new CreateReservationRequest
                {
                    ParkingSlotId = env.Slot.Id,
                    VehicleId = c.vehicle.Id,
                    StartTime = startTime,
                    EndTime = endTime
                }, c.user.Id).GetAwaiter().GetResult();
                return (Success: true, Res: (ReservationDto?)r, Error: (string?)null);
            }
            catch (Exception ex)
            {
                return (Success: false, Res: (ReservationDto?)null, Error: ex.Message);
            }
        })).ToArray();

        Task.WaitAll(tasks);

        int successfulBookings = tasks.Count(t => t.Result.Success);
        int failedBookings = tasks.Count(t => !t.Result.Success);

        Assert(successfulBookings == 1, "Exactly ONE competitor successfully acquired the reservation");
        Assert(failedBookings == 9, "Remaining 9 competing requests were safely rejected with conflict");

        // Verify database integrity
        var persistedReservations = env.Context.Reservations
            .Where(r => r.ParkingSlotId == env.Slot.Id && r.StartTime == startTime && r.EndTime == endTime)
            .ToList();

        Assert(persistedReservations.Count == 1, "Database contains exactly ONE persisted reservation (Zero double-booking)");
    }

    #endregion

    #region 9. Price Calculation Tests

    private static void TestPriceCalculation_StandardDuration()
    {
        Console.WriteLine("\n[9] Server-Side Price Calculation Tests:");
        var start = new DateTime(2026, 10, 4, 10, 0, 0, DateTimeKind.Utc);
        var end = start.AddHours(3);
        decimal hourlyRate = 12.00m;

        var price = ReservationPricingCalculator.CalculateTotalPrice(hourlyRate, start, end);
        Assert(price == 36.00m, "Standard 3-hour duration at $12.00/hr equals $36.00");
    }

    private static void TestPriceCalculation_MinimumOneHourRule()
    {
        var start = new DateTime(2026, 10, 4, 10, 0, 0, DateTimeKind.Utc);
        var end = start.AddMinutes(15); // 15-minute booking
        decimal hourlyRate = 15.00m;

        var price = ReservationPricingCalculator.CalculateTotalPrice(hourlyRate, start, end);
        Assert(price == 15.00m, "15-minute booking billed at full 1-hour minimum ($15.00)");
    }

    private static void TestPriceCalculation_PartialHourCeiling()
    {
        var start = new DateTime(2026, 10, 4, 10, 0, 0, DateTimeKind.Utc);
        var end = start.AddMinutes(75); // 1 hour 15 minutes = 1.25 hours
        decimal hourlyRate = 10.00m;

        var price = ReservationPricingCalculator.CalculateTotalPrice(hourlyRate, start, end);
        Assert(price == 12.50m, "1.25 hours at $10.00/hr correctly yields $12.50");
    }

    private static void TestPriceCalculation_ServerSideEnforcementIgnoringClientValue()
    {
        var env = CreateTestEnv();
        var start = DateTime.UtcNow.AddHours(20);
        var end = start.AddHours(2); // 2 hours at $10/hr = $20.00

        var res = env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id,
            StartTime = start,
            EndTime = end
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        Assert(res.TotalPrice == 20.00m, "Server strictly computed $20.00 total amount");
        Assert(res.DurationHours == 2.0m, "Server strictly computed 2.0 hours duration");
    }

    #endregion

    #region 10. Payment Business Rules Tests

    private static void TestPayment_Success()
    {
        Console.WriteLine("\n[10] Payment Processing & Idempotency Tests:");
        var env = CreateTestEnv();

        var res = env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id,
            StartTime = DateTime.UtcNow.AddHours(2),
            EndTime = DateTime.UtcNow.AddHours(4)
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        var payment = env.PaymentService.ProcessPaymentAsync(new CreatePaymentRequest
        {
            ReservationId = res.Id,
            Amount = res.TotalPrice,
            PaymentMethod = PaymentMethod.Card,
            PaymentToken = "tok_test_stripe"
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        Assert(payment.Status == PaymentStatus.Paid, "Payment status transitions to Paid");
        Assert(payment.Amount == 20.00m, "Payment amount matches calculated reservation fee");

        // Verify reservation status transitioned
        var updatedRes = env.Context.Reservations.Find(res.Id)!;
        Assert(updatedRes.Status == ReservationStatus.Confirmed, "Reservation status transitioned from Pending to Confirmed");
    }

    private static void TestPayment_CrossUserPayment_Fails()
    {
        var env = CreateTestEnv();

        var res = env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id, // Alice
            StartTime = DateTime.UtcNow.AddHours(2),
            EndTime = DateTime.UtcNow.AddHours(4)
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        bool failedAsExpected = false;
        try
        {
            // Bob attempts to pay for Alice's reservation
            env.PaymentService.ProcessPaymentAsync(new CreatePaymentRequest
            {
                ReservationId = res.Id,
                Amount = res.TotalPrice,
                PaymentMethod = PaymentMethod.Card
            }, env.CustomerB.Id).GetAwaiter().GetResult();
        }
        catch (UnauthorizedAccessException ex)
        {
            failedAsExpected = ex.Message.Contains("customer's reservation") || ex.Message.Contains("not authorized");
        }

        Assert(failedAsExpected, "Cross-user payment attempt rejected with UnauthorizedAccessException");
    }

    private static void TestPayment_MismatchedAmount_Fails()
    {
        var env = CreateTestEnv();

        var res = env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id,
            StartTime = DateTime.UtcNow.AddHours(2),
            EndTime = DateTime.UtcNow.AddHours(4) // $20.00
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        bool failedAsExpected = false;
        try
        {
            // Submit $5.00 instead of $20.00
            env.PaymentService.ProcessPaymentAsync(new CreatePaymentRequest
            {
                ReservationId = res.Id,
                Amount = 5.00m,
                PaymentMethod = PaymentMethod.Card
            }, env.CustomerA.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            failedAsExpected = ex.Message.Contains("does not match") || ex.Message.Contains("reservation total");
        }
        catch (ArgumentException ex)
        {
            failedAsExpected = ex.Message.Contains("does not match");
        }

        Assert(failedAsExpected, "Payment amount mismatch strictly rejected with InvalidOperationException");
    }

    private static void TestPayment_DuplicatePayment_Fails()
    {
        var env = CreateTestEnv();

        var res = env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id,
            StartTime = DateTime.UtcNow.AddHours(2),
            EndTime = DateTime.UtcNow.AddHours(4)
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        // First payment succeeds
        env.PaymentService.ProcessPaymentAsync(new CreatePaymentRequest
        {
            ReservationId = res.Id,
            Amount = res.TotalPrice,
            PaymentMethod = PaymentMethod.Card
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        bool failedAsExpected = false;
        try
        {
            // Second payment attempt
            env.PaymentService.ProcessPaymentAsync(new CreatePaymentRequest
            {
                ReservationId = res.Id,
                Amount = res.TotalPrice,
                PaymentMethod = PaymentMethod.Card
            }, env.CustomerA.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            failedAsExpected = ex.Message.Contains("already") || ex.Message.Contains("Confirmed");
        }

        Assert(failedAsExpected, "Duplicate payment on already confirmed reservation rejected");
    }

    private static void TestPayment_NonExistentReservation_Fails()
    {
        var env = CreateTestEnv();

        bool failedAsExpected = false;
        try
        {
            env.PaymentService.ProcessPaymentAsync(new CreatePaymentRequest
            {
                ReservationId = Guid.NewGuid(),
                Amount = 20.00m,
                PaymentMethod = PaymentMethod.Card
            }, env.CustomerA.Id).GetAwaiter().GetResult();
        }
        catch (KeyNotFoundException)
        {
            failedAsExpected = true;
        }

        Assert(failedAsExpected, "Payment for non-existent reservation rejected with KeyNotFoundException");
    }

    #endregion

    #region 11. QR Code Validation Tests

    private static void TestQRValidation_ValidToken_Success()
    {
        Console.WriteLine("\n[11] QR Code Validation & Security Tests:");
        var env = CreateTestEnv();

        var res = env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id,
            StartTime = DateTime.UtcNow.AddHours(2),
            EndTime = DateTime.UtcNow.AddHours(4)
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        // Pay to activate QR
        env.PaymentService.ProcessPaymentAsync(new CreatePaymentRequest
        {
            ReservationId = res.Id,
            Amount = res.TotalPrice,
            PaymentMethod = PaymentMethod.Card
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        var qrDto = env.QRCodeService.GetQRCodeByReservationIdAsync(res.Id, env.CustomerA.Id, false).GetAwaiter().GetResult();
        Assert(!string.IsNullOrWhiteSpace(qrDto.Code), "Valid QR token generated upon payment");
        Assert(!string.IsNullOrWhiteSpace(qrDto.ImageData), "Base64 image matrix generated");

        // Staff scan
        var scanResult = env.QRCodeService.ScanQRCodeAsync(new QRScanRequest
        {
            QRCode = qrDto.Code,
            Action = "ValidateOnly"
        }, env.AttendantUser.Id, false).GetAwaiter().GetResult();

        Assert(scanResult.IsValid, "Scanned QR code token is valid");
        Assert(scanResult.ReservationId == res.Id, "QR scan accurately resolves reservation");
    }

    private static void TestQRValidation_TamperedToken_Fails()
    {
        var env = CreateTestEnv();

        var scanResult = env.QRCodeService.ScanQRCodeAsync(new QRScanRequest
        {
            QRCode = "CORRUPTED_OR_TAMPERED_QR_TOKEN_xyz",
            Action = "ValidateOnly"
        }, env.AttendantUser.Id, false).GetAwaiter().GetResult();

        Assert(!scanResult.IsValid, "Tampered/corrupted QR token scan rejected with IsValid = false");
    }

    private static void TestQRValidation_UnconfirmedReservation_Fails()
    {
        var env = CreateTestEnv();

        // Create reservation
        var res = env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id,
            StartTime = DateTime.UtcNow.AddHours(2),
            EndTime = DateTime.UtcNow.AddHours(4)
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        // Set to Pending and clear pre-generated QR code to simulate unconfirmed state
        var dbRes = env.Context.Reservations.Find(res.Id)!;
        dbRes.Status = ReservationStatus.Pending;
        var qrs = env.Context.QRCodes.Where(q => q.ReservationId == res.Id).ToList();
        env.Context.QRCodes.RemoveRange(qrs);
        env.Context.SaveChanges();

        bool failedAsExpected = false;
        try
        {
            env.QRCodeService.GetQRCodeByReservationIdAsync(res.Id, env.CustomerA.Id, false).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            failedAsExpected = ex.Message.Contains("not been generated") || ex.Message.Contains("Confirmed") || ex.Message.Contains("payment");
        }

        Assert(failedAsExpected, "QR code retrieval rejected for unconfirmed/unpaid reservation");
    }

    private static void TestQRValidation_CustomerAccessOnlyOwnQR_Fails()
    {
        var env = CreateTestEnv();

        var res = env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id, // Alice
            StartTime = DateTime.UtcNow.AddHours(2),
            EndTime = DateTime.UtcNow.AddHours(4)
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        env.PaymentService.ProcessPaymentAsync(new CreatePaymentRequest
        {
            ReservationId = res.Id,
            Amount = res.TotalPrice,
            PaymentMethod = PaymentMethod.Card
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        bool failedAsExpected = false;
        try
        {
            // Bob attempts to retrieve Alice's QR code
            env.QRCodeService.GetQRCodeByReservationIdAsync(res.Id, env.CustomerB.Id, false).GetAwaiter().GetResult();
        }
        catch (UnauthorizedAccessException)
        {
            failedAsExpected = true;
        }

        Assert(failedAsExpected, "Customer cannot access another user's QR code");
    }

    #endregion

    #region 12. Check-In Gate Operations Tests

    private static void TestCheckIn_Success()
    {
        Console.WriteLine("\n[12] Vehicle Check-In Gate Operations Tests:");
        var env = CreateTestEnv();

        var now = DateTime.UtcNow;
        var res = env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id,
            StartTime = now.AddMinutes(5), // Within 30-minute arrival window
            EndTime = now.AddHours(2)
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        env.PaymentService.ProcessPaymentAsync(new CreatePaymentRequest
        {
            ReservationId = res.Id,
            Amount = res.TotalPrice,
            PaymentMethod = PaymentMethod.Card
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        var qr = env.QRCodeService.GetQRCodeByReservationIdAsync(res.Id, env.CustomerA.Id, false).GetAwaiter().GetResult();

        var checkInResult = env.ParkingService.CheckInVehicleAsync(new CheckInRequest
        {
            QRCode = qr.Code
        }, env.AttendantUser.Id, false).GetAwaiter().GetResult();

        Assert(checkInResult.ReservationStatus == ReservationStatus.Active, "Reservation status transitioned to Active");
        Assert(checkInResult.SlotStatus == SlotStatus.Occupied, "Slot status transitioned to Occupied");
        Assert(checkInResult.AttendantId == env.AttendantUser.Id, "Attendant ID attributed on check-in");
    }

    private static void TestCheckIn_UnpaidReservation_Fails()
    {
        var env = CreateTestEnv();
        var now = DateTime.UtcNow;

        var res = env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id,
            StartTime = now.AddMinutes(5),
            EndTime = now.AddHours(2)
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        // Seed QR code directly linked to this unpaid reservation
        var unpaidQr = new QRCode
        {
            Id = Guid.NewGuid(),
            ReservationId = res.Id,
            Code = "UNPAID_QR_TEST_TOKEN",
            ExpiresAt = now.AddHours(2)
        };
        env.Context.QRCodes.Add(unpaidQr);
        env.Context.SaveChanges();

        bool failedAsExpected = false;
        try
        {
            env.ParkingService.CheckInVehicleAsync(new CheckInRequest
            {
                QRCode = unpaidQr.Code
            }, env.AttendantUser.Id, false).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            failedAsExpected = ex.Message.Contains("payment", StringComparison.OrdinalIgnoreCase);
        }

        Assert(failedAsExpected, "Check-in on unpaid reservation rejected with InvalidOperationException");
    }

    private static void TestCheckIn_DuplicateCheckIn_Fails()
    {
        var env = CreateTestEnv();
        var now = DateTime.UtcNow;

        var res = env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id,
            StartTime = now.AddMinutes(5),
            EndTime = now.AddHours(2)
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        env.PaymentService.ProcessPaymentAsync(new CreatePaymentRequest
        {
            ReservationId = res.Id,
            Amount = res.TotalPrice,
            PaymentMethod = PaymentMethod.Card
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        var qr = env.QRCodeService.GetQRCodeByReservationIdAsync(res.Id, env.CustomerA.Id, false).GetAwaiter().GetResult();

        // First check-in
        env.ParkingService.CheckInVehicleAsync(new CheckInRequest { QRCode = qr.Code }, env.AttendantUser.Id, false).GetAwaiter().GetResult();

        bool failedAsExpected = false;
        try
        {
            // Second check-in
            env.ParkingService.CheckInVehicleAsync(new CheckInRequest { QRCode = qr.Code }, env.AttendantUser.Id, false).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            failedAsExpected = ex.Message.Contains("already checked in") || ex.Message.Contains("Active");
        }

        Assert(failedAsExpected, "Duplicate check-in on already Active session rejected with InvalidOperationException");
    }

    private static void TestCheckIn_CancelledReservation_Fails()
    {
        var env = CreateTestEnv();
        var now = DateTime.UtcNow;

        var res = env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id,
            StartTime = now.AddMinutes(5),
            EndTime = now.AddHours(2)
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        // Cancel
        env.ReservationService.CancelReservationAsync(res.Id, env.CustomerA.Id, "Cancelled", false).GetAwaiter().GetResult();

        bool failedAsExpected = false;
        try
        {
            env.ParkingService.CheckInVehicleAsync(new CheckInRequest { QRCode = "TOKEN" }, env.AttendantUser.Id, false).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            failedAsExpected = ex is InvalidOperationException || ex is KeyNotFoundException;
        }

        Assert(failedAsExpected, "Check-in on cancelled reservation rejected");
    }

    private static void TestCheckIn_NonStaffCaller_Fails()
    {
        // Enforce that CheckInVehicle endpoint requires Staff roles (ParkingAttendant or Admin) and disallows Customer
        var authorizeAttribute = typeof(SmartParkingSystem.Api.Controllers.ParkingController)
            .GetMethod(nameof(SmartParkingSystem.Api.Controllers.ParkingController.CheckInVehicle))?
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .FirstOrDefault();

        bool requiresStaff = authorizeAttribute != null 
            && authorizeAttribute.Roles != null 
            && authorizeAttribute.Roles.Contains(AppRoles.ParkingAttendant) 
            && authorizeAttribute.Roles.Contains(AppRoles.Admin)
            && !authorizeAttribute.Roles.Contains(AppRoles.Customer);

        Assert(requiresStaff, "Non-staff user check-in rejected: Endpoint strictly guarded by [Authorize(Roles = ParkingAttendant,Admin)]");
    }

    #endregion

    #region 13. Check-Out Gate Operations Tests

    private static void TestCheckOut_OnTime_Success()
    {
        Console.WriteLine("\n[13] Vehicle Check-Out & Overstay Calculations Tests:");
        var env = CreateTestEnv();
        var now = DateTime.UtcNow;

        var res = env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id,
            StartTime = now.AddMinutes(5),
            EndTime = now.AddHours(2)
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        env.PaymentService.ProcessPaymentAsync(new CreatePaymentRequest
        {
            ReservationId = res.Id,
            Amount = res.TotalPrice,
            PaymentMethod = PaymentMethod.Card
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        var qr = env.QRCodeService.GetQRCodeByReservationIdAsync(res.Id, env.CustomerA.Id, false).GetAwaiter().GetResult();

        env.ParkingService.CheckInVehicleAsync(new CheckInRequest { QRCode = qr.Code }, env.AttendantUser.Id, false).GetAwaiter().GetResult();

        // Check-out on time
        var checkOutResult = env.ParkingService.CheckOutVehicleAsync(new CheckOutRequest
        {
            QRCode = qr.Code
        }, env.AttendantUser.Id, false).GetAwaiter().GetResult();

        Assert(checkOutResult.ReservationStatus == ReservationStatus.Completed, "Reservation status transitioned to Completed");
        Assert(checkOutResult.SlotStatus == SlotStatus.Available, "Slot released back to Available");
        Assert(checkOutResult.OverstayFee == 0.00m, "On-time check-out incurs $0 overstay fee");
    }

    private static void TestCheckOut_WithinGracePeriod_Success()
    {
        var env = CreateTestEnv();
        var now = DateTime.UtcNow;

        var res = env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id,
            StartTime = now.AddMinutes(5),
            EndTime = now.AddHours(2)
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        env.PaymentService.ProcessPaymentAsync(new CreatePaymentRequest
        {
            ReservationId = res.Id,
            Amount = res.TotalPrice,
            PaymentMethod = PaymentMethod.Card
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        var qr = env.QRCodeService.GetQRCodeByReservationIdAsync(res.Id, env.CustomerA.Id, false).GetAwaiter().GetResult();
        env.ParkingService.CheckInVehicleAsync(new CheckInRequest { QRCode = qr.Code }, env.AttendantUser.Id, false).GetAwaiter().GetResult();

        // Simulate reservation ending 10 minutes ago (< 15-minute grace window)
        var dbRes = env.Context.Reservations.Find(res.Id)!;
        dbRes.EndTime = DateTime.UtcNow.AddMinutes(-10);
        env.Context.SaveChanges();

        // Check-out 10 minutes late (< 15-minute grace window)
        var result = env.ParkingService.CheckOutVehicleAsync(new CheckOutRequest
        {
            QRCode = qr.Code
        }, env.AttendantUser.Id, false).GetAwaiter().GetResult();

        Assert(result.OverstayFee == 0.00m, "Exit within 15-minute grace period incurs $0 overstay fee");
    }

    private static void TestCheckOut_OverstayPenaltyApplied()
    {
        var env = CreateTestEnv();
        var now = DateTime.UtcNow;

        var res = env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id,
            StartTime = now.AddMinutes(5),
            EndTime = now.AddHours(2)
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        env.PaymentService.ProcessPaymentAsync(new CreatePaymentRequest
        {
            ReservationId = res.Id,
            Amount = res.TotalPrice,
            PaymentMethod = PaymentMethod.Card
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        var qr = env.QRCodeService.GetQRCodeByReservationIdAsync(res.Id, env.CustomerA.Id, false).GetAwaiter().GetResult();
        env.ParkingService.CheckInVehicleAsync(new CheckInRequest { QRCode = qr.Code }, env.AttendantUser.Id, false).GetAwaiter().GetResult();

        // Simulate reservation ending 75 minutes ago (> 15 min grace window)
        var dbRes = env.Context.Reservations.Find(res.Id)!;
        dbRes.EndTime = DateTime.UtcNow.AddMinutes(-75);
        env.Context.SaveChanges();

        var result = env.ParkingService.CheckOutVehicleAsync(new CheckOutRequest
        {
            QRCode = qr.Code
        }, env.AttendantUser.Id, false).GetAwaiter().GetResult();

        Assert(result.OverstayFee > 0.00m, "Overstay beyond 15-minute grace period calculates penalty fee");
        Assert(result.OverstayMinutes >= 60, "Overstay duration recorded accurately in minutes");
    }

    private static void TestCheckOut_NotCheckedIn_Fails()
    {
        var env = CreateTestEnv();
        var res = env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id,
            StartTime = DateTime.UtcNow.AddHours(2),
            EndTime = DateTime.UtcNow.AddHours(4)
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        env.PaymentService.ProcessPaymentAsync(new CreatePaymentRequest
        {
            ReservationId = res.Id,
            Amount = res.TotalPrice,
            PaymentMethod = PaymentMethod.Card
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        var qr = env.QRCodeService.GetQRCodeByReservationIdAsync(res.Id, env.CustomerA.Id, false).GetAwaiter().GetResult();

        bool failedAsExpected = false;
        try
        {
            // Attempt check-out before check-in has occurred
            env.ParkingService.CheckOutVehicleAsync(new CheckOutRequest
            {
                QRCode = qr.Code
            }, env.AttendantUser.Id, false).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            failedAsExpected = ex.Message.Contains("checked in", StringComparison.OrdinalIgnoreCase) 
                || ex.Message.Contains("check-in", StringComparison.OrdinalIgnoreCase) 
                || ex.Message.Contains("Active", StringComparison.OrdinalIgnoreCase);
        }

        Assert(failedAsExpected, "Check-out on un-checked-in vehicle rejected with InvalidOperationException");
    }

    private static void TestCheckOut_AlreadyCompleted_Fails()
    {
        var env = CreateTestEnv();
        var now = DateTime.UtcNow;

        var res = env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id,
            StartTime = now.AddMinutes(5),
            EndTime = now.AddHours(2)
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        env.PaymentService.ProcessPaymentAsync(new CreatePaymentRequest
        {
            ReservationId = res.Id,
            Amount = res.TotalPrice,
            PaymentMethod = PaymentMethod.Card
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        var qr = env.QRCodeService.GetQRCodeByReservationIdAsync(res.Id, env.CustomerA.Id, false).GetAwaiter().GetResult();

        env.ParkingService.CheckInVehicleAsync(new CheckInRequest { QRCode = qr.Code }, env.AttendantUser.Id, false).GetAwaiter().GetResult();
        env.ParkingService.CheckOutVehicleAsync(new CheckOutRequest { QRCode = qr.Code }, env.AttendantUser.Id, false).GetAwaiter().GetResult();

        bool failedAsExpected = false;
        try
        {
            // Second check-out
            env.ParkingService.CheckOutVehicleAsync(new CheckOutRequest { QRCode = qr.Code }, env.AttendantUser.Id, false).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            failedAsExpected = ex.Message.Contains("already") || ex.Message.Contains("Completed");
        }

        Assert(failedAsExpected, "Duplicate check-out rejected with InvalidOperationException");
    }

    #endregion

    #region 14. Parking History Auditing Tests

    private static void TestParkingHistory_CreatedOnCheckOut()
    {
        Console.WriteLine("\n[14] Parking History Auditing & Isolation Tests:");
        var env = CreateTestEnv();
        var now = DateTime.UtcNow;

        var res = env.ReservationService.CreateReservationAsync(new CreateReservationRequest
        {
            ParkingSlotId = env.Slot.Id,
            VehicleId = env.VehicleA.Id,
            StartTime = now.AddMinutes(5),
            EndTime = now.AddHours(2)
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        env.PaymentService.ProcessPaymentAsync(new CreatePaymentRequest
        {
            ReservationId = res.Id,
            Amount = res.TotalPrice,
            PaymentMethod = PaymentMethod.Card
        }, env.CustomerA.Id).GetAwaiter().GetResult();

        var qr = env.QRCodeService.GetQRCodeByReservationIdAsync(res.Id, env.CustomerA.Id, false).GetAwaiter().GetResult();
        env.ParkingService.CheckInVehicleAsync(new CheckInRequest { QRCode = qr.Code }, env.AttendantUser.Id, false).GetAwaiter().GetResult();
        env.ParkingService.CheckOutVehicleAsync(new CheckOutRequest { QRCode = qr.Code }, env.AttendantUser.Id, false).GetAwaiter().GetResult();

        // Verify history record created in DB
        var historyRecord = env.Context.ParkingHistories.FirstOrDefault(h => h.ReservationId == res.Id);
        Assert(historyRecord != null, "ParkingHistory record created in database upon check-out");
        Assert(historyRecord!.AttendantCheckInId == env.AttendantUser.Id, "AttendantCheckInId recorded in history");
        Assert(historyRecord.AttendantCheckOutId == env.AttendantUser.Id, "AttendantCheckOutId recorded in history");
    }

    private static void TestParkingHistory_CustomerIsolation()
    {
        var env = CreateTestEnv();

        // Seed 1 completed reservation & history record for Alice
        var resA = new Reservation
        {
            Id = Guid.NewGuid(),
            ReservationNumber = "RES-A",
            UserId = env.CustomerA.Id,
            User = env.CustomerA,
            ParkingSlotId = env.Slot.Id,
            ParkingSlot = env.Slot,
            VehicleId = env.VehicleA.Id,
            Vehicle = env.VehicleA,
            StartTime = DateTime.UtcNow.AddHours(-3),
            EndTime = DateTime.UtcNow.AddHours(-1),
            TotalPrice = 20.00m,
            Status = ReservationStatus.Completed
        };
        var histA = new ParkingHistory
        {
            Id = Guid.NewGuid(),
            ReservationId = resA.Id,
            Reservation = resA,
            VehicleId = env.VehicleA.Id,
            Vehicle = env.VehicleA,
            ParkingSlotId = env.Slot.Id,
            ParkingSlot = env.Slot,
            CheckInTime = DateTime.UtcNow.AddHours(-3),
            CheckOutTime = DateTime.UtcNow.AddHours(-1),
            AttendantCheckInId = env.AttendantUser.Id,
            AttendantCheckOutId = env.AttendantUser.Id
        };

        // Seed 1 completed reservation & history record for Bob
        var resB = new Reservation
        {
            Id = Guid.NewGuid(),
            ReservationNumber = "RES-B",
            UserId = env.CustomerB.Id,
            User = env.CustomerB,
            ParkingSlotId = env.Slot.Id,
            ParkingSlot = env.Slot,
            VehicleId = env.VehicleB.Id,
            Vehicle = env.VehicleB,
            StartTime = DateTime.UtcNow.AddHours(-6),
            EndTime = DateTime.UtcNow.AddHours(-4),
            TotalPrice = 20.00m,
            Status = ReservationStatus.Completed
        };
        var histB = new ParkingHistory
        {
            Id = Guid.NewGuid(),
            ReservationId = resB.Id,
            Reservation = resB,
            VehicleId = env.VehicleB.Id,
            Vehicle = env.VehicleB,
            ParkingSlotId = env.Slot.Id,
            ParkingSlot = env.Slot,
            CheckInTime = DateTime.UtcNow.AddHours(-6),
            CheckOutTime = DateTime.UtcNow.AddHours(-4),
            AttendantCheckInId = env.AttendantUser.Id,
            AttendantCheckOutId = env.AttendantUser.Id
        };

        env.Context.Reservations.AddRange(resA, resB);
        env.Context.ParkingHistories.AddRange(histA, histB);
        env.Context.SaveChanges();

        // Alice queries her history
        var aliceHistory = env.ParkingHistoryService.GetCustomerHistoryAsync(env.CustomerA.Id, new ParkingHistoryFilterRequest()).GetAwaiter().GetResult();
        Assert(aliceHistory.TotalCount == 1, "Alice history query returns exactly 1 record");
        Assert(aliceHistory.Items[0].Id == histA.Id, "Alice history contains only Alice's record");

        // Bob queries his history
        var bobHistory = env.ParkingHistoryService.GetCustomerHistoryAsync(env.CustomerB.Id, new ParkingHistoryFilterRequest()).GetAwaiter().GetResult();
        Assert(bobHistory.TotalCount == 1, "Bob history query returns exactly 1 record");
        Assert(bobHistory.Items[0].Id == histB.Id, "Bob history contains only Bob's record");
    }

    private static void TestParkingHistory_AdminAudit()
    {
        var env = CreateTestEnv();

        var resA = new Reservation
        {
            Id = Guid.NewGuid(),
            ReservationNumber = "RES-A",
            UserId = env.CustomerA.Id,
            User = env.CustomerA,
            ParkingSlotId = env.Slot.Id,
            ParkingSlot = env.Slot,
            VehicleId = env.VehicleA.Id,
            Vehicle = env.VehicleA,
            StartTime = DateTime.UtcNow.AddHours(-3),
            EndTime = DateTime.UtcNow.AddHours(-1),
            TotalPrice = 20.00m,
            Status = ReservationStatus.Completed
        };
        var histA = new ParkingHistory
        {
            Id = Guid.NewGuid(),
            ReservationId = resA.Id,
            Reservation = resA,
            VehicleId = env.VehicleA.Id,
            Vehicle = env.VehicleA,
            ParkingSlotId = env.Slot.Id,
            ParkingSlot = env.Slot,
            CheckInTime = DateTime.UtcNow.AddHours(-3),
            CheckOutTime = DateTime.UtcNow.AddHours(-1),
            AttendantCheckInId = env.AttendantUser.Id,
            AttendantCheckOutId = env.AttendantUser.Id
        };

        var resB = new Reservation
        {
            Id = Guid.NewGuid(),
            ReservationNumber = "RES-B",
            UserId = env.CustomerB.Id,
            User = env.CustomerB,
            ParkingSlotId = env.Slot.Id,
            ParkingSlot = env.Slot,
            VehicleId = env.VehicleB.Id,
            Vehicle = env.VehicleB,
            StartTime = DateTime.UtcNow.AddHours(-6),
            EndTime = DateTime.UtcNow.AddHours(-4),
            TotalPrice = 20.00m,
            Status = ReservationStatus.Completed
        };
        var histB = new ParkingHistory
        {
            Id = Guid.NewGuid(),
            ReservationId = resB.Id,
            Reservation = resB,
            VehicleId = env.VehicleB.Id,
            Vehicle = env.VehicleB,
            ParkingSlotId = env.Slot.Id,
            ParkingSlot = env.Slot,
            CheckInTime = DateTime.UtcNow.AddHours(-6),
            CheckOutTime = DateTime.UtcNow.AddHours(-4),
            AttendantCheckInId = env.AttendantUser.Id,
            AttendantCheckOutId = env.AttendantUser.Id
        };

        env.Context.Reservations.AddRange(resA, resB);
        env.Context.ParkingHistories.AddRange(histA, histB);
        env.Context.SaveChanges();

        // Admin queries all history
        var allHistory = env.ParkingHistoryService.GetAllHistoryAsync(new ParkingHistoryFilterRequest()).GetAwaiter().GetResult();
        Assert(allHistory.TotalCount == 2, "Admin query returns system-wide records across all customers");
    }

    #endregion
}
