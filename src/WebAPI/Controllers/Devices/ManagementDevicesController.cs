using Application.Devices.Commands;
using Application.Devices.Queries;
using Application.Devices.Requests;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Devices;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management")]
public sealed class ManagementDevicesController : ControllerBase
{
    private readonly ListDevicesQueryHandler _listDevicesHandler;
    private readonly GetDeviceQueryHandler _getDeviceHandler;
    private readonly CreateDeviceCommandHandler _createDeviceHandler;
    private readonly UpdateDeviceCommandHandler _updateDeviceHandler;
    private readonly SetDeviceStatusCommandHandler _setDeviceStatusHandler;
    private readonly RetireDeviceCommandHandler _retireDeviceHandler;

    public ManagementDevicesController(
        ListDevicesQueryHandler listDevicesHandler,
        GetDeviceQueryHandler getDeviceHandler,
        CreateDeviceCommandHandler createDeviceHandler,
        UpdateDeviceCommandHandler updateDeviceHandler,
        SetDeviceStatusCommandHandler setDeviceStatusHandler,
        RetireDeviceCommandHandler retireDeviceHandler)
    {
        _listDevicesHandler = listDevicesHandler;
        _getDeviceHandler = getDeviceHandler;
        _createDeviceHandler = createDeviceHandler;
        _updateDeviceHandler = updateDeviceHandler;
        _setDeviceStatusHandler = setDeviceStatusHandler;
        _retireDeviceHandler = retireDeviceHandler;
    }

    [HttpGet("devices")]
    [Authorize(Policy = "devices.view")]
    public async Task<IActionResult> ListDevices(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? storeId,
        [FromQuery] Guid? kioskId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var query = new ListDevicesQuery
        {
            UserContext = context,
            OrganizationId = organizationId,
            StoreId = storeId,
            KioskId = kioskId,
            Status = status,
            Search = search
        };

        var result = await _listDevicesHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("devices/{deviceId:guid}")]
    [Authorize(Policy = "devices.view")]
    public async Task<IActionResult> GetDevice(
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var query = new GetDeviceQuery
        {
            UserContext = context,
            DeviceId = deviceId
        };

        var result = await _getDeviceHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("kiosks/{kioskId:guid}/devices")]
    [Authorize(Policy = "devices.manage")]
    public async Task<IActionResult> CreateDevice(
        Guid kioskId,
        [FromBody] CreateDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var command = new CreateDeviceCommand
        {
            UserContext = context,
            KioskId = kioskId,
            Request = request
        };

        var result = await _createDeviceHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("devices/{deviceId:guid}")]
    [Authorize(Policy = "devices.manage")]
    public async Task<IActionResult> UpdateDevice(
        Guid deviceId,
        [FromBody] UpdateDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var command = new UpdateDeviceCommand
        {
            UserContext = context,
            DeviceId = deviceId,
            Request = request
        };

        var result = await _updateDeviceHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("devices/{deviceId:guid}/status")]
    [Authorize(Policy = "devices.manage")]
    public async Task<IActionResult> SetDeviceStatus(
        Guid deviceId,
        [FromBody] SetDeviceStatusRequest request,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var command = new SetDeviceStatusCommand
        {
            UserContext = context,
            DeviceId = deviceId,
            Request = request
        };

        var result = await _setDeviceStatusHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("devices/{deviceId:guid}")]
    [Authorize(Policy = "devices.manage")]
    public async Task<IActionResult> RetireDevice(
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var command = new RetireDeviceCommand
        {
            UserContext = context,
            DeviceId = deviceId
        };

        var result = await _retireDeviceHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
