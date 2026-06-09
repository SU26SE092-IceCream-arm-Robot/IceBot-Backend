# Dependency Rules

This document defines dependency boundaries for the current modular monolith. The goal is to keep the codebase easy to split later without paying microservice complexity now.

## Search Keywords

`dependency rules`, `clean architecture`, `modular monolith`, `WebAPI`, `Infrastructure`, `Application`, `Domain`, `DbContext`, `unit of work`, `repository`, `thin repository`, `handler`, `controller`, `bounded context`, `layer boundary`, `EF Core`, `external adapter`, `provider adapter`, `microservice-ready`

## Project Dependencies

Current project direction:

```text
WebAPI -> Infrastructure -> Application -> Domain
```

Rules:

- `Domain` must not reference upper layers.
- `Application` may reference `Domain`.
- `Infrastructure` may reference `Application` and `Domain`.
- `WebAPI` may reference `Application` and `Infrastructure`.

`Infrastructure` owns EF Core and external adapters. `Application` owns use-case orchestration. `Domain` owns business rules.

## Domain Rules

Allowed in Domain:

- Entities.
- Value objects.
- Domain enums.
- Domain methods.
- Domain exceptions.
- Base abstractions such as `IAuditable`, `ISoftDeletable`, and sync/scoping interfaces.

Not allowed in Domain:

- EF Core attributes or DbContext usage.
- HTTP concepts.
- Logging framework calls.
- SDK/provider clients.
- Application DTOs.
- Infrastructure services.

## Application Rules

Application should contain:

- Commands and queries.
- Handlers/use cases.
- DTOs.
- Validators.
- Application interfaces for external dependencies.
- Transaction orchestration.

Application should not contain:

- Controller logic.
- EF Core mapping configuration.
- Provider SDK implementation details.
- Large generic CRUD service hierarchies.

DbContext can be used directly by handlers if the project chooses pragmatic EF access. Add repositories only when they express a real persistence boundary or complex reusable query.

When a repository abstraction exists, keep it thin:

- It may expose query composition entry points such as `IQueryable`.
- It may provide basic persistence operations such as add, update, remove, and soft delete.
- It should not own business decisions, workflow transitions, authorization, or response shaping.
- It should not hard-code one base entity shape if the domain uses multiple entity id/audit patterns.
- It should not hide eager-loading behavior behind string include lists.

Do not delete an existing repository abstraction during cleanup unless removal is explicitly requested or agreed as part of the fix. Prefer reshaping the abstraction to match the current architecture.

Repository/store audit conclusion:

- Current `I*Store` contracts are acceptable as thin persistence boundaries.
- The project direction is rich handler plus thin repository/store.
- `BaseRepository` is not the main Application persistence pattern. It may exist only as an Infrastructure helper when a present use case needs repeated low-level EF mechanics.
- Cross-context lookup methods are allowed when they are read-only validation helpers for the owning handler, such as checking parent store/kiosk/product/recipe existence before a command mutates its aggregate.
- These lookup methods must not grow into workflow orchestration, authorization, or response mapping.
- If a store method starts making business decisions, move that rule back to a handler, domain method, or focused rule helper.
- Do not introduce generic repositories to "standardize" store contracts. Standardize naming and behavior instead.

Do not refactor context stores to inherit from `BaseRepository` just because the helper exists. Current stores may keep direct EF Core access when that keeps the use case explicit.

Reusable techniques from older generic repository implementations:

- Use `AsNoTracking()` for read-only queries and projections.
- Keep soft-delete behavior consistent through EF filters or clearly named active/scoped store methods.
- Separate hard delete from business actions such as disable, revoke, cancel, or archive.
- Centralize low-risk timestamp mechanics in `IceBotDbContext.SaveChangesAsync`, not in generic CRUD methods.

Avoid carrying over these generic repository habits:

- string-based include lists;
- generic `GetAll` / `GetById` / `Update` methods as the default workflow API;
- hidden tenant-scope, authorization, state transition, or response mapping logic;
- base entity constraints that do not match all bounded contexts.

If `BaseRepository` is used later, it must not contain authorization, tenant-scope decisions, use-case validation, status transitions, payment/order/robot workflow, response mapping, or `ApiResult<T>` logic.

## Infrastructure Rules

Infrastructure owns:

- `IceBotDbContext`.
- EF Core migrations.
- Provider adapters.
- Fairino robot SDK integration.
- Payment provider integration.
- Sync workers.
- Background jobs.
- Technical persistence concerns.

Infrastructure should not add business rules that belong in Domain. It should translate external/provider details into application/domain concepts.

## WebAPI Rules

WebAPI owns:

- Controllers.
- Route contracts.
- Middleware.
- Authentication/authorization attributes.
- Swagger.
- HTTP request/response formatting.

Controllers should be thin. They should call application handlers/services and return `ApiResult<T>` or `PagedResult<T>`.

## Bounded Context Rules

- Prefer ids and snapshots across contexts.
- Keep navigation collections selective.
- Do not load large object graphs for API responses.
- Context-specific enums stay in that context.
- Shared primitives go in `Domain.Common` only when they are genuinely cross-context.
- Sync infrastructure may consume common idempotency/correlation/version fields, but business contexts should not depend on Sync entities.

## Data Rules

- EF Core `DbContext` is the unit of work.
- Use explicit transactions at use-case boundaries when needed.
- Do not hold database transactions across external network calls.
- `GuidEntity` IDs are application-generated UUID v7 values. Keep the database column type as PostgreSQL `uuid`.
- `LongEntity` IDs remain database-generated `long` values for catalog/reference rows.
- Detailed persistence, index, soft-delete, snapshot, and JSON rules live in [Data Modeling Rules](../data/DATA_MODELING_RULES.md).

## Related Docs

- [Architecture](../../ARCHITECTURE.md)
- [Working Protocol](../process/WORKING_PROTOCOL.md)
- [Boundary Contexts](BOUNDARY_CONTEXTS.md)
- [Naming Rules](../process/NAMING_RULES.md)
- [Authorization Rules](../api/AUTHORIZATION_RULES.md)
- [Data Modeling Rules](../data/DATA_MODELING_RULES.md)
- [Multi-Tenancy Rules](MULTI_TENANCY_RULES.md)
- [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
- [JSON Field Rules](../data/JSON_FIELD_RULES.md)
