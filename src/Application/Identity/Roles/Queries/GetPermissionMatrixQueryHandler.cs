using Application.Shared.Wrappers;

namespace Application.Identity.Roles.Queries;

public sealed class PermissionMatrixItem
{
    public string Policy { get; init; } = null!;
    public string Description { get; init; } = null!;
    public IReadOnlyList<string> Roles { get; init; } = null!;
    public bool ScopeRequired { get; init; }
}

internal static class PermissionMatrixRules
{
    public static readonly IReadOnlyList<PermissionMatrixItem> Matrix = new List<PermissionMatrixItem>
    {
        new()
        {
            Policy = "accounts.manage",
            Description = "Create, update, disable, assign roles, set password, and send invitations for internal accounts.",
            Roles = new[] { "SystemAdmin" },
            ScopeRequired = false
        },
        new()
        {
            Policy = "accounts.read",
            Description = "View internal accounts within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "organizations.manage",
            Description = "Platform-level organization management (create, activate, disable).",
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
            Policy = "kiosks.view",
            Description = "View kiosk details within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Technician" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "kiosks.manage",
            Description = "Create, activate, or disable kiosks.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Technician" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "kiosks.update",
            Description = "Update kiosk details within assigned scope.",
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
            Roles = new[] { "SystemAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "product-categories.read",
            Description = "Browse the global flat ProductCategory catalog used by product authoring.",
            Roles = new[] { "SystemAdmin", "Manager" },
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
            Roles = new[] { "SystemAdmin", "Manager" },
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
            Roles = new[] { "SystemAdmin", "Manager" },
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
            Roles = new[] { "SystemAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "payments.manage",
            Description = "Manage payment methods and status.",
            Roles = new[] { "SystemAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "roles.view",
            Description = "View roles catalog and static permission matrix.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager" },
            ScopeRequired = false
        },
        new()
        {
            Policy = "role-scope-options.view",
            Description = "View valid organizational scope options for a target role.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager" },
            ScopeRequired = false
        },
        new()
        {
            Policy = "orders.view",
            Description = "View back-office orders within allowed scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Staff" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "orders.manage",
            Description = "Manage orders (cancel unpaid, flag refund-required) within allowed scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Staff" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "refunds.manage",
            Description = "Manage refunds (request, processed, reject, cancel) within allowed scope.",
            Roles = new[] { "SystemAdmin", "Manager", "Staff" },
            ScopeRequired = true
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
            Policy = "inventory.manage",
            Description = "Manage inventory (refill, adjust estimate) within allowed scope.",
            Roles = new[] { "SystemAdmin", "Manager", "Staff", "Technician" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "inventory.configure",
            Description = "Provision, configure, activate/retire, and safely delete kiosk dispenser topology.",
            Roles = new[] { "SystemAdmin", "Manager", "Technician" },
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
            Policy = "devices.view",
            Description = "View devices/hardware details within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "devices.manage",
            Description = "Create, update, status-change, or retire devices/hardware within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Technician" },
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
            Policy = "alerts.manage",
            Description = "Acknowledge and resolve actionable telemetry alerts within allowed scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Technician" },
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
            Roles = ["SystemAdmin", "OrgAdmin"]
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
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "program.manage",
            Description = "Manage robot programs within assigned organization, store, or kiosk scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "release.read",
            Description = "Read production configuration releases and authoring options within assigned scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager" },
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
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "release.rollback",
            Description = "Deploy a previously validated release to assigned kiosks as rollback.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager" },
            ScopeRequired = true
        }
    };
}

public sealed class GetPermissionMatrixQueryHandler
{
    public Task<ApiResult<IEnumerable<PermissionMatrixItem>>> HandleAsync(
        GetPermissionMatrixQuery query,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApiResult<IEnumerable<PermissionMatrixItem>>.Success(
            PermissionMatrixRules.Matrix,
            "Permission matrix retrieved successfully."));
    }
}
