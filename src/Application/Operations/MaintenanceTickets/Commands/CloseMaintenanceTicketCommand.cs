using Application.Identity.Tokens.Claims;
using Application.Operations.Abstractions;
using Application.Operations.MaintenanceTickets.Mapping;
using Application.Operations.MaintenanceTickets.Results;
using Application.Operations.MaintenanceTickets.Rules;
using Application.Shared.Wrappers;
using Domain.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;

namespace Application.Operations.MaintenanceTickets.Commands;

public sealed class CloseMaintenanceTicketCommand
{
    public required Guid TicketId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
}

public sealed class CloseMaintenanceTicketCommandHandler
{
    private readonly IMaintenanceTicketStore _ticketStore;
    private readonly IRealtimeNotificationPublisher _publisher;

    public CloseMaintenanceTicketCommandHandler(
        IMaintenanceTicketStore ticketStore,
        IRealtimeNotificationPublisher publisher)
    {
        _ticketStore = ticketStore;
        _publisher = publisher;
    }

    public async Task<ApiResult<MaintenanceTicketResult>> HandleAsync(
        CloseMaintenanceTicketCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = command.UserContext;

        var ticket = await _ticketStore.GetByIdAsync(command.TicketId, cancellationToken);
        if (ticket is null)
        {
            return ApiResult<MaintenanceTicketResult>.Fail("Maintenance ticket not found.", 404);
        }

        string oldStatus = ticket.Status.ToString();

        if (!MaintenanceTicketAccessRules.CanClose(user, ticket.OrganizationId, ticket.StoreId, ticket.KioskId))
        {
            return ApiResult<MaintenanceTicketResult>.Fail("Access denied.", 403);
        }

        try
        {
            ticket.Close(DateTimeOffset.UtcNow);
            ticket.UpdatedAt = DateTimeOffset.UtcNow;
            await _ticketStore.SaveChangesAsync(cancellationToken);
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<MaintenanceTicketResult>.Fail(ex.Message, 400);
        }

        var result = MaintenanceTicketResultMapper.ToResult(ticket);

        await _publisher.PublishMaintenanceTicketChangedAsync(new MaintenanceTicketChangedEvent
        {
            TicketId = result.Id,
            TicketNumber = result.TicketNumber,
            KioskId = result.KioskId,
            OrganizationId = result.OrganizationId,
            StoreId = result.StoreId,
            OldStatus = oldStatus,
            NewStatus = result.Status.ToString(),
            Priority = result.Priority.ToString(),
            UpdatedAt = result.UpdatedAt ?? DateTimeOffset.UtcNow,
            Version = 1
        }, cancellationToken);

        return ApiResult<MaintenanceTicketResult>.Success(result, "Maintenance ticket closed successfully.");
    }
}
