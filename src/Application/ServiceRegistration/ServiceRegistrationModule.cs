using Microsoft.Extensions.DependencyInjection;

namespace Application.ServiceRegistration;

public static class ServiceRegistrationModule
{
    public static IServiceCollection AddServiceRegistrationApplication(this IServiceCollection services)
    {
        services.AddScoped<ServiceRegistrationService>();
        return services;
    }
}
