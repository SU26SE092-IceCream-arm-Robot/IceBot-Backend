using Application.Identity.Tokens.Claims;
using Application.Operations.Abstractions;
using Application.Operations.MaintenanceTickets.Mapping;
using Application.Operations.MaintenanceTickets.Results;
using Application.Operations.MaintenanceTickets.Rules;
using Application.Shared.Wrappers;

namespace Application.Operations.MaintenanceTickets.Queries;

public sealed class GetMaintenanceTicketQuery
{
    public required Guid TicketId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
}

public sealed class GetMaintenanceTicketQueryHandler
{
    private readonly IMaintenanceTicketStore _ticketStore;

    public GetMaintenanceTicketQueryHandler(IMaintenanceTicketStore ticketStore)
    {
        _ticketStore = ticketStore;
    }

    public async Task<ApiResult<MaintenanceTicketResult>> HandleAsync(
        GetMaintenanceTicketQuery query,
        CancellationToken cancellationToken = default)
    {
        var user = query.UserContext;

        var ticket = await _ticketStore.GetByIdAsync(query.TicketId, cancellationToken);
        if (ticket is null)
        {
            return ApiResult<MaintenanceTicketResult>.Fail("Maintenance ticket not found.", 404);
        }

        if (!MaintenanceTicketAccessRules.CanView(user, ticket.OrganizationId, ticket.StoreId, ticket.KioskId, ticket.AssignedToAccountId))
        {
            return ApiResult<MaintenanceTicketResult>.Fail("Access denied.", 403);
        }

        var result = MaintenanceTicketResultMapper.ToResult(ticket);
        return ApiResult<MaintenanceTicketResult>.Success(result);
    }
}
