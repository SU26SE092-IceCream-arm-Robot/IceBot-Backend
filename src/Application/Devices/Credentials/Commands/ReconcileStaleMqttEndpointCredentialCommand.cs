namespace Application.Devices.Credentials.Commands;

public sealed record ReconcileStaleMqttEndpointCredentialCommand(
    Guid EndpointId,
    DateTimeOffset ObservedAt);

public enum MqttCredentialReconciliationOutcome
{
    NotFound = 0,
    NoLongerStale = 1,
    ProvisioningMarkedFailed = 2,
    RotationMarkedFailed = 3,
    Revoked = 4,
    RevokeRetryFailed = 5,
    Superseded = 6
}
