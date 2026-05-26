# Authorization Rules

This document records backend API authorization direction for internal users.

Customer ordering is anonymous in the current system. Customer is a business actor, not an internal account role.

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

## Policy Direction

| Policy | Allowed roles | Notes |
| --- | --- | --- |
| `accounts.manage` | `SystemAdmin` | Internal account and role management |
| `products.manage` | `SystemAdmin`, `Manager` | Product/catalog management. Staff and Technician should not change product pricing/catalog by default |
| `menus.manage` | `SystemAdmin`, `Manager` | Menu, price, promotion, and sellable offer management |
| `payments.manage` | `SystemAdmin`, `Manager` | Payment method/config management |
| `refunds.manage` | `SystemAdmin`, `Manager`, `Staff` | Manual support/refund workflow. Auto provider refund is future work |
| `inventory.operate` | `SystemAdmin`, `Manager`, `Staff` | Refill and stock movement operations |
| `maintenance.manage` | `SystemAdmin`, `Manager`, `Technician` | Maintenance tickets and technical work coordination |
| `robot-config.manage` | `SystemAdmin`, `Technician` | Robot program/config/profile setup |
| `kiosks.manage` | `SystemAdmin`, `Manager`, `Technician` | Kiosk setup and operational configuration |
| `reports.view` | `SystemAdmin`, `Manager`, `OrgAdmin` | Scope filtering must be enforced when scoped authorization is implemented |

## Current Implementation Notes

- Current `ScopedRoleAuthorizationHandler` checks role presence only.
- Route/resource scope matching is deferred until APIs pass `OrganizationId`, `StoreId`, or `KioskId` authorization context.
- When scoped authorization is implemented, role checks must also validate the requested resource scope.
- Do not add `Staff` or `Technician` to product/menu pricing policies unless the business explicitly gives them that responsibility.

## Related Docs

- [Business Flows](../../Docs/BUSINESS_FLOWS.md)
- [API Surface Rules](API_SURFACE_RULES.md)
- [Dependency Rules](DEPENDENCY_RULES.md)
- [Naming Rules](NAMING_RULES.md)
