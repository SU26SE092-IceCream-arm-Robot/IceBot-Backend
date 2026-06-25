using Application.RobotConfiguration.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Application.RobotConfiguration;

public static class RobotConfigurationModule
{
    public static IServiceCollection AddRobotConfigurationModule(this IServiceCollection services)
    {
        services.AddScoped<UploadRobotArtifactCommandHandler>();
        services.AddScoped<PublishRobotArtifactCommandHandler>();
        services.AddScoped<PublishRobotProgramCommandHandler>();

        return services;
    }
}
