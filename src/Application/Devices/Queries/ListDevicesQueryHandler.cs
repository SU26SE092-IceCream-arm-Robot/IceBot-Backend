using Application.Devices.Abstractions;
using Application.Devices.Mapping;
using Application.Devices.Results;
using Application.Shared.Wrappers;
using Domain.Devices.Entities;
using Domain.Devices.Enums;

namespace Application.Devices.Queries;

public sealed class ListDevicesQueryHandler
{
    private readonly IDeviceManagementStore _deviceStore;

    public ListDevicesQueryHandler(IDeviceManagementStore deviceStore)
    {
        _deviceStore = deviceStore;
    }

    public async Task<ApiResult<IReadOnlyList<DeviceResult>>> HandleAsync(
        ListDevicesQuery query,
        CancellationToken cancellationToken = default)
    {
        var userContext = query.UserContext;
        var organizationId = query.OrganizationId;
        var storeId = query.StoreId;
        var kioskId = query.KioskId;
        var status = query.Status;
        var search = query.Search;

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<DeviceStatus>(status.Trim(), ignoreCase: true, out var deviceStatus) ||
                !Enum.IsDefined(deviceStatus))
            {
                return ApiResult<IReadOnlyList<DeviceResult>>.Fail("Invalid device status.", 400);
            }
        }

        IReadOnlyList<Device> list;
        if (userContext.IsSystemAdmin)
        {
            list = await _deviceStore.ListAsync(
                organizationId,
                storeId,
                kioskId,
                status,
                search,
                cancellationToken);
        }
        else
        {
            list = await _deviceStore.ListAccessibleAsync(
                userContext.AllowedOrganizationIds,
                userContext.AllowedStoreIds,
                userContext.AllowedKioskIds,
                organizationId,
                storeId,
                kioskId,
                status,
                search,
                cancellationToken);
        }

        var results = list.Select(DeviceResultMapper.ToResult).ToList();
        return ApiResult<IReadOnlyList<DeviceResult>>.Success(results);
    }
}
