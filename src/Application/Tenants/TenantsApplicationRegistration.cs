using Application.Tenants.Kiosks.Commands;
using Application.Tenants.Kiosks.Queries;
using Application.Tenants.Onboarding;
using Application.Tenants.Organizations.Commands;
using Application.Tenants.Organizations.Queries;
using Application.Tenants.RoleScopes.Queries;
using Application.Tenants.Stores.Commands;
using Application.Tenants.Stores.Queries;
using Application.Tenants.TenantTree.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Tenants;

public static class TenantsApplicationRegistration
{
    public static IServiceCollection AddTenantsApplication(this IServiceCollection services)
    {
        services.AddScoped<GetRoleScopeOptionsQueryHandler>();

        services.AddScoped<ListKiosksQueryHandler>();
        services.AddScoped<GetKioskQueryHandler>();
        services.AddScoped<CreateKioskCommandHandler>();
        services.AddScoped<UpdateKioskCommandHandler>();
        services.AddScoped<SetKioskStatusCommandHandler>();

        services.AddScoped<ListStoresQueryHandler>();
        services.AddScoped<GetStoreQueryHandler>();
        services.AddScoped<CreateStoreCommandHandler>();
        services.AddScoped<UpdateStoreCommandHandler>();
        services.AddScoped<DisableStoreCommandHandler>();
        services.AddScoped<ActivateStoreCommandHandler>();

        services.AddScoped<ListOrganizationsQueryHandler>();
        services.AddScoped<GetOrganizationQueryHandler>();
        services.AddScoped<CreateOrganizationCommandHandler>();
        services.AddScoped<UpdateOrganizationCommandHandler>();
        services.AddScoped<DisableOrganizationCommandHandler>();
        services.AddScoped<ActivateOrganizationCommandHandler>();

        services.AddScoped<GetTenantTreeQueryHandler>();
        services.AddScoped<FranchiseOnboardingService>();
        return services;
    }
}
