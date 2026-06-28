using Application.Devices.Commands;
using Application.Devices.Queries;
using Application.Devices.Requests;
using Asp.Versioning;
using Domain.Devices.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Devices;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management")]
public sealed class ManagementExecutionEndpointsController : ControllerBase
{
    private readonly ListExecutionEndpointsQueryHandler _listHandler;
    private readonly GetExecutionEndpointQueryHandler _getHandler;
    private readonly CreateExecutionEndpointCommandHandler _createHandler;
    private readonly ReplaceExecutionEndpointRobotTargetsCommandHandler _replaceTargetsHandler;
    private readonly ProvisionExecutionEndpointCommandHandler _provisionHandler;
    private readonly DisableExecutionEndpointCommandHandler _disableHandler;
    private readonly ReactivateExecutionEndpointCommandHandler _reactivateHandler;
    private readonly RetireExecutionEndpointCommandHandler _retireHandler;
    private readonly RotateExecutionEndpointCredentialCommandHandler _rotateCredentialHandler;

    public ManagementExecutionEndpointsController(
        ListExecutionEndpointsQueryHandler listHandler,
        GetExecutionEndpointQueryHandler getHandler,
        CreateExecutionEndpointCommandHandler createHandler,
        ReplaceExecutionEndpointRobotTargetsCommandHandler replaceTargetsHandler,
        ProvisionExecutionEndpointCommandHandler provisionHandler,
        DisableExecutionEndpointCommandHandler disableHandler,
        ReactivateExecutionEndpointCommandHandler reactivateHandler,
        RetireExecutionEndpointCommandHandler retireHandler,
        RotateExecutionEndpointCredentialCommandHandler rotateCredentialHandler)
    {
        _listHandler = listHandler;
        _getHandler = getHandler;
        _createHandler = createHandler;
        _replaceTargetsHandler = replaceTargetsHandler;
        _provisionHandler = provisionHandler;
        _disableHandler = disableHandler;
        _reactivateHandler = reactivateHandler;
        _retireHandler = retireHandler;
        _rotateCredentialHandler = rotateCredentialHandler;
    }

    [HttpGet("execution-endpoints")]
    [Authorize(Policy = "devices.view")]
    public async Task<IActionResult> List(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? storeId,
        [FromQuery] Guid? kioskId,
        [FromQuery] string? profile,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await _listHandler.HandleAsync(new ListExecutionEndpointsQuery
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            StoreId = storeId,
            KioskId = kioskId,
            Profile = profile,
            Status = status
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("execution-endpoints/{endpointId:guid}")]
    [Authorize(Policy = "devices.view")]
    public async Task<IActionResult> Get(Guid endpointId, CancellationToken cancellationToken)
    {
        var result = await _getHandler.HandleAsync(new GetExecutionEndpointQuery
        {
            UserContext = User.GetUserContext(),
            EndpointId = endpointId
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("kiosks/{kioskId:guid}/execution-endpoints")]
    [Authorize(Policy = "devices.manage")]
    public async Task<IActionResult> Create(
        Guid kioskId, [FromBody] CreateExecutionEndpointRequest request, CancellationToken cancellationToken)
    {
        var result = await _createHandler.HandleAsync(new CreateExecutionEndpointCommand
        {
            UserContext = User.GetUserContext(), KioskId = kioskId, Request = request
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("execution-endpoints/{endpointId:guid}/supported-robot-targets")]
    [Authorize(Policy = "devices.manage")]
    public async Task<IActionResult> ReplaceSupportedRobotTargets(
        Guid endpointId, [FromBody] ReplaceExecutionEndpointRobotTargetsRequest request, CancellationToken cancellationToken)
    {
        var result = await _replaceTargetsHandler.HandleAsync(new ReplaceExecutionEndpointRobotTargetsCommand
        {
            UserContext = User.GetUserContext(), EndpointId = endpointId, Request = request
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("execution-endpoints/{endpointId:guid}/provision")]
    [Authorize(Policy = "devices.manage")]
    public async Task<IActionResult> Provision(
        Guid endpointId, [FromBody] ProvisionExecutionEndpointRequest request, CancellationToken cancellationToken)
    {
        var result = await _provisionHandler.HandleAsync(new ProvisionExecutionEndpointCommand
        {
            UserContext = User.GetUserContext(), EndpointId = endpointId, Request = request
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("execution-endpoints/{endpointId:guid}/disable")]
    [Authorize(Policy = "devices.manage")]
    public async Task<IActionResult> Disable(Guid endpointId, CancellationToken cancellationToken)
    {
        var result = await _disableHandler.HandleAsync(new DisableExecutionEndpointCommand
        {
            UserContext = User.GetUserContext(), EndpointId = endpointId
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("execution-endpoints/{endpointId:guid}/reactivate")]
    [Authorize(Policy = "devices.manage")]
    public async Task<IActionResult> Reactivate(Guid endpointId, CancellationToken cancellationToken)
    {
        var result = await _reactivateHandler.HandleAsync(new ReactivateExecutionEndpointCommand
        {
            UserContext = User.GetUserContext(), EndpointId = endpointId
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("execution-endpoints/{endpointId:guid}/retire")]
    [Authorize(Policy = "devices.manage")]
    public async Task<IActionResult> Retire(Guid endpointId, CancellationToken cancellationToken)
    {
        var result = await _retireHandler.HandleAsync(new RetireExecutionEndpointCommand
        {
            UserContext = User.GetUserContext(), EndpointId = endpointId
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("execution-endpoints/{endpointId:guid}/credential")]
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
            ClientCertificateSha256Fingerprint = request.ClientCertificateSha256Fingerprint,
            EcdsaPublicKeyPem = request.EcdsaPublicKeyPem,
            AuthenticationMode = request.AuthenticationMode
        };

        var result = await _rotateCredentialHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}

public sealed class RotateExecutionEndpointCredentialRequest
{
    [StringLength(128)]
    public string? ClientCertificateSha256Fingerprint { get; init; }

    public string? EcdsaPublicKeyPem { get; init; }

    public ExecutionEndpointAuthenticationMode? AuthenticationMode { get; init; }
}
