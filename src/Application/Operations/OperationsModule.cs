using Application.Operations.MaintenanceTickets.Commands;
using Application.Operations.MaintenanceTickets.Queries;
using Application.Operations.Alerts.Commands;
using Application.Operations.Alerts.Queries;
using Application.Operations.Alerts.Notifications;
using Application.Operations.OperationLogs.Queries;
using Application.Operations.Notifications.Diagnostics;
using Application.Operations.Notifications.Recovery;
using Application.Operations.Notifications;
using Application.Operations.Alerts.Automation;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Operations;

public static class OperationsModule
{
    public static IServiceCollection AddOperationsModule(this IServiceCollection services)
    {
        services.AddScoped<CreateMaintenanceTicketCommandHandler>();
        services.AddScoped<UpdateMaintenanceTicketCommandHandler>();
        services.AddScoped<AssignMaintenanceTicketCommandHandler>();
        services.AddScoped<StartMaintenanceTicketCommandHandler>();
        services.AddScoped<ResolveMaintenanceTicketCommandHandler>();
        services.AddScoped<CloseMaintenanceTicketCommandHandler>();
        services.AddScoped<CancelMaintenanceTicketCommandHandler>();
        services.AddScoped<GetMaintenanceTicketQueryHandler>();
        services.AddScoped<ListMaintenanceTicketsQueryHandler>();
        services.AddScoped<ListAlertsQueryHandler>();
        services.AddScoped<GetAlertQueryHandler>();
        services.AddScoped<AcknowledgeAlertCommandHandler>();
        services.AddScoped<ResolveAlertCommandHandler>();
        services.AddScoped<ICriticalOperationalAlertNotifier, CriticalOperationalAlertNotifier>();
        services.AddScoped<IInventoryOperationalAlertNotifier, InventoryOperationalAlertNotifier>();
        services.AddScoped<ListOperationLogsQueryHandler>();
        services.AddScoped<GetOperationLogQueryHandler>();
        services.AddScoped<GetOperationLogDiagnosticsQueryHandler>();
        services.AddScoped<NotificationDeliveryDiagnosticsService>();
        services.AddScoped<NotificationDeliveryOperationsService>();
        services.AddScoped<RequeueNotificationDeliveryService>();
        services.AddScoped<IMaintenanceAssignmentNotifier, MaintenanceAssignmentNotifier>();
        services.AddScoped<MqttCredentialOperationalAlertReconciler>();

        return services;
    }
}
