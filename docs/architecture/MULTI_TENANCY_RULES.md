# Multi-Tenancy Rules

This document defines tenant isolation and configurable override scope for organizations, stores, kiosks, devices, catalog, menu, recipe, and robot configuration.

## Search Keywords

`multi-tenancy`, `tenant`, `tenant isolation`, `Organization`, `Store`, `Kiosk`, `TenantScopeType`, `Global`, `Organization scope`, `Store scope`, `Kiosk scope`, `Device scope`, `global query filter`, `Product`, `ProductVariant`, `Recipe`, `Menu`, `RobotProgram`, `KioskRecipeExecutionProfile`, `OrgAdmin`

## Tenant Root

`Organization` is the tenant root.

`Store` belongs to an organization. `Store.OrganizationId` is required (non-nullable) to ensure all stores are bound to an organization.

`Kiosk` belongs to a store. `Kiosk.OrganizationId` and `Kiosk.StoreId` are both required (non-nullable) to ensure all kiosks are bound to an organization and store. When creating or updating a kiosk, validate that `Kiosk.OrganizationId == Store.OrganizationId`. OrganizationId is used for tenant isolation, reporting, and query filters.

## Organization Management

Organization management APIs live under:

```text
/api/v1/management/organizations
```

`SystemAdmin` owns platform-level organization lifecycle:

- create organization
- update all organization fields
- activate organization
- disable organization
- list/view all organizations

`OrgAdmin` is scoped to assigned organizations through `AccountRole`:

```text
RoleCode = OrgAdmin
OrganizationId = organizationId
StoreId = null
KioskId = null
```

`OrgAdmin` can:

- view assigned organization(s)
- update basic profile/contact fields for assigned organization(s)

`OrgAdmin` cannot:

- create organizations
- activate or disable organizations
- change `Code`
- change `Status`
- change legal/platform-managed fields such as `LegalName`, `TaxCode`, or `MetadataJson`
- access organizations outside assigned `AccountRole` scope

Do not infer organization access from email domain. Tenant access must come from scoped roles, not from addresses such as `@gmail.com`, `@company.com`, or `@corp.xyz.vn`.

Organization persistence ports should stay context-specific:

```text
Application.Tenants.Abstractions.IOrganizationStore
Infrastructure.Tenants.Persistence.OrganizationStore
```

Do not place organization-specific persistence in the generic `Infrastructure.Persistence.Repositories` namespace. That namespace is for generic/shared repository infrastructure.

## Store Management

Store management APIs live under:

```text
/api/v1/management/stores
/api/v1/management/organizations/{organizationId}/stores
```

`SystemAdmin` owns platform-level store operations.
`OrgAdmin` owns store management operations within their assigned organization scope.

- **Create Store:** `OrgAdmin` can create stores under their assigned organization.
- **Update Store:** `OrgAdmin` and `Manager` can update store details within their assigned scope. `Code` is immutable.
- **Disable/Activate Store:** `OrgAdmin` can activate or disable stores. Activating a store requires that its parent organization is active. Disabling a store does not cascade disable kiosks.
- Organization-scoped roles can access all stores in that organization.
- Store-scoped roles can access only the assigned store and must not be expanded to the whole organization.

Store persistence ports should stay context-specific:

```text
Application.Tenants.Abstractions.IStoreStore
Infrastructure.Tenants.Persistence.StoreStore
```

Do not place store-specific persistence in the generic repositories namespace.

## Kiosk Management

Kiosk management APIs live under:

```text
/api/v1/management/kiosks
/api/v1/management/stores/{storeId}/kiosks
```

`SystemAdmin` owns platform-level kiosk operations.
`OrgAdmin`, `Manager`, and `Technician` own kiosk management operations within their assigned scope:
- **Create Kiosk:** Can create kiosks under their assigned store. Validates parent store and organization are active, and Kiosk's OrganizationId matches Store's OrganizationId.
- **Update Kiosk:** Can update kiosk details within scope. `Code`, `StoreId`, and `OrganizationId` are immutable.
- **Status Change:** Can change kiosk status. Setting to `Active` requires parent store and organization to be active.

Kiosk-scoped roles (e.g. Technician with KioskId scope) can access only their assigned kiosk.

Kiosk persistence ports:

```text
Application.Tenants.Abstractions.IKioskStore
Infrastructure.Tenants.Persistence.KioskStore
```

Do not place kiosk-specific persistence in the generic repositories namespace.

## Tenant Tree

Tenant tree is a management read model for scope selection and tenant navigation:

```text
GET /api/v1/management/tenant-tree
```

It returns:

```text
Organization
  -> Store
      -> Kiosk
```

Use it for:

- choosing valid `OrganizationId`, `StoreId`, and `KioskId` values when assigning role scope;
- management UI tenant navigation;
- avoiding invalid cross-tenant scope combinations.

Do not use `tenant-tree` as an operations overview endpoint. Keep revenue, alerts, runtime state, inventory, and dashboard metrics in separate overview/reporting APIs.

REST is sufficient for this endpoint. Do not introduce GraphQL or OData for tenant tree unless broader read-query requirements appear.

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

### ProductVariant

`ProductVariant` belongs to a `Product` and represents a sellable/recipe-bearing variant such as size, portion, flavor, or package.

Use:

- `ProductId`
- `Code`
- `VariantType`
- `SizeCode` when the variant is size-based

Recommended uniqueness:

- `ProductId + Code`

Tenant ownership is inherited from the parent product. Do not duplicate tenant scope fields on `ProductVariant` unless variant overrides need independent scope later.

### Recipe

`Recipe` follows product variant scoping and can be overridden per tenant or kiosk.

Use:

- `ScopeType`
- `OrganizationId`
- `StoreId`
- `KioskId`
- `TemplateRecipeId`
- `ProductVariantId`
- `Version`

Recommended uniqueness:

- `ScopeType + OrganizationId + StoreId + KioskId + ProductVariantId + Version`
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

### KioskRecipeExecutionProfile

`KioskRecipeExecutionProfile` is kiosk/device-scoped robot configuration. It binds a catalog recipe to a robot program for a concrete kiosk/device context.

Use:

- `OrganizationId`
- `StoreId`
- `KioskId`
- `DeviceId`
- `ProductVariantId`
- `RecipeId`
- `RobotProgramId`

Recommended uniqueness:

- `OrganizationId + StoreId + KioskId + DeviceId + RecipeId + Code`

This is the Cloud configuration/backup source that syncs to Edge as a runtime recipe-program binding. Do not put this relationship directly on `Product`, `ProductVariant`, `Recipe`, or `MenuItem`.

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

## Related Docs

- [Boundary Contexts](BOUNDARY_CONTEXTS.md)
- [Authorization Rules](../api/AUTHORIZATION_RULES.md)
- [Data Modeling Rules](../data/DATA_MODELING_RULES.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
