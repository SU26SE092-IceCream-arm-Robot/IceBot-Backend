using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.Kiosks.Results;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;

namespace Application.Tenants.Kiosks.Queries;

public sealed class ListKiosksQueryHandler
{
    private readonly IKioskStore _kioskStore;

    public ListKiosksQueryHandler(IKioskStore kioskStore)
    {
        _kioskStore = kioskStore;
    }

    public async Task<ApiResult<IReadOnlyList<KioskResult>>> HandleAsync(
        ListKiosksQuery query,
        CancellationToken cancellationToken = default)
    {
        var userContext = query.UserContext;
        var organizationId = query.OrganizationId;
        var storeId = query.StoreId;
        var status = query.Status;
        var search = query.Search;

        KioskStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<KioskStatus>(status.Trim(), ignoreCase: true, out var resultStatus) ||
                !Enum.IsDefined(resultStatus))
            {
                return ApiResult<IReadOnlyList<KioskResult>>.Fail("Invalid kiosk status.", 400);
            }

            parsedStatus = resultStatus;
        }

        IReadOnlyList<Kiosk> list;
        if (userContext.IsSystemAdmin)
        {
            list = await _kioskStore.ListAsync(organizationId, storeId, parsedStatus, search, cancellationToken);
        }
        else
        {
            list = await _kioskStore.ListAccessibleAsync(
                userContext.AllowedOrganizationIds,
                userContext.AllowedStoreIds,
                userContext.AllowedKioskIds,
                organizationId,
                storeId,
                parsedStatus,
                search,
                cancellationToken);
        }

        var results = list.Select(KioskResultMapper.ToResult).ToList();
        return ApiResult<IReadOnlyList<KioskResult>>.Success(results);
    }
}
