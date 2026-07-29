namespace Application.EdgeIntegration.Reports.Contracts;

public static class ExecutionInterruptionCodes
{
    public const string RuntimeRestarted = "RuntimeRestarted";
    public const string ControllerRestarted = "ControllerRestarted";
    public const string PowerInterrupted = "PowerInterrupted";

    public static bool IsRestartRecoveryCode(string? errorCode) =>
        string.Equals(errorCode, RuntimeRestarted, StringComparison.Ordinal) ||
        string.Equals(errorCode, ControllerRestarted, StringComparison.Ordinal) ||
        string.Equals(errorCode, PowerInterrupted, StringComparison.Ordinal);
}
