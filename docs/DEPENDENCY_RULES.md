# Dependency Rules

This document defines dependency boundaries for the current modular monolith. The goal is to keep the codebase easy to split later without paying microservice complexity now.

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

## Infrastructure Rules

Infrastructure owns:

- `IceBotDbContext`.
- EF Core migrations.
- Provider adapters.
- Farino robot SDK integration.
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
- Append-only event/log/ledger tables should not be soft-deleted.
- JSON payloads are not source of truth unless explicitly documented as configuration JSON with schema version.

## Related Docs

- [Architecture](../ARCHITECTURE.md)
- [Boundary Contexts](BOUNDARY_CONTEXTS.md)
- [Multi-Tenancy Notes](../IceBot/Domain/MULTI_TENANCY_NOTES.md)
- [Idempotency and Retry Notes](../IceBot/Domain/IDEMPOTENCY_RETRY_NOTES.md)
- [JSON Field Rules](../IceBot/Domain/JSON_FIELD_NOTES.md)
