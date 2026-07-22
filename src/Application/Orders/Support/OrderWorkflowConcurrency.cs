namespace Application.Orders.Support;

public static class OrderWorkflowConcurrency
{
    public static string OrderLockKey(Guid orderId) => $"order-workflow:{orderId:D}";
}
