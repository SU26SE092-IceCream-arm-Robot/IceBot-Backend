using Microsoft.Extensions.DependencyInjection;
using Application.Shared.Ownership;
using Application.ProductionPackages.Ownership;

namespace Application.ProductionPackages;

public static class ProductionPackagesModule
{
    public static IServiceCollection AddProductionPackagesModule(this IServiceCollection services)
    {
        services.AddScoped<ProductionPackageHandlers>();
        services.AddScoped<Installation.ProductionPackageInstallationService>();
        services.AddScoped<Workspace.ProductionPackageWorkspaceService>();
        services.AddScoped<Upgrades.ProductionPackageUpgradeService>();
        services.AddScoped<Upgrades.ProductionPackageUpgradePreviewService>();
        services.AddScoped<Upgrades.ProductionPackageUpgradeMutationPolicy>();
        services.AddScoped<Upgrades.ProductionPackageUpgradeReconciliationService>();
        services.AddScoped<ITechnicalResourceMutationPolicy, ProductionPackageTechnicalOwnershipPolicy>();
        return services;
    }
}
