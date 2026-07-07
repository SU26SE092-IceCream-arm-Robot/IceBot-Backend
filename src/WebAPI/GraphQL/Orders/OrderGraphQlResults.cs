using Application.Orders.Management.Results;

namespace WebAPI.GraphQL.Orders;

public sealed record OrderPageInfo(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasNext,
    bool HasPrevious);

public sealed record ManagementOrdersPage(
    IReadOnlyCollection<ManagementOrderListItemResult> Items,
    OrderPageInfo PageInfo);

public sealed record OrderStatusHistoryPage(
    IReadOnlyCollection<OrderStatusHistoryResult> Items,
    OrderPageInfo PageInfo);

public sealed record OrderExecutionAttemptsPage(
    IReadOnlyCollection<ExecutionAttemptResult> Items,
    OrderPageInfo PageInfo);
