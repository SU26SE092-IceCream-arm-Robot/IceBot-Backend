using Application.Tenants;
using Application.Tenants.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WebAPI.Authorization;

namespace WebAPI.SignalR.Hubs;

[Authorize]
public sealed class OperationsHub : Hub
{
    private readonly IKioskStore _kioskStore;
    private readonly IOrganizationAccessStateReader _organizationAccess;

    public OperationsHub(IKioskStore kioskStore, IOrganizationAccessStateReader organizationAccess)
    {
        _kioskStore = kioskStore;
        _organizationAccess = organizationAccess;
    }

    public async Task JoinKiosk(Guid kioskId)
    {
        var user = Context.User!.GetUserContext();
        var kiosk = await _kioskStore.GetByIdAsync(kioskId, Context.ConnectionAborted);
        if (kiosk is null ||
            !await _organizationAccess.IsActiveAsync(kiosk.OrganizationId, Context.ConnectionAborted) ||
            !ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.OperationsView, user, kiosk.OrganizationId, kiosk.StoreId, kiosk.Id))
        {
            throw new HubException("Access denied: you do not have access to this kiosk.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"kiosk:{kioskId}");
    }

    public async Task LeaveKiosk(Guid kioskId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"kiosk:{kioskId}");
    }
}
