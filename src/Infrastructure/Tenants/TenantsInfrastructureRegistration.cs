using Application.Tenants.Abstractions;
using Infrastructure.Tenants.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tenants;

public static class TenantsInfrastructureRegistration
{
    public static IServiceCollection AddTenantsInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IOrganizationStore, OrganizationStore>();
        services.AddScoped<IStoreStore, StoreStore>();
        return services;
    }
}
