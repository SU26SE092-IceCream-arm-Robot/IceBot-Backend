namespace Domain.Orders.Enums
{
    public enum OrderStatus
    {
        Draft = 1,
        PendingPayment = 2,
        Paid = 3,
        ReadyForFulfillment = 4,
        Accepted = 5,
        Preparing = 6,
        Ready = 7,
        Completed = 8,
        Cancelled = 9,
        Failed = 10,
        ExecutionRejected = 11,
        RefundRequired = 12,
        Refunded = 13,
        Compensated = 14,
        FulfillmentIssue = 15
    }
}
