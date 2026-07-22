using System.ComponentModel.DataAnnotations;
using Application.Tenants.Onboarding;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Tenants;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/organizations/{organizationId:guid}/franchise-onboardings")]
[Authorize(Policy = "stores.manage")]
public sealed class ManagementFranchiseOnboardingsController(FranchiseOnboardingService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        Guid organizationId,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await service.ListAsync(
            User.GetUserContext(), organizationId, status, pageNumber, pageSize, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    public async Task<IActionResult> Start(
        Guid organizationId,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        [FromBody] StartFranchiseOnboardingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.StartAsync(new StartFranchiseOnboardingCommand
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            IdempotencyKey = idempotencyKey,
            Request = request
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{onboardingId:guid}")]
    public async Task<IActionResult> Get(
        Guid organizationId,
        Guid onboardingId,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(
            User.GetUserContext(), organizationId, onboardingId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{onboardingId:guid}/resume")]
    public async Task<IActionResult> Resume(
        Guid organizationId,
        Guid onboardingId,
        CancellationToken cancellationToken)
    {
        var result = await service.ResumeAsync(
            User.GetUserContext(), organizationId, onboardingId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{onboardingId:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid organizationId,
        Guid onboardingId,
        [FromBody] CancelFranchiseOnboardingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CancelAsync(
            User.GetUserContext(), organizationId, onboardingId, request.Reason, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}

public sealed class CancelFranchiseOnboardingRequest
{
    [Required, StringLength(500)] public string Reason { get; init; } = null!;
}
