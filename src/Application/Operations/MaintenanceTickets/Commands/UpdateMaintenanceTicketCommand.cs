using Application.Identity.Tokens.Claims;
using Application.Operations.Abstractions;
using Application.Operations.MaintenanceTickets.Mapping;
using Application.Operations.MaintenanceTickets.Requests;
using Application.Operations.MaintenanceTickets.Results;
using Application.Operations.MaintenanceTickets.Rules;
using Application.Shared.Wrappers;
using Domain.Operations.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Operations.MaintenanceTickets.Commands;

public sealed class UpdateMaintenanceTicketCommand
{
    public required Guid TicketId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
    public required UpdateMaintenanceTicketRequest Request { get; init; }
}

public sealed class UpdateMaintenanceTicketCommandHandler
{
    private readonly IMaintenanceTicketStore _ticketStore;

    public UpdateMaintenanceTicketCommandHandler(IMaintenanceTicketStore ticketStore)
    {
        _ticketStore = ticketStore;
    }

    public async Task<ApiResult<MaintenanceTicketResult>> HandleAsync(
        UpdateMaintenanceTicketCommand command,
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

        // 2. Authorization Access Check
        if (!MaintenanceTicketAccessRules.CanUpdate(user, ticket.OrganizationId, ticket.StoreId, ticket.KioskId))
        {
            return ApiResult<MaintenanceTicketResult>.Fail("Access denied.", 403);
        }

        // 3. Ensure ticket is not resolved, closed, or cancelled
        if (ticket.Status is MaintenanceTicketStatus.Resolved or MaintenanceTicketStatus.Closed or MaintenanceTicketStatus.Cancelled)
        {
            return ApiResult<MaintenanceTicketResult>.Fail("Cannot update a resolved, closed, or cancelled maintenance ticket.", 400);
        }

        // 4. Validate optional DeviceId
        if (req.DeviceId.HasValue)
        {
            var isDeviceValid = await _ticketStore.DeviceBelongsToKioskAsync(req.DeviceId.Value, ticket.KioskId, cancellationToken);
            if (!isDeviceValid)
            {
                return ApiResult<MaintenanceTicketResult>.Fail("The specified device does not belong to this kiosk.", 400);
            }
        }

        // 5. Validate optional OrderId
        if (req.OrderId.HasValue)
        {
            var isOrderValid = await _ticketStore.OrderBelongsToScopeAsync(req.OrderId.Value, ticket.OrganizationId, ticket.StoreId, ticket.KioskId, cancellationToken);
            if (!isOrderValid)
            {
                return ApiResult<MaintenanceTicketResult>.Fail("The specified order does not belong to this kiosk scope.", 400);
            }
        }

        // 6. Validate optional DeviceEventId
        if (req.DeviceEventId.HasValue)
        {
            var isEventValid = await _ticketStore.DeviceEventBelongsToKioskAsync(req.DeviceEventId.Value, ticket.KioskId, cancellationToken);
            if (!isEventValid)
            {
                return ApiResult<MaintenanceTicketResult>.Fail("The specified device event does not belong to this kiosk.", 400);
            }
        }

        // 7. Update ticket details
        ticket.Title = req.Title.Trim();
        ticket.Description = req.Description?.Trim();
        ticket.Priority = req.Priority;
        ticket.DeviceId = req.DeviceId;
        ticket.OrderId = req.OrderId;
        ticket.DeviceEventId = req.DeviceEventId;
        ticket.UpdatedAt = DateTimeOffset.UtcNow;

        await _ticketStore.SaveChangesAsync(cancellationToken);

        var result = MaintenanceTicketResultMapper.ToResult(ticket);
        return ApiResult<MaintenanceTicketResult>.Success(result, "Maintenance ticket updated successfully.");
    }
}
