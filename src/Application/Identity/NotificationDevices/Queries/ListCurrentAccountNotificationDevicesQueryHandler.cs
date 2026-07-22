using Application.Identity.NotificationDevices.Abstractions;
using Application.Identity.NotificationDevices.Results;
using Application.Shared.Wrappers;

namespace Application.Identity.NotificationDevices.Queries;

public sealed class ListCurrentAccountNotificationDevicesQueryHandler
{
    private readonly IAccountNotificationDeviceStore _devices;

    public ListCurrentAccountNotificationDevicesQueryHandler(IAccountNotificationDeviceStore devices)
    {
        _devices = devices;
    }

    public async Task<ApiResult<IReadOnlyList<AccountNotificationDeviceResult>>> HandleAsync(
        ListCurrentAccountNotificationDevicesQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.AccountId == Guid.Empty)
        {
            return ApiResult<IReadOnlyList<AccountNotificationDeviceResult>>.Fail("A valid account id is required.");
        }

        var devices = await _devices.ListByAccountIdAsync(query.AccountId, cancellationToken);
        var result = devices.Select(AccountNotificationDeviceResultMapper.ToResult).ToList();
        return ApiResult<IReadOnlyList<AccountNotificationDeviceResult>>.Success(result);
    }
}
