using System.Security.Claims;
using Application.Catalog.Products.Commands;
using Application.Catalog.Recipes.Commands;
using Application.Catalog.Recipes.Queries;
using Application.Catalog.Recipes.Requests;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Catalog;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/organizations/{organizationId:guid}/products/{productId:guid}/variants/{variantId:guid}/recipes")]
[Authorize(Policy = "products.manage")]
public sealed class ManagementRecipesController(
    ListRecipesQueryHandler listHandler,
    GetRecipeQueryHandler getHandler,
    CreateRecipeCommandHandler createHandler,
    UpdateRecipeCommandHandler updateHandler,
    ReplaceRecipeItemsCommandHandler replaceItemsHandler,
    SetRecipeStatusCommandHandler setStatusHandler,
    CreateRecipeVersionCommandHandler createVersionHandler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid organizationId, Guid productId, Guid variantId,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await listHandler.HandleAsync(
            new ListRecipesQuery(Scope(organizationId), productId, variantId, pageNumber, pageSize), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{recipeId:guid}")]
    public async Task<IActionResult> Get(Guid organizationId, Guid productId, Guid variantId, Guid recipeId,
        CancellationToken cancellationToken)
    {
        var result = await getHandler.HandleAsync(
            new GetRecipeQuery(Scope(organizationId), productId, variantId, recipeId), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid organizationId, Guid productId, Guid variantId,
        [FromBody] CreateRecipeRequest request, CancellationToken cancellationToken)
    {
        var result = await createHandler.HandleAsync(
            new CreateRecipeCommand(Scope(organizationId), productId, variantId, request, CurrentAccountId()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{recipeId:guid}")]
    public async Task<IActionResult> Update(Guid organizationId, Guid productId, Guid variantId, Guid recipeId,
        [FromBody] UpdateRecipeRequest request, CancellationToken cancellationToken)
    {
        var result = await updateHandler.HandleAsync(
            new UpdateRecipeCommand(Scope(organizationId), productId, variantId, recipeId, request, CurrentAccountId()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{recipeId:guid}/items")]
    public async Task<IActionResult> ReplaceItems(Guid organizationId, Guid productId, Guid variantId, Guid recipeId,
        [FromBody] ReplaceRecipeItemsRequest request, CancellationToken cancellationToken)
    {
        var result = await replaceItemsHandler.HandleAsync(
            new ReplaceRecipeItemsCommand(Scope(organizationId), productId, variantId, recipeId, request, CurrentAccountId()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{recipeId:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid organizationId, Guid productId, Guid variantId, Guid recipeId,
        [FromBody] SetRecipeStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await setStatusHandler.HandleAsync(
            new SetRecipeStatusCommand(Scope(organizationId), productId, variantId, recipeId, request.Status, CurrentAccountId()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{recipeId:guid}/versions")]
    public async Task<IActionResult> CreateVersion(Guid organizationId, Guid productId, Guid variantId, Guid recipeId,
        CancellationToken cancellationToken)
    {
        var result = await createVersionHandler.HandleAsync(
            new CreateRecipeVersionCommand(Scope(organizationId), productId, variantId, recipeId, CurrentAccountId()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    private ProductManagementCommandScope Scope(Guid organizationId) => new(User.GetUserContext(), organizationId);
    private Guid? CurrentAccountId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
