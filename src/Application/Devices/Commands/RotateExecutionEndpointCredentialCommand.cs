using Application.Identity.Tokens.Claims;
using Domain.Devices.Enums;

namespace Application.Devices.Commands;

public sealed class RotateExecutionEndpointCredentialCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid EndpointId { get; init; }
    public string? ClientCertificateSha256Fingerprint { get; init; }
    public string? EcdsaPublicKeyPem { get; init; }
    public ExecutionEndpointAuthenticationMode? AuthenticationMode { get; init; }
}
