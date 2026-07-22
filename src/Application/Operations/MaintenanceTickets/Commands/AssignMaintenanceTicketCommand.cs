using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.Identity.Tokens.Claims;
using Application.Operations.Abstractions;
using Application.Operations.MaintenanceTickets.Mapping;
using Application.Operations.MaintenanceTickets.Requests;
using Application.Operations.MaintenanceTickets.Results;
using Application.Operations.MaintenanceTickets.Rules;
using Application.Shared.Wrappers;
using Domain.Common;
using Application.Operations.Notifications;

namespace Application.Operations.MaintenanceTickets.Commands;

public sealed class AssignMaintenanceTicketCommand
{
    public required Guid TicketId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
    public required AssignMaintenanceTicketRequest Request { get; init; }
}

public sealed class AssignMaintenanceTicketCommandHandler
{
    private readonly IMaintenanceTicketStore _ticketStore;
    private readonly IRealtimeNotificationPublisher _publisher;
    private readonly IMaintenanceAssignmentNotifier _notifier;

    public AssignMaintenanceTicketCommandHandler(
        IMaintenanceTicketStore ticketStore,
        IRealtimeNotificationPublisher publisher,
        IMaintenanceAssignmentNotifier notifier)
    {
        _ticketStore = ticketStore;
        _publisher = publisher;
        _notifier = notifier;
    }

    public async Task<ApiResult<MaintenanceTicketResult>> HandleAsync(
        AssignMaintenanceTicketCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = command.UserContext;
        var req = command.Request;

        // 1. Fetch MaintenanceTicket
        var ticket = await _ticketStore.GetByIdAsync(command.TicketId, cancellationToken);
        if (ticket is null)
        {
            return ApiResult<MaintenanceTicketResult>.Fail("Maintenance ticket not found.", 404);
        }

        string oldStatus = ticket.Status.ToString();

        // 2. Authorization Access Check
        if (!MaintenanceTicketAccessRules.CanAssign(user, ticket.OrganizationId, ticket.StoreId, ticket.KioskId))
        {
            return ApiResult<MaintenanceTicketResult>.Fail("Access denied.", 403);
        }

        if (!await _ticketStore.CanAssignAccountAsync(
                req.AssignedToAccountId,
                ticket.OrganizationId,
                ticket.StoreId,
                ticket.KioskId,
                cancellationToken))
        {
            return ApiResult<MaintenanceTicketResult>.Fail(
                "Assignee is not an active maintenance operator in the ticket scope.",
                400);
        }

        // 3. Assign
        try
        {
            ticket.Assign(req.AssignedToAccountId);
            ticket.UpdatedAt = DateTimeOffset.UtcNow;
            await _notifier.NotifyAsync(ticket, cancellationToken);
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

        return ApiResult<MaintenanceTicketResult>.Success(result, "Maintenance ticket assigned successfully.");
    }
}
