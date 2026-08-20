# Authorization Rules

This document records backend API authorization direction for internal users.

Customer ordering is anonymous in the current system. Customer is a business actor, not an internal account role.

## Search Keywords

`authorization`, `authz`, `RBAC`, `scoped RBAC`, `role`, `roles`, `SystemAdmin`, `Manager`, `Staff`, `Technician`, `OrgAdmin`, `Organization Admin`, `policy`, `permissions`, `account roles`, `organization scope`, `store scope`, `kiosk scope`, `accounts.manage`, `products.manage`, `menus.manage`, `robot-config.manage`

## Route Surface

API surface ownership, route categories, and examples live in [API Surface Rules](API_SURFACE_RULES.md).

This document only defines authorization direction for those surfaces.

## Internal Roles

| Role code | Meaning |
| --- | --- |
| `SystemAdmin` | System-wide administration, accounts, permissions, security, and platform health |
| `Manager` | Business/operations management across kiosks, reports, menus, pricing, and maintenance coordination |
| `Staff` | On-site operations such as refill, cleaning, status checks, issue reporting, and manual support/refund handling |
| `Technician` | Installation, robot/kiosk setup, technical maintenance, troubleshooting, and device/robot configuration |
| `OrgAdmin` | Organization admin who can view and manage resources within their assigned organization scope |

## OrgAdmin Flow

OrgAdmin is created through internal account onboarding, not public signup.

Recommended flow:

```text
SystemAdmin creates Organization
  -> SystemAdmin creates internal account
  -> assign RoleCode = OrgAdmin with OrganizationId
  -> backend creates invitation link
  -> OrgAdmin accepts invitation
  -> OrgAdmin can access assigned organization scope
```

OrgAdmin scope must be stored through `AccountRole`:

```text
RoleCode = OrgAdmin
OrganizationId = organizationId
StoreId = null
KioskId = null
```

OrgAdmin access must be checked against role scope. Do not infer tenant access from email domain.

## Permission Entity Decision

Do not add a `Permission` entity for v1.

Current authorization uses:

```text
RoleCode
+ AccountRole scope
+ ASP.NET policy name
```

Policy names are treated as permission-like constants for now. This keeps v1 authorization explicit while roles and business flows are still being finalized.

Add `Permission` and `RolePermission` entities only when there is a concrete need for dynamic permission management, such as:

- admins configuring permissions from UI,
- tenant-specific custom roles,
- many custom roles beyond the current internal role set,
- hardcoded policies becoming too large to maintain safely,
- permission changes needing their own audit and lifecycle.

## Implemented RBAC APIs

These APIs are implemented to make RBAC and tenant scope selection easier to manage in FE/admin screens:

```http
GET /api/v1/management/accounts/assignable-role-options
GET /api/v1/management/role-scope-options
GET /api/v1/management/permission-matrix
```

## Implemented Account Access APIs

These account-specific access APIs are implemented:

```http
PUT /api/v1/management/organizations/{organizationId}/accounts/{accountId}/roles
GET /api/v1/management/organizations/{organizationId}/accounts/{accountId}/effective-access
GET /api/v1/me/access
```

Tenant scope/resource lookup lets admins choose valid assignment scopes:

```http
GraphQL tenantTree
GET /api/v1/management/accounts/assignable-role-options
GET /api/v1/management/role-scope-options
```

GraphQL `tenantTree` returns the management-visible tenant hierarchy:

```text
Organization
  -> Store
      -> Kiosk
```

Use it for role scope selection and tenant navigation. This is not dynamic permission management. The previous REST tenant-tree route is intentionally removed to avoid a duplicated API surface with GraphQL.

When assigning roles, the backend must validate scope hierarchy:

```text
Organization exists
Store exists
Kiosk exists
Store.OrganizationId == OrganizationId
Kiosk.StoreId == StoreId
Kiosk.OrganizationId == OrganizationId
```

This is a tenant RBAC usability implementation. It does not add `Permission` or `RolePermission` tables.

## Policy Direction

Register backend authorization policies in `src/WebAPI/Authorization/AuthorizationPolicyExtensions.cs`; do not add feature-specific policy registrations directly to `Program.cs`.

| Policy | Allowed roles | Notes |
| --- | --- | --- |
| `permission-matrix.view` | `SystemAdmin` | View the platform-wide static permission matrix. This policy does not authorize account role assignment. |
| `dashboard.view` | `SystemAdmin`, `OrgAdmin`, `Manager` | View management dashboard metrics within assigned scope |
| `accounts.read` | `SystemAdmin`, `OrgAdmin` | Read internal accounts through an organization-owned route. Results contain only role scopes belonging to that organization. |
| `accounts.manage` | `SystemAdmin`, `OrgAdmin` | Create, update, disable, assign/update roles, set password, and send invitations for organization-owned internal accounts. `OrgAdmin` is limited to accounts with assignable roles and scopes inside the actor's assigned organization; it cannot grant `SystemAdmin` or access another organization. Global `SystemAdmin` provisioning is bootstrap-only. |
| `workforce.staff.read` | `OrgAdmin`, `Manager` | Read Staff-only workforce accounts within the exact Organization or Store scope granted by the same role assignment. This does not expose OrgAdmin, Manager, Technician, SystemAdmin, or mixed-role accounts. |
| `workforce.staff.manage` | `OrgAdmin`, `Manager` | Create, update, scope, invite, deactivate, and reactivate Staff-only workforce accounts inside the actor's exact scope. It does not grant broad account management, role assignment, password management, cross-organization movement, or Technician management. |
| `organizations.manage` | `SystemAdmin` | Platform-level organization management: create organizations; suspend/resume a temporary tenant hold; deactivate/reactivate organization service; and inspect lifecycle history. Tenant actors cannot change Organization lifecycle. |
| `organizations.view` | `SystemAdmin`, `OrgAdmin` | View organizations. OrgAdmin can view/read only their assigned organization(s) |
| `organizations.update` | `SystemAdmin`, `OrgAdmin` | Update organizations. OrgAdmin can update only basic profile/contact info for assigned organization(s); SystemAdmin can update platform-managed fields |
| `stores.view` | `SystemAdmin`, `OrgAdmin`, `Manager` | View stores. Scoped to assigned organization/store |
| `stores.manage` | `SystemAdmin`, `OrgAdmin` | Create, disable, and activate stores. Scoped to assigned organization |
| `stores.update` | `SystemAdmin`, `OrgAdmin`, `Manager` | Update store details. Scoped to assigned organization/store |
| `stores.sales.manage` | `SystemAdmin`, `OrgAdmin`, `Manager` | Pause or resume store sales. Scoped to assigned organization/store |
| `kiosks.view` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | View kiosks. Scoped to assigned organization/store/kiosk |
| `kiosks.manage` | `SystemAdmin`, `OrgAdmin` | Create and change kiosk lifecycle status. Scoped to assigned organization/store/kiosk |
| `kiosks.update` | `SystemAdmin`, `OrgAdmin`, `Manager` | Update kiosk business and location details. Scoped to assigned organization/store/kiosk |
| `kiosks.operations.manage` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | Change operational and maintenance state without changing kiosk lifecycle or location metadata |
| `devices.view` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | View devices/hardware details within assigned scope |
| `devices.manage` | `SystemAdmin`, `OrgAdmin`, `Technician` | Create, update, replace, or retire physical devices/hardware within assigned scope |
| `devices.operations.manage` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | Change the operational status of a non-retired device within assigned scope |
| `execution-endpoints.manage` | `SystemAdmin`, `OrgAdmin`, `Technician` | Create or retire Edge execution endpoints within assigned scope |
| `execution-endpoints.operations.manage` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | Disable or reactivate Edge execution endpoints within assigned scope |
| `execution-endpoints.provision` | `SystemAdmin`, `OrgAdmin`, `Technician` | Provision endpoint identity and profile without exposing private key material |
| `execution-endpoints.credentials.manage` | `SystemAdmin`, `OrgAdmin`, `Technician` | Rotate mTLS identity and provision, rotate, or revoke MQTT credentials |
| `device-catalog.read` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | Read the global DeviceType/DeviceModel lookup catalog; no tenant scope is required |
| `device-catalog.manage` | `SystemAdmin` | Create/update/deactivate DeviceType and create/update/retire DeviceModel records |
| `artifact.read` | `SystemAdmin`, `OrgAdmin` | List and inspect metadata for organization-owned robot Lua artifacts |
| `artifact.upload` | `SystemAdmin`, `OrgAdmin` | Upload, request short-lived Lua review URLs, discard Draft, publish, and retire organization-owned robot Lua artifacts |
| `artifact-template.read` | `SystemAdmin`, `OrgAdmin` | List and review global robot Lua templates; templates cannot execute directly |
| `artifact-template.manage` | `SystemAdmin` | Upload, discard Draft, publish, and retire global robot Lua templates |
| `program.read` | `SystemAdmin`, `OrgAdmin`, `Manager` | Read robot programs within the actor's matching organization/store/kiosk scope |
| `program.manage` | `SystemAdmin`, `OrgAdmin` | Author, publish, and retire robot programs within the actor's matching organization/store/kiosk scope |
| `release.read` | `SystemAdmin`, `OrgAdmin`, `Manager` | Read production configuration releases and authoring options within the actor's matching organization scope |
| `release.publish` | `SystemAdmin`, `OrgAdmin` | Author, publish, and retire organization-owned production configuration releases |
| `deployment.read` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | Monitor configuration deployment state and failure details within assigned kiosk scope |
| `release.deploy` | `SystemAdmin`, `OrgAdmin`, `Manager` | Request configuration deployment to assigned kiosks. The request requires an operator reason and backend records actor plus matching authorization scope in the kiosk operation log. |
| `package.read` | `SystemAdmin`, `OrgAdmin`, `Manager` | Read published package catalog and installation state within tenant scope |
| `package.manage` | `SystemAdmin` | Author and publish global production package versions |
| `package.install` | `SystemAdmin`, `OrgAdmin` | Preview and install published packages within tenant scope |
| `package.fork` | `SystemAdmin`, `OrgAdmin` | Convert package-managed technical configuration into an explicit organization fork |
| `release.rollback` | `SystemAdmin`, `OrgAdmin`, `Manager` | Request a new deployment from a previously Active Full Edge release or low-cost artifact set within assigned scope. The request requires a reason and the client-observed active deployment id; backend rejects a stale observation and audits the request. |
| `tenant-tree.view` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | View tenant hierarchy for RBAC scope selection and management navigation |
| `product-templates.read` | `SystemAdmin`, `OrgAdmin`, `Manager` | Browse global product templates for cloning into an assigned organization |
| `product-templates.manage` | `SystemAdmin` | Manage global product templates; tenant roles cannot author or mutate global catalog rows |
| `products.manage` | `SystemAdmin`, `OrgAdmin`, `Manager` | Manage organization-owned products and variants within assigned organization/store/kiosk scope |
| `product-categories.read` | `SystemAdmin`, `OrgAdmin`, `Manager` | Browse the global flat ProductCategory catalog used by product authoring |
| `product-categories.manage` | `SystemAdmin` | Create, update, activate/deactivate, and safely delete unreferenced global ProductCategory definitions |
| `ingredients.read` | `SystemAdmin`, `OrgAdmin`, `Manager` | Browse the global ingredient reference catalog used by recipe authoring |
| `ingredients.manage` | `SystemAdmin` | Create, update, activate/deactivate, and safely delete unreferenced global ingredient definitions |
| `menus.manage` | `SystemAdmin`, `OrgAdmin`, `Manager` | Manage organization-owned menus, prices, promotions, and sellable offers within assigned scope |
| `orders.view` | `OrgAdmin`, `Manager`, `Staff` | View back-office orders within assigned tenant scope; SystemAdmin uses aggregate platform reporting instead of tenant order detail. |
| `orders.fulfillment.manage` | `OrgAdmin`, `Manager`, `Staff` | Record manual and packaged-item fulfillment outcomes within assigned tenant scope. |
| `orders.intervention.manage` | `OrgAdmin`, `Manager` | Cancel orders, redispatch execution, and request production remakes within assigned tenant scope. |
| `orders.refund-flag` | `OrgAdmin`, `Manager`, `Staff` | Mark an order as requiring refund review within assigned tenant scope. |
| `payments.manage` | `OrgAdmin`, `Manager` | Tenant payment-session intervention workflows within assigned scope. |
| `payment-methods.manage` | `SystemAdmin` | Global payment-method catalog status management |
| `refunds.view` | `OrgAdmin`, `Manager`, `Staff` | View manual support/refund records within assigned tenant scope. |
| `refunds.request` | `OrgAdmin`, `Manager`, `Staff` | Request manual compensation with a reason and idempotency key. |
| `refunds.process` | `OrgAdmin`, `Manager` | Mark money refunded, reject, or cancel a manual compensation record. |
| `platform.organization-sales.view` | `SystemAdmin` | Read organization-level aggregate sales collections for platform administration and reporting. It does not authorize tenant order, payment, refund, customer, or provider-transaction detail. |
| `inventory.view` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | View dispenser states and stock movements within assigned scope |
| `inventory.refill.manage` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff` | Request, start, complete, or cancel an audited physical refill task within assigned scope |
| `inventory.adjust.manage` | `SystemAdmin`, `OrgAdmin`, `Manager` | Correct an inventory estimate outside the refill workflow within assigned scope |
| `inventory.configure` | `SystemAdmin`, `OrgAdmin`, `Technician` | Provision balances or dispenser topology, configure tracking, activate/retire states, and delete only unused states within assigned scope |
| `operations.view` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | View kiosk heartbeat history, device events, and curated operation logs within assigned scope |
| `operations.diagnostics` | `SystemAdmin`, `Technician` | View raw operation-log payloads and order execution diagnostics within assigned kiosk scope |
| `payments.diagnostics.view` | `OrgAdmin`, `Manager` | View bounded payment-session diagnostics within assigned scope; raw provider request/response payloads are never returned |
| `notifications.view` | `SystemAdmin`, `OrgAdmin`, `Manager` | View normal notification delivery status and retry evidence within assigned scope; message content and provider diagnostics are excluded |
| `notifications.manage` | `SystemAdmin`, `OrgAdmin`, `Manager` | Requeue permanently failed notification deliveries within assigned scope; reason and actor are audited |
| `maintenance.view` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | View maintenance tickets within assigned scope |
| `maintenance.create` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | Create maintenance tickets within assigned scope |
| `maintenance.manage` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | Manage, assign, resolve, and close maintenance tickets within assigned scope. Staff can create/view tickets but cannot assign or resolve by default |
| `sync-dead-letters.manage` | `SystemAdmin` | Inspect retry audit, replay supported sync event types, and resolve/ignore Cloud dead letters. Raw replay control is intentionally not tenant-admin self-service in V1 |
| `alerts.view` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | View actionable telemetry alerts within assigned scope |
| `alerts.acknowledge` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | Acknowledge actionable telemetry alerts within assigned scope |
| `alerts.resolve` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | Resolve actionable telemetry alerts with an outcome/reason within assigned scope |
| `robot-config.manage` | `SystemAdmin`, `Technician` | Robot program/config/profile setup |
| `reports.view` | `SystemAdmin`, `Manager`, `OrgAdmin` | Scope filtering must be enforced when scoped authorization is implemented |

## Current Implementation Notes

- Current `ScopedRoleAuthorizationHandler` checks role presence only.
- Scoped authorization evaluates role and resource scope from the same `UserRoleScope`. A privileged role in one tenant cannot borrow organization, store, or kiosk ids assigned to another role.
- Management list queries pass role-specific effective scope sets into persistence filters. Sensitive read-by-id and mutation queries should include the same scope predicate and return `404` when the resource is outside that scope.
- Route/resource authorization must validate requested scope before returning scoped tenant data or applying a state transition.
- Account read APIs use `accounts.read` and must remain scope-filtered for non-`SystemAdmin` callers. Account mutation APIs use `accounts.manage`; `OrgAdmin` can mutate only an account whose every active role is inside the caller's own organization scope. This prevents a shared or cross-organization account from being modified through one matching role.
- Workforce Staff APIs are separate from account-management APIs. A Manager may manage only an account whose active roles are exclusively `Staff`, never itself, and only when every target Staff scope is reachable from the same Organization or Store assignment that grants workforce access. A kiosk Staff assignment must include its parent Store id; backend validates Organization -> Store -> Kiosk ownership from persisted tenant data.
- `GET /management/organizations/{organizationId}/accounts/{accountId}/effective-access` uses `accounts.read` and returns only the target account's active role scopes and effective ids for that organization.
- `GET /management/accounts/assignable-role-options` is account-authoring input, not a global role-management surface. It returns only roles the current `accounts.manage` actor may assign, with required scope metadata. `GET /management/role-scope-options` is the second step after selecting one of those roles; mutation handlers still validate the assignment.
- Effective access does not expand organization scope into store/kiosk ids. Use GraphQL `tenantTree` or the account-authoring scope lookup for UI tree display.
- `GET /me/access` is a self-inspection endpoint based on current access-token claims. It returns `permissionCodes` plus permission-specific `permissionScopes`; clients must not infer permissions or their scopes from role names. Scope tuples are derived only from role assignments that grant that permission, preventing a permission from one role from borrowing another role's tenant scope. Refresh the token after role changes to see updated access.
- `permissionScopes[].isGlobal` is true only for System Admin access, a globally assigned granting role, or a permission whose catalog entry has `ScopeRequired = false`. A scoped permission with no matching tuple is not usable for a scoped UI action even if its code appears in `permissionCodes`; backend resource authorization remains authoritative.
- `GET /management/maintenance-tickets/{ticketId}/assignee-options` requires `maintenance.manage`, derives organization/store/kiosk from the ticket, and does not grant account-directory access. It returns only Active `Technician` or `Manager` accounts with a role scope matching that ticket. Assignment revalidates the same eligibility at submit time.
- `platform.organization-sales.view` authorizes only `GET /management/organizations/sales-summaries`. The platform operator is assumed authorized to access organization-level aggregate sales metrics for administration and reporting. The response excludes customer identity, order detail, provider transaction detail, and exact payment/refund timestamps.
- `/me/notification-devices` is authenticated self-service only; callers can register, inspect, or invalidate only their own FCM installations.
- `PUT /management/organizations/{organizationId}/accounts/{accountId}/roles` replaces active role assignments for the target account. `POST /management/organizations/{organizationId}/accounts/{accountId}/roles` remains an add/upsert single-role operation. Every submitted role must carry the same `OrganizationId` as the route, including Store/Kiosk-scoped roles.
- Do not add `Staff` or `Technician` to product/menu pricing policies unless the business explicitly gives them that responsibility.

## Related Docs

- [API Surface Rules](API_SURFACE_RULES.md)
- [Identity Onboarding Rules](IDENTITY_ONBOARDING_RULES.md)
- [Dependency Rules](../architecture/DEPENDENCY_RULES.md)
- [Naming Rules](../process/NAMING_RULES.md)
