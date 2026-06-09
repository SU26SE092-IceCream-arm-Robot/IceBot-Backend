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

## RBAC Support API Backlog

These APIs are useful for making RBAC easier to manage and debug, but they are not required immediately.

Later candidates:

```http
GET /api/v1/management/roles
GET /api/v1/management/authorization/policies
PUT /api/v1/management/accounts/{accountId}/roles
GET /api/v1/management/accounts/{accountId}/effective-access
GET /api/v1/me/access
```

Do not implement these until the management UI or debugging need is concrete.

Current Tenant priority is scope/resource lookup so admins can choose valid tenant scopes.

Immediate tenant candidate:

```http
GET /api/v1/management/tenant-tree
```

`GET /management/tenant-tree` should return the management-visible tenant hierarchy:

```text
Organization
  -> Store
      -> Kiosk
```

Use it for role scope selection and tenant navigation. This is not dynamic permission management.

When assigning roles, the backend must validate scope hierarchy:

```text
Organization exists
Store exists
Kiosk exists
Store.OrganizationId == OrganizationId
Kiosk.StoreId == StoreId
Kiosk.OrganizationId == OrganizationId
```

This is a tenant RBAC usability proposal. It is not a decision to add `Permission` or `RolePermission` tables.

## Policy Direction

| Policy | Allowed roles | Notes |
| --- | --- | --- |
| `accounts.manage` | `SystemAdmin` | Internal account and role management |
| `organizations.manage` | `SystemAdmin` | Platform-level organization management: create, activate, disable organizations |
| `organizations.view` | `SystemAdmin`, `OrgAdmin` | View organizations. OrgAdmin can view/read only their assigned organization(s) |
| `organizations.update` | `SystemAdmin`, `OrgAdmin` | Update organizations. OrgAdmin can update only basic profile/contact info for assigned organization(s); SystemAdmin can update platform-managed fields |
| `stores.view` | `SystemAdmin`, `OrgAdmin`, `Manager` | View stores. Scoped to assigned organization/store |
| `stores.manage` | `SystemAdmin`, `OrgAdmin` | Create, disable, and activate stores. Scoped to assigned organization |
| `stores.update` | `SystemAdmin`, `OrgAdmin`, `Manager` | Update store details. Scoped to assigned organization/store |
| `kiosks.view` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | View kiosks. Scoped to assigned organization/store/kiosk |
| `kiosks.manage` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | Create and change status of kiosks. Scoped to assigned organization/store/kiosk |
| `kiosks.update` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | Update kiosk details. Scoped to assigned organization/store/kiosk |
| `tenant-tree.view` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | View tenant hierarchy for RBAC scope selection and management navigation |
| `products.manage` | `SystemAdmin`, `Manager` | Product/catalog management. Staff and Technician should not change product pricing/catalog by default |
| `menus.manage` | `SystemAdmin`, `Manager` | Menu, price, promotion, and sellable offer management |
| `payments.manage` | `SystemAdmin`, `Manager` | Payment method/config management |
| `refunds.manage` | `SystemAdmin`, `Manager`, `Staff` | Manual support/refund workflow. Auto provider refund is future work |
| `inventory.operate` | `SystemAdmin`, `Manager`, `Staff` | Refill and stock movement operations |
| `maintenance.manage` | `SystemAdmin`, `Manager`, `Technician` | Maintenance tickets and technical work coordination |
| `robot-config.manage` | `SystemAdmin`, `Technician` | Robot program/config/profile setup |
| `reports.view` | `SystemAdmin`, `Manager`, `OrgAdmin` | Scope filtering must be enforced when scoped authorization is implemented |

## Current Implementation Notes

- Current `ScopedRoleAuthorizationHandler` checks role presence only.
- Store management APIs perform service-level scope checks using `OrganizationId` and `StoreId` from role scope claims.
- Other route/resource scope matching is still added incrementally as APIs pass `OrganizationId`, `StoreId`, or `KioskId` authorization context.
- Role checks must validate requested resource scope before returning scoped tenant data.
- Do not add `Staff` or `Technician` to product/menu pricing policies unless the business explicitly gives them that responsibility.

## Related Docs

- [Business Flows](../../../Docs/BUSINESS_FLOWS.md)
- [API Surface Rules](API_SURFACE_RULES.md)
- [Identity Onboarding Rules](IDENTITY_ONBOARDING_RULES.md)
- [Dependency Rules](../architecture/DEPENDENCY_RULES.md)
- [Naming Rules](../process/NAMING_RULES.md)
