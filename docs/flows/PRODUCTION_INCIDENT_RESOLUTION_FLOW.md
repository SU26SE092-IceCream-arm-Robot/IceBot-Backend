# Production Incident Resolution Flow

## Search Keywords

`production incident`, `outcome unknown`, `partial output`, `defective output`, `inspection`, `discard`, `exact-unit remake`, `compensation`, `manual intervention`

## Purpose

This flow handles production truth after a machine job fails, reports possible output, or requires manual intervention. It preserves successful output and inventory evidence while giving operations an explicit path for inspection, discard, exact-unit remake, or compensation.

## Ownership

- Orders owns `ProductionIncident`, inspection, operational resolution, and audit history.
- Production Execution owns immutable command/job/unit evidence.
- Payments owns refund records and settlement state.
- Inventory owns immutable stock movements. Incident resolution does not reverse consumption automatically.

## Automatic Opening

```text
Authenticated terminal production report
-> validate immutable job/unit provenance
-> persist execution and stock evidence
-> project Order/OrderItem state
-> create one incident for failed/manual-intervention job evidence
-> commit atomically
```

`PhysicalOutputState.No` infers `NotProduced`. `Yes` or `Unknown` requires inspection. Report retries return the existing incident because command/job provenance is unique.

The immutable production job range is the incident resolution granularity. When units in one dispatched line have different outcomes, Edge must report disjoint job ranges so Cloud can preserve and resolve each outcome independently; a mixed range is not silently split from ambiguous evidence.

## Staff Workflow

```text
Work queue
-> incident detail and history
-> inspect exact output range
-> select one idempotent resolution
-> execute linked remake/refund action when applicable
-> complete with staff notes
```

Inspection outcomes are `ConfirmedGood`, `NotProduced`, `Defective`, `PartialOrUncertain`, and `Unknown`. A resolution cannot be selected before inspection is known.

Resolution selection uses a client-generated `resolutionRequestId` and a backend-stored normalized request fingerprint. A retry must use the same complete payload.

Resolution rules:

- `DeliverExistingOutput` requires `ConfirmedGood`.
- `RequestRemake` requires `NotProduced` or `Defective`.
- A normal remake still requires failed evidence with confirmed no physical output.
- A defective-output remake additionally requires the matching incident, exact item/unit range, `Defective` inspection, and selected `RequestRemake` resolution.
- `RequestRefund` and `IssueVoucher` are V1 full-order compensation only and require explicit acknowledgement plus `refunds.request` authorization. Completing, rejecting, or cancelling the resulting compensation requires `refunds.process`.
- No resolution automatically deletes execution records, successful-unit evidence, or stock movements.

## API

```text
GET   /api/v1/management/production-incidents
GET   /api/v1/management/orders/{orderId}/production-incidents/{incidentId}
POST  /api/v1/management/orders/{orderId}/items/{orderItemId}/production-incidents
PATCH /api/v1/management/orders/{orderId}/production-incidents/{incidentId}/inspection
POST  /api/v1/management/orders/{orderId}/production-incidents/{incidentId}/resolution
PATCH /api/v1/management/orders/{orderId}/production-incidents/{incidentId}/complete
```

The manual-open endpoint reports a defective output against existing execution evidence; it cannot create arbitrary command/job provenance. All routes enforce the owning Order tenant scope and use `404` for scope mismatch.

## V1 Limits

- No automatic refund or provider payout.
- No partial-money refund; incident compensation is explicitly full-order.
- No sensor-based inference that an output was delivered or discarded.
- Resolution completion is an explicit staff audit action; automatic completion reconciliation can be added after Edge and provider completion contracts are stable.

## Related Docs

- [Checkout Execution Flow](CHECKOUT_EXECUTION_FLOW.md)
- [Failure Flow Index](FAILURE_FLOW_INDEX.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
