namespace Application.Devices.Connectivity.Contracts;

public enum LocalDatabaseHealth
{
    Unknown = 0,
    Healthy = 1,
    Degraded = 2,
    Corrupt = 3,
    Unavailable = 4
}

public sealed record LocalPersistenceHealthInput(
    bool StorageWritable,
    long FreeSpaceBytes,
    long MinimumRequiredFreeSpaceBytes,
    LocalDatabaseHealth LocalDatabaseHealth,
    int PendingEventCount,
    int MaximumPendingEventCount);

