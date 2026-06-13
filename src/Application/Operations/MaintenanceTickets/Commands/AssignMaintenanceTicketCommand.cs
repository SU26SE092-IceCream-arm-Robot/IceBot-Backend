using Application.Identity.Tokens.Claims;
using Application.Operations.Abstractions;
using Application.Operations.MaintenanceTickets.Mapping;
using Application.Operations.MaintenanceTickets.Requests;
using Application.Operations.MaintenanceTickets.Results;
using Application.Operations.MaintenanceTickets.Rules;
using Application.Shared.Wrappers;
using Domain.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

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

    public AssignMaintenanceTicketCommandHandler(IMaintenanceTicketStore ticketStore)
    {
        _ticketStore = ticketStore;
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

        // 2. Authorization Access Check
        if (!MaintenanceTicketAccessRules.CanAssign(user, ticket.OrganizationId, ticket.StoreId, ticket.KioskId))
        {
            return ApiResult<MaintenanceTicketResult>.Fail("Access denied.", 403);
        }

        // 3. Assign
        try
        {
            ticket.Assign(req.AssignedToAccountId);
            ticket.UpdatedAt = DateTimeOffset.UtcNow;
            await _ticketStore.SaveChangesAsync(cancellationToken);
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<MaintenanceTicketResult>.Fail(ex.Message, 400);
        }

        var result = MaintenanceTicketResultMapper.ToResult(ticket);
        return ApiResult<MaintenanceTicketResult>.Success(result, "Maintenance ticket assigned successfully.");
    }
}
