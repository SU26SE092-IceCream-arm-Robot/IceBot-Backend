using Application.ClientDevices.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Application.ClientDevices;

public static class ClientDevicesModule
{
    public static IServiceCollection AddClientDevicesModule(this IServiceCollection services)
    {
        services.AddScoped<ClientDeviceManagementService>();
        services.AddScoped<ClientDeviceSessionService>();
        services.AddScoped<ClientDeviceCredentialHasher>();
        services.AddScoped<ICurrentClientDeviceContext, CurrentClientDeviceContext>();
        return services;
    }
}
