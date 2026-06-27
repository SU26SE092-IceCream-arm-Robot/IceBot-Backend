using Application.ProductionConfiguration.Commands;
using Application.ProductionConfiguration.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Application.ProductionConfiguration;

public static class ProductionConfigurationModule
{
    public static IServiceCollection AddProductionConfigurationModule(this IServiceCollection services)
    {
        services.AddScoped<PublishConfigurationReleaseCommandHandler>();
        services.AddScoped<DeployFullEdgeConfigurationCommandHandler>();
        services.AddScoped<DeployLowCostArtifactSetCommandHandler>();
        services.AddScoped<CreateConfigurationReleaseCommandHandler>();
        services.AddScoped<ReplaceConfigurationReleaseRoutesCommandHandler>();
        services.AddScoped<ListConfigurationReleasesQueryHandler>();
        services.AddScoped<GetConfigurationReleaseQueryHandler>();

        return services;
    }
}
