using Application.Operations.MaintenanceTickets.Commands;
using Application.Operations.MaintenanceTickets.Queries;
using Application.Operations.Alerts.Commands;
using Application.Operations.Alerts.Queries;
using Application.Operations.OperationLogs.Queries;
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
        services.AddScoped<ListOperationLogsQueryHandler>();
        services.AddScoped<GetOperationLogQueryHandler>();
        services.AddScoped<GetOperationLogDiagnosticsQueryHandler>();

        return services;
    }
}
