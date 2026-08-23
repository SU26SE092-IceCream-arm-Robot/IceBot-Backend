# Inventory Refill Flow

This document owns the audited physical refill workflow for a kiosk ingredient
balance. It is not a warehouse-management or supplier-receiving system.

## Search Keywords

`inventory refill`, `refill task`, `physical refill`, `external lot reference`,
`sensor rebaseline`, `inventory.refill.manage`, `ManualRefill`

## Boundary

`KioskIngredientInventory` is the authoritative inventory balance used for
sellability and expected consumption. `IngredientDispenserState` is optional
topology and sensor evidence. A kiosk may complete a manual refill without an
Edge-connected dispenser or any dispenser state.

## Lifecycle

```text
Low/Empty alert or operator request
  -> Requested
  -> InProgress
  -> Completed

Requested | InProgress
  -> Cancelled
```

A requester may complete a `Requested` task directly when physical work is
already complete. Completed and Cancelled tasks are terminal.

## Management API

```text
GET  /api/v1/management/kiosks/{kioskId}/inventory/refill-tasks
GET  /api/v1/management/kiosks/{kioskId}/inventory/refill-tasks/{taskId}
POST /api/v1/management/kiosks/{kioskId}/inventory/balances/{inventoryId}/refill-tasks
POST /api/v1/management/kiosks/{kioskId}/inventory/refill-tasks/{taskId}/start
POST /api/v1/management/kiosks/{kioskId}/inventory/refill-tasks/{taskId}/complete
POST /api/v1/management/kiosks/{kioskId}/inventory/refill-tasks/{taskId}/cancel
```

Reads require `inventory.view`. Lifecycle mutations require
`inventory.refill.manage`, an `Idempotency-Key`, and tenant scope matching the
kiosk.

## Completion Effects

Completing a task records the actual quantity, optional reason, notes, and
external lot reference. In one transaction it:

- updates the authoritative estimated balance;
- writes a `ManualRefill` `StockMovement` with before/after quantities;
- records an immutable task transition with actor and request fingerprint;
- validates an optional dispenser-state reference against the same balance;
- marks sensor-assisted evidence for rebaseline when applicable; and
- resolves the source inventory alert only when the resulting balance is above
  its configured recovery threshold.

An alert recovery never silently completes an open refill task. Refill-task
history remains the audit record of physical operator confirmation.

## Retry And Failure Rules

The task transition is idempotent per task and `Idempotency-Key`. Retrying the
same normalized request returns the existing result. Reusing a key with changed
operation, quantity, evidence, or reason returns `409`. A failed event or
realtime publication does not roll back the committed balance and stock ledger;
fresh inventory reads remain authoritative for admission decisions.

## Related Docs

- [Management API Surface](../api/MANAGEMENT_API_SURFACE.md)
- [Authorization Rules](../api/AUTHORIZATION_RULES.md)
- [Alert Lifecycle Flow](ALERT_LIFECYCLE_FLOW.md)
- [Catalog Runtime Menu Flow](CATALOG_RUNTIME_MENU_FLOW.md)
