using Application.ServiceRegistration;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebAPI.Authorization;

namespace WebAPI.Controllers.ServiceRegistration;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/service-registrations")]
public sealed class ServiceRegistrationsController(ServiceRegistrationService service) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("service-registration-submission")]
    public async Task<IActionResult> Submit(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] SubmitServiceRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.SubmitAsync(idempotencyKey, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/service-registrations")]
[Authorize(Policy = "service-registrations.read")]
public sealed class ManagementServiceRegistrationsController(ServiceRegistrationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status, [FromQuery] string? search,
        [FromQuery] DateTimeOffset? createdFrom, [FromQuery] DateTimeOffset? createdTo,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await service.ListAsync(new ServiceRegistrationManagementQuery
        {
            UserContext = User.GetUserContext(),
            Status = status,
            Search = search,
            CreatedFrom = createdFrom,
            CreatedTo = createdTo,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{registrationId:guid}")]
    public async Task<IActionResult> Get(Guid registrationId, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(User.GetUserContext(), registrationId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{registrationId:guid}/start-review")]
    [Authorize(Policy = "service-registrations.manage")]
    public async Task<IActionResult> StartReview(Guid registrationId, [FromBody] ChangeServiceRegistrationStateRequest request, CancellationToken cancellationToken)
    {
        var result = await service.StartReviewAsync(User.GetUserContext(), registrationId, request.ExpectedRevision, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{registrationId:guid}/reject")]
    [Authorize(Policy = "service-registrations.manage")]
    public async Task<IActionResult> Reject(Guid registrationId, [FromBody] ChangeServiceRegistrationStateRequest request, CancellationToken cancellationToken)
    {
        var result = await service.RejectAsync(User.GetUserContext(), registrationId, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{registrationId:guid}/approve")]
    [Authorize(Policy = "service-registrations.manage")]
    public async Task<IActionResult> Approve(Guid registrationId, [FromBody] ServiceRegistrationProvisioningRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ApproveAsync(User.GetUserContext(), registrationId, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{registrationId:guid}/retry-provisioning")]
    [Authorize(Policy = "service-registrations.manage")]
    public async Task<IActionResult> Retry(Guid registrationId, [FromBody] ChangeServiceRegistrationStateRequest request, CancellationToken cancellationToken)
    {
        var result = await service.RetryProvisioningAsync(User.GetUserContext(), registrationId, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
