using Application.Devices.Catalog.Commands;
using Application.Devices.ExecutionEndpoints.Commands;
using Application.Devices.Telemetry.Commands;
using Application.Devices.Connectivity.Commands;
using Application.Devices.Credentials.Commands;
using Application.Devices.Catalog.Queries;
using Application.Devices.ExecutionEndpoints.Queries;
using Application.Devices.Telemetry.Queries;
using Application.Devices.Connectivity.Queries;
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
        services.AddScoped<IngestExecutionReadinessCommandHandler>();
        services.AddScoped<ReplaceExecutionEndpointReportedDevicesCommandHandler>();
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
        services.AddScoped<ProvisionExecutionEndpointCommandHandler>();
        services.AddScoped<DisableExecutionEndpointCommandHandler>();
        services.AddScoped<ReactivateExecutionEndpointCommandHandler>();
        services.AddScoped<RetireExecutionEndpointCommandHandler>();
        services.AddScoped<RotateExecutionEndpointCredentialCommandHandler>();
        services.AddScoped<ProvisionMqttEndpointCredentialCommandHandler>();
        services.AddScoped<RotateMqttEndpointCredentialCommandHandler>();
        services.AddScoped<RevokeMqttEndpointCredentialCommandHandler>();
        services.AddScoped<ReconcileStaleMqttEndpointCredentialCommandHandler>();

        return services;
    }
}
