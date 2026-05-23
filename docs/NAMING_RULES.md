# Naming Rules

This document defines naming conventions for IceBot domain, application, infrastructure, and API code. Prefer names that describe business ownership and runtime behavior instead of technical convenience.

## General Rules

- Use English names in code.
- Use PascalCase for types, methods, properties, enums, and enum values.
- Use camelCase for local variables and parameters.
- Use `Async` suffix for async methods that return `Task` or `ValueTask`.
- Avoid abbreviations unless they are stable domain terms, such as `Id`, `API`, `SDK`, `JWT`, or `URL`.
- Prefer precise domain names over generic names such as `Data`, `Info`, `Manager`, `Helper`, or `Processor`.
- Do not reuse names from old projects if they do not match the current domain model.

## Bounded Contexts

Folder and namespace names should follow bounded context ownership:

```text
Domain.Orders
Domain.Payments
Domain.RobotConfiguration
Domain.RobotRuntime
Domain.Inventory
Domain.Devices
Domain.Tenants
Domain.Sync
```

Use singular entity names:

```csharp
Order
OrderItem
RobotJob
RobotProgram
StockMovement
```

Use plural folder names only for grouping:

```text
Entities/
Enums/
ValueObjects/
```

## Entity Base Names

Use the existing base entity names as semantic boundaries:

- `GuidEntity`: entity with app-generated `Guid` id.
- `LongEntity`: catalog/reference entity with database-generated `long` id.
- `BusinessEntity`: mutable business record with audit and soft delete.
- `CatalogEntity`: stable reference/catalog row with `Code`, `Name`, display order, and audit.
- `AppendOnlyEntity`: append-only audit/event-style record without soft delete.
- `AppendOnlySyncEntity`: append-only record that participates in edge-cloud sync.
- `RobotConfigurationEntity`: versioned robot configuration that can sync to edge.
- `RobotRuntimeAggregateEntity`: mutable runtime aggregate for robot/operation workflows.

Do not introduce another generic base such as `BaseFullEntity`. If a new base type is needed, name it after behavior, not inheritance convenience.

## Id Fields

Use `Id` for the primary key.

`GuidEntity` uses UUID v7 by default through `Domain.Common.GuidId.New()`.
This keeps the CLR/database type as `Guid`/PostgreSQL `uuid`, but makes new IDs time-ordered for better B-tree locality, write performance, and operational debugging.

Use UUID v7 for distributed/offline-created records, runtime records, append-only events, sync records, orders, payments, robot jobs, and tenant/topology entities.

Keep `LongEntity` for stable catalog/reference tables that do not need offline/global id creation.

Do not use primary keys as secrets. If a public opaque token is needed, add a dedicated token/code field and hash it when appropriate.

Use `{EntityName}Id` for foreign keys:

```csharp
OrderId
KioskId
RobotJobId
PaymentTransactionId
```

Use nullable foreign keys only when the relationship is genuinely optional.

Use these external id names consistently:

- `Client...Id`: id created by tablet/POS/client before backend persistence.
- `Provider...Id`: id created by an external payment/provider system.
- `External...Id`: id from a non-owned external system when provider-specific naming is not appropriate.
- `EventId`: source event/message id.
- `SourceEventId`: upstream event that caused a state/ledger record.
- `CorrelationId`: traces one business flow.
- `CausationId`: command/event that caused the current command/event.

See [Idempotency and Retry Rules](IDEMPOTENCY_RETRY_RULES.md).

## Status And Enums

Use enum names that include the owning concept:

```csharp
OrderStatus
PaymentTransactionStatus
RobotJobStatus
MaintenanceTicketStatus
```

Do not use raw `string Status` for stable domain states.

Use strings only for vendor/extensible values:

```csharp
VendorErrorCode
ExternalEventType
Provider
ProductType
DeviceCategory
```

Context-specific enums should live in the owning context. Shared primitives may live in `Domain.Common.Enums` only when genuinely cross-context.

## Audit, Soft Delete, And Sync Fields

Use audit names from `IAuditable`:

```csharp
CreatedAt
UpdatedAt
CreatedByAccountId
UpdatedByAccountId
```

Use soft-delete names from `ISoftDeletable`:

```csharp
DeletedAt
DeletedByAccountId
```

Do not use old names such as:

```text
ModifiedAt
IsDeleted
DeletedBy
```

Use sync names from `IRobotSyncEntity`:

```csharp
OriginNodeId
Version
SyncedAt
```

## Repository Names

Application persistence contracts belong under:

```text
Application/Abstractions/Persistence
```

Infrastructure implementations belong under:

```text
Infrastructure/Persistence/Repositories
```

Use repository names only for persistence boundaries:

```csharp
IBaseRepository<TEntity>
BaseRepository<TEntity>
IRobotJobRepository
RobotJobRepository
```

Repositories should stay thin. They may expose query composition and persistence operations, but should not contain workflow transitions or business decisions.

Do not create generic service/controller names such as:

```text
BaseService<TEntity, TKey>
GenericController<TEntity, TKey>
CrudManager
```

## Application Names

Use feature/use-case names:

```text
CreateOrder
ProcessPaymentCallback
CreateRobotJob
RecordStockMovement
RetrySyncEvent
```

Recommended type suffixes:

- `Command`: state-changing request.
- `Query`: read request.
- `Handler`: use-case implementation.
- `Request`: API/application input DTO.
- `Response`: API/application output DTO.
- `Validator`: validation class.

Examples:

```csharp
CreateOrderCommand
CreateOrderHandler
GetRobotJobQuery
RobotJobResponse
```

Avoid actor-based organization such as `AdminProductService` or `CustomerOrderService` unless the actor is part of the actual domain concept.

## API Names

Application names describe what the system does. WebAPI names describe what the client sees. Do not mirror Application use-case folders one-to-one in WebAPI.

Controller names should follow resource/capability names:

```csharp
OrdersController
PaymentsController
RobotJobsController
KiosksController
```

Route names should be stable and resource-oriented:

```text
/api/v1/orders
/api/v1/robot-jobs
/api/v1/kiosks/{kioskId}/heartbeats
```

Action method names may map to use cases:

```text
OrdersController.Create -> CreateOrderCommand
OrdersController.Cancel -> CancelOrderCommand
RobotJobsController.Retry -> RetryRobotJobCommand
```

Keep public route changes explicit and intentional.

## JSON Field Names

Use JSON suffixes by role:

- `*ConfigJson`: source-of-truth configuration.
- `*SettingsJson`: source-of-truth settings.
- `*ParametersJson`: command/robot parameters.
- `*SnapshotJson`: immutable historical snapshot.
- `PayloadJson`: external event/provider payload.
- `HeadersJson`: external message headers.
- `Raw*Json`: raw request/response evidence.
- `MetadataJson`: optional extension data only.

Source-of-truth configuration JSON should have a matching schema version:

```csharp
ProgramPayloadJson
ProgramPayloadSchemaVersion
```

See [JSON Field Rules](JSON_FIELD_RULES.md).

## Retry And Idempotency Names

Use `IdempotencyKey` for retried client/API commands.

Use processing retry names for infrastructure/event processing:

```csharp
ProcessingAttempts
MaxProcessingAttempts
LastAttemptAt
NextRetryAt
LastError
LockId
LockedUntil
```

Use business retry names for workflow execution:

```csharp
RetryCount
MaxRetries
NextRetryAt
LastErrorCode
LastErrorMessage
```

## File Names

File name should match the primary type name:

```text
Order.cs
OrderStatus.cs
IBaseRepository.cs
BaseRepository.cs
```

Group related small enums by context only when it improves readability. Do not recreate a global `DomainEnums.cs` dumping ground for context-specific states.

## Related Docs

- [Architecture](../ARCHITECTURE.md)
- [Boundary Contexts](BOUNDARY_CONTEXTS.md)
- [Dependency Rules](DEPENDENCY_RULES.md)
- [Multi-Tenancy Rules](MULTI_TENANCY_RULES.md)
- [Idempotency and Retry Rules](IDEMPOTENCY_RETRY_RULES.md)
- [JSON Field Rules](JSON_FIELD_RULES.md)
- [IoT Contract](IOT_CONTRACT.md)
