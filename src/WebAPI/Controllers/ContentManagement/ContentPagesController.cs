using Application.ContentManagement;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.ContentManagement;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/content-pages")]
public sealed class ContentPagesController(ContentPageService service) : ControllerBase
{
    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublished(string slug, CancellationToken cancellationToken)
    {
        var page = await service.GetPublishedAsync(slug, cancellationToken);
        if (page is null) return NotFound();
        Response.Headers.ETag = page.ETag;
        Response.Headers.CacheControl = "public, max-age=300";
        return Ok(page);
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/content-pages")]
[Authorize(Policy = "content-pages.read")]
public sealed class ManagementContentPagesController(ContentPageService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await service.ListAsync(User.GetUserContext(), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(User.GetUserContext(), key, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
    [HttpPut("{key}/draft")]
    [Authorize(Policy = "content-pages.manage")]
    public async Task<IActionResult> UpdateDraft(string key, [FromBody] UpdateContentPageDraftRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateDraftAsync(User.GetUserContext(), key, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
    [HttpPost("{key}/publish")]
    [Authorize(Policy = "content-pages.manage")]
    public async Task<IActionResult> Publish(string key, [FromBody] PublishContentPageRequest request, CancellationToken cancellationToken)
    {
        var result = await service.PublishAsync(User.GetUserContext(), key, request.ExpectedRevision, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
