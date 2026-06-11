using Application.Devices.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Devices;

public static class DevicesModule
{
    public static IServiceCollection AddDevicesModule(this IServiceCollection services)
    {
        services.AddScoped<GetKioskHeartbeatsQueryHandler>();
        services.AddScoped<GetKioskDeviceEventsQueryHandler>();
        services.AddScoped<GetKioskStatusOverviewQueryHandler>();

        return services;
    }
}
