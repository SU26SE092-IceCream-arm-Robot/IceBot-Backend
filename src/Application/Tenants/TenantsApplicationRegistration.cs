using Application.Tenants.Organizations.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Tenants;

public static class TenantsApplicationRegistration
{
    public static IServiceCollection AddTenantsApplication(this IServiceCollection services)
    {
        services.AddScoped<OrganizationManagementService>();
        return services;
    }
}
