using Application.SalesCatalog.Menus.Commands;
using Application.SalesCatalog.Menus.Queries;
using Application.SalesCatalog.Menus.Requests;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Authorization;

namespace WebAPI.Controllers.SalesCatalog;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/organizations/{organizationId:guid}/menus")]
[Authorize(Policy = "menus.manage")]
public sealed class ManagementMenusController : ControllerBase
{
    private readonly ListMenusQueryHandler _listMenusHandler;
    private readonly GetMenuQueryHandler _getMenuHandler;
    private readonly CreateMenuCommandHandler _createMenuHandler;
    private readonly UpdateMenuCommandHandler _updateMenuHandler;
    private readonly SetMenuStatusCommandHandler _setMenuStatusHandler;
    private readonly DeleteMenuCommandHandler _deleteMenuHandler;
    private readonly AddMenuItemCommandHandler _addMenuItemHandler;
    private readonly UpdateMenuItemCommandHandler _updateMenuItemHandler;
    private readonly SetMenuItemStatusCommandHandler _setMenuItemStatusHandler;
    private readonly DeleteMenuItemCommandHandler _deleteMenuItemHandler;

    public ManagementMenusController(
        ListMenusQueryHandler listMenusHandler,
        GetMenuQueryHandler getMenuHandler,
        CreateMenuCommandHandler createMenuHandler,
        UpdateMenuCommandHandler updateMenuHandler,
        SetMenuStatusCommandHandler setMenuStatusHandler,
        DeleteMenuCommandHandler deleteMenuHandler,
        AddMenuItemCommandHandler addMenuItemHandler,
        UpdateMenuItemCommandHandler updateMenuItemHandler,
        SetMenuItemStatusCommandHandler setMenuItemStatusHandler,
        DeleteMenuItemCommandHandler deleteMenuItemHandler)
    {
        _listMenusHandler = listMenusHandler;
        _getMenuHandler = getMenuHandler;
        _createMenuHandler = createMenuHandler;
        _updateMenuHandler = updateMenuHandler;
        _setMenuStatusHandler = setMenuStatusHandler;
        _deleteMenuHandler = deleteMenuHandler;
        _addMenuItemHandler = addMenuItemHandler;
        _updateMenuItemHandler = updateMenuItemHandler;
        _setMenuItemStatusHandler = setMenuItemStatusHandler;
        _deleteMenuItemHandler = deleteMenuItemHandler;
    }

    [HttpGet]
    public async Task<IActionResult> ListMenus(
        Guid organizationId,
        [FromQuery] string? search,
        [FromQuery] Guid? storeId,
        [FromQuery] Guid? kioskId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListMenusQuery
        {
            UserContext = User.GetUserContext(),
            Search = search,
            OrganizationId = organizationId,
            StoreId = storeId,
            KioskId = kioskId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var result = await _listMenusHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{menuId:guid}")]
    public async Task<IActionResult> GetMenu(Guid organizationId, Guid menuId, CancellationToken cancellationToken)
    {
        var query = new GetMenuQuery(menuId)
        {
            OrganizationId = organizationId,
            UserContext = User.GetUserContext()
        };
        var result = await _getMenuHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateMenu(
        Guid organizationId,
        [FromBody] CreateMenuRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateMenuCommand
        {
            Scope = Scope(organizationId),
            Request = request,
            CreatedByAccountId = GetCurrentAccountId()
        };
        var result = await _createMenuHandler.HandleAsync(command, cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{menuId:guid}")]
    public async Task<IActionResult> UpdateMenu(
        Guid organizationId,
        Guid menuId,
        [FromBody] UpdateMenuRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateMenuCommand
        {
            Scope = Scope(organizationId),
            MenuId = menuId,
            Request = request,
            UpdatedByAccountId = GetCurrentAccountId()
        };
        var result = await _updateMenuHandler.HandleAsync(command, cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{menuId:guid}/status")]
    public async Task<IActionResult> SetMenuStatus(
        Guid organizationId,
        Guid menuId,
        [FromBody] SetMenuStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SetMenuStatusCommand
        {
            Scope = Scope(organizationId),
            MenuId = menuId,
            Status = request.Status,
            UpdatedByAccountId = GetCurrentAccountId()
        };
        var result = await _setMenuStatusHandler.HandleAsync(command, cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{menuId:guid}")]
    public async Task<IActionResult> DeleteMenu(Guid organizationId, Guid menuId, CancellationToken cancellationToken)
    {
        var command = new DeleteMenuCommand
        {
            Scope = Scope(organizationId),
            MenuId = menuId,
            DeletedByAccountId = GetCurrentAccountId()
        };
        var result = await _deleteMenuHandler.HandleAsync(command, cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{menuId:guid}/items")]
    public async Task<IActionResult> AddMenuItem(
        Guid organizationId,
        Guid menuId,
        [FromBody] CreateMenuItemRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddMenuItemCommand
        {
            Scope = Scope(organizationId),
            MenuId = menuId,
            Request = request,
            CreatedByAccountId = GetCurrentAccountId()
        };
        var result = await _addMenuItemHandler.HandleAsync(command, cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{menuId:guid}/items/{menuItemId:guid}")]
    public async Task<IActionResult> UpdateMenuItem(
        Guid organizationId,
        Guid menuId,
        Guid menuItemId,
        [FromBody] UpdateMenuItemRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateMenuItemCommand
        {
            Scope = Scope(organizationId),
            MenuId = menuId,
            MenuItemId = menuItemId,
            Request = request,
            UpdatedByAccountId = GetCurrentAccountId()
        };
        var result = await _updateMenuItemHandler.HandleAsync(command, cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{menuId:guid}/items/{menuItemId:guid}/status")]
    public async Task<IActionResult> SetMenuItemStatus(
        Guid organizationId,
        Guid menuId,
        Guid menuItemId,
        [FromBody] SetMenuItemStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SetMenuItemStatusCommand
        {
            Scope = Scope(organizationId),
            MenuId = menuId,
            MenuItemId = menuItemId,
            Status = request.Status,
            UpdatedByAccountId = GetCurrentAccountId()
        };
        var result = await _setMenuItemStatusHandler.HandleAsync(command, cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{menuId:guid}/items/{menuItemId:guid}")]
    public async Task<IActionResult> DeleteMenuItem(
        Guid organizationId,
        Guid menuId,
        Guid menuItemId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteMenuItemCommand
        {
            Scope = Scope(organizationId),
            MenuId = menuId,
            MenuItemId = menuItemId,
            DeletedByAccountId = GetCurrentAccountId()
        };
        var result = await _deleteMenuItemHandler.HandleAsync(command, cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    private Guid? GetCurrentAccountId()
    {
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(accountId, out var parsedAccountId) ? parsedAccountId : null;
    }

    private MenuManagementCommandScope Scope(Guid organizationId) =>
        new(User.GetUserContext(), organizationId);
}
