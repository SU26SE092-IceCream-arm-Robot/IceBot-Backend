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

namespace Application.Operations.MaintenanceTickets.Commands;

public sealed class CloseMaintenanceTicketCommand
{
    public required Guid TicketId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
}

public sealed class CloseMaintenanceTicketCommandHandler
{
    private readonly IMaintenanceTicketStore _ticketStore;

    public CloseMaintenanceTicketCommandHandler(IMaintenanceTicketStore ticketStore)
    {
        _ticketStore = ticketStore;
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
        return ApiResult<MaintenanceTicketResult>.Success(result, "Maintenance ticket closed successfully.");
    }
}
