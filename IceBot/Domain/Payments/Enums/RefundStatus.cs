namespace Domain.Payments.Enums
{
    public enum RefundStatus
    {
        Requested = 1,
        Processing = 2,
        Processed = 3,
        Rejected = 4,
        Failed = 5,
        Cancelled = 6
    }
}