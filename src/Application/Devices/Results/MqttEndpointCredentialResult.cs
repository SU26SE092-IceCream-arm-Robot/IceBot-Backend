namespace Application.Devices.Results;

public sealed class MqttEndpointCredentialResult
{
    public Guid ExecutionEndpointId { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
    public required string ClientId { get; init; }
    public required string SubscribeTopic { get; init; }
    public int CredentialVersion { get; init; }
    public required string Status { get; init; }
}
