using Application.Tenants.Abstractions;
using Application.Tenants.Onboarding;
using Application.Tenants.Organizations.Abstractions;
using Infrastructure.Tenants.Persistence;
using Infrastructure.Tenants.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tenants;

public static class TenantsInfrastructureRegistration
{
    public static IServiceCollection AddTenantsInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IOrganizationStore, OrganizationStore>();
        services.AddScoped<IOrganizationSalesSummaryStore, OrganizationSalesSummaryStore>();
        services.AddScoped<IOrganizationAccessStateReader, OrganizationAccessStateReader>();
        services.AddOptions<OrganizationSessionRevocationOptions>()
            .Validate(options => options.IntervalSeconds is >= 5 and <= 3600 &&
                                 options.BatchSize is >= 1 and <= 500 &&
                                 options.RetryDelaySeconds is >= 5 and <= 3600,
                "Organization session revocation settings are invalid.")
            .ValidateOnStart();
        services.AddHostedService<OrganizationSessionRevocationJob>();
        services.AddScoped<IStoreStore, StoreStore>();
        services.AddScoped<IKioskStore, KioskStore>();
        services.AddScoped<ITenantTreeStore, TenantTreeStore>();
        services.AddScoped<IFranchiseOnboardingStore, FranchiseOnboardingStore>();
        return services;
    }
}
