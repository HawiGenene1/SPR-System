using Microsoft.Extensions.DependencyInjection;

namespace SmartParkingSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Application layer services, validators, and mapping profiles will be registered here
        return services;
    }
}
