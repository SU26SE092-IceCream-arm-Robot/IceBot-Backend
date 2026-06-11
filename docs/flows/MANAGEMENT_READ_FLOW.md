# Management Read Flow

This document describes how management UI reads aggregated backend data through GraphQL read models and focused REST endpoints.

## Search Keywords

`management read flow`, `GraphQL read model`, `dashboard`, `tenantTree`, `orderOverview`, `kioskStatusOverview`, `inventorySummary`, `management dashboard`, `read model aggregation`, `REST read endpoint`

## Flow

```text
Management UI
  -> GraphQL read model / REST read endpoint
  -> Application query handler
  -> scoped store query
  -> DTO/read model result
```

Current GraphQL read model direction:

```text
dashboard
tenantTree
orderOverview
kioskStatusOverview
inventorySummary
```

## Rules

- GraphQL is a read/query surface in the current phase.
- GraphQL resolvers are transport adapters and must delegate to Application query handlers.
- Domain/Application handlers remain the source of business behavior.
- REST remains the command/integration surface for mutations, tablet actions, webhooks, and IoT/edge contracts.
- Do not duplicate the same read surface in both REST and GraphQL unless there is a deliberate client/integration reason.
- Scoped RBAC still applies to management read models.

## Related Docs

- [System Flows](SYSTEM_FLOWS.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Authorization Rules](../api/AUTHORIZATION_RULES.md)
- [Multi-Tenancy Rules](../architecture/MULTI_TENANCY_RULES.md)
