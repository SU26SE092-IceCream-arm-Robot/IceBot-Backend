using System.Security.Claims;
using Application.Catalog.ProductCategories.Commands;
using Application.Catalog.ProductCategories.Queries;
using Application.Catalog.ProductCategories.Requests;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers.Catalog;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/product-categories")]
[Authorize]
public sealed class ManagementProductCategoriesController(
    ListProductCategoriesQueryHandler listHandler,
    CreateProductCategoryCommandHandler createHandler,
    UpdateProductCategoryCommandHandler updateHandler,
    SetProductCategoryStatusCommandHandler setStatusHandler,
    DeleteProductCategoryCommandHandler deleteHandler) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "product-categories.read")]
    public async Task<IActionResult> List(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await listHandler.HandleAsync(new ListProductCategoriesQuery
        {
            IncludeInactive = includeInactive
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    [Authorize(Policy = "product-categories.manage")]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createHandler.HandleAsync(
            new CreateProductCategoryCommand(request, CurrentAccountId()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{categoryId:long}")]
    [Authorize(Policy = "product-categories.manage")]
    public async Task<IActionResult> Update(
        long categoryId,
        [FromBody] UpdateProductCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await updateHandler.HandleAsync(
            new UpdateProductCategoryCommand(categoryId, request, CurrentAccountId()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{categoryId:long}/status")]
    [Authorize(Policy = "product-categories.manage")]
    public async Task<IActionResult> SetStatus(
        long categoryId,
        [FromBody] SetProductCategoryStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await setStatusHandler.HandleAsync(
            new SetProductCategoryStatusCommand(categoryId, request.IsActive, CurrentAccountId()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{categoryId:long}")]
    [Authorize(Policy = "product-categories.manage")]
    public async Task<IActionResult> Delete(long categoryId, CancellationToken cancellationToken)
    {
        var result = await deleteHandler.HandleAsync(new DeleteProductCategoryCommand(categoryId), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    private Guid? CurrentAccountId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var accountId) ? accountId : null;
}
