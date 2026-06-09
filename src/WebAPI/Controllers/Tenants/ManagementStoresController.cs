using Application.Shared.Exceptions;
using Application.Tenants.Stores.Commands;
using Application.Tenants.Stores.Queries;
using Application.Tenants.Stores.Requests;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Tenants;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management")]
public class ManagementStoresController : ControllerBase
{
    private readonly ListStoresQueryHandler _listStores;
    private readonly GetStoreQueryHandler _getStore;
    private readonly CreateStoreCommandHandler _createStore;
    private readonly UpdateStoreCommandHandler _updateStore;
    private readonly DisableStoreCommandHandler _disableStore;
    private readonly ActivateStoreCommandHandler _activateStore;

    public ManagementStoresController(
        ListStoresQueryHandler listStores,
        GetStoreQueryHandler getStore,
        CreateStoreCommandHandler createStore,
        UpdateStoreCommandHandler updateStore,
        DisableStoreCommandHandler disableStore,
        ActivateStoreCommandHandler activateStore)
    {
        _listStores = listStores;
        _getStore = getStore;
        _createStore = createStore;
        _updateStore = updateStore;
        _disableStore = disableStore;
        _activateStore = activateStore;
    }

    [HttpGet("stores")]
    [Authorize(Policy = "stores.view")]
    public async Task<IActionResult> ListStores(
        [FromQuery] Guid? organizationId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var query = new ListStoresQuery
        {
            UserContext = context,
            OrganizationId = organizationId,
            Status = status,
            Search = search
        };
        var result = await _listStores.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("stores/{storeId:guid}")]
    [Authorize(Policy = "stores.view")]
    public async Task<IActionResult> GetStore(
        Guid storeId,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var query = new GetStoreQuery
        {
            UserContext = context,
            StoreId = storeId
        };
        var result = await _getStore.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("organizations/{organizationId:guid}/stores")]
    [Authorize(Policy = "stores.manage")]
    public async Task<IActionResult> CreateStore(
        Guid organizationId,
        [FromBody] CreateStoreRequest request,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var command = new CreateStoreCommand
        {
            UserContext = context,
            OrganizationId = organizationId,
            Request = request
        };
        var result = await _createStore.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("stores/{storeId:guid}")]
    [Authorize(Policy = "stores.update")]
    public async Task<IActionResult> UpdateStore(
        Guid storeId,
        [FromBody] UpdateStoreRequest request,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var command = new UpdateStoreCommand
        {
            UserContext = context,
            StoreId = storeId,
            Request = request
        };
        var result = await _updateStore.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("stores/{storeId:guid}/disable")]
    [Authorize(Policy = "stores.manage")]
    public async Task<IActionResult> DisableStore(
        Guid storeId,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var command = new DisableStoreCommand
        {
            UserContext = context,
            StoreId = storeId
        };
        var result = await _disableStore.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("stores/{storeId:guid}/activate")]
    [Authorize(Policy = "stores.manage")]
    public async Task<IActionResult> ActivateStore(
        Guid storeId,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var command = new ActivateStoreCommand
        {
            UserContext = context,
            StoreId = storeId
        };
        var result = await _activateStore.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
