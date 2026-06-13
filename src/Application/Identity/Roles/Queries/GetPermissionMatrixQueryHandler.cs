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
            Description = "Manage products and variants.",
            Roles = new[] { "SystemAdmin", "Manager" },
            ScopeRequired = true
        },
        new()
        {
            Policy = "menus.manage",
            Description = "Manage menus and menu items.",
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
            Description = "View kiosk operations telemetry such as heartbeats and device events within allowed scope.",
            Roles = new[] { "SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician" },
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
