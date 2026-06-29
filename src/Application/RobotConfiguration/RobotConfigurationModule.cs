using Application.RobotConfiguration.Commands;
using Application.RobotConfiguration.Queries;
using Application.RobotConfiguration.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.RobotConfiguration;

public static class RobotConfigurationModule
{
    public static IServiceCollection AddRobotConfigurationModule(this IServiceCollection services)
    {
        services.AddScoped<ArtifactUploadContentService>();
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

        return services;
    }
}
