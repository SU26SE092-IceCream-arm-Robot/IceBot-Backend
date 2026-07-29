namespace Application.EdgeIntegration.Reports.Contracts;

public static class ExecutionPersistenceFailureCodes
{
    public const string LocalPersistenceLost = "LocalPersistenceLost";

    public static bool IsPersistenceFailureCode(string? errorCode) =>
        string.Equals(errorCode, LocalPersistenceLost, StringComparison.Ordinal);
}

