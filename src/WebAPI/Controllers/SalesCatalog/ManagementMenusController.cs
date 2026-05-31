using Application.SalesCatalog.Menus.Requests;
using Application.SalesCatalog.Menus.Services;
using Application.Shared.Exceptions;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebAPI.Controllers.SalesCatalog;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/menus")]
[Authorize(Policy = "menus.manage")]
public sealed class ManagementMenusController : ControllerBase
{
    private readonly MenuManagementService _menus;

    public ManagementMenusController(MenuManagementService menus)
    {
        _menus = menus;
    }

    [HttpGet]
    public async Task<IActionResult> ListMenus(
        [FromQuery] string? search,
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? storeId,
        [FromQuery] Guid? kioskId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _menus.ListMenusAsync(
            search,
            organizationId,
            storeId,
            kioskId,
            pageNumber,
            pageSize,
            cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{menuId:guid}")]
    public async Task<IActionResult> GetMenu(Guid menuId, CancellationToken cancellationToken)
    {
        var result = await _menus.GetMenuAsync(menuId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateMenu(
        [FromBody] CreateMenuRequest request,
        CancellationToken cancellationToken)
    {
        EnsureValidModel();

        var result = await _menus.CreateMenuAsync(
            request,
            GetCurrentAccountId(),
            cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{menuId:guid}")]
    public async Task<IActionResult> UpdateMenu(
        Guid menuId,
        [FromBody] UpdateMenuRequest request,
        CancellationToken cancellationToken)
    {
        EnsureValidModel();

        var result = await _menus.UpdateMenuAsync(
            menuId,
            request,
            GetCurrentAccountId(),
            cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{menuId:guid}/status")]
    public async Task<IActionResult> SetMenuStatus(
        Guid menuId,
        [FromBody] SetMenuStatusRequest request,
        CancellationToken cancellationToken)
    {
        EnsureValidModel();

        var result = await _menus.SetMenuStatusAsync(
            menuId,
            request.Status,
            GetCurrentAccountId(),
            cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{menuId:guid}")]
    public async Task<IActionResult> DeleteMenu(Guid menuId, CancellationToken cancellationToken)
    {
        var result = await _menus.DeleteMenuAsync(
            menuId,
            GetCurrentAccountId(),
            cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{menuId:guid}/items")]
    public async Task<IActionResult> AddMenuItem(
        Guid menuId,
        [FromBody] CreateMenuItemRequest request,
        CancellationToken cancellationToken)
    {
        EnsureValidModel();

        var result = await _menus.AddMenuItemAsync(
            menuId,
            request,
            GetCurrentAccountId(),
            cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{menuId:guid}/items/{menuItemId:guid}")]
    public async Task<IActionResult> UpdateMenuItem(
        Guid menuId,
        Guid menuItemId,
        [FromBody] UpdateMenuItemRequest request,
        CancellationToken cancellationToken)
    {
        EnsureValidModel();

        var result = await _menus.UpdateMenuItemAsync(
            menuId,
            menuItemId,
            request,
            GetCurrentAccountId(),
            cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{menuId:guid}/items/{menuItemId:guid}/status")]
    public async Task<IActionResult> SetMenuItemStatus(
        Guid menuId,
        Guid menuItemId,
        [FromBody] SetMenuItemStatusRequest request,
        CancellationToken cancellationToken)
    {
        EnsureValidModel();

        var result = await _menus.SetMenuItemStatusAsync(
            menuId,
            menuItemId,
            request.Status,
            GetCurrentAccountId(),
            cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{menuId:guid}/items/{menuItemId:guid}")]
    public async Task<IActionResult> DeleteMenuItem(
        Guid menuId,
        Guid menuItemId,
        CancellationToken cancellationToken)
    {
        var result = await _menus.DeleteMenuItemAsync(
            menuId,
            menuItemId,
            GetCurrentAccountId(),
            cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    private Guid? GetCurrentAccountId()
    {
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(accountId, out var parsedAccountId) ? parsedAccountId : null;
    }

    private void EnsureValidModel()
    {
        if (ModelState.IsValid)
        {
            return;
        }

        var errors = ModelState.ToDictionary(
            item => item.Key,
            item => item.Value?.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid");

        throw new ValidationException(errors);
    }
}
