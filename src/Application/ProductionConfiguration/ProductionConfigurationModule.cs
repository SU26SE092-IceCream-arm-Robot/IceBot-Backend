using Application.ProductionConfiguration.Commands;
using Application.ProductionConfiguration.Queries;
using Application.ProductionConfiguration.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.ProductionConfiguration;

public static class ProductionConfigurationModule
{
    public static IServiceCollection AddProductionConfigurationModule(this IServiceCollection services)
    {
        services.AddScoped<PublishConfigurationReleaseCommandHandler>();
        services.AddScoped<FullEdgeReleaseBundleService>();
        services.AddScoped<RetireConfigurationReleaseCommandHandler>();
        services.AddScoped<DiscardDraftConfigurationReleaseCommandHandler>();
        services.AddScoped<DeployFullEdgeConfigurationCommandHandler>();
        services.AddScoped<DeployLowCostArtifactSetCommandHandler>();
        services.AddScoped<CreateConfigurationReleaseCommandHandler>();
        services.AddScoped<ReplaceConfigurationReleaseRoutesCommandHandler>();
        services.AddScoped<ListConfigurationReleasesQueryHandler>();
        services.AddScoped<GetConfigurationReleaseQueryHandler>();
        services.AddScoped<GetConfigurationReleaseAuthoringOptionsQueryHandler>();
        services.AddScoped<ListConfigurationDeploymentsQueryHandler>();
        services.AddScoped<GetConfigurationDeploymentQueryHandler>();
        services.AddScoped<RollbackConfigurationDeploymentCommandHandler>();
        services.AddScoped<ReconcileExpiredDeploymentCommandsCommandHandler>();
        services.AddScoped<ReconcileAcceptedDeploymentReportTimeoutsCommandHandler>();
        services.AddScoped<ReconcileInstalledDeploymentActivationTimeoutsCommandHandler>();

        return services;
    }
}
