using Application.Identity.NotificationDevices.Abstractions;
using Application.Shared.Wrappers;

namespace Application.Identity.NotificationDevices.Commands;

public sealed class UnregisterCurrentAccountNotificationDeviceCommandHandler
{
    private readonly IAccountNotificationDeviceStore _devices;

    public UnregisterCurrentAccountNotificationDeviceCommandHandler(IAccountNotificationDeviceStore devices)
    {
        _devices = devices;
    }

    public async Task<ApiResult<bool>> HandleAsync(
        UnregisterCurrentAccountNotificationDeviceCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.AccountId == Guid.Empty || command.InstallationId == Guid.Empty)
        {
            return ApiResult<bool>.Fail("A valid account and installation id are required.");
        }

        var device = await _devices.GetByAccountAndInstallationAsync(
            command.AccountId,
            command.InstallationId,
            asNoTracking: false,
            cancellationToken);

        if (device?.InvalidatedAt is null)
        {
            if (device is not null)
            {
                device.Invalidate("Unregistered", DateTimeOffset.UtcNow);
                device.UpdatedByAccountId = command.AccountId;
                await _devices.SaveChangesAsync(cancellationToken);
            }
        }

        return ApiResult<bool>.Success(true, "Notification device unregistered.");
    }
}
