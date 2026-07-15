using Application.ProductionConfiguration.Releases.Commands;
using Application.ProductionConfiguration.Deployments.Commands;
using Application.ProductionConfiguration.Routes.Commands;
using Application.ProductionConfiguration.Releases.Queries;
using Application.ProductionConfiguration.Deployments.Queries;
using Application.ProductionConfiguration.Readiness.Queries;
using Application.ProductionConfiguration.Releases.Services;
using Application.ProductionConfiguration.Readiness.Services;
using Application.ProductionConfiguration.Deployments.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.ProductionConfiguration;

public static class ProductionConfigurationModule
{
    public static IServiceCollection AddProductionConfigurationModule(this IServiceCollection services)
    {
        services.AddScoped<PublishConfigurationReleaseCommandHandler>();
        services.AddScoped<FullEdgeReleaseBundleService>();
        services.AddScoped<ProductionInventoryReadinessGuard>();
        services.AddScoped<ProductionDefinitionPublicationService>();
        services.AddScoped<DeploymentValidationService>();
        services.AddScoped<DeploymentValidationPreviewHandler>();
        services.AddScoped<RetireConfigurationReleaseCommandHandler>();
        services.AddScoped<DiscardDraftConfigurationReleaseCommandHandler>();
        services.AddScoped<DeployFullEdgeConfigurationCommandHandler>();
        services.AddScoped<DeployLowCostArtifactSetCommandHandler>();
        services.AddScoped<CreateConfigurationReleaseCommandHandler>();
        services.AddScoped<ReplaceConfigurationReleaseRoutesCommandHandler>();
        services.AddScoped<ListConfigurationReleasesQueryHandler>();
        services.AddScoped<GetConfigurationReleaseQueryHandler>();
        services.AddScoped<GetConfigurationReleaseAuthoringOptionsQueryHandler>();
        services.AddScoped<GetConfigurationInventoryReadinessQueryHandler>();
        services.AddScoped<ListConfigurationDeploymentsQueryHandler>();
        services.AddScoped<GetConfigurationDeploymentQueryHandler>();
        services.AddScoped<GetConfigurationDeploymentArtifactsQueryHandler>();
        services.AddScoped<RollbackConfigurationDeploymentCommandHandler>();
        services.AddScoped<ReconcileExpiredDeploymentCommandsCommandHandler>();
        services.AddScoped<ReconcileAcceptedDeploymentReportTimeoutsCommandHandler>();
        services.AddScoped<ReconcileInstalledDeploymentActivationTimeoutsCommandHandler>();

        return services;
    }
}
