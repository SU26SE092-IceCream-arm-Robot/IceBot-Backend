Exit code: 0
Wall time: 0.1 seconds
Output:
# IoT Contract

This document owns the shared boundary, source-of-truth split, common message envelope, and cross-cutting rules for the IceBot IoT contracts.

The Cloud artifact-first schema is implemented by the current backend migration. Edge-local runtime persistence remains an external implementation. Do not interpret the historical `RobotJob` examples below as Cloud backend entities.

The contract is written for the current pre-deployment system:

- One tablet per kiosk.
- Tablet payment uses bank transfer QR/payment session.
- No inventory reservation before payment.
- Cloud can publish MQTT notifications.
- Edge still pulls commands from cloud for retry and offline recovery.
- Edge owns runtime execution and local machine state.
- Cloud owns payment verification, central order state, reporting, and monitoring.

## Search Keywords

`IoT contract`, `edge-cloud contract`, `tablet`, `local edge`, `cloud backend`, `MQTT`, `payment session`, `QR payment`, `order execution`, `ready for execution`, `executable order`, `pull commands`, `command ack`, `fast runtime check`, `sync events batch`, `heartbeat`, `configuration sync`, `runtime menu projection`, `payment callback`, `refund required`

## Source Of Truth

### Tablet

The tablet owns only transient user interaction state:

- Current menu view.
- Temporary cart/session.
- Payment QR display.
- Local UX status after checkout.

The tablet must not start robot execution directly.

### Local Edge Backend

The local edge backend owns runtime machine truth:

- Runtime menu/product projection.
- Estimated inventory availability.
- Device and robot availability.
- Local execution queue.
- Local production execution state.
- Runtime telemetry and event capture.

Edge can reject execution after payment if the machine cannot fulfill the order.

### Cloud Backend

Cloud owns central business truth:

- `Order`
- `PaymentTransaction`
- Payment provider session/callback verification.
- Executable order command creation.
- Final order state, analytics, audit, and monitoring.

Payment success does not guarantee robot execution. Execution still requires edge acceptance.

### MQTT

MQTT is notification only. It is not the source of truth and must not contain large executable payloads.

Edge must pull commands from cloud after receiving an MQTT notification. Edge must also poll/pull periodically in case MQTT is missed while offline.

MQTT is the machine-to-machine runtime integration boundary. It is separate from SignalR, which is used for Cloud-to-human-UI realtime updates. When Edge or robot state changes, Cloud may project the validated state to UI through SignalR, but SignalR must not drive robot execution directly.

## System Flow

End-to-end checkout, payment, edge dispatch, and robot execution flow lives in [Checkout Execution Flow](../flows/CHECKOUT_EXECUTION_FLOW.md). Failure flows live in [Failure Flows](../flows/FAILURE_FLOWS.md).

This document focuses on API/message contract shape, source-of-truth boundaries, state mapping, and idempotency requirements.

Backend API surface categories and route ownership live in [API Surface Rules](../api/API_SURFACE_RULES.md). This document only expands the IoT/tablet/edge contracts that need integration detail.

## State Mapping

Use current domain states where possible.

### Order

Current enum: `Domain.Orders.Enums.OrderStatus`

Current mapping:

| Business state | Current enum |
| --- | --- |
| Created, waiting for payment | `PendingPayment` |
| Payment verified, ready for all line fulfillment modes | `ReadyForFulfillment` |
| Edge accepted executable command | `Accepted` |
| Robot job running | `Preparing` |
| Robot execution completed | `Completed` |
| Edge rejected execution after payment | `ExecutionRejected` |
| Paid order needs manual refund/support | `RefundRequired` |
| Payment failed, cancelled, or non-refundable execution failure | `Failed` / `Cancelled` |

`Paid` remains a coarse payment-confirmed state, but current orchestration should move fully paid orders to `ReadyForFulfillment`.

### Payment

Current enum: `Domain.Payments.Enums.PaymentTransactionStatus`

Use:

- `Pending` when QR/payment session is created.
- `Paid` after provider callback is verified.
- `Failed`, `Cancelled`, or `Expired` based on provider result.
- `Refunded` after refund completion.

### Cloud Execution Projection

Cloud has no runtime `RobotJob` entity. An accepted executor report creates `OrderExecutionRecord` and `ProductionExecutionRecord`; these retain executor status, observation state, physical-output evidence and source command identity for customer/support decisions.

The Edge runtime may have local `ProductionJob` records, but it owns their scheduler and status transitions.

## Common Envelope Rules

All edge-cloud commands and events should use UTC timestamps and stable ids.

Required common fields:

```json
{
  "messageId": "uuid",
  "correlationId": "uuid-or-order-id",
  "causationId": "uuid-or-command-id",
  "originNodeId": "kiosk-edge-node-id",
  "occurredAt": "2026-05-21T10:00:00Z",
  "contractVersion": 1
}
```

Rules:

- `messageId` identifies this transport message.
- `eventId` identifies a business/runtime event and must be deduplicated.
- `commandId` identifies a command and must be idempotent.
- `correlationId` traces the whole checkout/execution flow.
- `causationId` points to the command/event that caused the current message.
- `originNodeId` identifies the edge node that produced the message.
- All timestamps are UTC ISO 8601.


## Contract Map

| Need | Read |
| --- | --- |
| Tablet runtime menu, Cloud checkout/payment, or customer status | [Tablet and Cloud Contract](TABLET_CLOUD_CONTRACT.md) |
| Cloud-to-Edge commands, endpoint authentication, ACK/report, or configuration distribution | [Edge Command Contract](EDGE_COMMAND_CONTRACT.md) |
| Device evidence, replay, checkpoint, heartbeat, readiness, or capability projection | [Edge Sync and Telemetry Contract](EDGE_SYNC_TELEMETRY_CONTRACT.md) |

## Idempotency And Retry Rules

Required unique keys:

| Boundary | Key |
| --- | --- |
| Tablet checkout to Cloud | `Idempotency-Key` |
| Provider callback | provider event id |
| Cloud executable command | `commandId`, `idempotencyKey` |
| Edge local job creation | `commandId` or `orderId` unique |
| Edge event sync | `eventId` |
| Heartbeat | `kioskId`, `originNodeId`, `heartbeatSequence` |

Retry behavior:

- Retrying payment session creation must return the same payment session if the idempotency key matches.
- Retrying command pull can return already unacked commands.
- Retrying command ack must not create duplicate state transitions.
- Retrying event batch must classify duplicates item-by-item.

## Failure Paths

Failure flows live in [Failure Flows](../flows/FAILURE_FLOWS.md).

Contract-level rules:

- Payment success and robot execution are separate concerns.
- Payment webhook handling must not wait for Edge acceptance.
- Edge deduplicates commands returned after duplicate MQTT notifications.
- Paid execution failures use the staff-managed refund or voucher compensation workflow.
- The default flow does not call provider refund or automatic payout APIs.

## Security

Do not use admin/internal account JWT for kiosk runtime.

Current security contract:

- Tablet to Edge: local network trust plus short-lived local token if needed.
- Tablet to Cloud: public checkout endpoint with idempotency and validation.
- Edge to Cloud: execution-endpoint credentials; `FullEdge` uses mutual TLS and `LowCostController` uses signed-command TLS.
- MQTT: per-kiosk credential/topic authorization.

Future hardening:

- Per-device key rotation.
- Command payload checksum/signature.

## Implementation Notes

- Keep IoT request/response DTOs separate from EF entities.
- Do not expose domain entities directly as IoT contracts.
- Use typed columns for idempotency, retry, status, and timestamps.
- JSON payloads are allowed for robot SDK/config/provider evidence, but source-of-truth workflow fields must be typed.
- `StockMovement` should record estimated consumption after accepted/completed execution. Future sensor conversion can refine quantity handling later.
- `IngredientDispenserState` can remain hardware-level `Low` / `Medium` / `Full` for availability checks.

## Related Docs

- [Architecture](../../ARCHITECTURE.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Checkout Execution Flow](../flows/CHECKOUT_EXECUTION_FLOW.md)
- [Failure Flows](../flows/FAILURE_FLOWS.md)
- [Historical Step-First Local Edge Runtime ERD](HISTORICAL_STEP_FIRST_LOCAL_EDGE_RUNTIME_ERD.md) (comparison only)
- [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
- [JSON Field Rules](../data/JSON_FIELD_RULES.md)
