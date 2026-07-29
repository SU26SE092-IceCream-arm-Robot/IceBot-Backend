using System.Security.Claims;
using Application.Catalog.Ingredients.Commands;
using Application.Catalog.Ingredients.Queries;
using Application.Catalog.Ingredients.Requests;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Catalog;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/ingredients")]
[Authorize]
public sealed class ManagementIngredientsController(
    ListIngredientsQueryHandler listHandler,
    GetIngredientQueryHandler getHandler,
    CreateIngredientCommandHandler createHandler,
    UpdateIngredientCommandHandler updateHandler,
    SetIngredientStatusCommandHandler setStatusHandler,
    DeleteIngredientCommandHandler deleteHandler) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "ingredients.read")]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await listHandler.HandleAsync(new ListIngredientsQuery
        {
            UserContext = User.GetUserContext(),
            Search = search,
            IsActive = isActive,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{ingredientId:guid}/status")]
    [Authorize(Policy = "ingredients.manage")]
    public async Task<IActionResult> SetStatus(
        Guid ingredientId,
        [FromBody] SetIngredientStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await setStatusHandler.HandleAsync(
            new SetIngredientStatusCommand(ingredientId, request.IsActive, CurrentAccountId()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{ingredientId:guid}")]
    [Authorize(Policy = "ingredients.manage")]
    public async Task<IActionResult> Delete(Guid ingredientId, CancellationToken cancellationToken)
    {
        var result = await deleteHandler.HandleAsync(new DeleteIngredientCommand(ingredientId), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{ingredientId:guid}")]
    [Authorize(Policy = "ingredients.read")]
    public async Task<IActionResult> Get(Guid ingredientId, CancellationToken cancellationToken)
    {
        var result = await getHandler.HandleAsync(new GetIngredientQuery(ingredientId, User.GetUserContext()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    [Authorize(Policy = "ingredients.manage")]
    public async Task<IActionResult> Create([FromBody] CreateIngredientRequest request, CancellationToken cancellationToken)
    {
        var result = await createHandler.HandleAsync(new CreateIngredientCommand(request, CurrentAccountId()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{ingredientId:guid}")]
    [Authorize(Policy = "ingredients.manage")]
    public async Task<IActionResult> Update(
        Guid ingredientId,
        [FromBody] UpdateIngredientRequest request,
        CancellationToken cancellationToken)
    {
        var result = await updateHandler.HandleAsync(
            new UpdateIngredientCommand(ingredientId, request, CurrentAccountId()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    private Guid? CurrentAccountId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var accountId) ? accountId : null;
}
