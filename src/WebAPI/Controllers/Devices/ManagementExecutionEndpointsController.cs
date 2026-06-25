using Application.Devices.Commands;
using Asp.Versioning;
using Domain.Devices.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Devices;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/execution-endpoints")]
public sealed class ManagementExecutionEndpointsController : ControllerBase
{
    private readonly RotateExecutionEndpointCredentialCommandHandler _rotateCredentialHandler;

    public ManagementExecutionEndpointsController(RotateExecutionEndpointCredentialCommandHandler rotateCredentialHandler)
    {
        _rotateCredentialHandler = rotateCredentialHandler;
    }

    [HttpPatch("{endpointId:guid}/credential")]
    [Authorize(Policy = "devices.manage")]
    public async Task<IActionResult> RotateCredential(
        Guid endpointId,
        [FromBody] RotateExecutionEndpointCredentialRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RotateExecutionEndpointCredentialCommand
        {
            UserContext = User.GetUserContext(),
            EndpointId = endpointId,
            CredentialReference = request.CredentialReference,
            AuthenticationMode = request.AuthenticationMode
        };

        var result = await _rotateCredentialHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}

public sealed class RotateExecutionEndpointCredentialRequest
{
    [Required]
    [StringLength(300)]
    public string CredentialReference { get; init; } = string.Empty;

    public ExecutionEndpointAuthenticationMode? AuthenticationMode { get; init; }
}
