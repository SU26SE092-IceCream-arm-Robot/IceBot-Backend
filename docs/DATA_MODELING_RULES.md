# Data Modeling Rules

This document captures small data-modeling rules that are easy to miss during ERD and EF Core changes. These rules are practical guardrails for tables, indexes, constraints, and persistence behavior.

## Search Keywords

`data modeling`, `EF Core`, `PostgreSQL`, `soft delete`, `filtered unique index`, `partial unique index`, `DeletedAt IS NULL`, `nullable unique`, `tenant scope`, `historical snapshot`, `DeleteBehavior.Restrict`, `enum status`, `decimal money`, `JSONB`, `high-volume logs`, `partitioning`, `retention`, `SyncEventInbox index`, `KioskHeartbeats`, `DeviceEvents`, `RobotJobEvents`

## Soft Delete And Unique Indexes

If an entity uses `ISoftDeletable`, unique indexes for reusable business identifiers must filter out deleted rows.

Use:

```csharp
.IsUnique().HasFilter("\"DeletedAt\" IS NULL")
```

For nullable unique fields, combine both conditions:

```csharp
.IsUnique().HasFilter("\"SerialNumber\" IS NOT NULL AND \"DeletedAt\" IS NULL")
```

Apply this to reusable identifiers such as:

- `Account.UserName`
- `Account.Email`
- `Organization.Code`
- `Store.Code`
- `Kiosk.Code`
- `Product.Code`
- `ProductVariant.Code`
- `Recipe.Code`
- `Menu.Code`
- `MenuItem.Code`
- `RobotProgram.Code`
- `RobotProgramStep.StepCode`
- `KioskRecipeExecutionProfile.Code`
- `Device.Code`
- `Device.SerialNumber`

Do not apply soft-delete filters to immutable evidence or retry keys:

- `IdempotencyKey`
- `EventId`
- `SourceEventId`
- `OrderNumber`
- `TransactionNumber`
- `RefundNumber`
- `JobNumber`
- `TokenHash`
- payment provider callback ids

Reason: those keys protect audit, deduplication, retry, and historical evidence. They should not be reused after deletion.

## Nullable Unique Columns

PostgreSQL allows multiple `NULL` values in a unique index. If uniqueness should apply only when the value exists, use an explicit filter:

```csharp
.HasFilter("\"ProviderTransactionId\" IS NOT NULL")
```

If the entity is also soft-deletable and the identifier is reusable, include `DeletedAt IS NULL`.

## Tenant Scope Consistency

Entities with `OrganizationId`, `StoreId`, `KioskId`, and `ScopeType` need validation that the ids match the declared scope.

Examples:

- `ScopeType = Global`: all scope ids should be null.
- `ScopeType = Organization`: `OrganizationId` should exist; `StoreId` and `KioskId` should be null.
- `ScopeType = Store`: `StoreId` should exist.
- `ScopeType = Kiosk`: `KioskId` should exist.

Database foreign keys only prove the referenced row exists. They do not prove the scope combination is meaningful. Enforce this in admin/application use cases or domain methods.

## Historical Snapshots

Orders, payments, robot jobs, stock movements, and audit/event tables should not rely only on mutable foreign rows for historical truth.

Use snapshots for values that must remain true after catalog/menu/configuration changes:

- product/product variant/menu display name at order time
- unit price and discount at order time
- recipe/config version used for execution
- provider raw payload evidence
- robot program/step execution parameters

Foreign keys are still useful for traceability, but snapshots protect reporting and audit.

## Delete Behavior

Default delete behavior should stay restrictive.

Use `DeleteBehavior.Restrict` unless a cascade is explicitly part of the aggregate lifecycle. This avoids accidental deletion across large navigation graphs.

Soft delete is preferred for mutable business records. Append-only event/evidence records should not be soft-deleted by default.

## Status Fields

Stable workflow states should use enums. Vendor-specific or externally extensible values may stay as strings.

See [Naming Rules](NAMING_RULES.md) for enum and status naming.

## Money And Quantity

Money and inventory quantities must use `decimal`, not `double` or `float`.

Current EF convention sets:

```csharp
decimal(18, 4)
```

If a field needs a different precision, configure it explicitly.

## JSON Columns

JSON fields are allowed for snapshots, provider payloads, robot parameters, and extension metadata. They should not replace typed workflow fields used for validation, querying, idempotency, retry, or status transitions.

Every source-of-truth JSON configuration should have a matching schema version field.

See [JSON Field Rules](JSON_FIELD_RULES.md).

## High-Volume Log And Event Tables

Append-only log, event, heartbeat, and sync tables must be designed for growth before production.

High-volume tables include:

- `KioskHeartbeats`
- `DeviceEvents`
- `OperationLogs`
- `RobotJobEvents`
- `SyncEventInbox`
- `SyncDeadLetters`

Rules:

- Every high-volume table must have a time-based index aligned with its normal query field, such as `ReportedAt`, `OccurredAt`, `ReceivedAt`, or `FailedAt`.
- Kiosk/device scoped logs should include the scope id before the time field in common query indexes, such as `(KioskId, ReportedAt)` or `(DeviceId, OccurredAt)`.
- Background-worker queues must have indexes matching their scan predicate. For example, `SyncEventInbox` needs `(Status, NextRetryAt, LockedUntil)` for retry/lock scans.
- Define retention policy before production. Examples: keep raw heartbeats for a short period, aggregate health metrics, then archive or purge old rows.
- Define a PostgreSQL partition plan for high-volume append-only tables before production. Monthly range partitions by the main time field are the default starting point.
- Do not rely on EF Core fluent configuration alone for partition lifecycle. PostgreSQL partition creation/maintenance should be handled by raw SQL migrations, DBA scripts, or scheduled database maintenance.

Partition key direction:

| Table | Partition field |
| --- | --- |
| `KioskHeartbeats` | `ReportedAt` |
| `DeviceEvents` | `OccurredAt` |
| `OperationLogs` | `OccurredAt` |
| `RobotJobEvents` | `OccurredAt` |
| `SyncEventInbox` | `ReceivedAt` or `OccurredAt`, depending on worker/query ownership |
| `SyncDeadLetters` | `FailedAt` |

## Index Review Checklist

Before finishing a new entity or relationship, check:

- Does the entity use soft delete?
- Do unique business identifiers need `DeletedAt IS NULL`?
- Are nullable unique fields filtered with `IS NOT NULL`?
- Are idempotency/event/provider keys intentionally reusable or immutable?
- Are tenant scope indexes aligned with query patterns?
- Are common list/detail queries covered by non-unique indexes?
- Do high-volume log/event tables have time indexes, retention rules, and a partition plan?
- Are FK delete behaviors restrictive unless cascade is intentional?
- Are historical values snapshotted when mutable references can change?

## Related Docs

- [Architecture](../ARCHITECTURE.md)
- [Working Protocol](WORKING_PROTOCOL.md)
- [Boundary Contexts](BOUNDARY_CONTEXTS.md)
- [Dependency Rules](DEPENDENCY_RULES.md)
- [Naming Rules](NAMING_RULES.md)
- [Idempotency and Retry Rules](IDEMPOTENCY_RETRY_RULES.md)
- [JSON Field Rules](JSON_FIELD_RULES.md)
