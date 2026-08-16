using Domain.Devices.ExecutionEndpoints;
using System.ComponentModel.DataAnnotations;

namespace Application.Devices.ExecutionEndpoints.Requests;

public sealed class CreateExecutionEndpointRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string EndpointCode { get; init; } = null!;

    public KioskExecutionProfile ExecutionProfile { get; init; }
}

public sealed class ProvisionExecutionEndpointRequest
{
    public Guid ProfileIdentity { get; init; }

    [StringLength(128)]
    public string? ClientCertificateSha256Fingerprint { get; init; }

    public string? EcdsaPublicKeyPem { get; init; }
}
