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
- Detailed persistence, index, soft-delete, snapshot, and JSON rules live in [Data Modeling Rules](DATA_MODELING_RULES.md).

## Related Docs

- [Architecture](../ARCHITECTURE.md)
- [Working Protocol](WORKING_PROTOCOL.md)
- [Boundary Contexts](BOUNDARY_CONTEXTS.md)
- [Naming Rules](NAMING_RULES.md)
- [Authorization Rules](AUTHORIZATION_RULES.md)
- [Data Modeling Rules](DATA_MODELING_RULES.md)
- [Multi-Tenancy Rules](MULTI_TENANCY_RULES.md)
- [Idempotency and Retry Rules](IDEMPOTENCY_RETRY_RULES.md)
- [JSON Field Rules](JSON_FIELD_RULES.md)
