using Application.Identity.Tokens.Claims;

namespace Application.Devices.Credentials.Commands;

public sealed class RotateExecutionEndpointCredentialCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid KioskId { get; init; }
    public required Guid EndpointId { get; init; }
    public string? ClientCertificateSha256Fingerprint { get; init; }
    public string? EcdsaPublicKeyPem { get; init; }
}
