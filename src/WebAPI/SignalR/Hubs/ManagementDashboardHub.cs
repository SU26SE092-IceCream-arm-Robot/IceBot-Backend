using Application.Tenants;
using Application.Tenants.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WebAPI.Authorization;

namespace WebAPI.SignalR.Hubs;

[Authorize]
public sealed class ManagementDashboardHub : Hub
{
    private readonly IStoreStore _storeStore;

    public ManagementDashboardHub(IStoreStore storeStore)
    {
        _storeStore = storeStore;
    }

    public async Task JoinDashboard(string scope, Guid? organizationId, Guid? storeId)
    {
        var user = Context.User!.GetUserContext();

        string groupKey;
        switch (scope.ToLowerInvariant())
        {
            case "system":
                groupKey = user.IsSystemAdmin
                    ? "dashboard:system"
                    : throw new HubException("Access denied: you do not have SystemAdmin access.");
                break;
            case "organization":
                groupKey = organizationId.HasValue && ScopeAccessRules.CanAccessScopedRow(user, organizationId.Value, null, null)
                    ? $"dashboard:organization:{organizationId.Value}"
                    : throw new HubException("Access denied: you do not have access to this organization dashboard.");
                break;
            case "store":
                if (!storeId.HasValue)
                {
                    throw new HubException("Store dashboard requires storeId.");
                }

                var store = await _storeStore.GetByIdAsync(storeId.Value, Context.ConnectionAborted);
                groupKey = store is not null && ScopeAccessRules.CanAccessScopedRow(user, store.OrganizationId, store.Id, null)
                    ? $"dashboard:store:{store.Id}"
                    : throw new HubException("Access denied: you do not have access to this store dashboard.");
                break;
            default:
                throw new HubException($"Invalid dashboard scope: '{scope}'. Allowed values are 'system', 'organization', 'store'.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, groupKey);
    }

    public async Task LeaveDashboard(string scope, Guid? organizationId, Guid? storeId)
    {
        string groupKey = scope.ToLowerInvariant() switch
        {
            "system" => "dashboard:system",
            "organization" when organizationId.HasValue => $"dashboard:organization:{organizationId.Value}",
            "store" when storeId.HasValue => $"dashboard:store:{storeId.Value}",
            _ => throw new HubException($"Invalid dashboard scope: '{scope}'.")
        };

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupKey);
    }
}
