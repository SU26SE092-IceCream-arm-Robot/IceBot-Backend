namespace Application.EdgeIntegration.CommandDelivery.Contracts;

public static class EdgeCommandRejectionCodes
{
    public const string LocalPersistenceUnavailable = "LocalPersistenceUnavailable";
    public const string LocalDatabaseCorrupt = "LocalDatabaseCorrupt";
    public const string InsufficientLocalStorage = "InsufficientLocalStorage";
    public const string EventBacklogLimitExceeded = "EventBacklogLimitExceeded";
}

