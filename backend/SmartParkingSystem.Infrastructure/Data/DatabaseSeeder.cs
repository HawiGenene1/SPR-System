using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration)
    {
        // 1. Seed Roles (Idempotent: Check each role by name)
        var roleDefinitions = new[]
        {
            (UserRole.Admin, "Administrator with full system control and management privileges"),
            (UserRole.ParkingAttendant, "Parking attendant who scans QR codes and manages vehicle check-in and check-out"),
            (UserRole.Customer, "Customer who searches parking, reserves slots, registers vehicles, and pays")
        };

        var roleMap = new Dictionary<UserRole, Role>();

        foreach (var (userRole, description) in roleDefinitions)
        {
            var roleName = userRole.ToString();
            var existingRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);

            if (existingRole == null)
            {
                existingRole = new Role
                {
                    Name = roleName,
                    Description = description
                };
                await context.Roles.AddAsync(existingRole);
                await context.SaveChangesAsync();
            }

            roleMap[userRole] = existingRole;
        }

        // 2. Seed Initial Parking Area & Slots
        var areaName = "Central Plaza Parking";
        var existingArea = await context.ParkingAreas
            .Include(a => a.ParkingSlots)
            .FirstOrDefaultAsync(a => a.Name == areaName);

        if (existingArea == null)
        {
            existingArea = new ParkingArea
            {
                Name = areaName,
                Description = "Multi-level modern parking facility with 24/7 security and EV charging stations.",
                Address = "100 Commercial Avenue",
                City = "Metropolis",
                Latitude = 40.7128m,
                Longitude = -74.0060m,
                TotalSlots = 20,
                HourlyRate = 5.00m,
                OperatingHours = "24/7",
                IsActive = true
            };

            await context.ParkingAreas.AddAsync(existingArea);
            await context.SaveChangesAsync();

            var slots = new List<ParkingSlot>();

            // Floor 1: Standard (A-01 to A-08), Accessible (AC-01, AC-02), EV Charging (EV-01, EV-02)
            for (int i = 1; i <= 8; i++)
            {
                slots.Add(new ParkingSlot
                {
                    ParkingAreaId = existingArea.Id,
                    SlotNumber = $"A-{i:D2}",
                    FloorNumber = 1,
                    SlotType = SlotType.Standard,
                    Status = SlotStatus.Available
                });
            }

            slots.Add(new ParkingSlot
            {
                ParkingAreaId = existingArea.Id,
                SlotNumber = "AC-01",
                FloorNumber = 1,
                SlotType = SlotType.Accessible,
                Status = SlotStatus.Available
            });

            slots.Add(new ParkingSlot
            {
                ParkingAreaId = existingArea.Id,
                SlotNumber = "AC-02",
                FloorNumber = 1,
                SlotType = SlotType.Accessible,
                Status = SlotStatus.Available
            });

            slots.Add(new ParkingSlot
            {
                ParkingAreaId = existingArea.Id,
                SlotNumber = "EV-01",
                FloorNumber = 1,
                SlotType = SlotType.EVCharging,
                VehicleType = VehicleType.ElectricVehicle,
                Status = SlotStatus.Available
            });

            slots.Add(new ParkingSlot
            {
                ParkingAreaId = existingArea.Id,
                SlotNumber = "EV-02",
                FloorNumber = 1,
                SlotType = SlotType.EVCharging,
                VehicleType = VehicleType.ElectricVehicle,
                Status = SlotStatus.Available
            });

            // Floor 2: Compact (B-01 to B-06), VIP (VIP-01, VIP-02)
            for (int i = 1; i <= 6; i++)
            {
                slots.Add(new ParkingSlot
                {
                    ParkingAreaId = existingArea.Id,
                    SlotNumber = $"B-{i:D2}",
                    FloorNumber = 2,
                    SlotType = SlotType.Compact,
                    Status = SlotStatus.Available
                });
            }

            slots.Add(new ParkingSlot
            {
                ParkingAreaId = existingArea.Id,
                SlotNumber = "VIP-01",
                FloorNumber = 2,
                SlotType = SlotType.VIP,
                Status = SlotStatus.Available
            });

            slots.Add(new ParkingSlot
            {
                ParkingAreaId = existingArea.Id,
                SlotNumber = "VIP-02",
                FloorNumber = 2,
                SlotType = SlotType.VIP,
                Status = SlotStatus.Available
            });

            await context.ParkingSlots.AddRangeAsync(slots);
            await context.SaveChangesAsync();
        }

        // 3. Seed Default Administrator Account
        var adminEmail = configuration["InitialAdmin:Email"] ?? "admin@smartparking.com";
        var existingAdmin = await context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);

        if (existingAdmin == null)
        {
            var adminPassword = configuration["InitialAdmin:Password"] ?? "Admin@Password123!";
            var adminFullName = configuration["InitialAdmin:FullName"] ?? "System Administrator";
            var adminPhone = configuration["InitialAdmin:PhoneNumber"] ?? "+1-555-0100";

            var adminUser = new User
            {
                FullName = adminFullName,
                Email = adminEmail,
                PasswordHash = passwordHasher.HashPassword(adminPassword),
                PhoneNumber = adminPhone,
                RoleId = roleMap[UserRole.Admin].Id,
                IsActive = true
            };

            await context.Users.AddAsync(adminUser);
            await context.SaveChangesAsync();
        }

        // 4. Seed Default Parking Attendant Account (assigned to initial parking area)
        var attendantEmail = "attendant@smartparking.com";
        var existingAttendant = await context.Users.FirstOrDefaultAsync(u => u.Email == attendantEmail);

        if (existingAttendant == null)
        {
            var attendantUser = new User
            {
                FullName = "Central Gate Attendant",
                Email = attendantEmail,
                PasswordHash = passwordHasher.HashPassword("Attendant@123456"),
                PhoneNumber = "+1-555-0101",
                RoleId = roleMap[UserRole.ParkingAttendant].Id,
                AssignedParkingAreaId = existingArea.Id,
                IsActive = true
            };

            await context.Users.AddAsync(attendantUser);
            await context.SaveChangesAsync();
        }

        // 5. Seed Sample Customer Account with Registered Vehicle
        var customerEmail = "customer@smartparking.com";
        var existingCustomer = await context.Users
            .Include(u => u.Vehicles)
            .FirstOrDefaultAsync(u => u.Email == customerEmail);

        if (existingCustomer == null)
        {
            var customerUser = new User
            {
                FullName = "John Customer",
                Email = customerEmail,
                PasswordHash = passwordHasher.HashPassword("Customer@123456"),
                PhoneNumber = "+1-555-0102",
                RoleId = roleMap[UserRole.Customer].Id,
                IsActive = true
            };

            await context.Users.AddAsync(customerUser);
            await context.SaveChangesAsync();

            var sampleVehicle = new Vehicle
            {
                UserId = customerUser.Id,
                LicensePlate = "ABC-9876",
                VehicleType = VehicleType.Car,
                Make = "Toyota",
                Model = "Camry",
                Color = "Silver"
            };

            await context.Vehicles.AddAsync(sampleVehicle);
            await context.SaveChangesAsync();
        }
    }
}
