using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Infrastructure.Data;

public static class SeedVerificationRunner
{
    public static async Task RunAsync(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine("           SMART PARKING SYSTEM - DATABASE SEED VERIFICATION REPORT             ");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        // 1. Verify Roles
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[1] Verifying Seeded Roles...");
        Console.ResetColor();

        var roles = await context.Roles.OrderBy(r => r.Name).ToListAsync();
        Console.WriteLine($"Total Roles found in DB: {roles.Count}");

        var expectedRoles = new[] { "Admin", "ParkingAttendant", "Customer" };
        foreach (var expected in expectedRoles)
        {
            var matched = roles.FirstOrDefault(r => r.Name == expected);
            if (matched != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  [OK] Role '{matched.Name}': ID={matched.Id}, Description=\"{matched.Description}\"");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  [FAIL] Missing expected role: '{expected}'");
                Console.ResetColor();
            }
        }

        // 2. Verify Default Administrator Account
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[2] Verifying Administrator Account & Password Hashing...");
        Console.ResetColor();

        var adminEmail = configuration["InitialAdmin:Email"] ?? "admin@smartparking.com";
        var adminPassword = configuration["InitialAdmin:Password"] ?? "Admin@Password123!";

        var adminUser = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == adminEmail);

        if (adminUser == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [FAIL] Administrator account '{adminEmail}' not found in database.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  [OK] Admin User Found: ID={adminUser.Id}");
            Console.WriteLine($"       Full Name : {adminUser.FullName}");
            Console.WriteLine($"       Email     : {adminUser.Email}");
            Console.WriteLine($"       Role      : {adminUser.Role.Name}");
            Console.WriteLine($"       Phone     : {adminUser.PhoneNumber}");
            Console.WriteLine($"       IsActive  : {adminUser.IsActive}");
            Console.ResetColor();

            // Verify Password Security
            Console.WriteLine("\n  Checking Password Security:");
            bool isPlainText = adminUser.PasswordHash.Equals(adminPassword, StringComparison.OrdinalIgnoreCase);
            if (isPlainText)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  [SECURITY ALERT] Password is stored in PLAIN TEXT!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  [OK] Password is NOT plain text.");
                Console.ResetColor();
            }

            // Verify Hash Format (PBKDF2 segments: Hash:Salt:Iterations:Algorithm)
            var segments = adminUser.PasswordHash.Split(':');
            if (segments.Length == 4)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  [OK] Password Hash format is valid PBKDF2 (4 segments: Hash:Salt:Iterations:Algorithm)");
                Console.WriteLine($"       - Hash (Hex, {segments[0].Length} chars)   : {segments[0]}");
                Console.WriteLine($"       - Salt (Hex, {segments[1].Length} chars)   : {segments[1]}");
                Console.WriteLine($"       - Iterations ({segments[2]})        : {segments[2]}");
                Console.WriteLine($"       - Algorithm                  : {segments[3]}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  [FAIL] Invalid hash format: '{adminUser.PasswordHash}'");
                Console.ResetColor();
            }

            // Verify Password Verification Logic
            bool verifyCorrect = passwordHasher.VerifyPassword(adminPassword, adminUser.PasswordHash);
            bool verifyIncorrect = passwordHasher.VerifyPassword("WrongPassword123!", adminUser.PasswordHash);

            if (verifyCorrect && !verifyIncorrect)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  [OK] Password verification passed: Correct password accepted, wrong password rejected.");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  [FAIL] Password verification failed! CorrectAccepted={verifyCorrect}, WrongRejected={!verifyIncorrect}");
                Console.ResetColor();
            }
        }

        // 3. Verify Other Seeded Accounts (Attendant & Customer)
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[3] Verifying Parking Attendant and Customer Accounts...");
        Console.ResetColor();

        var attendant = await context.Users
            .Include(u => u.Role)
            .Include(u => u.AssignedParkingArea)
            .FirstOrDefaultAsync(u => u.Email == "attendant@smartparking.com");

        if (attendant != null)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  [OK] Attendant Account: '{attendant.FullName}' ({attendant.Email})");
            Console.WriteLine($"       Role: {attendant.Role.Name}, Assigned Area: {attendant.AssignedParkingArea?.Name ?? "None"}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  [FAIL] Attendant account not found.");
            Console.ResetColor();
        }

        var customer = await context.Users
            .Include(u => u.Role)
            .Include(u => u.Vehicles)
            .FirstOrDefaultAsync(u => u.Email == "customer@smartparking.com");

        if (customer != null)
        {
            var vehicle = customer.Vehicles.FirstOrDefault();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  [OK] Customer Account: '{customer.FullName}' ({customer.Email})");
            Console.WriteLine($"       Role: {customer.Role.Name}, Vehicle: {vehicle?.LicensePlate} ({vehicle?.Make} {vehicle?.Model})");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  [FAIL] Customer account not found.");
            Console.ResetColor();
        }

        // 4. Verify Parking Area & Slots
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[4] Verifying Seeded Parking Areas and Slots...");
        Console.ResetColor();

        var area = await context.ParkingAreas
            .Include(a => a.ParkingSlots)
            .FirstOrDefaultAsync(a => a.Name == "Central Plaza Parking");

        if (area != null)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  [OK] Parking Area: '{area.Name}', TotalSlots={area.TotalSlots}, Slots in DB={area.ParkingSlots.Count}");
            var standardCount = area.ParkingSlots.Count(s => s.SlotType == SlotType.Standard);
            var accessibleCount = area.ParkingSlots.Count(s => s.SlotType == SlotType.Accessible);
            var evCount = area.ParkingSlots.Count(s => s.SlotType == SlotType.EVCharging);
            var compactCount = area.ParkingSlots.Count(s => s.SlotType == SlotType.Compact);
            var vipCount = area.ParkingSlots.Count(s => s.SlotType == SlotType.VIP);
            Console.WriteLine($"       Slot Breakdown: Standard={standardCount}, Accessible={accessibleCount}, EV={evCount}, Compact={compactCount}, VIP={vipCount}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  [FAIL] Central Plaza Parking area not found.");
            Console.ResetColor();
        }

        // 5. Test Seed Idempotency (Execute SeedAsync second time)
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[5] Testing Seed Idempotency (Running SeedAsync again to check for duplicate prevention)...");
        Console.ResetColor();

        int rolesBefore = await context.Roles.CountAsync();
        int usersBefore = await context.Users.CountAsync();
        int areasBefore = await context.ParkingAreas.CountAsync();
        int slotsBefore = await context.ParkingSlots.CountAsync();
        int vehiclesBefore = await context.Vehicles.CountAsync();

        // Run seed a 2nd time
        await DatabaseSeeder.SeedAsync(context, passwordHasher, configuration);

        int rolesAfter = await context.Roles.CountAsync();
        int usersAfter = await context.Users.CountAsync();
        int areasAfter = await context.ParkingAreas.CountAsync();
        int slotsAfter = await context.ParkingSlots.CountAsync();
        int vehiclesAfter = await context.Vehicles.CountAsync();

        bool isIdempotent = (rolesBefore == rolesAfter) &&
                            (usersBefore == usersAfter) &&
                            (areasBefore == areasAfter) &&
                            (slotsBefore == slotsAfter) &&
                            (vehiclesBefore == vehiclesAfter);

        if (isIdempotent)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  [OK] IDEMPOTENCY CONFIRMED: Re-running SeedAsync produced ZERO duplicate records!");
            Console.WriteLine($"       Roles: {rolesBefore} -> {rolesAfter}");
            Console.WriteLine($"       Users: {usersBefore} -> {usersAfter}");
            Console.WriteLine($"       Areas: {areasBefore} -> {areasAfter}");
            Console.WriteLine($"       Slots: {slotsBefore} -> {slotsAfter}");
            Console.WriteLine($"       Vehicles: {vehiclesBefore} -> {vehiclesAfter}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  [FAIL] Idempotency failed! Duplicate records detected after second seed execution.");
            Console.WriteLine($"       Roles: {rolesBefore} -> {rolesAfter}");
            Console.WriteLine($"       Users: {usersBefore} -> {usersAfter}");
            Console.WriteLine($"       Areas: {areasBefore} -> {areasAfter}");
            Console.WriteLine($"       Slots: {slotsBefore} -> {slotsAfter}");
            Console.WriteLine($"       Vehicles: {vehiclesBefore} -> {vehiclesAfter}");
            Console.ResetColor();
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n================================================================================");
        Console.WriteLine("                  SEED VERIFICATION COMPLETED SUCCESSFULLY                      ");
        Console.WriteLine("================================================================================");
        Console.ResetColor();
    }
}
