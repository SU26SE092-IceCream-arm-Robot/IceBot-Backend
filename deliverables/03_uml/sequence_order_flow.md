# Sequence Diagram — Checkout → Payment → Execution Flow

**Document type**: Team-facing UML baseline (working draft), part of the `deliverables/03_uml/` set.

**Source basis**: `deliverables/00_repo_evidence/repo_truth_map.md` §5 item 4 ("Checkout → Payment → Execution" flow) and §8, cross-checked against `deliverables/00_repo_evidence/functional_inventory.md` rows ORD-01–ORD-03, PAY-01–PAY-03, SYNC-01, MQTT-01, IOT-05/IOT-06, and the corresponding FRs in `deliverables/02_srs/srs.md` §4.7–§4.8, §4.13, §4.15 (FR-057, FR-058, FR-068, FR-070, FR-125, FR-120, FR-121, FR-130). No `src/` or `docs/` files were modified; `srs.md`/`project_introduction.md` were not modified.

**Scope note**: This diagram covers the order/payment lifecycle end-to-end but treats the Edge-side robot execution itself as an opaque step (`Edge Runtime: execute robot program`) — the detailed command-pull/acknowledge/report/timeout mechanics are in `sequence_robot_execution.md` instead, to keep each diagram readable.

---

## Diagram

```mermaid
sequenceDiagram
    actor Customer
    participant Tablet as Tablet (kiosk client)
    participant CloudAPI as Cloud WebAPI (Orders/Payments)
    participant PayOS
    participant Edge as Local Edge Backend

    Customer->>Tablet: Select items
    Tablet->>CloudAPI: GET /kiosks/{id}/runtime-menu
    CloudAPI-->>Tablet: Runtime menu snapshot (ETag, 15s cache)

    Tablet->>CloudAPI: POST /orders (Idempotency-Key)
    Note over CloudAPI: Re-price items server-side;<br/>reject if store closed or kiosk offline for sales
    CloudAPI-->>Tablet: Order created (PendingPayment), OrderAccessToken

    Tablet->>CloudAPI: POST /orders/{id}/payment-sessions (Idempotency-Key)
    Note over CloudAPI: Validate order is paid-eligible;<br/>amount/currency must match server totals
    CloudAPI->>PayOS: Create payment session
    PayOS-->>CloudAPI: Checkout URL / QR payload / expiry
    CloudAPI-->>Tablet: Payment session details
    Tablet-->>Customer: Display QR / checkout link

    Customer->>PayOS: Pay (out-of-band, outside this backend)

    PayOS->>CloudAPI: POST /payments/payos/webhook (signed)
    Note over CloudAPI: Verify x-payos-signature;<br/>idempotent apply
    CloudAPI->>CloudAPI: Set PaymentTransaction=Paid, Order=ReadyForFulfillment (one transaction)
    CloudAPI->>CloudAPI: Dispatch EdgeCommand(ExecuteOrder), attempt 1
    CloudAPI--)Edge: MQTT wake-up (best-effort, QoS1, not retained)

    Edge->>CloudAPI: POST commands/pull
    CloudAPI-->>Edge: Pending command(s), artifact URLs enriched
    Edge->>CloudAPI: POST commands/{id}/ack (Accepted)
    CloudAPI->>CloudAPI: Project acceptance onto Order status
    CloudAPI--)Tablet: SignalR OrderStatusChanged

    Edge->>Edge: Execute robot program<br/>(see sequence_robot_execution.md)

    Edge->>CloudAPI: Execution report (REST fallback or MQTT uplink)
    Note over CloudAPI: Validate provenance checksum against accepted command;<br/>apply idempotently by (SourceEventId, SequenceNumber)
    CloudAPI->>CloudAPI: Update Order/OrderItem/ProductionExecutionRecord
    CloudAPI--)Tablet: SignalR OrderStatusChanged / OrderItemFulfillmentChanged

    Tablet->>CloudAPI: GET /orders/{id} (poll, using OrderAccessToken)
    CloudAPI-->>Tablet: CustomerStatus, CanRetryPayment, RequiresStaffSupport

    Note over CloudAPI: If report/ack never arrives within timeout,<br/>a background job repairs missed dispatch<br/>(see sequence_robot_execution.md)
```

## Explanation

- Payment confirmation and physical execution are **explicitly decoupled in time** — the webhook only commits `PaymentTransaction=Paid` / `Order=ReadyForFulfillment`; robot dispatch happens as a separate step immediately after, but is not guaranteed to succeed synchronously (BR-03 in `srs.md` §7).
- The `Idempotency-Key` header on both order placement and payment-session creation means a retried request returns the original result rather than creating a duplicate order or session (NFR-001).
- MQTT is drawn as a **fire-and-forget** notification (dashed arrow, `--)`) — Edge is not guaranteed to receive it, so it always still pulls commands over REST independently; this is why the diagram shows both the MQTT wake-up and the REST `commands/pull` call rather than treating MQTT as the delivery mechanism itself (BR-04).
- The customer's actual payment to PayOS happens outside this backend entirely (browser/bank-app redirect); the diagram marks it "out-of-band" rather than modeling PayOS-internal steps, since those are not part of this repository's evidence.
- `Order` status is surfaced to the Tablet two ways: an active push (SignalR) and a poll fallback (`GET /orders/{id}`) — both are evidenced, so both are shown.

## Evidence Notes

- Overall flow narrative: `repo_truth_map.md` §5 item 4 (citing `docs/flows/CHECKOUT_EXECUTION_FLOW.md:11-72`, not re-read directly here — inherited via `repo_truth_map.md`).
- Runtime menu projection (ETag/15s cache): `functional_inventory.md` SC-08; `srs.md` FR-045, NFR-013.
- Place order / idempotent checkout: `functional_inventory.md` ORD-01; `srs.md` FR-057.
- Get order status / customer cancel: `functional_inventory.md` ORD-02, ORD-03; `srs.md` FR-058.
- Create payment session: `functional_inventory.md` PAY-01; `srs.md` FR-068.
- PayOS webhook ingestion, signature verification, atomic Paid+ReadyForFulfillment transition, dispatch of `ExecuteOrder` attempt 1: `functional_inventory.md` PAY-03; `srs.md` FR-070; NFR-012.
- Post-sync exception: a signature-verified PayOS callback with no matching local provider transaction is acknowledged without creating payment/order/fulfillment state and increments bounded observability. It does not enter the normal paid/dispatch sequence: `backend_update_impact_2026-08-11.md` §5; `srs.md` FR-070.
- MQTT wake-up (best-effort, QoS1, not retained, failure does not roll back the command): `functional_inventory.md` MQTT-01; `srs.md` FR-125.
- Edge command pull and acknowledgement: `functional_inventory.md` IOT-05, IOT-06; `srs.md` FR-120, FR-121.
- Execution report ingestion (idempotent by `(SourceEventId, SequenceNumber)`, provenance checksum validation): `functional_inventory.md` IOT-07; `srs.md` FR-122.
- Real-time order/fulfillment notifications: `functional_inventory.md` ORD-25; `srs.md` FR-067 (this SRS FR's own Evidence field cites only ORD-25; the underlying SignalR transport mechanism is `functional_inventory.md` SIG-04, added explicitly in `requirements_traceability_matrix.md`'s FR-067 row).
- Payment/execution decoupling and reconciliation-worker repair of missed dispatch: `repo_truth_map.md` §5 item 4, §8; `srs.md` BR-03, and `functional_inventory.md` SYNC-01/SYNC-02 (detailed in `sequence_robot_execution.md`).
- `Idempotency-Key` behavior: `srs.md` NFR-001, BR-13.
- The Tablet client's own internal behavior (menu rendering, QR display) is `[Inferred]` from the API contract only — no tablet/frontend source code exists in this repository (`project_introduction.md` §12; `srs.md` §3.1).
