namespace Domain.Sync.Enums
{
    public enum SyncEventStatus
    {
        Received = 1,
        Processing = 2,
        Processed = 3,
        Failed = 4,
        DeadLettered = 5,
        Ignored = 6
    }
}
