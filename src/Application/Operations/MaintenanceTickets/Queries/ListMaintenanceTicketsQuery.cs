using Application.Identity.Tokens.Claims;
using Application.Operations.Abstractions;
using Application.Operations.MaintenanceTickets.Mapping;
using Application.Operations.MaintenanceTickets.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Operations.Enums;

namespace Application.Operations.MaintenanceTickets.Queries;

public sealed class ListMaintenanceTicketsQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public string? Status { get; init; }
    public string? Priority { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
    public Guid? AssignedToAccountId { get; init; }
    public Guid? CreatedByAccountId { get; init; }
    public DateTimeOffset? FromDate { get; init; }
    public DateTimeOffset? ToDate { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public sealed class ListMaintenanceTicketsQueryHandler
{
    private readonly IMaintenanceTicketStore _ticketStore;

    public ListMaintenanceTicketsQueryHandler(IMaintenanceTicketStore ticketStore)
    {
        _ticketStore = ticketStore;
    }

    public async Task<PagedResult<MaintenanceTicketResult>> HandleAsync(
        ListMaintenanceTicketsQuery query,
        CancellationToken cancellationToken = default)
    {
        var user = query.UserContext;
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        if (query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate > query.ToDate)
            return PagedResult<MaintenanceTicketResult>.Fail(
                "Maintenance-ticket from timestamp cannot be after to timestamp.", 400, pageNumber, pageSize);

        // Parse Status
        MaintenanceTicketStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<MaintenanceTicketStatus>(query.Status.Trim(), ignoreCase: true, out var statusVal) ||
                !Enum.IsDefined(statusVal))
            {
                return PagedResult<MaintenanceTicketResult>.Fail("Invalid maintenance ticket status.", 400, pageNumber, pageSize);
            }
            parsedStatus = statusVal;
        }

        // Parse Priority
        MaintenancePriority? parsedPriority = null;
        if (!string.IsNullOrWhiteSpace(query.Priority))
        {
            if (!Enum.TryParse<MaintenancePriority>(query.Priority.Trim(), ignoreCase: true, out var priorityVal) ||
                !Enum.IsDefined(priorityVal))
            {
                return PagedResult<MaintenanceTicketResult>.Fail("Invalid maintenance ticket priority.", 400, pageNumber, pageSize);
            }
            parsedPriority = priorityVal;
        }

        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.MaintenanceView, user);

        // Query total count
        var totalCount = await _ticketStore.CountAsync(
            parsedStatus,
            parsedPriority,
            query.OrganizationId,
            query.StoreId,
            query.KioskId,
            query.AssignedToAccountId,
            query.CreatedByAccountId,
            query.FromDate,
            query.ToDate,
            user.IsSystemAdmin,
            scope.OrganizationIds,
            scope.StoreIds,
            scope.KioskIds,
            cancellationToken);

        // Query items
        var items = await _ticketStore.ListAsync(
            parsedStatus,
            parsedPriority,
            query.OrganizationId,
            query.StoreId,
            query.KioskId,
            query.AssignedToAccountId,
            query.CreatedByAccountId,
            query.FromDate,
            query.ToDate,
            user.IsSystemAdmin,
            scope.OrganizationIds,
            scope.StoreIds,
            scope.KioskIds,
            pageNumber,
            pageSize,
            cancellationToken);

        var results = items.Select(MaintenanceTicketResultMapper.ToResult).ToList();
        return PagedResult<MaintenanceTicketResult>.Success(results, totalCount, pageNumber, pageSize);
    }
}
