# Authorization Rules

This document records backend API authorization direction for internal users.

Customer ordering is anonymous in the current system. Customer is a business actor, not an internal account role.

## Route Surface

Use `/management/...` for authenticated internal management APIs.

`management` means the internal management/back-office surface. It does not mean only `Manager` role can call it. Actual permissions are controlled by authorization policies and scoped role claims.

Do not use `/admin/...` for new APIs. `Admin` is a role concept (`SystemAdmin`) and can be confused with route ownership when multiple internal roles can access the same management API.

Use `/me` for the authenticated user's own account profile and security actions only.

Allowed `/me` examples:

- `GET /api/v1/me`
- `PUT /api/v1/me/profile`
- `PUT /api/v1/me/password`

Do not use `/me` as a catch-all for business resources such as orders, kiosks, reports, or maintenance tickets. Those resources should stay in their owning controller and use filters or dedicated use cases.

## Internal Roles

| Role code | Meaning |
| --- | --- |
| `SystemAdmin` | System-wide administration, accounts, permissions, security, and platform health |
| `Manager` | Business/operations management across kiosks, reports, menus, pricing, and maintenance coordination |
| `Staff` | On-site operations such as refill, cleaning, status checks, issue reporting, and manual support/refund handling |
| `Technician` | Installation, robot/kiosk setup, technical maintenance, troubleshooting, and device/robot configuration |
| `LocationOwner` | Placement-location owner who can view activity, usage, and revenue/commission for their scoped location |

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
| `reports.view` | `SystemAdmin`, `Manager`, `LocationOwner` | Scope filtering must be enforced when scoped authorization is implemented |

## Current Implementation Notes

- Current `ScopedRoleAuthorizationHandler` checks role presence only.
- Route/resource scope matching is deferred until APIs pass `OrganizationId`, `StoreId`, or `KioskId` authorization context.
- When scoped authorization is implemented, role checks must also validate the requested resource scope.
- Do not add `Staff` or `Technician` to product/menu pricing policies unless the business explicitly gives them that responsibility.

## Related Docs

- [Business Flows](../../Docs/BUSINESS_FLOWS.md)
- [Dependency Rules](DEPENDENCY_RULES.md)
- [Naming Rules](NAMING_RULES.md)
