# Architecture

IceBot is an ASP.NET Core backend for a multi-location automated vending system with robot arm integration. The current architecture is a modular monolith designed for sync-first edge/cloud operation and future service extraction.

## Architecture Style

The project uses:

- Clean Architecture boundaries at project level.
- Modular Monolith organization inside one deployable backend.
- Bounded-context grouping for business ownership.
- Tactical DDD where domain rules matter.
- CQRS-lite for complex workflows.
- Event-driven integration for sync, robot runtime, payment callbacks, and operational events.
- EF Core as the primary unit-of-work and persistence model.

Do not split into microservices yet. Keep module boundaries clear inside the monolith until domain ownership, transaction boundaries, and sync flows are stable.

## Project Layout

```text
IceBot/
  WebAPI/
  Application/
  Domain/
  Infrastructure/
```

Current compile-time dependency chain:

```text
WebAPI -> Infrastructure -> Application -> Domain
```

This is a pragmatic clean-ish structure. `Domain` remains independent. `Application` owns use cases and contracts. `Infrastructure` owns persistence and external adapters. `WebAPI` owns HTTP concerns.

Detailed dependency rules live in [docs/DEPENDENCY_RULES.md](docs/DEPENDENCY_RULES.md).

## Layer Responsibilities

### WebAPI

Owns presentation and HTTP concerns:

- Controllers and route contracts.
- Middleware.
- Authentication and authorization attributes.
- Swagger/API versioning.
- Request/response shaping.

Controllers should delegate to Application use cases. They should not contain domain rules or persistence logic.

### Application

Owns use-case orchestration:

- Commands and queries.
- Request/response DTOs.
- Validators.
- Application handlers/services.
- Transaction boundaries.
- Idempotency checks at API/use-case boundary.
- Contracts for external dependencies.

Use CQRS-lite for workflows with meaningful behavior: order checkout, payment processing, robot job creation, sync ingestion, and stock reporting. Simple admin CRUD can stay lightweight.

### Domain

Owns business model and invariants:

- Entities.
- Value objects.
- Domain enums.
- Domain methods.
- Base entity abstractions.
- Bounded-context namespaces.

Domain must not depend on WebAPI, Infrastructure, EF Core, logging, messaging, or external SDKs.

Domain context ownership is documented in [docs/BOUNDARY_CONTEXTS.md](docs/BOUNDARY_CONTEXTS.md).

### Infrastructure

Owns technical implementation:

- `IceBotDbContext`.
- EF Core mappings and migrations.
- PostgreSQL persistence.
- External provider adapters.
- Farino robot SDK adapter.
- Payment provider adapters.
- Sync/inbox/dead-letter workers.
- Background jobs and technical integrations.

Infrastructure may reference Application and Domain because it implements application contracts and persists domain entities.

## Request Flow

Typical API flow:

```text
HTTP request
  -> WebAPI controller
  -> Application command/query handler
  -> Domain entity methods/invariants
  -> Infrastructure persistence/adapters
  -> DbContext SaveChangesAsync
  -> ApiResult<T> / PagedResult<T>
```

Cross-cutting WebAPI pipeline:

- `CorrelationIdMiddleware`
- `GlobalExceptionMiddleware`
- `RequestResponseLoggingMiddleware`
- Authentication
- Authorization

## Persistence And Transactions

EF Core `IceBotDbContext` is the primary unit of work.

Default approach:

- Use DbContext directly in application handlers for simple use cases.
- Commit once at the use-case boundary.
- Use explicit transactions when a use case spans multiple writes that must succeed together.
- Add focused repositories only for complex aggregate queries or persistence behavior that repeats across use cases.
- Keep repository abstractions thin when they exist. They should support rich handlers, not replace them.

When external systems are involved, do not hold a database transaction across network calls. Persist intent/state first, call the external provider, then persist result or retry state.

Avoid a global generic repository/service/controller stack. It creates long signatures and hides domain decisions. If an existing generic repository must stay, reshape it into a thin persistence helper instead of adding business behavior to it.

## Edge-Cloud Model

IceBot is sync-first and must tolerate intermittent connectivity.

Edge/kiosk runtime owns:

- Robot execution.
- Local device communication.
- Telemetry capture.
- Temporary operation while offline.
- Kiosk/device-scoped calibration updates.

Cloud/backend owns:

- Organization/store/kiosk management.
- Global catalog/configuration templates.
- Reporting and monitoring.
- Payment integration.
- Central sync coordination.

Synchronization is event-oriented. Inbound processing should use inbox, idempotency keys, correlation ids, causation ids, retry state, and dead-letter handling.

## Event-Driven Patterns

Use events for integration and runtime evidence, not as a blanket replacement for domain state.

Recommended patterns:

- Inbox for incoming edge/provider events.
- Dead letter for failed sync/event processing.
- Append-only event tables for robot/device/runtime evidence.
- Retry with typed status/retry columns.
- Idempotency keys for public API and provider calls.
- Correlation and causation ids for traceability.

Outbox can be added when the system starts publishing reliable integration events from local transactions.

See [Idempotency and Retry Rules](docs/IDEMPOTENCY_RETRY_RULES.md).

## JSON Fields

JSON fields are allowed for robot SDK payloads, provider payloads, snapshots, and metadata. They must be classified as configuration, immutable snapshot, append-only payload/debug, or metadata extension.

Query-critical or invariant-critical values should be promoted to typed columns.

See [JSON Field Rules](docs/JSON_FIELD_RULES.md).

## Multi-Tenancy

`Organization` is the tenant root. `Store` belongs to an organization. `Kiosk` belongs to a store and carries `OrganizationId` for tenant filtering.

Configurable data can use `TenantScopeType`:

```text
Device > Kiosk > Store > Organization > Global
```

Tenant filters should be explicit and safe for admin/platform queries.

See [Multi-Tenancy Rules](docs/MULTI_TENANCY_RULES.md).

## API And Observability

API conventions:

- Keep route contracts stable unless explicitly changed.
- Use commands for state changes.
- Use queries for reads.
- Use explicit nested routes for parent-child resources.
- Use `ApiResult<T>` for normal responses.
- Use `PagedResult<T>` for paged list responses.

Operational concerns:

- Correlation id per request.
- Global exception handling.
- Request/response logging with sensitive data masking.
- Serilog file/console logs.
- Swagger for API visibility.

## Design Constraints

Prefer:

- Bounded-context placement for new domain concepts.
- Thin WebAPI controllers.
- Application handlers/use cases for orchestration.
- Rich handlers with thin repositories for persistence helper behavior.
- Typed columns for workflow-critical state.
- Snapshots when runtime history must not depend on mutable catalog/configuration.

Avoid:

- Microservices before module boundaries are stable.
- Generic `Repository<TEntity, TKey>` and `BaseService<TEntity, TKey>` everywhere.
- Generic controllers for domain workflows.
- Event sourcing for the whole system.
- Hidden source-of-truth JSON payloads.
- Soft delete for append-only events, logs, and ledgers.
- Loading and serializing large EF navigation graphs.

## Documentation Map

- [Boundary Contexts](docs/BOUNDARY_CONTEXTS.md)
- [Dependency Rules](docs/DEPENDENCY_RULES.md)
- [Naming Rules](docs/NAMING_RULES.md)
- [Multi-Tenancy Rules](docs/MULTI_TENANCY_RULES.md)
- [Idempotency and Retry Rules](docs/IDEMPOTENCY_RETRY_RULES.md)
- [JSON Field Rules](docs/JSON_FIELD_RULES.md)
