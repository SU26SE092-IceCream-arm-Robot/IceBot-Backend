using Application.Operations.MaintenanceTickets.Commands;
using Application.Operations.MaintenanceTickets.Queries;
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

        return services;
    }
}
