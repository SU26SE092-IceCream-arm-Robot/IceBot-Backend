using Application.Devices.Connectivity.Contracts;
using Domain.Devices.ExecutionEndpoints;

namespace Application.Devices.Connectivity.Rules;

public static class LocalPersistenceReadinessRules
{
    public const string StorageNotWritable = "LocalStorageNotWritable";
    public const string InsufficientStorage = "InsufficientLocalStorage";
    public const string DatabaseUnhealthy = "LocalDatabaseUnhealthy";
    public const string EventBacklogLimitExceeded = "EventBacklogLimitExceeded";

    public static string? Validate(LocalPersistenceHealthInput health)
    {
        if (!Enum.IsDefined(health.LocalDatabaseHealth))
            return "Local database health is invalid.";
        if (health.FreeSpaceBytes < 0 || health.MinimumRequiredFreeSpaceBytes <= 0)
            return "Local free-space values must be non-negative and the required minimum must be positive.";
        if (health.PendingEventCount < 0 || health.MaximumPendingEventCount <= 0)
            return "Local event backlog values must be non-negative and the maximum must be positive.";
        return null;
    }

    public static LocalPersistenceReadinessDecision Apply(
        LocalPersistenceHealthInput health,
        ExecutionReadinessState requestedReadiness,
        string? requestedFaultCode)
    {
        var persistenceFaultCode = ResolveFaultCode(health);
        return persistenceFaultCode is null
            ? new LocalPersistenceReadinessDecision(requestedReadiness, Normalize(requestedFaultCode))
            : new LocalPersistenceReadinessDecision(ExecutionReadinessState.NotReady, persistenceFaultCode);
    }

    private static string? ResolveFaultCode(LocalPersistenceHealthInput health)
    {
        if (!health.StorageWritable) return StorageNotWritable;
        if (health.LocalDatabaseHealth != LocalDatabaseHealth.Healthy) return DatabaseUnhealthy;
        if (health.FreeSpaceBytes < health.MinimumRequiredFreeSpaceBytes) return InsufficientStorage;
        if (health.PendingEventCount > health.MaximumPendingEventCount) return EventBacklogLimitExceeded;
        return null;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record LocalPersistenceReadinessDecision(
    ExecutionReadinessState Readiness,
    string? FaultCode);
