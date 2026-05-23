# IoT Contract

This document defines the first edge-cloud contract for IceBot. It covers the end-to-end flow between the Flutter tablet, local edge backend, cloud backend, MQTT notification, and robot executor.

The contract is written for the current pre-deployment system:

- One tablet per kiosk.
- Tablet payment uses bank transfer QR/payment session.
- No inventory reservation before payment.
- Cloud can publish MQTT notifications.
- Edge still pulls commands from cloud for retry and offline recovery.
- Edge owns runtime execution and local machine state.
- Cloud owns payment verification, central order state, reporting, and monitoring.

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

- Runtime product projection.
- Estimated inventory availability.
- Device and robot availability.
- Local execution queue.
- Robot job execution state.
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

## End-To-End Flow

```text
1. Customer opens Tablet.
2. Tablet calls Local Edge for runtime product projection.
3. Edge builds projection from product snapshot, recipe snapshot, inventory state, device state, and availability policy.
4. Tablet stores temporary cart/session in memory or local storage.
5. Customer confirms checkout.
6. Tablet checks projection freshness: now - generatedAt <= 5-15 seconds.
7. Tablet calls Cloud to create payment session.
8. Cloud creates Order, PaymentTransaction, and provider payment session.
9. Cloud returns payment token, QR payload, and expire time.
10. Tablet renders QR.
11. Customer pays.
12. Payment provider calls Cloud callback.
13. Cloud verifies payment.
14. Cloud marks PaymentTransaction = Paid and Order = Paid/ready for execution in one database transaction.
15. Cloud commits the transaction.
16. Cloud emits `PaymentSucceeded` / `OrderReadyForExecution` through outbox/event handlers.
17. Tablet notification handler publishes payment/order status update.
18. Tablet switches from QR screen to paid/preparing screen.
19. Edge dispatch handler creates executable order command and publishes MQTT notification.
20. Edge receives MQTT notification or discovers command by polling.
21. Edge pulls executable order command from Cloud.
22. Edge performs fast runtime check with 5-10 second timeout.
23. If ready, Edge accepts command and persists local RobotJob/queue.
24. Robot executor runs the job through the Fairino SDK/local integration.
25. Edge records execution status, estimated inventory deduction, telemetry, and logs.
26. Edge syncs execution events/results to Cloud.
27. Cloud finalizes Order = Completed, or Failed/RefundRequired path if execution fails.
```

Payment success and robot execution are separate concerns. Tablet should not wait for Edge acceptance before showing that payment succeeded.

## State Mapping

Use current domain states where possible.

### Order

Current enum: `Domain.Orders.Enums.OrderStatus`

Recommended v1 mapping:

| Business state | Current enum |
| --- | --- |
| Created, waiting for payment | `PendingPayment` |
| Payment verified, waiting for edge execution | `Paid` |
| Edge accepted executable command | `Accepted` |
| Robot job running | `Preparing` |
| Robot execution completed | `Completed` |
| Payment failed, cancelled, or execution failed | `Failed` / `Cancelled` |

If the project later needs more precision, add explicit states such as `ReadyForExecution`, `ExecutionRejected`, and `RefundRequired`.

### Payment

Current enum: `Domain.Payments.Enums.PaymentTransactionStatus`

Use:

- `Pending` when QR/payment session is created.
- `Paid` after provider callback is verified.
- `Failed`, `Cancelled`, or `Expired` based on provider result.
- `Refunded` after refund completion.

### Robot Job

Current enum: `Domain.RobotRuntime.Enums.RobotJobStatus`

Use:

- `Queued` after Edge accepts command and persists local job.
- `Running` while robot executor is active.
- `Completed` after successful execution.
- `Failed` if execution cannot complete.
- `Cancelled` if Cloud/Edge cancels before completion.

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

## Tablet To Local Edge

### Get Runtime Product Projection

```http
GET /api/v1/local/runtime-products?kioskId={kioskId}
```

Purpose: return the menu that can currently be sold from this kiosk.

Response:

```json
{
  "snapshotId": "uuid",
  "kioskId": "uuid",
  "generatedAt": "2026-05-21T10:00:00Z",
  "expiresAt": "2026-05-21T10:00:15Z",
  "runtimeStateTimestamp": "2026-05-21T09:59:59Z",
  "machineAvailable": true,
  "products": [
    {
      "productId": "uuid",
      "productCode": "VANILLA_CUP",
      "displayName": "Vanilla Cup",
      "price": 25000,
      "currency": "VND",
      "available": true,
      "unavailableReason": null,
      "recipeId": "uuid",
      "recipeVersion": 3,
      "estimatedIngredientLevels": [
        {
          "ingredientId": "uuid",
          "ingredientCode": "VANILLA_MIX",
          "levelStatus": "Medium"
        }
      ]
    }
  ]
}
```

Projection inputs:

- Product snapshot.
- Recipe snapshot.
- `IngredientDispenserState`.
- Device state.
- Robot availability.
- Availability policy.

This response is a quote for UX, not a reservation.

## Tablet To Cloud

### Create Payment Session

```http
POST /api/v1/payment-sessions
```

Headers:

```text
X-Idempotency-Key: checkout:{tabletSessionId}
X-Correlation-Id: {correlationId}
```

Request:

```json
{
  "kioskId": "uuid",
  "tabletSessionId": "uuid",
  "runtimeSnapshotId": "uuid",
  "runtimeSnapshotGeneratedAt": "2026-05-21T10:00:00Z",
  "items": [
    {
      "clientLineId": "uuid",
      "productId": "uuid",
      "productCode": "VANILLA_CUP",
      "recipeId": "uuid",
      "recipeVersion": 3,
      "quantity": 1,
      "unitPrice": 25000
    }
  ],
  "totalAmount": 25000,
  "currency": "VND"
}
```

Response:

```json
{
  "orderId": "uuid",
  "orderNumber": "ORD-20260521-0001",
  "paymentTransactionId": "uuid",
  "paymentToken": "provider-token",
  "qrPayload": "provider-qr-payload",
  "expiresAt": "2026-05-21T10:05:00Z"
}
```

Cloud creates:

- `Order`
- `OrderItem`
- `PaymentTransaction`
- provider payment session

Do not create `RobotJob` at this stage.

## Provider To Cloud

### Payment Callback

Provider callback is provider-specific and should be handled by the Payments context.

Cloud must:

- Verify signature/provider authenticity.
- Deduplicate provider event by provider event id.
- Update `PaymentTransactionStatus`.
- Set `OrderStatus = Paid` only after verified payment.
- Commit payment/order state before notifying Tablet or Edge.
- Emit a durable domain/application event after commit, such as `PaymentSucceeded` or `OrderReadyForExecution`.

Cloud must not:

- Block the provider webhook response while waiting for Edge acceptance.
- Let Tablet notification depend on Edge dispatch success.
- Create robot runtime state in the payment webhook transaction.

After commit, event/outbox handlers fan out into independent flows:

```text
PaymentSucceeded / OrderReadyForExecution
  -> Tablet status notification
  -> Edge executable command dispatch
```

If either fan-out fails, retry that handler independently. Payment remains paid because the provider-confirmed payment is already committed.

## Cloud To Tablet Status

Tablet needs fast feedback after the customer pays. Cloud can support this through polling first, then MQTT/WebSocket/SSE later if needed.

Recommended v1:

- Tablet polls `GET /api/v1/orders/{orderId}/payment-status` every 2-3 seconds while QR is displayed.
- When `Order.PaymentStatus = Paid`, Tablet shows payment success immediately.
- If `Order.Status = Paid` but Edge has not accepted yet, Tablet shows "payment successful, preparing order".
- If `Order.Status = Preparing`, Tablet shows "making item".
- If `Order.Status = Completed`, Tablet shows "ready/pick up".
- If `Order.Status = Failed` after payment, Tablet shows staff support/manual refund message.

Tablet state mapping:

| Cloud state | Tablet screen |
| --- | --- |
| `PaymentTransaction = Pending` | QR payment screen |
| `PaymentTransaction = Paid`, `Order = Paid` | Payment successful, preparing order |
| `Order = Accepted` | Machine accepted order |
| `Order = Preparing` | Making item |
| `Order = Completed` | Ready / pick up |
| `Order = Failed` after paid | Staff support / manual refund required |

## Cloud To Edge Notification

### MQTT Executable Order Notification

Topic:

```text
icebot/kiosks/{kioskId}/commands/notify
```

Payload:

```json
{
  "messageId": "uuid",
  "type": "ExecutableOrderAvailable",
  "kioskId": "uuid",
  "orderId": "uuid",
  "createdAt": "2026-05-21T10:00:00Z",
  "contractVersion": 1
}
```

Rules:

- MQTT payload is a wake-up signal only.
- Edge must call command pull after receiving this notification.
- Duplicate MQTT messages are expected and must be harmless.
- Missing MQTT messages are acceptable because Edge also pulls periodically.

## Edge To Cloud

### Pull Commands

```http
POST /api/v1/iot/kiosks/{kioskId}/commands/pull
```

Request:

```json
{
  "originNodeId": "kiosk-edge-node-id",
  "lastCommandSequence": 123,
  "maxCommands": 10,
  "edgeTime": "2026-05-21T10:00:02Z"
}
```

Response:

```json
{
  "serverTime": "2026-05-21T10:00:03Z",
  "commands": [
    {
      "commandId": "uuid",
      "commandSequence": 124,
      "commandType": "StartRobotJob",
      "kioskId": "uuid",
      "orderId": "uuid",
      "paymentTransactionId": "uuid",
      "idempotencyKey": "order:{orderId}:execute",
      "issuedAt": "2026-05-21T10:00:00Z",
      "expiresAt": "2026-05-21T10:10:00Z",
      "payloadSchemaVersion": 1,
      "payload": {
        "orderNumber": "ORD-20260521-0001",
        "items": [
          {
            "orderItemId": "uuid",
            "productId": "uuid",
            "productCode": "VANILLA_CUP",
            "recipeId": "uuid",
            "recipeVersion": 3,
            "quantity": 1,
            "recipeSnapshotJson": {}
          }
        ]
      }
    }
  ]
}
```

Rules:

- Edge must deduplicate by `commandId` and `idempotencyKey`.
- A retried command must not create duplicate `RobotJob`.
- Edge should persist the command and local job before starting execution.

### Command Ack And Fast Runtime Check

```http
POST /api/v1/iot/kiosks/{kioskId}/commands/{commandId}/ack
```

Request:

```json
{
  "originNodeId": "kiosk-edge-node-id",
  "orderId": "uuid",
  "ackStatus": "Accepted",
  "checkedAt": "2026-05-21T10:00:05Z",
  "robotJobId": "uuid",
  "rejectionReason": null,
  "readinessSnapshot": {
    "isReady": true,
    "checkDurationMs": 320,
    "robotAvailable": true,
    "deviceHealthy": true,
    "inventorySufficient": true,
    "queueCapacityAvailable": true,
    "runtimeStateTimestamp": "2026-05-21T10:00:04Z"
  }
}
```

Allowed `ackStatus` values:

- `Received`
- `Accepted`
- `Rejected`
- `Started`
- `Completed`
- `Failed`

Fast runtime check timeout: 5-10 seconds.

Check:

- Edge process is healthy.
- Robot executor is available.
- Required robot program/config version and local point/frame references exist.
- Required devices are online and not in error.
- Required ingredients are not below allowed level.
- Queue capacity is available.

If rejected after payment, Cloud should mark the order as failed/refund-required using the current compensation workflow.

### Sync Events Batch

```http
POST /api/v1/iot/kiosks/{kioskId}/events
```

Request:

```json
{
  "batchId": "uuid",
  "originNodeId": "kiosk-edge-node-id",
  "sentAt": "2026-05-21T10:02:00Z",
  "events": [
    {
      "eventId": "uuid",
      "eventType": "RobotJobCompleted",
      "sequence": 12001,
      "occurredAt": "2026-05-21T10:01:58Z",
      "correlationId": "order-id",
      "causationId": "command-id",
      "orderId": "uuid",
      "robotJobId": "uuid",
      "robotJobStepId": null,
      "payloadSchemaVersion": 1,
      "payload": {
        "status": "Completed",
        "durationMs": 95000
      }
    }
  ]
}
```

Response:

```json
{
  "serverTime": "2026-05-21T10:02:01Z",
  "accepted": ["event-id-1"],
  "duplicates": ["event-id-2"],
  "rejected": [
    {
      "eventId": "event-id-3",
      "reason": "InvalidRobotJobState"
    }
  ]
}
```

Event types should map to existing domain tables:

| Event type | Domain target |
| --- | --- |
| `RobotJobQueued` / `RobotJobStarted` / `RobotJobCompleted` / `RobotJobFailed` | `RobotJobEvent`, `RobotJob` |
| `RobotJobStepStarted` / `RobotJobStepCompleted` / `RobotJobStepFailed` | `RobotJobEvent`, `RobotJobStep` |
| `DeviceStatusChanged` / `DeviceErrorRaised` | `DeviceEvent` |
| `IngredientLevelChanged` | `IngredientDispenserState` |
| `StockConsumed` / `StockRefilled` / `StockAdjusted` | `StockMovement` |
| `KioskHeartbeatReported` | `KioskHeartbeat` |

Cloud ingestion should use `SyncEventInbox` for deduplication and `SyncDeadLetter` for failed processing.

### Heartbeat

```http
POST /api/v1/iot/kiosks/{kioskId}/heartbeat
```

Request:

```json
{
  "messageId": "uuid",
  "originNodeId": "kiosk-edge-node-id",
  "heartbeatSequence": 123,
  "reportedAt": "2026-05-21T10:00:00Z",
  "status": "Online",
  "appVersion": "1.0.0",
  "robotSdkVersion": "farino-x.y",
  "networkStatus": "Online",
  "runtimeStateTimestamp": "2026-05-21T09:59:59Z"
}
```

Maps to `KioskHeartbeat`.

### Configuration Sync

```http
GET /api/v1/iot/kiosks/{kioskId}/configuration?currentVersion={version}
```

Purpose: edge fetches product, recipe, robot program, and device configuration snapshots.

Response:

```json
{
  "configurationVersion": 42,
  "generatedAt": "2026-05-21T10:00:00Z",
  "checksum": "sha256",
  "products": [],
  "recipes": [],
  "robotPrograms": [],
  "devices": []
}
```

Rules:

- Robot programs and steps are shipped as complete versioned packages.
- `RobotProgramStep` represents a workflow action/instruction. For motion steps, it references local Fairino point/frame names instead of Cloud-owned coordinates.
- Do not send realtime robot step commands from Cloud.
- Edge executes robot steps locally through the robot SDK/controller using local Fairino execution data.

## Idempotency And Retry Rules

Required unique keys:

| Boundary | Key |
| --- | --- |
| Tablet checkout to Cloud | `X-Idempotency-Key` |
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

### Payment Paid But Edge Cannot Execute

Expected causes:

- Robot offline.
- Device error.
- Ingredient level too low.
- Required config/program missing.
- Edge queue unavailable.

Flow:

```text
Edge rejects command
Cloud marks order failed/refund-required
Cloud creates manual cash refund request
Staff handles cash refund outside payment provider
Staff confirms refund completion
Monitoring/audit log is created
```

Current phase uses manual cash refund only:

- Do not call provider refund or auto payout APIs in the default flow yet.
- Do not use payout terminology for this phase. `Payout` is reserved for a later provider/bank-transfer automation phase.
- Cloud should create a refund request/record for staff follow-up.
- Staff refunds the customer in cash and confirms completion in the admin UI.

Current domain can represent this with `OrderStatus.Failed` plus payment/refund state. Add explicit `RefundRequired` later if needed.

Manual refund records should capture:

- related order and payment transaction
- refund amount and reason
- refund method: manual cash
- staff account that confirmed completion
- confirmation time
- optional note/evidence

### Edge Offline During Payment

Flow:

```text
Cloud marks payment paid
Cloud creates executable command
MQTT notification may fail or be missed
Edge reconnects and pulls pending commands
Edge accepts or rejects after runtime check
```

### Duplicate MQTT Notification

Duplicate MQTT notifications are harmless because Edge always pulls and deduplicates commands.

## Security

Do not use admin/internal account JWT for kiosk runtime.

Recommended v1 security:

- Tablet to Edge: local network trust plus short-lived local token if needed.
- Tablet to Cloud: public checkout endpoint with idempotency and validation.
- Edge to Cloud: kiosk/device credential.
- MQTT: per-kiosk credential/topic authorization.

Future hardening:

- mTLS for Edge to Cloud.
- Signed edge messages.
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

- [Architecture](../ARCHITECTURE.md)
- [Boundary Contexts](BOUNDARY_CONTEXTS.md)
- [Local Edge Runtime ERD](LOCAL_EDGE_RUNTIME_ERD.md)
- [Idempotency and Retry Rules](IDEMPOTENCY_RETRY_RULES.md)
- [JSON Field Rules](JSON_FIELD_RULES.md)
- [Multi-Tenancy Rules](MULTI_TENANCY_RULES.md)
