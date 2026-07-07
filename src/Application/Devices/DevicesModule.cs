using Application.Devices.Commands;
using Application.Devices.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Devices;

public static class DevicesModule
{
    public static IServiceCollection AddDevicesModule(this IServiceCollection services)
    {
        services.AddScoped<GetKioskHeartbeatsQueryHandler>();
        services.AddScoped<GetKioskDeviceEventsQueryHandler>();
        services.AddScoped<GetKioskStatusOverviewQueryHandler>();
        services.AddScoped<IngestKioskHeartbeatCommandHandler>();
        services.AddScoped<IngestDeviceEventCommandHandler>();
        services.AddScoped<IngestLocalOperationLogCommandHandler>();
        services.AddScoped<IngestBatchEventsCommandHandler>();
        services.AddScoped<IngestProductionEventCommandHandler>();
        services.AddScoped<IngestProductionEventsBatchCommandHandler>();
        services.AddScoped<IngestExecutionReadinessCommandHandler>();
        services.AddScoped<IngestEdgeStateSummariesCommandHandler>();
        services.AddScoped<GetProductionEventCheckpointQueryHandler>();
        services.AddScoped<ReconcileKioskConnectivityCommandHandler>();

        services.AddScoped<ListDevicesQueryHandler>();
        services.AddScoped<GetDeviceQueryHandler>();
        services.AddScoped<CreateDeviceCommandHandler>();
        services.AddScoped<UpdateDeviceCommandHandler>();
        services.AddScoped<SetDeviceStatusCommandHandler>();
        services.AddScoped<RetireDeviceCommandHandler>();
        services.AddScoped<ReplaceDeviceCommandHandler>();
        services.AddScoped<ListDeviceTypesQueryHandler>();
        services.AddScoped<GetDeviceTypeQueryHandler>();
        services.AddScoped<ListDeviceModelsQueryHandler>();
        services.AddScoped<GetDeviceModelQueryHandler>();
        services.AddScoped<CreateDeviceTypeCommandHandler>();
        services.AddScoped<UpdateDeviceTypeCommandHandler>();
        services.AddScoped<SetDeviceTypeStatusCommandHandler>();
        services.AddScoped<CreateDeviceModelCommandHandler>();
        services.AddScoped<UpdateDeviceModelCommandHandler>();
        services.AddScoped<RetireDeviceModelCommandHandler>();
        services.AddScoped<ListExecutionEndpointsQueryHandler>();
        services.AddScoped<GetExecutionEndpointQueryHandler>();
        services.AddScoped<CreateExecutionEndpointCommandHandler>();
        services.AddScoped<ReplaceExecutionEndpointRobotTargetsCommandHandler>();
        services.AddScoped<ProvisionExecutionEndpointCommandHandler>();
        services.AddScoped<DisableExecutionEndpointCommandHandler>();
        services.AddScoped<ReactivateExecutionEndpointCommandHandler>();
        services.AddScoped<RetireExecutionEndpointCommandHandler>();
        services.AddScoped<RotateExecutionEndpointCredentialCommandHandler>();
        services.AddScoped<ProvisionMqttEndpointCredentialCommandHandler>();
        services.AddScoped<RotateMqttEndpointCredentialCommandHandler>();
        services.AddScoped<RevokeMqttEndpointCredentialCommandHandler>();

        return services;
    }
}
