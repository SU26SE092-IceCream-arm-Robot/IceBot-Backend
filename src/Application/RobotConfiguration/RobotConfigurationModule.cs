using Application.RobotConfiguration.ArtifactTemplates.Commands;
using Application.RobotConfiguration.ArtifactTemplates.Queries;
using Application.RobotConfiguration.Artifacts.Commands;
using Application.RobotConfiguration.Artifacts.Queries;
using Application.RobotConfiguration.Programs.Commands;
using Application.RobotConfiguration.Programs.Queries;
using Application.RobotConfiguration.Storage.Services;
using Application.RobotConfiguration.ArtifactContracts;
using Application.RobotConfiguration.AuthoringImports;
using Application.RobotConfiguration.AuthoringImports.ReleaseLinkage;
using Application.RobotConfiguration.AuthoringImports.Composition;
using Application.RobotConfiguration.AuthoringImports.Workspace;
using Application.RobotConfiguration.AuthoringImports.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Application.RobotConfiguration;

public static class RobotConfigurationModule
{
    public static IServiceCollection AddRobotConfigurationModule(this IServiceCollection services)
    {
        services.AddScoped<ArtifactUploadContentService>();
        services.AddScoped<ArtifactPublicationValidator>();
        services.AddScoped<UploadRobotArtifactCommandHandler>();
        services.AddScoped<BulkUploadRobotArtifactsCommandHandler>();
        services.AddScoped<BulkPublishRobotArtifactsCommandHandler>();
        services.AddScoped<PublishRobotArtifactCommandHandler>();
        services.AddScoped<RetireRobotArtifactCommandHandler>();
        services.AddScoped<DiscardDraftRobotArtifactCommandHandler>();
        services.AddScoped<PublishRobotProgramCommandHandler>();
        services.AddScoped<RetireRobotProgramCommandHandler>();
        services.AddScoped<DiscardDraftRobotProgramCommandHandler>();
        services.AddScoped<CreateRobotProgramCommandHandler>();
        services.AddScoped<ReplaceRobotProgramArtifactsCommandHandler>();
        services.AddScoped<UpdateRobotProgramCommandHandler>();
        services.AddScoped<ListRobotArtifactsQueryHandler>();
        services.AddScoped<GetRobotArtifactQueryHandler>();
        services.AddScoped<GetRobotArtifactUsageQueryHandler>();
        services.AddScoped<CreateRobotArtifactReviewUrlQueryHandler>();
        services.AddScoped<ListRobotProgramsQueryHandler>();
        services.AddScoped<GetRobotProgramQueryHandler>();
        services.AddScoped<UploadRobotArtifactTemplateCommandHandler>();
        services.AddScoped<BulkUploadRobotArtifactTemplatesCommandHandler>();
        services.AddScoped<ListRobotArtifactTemplatesQueryHandler>();
        services.AddScoped<GetRobotArtifactTemplateQueryHandler>();
        services.AddScoped<CreateRobotArtifactTemplateReviewUrlQueryHandler>();
        services.AddScoped<PublishRobotArtifactTemplateCommandHandler>();
        services.AddScoped<RetireRobotArtifactTemplateCommandHandler>();
        services.AddScoped<DiscardDraftRobotArtifactTemplateCommandHandler>();
        services.AddScoped<CloneRobotArtifactTemplateCommandHandler>();
        services.AddScoped<RobotArtifactTechnicalContractHandlers>();
        services.AddScoped<AssignRobotArtifactTechnicalContractHandler>();
        services.AddScoped<RobotAuthoringImportHandlers>();
        services.AddScoped<ListRobotAuthoringImportsQueryHandler>();
        services.AddScoped<RobotAuthoringImportValidator>();
        services.AddScoped<CreateRobotAuthoringReleaseDraftCommandHandler>();
        services.AddScoped<RobotAuthoringCompositionHandlers>();
        services.AddScoped<RobotAuthoringWorkspaceHandler>();

        return services;
    }
}
