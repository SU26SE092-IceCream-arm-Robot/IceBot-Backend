using Application.RobotConfiguration.Commands;
using Application.RobotConfiguration.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Application.RobotConfiguration;

public static class RobotConfigurationModule
{
    public static IServiceCollection AddRobotConfigurationModule(this IServiceCollection services)
    {
        services.AddScoped<UploadRobotArtifactCommandHandler>();
        services.AddScoped<BulkUploadRobotArtifactsCommandHandler>();
        services.AddScoped<PublishRobotArtifactCommandHandler>();
        services.AddScoped<PublishRobotProgramCommandHandler>();
        services.AddScoped<CreateRobotProgramCommandHandler>();
        services.AddScoped<ReplaceRobotProgramArtifactsCommandHandler>();
        services.AddScoped<UpdateRobotProgramCommandHandler>();
        services.AddScoped<ListRobotArtifactsQueryHandler>();
        services.AddScoped<GetRobotArtifactQueryHandler>();
        services.AddScoped<ListRobotProgramsQueryHandler>();
        services.AddScoped<GetRobotProgramQueryHandler>();

        return services;
    }
}
