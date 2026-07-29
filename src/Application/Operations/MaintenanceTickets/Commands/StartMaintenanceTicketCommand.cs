using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.Identity.Tokens.Claims;
using Application.Operations.Abstractions;
using Application.Operations.MaintenanceTickets.Mapping;
using Application.Operations.MaintenanceTickets.Results;
using Application.Operations.MaintenanceTickets.Rules;
using Application.Shared.Wrappers;
using Domain.Common;
using Domain.Operations.Enums;
using Domain.Tenants.Enums;

namespace Application.Operations.MaintenanceTickets.Commands;

public sealed class StartMaintenanceTicketCommand
{
    public required Guid TicketId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
}

public sealed class StartMaintenanceTicketCommandHandler
{
    private readonly IMaintenanceTicketStore _ticketStore;
    private readonly IRealtimeNotificationPublisher _publisher;

    public StartMaintenanceTicketCommandHandler(
        IMaintenanceTicketStore ticketStore,
        IRealtimeNotificationPublisher publisher)
    {
        _ticketStore = ticketStore;
        _publisher = publisher;
    }

    public async Task<ApiResult<MaintenanceTicketResult>> HandleAsync(
        StartMaintenanceTicketCommand command,
        CancellationToken cancellationToken = default)
    {
        MaintenanceTicketChangedEvent? ticketEvent = null;
        KioskOperationalStateChangedEvent? kioskEvent = null;
        var result = await _ticketStore.ExecuteInTransactionAsync(
            async ct =>
            {
                var ticket = await _ticketStore.GetByIdAsync(command.TicketId, ct);
                if (ticket is null)
                {
                    return ApiResult<MaintenanceTicketResult>.Fail("Maintenance ticket not found.", 404);
                }

                if (!MaintenanceTicketAccessRules.CanStart(
                        command.UserContext,
                        ticket.OrganizationId,
                        ticket.StoreId,
                        ticket.KioskId))
                {
                    return ApiResult<MaintenanceTicketResult>.Fail("Access denied.", 403);
                }

                await _ticketStore.AcquireKioskOperationalLockAsync(ticket.KioskId, ct);
                if (ticket.OperationalImpact == MaintenanceOperationalImpact.BlocksNewOrders &&
                    await _ticketStore.HasRunningExecutionAsync(ticket.KioskId, ct))
                {
                    return ApiResult<MaintenanceTicketResult>.Fail(
                        "Maintenance that blocks orders cannot start while an execution is running. Use RequestsEmergencyStop when immediate safety intervention must be requested.",
                        409);
                }

                var oldStatus = ticket.Status.ToString();
                var oldOperationalState = ticket.Kiosk.OperationalState;
                var changedAt = DateTimeOffset.UtcNow;
                try
                {
                    ticket.StartWork();
                    ticket.UpdatedAt = changedAt;
                    var targetState = ticket.OperationalImpact switch
                    {
                        MaintenanceOperationalImpact.BlocksNewOrders => KioskOperationalState.Maintenance,
                        MaintenanceOperationalImpact.RequestsEmergencyStop => KioskOperationalState.EmergencyStopRequested,
                        _ => (KioskOperationalState?)null
                    };
                    if (targetState.HasValue)
                    {
                        var transition = ticket.Kiosk.ChangeOperationalState(
                            targetState.Value,
                            $"Maintenance ticket {ticket.TicketNumber} started.",
                            command.UserContext.AccountId,
                            changedAt,
                            ticket.Id);
                        if (transition is not null)
                        {
                            await _ticketStore.AddOperationalStateTransitionAsync(transition, ct);
                            kioskEvent = new KioskOperationalStateChangedEvent
                            {
                                KioskId = ticket.KioskId,
                                OrganizationId = ticket.OrganizationId,
                                StoreId = ticket.StoreId,
                                OldState = oldOperationalState.ToString(),
                                NewState = targetState.Value.ToString(),
                                Reason = transition.Reason,
                                ChangedByAccountId = command.UserContext.AccountId,
                                SourceMaintenanceTicketId = ticket.Id,
                                ChangedAt = changedAt
                            };
                        }
                    }

                    await _ticketStore.SaveChangesAsync(ct);
                }
                catch (DomainRuleException exception)
                {
                    return ApiResult<MaintenanceTicketResult>.Fail(exception.Message, 400);
                }

                var response = MaintenanceTicketResultMapper.ToResult(ticket);
                ticketEvent = new MaintenanceTicketChangedEvent
                {
                    TicketId = response.Id,
                    TicketNumber = response.TicketNumber,
                    KioskId = response.KioskId,
                    OrganizationId = response.OrganizationId,
                    StoreId = response.StoreId,
                    OldStatus = oldStatus,
                    NewStatus = response.Status,
                    Priority = response.Priority,
                    UpdatedAt = response.UpdatedAt ?? changedAt,
                    Version = 1
                };
                return ApiResult<MaintenanceTicketResult>.Success(
                    response,
                    "Maintenance ticket status updated to InProgress.");
            },
            cancellationToken);

        if (ticketEvent is not null)
        {
            await _publisher.PublishMaintenanceTicketChangedAsync(ticketEvent, cancellationToken);
        }
        if (kioskEvent is not null)
        {
            await _publisher.PublishKioskOperationalStateChangedAsync(kioskEvent, cancellationToken);
        }

        return result;
    }
}
