using Application.Catalog.Products.Commands;
using Application.Catalog.Products.Requests;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Catalog;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/organizations/{organizationId:guid}/products/{productId:guid}/option-groups")]
[Authorize(Policy = "products.manage")]
public sealed class ManagementProductOptionsController(
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
    public async Task<IActionResult> CreateGroup(Guid organizationId, Guid productId, [FromBody] CreateOptionGroupRequest request, CancellationToken ct)
    {
        var result = await createGroup.HandleAsync(new CreateOptionGroupCommand
        {
            Scope = Scope(organizationId), ProductId = productId, Request = request, CreatedByAccountId = CurrentAccountId()
        }, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{optionGroupId:long}")]
    public async Task<IActionResult> UpdateGroup(Guid organizationId, Guid productId, long optionGroupId, [FromBody] UpdateOptionGroupRequest request, CancellationToken ct)
    {
        var result = await updateGroup.HandleAsync(new UpdateOptionGroupCommand
        {
            Scope = Scope(organizationId), ProductId = productId, OptionGroupId = optionGroupId,
            Request = request, UpdatedByAccountId = CurrentAccountId()
        }, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{optionGroupId:long}/status")]
    public async Task<IActionResult> SetGroupStatus(Guid organizationId, Guid productId, long optionGroupId, [FromBody] SetOptionGroupStatusRequest request, CancellationToken ct)
    {
        var result = await setGroupStatus.HandleAsync(new SetOptionGroupStatusCommand
        {
            Scope = Scope(organizationId), ProductId = productId, OptionGroupId = optionGroupId,
            IsActive = request.IsActive, UpdatedByAccountId = CurrentAccountId()
        }, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{optionGroupId:long}")]
    public async Task<IActionResult> DeleteGroup(Guid organizationId, Guid productId, long optionGroupId, CancellationToken ct)
    {
        var result = await deleteGroup.HandleAsync(new DeleteOptionGroupCommand
        {
            Scope = Scope(organizationId), ProductId = productId, OptionGroupId = optionGroupId
        }, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{optionGroupId:long}/options")]
    public async Task<IActionResult> CreateOption(Guid organizationId, Guid productId, long optionGroupId, [FromBody] CreateProductOptionRequest request, CancellationToken ct)
    {
        var result = await createOption.HandleAsync(new CreateProductOptionCommand
        {
            Scope = Scope(organizationId), ProductId = productId, OptionGroupId = optionGroupId,
            Request = request, CreatedByAccountId = CurrentAccountId()
        }, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{optionGroupId:long}/options/{productOptionId:guid}")]
    public async Task<IActionResult> UpdateOption(Guid organizationId, Guid productId, long optionGroupId, Guid productOptionId, [FromBody] UpdateProductOptionRequest request, CancellationToken ct)
    {
        var result = await updateOption.HandleAsync(new UpdateProductOptionCommand
        {
            Scope = Scope(organizationId), ProductId = productId, OptionGroupId = optionGroupId,
            ProductOptionId = productOptionId, Request = request, UpdatedByAccountId = CurrentAccountId()
        }, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{optionGroupId:long}/options/{productOptionId:guid}/ingredient-requirements")]
    public async Task<IActionResult> ReplaceIngredientRequirements(
        Guid organizationId, Guid productId, long optionGroupId, Guid productOptionId,
        [FromBody] ReplaceProductOptionIngredientRequirementsRequest request, CancellationToken ct)
    {
        var result = await replaceIngredientRequirements.HandleAsync(new ReplaceProductOptionIngredientRequirementsCommand
        {
            Scope = Scope(organizationId), ProductId = productId, OptionGroupId = optionGroupId,
            ProductOptionId = productOptionId, Request = request, UpdatedByAccountId = CurrentAccountId()
        }, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{optionGroupId:long}/options/{productOptionId:guid}/availability")]
    public async Task<IActionResult> SetOptionAvailability(Guid organizationId, Guid productId, long optionGroupId, Guid productOptionId, [FromBody] SetAvailabilityRequest request, CancellationToken ct)
    {
        var result = await setOptionAvailability.HandleAsync(new SetProductOptionAvailabilityCommand
        {
            Scope = Scope(organizationId), ProductId = productId, OptionGroupId = optionGroupId,
            ProductOptionId = productOptionId, IsAvailable = request.IsAvailable, UpdatedByAccountId = CurrentAccountId()
        }, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{optionGroupId:long}/options/{productOptionId:guid}")]
    public async Task<IActionResult> DeleteOption(Guid organizationId, Guid productId, long optionGroupId, Guid productOptionId, CancellationToken ct)
    {
        var result = await deleteOption.HandleAsync(new DeleteProductOptionCommand
        {
            Scope = Scope(organizationId), ProductId = productId, OptionGroupId = optionGroupId, ProductOptionId = productOptionId
        }, ct);
        return StatusCode(result.StatusCode, result);
    }

    private ProductManagementCommandScope Scope(Guid organizationId) => new(User.GetUserContext(), organizationId);

    private Guid? CurrentAccountId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
