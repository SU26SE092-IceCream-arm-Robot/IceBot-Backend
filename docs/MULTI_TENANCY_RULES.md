# Multi-Tenancy Rules

## Tenant Root

`Organization` is the tenant root.

`Store` belongs to an organization.

`Kiosk` belongs to a store and carries `OrganizationId` as a denormalized filter key. Keep `StoreId` required for the operational hierarchy, and use `OrganizationId` for tenant isolation and query filters.

## Scope Model

Use `TenantScopeType` for configurable data that can exist as global defaults and tenant overrides:

- `Global`
- `Organization`
- `Store`
- `Kiosk`
- `Device`

Resolution priority:

```text
Device > Kiosk > Store > Organization > Global
```

When selecting effective configuration, query the most specific matching row first.

## Scoped Entities

### Product

`Product` supports global catalog definitions and tenant-specific overrides.

Use:

- `ScopeType`
- `OrganizationId`
- `StoreId`
- `KioskId`
- `TemplateProductId`

Recommended uniqueness:

- `ScopeType + OrganizationId + StoreId + KioskId + Code`

Global product templates should have all scope IDs null.

### Recipe

`Recipe` follows product scoping and can be overridden per tenant or kiosk.

Use:

- `ScopeType`
- `OrganizationId`
- `StoreId`
- `KioskId`
- `TemplateRecipeId`
- `ProductId`
- `Version`

Recommended uniqueness:

- `ScopeType + OrganizationId + StoreId + KioskId + ProductId + Version`
- or `ScopeType + OrganizationId + StoreId + KioskId + Code + Version`

### ProductOption

`ProductOption` is scoped at global or organization level for now.

Use:

- `ScopeType`
- `OrganizationId`
- `TemplateProductOptionId`

Avoid store/kiosk option overrides unless there is a real pricing or availability need.

### RobotProgram

`RobotProgram` already supports the full override hierarchy.

Use:

- `ScopeType`
- `OrganizationId`
- `StoreId`
- `KioskId`
- `DeviceId`
- `TemplateProgramId`

Robot program resolution should use:

```text
Device > Kiosk > Store > Organization > Global
```

Kiosk/device-scoped robot programs may contain local Fairino point/frame references and backup snapshots specific to a physical installation.

## Operational Entities

Operational rows should carry `OrganizationId` when they need direct tenant filtering/reporting without joining through `Kiosk`.

Already applied:

- `Order`
- `StockMovement`
- `Kiosk`

Consider adding later when implementing persistence/query filters:

- `RobotJob`
- `Alert`
- `MaintenanceTicket`
- `OperationLog`
- `KioskHeartbeat`
- `DeviceEvent`
- `SyncEventInbox`
- `SyncDeadLetter`

These can be populated from the kiosk/store hierarchy at write time.

## Global Query Filter Guidance

For EF Core, apply tenant filters to entities implementing `IOrganizationScoped`.

Recommended behavior:

```text
Global/shared config:
OrganizationId == null

Tenant-owned data:
OrganizationId == currentOrganizationId

Effective config queries:
OrganizationId == null OR OrganizationId == currentOrganizationId
then order by scope specificity
```

Do not use global filters blindly for admin/platform queries. Platform admin screens need explicit bypass behavior.

## Ownership Boundary

Cloud/platform owns:

- global product templates
- global recipes
- global robot program templates
- payment methods
- device types
- roles

Organization owns:

- scoped products
- scoped recipes
- scoped robot programs
- stores
- kiosks
- orders
- stock movements
- operational reports

Kiosk/edge may create or update:

- kiosk/device-scoped robot programs and local Fairino point/frame snapshots
- robot jobs and steps
- ingredient dispenser state
- stock movements
- device/robot events
- sync inbox/dead letter records

When edge creates tenant-owned rows, it must include `OrganizationId` if known. If not known at the edge, cloud ingestion must enrich it from `KioskId` before storing/reporting.
