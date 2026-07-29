using Application.Identity.Tokens.Claims;

namespace Application.Devices.Credentials.Commands;

public sealed class ProvisionMqttEndpointCredentialCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid KioskId { get; init; }
    public required Guid EndpointId { get; init; }
}

public sealed class RotateMqttEndpointCredentialCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid KioskId { get; init; }
    public required Guid EndpointId { get; init; }
}

public sealed class RevokeMqttEndpointCredentialCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid KioskId { get; init; }
    public required Guid EndpointId { get; init; }
}
