using Application.EdgeIntegration.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Application.EdgeIntegration;

public static class EdgeIntegrationModule
{
    public static IServiceCollection AddEdgeIntegrationModule(this IServiceCollection services)
    {
        services.AddScoped<PullEdgeCommandsCommandHandler>();
        services.AddScoped<AcknowledgeEdgeCommandCommandHandler>();
        services.AddScoped<IngestExecutionReportCommandHandler>();

        return services;
    }
}
