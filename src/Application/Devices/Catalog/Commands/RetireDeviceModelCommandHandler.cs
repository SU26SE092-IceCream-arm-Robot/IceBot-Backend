using Application.Devices.Catalog.Abstractions;
using Application.Devices.ExecutionEndpoints.Abstractions;
using Application.Devices.Telemetry.Abstractions;
using Application.Devices.Connectivity.Abstractions;
using Application.Devices.Credentials.Abstractions;
using Application.Shared.Wrappers;

namespace Application.Devices.Catalog.Commands;

public sealed class RetireDeviceModelCommandHandler(IDeviceManagementStore store)
{
    public async Task<ApiResult<object>> HandleAsync(
        RetireDeviceModelCommand command,
        CancellationToken cancellationToken = default)
    {
        var entity = await store.GetDeviceModelAsync(command.DeviceModelId, false, cancellationToken);
        if (entity is null)
        {
            return ApiResult<object>.Fail("Device model not found.", 404);
        }

        if (await store.DeviceModelIsAssignedAsync(entity.Id, cancellationToken))
        {
            return ApiResult<object>.Fail(
                "Device model cannot be retired while it is assigned to a non-retired device.", 409);
        }

        entity.DeletedAt = DateTimeOffset.UtcNow;
        entity.DeletedByAccountId = command.ActorId;
        await store.SaveChangesAsync(cancellationToken);
        return ApiResult<object>.Success(new { entity.Id }, "Device model retired.");
    }
}
