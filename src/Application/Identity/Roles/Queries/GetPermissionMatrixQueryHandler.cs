using Application.Shared.Wrappers;

namespace Application.Identity.Roles.Queries;

public sealed class PermissionMatrixItem
{
    public string Policy { get; init; } = null!;
    public string Description { get; init; } = null!;
    public IReadOnlyList<string> Roles { get; init; } = null!;
    public bool ScopeRequired { get; init; }
}

public static class PermissionCatalog
{
    public static readonly IReadOnlyList<PermissionMatrixItem> Matrix = new List<PermissionMatrixItem>
    {
        new()
        {
            Policy = "accounts.manage",
            Description = "Create, update, disable, assign roles, set password, and send invitations for internal accounts.",
            Roles = new[] { "SystemAdmin", "OrgAdmin" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "platform-technicians.read",
            Description = "View platform-owned Technician accounts and their assigned support scopes.",
            Roles = new[] { "SystemAdmin" },
            ScopeRequired = false
        },
        new()
        {
            Policy = "platform-technicians.manage",
            Description = "Create, update, scope, deactivate, and reactivate platform-owned Technician accounts.",
            Roles = new[] { "SystemAdmin" },
            ScopeRequired = false
        },
        new()
        {
            Policy = "accounts.read",
            Description = "View internal accounts within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "organizations.manage",
            Description = "Platform-level organization management: create, suspend/resume, deactivate/reactivate, and inspect lifecycle history.",
            Roles = new[] { "SystemAdmin" },
            ScopeRequired = false
        },
        new()
        {
            Policy = "organizations.view",
            Description = "View organization details within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "organizations.update",
            Description = "Update organization details within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "stores.view",
            Description = "View store details within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "stores.manage",
            Description = "Create, disable, or activate stores.",
            Roles = new[] { "SystemAdmin", "OrgAdmin" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "stores.update",
            Description = "Update store details within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "stores.sales.manage",
            Description = "Pause or resume store sales within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "kiosks.view",
            Description = "View kiosk details within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "kiosks.manage",
            Description = "Create, activate, or disable kiosks.",
            Roles = new[] { "SystemAdmin", "OrgAdmin" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "kiosks.update",
            Description = "Update kiosk details within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "kiosks.operations.manage",
            Description = "Change kiosk operational and maintenance state within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Technician" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "tenant-tree.view",
            Description = "View tenant hierarchy for scope selection and navigation.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Technician" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "products.manage",
            Description = "Manage organization-owned products and variants within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "product-categories.read",
            Description = "Browse the global flat ProductCategory catalog used by product authoring.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager" },
            ScopeRequired = false
        },
        new()
        {
            Policy = "product-categories.manage",
            Description = "Create, update, activate/deactivate, and safely delete unreferenced ProductCategory definitions.",
            Roles = new[] { "SystemAdmin" },
            ScopeRequired = false
        },
        new()
        {
            Policy = "ingredients.read",
            Description = "Browse the global ingredient reference catalog used by recipe authoring.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager" },
            ScopeRequired = false
        },
        new()
        {
            Policy = "ingredients.manage",
            Description = "Create, update, activate/deactivate, and safely delete unreferenced ingredient definitions.",
            Roles = new[] { "SystemAdmin" },
            ScopeRequired = false
        },
        new()
        {
            Policy = "product-templates.read",
            Description = "Browse global product templates for tenant cloning.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager" },
            ScopeRequired = false
        },
        new()
        {
            Policy = "product-templates.manage",
            Description = "Manage global product templates.",
            Roles = new[] { "SystemAdmin" },
            ScopeRequired = false
        },
        new()
        {
            Policy = "menus.manage",
            Description = "Manage organization-owned menus and menu items within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "payments.manage",
            Description = "Manage payment methods and status.",
            Roles = new[] { "OrgAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "payments.reconciliation.view",
            Description = "View organization-level daily payment reconciliation aggregates and scoped discrepancies.",
            Roles = new[] { "OrgAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "cash-payments.confirm",
            Description = "Confirm staff-received cash payments for orders within assigned scope.",
            Roles = new[] { "OrgAdmin", "Manager", "Staff" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "payment-methods.manage",
            Description = "Manage the global payment-method catalog status.",
            Roles = new[] { "SystemAdmin" },
            ScopeRequired = false
        },
        new()
        {
            Policy = "permission-matrix.view",
            Description = "View the platform permission matrix.",
            Roles = new[] { "SystemAdmin" },
            ScopeRequired = false
        },
        new()
        {
            Policy = "dashboard.view",
            Description = "View management dashboard metrics within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "orders.view",
            Description = "View back-office orders within allowed scope.",
            Roles = new[] { "OrgAdmin", "Manager", "Staff" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "orders.fulfillment.manage",
            Description = "Record manual and packaged-item fulfillment outcomes within allowed scope.",
            Roles = new[] { "OrgAdmin", "Manager", "Staff" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "orders.intervention.manage",
            Description = "Cancel orders, redispatch execution, or request production remake within allowed scope.",
            Roles = new[] { "OrgAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "orders.refund-flag",
            Description = "Mark an order as requiring refund review within allowed scope.",
            Roles = new[] { "OrgAdmin", "Manager", "Staff" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "refunds.view",
            Description = "View refunds within allowed scope.",
            Roles = new[] { "OrgAdmin", "Manager", "Staff" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "refunds.request",
            Description = "Request refunds within allowed scope.",
            Roles = new[] { "OrgAdmin", "Manager", "Staff" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "refunds.process",
            Description = "Mark refunds processed, reject, or cancel them within allowed scope.",
            Roles = new[] { "OrgAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "workforce.staff.read",
            Description = "View Staff workforce accounts within assigned organization or store scope.",
            Roles = new[] { "OrgAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "workforce.staff.manage",
            Description = "Create, update, scope, invite, deactivate, and reactivate Staff-only workforce accounts within assigned scope.",
            Roles = new[] { "OrgAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "platform.organization-sales.view",
            Description = "View organization-level aggregate sales collections for platform administration and reporting.",
            Roles = new[] { "SystemAdmin" },
            ScopeRequired = false
        },
        new()
        {
            Policy = "inventory.view",
            Description = "View inventory (dispenser states, stock movements) within allowed scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "inventory.refill.manage",
            Description = "Request, start, complete, or cancel physical inventory refill tasks within allowed scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Staff" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "inventory.adjust.manage",
            Description = "Correct an inventory estimate outside the audited refill workflow within allowed scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "inventory.configure",
            Description = "Provision, configure, activate/retire, and safely delete kiosk dispenser topology.",
            Roles = new[] { "SystemAdmin", "OrgAdmin" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "maintenance.view",
            Description = "View maintenance tickets within allowed scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "maintenance.create",
            Description = "Create maintenance tickets within allowed scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "maintenance.manage",
            Description = "Manage maintenance tickets within allowed scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Technician" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "operations.view",
            Description = "View kiosk operations telemetry such as heartbeats, device events, and curated operation logs within allowed scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "operations.diagnostics",
            Description = "View raw operation-log diagnostic payloads within allowed kiosk scope.",
            Roles = new[] { "SystemAdmin", "Technician" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "payments.diagnostics.view",
            Description = "View bounded payment-session diagnostics without raw provider payloads within allowed scope.",
            Roles = new[] { "OrgAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "notifications.manage",
            Description = "Requeue permanently failed notification deliveries within allowed scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "notifications.view",
            Description = "View notification delivery status and retry evidence without provider diagnostic details.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "devices.view",
            Description = "View devices/hardware details within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "devices.manage",
            Description = "Create, update, replace, or retire devices/hardware within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "devices.operations.manage",
            Description = "Change the operational status of non-retired devices within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Technician" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "execution-endpoints.manage",
            Description = "Create or retire Edge execution endpoints within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "execution-endpoints.operations.manage",
            Description = "Disable or reactivate Edge execution endpoints within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Technician" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "execution-endpoints.provision",
            Description = "Provision Edge execution endpoint identity within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Technician" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "execution-endpoints.credentials.manage",
            Description = "Rotate mTLS identity or manage MQTT credentials for Edge execution endpoints within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Technician" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "device-catalog.read",
            Description = "Read the global device type and model catalog.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician" },
            ScopeRequired = false
        },
        new()
        {
            Policy = "device-catalog.manage",
            Description = "Manage the global device type and model catalog.",
            Roles = new[] { "SystemAdmin" },
            ScopeRequired = false
        },
        new()
        {
            Policy = "alerts.view",
            Description = "View actionable telemetry alerts within allowed scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "alerts.acknowledge",
            Description = "Acknowledge actionable telemetry alerts within allowed scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "alerts.resolve",
            Description = "Resolve actionable telemetry alerts within allowed scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "sync-dead-letters.manage",
            Description = "Inspect, retry, resolve, or ignore Cloud sync dead letters.",
            Roles = ["SystemAdmin"],
            ScopeRequired = false
        },
        new()
        {
            Policy = "artifact.read",
            Description = "Read robot artifact metadata within assigned scope.",
            Roles = ["SystemAdmin", "OrgAdmin", "Technician"],
            ScopeRequired = true
        },
        new()
        {
            Policy = "artifact-template.read",
            Description = "Read and review global robot Lua templates.",
            Roles = new[] { "SystemAdmin", "OrgAdmin" },
            ScopeRequired = false
        },
        new()
        {
            Policy = "artifact-template.manage",
            Description = "Upload, discard Draft, publish, and retire global robot Lua templates.",
            Roles = new[] { "SystemAdmin" },
            ScopeRequired = false
        },
        new()
        {
            Policy = "artifact.upload",
            Description = "Upload, review Lua bytes, and manage robot artifact lifecycle within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "program.read",
            Description = "Read robot programs within assigned organization, store, or kiosk scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Technician" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "program.manage",
            Description = "Manage robot programs within assigned organization, store, or kiosk scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "release.read",
            Description = "Read production configuration releases and authoring options within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Technician" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "release.publish",
            Description = "Publish immutable production configuration releases within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "deployment.read",
            Description = "Monitor production configuration deployments within assigned kiosk scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Technician" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "release.deploy",
            Description = "Deploy production configuration to assigned kiosks.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Technician" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "release.rollback",
            Description = "Deploy a previously validated release to assigned kiosks as rollback.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Technician" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "package.read",
            Description = "Read production package catalog and installation state within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "package.manage",
            Description = "Author and publish global production package versions.",
            Roles = new[] { "SystemAdmin" },
            ScopeRequired = false
        },
        new()
        {
            Policy = "package.install",
            Description = "Install production packages within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "package.fork",
            Description = "Fork package-managed technical configuration within assigned organization.",
            Roles = new[] { "SystemAdmin", "OrgAdmin" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "menu-items.availability.manage",
            Description = "Pause or resume menu-item sales for assigned kiosks without changing menu authoring data.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Staff" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "service-registrations.read",
            Description = "Read pre-tenant service registration requests.",
            Roles = new[] { "SystemAdmin" },
            ScopeRequired = false
        },
        new()
        {
            Policy = "service-registrations.manage",
            Description = "Review, reject, and provision pre-tenant service registrations.",
            Roles = new[] { "SystemAdmin" },
            ScopeRequired = false
        },
        new()
        {
            Policy = "content-pages.read",
            Description = "Read platform-managed public content page drafts and publication state.",
            Roles = new[] { "SystemAdmin" },
            ScopeRequired = false
        },
        new()
        {
            Policy = "content-pages.manage",
            Description = "Author and publish platform-managed public content pages.",
            Roles = new[] { "SystemAdmin" },
            ScopeRequired = false
        }
    };

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> RolesByPolicy = Matrix
        .ToDictionary(item => item.Policy, item => item.Roles, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> GetRoles(string policy)
    {
        if (RolesByPolicy.TryGetValue(policy, out var roles))
        {
            return roles.ToArray();
        }

        throw new InvalidOperationException($"Permission policy '{policy}' is not registered.");
    }

    public static List<string> ResolvePermissionCodes(IEnumerable<string> roleCodes)
    {
        var roles = roleCodes
            .Where(roleCode => !string.IsNullOrWhiteSpace(roleCode))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Matrix
            .Where(permission => permission.Roles.Any(roles.Contains))
            .Select(permission => permission.Policy)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();
    }
}

public sealed class GetPermissionMatrixQueryHandler
{
    public Task<ApiResult<IEnumerable<PermissionMatrixItem>>> HandleAsync(
        GetPermissionMatrixQuery query,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApiResult<IEnumerable<PermissionMatrixItem>>.Success(
            PermissionCatalog.Matrix,
            "Permission matrix retrieved successfully."));
    }
}
