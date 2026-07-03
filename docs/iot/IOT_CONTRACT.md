Exit code: 0
Wall time: 0.1 seconds
Output:
# IoT Contract

This document defines the first edge-cloud contract for IceBot. It covers the end-to-end flow between the Flutter tablet, local edge backend, cloud backend, MQTT notification, and robot executor.

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

Recommended v1 mapping:

| Business state | Current enum |
| --- | --- |
| Created, waiting for payment | `PendingPayment` |
| Payment verified, ready to dispatch to edge | `ReadyForExecution` |
| Edge accepted executable command | `Accepted` |
| Robot job running | `Preparing` |
| Robot execution completed | `Completed` |
| Edge rejected execution after payment | `ExecutionRejected` |
| Paid order needs manual refund/support | `RefundRequired` |
| Payment failed, cancelled, or non-refundable execution failure | `Failed` / `Cancelled` |

`Paid` remains a coarse payment-confirmed state, but current orchestration should move fully paid orders to `ReadyForExecution`.

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

## Tablet To Local Edge

### Get Runtime Menu Projection

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
      "productVariantId": "uuid",
      "menuItemId": "uuid",
      "productCode": "VANILLA_CUP",
      "productVariantCode": "M",
      "displayName": "Vanilla Cup",
      "sizeCode": "M",
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

- Menu item snapshot.
- Product variant snapshot.
- Product snapshot.
- Recipe snapshot.
- `IngredientDispenserState`.
- Device state.
- Robot availability.
- Availability policy.

This response is a quote for UX, not a reservation.

## Tablet To Cloud

### Get Kiosk Sales Catalog Snapshot

```http
GET /api/v1/kiosks/{kioskId}/runtime-menu
```

Purpose: return the Cloud Sales Catalog snapshot that is currently sellable for a kiosk.

This endpoint is useful when the tablet needs a Cloud-backed menu snapshot, but it is not a replacement for the Local Edge runtime projection. It does not include live machine availability, ingredient sufficiency, robot status, or local queue state. Read-model boundaries and data exclusions for this endpoint are documented in [API Surface Rules](../api/API_SURFACE_RULES.md#read-model-api-boundaries).

Response:

```json
{
  "snapshotId": "uuid",
  "kioskId": "uuid",
  "generatedAt": "2026-05-21T10:00:00Z",
  "expiresAt": "2026-05-21T10:00:15Z",
  "availabilitySource": "CloudSalesCatalog",
  "containsMachineRuntimeState": false,
  "items": [
    {
      "menuId": "uuid",
      "menuItemId": "uuid",
      "productId": "uuid",
      "productVariantId": "uuid",
      "recipeId": "uuid",
      "menuItemCode": "VANILLA_CUP_M",
      "productCode": "VANILLA_CUP",
      "productVariantCode": "M",
      "displayName": "Vanilla Cup",
      "sizeCode": "M",
      "price": 25000,
      "discountAmount": 0,
      "finalPrice": 25000,
      "currency": "VND",
      "preparationTimeSeconds": 90,
      "imageUrl": null,
      "recipeVersion": 3
    }
  ]
}
```

Rules:

- Use this endpoint only for Cloud Sales Catalog truth.
- Cloud online sales require `KioskStatus.Active` and active parent tenant scope.
- `KioskStatus.Offline` is a connectivity/availability signal, not permission to create new online sales.
- Offline-created orders may be synchronized later only if they were created under a valid offline sales session issued while the kiosk was active and offline sales was enabled.
- For final runtime availability before checkout, the tablet should still prefer the Local Edge runtime projection when the edge service is available.
- The returned `snapshotId` can be sent to `POST /api/v1/orders` as `runtimeSnapshotId`, but Cloud still recalculates prices from `MenuItem.Price`.

### Create Order

```http
POST /api/v1/orders
```

Headers:

```text
X-Idempotency-Key: create-order:{tabletSessionId}
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
      "menuItemId": "uuid",
      "quantity": 1
    }
  ],
  "clientTotalAmount": 25000
}
```

Response:

```json
{
  "orderId": "uuid",
  "orderNumber": "ORD-20260521-0001",
  "status": "PendingPayment",
  "paymentStatus": "Unpaid",
  "totalAmount": 25000,
  "currency": "VND"
}
```

Cloud creates:

- `Order`
- `OrderItem`

Cloud must calculate price from backend Sales Catalog `MenuItem.Price`. Tablet totals are used only for comparison and conflict detection.

### Create Payment Session

```http
POST /api/v1/orders/{orderId}/payment-sessions
```

Headers:

```text
X-Idempotency-Key: payment-session:{orderId}
X-Correlation-Id: {correlationId}
```

Request:

```json
{
  "idempotencyKey": "payment-session:{orderId}",
  "description": "IceBot order ORD-20260521-0001"
}
```

Response:

```json
{
  "orderId": "uuid",
  "paymentTransactionId": "uuid",
  "checkoutUrl": "https://provider-checkout-url",
  "qrCodePayload": "provider-qr-payload",
  "expiresAt": "2026-05-21T10:05:00Z"
}
```

Cloud creates:

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
- Set `OrderStatus = ReadyForExecution` only after verified payment.
- Commit payment/order state before notifying Tablet or Edge.
- Emit a durable domain/application event after commit, such as `PaymentSucceeded` or `OrderReadyForExecution`.

Cloud must not:

- Block the provider webhook response while waiting for Edge acceptance.
- Let Tablet notification depend on Edge dispatch success.
- Create robot runtime state in the payment webhook transaction.

After commit, independent flows run:

```text
Paid order committed
  -> Tablet status notification
  -> ExecuteOrder dispatch attempt 1
  -> reconciliation of a missing initial command
```

The dispatch handler selects exactly one active execution endpoint whose observed active release or low-cost artifact set covers every machine-produced order line. It resolves each line to a release route and ordered robot-program bindings before creating the durable command. Zero matching endpoints defers dispatch; multiple matching endpoints are rejected as ambiguous rather than selected implicitly.

The command identity is `(OrderId, DispatchAttemptNo)`. Repeating the same attempt returns the existing command. The reconciliation worker creates only missing attempt `1`; it does not invent a new attempt after Edge rejection. Command expiry and the active-command admission limit are configured independently from delivery retries. Payment remains paid when dispatch fails because the provider-confirmed payment transaction has already committed.

## Cloud To Tablet Status

Tablet needs fast feedback after the customer pays. Cloud supports this through polling `GET /api/v1/orders/{orderId}` or `GET /api/v1/orders/{orderId}/payment-status` every 2-3 seconds.

Rather than parsing raw database enums, the tablet client should consume the following projected fields on `OrderResult` and `PaymentStatusResult`:

- `CustomerStatus` (string code)
- `CustomerStatusMessage` (client-facing fallback message; frontend may localize by `CustomerStatus`)
- `CanRetryPayment` (boolean indicator)
- `RequiresStaffSupport` (boolean indicator)

Tablet screen mapping based on projections (v1):

| CustomerStatus | CanRetryPayment | RequiresStaffSupport | CustomerStatusMessage | Tablet screen / action |
| --- | --- | --- | --- | --- |
| `WaitingForPayment` | true | false | Waiting for payment. Please scan the QR code. | QR payment screen |
| `PaymentCancelled` | true | false | Payment was cancelled. You can try paying again. | QR payment screen + retry |
| `PaymentExpired` | true | false | Payment session expired. Please retry. | QR payment screen + retry |
| `PaymentFailed` | true | false | Payment failed. You can try paying again. | QR payment screen + retry |
| `Preparing` | false | false | Payment successful. Preparing your order. | Payment successful, preparing order |
| `Ready` | false | false | Your order is ready. Please pick it up! | Ready / pick up |
| `Completed` | false | false | Order completed. Thank you! | Completed |
| `Cancelled` | false | false | Order cancelled. | Order cancelled / aborted |
| `RefundRequired` | false | true | Order cancelled after payment. Please contact staff... / Order execution failed... | Staff support / manual refund required |

## Cloud To Edge Notification

### MQTT Command-Available Wake-Up

After a durable `ExecuteOrder` or `DeployConfiguration` command commits, Cloud makes a best-effort MQTT publish. The wake-up lowers latency only; periodic authenticated command pull remains the delivery authority and recovers broker outages or missed messages.

Topic:

```text
icebot/execution-endpoints/{executionEndpointId}/commands/available
```

Payload:

```json
{
  "type": "CommandAvailable",
  "commandId": "uuid",
  "commandType": "ExecuteOrder",
  "targetExecutionEndpointId": "uuid",
  "notifiedAt": "2026-05-21T10:00:00Z",
  "version": 1
}
```

Rules:

- MQTT payload is a wake-up signal only.
- Edge must call command pull after receiving this notification.
- The message uses QoS 1 and is not retained. Duplicate delivery is expected.
- MQTT publish failure does not roll back or fail the already committed command.
- Duplicate MQTT messages are expected and must be harmless.
- Missing MQTT messages are acceptable because Edge also pulls periodically.
- Broker ACLs bind each MQTT subscriber to its own execution-endpoint topic. Edge calls command pull after every wake-up and also after reconnect/on its polling interval. Operational setup is defined in [MQTT Operations](../operations/MQTT_OPERATIONS.md).
- MQTT subscriber identity is provisioned separately from HTTPS execution authentication. Username and client id equal `executionEndpointId`; the generated password is returned once, held by the broker/Edge secret stores, and never persisted in the application database. Rotation immediately invalidates the old password; revoke disables and disconnects the broker client.

## Edge To Cloud

### Pull Commands

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/commands/pull
```

Full Edge sends its provisioned client certificate during the TLS handshake. Low-cost controllers add the signed-request headers defined under **Execution Endpoint Transport Authentication** below.

Request:

```json
{
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
      "commandType": "DeployConfiguration",
      "kioskId": "uuid",
      "targetExecutionEndpointId": "uuid",
      "orderId": null,
      "dispatchAttemptNo": null,
      "issuedAt": "2026-05-21T10:00:00Z",
      "expiresAt": "2026-05-21T10:10:00Z",
      "payloadJson": "{... canonical command payload ...}"
    }
  ]
}
```

Rules:

- Edge must deduplicate by `commandId`.
- New `ExecuteOrder` payloads include `SchemaVersion = 1`. Edge must reject unsupported schema versions; Cloud may read release provenance from legacy payloads without treating those payloads as fully executable V1 contracts.
- Deployment commands include typed Cloud correlation fields for deployment ownership. `PayloadJson` is execution data, not the authoritative link used by timeout reconciliation.
- Pull first materializes any short-lived artifact download URLs. Only after payload enrichment succeeds does it mark returned commands as `Delivered` and record a delivery attempt.
- Retrying command pull can return delivered but unacknowledged commands.
- Runtime execution state is reported through the event/report ingest boundary, not command ack.
- If a deployment command expires before acceptance, Cloud marks the command `Rejected` with `CommandExpired` and marks the linked Pending deployment `Failed`.
- Once accepted, command expiry no longer applies. If no `Installed`, `Active`, or `Failed` report moves the deployment out of Pending within the configured accepted-report timeout, Cloud marks that attempt `Failed/ExecutionReportTimeout` without changing the endpoint's previously active release/artifact set.
- Late reports do not revive a timed-out attempt. Reconciliation requires a new deployment/rollback attempt so Cloud and endpoint history remain explicit.
- A `DeployConfiguration` payload contains immutable artifact descriptors. During an authenticated pull, each descriptor with a `StorageKey` is enriched with a short-lived `DownloadUrl` and `DownloadUrlExpiresAt`.
- Rollback uses the same `DeployConfiguration` command contract and includes `RollbackTargetDeploymentId` as provenance. Edge installs it as a new deployment attempt; it does not locally mutate the historical deployment.
- Presigned download URLs are transport data only. They are not persisted in `EdgeCommand.PayloadJson`, release manifests, or artifact metadata.
- The object-storage bucket remains private. Edge must download before URL expiry and must not treat the URL as an artifact identity.
- `DownloadUrl` must use an endpoint reachable from the execution endpoint. A Docker-internal MinIO hostname is not a valid external Edge download endpoint unless both runtimes share that network.
- After download, Edge must verify both `ContentLengthBytes` and the SHA-256 `ArtifactChecksum` before installing or activating the artifact.
- A failed download, expired URL, size mismatch, or checksum mismatch must fail the deployment attempt. Edge may pull the unacknowledged command again to obtain fresh download URLs; it must not activate partial or unverified files.
- Fairino-Studio currently exports multiple `.lua` files: normally one file per editor step, while a paired loop is exported as one file. Each exported file is stored as one `RobotArtifact`; `RobotProgramArtifact.RunOrder` defines their runtime sequence.
- Filename prefixes such as `01_` are human-facing export hints, not execution authority. Edge executes the ordered program manifest delivered by Cloud.

### Execution Endpoint Provisioning Boundary

Before command pull, Cloud management must create and activate a `KioskExecutionEndpoint`:

1. Create the endpoint in `Provisioning` for one kiosk and one execution profile.
2. Replace its supported robot targets using runtime-target code, machine-model code, and optional same-kiosk device binding.
3. Provision profile-specific authentication material and a profile identity.
4. Activate the endpoint; only an Active endpoint with an Active credential may authenticate command pull or report execution state.

Full Edge uses `FullEdgeRuntimeId` and requires `MutualTls`; provisioning accepts the client certificate SHA-256 fingerprint. Low-cost uses `ControllerId` and requires `SignedCommandTls`; provisioning accepts an ECDSA NIST P-256 public key PEM. The backend stores only the canonical public key and its SHA-256 fingerprint, never the controller private key. Disabling or retiring an endpoint blocks runtime authentication without deleting deployment or execution history.

Full Edge provisioning body:

```json
{
  "profileIdentity": "<full-edge-runtime-id>",
  "clientCertificateSha256Fingerprint": "<64 lowercase hex characters>"
}
```

Low-cost provisioning body:

```json
{
  "profileIdentity": "<controller-id>",
  "ecdsaPublicKeyPem": "-----BEGIN PUBLIC KEY-----\n...\n-----END PUBLIC KEY-----"
}
```

### Execution Endpoint Transport Authentication

All IoT routes require HTTPS. The authenticated `KioskExecutionEndpoint` is
identified by `{endpointId}` in the route; Cloud derives `KioskId` from that
endpoint instead of trusting a second route or header identity.

Persistence uses the same boundary: readiness, supported-target, telemetry,
alert, deployment, and execution projection rows cannot pair an endpoint or
device with a different kiosk. Deployments also require the selected release
and kiosk to belong to the same organization. Production execution reports
must reference the endpoint targeted by their source command. These are
database constraints in addition to transport authentication and handler
authorization checks.

**Full Edge:** Kestrel requests a client certificate and the application compares its SHA-256 fingerprint with the active credential binding using constant-time comparison. Certificate-chain trust is not used as endpoint identity; the provisioned fingerprint is the pinning boundary.

**Low-cost controller:** every request includes:

```text
X-Execution-Timestamp: <Unix seconds>
X-Execution-Nonce: <new UUID per request>
X-Execution-Signature: <Base64 ECDSA P-256 signature>
```

The signature uses SHA-256 and IEEE P1363 fixed-field format (64-byte `r || s`). It signs this UTF-8 canonical string, with one LF between fields and no trailing LF:

```text
UPPERCASE_HTTP_METHOD
REQUEST_PATH
RAW_QUERY_STRING_OR_EMPTY
endpoint-id-in-D-format
unix-timestamp
nonce-in-D-format
lowercase-sha256-of-raw-request-body
```

The request path includes the API version and route exactly as sent. The query string includes its leading `?` when present. The body hash is computed from raw bytes before JSON model binding.

Cloud rejects signatures outside `ExecutionEndpointSecurity:SignedRequestMaxClockSkewSeconds`. After successful signature verification, it atomically stores `(EndpointId, Nonce)`; reuse is rejected even across backend instances. Expired nonce rows are removed by data retention. Clients retry with a new timestamp, nonce, and signature.

### Command Ack

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/commands/{commandId}/ack
```

Request:

```json
{
  "ackStatus": "Accepted",
  "acknowledgedAt": "2026-05-21T10:00:05Z",
  "rejectionCode": null,
  "rejectionMessage": null,
  "physicalOutputMayHaveOccurred": null
}
```

Allowed `ackStatus` values:

- `Received`
- `Accepted`
- `Rejected`
- `ExecutorBusy`
- `DeliveryFailed`

Command ack owns delivery and executor-admission state. For `ExecuteOrder`, that admission decision also projects to the order lifecycle as defined below. `Running`, `Completed`, `Failed`, and
`RequiresManualIntervention` belong to the execution event/report ingest
boundary, not this endpoint.

`acknowledgedAt` is executor evidence, not ordering authority. It must not be farther in the future than `ExecutionReportIngestion__MaxFutureClockSkewSeconds`, and must not predate command creation or recorded delivery beyond that same skew allowance.

For `ExecuteOrder` commands:

- `Accepted` moves `Order` from `ReadyForExecution` to `Accepted`.
- `ExecutorBusy` is temporary: the command returns to `PendingDelivery` and the order remains `ReadyForExecution`.
- `Rejected` with `physicalOutputMayHaveOccurred` absent or `false` moves the order to `ExecutionRejected`.
- `Rejected` with `physicalOutputMayHaveOccurred=true` moves the paid order to `RefundRequired` because staff support or compensation may be required.
- Order status changes and their `OrderStatusHistory` row commit together with the command acknowledgement.
- Accepting an `ExecuteOrder` command creates a provisional `OrderExecutionRecord` with sequence `0`. This is Cloud correlation state, not a fabricated Edge event; the first order-summary report starts at sequence `1` and replaces it with executor evidence.

Execution timeout reconciliation:

- Before ACK, an expired `ExecuteOrder` command becomes `Rejected/CommandExpired`; a still-`ReadyForExecution` order becomes `ExecutionRejected` with status history.
- An Accepted command with no order-summary report beyond the configured deadline becomes `Stale/Delayed` when the executor heartbeat is still current.
- An Accepted or Running execution with no current heartbeat becomes `Unreachable/PendingRecovery`.
- Prolonged unreachable observation becomes `Unreachable/SupportRequired` for customer/support handling without asserting physical failure.
- Reconciliation changes `OrderExecutionRecord.ObservationStatus` and `CustomerExecutionStatus`; it does not claim that the physical job failed and does not change an Accepted/Preparing Order to `Failed`.
- Customer order/payment polling reads the latest dispatch attempt projection. SignalR publishes `OrderExecutionObservationChanged` to the order group for the same projection.
- A later valid executor report restores observation to `Fresh` through normal sequence validation.

Redispatch is an explicit management operation, not an Edge-side automatic retry. Backend permits a new attempt only after transport `DeliveryFailed` or a rejection proven to be before physical output. It allocates the next attempt number under the order lock, enforces the configured maximum, and records operator/reason audit. `ExecutorBusy` redelivers the same command; possible physical output, `RefundRequired`, production failure, and manual intervention remain support/compensation flows.

### Execution Reports

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/commands/{commandId}/reports
```

Request:

```json
{
  "sourceEventId": "uuid",
  "sequenceNumber": 12001,
  "edgeCreatedAt": "2026-05-21T10:01:58Z",
  "executorReportedAt": "2026-05-21T10:01:59Z",
  "reportType": "ProductionExecution",
  "status": "Running",
  "deploymentId": null,
  "sourceProductionJobId": "uuid",
  "sourceConfigurationReleaseId": "uuid",
  "releaseChecksum": "sha256-hex",
  "physicalOutputMayHaveOccurred": true,
  "errorCode": null,
  "errorMessage": null,
  "payloadJson": null,
  "stockMovements": [
    {
      "sourceEventId": "uuid",
      "ingredientDispenserStateId": "uuid",
      "quantityConsumed": 12.5,
      "balanceAfter": 87.5,
      "occurredAt": "2026-05-21T10:01:57Z",
      "isEstimated": false
    }
  ]
}
```

Response:

```json
{
  "succeeded": true,
  "statusCode": 200,
  "message": "Execution report applied successfully.",
  "data": {
    "commandId": "uuid",
    "sourceEventId": "uuid",
    "reportType": "Deployment",
    "status": "Active",
    "applied": true,
    "duplicate": false
  }
}
```

Allowed `reportType` values in V1:

| Report type | Meaning | Target |
| --- | --- |
| `Deployment` | Full Edge configuration deployment or low-cost active artifact-set deployment result | `KioskConfigurationDeployment`, `ControllerArtifactSetDeployment`, `KioskExecutionEndpoint` active snapshot |
| `ProductionExecution` | Execute-order production progress/result | `ProductionExecutionRecord`, `OrderExecutionRecord` when order provenance is present |

Deployment `status` values:

- `Installed`
- `Active`
- `Failed`

Production execution `status` values:

- `Accepted`
- `Running`
- `Completed`
- `Failed`
- `RequiresManualIntervention`

Rules:

- Command ack is dispatch-only. Execution reports are the current V1 boundary for deployment and production status after a command has been accepted.
- The endpoint deduplicates by `(sourceExecutorId, sourceEventId)` using `SyncEventInbox`. A retry is a duplicate only when command identity and the complete normalized report payload match; reusing the event id for another command or payload returns `409 Conflict`.
- Production reports must repeat the `SourceConfigurationReleaseId` and `ReleaseChecksum` from the accepted execute-order command. Cloud compares both values against the immutable command payload before creating execution or stock projections.
- `edgeCreatedAt`, optional `executorReportedAt`, and stock-evidence `occurredAt` cannot be farther in the future than `ExecutionReportIngestion__MaxFutureClockSkewSeconds` relative to Cloud receipt time.
- `sequenceNumber` is executor-local ordering evidence for projection updates.
- A final replay may transition `Accepted` directly to `Completed`, `Failed`, or `RequiresManualIntervention`; `Running` may have been lost while the controller was disconnected.
- `physicalOutputMayHaveOccurred` must be set when reporting failed production execution. It drives customer/support projection: failure before output can be handled differently from failure after possible physical output.
- Deployment report `Active` updates the observed active configuration/artifact-set snapshot on `KioskExecutionEndpoint`.
- Production reports update the business order in the same transaction: `Accepted -> Accepted`, `Running -> Preparing`, `Completed -> Completed`, `Failed -> Failed`, and `RequiresManualIntervention -> RefundRequired`. Each change appends `OrderStatusHistory`.
- A report with `sourceProductionJobId` set is job/unit-level evidence: it updates `ProductionExecutionRecord` and optional stock evidence only. It must not complete or fail the whole order.
- A report with `sourceProductionJobId=null` is the Edge-computed order summary: it updates `OrderExecutionRecord` and the business Order, not a job-level `ProductionExecutionRecord`. Edge emits this summary only after applying its local multi-job aggregation policy.
- Successful order transitions publish `OrderStatusChanged` through SignalR after commit.
- `stockMovements` is typed append-only consumption evidence and is accepted only on a report with `sourceProductionJobId`. Each item has its own globally unique `sourceEventId`; duplicates are serialized by evidence identity and ignored even when two different reports arrive concurrently. The dispenser must belong to the reporting kiosk.
- A supplied `balanceAfter` updates the observed dispenser estimate. Without it, Cloud records evidence without guessing a new balance. Inventory evidence does not gate runtime-menu sellability or order creation in V1.
- Applied stock evidence publishes `InventoryChanged` after commit. Do not encode authoritative stock adjustments only inside `payloadJson`.

### Device Warning/Error Evidence

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/device-events
```

This single-event endpoint accepts authenticated `Warning`, `Error`, or `Critical` evidence for a device attached to the reporting kiosk. `originNodeId` must match the execution endpoint profile identity. `eventId` is globally unique and acts as the idempotency key; a retry returns the existing event and does not publish SignalR again. `occurredAt` uses the Edge telemetry future-skew limit. Optional structured payload is limited to 16384 characters, stored as evidence, and excluded from the normal management read API. After commit, Cloud publishes `DeviceEventCreated` to the kiosk operations group. A newly accepted `Error` or `Critical` event creates an Open Alert in the same transaction and publishes `AlertChanged`; Warning remains evidence only.

### Telemetry Replay

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/telemetry-events
```

`/telemetry-events` is the authenticated replay surface for heartbeat evidence,
device warning/error evidence, and local operation logs. It does not accept
production history.

Request shape:

```json
{
  "originNodeId": "uuid-bound-to-execution-endpoint",
  "events": [
    {
      "eventId": "uuid-envelope-id",
      "eventType": "Heartbeat",
      "heartbeat": {
        "heartbeatSequence": 41,
        "reportedAt": "2026-07-01T10:00:00Z",
        "status": "Online",
        "pendingSyncEventCount": 2
      }
    },
    {
      "eventId": "uuid-device-event-id",
      "eventType": "DeviceEvent",
      "deviceEvent": {
        "deviceId": "uuid",
        "eventType": "MotorOverheat",
        "severity": "Error",
        "message": "Motor exceeded temperature threshold.",
        "occurredAt": "2026-07-01T09:59:30Z",
        "payload": { "temperatureC": 85 }
      }
    },
    {
      "eventId": "uuid-local-log-source-id",
      "eventType": "LocalLog",
      "localLog": {
        "deviceId": "uuid",
        "action": "RuntimeRestarted",
        "category": "EdgeRuntime",
        "severity": "Info",
        "message": "Runtime restarted after local power interruption.",
        "occurredAt": "2026-07-01T09:58:00Z"
      }
    }
  ]
}
```

Rules:

- The batch contains 1 to `EdgeTelemetryIngestion__MaxBatchEventCount` items; default maximum is 100.
- `eventId` values must be non-empty and unique inside one request.
- Exactly one typed payload must match each `eventType`.
- Each item is atomic and independent. Valid items commit even when another item is rejected.
- A fully accepted/duplicate batch returns `200`; a valid envelope with one or more rejected/failed items returns `207 Multi-Status` with per-item status.
- Per-item statuses are `Accepted`, `Duplicate`, `Rejected`, or `Failed`.
- A successful telemetry item records a processed `SyncEventInbox` receipt. Destination tables remain the data source: `KioskHeartbeat`, `DeviceEvent`, or `OperationLog`.
- Heartbeats retain `(kioskId, originNodeId, heartbeatSequence)` destination idempotency. Device events and local logs use the envelope `eventId` as their source identity.
- If processing commits but receipt recording is interrupted, retry reaches destination dedup and then records the missing receipt.
- Retrying an existing processed `eventId` returns `Duplicate` without replaying side effects or SignalR notifications.
- Batch device events retain the same Alert rule: newly accepted Error/Critical evidence creates one Open Alert; Warning does not.
- Local operation logs may reference a device or order only when it belongs to the reporting kiosk. Raw payload remains bounded to 16384 characters.

### Production History Replay

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/production-sync/events
```

This endpoint accepts only durable production-history items. Each item carries
`eventId`, `sequenceNumber`, `eventType`, `schemaVersion`, `edgeCreatedAt`, and
optional order/command/job correlation plus payload. It returns item-level
results and the contiguous acknowledged sequence.

Production-history rules:

- `originNodeId` is the persistent executor identity bound to the authenticated endpoint: `FullEdgeRuntimeId` or `ControllerId`.
- `(originNodeId, eventId)` is the production-event idempotency identity. `sequenceNumber` is positive, monotonic, persistent across ordinary restart, and unique per origin.
- A ProductionEvent is stored directly in `SyncEventInbox` with its sequence. It does not create a second generic receipt.
- Cloud accepts an event received beyond a gap but advances `ProductionEventCheckpoint` only over committed contiguous sequences. The item result returns `acknowledgedSequenceNumber`; Edge retains and retries everything above it.
- Reusing an event id with another sequence, type, correlation identity, schema version, or payload returns conflict for that item. Reusing a sequence for another event id also returns conflict.
- An event at or below the acknowledged checkpoint is an idempotent duplicate even after its old detailed receipt has passed retention.

Checkpoint query:

```http
GET /api/v1/iot/execution-endpoints/{endpointId}/production-sync/checkpoint?sourceExecutorId={id}
```

The authenticated endpoint may query only its bound executor identity. A new stream returns sequence `0`. This endpoint is the reconnect resume cursor; timestamps are not ordering authority.

### Latest-State Summary Channel

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/production-sync/state-summaries
```

```json
{
  "sourceExecutorId": "uuid-bound-to-execution-endpoint",
  "summaries": [
    {
      "summaryKind": "CurrentExecution",
      "stateRevision": 18,
      "summarySchemaVersion": 1,
      "edgeCreatedAt": "2026-07-01T10:00:00Z",
      "payload": { "status": "Running", "sourceCommandId": "uuid" }
    }
  ]
}
```

`stateRevision` is positive and monotonic per `(sourceExecutorId, summaryKind)`. A newer revision replaces the current summary, an exact same revision is `Duplicate`, an older revision is `Stale`, and the same revision with different content is rejected as conflict. Summary ingestion is item-level and may return `207 Multi-Status`.

The summary channel is advisory current state used to recover visibility quickly after reconnect. It is not durable event history, does not create production events, and never advances `ProductionEventCheckpoint`.

### Heartbeat

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/heartbeat
```

Request:

```json
{
  "originNodeId": "uuid-bound-to-execution-endpoint",
  "heartbeatSequence": 123,
  "reportedAt": "2026-05-21T10:00:00Z",
  "status": "Online",
  "appVersion": "1.0.0",
  "firmwareVersion": "farino-x.y",
  "networkStatus": "Online",
  "robotStatus": "Ready",
  "cpuUsagePercent": 10,
  "memoryUsagePercent": 20,
  "diskUsagePercent": 30,
  "pendingSyncEventCount": 0
}
```

The request uses the same HTTPS execution-endpoint authentication as command pull and execution reports. `originNodeId` must equal the endpoint's bound `FullEdgeRuntimeId` or `ControllerId`. `(kioskId, originNodeId, heartbeatSequence)` is the idempotency key; retry returns the existing heartbeat. `reportedAt` cannot exceed `EdgeTelemetryIngestion__MaxFutureClockSkewSeconds` into the future. Cloud stores unique out-of-order heartbeat evidence, but only a heartbeat whose sequence is newer than the latest stored sequence may change current connectivity. `Kiosk.LastOnlineAt` advances only for a newer `Online` or `Degraded` heartbeat and uses Cloud receive time, never the Edge clock.

Connectivity state machine:

- A current `Offline` heartbeat transitions only `KioskStatus.Active -> KioskStatus.Offline`.
- A current `Online` or `Degraded` heartbeat transitions only `KioskStatus.Offline -> KioskStatus.Active`, and only while the parent organization and store remain active.
- A stale lower-sequence heartbeat is retained for history and returned with `stale=true`; it never rewinds `KioskStatus` or `LastOnlineAt`.
- The reconciliation job transitions `Active -> Offline` with connectivity `Unreachable` after `EdgeTelemetryIngestion__HeartbeatTimeoutSeconds` without an accepted heartbeat.
- Connectivity automation never changes `Provisioning`, `Maintenance`, `Disabled`, or `Retired` kiosks.
- Manual management updates cannot set `Offline` or recover `Offline -> Active`; those transitions belong to accepted heartbeat evidence and timeout reconciliation.
- Heartbeat ingest and timeout reconciliation use the same per-kiosk serialized boundary and recheck current state inside it.
- `KioskStatusChanged` is published only after a committed status transition. Duplicate heartbeats and unchanged states do not publish an event.

### Execution Readiness And Capability Projection

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/readiness
```

The authenticated execution endpoint publishes a complete observed snapshot:

```json
{
  "sourceExecutorId": "uuid",
  "stateRevision": 42,
  "executorReportedAt": "2026-07-01T12:00:00Z",
  "readiness": "Ready",
  "activity": "Idle",
  "safety": "Safe",
  "currentCommandId": null,
  "physicalOutputState": "No",
  "faultCode": null,
  "capabilities": [
    { "capabilityCode": "ICE_CREAM", "workcellCode": "CELL-A", "isAvailable": true }
  ]
}
```

`stateRevision` is positive and monotonic per source executor. Older revisions
are ignored, exact retries are duplicates, and reuse of one revision with
different content returns conflict. `capabilities` is a complete replacement,
not a patch. Cloud stores typed readiness and capability rows; it does not infer
availability from heartbeat strings or generic summary payloads.

`KioskStatus` remains lifecycle/connectivity. Readiness controls machine
sellability and admission: online menu/order validation requires Ready + Safe
and every declared route capability available; command dispatch also requires
Idle. Busy is temporary executor occupancy, not kiosk Offline. SignalR emits
`ExecutionReadinessChanged` only after a newer projection commits.

### Configuration Sync

There is no current `GET /api/v1/iot/execution-endpoints/{endpointId}/configuration` endpoint.

Current production configuration distribution uses the durable command flow:

```text
Published ConfigurationRelease
-> DeployConfiguration EdgeCommand
-> authenticated command pull
-> short-lived artifact download URLs
-> local size/checksum verification
-> Installed report
-> Active report
```

Cloud ships immutable release/program manifests and ordered `RobotArtifact` descriptors. `RobotProgramArtifact.RunOrder` defines artifact execution order; Cloud does not ship `RobotProgramStep`, motion commands, Blockly trees, teaching points, or realtime robot steps.

A future catalog/menu snapshot endpoint is a separate contract. It must not reintroduce the removed step-first robot configuration model or duplicate the deployment command path.

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

Failure flows live in [Failure Flows](../flows/FAILURE_FLOWS.md).

Contract-level rules:

- Payment success and robot execution are separate concerns.
- Payment webhook handling must not wait for Edge acceptance.
- Duplicate MQTT notifications are harmless because Edge always pulls and deduplicates commands.
- Current phase uses manual cash refund only when paid execution fails.
- Do not call provider refund or auto payout APIs in the default flow yet.

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

- [Architecture](../../ARCHITECTURE.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Boundary Contexts](../architecture/BOUNDARY_CONTEXTS.md)
- [System Flows](../flows/SYSTEM_FLOWS.md)
- [Checkout Execution Flow](../flows/CHECKOUT_EXECUTION_FLOW.md)
- [Failure Flows](../flows/FAILURE_FLOWS.md)
- [Historical Step-First Local Edge Runtime ERD](HISTORICAL_STEP_FIRST_LOCAL_EDGE_RUNTIME_ERD.md) (comparison only)
- [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
- [JSON Field Rules](../data/JSON_FIELD_RULES.md)
- [Multi-Tenancy Rules](../architecture/MULTI_TENANCY_RULES.md)
