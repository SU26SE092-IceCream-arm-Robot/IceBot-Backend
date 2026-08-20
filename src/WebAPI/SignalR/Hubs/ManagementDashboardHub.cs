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
    private readonly IOrganizationAccessStateReader _organizationAccess;

    public ManagementDashboardHub(IStoreStore storeStore, IOrganizationAccessStateReader organizationAccess)
    {
        _storeStore = storeStore;
        _organizationAccess = organizationAccess;
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
                if (!organizationId.HasValue ||
                    !ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.DashboardView, user, organizationId.Value, null, null))
                {
                    throw new HubException("Access denied: you do not have access to this organization dashboard.");
                }

                if (!await _organizationAccess.IsActiveAsync(organizationId.Value, Context.ConnectionAborted))
                {
                    Context.Abort();
                    throw new HubException("Organization access is unavailable.");
                }

                groupKey = $"dashboard:organization:{organizationId.Value}";
                break;
            case "store":
                if (!storeId.HasValue)
                {
                    throw new HubException("Store dashboard requires storeId.");
                }

                var store = await _storeStore.GetByIdAsync(storeId.Value, Context.ConnectionAborted);
                if (store is null ||
                    !ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.DashboardView, user, store.OrganizationId, store.Id, null))
                {
                    throw new HubException("Access denied: you do not have access to this store dashboard.");
                }

                if (!await _organizationAccess.IsActiveAsync(store.OrganizationId, Context.ConnectionAborted))
                {
                    Context.Abort();
                    throw new HubException("Organization access is unavailable.");
                }

                groupKey = $"dashboard:store:{store.Id}";
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
