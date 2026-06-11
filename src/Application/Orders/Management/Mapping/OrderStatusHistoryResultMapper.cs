using Application.Orders.Management.Results;
using Domain.Orders.Entities;

namespace Application.Orders.Management.Mapping;

internal static class OrderStatusHistoryResultMapper
{
    public static OrderStatusHistoryResult ToResult(OrderStatusHistory history)
    {
        return new OrderStatusHistoryResult
        {
            Id = history.Id,
            OrderId = history.OrderId,
            ChangedByAccountId = history.ChangedByAccountId,
            ChangedByName = history.ChangedByAccount?.FullName ?? history.ChangedByAccount?.UserName,
            ChangedByEmail = history.ChangedByAccount?.Email,
            FromStatus = history.FromStatus,
            ToStatus = history.ToStatus,
            Reason = history.Reason,
            ChangedAt = history.ChangedAt
        };
    }
}
