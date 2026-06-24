Exit code: 0
Wall time: 0.1 seconds
Output:
# Boundary Contexts

This project keeps one Domain project, but domain entities are grouped by bounded context. The folder and namespace should describe business ownership, not technical implementation.

## Search Keywords

`bounded context`, `domain ownership`, `Domain.Identity`, `Domain.Tenants`, `Domain.Catalog`, `Domain.SalesCatalog`, `Domain.Orders`, `Domain.Payments`, `Domain.RobotConfiguration`, `Domain.ProductionConfiguration`, `Domain.ProductionExecution`, `Domain.Devices`, `Domain.Inventory`, `Domain.Operations`, `Domain.Sync`, `Domain.Common`, `ProductVariant`, `MenuItem`, `RobotArtifact`, `RobotProgram`, `ConfigurationRelease`, `EdgeCommand`, `SyncEventInbox`

## Bounded Context Ownership

### Ownership Lookup

| Context | Namespace | Owns |
| --- | --- | --- |
| Identity | `Domain.Identity` | accounts, roles, login devices, refresh tokens, password reset requests |
| Tenants | `Domain.Tenants` | organizations, stores, kiosks, tenant scope |
| Catalog | `Domain.Catalog` | product definitions, variants, options, recipes, ingredients |
| Sales Catalog | `Domain.SalesCatalog` | menus, menu items, sellable offers, pricing |
| Orders | `Domain.Orders` | order lifecycle, order items, historical order snapshots |
| Payments | `Domain.Payments` | payment transactions, callbacks, refunds, payment methods |
| Robot Configuration | `Domain.RobotConfiguration` | robot Lua artifacts and reusable robot manifests |
| Production Configuration | `Domain.ProductionConfiguration` | configuration releases, routes, robot bindings and deployment records |
| Production Execution | `Domain.ProductionExecution` | Cloud execution projections from executor evidence |
| Devices | `Domain.Devices` | device catalog, telemetry, heartbeats and kiosk execution endpoints |
| Inventory | `Domain.Inventory` | dispenser state, stock movements |
| Operations | `Domain.Operations` | alerts, maintenance tickets, operation logs |
| Sync | `Domain.Sync` | edge-cloud inbox/dead letters and dispatch-only edge commands |
| Common | `Domain.Common` | base entities, shared abstractions, shared primitives |

### Identity

Namespace: `Domain.Identity`

Owns accounts, roles, login devices, and refresh tokens.

Entities:

- `Account`
- `AccountDevice`
- `PasswordResetRequest`
- `RefreshToken`
- `Role`

`PasswordResetRequest` is separated from `Account` because password recovery has its own token lifecycle, expiry, usage, and audit evidence.

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

Owns product definitions, product variants, product options, recipes, and ingredient definitions used to describe products.

Entities:

- `Product`
- `ProductVariant`
- `ProductCategory`
- `ProductOption`
- `OptionGroup`
- `Recipe`
- `RecipeItem`
- `Ingredient`

### Sales Catalog

Namespace: `Domain.SalesCatalog`

Owns menus and menu items: the products/recipes currently offered for sale in a tenant/store/kiosk context, including sellable price and availability windows.

Entities:

- `Menu`
- `MenuItem`

`MenuItem` is a domain concept, not just a database mapping. It represents a sellable offer that points to Catalog product variant/recipe data and provides order pricing.

### Orders

Namespace: `Domain.Orders`

Owns customer order lifecycle and order snapshots.

Entities:

- `Order`
- `OrderItem`
- `OrderStatusHistory`

Orders may reference catalog, payment, kiosk, and execution evidence by id or snapshot, but should not depend on mutable Edge runtime state for historical truth.

### Payments

Namespace: `Domain.Payments`

Owns payment methods, payment attempts, provider callbacks, and refunds.

Entities:

- `PaymentMethod`
- `PaymentTransaction`
- `PaymentCallback`
- `Refund`

Provider payloads are external evidence. Idempotency and retry decisions must use typed columns.

Current refund phase is manual cash refund. Auto provider refund or payout integration can be added later, but should not be assumed in the first implementation.

### Robot Configuration

Namespace: `Domain.RobotConfiguration`

Owns immutable exported robot Lua artifacts and reusable declared robot manifests.

Entities:

- `RobotProgram`
- `RobotProgramArtifact`
- `RobotArtifact`

This context is configuration-time. It should not own runtime execution state.

`RobotProgramArtifact` owns ordered artifact membership. This context does not persist Blockly trees, teaching points, calibration, motion coordinates or live runtime work.

### Production Configuration

Namespace: `Domain.ProductionConfiguration`

Owns organization-scoped configuration releases, release-owned route snapshots, ordered robot bindings and Cloud rollout acknowledgement records.

Entities:

- `ConfigurationRelease`
- `ExecutionRoute`
- `ExecutionRouteRobotBinding`
- `KioskConfigurationDeployment`

Published releases are immutable. A route links catalog variant/recipe requirements to robot programs through bindings, rather than Catalog holding a direct program id.

### Production Execution

Namespace: `Domain.ProductionExecution`

Owns Cloud read/audit projections created from accepted executor evidence.

Entities:

- `OrderExecutionRecord`
- `ProductionExecutionRecord`

This context does not own an Edge queue, scheduler, workcell lease, local `ProductionJob`, or physical safety transition.

### Devices

Namespace: `Domain.Devices`

Owns physical device catalog, installed devices, device events, and edge telemetry.

Entities:

- `DeviceType`
- `DeviceModel`
- `Device`
- `DeviceEvent`
- `KioskHeartbeat`
- `KioskExecutionEndpoint`

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
- `EdgeCommand`
- `EdgeCommandDeliveryAttempt`

Business contexts should not depend on Sync entities. They may expose idempotency, correlation, causation, version, and origin node fields for sync infrastructure to consume.

### Common

Namespace: `Domain.Common`

Owns base entities, domain abstractions, shared exceptions, and truly shared enums.

Allowed here:

- `GuidEntity`
- `LongEntity`
- `GuidId`
- `BusinessEntity`
- `CatalogEntity`
- `RobotConfigurationEntity`
- `SyncAggregateEntity`
- `IAuditable`
- `ISoftDeletable`
- `IRobotSyncEntity`
- `IOrganizationScoped`
- `IStoreScoped`
- `IKioskScoped`
- `DomainRuleException`
- `EntityStatus`
- `SeverityLevel`

`GuidId.New()` is the shared UUID v7 generator used by `GuidEntity`.

Do not place context-specific enums here.

## Dependency Rules

Dependency and cross-layer rules live in [Dependency Rules](DEPENDENCY_RULES.md). This file only defines bounded-context ownership and intentional cross-context references.

## Current Intentional Cross-Context References

- Orders reference Tenants through `OrganizationId`, `StoreId`, and `KioskId`.
- Orders reference Sales Catalog through `MenuItemId`, and keep Catalog references through `ProductId`, `ProductVariantId`, `RecipeId`, and item snapshots.
- Payments reference Orders through `OrderId`.
- Production Configuration references Catalog and Robot Configuration by ids/snapshots.
- Production Execution retains executor evidence by source command, endpoint, release and order ids.
- Inventory references Devices, Tenants, Catalog ingredients, and dispenser state.
- Operations references Accounts, Devices, Orders, execution evidence, and Tenants as operational evidence.

These references are acceptable because the current project uses one database and one Domain assembly. They should still be treated as bounded-context boundaries in application services and APIs.

## Related Docs

- [Architecture](../../ARCHITECTURE.md)
- [Working Protocol](../process/WORKING_PROTOCOL.md)
- [Dependency Rules](DEPENDENCY_RULES.md)
- [Naming Rules](../process/NAMING_RULES.md)
- [Data Modeling Rules](../data/DATA_MODELING_RULES.md)
- [Multi-Tenancy Rules](MULTI_TENANCY_RULES.md)
- [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
- [JSON Field Rules](../data/JSON_FIELD_RULES.md)
