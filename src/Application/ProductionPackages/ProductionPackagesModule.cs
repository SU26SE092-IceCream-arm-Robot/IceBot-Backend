using Microsoft.Extensions.DependencyInjection;

namespace Application.ProductionPackages;

public static class ProductionPackagesModule
{
    public static IServiceCollection AddProductionPackagesModule(this IServiceCollection services)
    {
        services.AddScoped<ProductionPackageHandlers>();
        services.AddScoped<Installation.ProductionPackageInstallationService>();
        services.AddScoped<Workspace.ProductionPackageWorkspaceService>();
        return services;
    }
}
