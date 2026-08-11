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
- Cloud runtime-menu reads require the Store to be open according to its typed schedule and `Store.TimeZone`. A closed Store returns `409` and no sellable snapshot is issued.
- An empty Store schedule means no opening-hours restriction. Once any day is configured, an omitted day is treated as closed; opening is inclusive and closing is exclusive. `OpensAt > ClosesAt` represents an overnight interval that continues until the following day's close time.
- Runtime menu from Cloud is a sales catalog snapshot, not a live machine readiness guarantee.
- Kiosk connectivity/operational state and Store sales admission are evaluated on every runtime-menu request before any cached projection is read. A paused, closed, offline, or non-operational kiosk therefore receives no stale cached menu.
- The optional Redis cache stores only the kiosk-specific sellable projection (`Revision` and items), never request-scoped `SnapshotId` or `GeneratedAt`. Each successful request creates a new snapshot identity after reading the projection.
- Redis caching is bounded-TTL acceleration, not sales authority: cache failure falls back to the database projection, and checkout still revalidates sellability transactionally. Cache expiry may delay static catalog visibility by at most the configured distributed TTL plus a short process-local TTL.
- Each snapshot has a random request identity and a deterministic content `Revision`. The runtime endpoint returns that revision as `ETag`; clients may revalidate with `If-None-Match` after `ExpiresAt` and receive `304` when sellable content is unchanged.
- Edge projection may include inventory, device, queue, and robot availability.
- Order item snapshots preserve historical sale truth after catalog/menu changes.
- Checkout revalidates Catalog and Sales Catalog under one repeatable-read transaction snapshot before persisting immutable order-item, recipe, and option snapshots.
- Product/ProductVariant deletion and Product currency changes are rejected while a non-deleted MenuItem references them. Menu currency changes are rejected once the Menu contains items. These rules prevent active menu references from retaining deleted catalog definitions or a currency mismatch.
- Activating a MenuItem performs static authoring preflight for Product/Variant/Recipe ownership, recipe lifecycle and ingredients, currency, and option satisfiability. Dynamic route, connectivity, and inventory readiness remain runtime or deployment concerns.
- Inventory V1 is reporting/operations only and does not decide runtime menu sellability.

## Related Docs

- [System Flows](SYSTEM_FLOWS.md)
- [Checkout Execution Flow](CHECKOUT_EXECUTION_FLOW.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Boundary Contexts](../architecture/BOUNDARY_CONTEXTS.md)
- [Deployment Configuration](../operations/DEPLOYMENT_CONFIG.md)
