using Application.Identity.Abstractions;
using Application.Identity.NotificationDevices.Abstractions;
using Application.Identity.NotificationDevices.Results;
using Application.Identity.NotificationDevices.Support;
using Application.Shared.Wrappers;
using Domain.Identity.Entities;
using Domain.Identity.Enums;

namespace Application.Identity.NotificationDevices.Commands;

public sealed class RegisterCurrentAccountNotificationDeviceCommandHandler
{
    private readonly IIdentityAccountStore _accounts;
    private readonly IAccountNotificationDeviceStore _devices;

    public RegisterCurrentAccountNotificationDeviceCommandHandler(
        IIdentityAccountStore accounts,
        IAccountNotificationDeviceStore devices)
    {
        _accounts = accounts;
        _devices = devices;
    }

    public async Task<ApiResult<AccountNotificationDeviceResult>> HandleAsync(
        RegisterCurrentAccountNotificationDeviceCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.AccountId == Guid.Empty || command.InstallationId == Guid.Empty)
        {
            return ApiResult<AccountNotificationDeviceResult>.Fail("A valid account and installation id are required.");
        }

        if (!NotificationDeviceRegistrationRules.TryNormalize(
                command.Request.Platform,
                command.Request.PushToken,
                command.Request.DeviceName,
                command.Request.AppVersion,
                out var registration,
                out var validationError))
        {
            return ApiResult<AccountNotificationDeviceResult>.Fail(validationError!);
        }

        var account = await _accounts.GetByIdAsync(command.AccountId, cancellationToken: cancellationToken);
        if (account is null)
        {
            return ApiResult<AccountNotificationDeviceResult>.Fail("Account not found.", 404);
        }

        if (account.Status != AccountStatus.Active)
        {
            return ApiResult<AccountNotificationDeviceResult>.Fail("Account is not active.", 403);
        }

        return await _devices.ExecuteRegistrationTransactionAsync(
            command.AccountId,
            command.InstallationId,
            registration.PushTokenHash,
            async transactionToken =>
            {
                var now = DateTimeOffset.UtcNow;
                var device = await _devices.GetByAccountAndInstallationAsync(
                    command.AccountId,
                    command.InstallationId,
                    asNoTracking: false,
                    transactionToken);
                var activeTokenOwner = await _devices.GetActiveByPushTokenHashAsync(
                    registration.PushTokenHash,
                    transactionToken);

                if (activeTokenOwner is not null && activeTokenOwner.Id != device?.Id)
                {
                    activeTokenOwner.Invalidate("TokenReassigned", now);
                    activeTokenOwner.UpdatedByAccountId = command.AccountId;
                }

                if (device is null)
                {
                    device = new AccountNotificationDevice
                    {
                        AccountId = command.AccountId,
                        InstallationId = command.InstallationId
                    };
                    await _devices.AddAsync(device, transactionToken);
                }

                device.Platform = registration.Platform;
                device.PushToken = registration.PushToken;
                device.PushTokenHash = registration.PushTokenHash;
                device.DeviceName = registration.DeviceName;
                device.AppVersion = registration.AppVersion;
                device.LastSeenAt = now;
                device.InvalidatedAt = null;
                device.InvalidationReason = null;
                device.UpdatedAt = now;
                device.UpdatedByAccountId = command.AccountId;

                await _devices.SaveChangesAsync(transactionToken);
                return ApiResult<AccountNotificationDeviceResult>.Success(
                    AccountNotificationDeviceResultMapper.ToResult(device),
                    "Notification device registered.");
            },
            cancellationToken);
    }
}
