using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Infrastructure.Data;
using SmartParkingSystem.Infrastructure.Repositories;
using SmartParkingSystem.Infrastructure.Services;

namespace SmartParkingSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
            });
        });

        // Security & Authentication services
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();

        // Vehicle Management
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IVehicleService, VehicleService>();

        // Parking Area Management
        services.AddScoped<IParkingAreaRepository, ParkingAreaRepository>();
        services.AddScoped<IParkingAreaService, ParkingAreaService>();

        // Parking Slot Management
        services.AddScoped<IParkingSlotRepository, ParkingSlotRepository>();
        services.AddScoped<IParkingSlotService, ParkingSlotService>();

        // Reservation Management
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IReservationService, ReservationService>();

        // Payment Management
        services.AddScoped<IPaymentGateway, MockPaymentGateway>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentService, PaymentService>();

        // QR Code Management
        services.AddScoped<IQRCodeRepository, QRCodeRepository>();
        services.AddScoped<IQRCodeService, QRCodeService>();

        // Vehicle Check-In & Parking Management
        services.AddScoped<IParkingHistoryRepository, ParkingHistoryRepository>();
        services.AddScoped<IParkingService, ParkingService>();
        services.AddScoped<IParkingHistoryService, ParkingHistoryService>();

        // Dashboard & Reporting Analytics
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}
