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
- Edge projection may include inventory, device, queue, and robot availability.
- Order item snapshots preserve historical sale truth after catalog/menu changes.
- Inventory V1 is reporting/operations only and does not decide runtime menu sellability.

## Related Docs

- [System Flows](SYSTEM_FLOWS.md)
- [Checkout Execution Flow](CHECKOUT_EXECUTION_FLOW.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Boundary Contexts](../architecture/BOUNDARY_CONTEXTS.md)
