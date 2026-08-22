using Application.Tenants.Organizations.Queries;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Tenants;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/organizations/sales-summaries")]
[Authorize(Policy = "platform.organization-sales.view")]
public sealed class ManagementOrganizationSalesSummariesController : ControllerBase
{
    private readonly ListOrganizationSalesSummariesQueryHandler _listSalesSummaries;
    private readonly ILogger<ManagementOrganizationSalesSummariesController> _logger;

    public ManagementOrganizationSalesSummariesController(ListOrganizationSalesSummariesQueryHandler listSalesSummaries,
        ILogger<ManagementOrganizationSalesSummariesController> logger)
    {
        _listSalesSummaries = listSalesSummaries;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
        [FromQuery] Guid? organizationId, [FromQuery] string? search, [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var userContext = User.GetUserContext();
        var result = await _listSalesSummaries.HandleAsync(new ListOrganizationSalesSummariesQuery
        {
            UserContext = userContext,
            From = from,
            To = to,
            OrganizationId = organizationId,
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);

        _logger.LogInformation(
            "Organization sales summary accessed. ActorId={ActorId}, From={From}, To={To}, OrganizationId={OrganizationId}, SearchProvided={SearchProvided}, ResultCount={ResultCount}, TraceId={TraceId}",
            userContext.AccountId, from, to, organizationId, !string.IsNullOrWhiteSpace(search),
            result.Pagination.TotalCount, HttpContext.TraceIdentifier);
        return StatusCode(result.StatusCode, result);
    }
}
