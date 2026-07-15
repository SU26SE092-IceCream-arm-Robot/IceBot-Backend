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
GET /api/v1/management/roles
GET /api/v1/management/role-scope-options
GET /api/v1/management/permission-matrix
```

## RBAC Support API Backlog

These APIs are useful for making RBAC easier to manage and debug, but they are not required immediately:

```http
PUT /api/v1/management/accounts/{accountId}/roles
GET /api/v1/management/accounts/{accountId}/effective-access
GET /api/v1/me/access
```

Current Tenant priority is scope/resource lookup so admins can choose valid tenant scopes.

Immediate tenant support:

```http
GraphQL tenantTree
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
| `roles.view` | `SystemAdmin`, `OrgAdmin`, `Manager` | View roles catalog and static permission matrix |
| `role-scope-options.view` | `SystemAdmin`, `OrgAdmin`, `Manager` | View valid organizational scope options for a target role |
| `accounts.read` | `SystemAdmin`, `OrgAdmin`, `Manager` | Read internal accounts. SystemAdmin can read all accounts; OrgAdmin and Manager are scope-filtered |
| `accounts.manage` | `SystemAdmin` | Create, update, disable, assign/update roles, set password, and send invitations for internal accounts |
| `organizations.manage` | `SystemAdmin` | Platform-level organization management: create, activate, disable organizations |
| `organizations.view` | `SystemAdmin`, `OrgAdmin` | View organizations. OrgAdmin can view/read only their assigned organization(s) |
| `organizations.update` | `SystemAdmin`, `OrgAdmin` | Update organizations. OrgAdmin can update only basic profile/contact info for assigned organization(s); SystemAdmin can update platform-managed fields |
| `stores.view` | `SystemAdmin`, `OrgAdmin`, `Manager` | View stores. Scoped to assigned organization/store |
| `stores.manage` | `SystemAdmin`, `OrgAdmin` | Create, disable, and activate stores. Scoped to assigned organization |
| `stores.update` | `SystemAdmin`, `OrgAdmin`, `Manager` | Update store details. Scoped to assigned organization/store |
| `kiosks.view` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | View kiosks. Scoped to assigned organization/store/kiosk |
| `kiosks.manage` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | Create and change status of kiosks. Scoped to assigned organization/store/kiosk |
| `kiosks.update` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | Update kiosk details. Scoped to assigned organization/store/kiosk |
| `devices.view` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | View devices/hardware details within assigned scope |
| `devices.manage` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | Create, update, status-change, replace, or retire devices/hardware; create, configure, provision, disable/reactivate, rotate credentials, or retire execution endpoints within assigned scope |
| `device-catalog.read` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | Read the global DeviceType/DeviceModel lookup catalog; no tenant scope is required |
| `device-catalog.manage` | `SystemAdmin` | Create/update/deactivate DeviceType and create/update/retire DeviceModel records |
| `artifact.read` | `SystemAdmin`, `OrgAdmin` | List and inspect metadata for organization-owned robot Lua artifacts |
| `artifact.upload` | `SystemAdmin`, `OrgAdmin` | Upload, request short-lived Lua review URLs, discard Draft, publish, and retire organization-owned robot Lua artifacts |
| `artifact-template.read` | `SystemAdmin`, `OrgAdmin` | List and review global robot Lua templates; templates cannot execute directly |
| `artifact-template.manage` | `SystemAdmin` | Upload, discard Draft, publish, and retire global robot Lua templates |
| `program.read` | `SystemAdmin`, `OrgAdmin`, `Manager` | Read robot programs within the actor's matching organization/store/kiosk scope |
| `program.manage` | `SystemAdmin`, `OrgAdmin`, `Manager` | Author, publish, and retire robot programs within the actor's matching organization/store/kiosk scope |
| `release.read` | `SystemAdmin`, `OrgAdmin`, `Manager` | Read production configuration releases and authoring options within the actor's matching organization scope |
| `release.publish` | `SystemAdmin`, `OrgAdmin` | Author, publish, and retire organization-owned production configuration releases |
| `deployment.read` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | Monitor configuration deployment state and failure details within assigned kiosk scope |
| `release.deploy` | `SystemAdmin`, `OrgAdmin`, `Manager` | Request configuration deployment to assigned kiosks |
| `package.read` | `SystemAdmin`, `OrgAdmin`, `Manager` | Read published package catalog and installation state within tenant scope |
| `package.manage` | `SystemAdmin` | Author and publish global production package versions |
| `package.install` | `SystemAdmin`, `OrgAdmin`, `Manager` | Preview and install published packages within tenant scope |
| `package.fork` | `SystemAdmin`, `OrgAdmin` | Convert package-managed technical configuration into an explicit organization fork |
| `release.rollback` | `SystemAdmin`, `OrgAdmin`, `Manager` | Request a new deployment from a previously Active Full Edge release or low-cost artifact set within assigned scope |
| `tenant-tree.view` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | View tenant hierarchy for RBAC scope selection and management navigation |
| `product-templates.read` | `SystemAdmin`, `Manager` | Browse global product templates for cloning into an assigned organization |
| `product-templates.manage` | `SystemAdmin` | Manage global product templates; tenant roles cannot author or mutate global catalog rows |
| `products.manage` | `SystemAdmin`, `Manager` | Manage organization-owned products and variants within assigned organization/store/kiosk scope |
| `product-categories.read` | `SystemAdmin`, `Manager` | Browse the global flat ProductCategory catalog used by product authoring |
| `product-categories.manage` | `SystemAdmin` | Create, update, activate/deactivate, and safely delete unreferenced global ProductCategory definitions |
| `ingredients.read` | `SystemAdmin`, `Manager` | Browse the global ingredient reference catalog used by recipe authoring |
| `ingredients.manage` | `SystemAdmin` | Create, update, activate/deactivate, and safely delete unreferenced global ingredient definitions |
| `menus.manage` | `SystemAdmin`, `Manager` | Manage organization-owned menus, prices, promotions, and sellable offers within assigned scope |
| `payments.manage` | `SystemAdmin`, `Manager` | Payment method/config management |
| `refunds.manage` | `SystemAdmin`, `Manager`, `Staff` | Manual support/refund workflow. Auto provider refund is future work |
| `inventory.view` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | View dispenser states and stock movements within assigned scope |
| `inventory.manage` | `SystemAdmin`, `Manager`, `Staff`, `Technician` | Refill dispenser state and adjust inventory estimates within assigned scope |
| `inventory.configure` | `SystemAdmin`, `Manager`, `Technician` | Provision and configure dispenser topology, activate/retire states, and delete only unused states within assigned scope |
| `operations.view` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | View kiosk heartbeat history, device events, and curated operation logs within assigned scope |
| `operations.diagnostics` | `SystemAdmin`, `Technician` | View raw operation-log diagnostic payloads within assigned kiosk scope |
| `maintenance.view` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | View maintenance tickets within assigned scope |
| `maintenance.create` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | Create maintenance tickets within assigned scope |
| `maintenance.manage` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | Manage, assign, resolve, and close maintenance tickets within assigned scope. Staff can create/view tickets but cannot assign or resolve by default |
| `sync-dead-letters.manage` | `SystemAdmin` | Inspect retry audit, replay supported sync event types, and resolve/ignore Cloud dead letters. Raw replay control is intentionally not tenant-admin self-service in V1 |
| `alerts.view` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | View actionable telemetry alerts within assigned scope |
| `alerts.manage` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | Acknowledge and resolve actionable telemetry alerts within assigned scope |
| `robot-config.manage` | `SystemAdmin`, `Technician` | Robot program/config/profile setup |
| `reports.view` | `SystemAdmin`, `Manager`, `OrgAdmin` | Scope filtering must be enforced when scoped authorization is implemented |

## Current Implementation Notes

- Current `ScopedRoleAuthorizationHandler` checks role presence only.
- Store management APIs perform service-level scope checks using `OrganizationId` and `StoreId` from role scope claims.
- Other route/resource scope matching is still added incrementally as APIs pass `OrganizationId`, `StoreId`, or `KioskId` authorization context.
- Role checks must validate requested resource scope before returning scoped tenant data.
- Account read APIs use `accounts.read` and must remain scope-filtered for non-`SystemAdmin` callers. Account mutation APIs use `accounts.manage` and remain `SystemAdmin` only.
- `GET /management/accounts/{accountId}/effective-access` uses `accounts.read` and returns the target account's active role scopes plus the effective ids used by current scoped authorization rules.
- Effective access does not expand organization scope into store/kiosk ids. Use GraphQL `tenantTree` or REST `role-scope-options` for UI tree display.
- `GET /me/access` is a self-inspection endpoint based on the current access token claims. Refresh the token after role changes to see updated access.
- `/me/notification-devices` is authenticated self-service only; callers can register, inspect, or invalidate only their own FCM installations.
- `PUT /management/accounts/{accountId}/roles` replaces active role assignments for the target account. `POST /management/accounts/{accountId}/roles` remains an add/upsert single-role operation.
- Do not add `Staff` or `Technician` to product/menu pricing policies unless the business explicitly gives them that responsibility.

## Related Docs

- [Business Flows](../../../Docs/BUSINESS_FLOWS.md)
- [API Surface Rules](API_SURFACE_RULES.md)
- [Identity Onboarding Rules](IDENTITY_ONBOARDING_RULES.md)
- [Dependency Rules](../architecture/DEPENDENCY_RULES.md)
- [Naming Rules](../process/NAMING_RULES.md)
