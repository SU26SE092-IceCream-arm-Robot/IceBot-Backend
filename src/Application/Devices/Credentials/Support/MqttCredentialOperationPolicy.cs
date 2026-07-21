namespace Application.Devices.Credentials.Support;

public static class MqttCredentialOperationPolicy
{
    public static readonly TimeSpan PendingOperationLease = TimeSpan.FromMinutes(5);
}
