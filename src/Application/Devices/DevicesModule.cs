using Application.Devices.Commands;
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

        services.AddScoped<ListDevicesQueryHandler>();
        services.AddScoped<GetDeviceQueryHandler>();
        services.AddScoped<CreateDeviceCommandHandler>();
        services.AddScoped<UpdateDeviceCommandHandler>();
        services.AddScoped<SetDeviceStatusCommandHandler>();
        services.AddScoped<RetireDeviceCommandHandler>();

        return services;
    }
}
