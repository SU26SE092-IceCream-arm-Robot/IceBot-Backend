using Application.Identity.Tokens.Claims;
using Application.Operations.Abstractions;
using Application.Operations.MaintenanceTickets.Results;
using Application.Operations.MaintenanceTickets.Rules;
using Application.Shared.Wrappers;

namespace Application.Operations.MaintenanceTickets.Queries;

public sealed class ListMaintenanceTicketAssigneeOptionsQuery
{
    public required Guid TicketId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
}

public sealed class ListMaintenanceTicketAssigneeOptionsQueryHandler
{
    private readonly IMaintenanceTicketStore _ticketStore;

    public ListMaintenanceTicketAssigneeOptionsQueryHandler(IMaintenanceTicketStore ticketStore)
    {
        _ticketStore = ticketStore;
    }

    public async Task<ApiResult<List<MaintenanceAssigneeOptionResult>>> HandleAsync(
        ListMaintenanceTicketAssigneeOptionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketStore.GetByIdAsync(query.TicketId, cancellationToken);
        if (ticket is null)
        {
            return ApiResult<List<MaintenanceAssigneeOptionResult>>.Fail("Maintenance ticket not found.", 404);
        }

        if (!MaintenanceTicketAccessRules.CanAssign(
                query.UserContext,
                ticket.OrganizationId,
                ticket.StoreId,
                ticket.KioskId))
        {
            return ApiResult<List<MaintenanceAssigneeOptionResult>>.Fail("Access denied.", 403);
        }

        var result = await _ticketStore.ListAssignableAccountsAsync(
            ticket.OrganizationId,
            ticket.StoreId,
            ticket.KioskId,
            cancellationToken);

        return ApiResult<List<MaintenanceAssigneeOptionResult>>.Success(
            result,
            "Maintenance assignee options retrieved successfully.");
    }
}
