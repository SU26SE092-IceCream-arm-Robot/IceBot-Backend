using Application.EdgeIntegration.CommandDelivery.Commands;
using Application.EdgeIntegration.Dispatch.Commands;
using Application.EdgeIntegration.Reports.Commands;
using Application.EdgeIntegration.Timeouts.Commands;
using Application.EdgeIntegration.CommandDelivery.Services;
using Application.EdgeIntegration.Dispatch.Services;
using Application.EdgeIntegration.Reports.Services;
using Application.EdgeIntegration.Uplink;
using Microsoft.Extensions.DependencyInjection;

namespace Application.EdgeIntegration;

public static class EdgeIntegrationModule
{
    public static IServiceCollection AddEdgeIntegrationModule(this IServiceCollection services)
    {
        services.AddScoped<PullEdgeCommandsCommandHandler>();
        services.AddScoped<AcknowledgeEdgeCommandCommandHandler>();
        services.AddScoped<IngestExecutionReportCommandHandler>();
        services.AddScoped<DispatchOrderExecutionCommandHandler>();
        services.AddScoped<EscalateInitialDispatchFailureCommandHandler>();
        services.AddScoped<ReconcileOrderExecutionTimeoutCommandHandler>();
        services.AddScoped<ArtifactCommandPayloadEnricher>();
        services.AddScoped<IEdgeUplinkMessageDispatcher, EdgeUplinkMessageDispatcher>();

        return services;
    }
}
