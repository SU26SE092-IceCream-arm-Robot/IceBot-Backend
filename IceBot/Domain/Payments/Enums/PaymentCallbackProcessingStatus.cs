namespace Domain.Payments.Enums
{
    public enum PaymentCallbackProcessingStatus
    {
        Received = 1,
        Processing = 2,
        Processed = 3,
        Failed = 4,
        Ignored = 5
    }
}
