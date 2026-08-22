# Catalog Runtime Menu Flow

This document describes how catalog data becomes a sellable runtime menu for the tablet and edge runtime.

## Search Keywords

`catalog runtime menu`, `runtime menu`, `sales catalog`, `menu item`, `product variant`, `recipe`, `edge runtime projection`, `tablet menu`, `CloudSalesCatalog`, `menu sellability`, `machine readiness`

## Flow

```text
Catalog
  -> Product / ProductVariant / Recipe / Ingredient
  -> SalesCatalog Menu / MenuItem
  -> Cloud runtime menu snapshot
  -> Edge runtime projection
  -> Tablet display and checkout
```

## Rules

- Catalog owns product definitions and recipes.
- Sales Catalog owns sellable menu items and prices.
- Cloud runtime-menu reads expose only active organization/store/kiosk catalog data; temporary admission blockers such as store closure, Edge loss, or an occupied customer session return a bounded unavailable state with no sellable items.
- An empty Store schedule means no opening-hours restriction. Once any day is configured, an omitted day is treated as closed; opening is inclusive and closing is exclusive. `OpensAt > ClosesAt` represents an overnight interval that continues until the following day's close time.
- Runtime menu is a static catalog projection overlaid with fresh operational admission evidence; it is not a machine-readiness guarantee retained in cache.
- Kiosk connectivity, lifecycle, store admission, customer-session occupancy, route/readiness/capability, inventory, and menu-item pause are evaluated on every request; the customer receives only safe blocker codes and scopes.
- A kiosk-scoped pause or recovery takes effect without changing shared catalog data. SignalR/cache invalidation improves recovery latency only; authoritative checkout evaluates fresh evidence.
- The optional Redis cache stores static kiosk catalog candidates (`Revision` and items), never request-scoped `SnapshotId`, generated time, inventory, route, or readiness evidence.
- Redis caching is bounded-TTL acceleration, not sales authority: cache failure falls back to the database projection, and checkout still revalidates sellability transactionally. Cache expiry may delay static catalog visibility by at most the configured distributed TTL plus a short process-local TTL.
- Each snapshot has a random request identity and a deterministic content `Revision`. The runtime endpoint returns that revision as `ETag`; clients may revalidate with `If-None-Match` after `ExpiresAt` and receive `304` when sellable content is unchanged.
- Edge projection may include inventory, device, queue, and robot availability.
- Order item snapshots preserve historical sale truth after catalog/menu changes.
- Checkout revalidates Catalog and Sales Catalog under one repeatable-read transaction snapshot before persisting immutable order-item, recipe, and option snapshots.
- Checkout also revalidates the current kiosk menu-item operational pause. A client that retained an older runtime snapshot receives `409` instead of creating a new order for a paused item.
- Product/ProductVariant deletion and Product currency changes are rejected while a non-deleted MenuItem references them. Menu currency changes are rejected once the Menu contains items. These rules prevent active menu references from retaining deleted catalog definitions or a currency mismatch.
- Activating a MenuItem performs static authoring preflight for Product/Variant/Recipe ownership, recipe lifecycle and ingredients, currency, and option satisfiability. Dynamic route, connectivity, and inventory readiness remain runtime or deployment concerns.
- Cloud inventory balances, with optional sensor/topology evidence, decide machine-produced runtime sellability once a kiosk opts into balance tracking.

## Related Docs

- [System Flows](SYSTEM_FLOWS.md)
- [Checkout Execution Flow](CHECKOUT_EXECUTION_FLOW.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Boundary Contexts](../architecture/BOUNDARY_CONTEXTS.md)
- [Deployment Configuration](../operations/DEPLOYMENT_CONFIG.md)
