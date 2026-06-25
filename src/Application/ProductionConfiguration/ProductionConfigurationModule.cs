using Application.ProductionConfiguration.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Application.ProductionConfiguration;

public static class ProductionConfigurationModule
{
    public static IServiceCollection AddProductionConfigurationModule(this IServiceCollection services)
    {
        services.AddScoped<PublishConfigurationReleaseCommandHandler>();
        services.AddScoped<DeployFullEdgeConfigurationCommandHandler>();
        services.AddScoped<DeployLowCostArtifactSetCommandHandler>();

        return services;
    }
}
