# Boundary Contexts

This project keeps one Domain project, but domain entities are grouped by bounded context. The folder and namespace should describe business ownership, not technical implementation.

## Context Map

### Identity

Namespace: `Domain.Identity`

Owns accounts, roles, login devices, and refresh tokens.

Entities:

- `Account`
- `AccountDevice`
- `RefreshToken`
- `Role`

### Tenants

Namespace: `Domain.Tenants`

Owns the business deployment hierarchy: organization, store, kiosk. A kiosk is an edge/business deployment unit, not just a device.

Entities:

- `Organization`
- `Store`
- `Kiosk`

Enums:

- `KioskStatus`
- `TenantScopeType`

`TenantScopeType` is shared by multiple contexts, but it belongs here because it models tenant override scope: global, organization, store, kiosk, device.

### Catalog

Namespace: `Domain.Catalog`

Owns sellable products, product options, recipes, and ingredient definitions used to describe products.

Entities:

- `Product`
- `ProductCategory`
- `ProductOption`
- `OptionGroup`
- `Recipe`
- `RecipeItem`
- `Ingredient`

### Orders

Namespace: `Domain.Orders`

Owns customer order lifecycle and order snapshots.

Entities:

- `Order`
- `OrderItem`
- `OrderStatusHistory`

Orders may reference catalog, payment, kiosk, and robot runtime by id or snapshot, but should not depend on mutable runtime state for historical truth.

### Payments

Namespace: `Domain.Payments`

Owns payment methods, payment attempts, provider callbacks, and refunds.

Entities:

- `PaymentMethod`
- `PaymentTransaction`
- `PaymentCallback`
- `Refund`

Provider payloads are external evidence. Idempotency and retry decisions must use typed columns.

### Robot Configuration

Namespace: `Domain.RobotConfiguration`

Owns robot program definitions, step definitions, calibration data, and versioned robot configuration shipped to edge kiosks.

Entities:

- `RobotProgram`
- `RobotProgramStep`

This context is configuration-time. It should not own runtime execution state.

### Robot Runtime

Namespace: `Domain.RobotRuntime`

Owns robot execution instances and append-only robot execution events.

Entities:

- `RobotJob`
- `RobotJobStep`
- `RobotJobEvent`

Runtime jobs should use snapshots copied from catalog/configuration where needed.

### Devices

Namespace: `Domain.Devices`

Owns physical device catalog, installed devices, device events, and edge telemetry.

Entities:

- `DeviceType`
- `DeviceModel`
- `Device`
- `DeviceEvent`
- `KioskHeartbeat`

`KioskHeartbeat` lives here because it is telemetry emitted by the edge node/device runtime. `KioskStatus` stays in Tenants because it describes the kiosk lifecycle.

### Inventory

Namespace: `Domain.Inventory`

Owns ingredient dispenser state and stock movement reporting.

Entities:

- `IngredientDispenserState`
- `StockMovement`

`Ingredient` remains in Catalog because it defines what a recipe uses. Inventory owns runtime state and quantity movement.

### Operations

Namespace: `Domain.Operations`

Owns operational alerts, maintenance tickets, and operation logs.

Entities:

- `Alert`
- `MaintenanceTicket`
- `OperationLog`

### Sync

Namespace: `Domain.Sync`

Owns edge-cloud sync inbox and dead-letter handling.

Entities:

- `SyncEventInbox`
- `SyncDeadLetter`

Business contexts should not depend on Sync entities. They may expose idempotency, correlation, causation, version, and origin node fields for sync infrastructure to consume.

### Common

Namespace: `Domain.Common`

Owns base entities, domain abstractions, shared exceptions, and truly shared enums.

Allowed here:

- `GuidEntity`
- `LongEntity`
- `BusinessEntity`
- `CatalogEntity`
- `RobotConfigurationEntity`
- `RobotRuntimeAggregateEntity`
- `IAuditable`
- `ISoftDeletable`
- `IRobotSyncEntity`
- `IOrganizationScoped`
- `IStoreScoped`
- `IKioskScoped`
- `DomainRuleException`
- `EntityStatus`
- `SeverityLevel`

Do not place context-specific enums here.

## Dependency Rules

Dependency and cross-layer rules live in [Dependency Rules](DEPENDENCY_RULES.md). This file only defines bounded-context ownership and intentional cross-context references.

## Current Intentional Cross-Context References

- Orders reference Tenants through `OrganizationId`, `StoreId`, and `KioskId`.
- Orders reference Catalog through `ProductId`, `RecipeId`, and item snapshots.
- Payments reference Orders through `OrderId`.
- Robot Runtime references Orders, Robot Configuration, Devices, Catalog recipes, and Tenants by ids/snapshots.
- Inventory references Devices, Tenants, Catalog ingredients, and dispenser state.
- Operations references Accounts, Devices, Orders, Robot Runtime, and Tenants as operational evidence.

These references are acceptable because the current project uses one database and one Domain assembly. They should still be treated as bounded-context boundaries in application services and APIs.

## Related Docs

- [Architecture](../ARCHITECTURE.md)
- [Dependency Rules](DEPENDENCY_RULES.md)
- [Naming Rules](NAMING_RULES.md)
- [Multi-Tenancy Rules](MULTI_TENANCY_RULES.md)
- [Idempotency and Retry Rules](IDEMPOTENCY_RETRY_RULES.md)
- [JSON Field Rules](JSON_FIELD_RULES.md)
