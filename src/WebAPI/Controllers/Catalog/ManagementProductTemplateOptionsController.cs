using Application.Catalog.Products.Commands;
using Application.Catalog.Products.Requests;
using Application.Identity.Tokens.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Catalog;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/product-templates/{productId:guid}/option-groups")]
[Authorize(Policy = "product-templates.manage")]
public sealed class ManagementProductTemplateOptionsController(
    CreateOptionGroupCommandHandler createGroup,
    UpdateOptionGroupCommandHandler updateGroup,
    SetOptionGroupStatusCommandHandler setGroupStatus,
    DeleteOptionGroupCommandHandler deleteGroup,
    CreateProductOptionCommandHandler createOption,
    UpdateProductOptionCommandHandler updateOption,
    SetProductOptionAvailabilityCommandHandler setOptionAvailability,
    DeleteProductOptionCommandHandler deleteOption,
    ReplaceProductOptionIngredientRequirementsCommandHandler replaceIngredientRequirements) : ControllerBase
{
    [HttpPost]
    public Task<IActionResult> CreateGroup(Guid productId, CreateOptionGroupRequest request, CancellationToken ct) =>
        Execute(createGroup.HandleAsync(new CreateOptionGroupCommand { Scope = Scope(), ProductId = productId, Request = request, CreatedByAccountId = CurrentAccountId() }, ct));

    [HttpPut("{optionGroupId:long}")]
    public Task<IActionResult> UpdateGroup(Guid productId, long optionGroupId, UpdateOptionGroupRequest request, CancellationToken ct) =>
        Execute(updateGroup.HandleAsync(new UpdateOptionGroupCommand { Scope = Scope(), ProductId = productId, OptionGroupId = optionGroupId, Request = request, UpdatedByAccountId = CurrentAccountId() }, ct));

    [HttpPatch("{optionGroupId:long}/status")]
    public Task<IActionResult> SetGroupStatus(Guid productId, long optionGroupId, SetOptionGroupStatusRequest request, CancellationToken ct) =>
        Execute(setGroupStatus.HandleAsync(new SetOptionGroupStatusCommand { Scope = Scope(), ProductId = productId, OptionGroupId = optionGroupId, IsActive = request.IsActive, UpdatedByAccountId = CurrentAccountId() }, ct));

    [HttpDelete("{optionGroupId:long}")]
    public Task<IActionResult> DeleteGroup(Guid productId, long optionGroupId, CancellationToken ct) =>
        Execute(deleteGroup.HandleAsync(new DeleteOptionGroupCommand { Scope = Scope(), ProductId = productId, OptionGroupId = optionGroupId }, ct));

    [HttpPost("{optionGroupId:long}/options")]
    public Task<IActionResult> CreateOption(Guid productId, long optionGroupId, CreateProductOptionRequest request, CancellationToken ct) =>
        Execute(createOption.HandleAsync(new CreateProductOptionCommand { Scope = Scope(), ProductId = productId, OptionGroupId = optionGroupId, Request = request, CreatedByAccountId = CurrentAccountId() }, ct));

    [HttpPut("{optionGroupId:long}/options/{productOptionId:guid}")]
    public Task<IActionResult> UpdateOption(Guid productId, long optionGroupId, Guid productOptionId, UpdateProductOptionRequest request, CancellationToken ct) =>
        Execute(updateOption.HandleAsync(new UpdateProductOptionCommand { Scope = Scope(), ProductId = productId, OptionGroupId = optionGroupId, ProductOptionId = productOptionId, Request = request, UpdatedByAccountId = CurrentAccountId() }, ct));

    [HttpPut("{optionGroupId:long}/options/{productOptionId:guid}/ingredient-requirements")]
    public Task<IActionResult> ReplaceIngredientRequirements(
        Guid productId, long optionGroupId, Guid productOptionId,
        ReplaceProductOptionIngredientRequirementsRequest request, CancellationToken ct) =>
        Execute(replaceIngredientRequirements.HandleAsync(new ReplaceProductOptionIngredientRequirementsCommand
        {
            Scope = Scope(), ProductId = productId, OptionGroupId = optionGroupId,
            ProductOptionId = productOptionId, Request = request, UpdatedByAccountId = CurrentAccountId()
        }, ct));

    [HttpPatch("{optionGroupId:long}/options/{productOptionId:guid}/availability")]
    public Task<IActionResult> SetOptionAvailability(Guid productId, long optionGroupId, Guid productOptionId, SetAvailabilityRequest request, CancellationToken ct) =>
        Execute(setOptionAvailability.HandleAsync(new SetProductOptionAvailabilityCommand { Scope = Scope(), ProductId = productId, OptionGroupId = optionGroupId, ProductOptionId = productOptionId, IsAvailable = request.IsAvailable, UpdatedByAccountId = CurrentAccountId() }, ct));

    [HttpDelete("{optionGroupId:long}/options/{productOptionId:guid}")]
    public Task<IActionResult> DeleteOption(Guid productId, long optionGroupId, Guid productOptionId, CancellationToken ct) =>
        Execute(deleteOption.HandleAsync(new DeleteProductOptionCommand { Scope = Scope(), ProductId = productId, OptionGroupId = optionGroupId, ProductOptionId = productOptionId }, ct));

    private ProductManagementCommandScope Scope() => new(CurrentUser(), null, true);

    private CurrentUserContext CurrentUser() => User.GetUserContext();

    private Guid? CurrentAccountId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private async Task<IActionResult> Execute<T>(Task<Application.Shared.Wrappers.ApiResult<T>> operation)
    {
        var result = await operation;
        return StatusCode(result.StatusCode, result);
    }
}
