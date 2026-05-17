namespace Domain.Payments.Enums
{
    public enum PaymentTransactionStatus
    {
        Pending = 1,
        Authorized = 2,
        Paid = 3,
        Failed = 4,
        Cancelled = 5,
        Refunded = 6,
        Expired = 7
    }
}
