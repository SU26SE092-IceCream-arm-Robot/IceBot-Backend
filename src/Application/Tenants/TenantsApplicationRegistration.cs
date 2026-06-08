using Application.Tenants.Organizations.Services;
using Application.Tenants.Stores.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Tenants;

public static class TenantsApplicationRegistration
{
    public static IServiceCollection AddTenantsApplication(this IServiceCollection services)
    {
        services.AddScoped<OrganizationManagementService>();
        services.AddScoped<StoreManagementService>();
        return services;
    }
}
