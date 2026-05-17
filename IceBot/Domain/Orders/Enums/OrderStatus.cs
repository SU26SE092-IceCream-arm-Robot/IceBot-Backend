namespace Domain.Orders.Enums
{
    public enum OrderStatus
    {
        Draft = 1,
        PendingPayment = 2,
        Paid = 3,
        Accepted = 4,
        Preparing = 5,
        Ready = 6,
        Completed = 7,
        Cancelled = 8,
        Failed = 9
    }
}
