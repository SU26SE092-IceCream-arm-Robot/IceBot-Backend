namespace Application.Devices.Telemetry.Rules;

public static class DeviceEventAutomationRules
{
    public static bool IsEligibleForAlertAutomation(
        DateTimeOffset occurredAt,
        DateTimeOffset cloudReceivedAt,
        int maximumEventAgeMinutes) =>
        occurredAt >= cloudReceivedAt.AddMinutes(-maximumEventAgeMinutes);
}
