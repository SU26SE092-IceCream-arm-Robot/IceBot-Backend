using System.Security.Claims;
using Application.Devices.Commands;
using Application.Devices.Queries;
using Application.Devices.Requests;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers.Devices;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management")]
[Authorize]
public sealed class ManagementDeviceCatalogController(
    ListDeviceTypesQueryHandler listTypesHandler,
    GetDeviceTypeQueryHandler getTypeHandler,
    ListDeviceModelsQueryHandler listModelsHandler,
    GetDeviceModelQueryHandler getModelHandler,
    CreateDeviceTypeCommandHandler createTypeHandler,
    UpdateDeviceTypeCommandHandler updateTypeHandler,
    SetDeviceTypeStatusCommandHandler setTypeStatusHandler,
    CreateDeviceModelCommandHandler createModelHandler,
    UpdateDeviceModelCommandHandler updateModelHandler,
    RetireDeviceModelCommandHandler retireModelHandler) : ControllerBase
{
    [HttpGet("device-types")]
    [Authorize(Policy = "device-catalog.read")]
    public async Task<IActionResult> ListTypes(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var result = await listTypesHandler.HandleAsync(new ListDeviceTypesQuery(search, isActive), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("device-types/{deviceTypeId:long}")]
    [Authorize(Policy = "device-catalog.read")]
    public async Task<IActionResult> GetType(long deviceTypeId, CancellationToken cancellationToken)
    {
        var result = await getTypeHandler.HandleAsync(new GetDeviceTypeQuery(deviceTypeId), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("device-types/{deviceTypeId:long}/models")]
    [Authorize(Policy = "device-catalog.read")]
    public async Task<IActionResult> ListModels(
        long deviceTypeId,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var result = await listModelsHandler.HandleAsync(new ListDeviceModelsQuery(deviceTypeId, search), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("device-models/{deviceModelId:guid}")]
    [Authorize(Policy = "device-catalog.read")]
    public async Task<IActionResult> GetModel(Guid deviceModelId, CancellationToken cancellationToken)
    {
        var result = await getModelHandler.HandleAsync(new GetDeviceModelQuery(deviceModelId), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("device-types")]
    [Authorize(Policy = "device-catalog.manage")]
    public async Task<IActionResult> CreateType(
        [FromBody] CreateDeviceTypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createTypeHandler.HandleAsync(new CreateDeviceTypeCommand(request, CurrentAccountId()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("device-types/{deviceTypeId:long}")]
    [Authorize(Policy = "device-catalog.manage")]
    public async Task<IActionResult> UpdateType(
        long deviceTypeId,
        [FromBody] UpdateDeviceTypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await updateTypeHandler.HandleAsync(
            new UpdateDeviceTypeCommand(deviceTypeId, request, CurrentAccountId()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("device-types/{deviceTypeId:long}/status")]
    [Authorize(Policy = "device-catalog.manage")]
    public async Task<IActionResult> SetTypeStatus(
        long deviceTypeId,
        [FromBody] SetDeviceTypeStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await setTypeStatusHandler.HandleAsync(
            new SetDeviceTypeStatusCommand(deviceTypeId, request.IsActive, CurrentAccountId()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("device-types/{deviceTypeId:long}/models")]
    [Authorize(Policy = "device-catalog.manage")]
    public async Task<IActionResult> CreateModel(
        long deviceTypeId,
        [FromBody] CreateDeviceModelRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createModelHandler.HandleAsync(
            new CreateDeviceModelCommand(deviceTypeId, request, CurrentAccountId()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("device-models/{deviceModelId:guid}")]
    [Authorize(Policy = "device-catalog.manage")]
    public async Task<IActionResult> UpdateModel(
        Guid deviceModelId,
        [FromBody] UpdateDeviceModelRequest request,
        CancellationToken cancellationToken)
    {
        var result = await updateModelHandler.HandleAsync(
            new UpdateDeviceModelCommand(deviceModelId, request, CurrentAccountId()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("device-models/{deviceModelId:guid}")]
    [Authorize(Policy = "device-catalog.manage")]
    public async Task<IActionResult> RetireModel(Guid deviceModelId, CancellationToken cancellationToken)
    {
        var result = await retireModelHandler.HandleAsync(
            new RetireDeviceModelCommand(deviceModelId, CurrentAccountId()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    private Guid? CurrentAccountId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var accountId) ? accountId : null;
}
