# Checkout Execution Flow

This document describes tablet checkout, payment, edge dispatch, robot execution, and customer-facing status projection.

Detailed API and message contracts live in [IoT Contract](../iot/IOT_CONTRACT.md).

## Search Keywords

`checkout to execution`, `tablet checkout`, `payment session`, `QR payment`, `post-payment fan-out`, `tablet status`, `edge command flow`, `execution event sync`, `payment success`, `ready for fulfillment`, `OrderReadyForFulfillment`, `MQTT`, `EdgeCommand`, `ProductionExecutionRecord`

## Checkout To Execution Flow

```text
1. Customer opens Tablet.
2. Tablet calls Local Edge Backend for runtime menu/product projection.
3. Edge builds projection from:
   - menu item snapshot
   - product variant snapshot
   - product snapshot
   - recipe snapshot
   - recipe execution profile
   - inventory state
   - device state
   - robot availability
   - availability policy
4. Tablet keeps temporary cart/session locally.
5. Customer confirms checkout.
6. Tablet checks runtime projection freshness:
   now - generatedAt <= 5-15 seconds.
7. Tablet calls Cloud Backend to place order. Cloud acquires the kiosk
   customer-session lock and rejects a second non-expired customer session for
   the same kiosk.
8. Cloud re-evaluates kiosk lifecycle, `KioskOperationalState.Operational`, connectivity, Store opening hours in `Store.TimeZone`, explicit Store sales pause, kiosk-scoped menu-item operational availability, Menu/MenuItem lifecycle and scope, Product/Variant availability, Recipe/Ingredient lifecycle, active production route, and every active OptionGroup against the selected option IDs. Checkout calculates server-authoritative prices and stores immutable recipe/option snapshots. A Store, kiosk operational state, operational item pause, or catalog definition that becomes unavailable after a runtime-menu snapshot was issued rejects the order with `409`; a scoped item that does not belong to the kiosk is returned as not found.
9. Cloud creates:
   - Order
   - OrderItems
   - status PendingPayment / Unpaid
   - immutable `paymentDeadlineAt`
10. Tablet calls Cloud Backend to create a payment session for the order.
11. For PayOS, Cloud creates:
   - PaymentTransaction
   - provider payment session
   Cloud persists the deterministic provider order code before calling the provider. A retry reconciles that same provider identity and must not create a second provider session.
12. For cash in Development only, Cloud creates a pending `PaymentTransaction`
    with no gateway request. The kiosk remains in the active customer session
    until a scoped Staff, Manager, or OrgAdmin confirms the physical cash receipt.
    The confirmation action is auditable and applies the same paid-order dispatch
    rule as a verified provider payment.
13. PayOS returns:
   - checkoutUrl
   - qrCodePayload
   - expiresAt
14. Tablet renders QR for PayOS, or directs the customer to Staff for cash.
15. Customer pays.
16. Payment provider calls Cloud webhook, or Staff confirms cash receipt.
17. Cloud verifies a provider callback and signature before payment/order lookup. A verified callback with no matching local provider transaction is acknowledged without creating callback/payment/order evidence or dispatching fulfillment.
18. For a matching payment transaction, Cloud acquires the same kiosk session
   lock, then updates PaymentTransaction = Paid and Order = ReadyForFulfillment
   in one DB transaction. A verified late payment received after another
   customer session began is retained as financial evidence but moves its Order
   to RefundRequired and never dispatches robot work.
19. Cloud commits payment/order state.
20. After the payment transaction commits, Cloud dispatches execution attempt `1`.
   A reconciliation worker repairs any paid `ReadyForFulfillment` order whose required machine-execution command was not created.
   If the kiosk is not `Operational`, the paid order remains queued. Cloud neither creates/delivers a new `ExecuteOrder` command nor cancels/refunds the order. Existing accepted/running execution evidence continues through its normal report lifecycle.
21. Tablet status flow updates payment/order screen.
22. Edge dispatch resolves one active execution endpoint and the active configuration release, then maps every machine-produced order line to an execution route and ordered robot programs.
23. Cloud publishes a best-effort MQTT `CommandAvailable` wake-up after commit. Edge still finds the durable `ExecuteOrder` command through authenticated pull; periodic polling recovers missed wake-ups.
24. Edge pulls executable command from Cloud.
25. Edge performs fast runtime check with 5-10 second timeout.
26. If ready, Edge accepts command and creates its own local execution state.
27. Robot executor runs the approved artifact plan through its local integration.
28. Edge records:
   - execution status
   - optional physical stock-movement evidence when metering exists
   - telemetry/logs
29. Edge syncs execution events/results to Cloud.
30. Cloud finalizes:
   - expected inventory consumption from the immutable order-item Recipe snapshot when a completed unit has no physical stock-movement evidence
   - Order = Completed
   - analytics
   - audit log
   - monitoring
```

## Customer-Attended Kiosk Admission

IceBot currently operates one kiosk as one customer session, not a customer
queue. A customer session starts when an Order enters `PendingPayment` and
continues through paid production and unresolved physical-output intervention.
It releases when the pending-payment deadline passes or the current order has
completed/cancelled terminally. `Order.Completed` is the normal release point:
the robot has confirmed output completion, while inventory projection and
analytics may continue asynchronously.

Edge may retain a durable technical command queue for delivery/retry recovery,
but Cloud does not admit a second customer order for the kiosk during the active
session. The endpoint active-command limit is a technical backstop, not a
customer queue capacity setting.

Payment success and robot execution are separate concerns. Tablet can show payment success before Edge accepts the executable command.

Cash is a staff-confirmed settlement, not a payment-provider simulation. The
public payment-session endpoint may create a pending `cash` transaction only
while that method is active. It is seeded active in `Development` and absent or
forced inactive in non-Development environments. Confirmation uses:

```text
POST /api/v1/management/orders/{orderId}/cash-payments/{paymentTransactionId}/confirm
```

The caller needs `cash-payments.confirm`; `Staff`, `Manager`, and `OrgAdmin`
are rechecked against the order's organization/store/kiosk scope inside the
handler. A retry after success returns the already-confirmed settlement without
creating a second payment or dispatch.

Store sales admission and active fulfillment are also separate concerns. Scheduled closing or an explicit sales pause stops runtime-menu access and new order placement, but does not cancel paid queue entries or stop accepted/running production. An Order placed before closure may create its payment session until its snapshotted `paymentDeadlineAt`; provider expiry is capped by that deadline. Once the deadline passes, no new session is created and the tablet must start a new Order. A verified late `Paid` webhook remains authoritative because money may already have moved.

If the provider accepted session creation but the original response was lost, a background reconciliation worker queries the persisted provider order code and restores the checkout URL or QR payload. This read-side recovery never replaces webhook verification: a provider lookup reporting `PAID` remains pending until a signed webhook authoritatively commits payment and order state. Reconciliation failures and exhausted retries are available through the scoped payment diagnostics read.

A known provider rejection marks the payment attempt failed and allows a new customer attempt. A timeout, transport failure, transient provider response, or incomplete successful response has an unknown creation outcome: Cloud keeps the transaction pending, schedules read-side reconciliation, and does not issue another create request. Operators use the scoped intervention queue and audited manual reconcile command when automatic recovery is exhausted or a signed webhook remains missing.

The intervention queue, automatic reconciliation, and manual reconcile command
share one eligibility policy. A pending provider session is eligible when its
checkout instructions are missing or its local expiry has been reached. An old
checkout URL or QR payload does not hide an expired session from the queue.

When reconciliation reaches manual intervention, Cloud also enqueues one durable
`payment_intervention` push per scoped Staff/Manager recipient, falling back to
the organization OrgAdmin. The identity is `(PaymentTransactionId,
InterventionCode, RecipientAccountId)`. The push recalls an absent operator but
does not change payment or Order state; the intervention queue and manual
reconcile API remain authoritative.

## Post-Payment Fan-Out

After payment is verified and committed, Cloud should fan out independently:

```text
Paid order committed
  -> Tablet status notification
  -> ExecuteOrder dispatch attempt 1
  -> reconciliation scan when the initial dispatch was missed
```

Rules:

- Do not wait for Edge acceptance inside the payment webhook transaction.
- Do not make Tablet status depend on Edge dispatch success.
- Dispatch is idempotent by `(OrderId, DispatchAttemptNo)`.
- Reconciliation creates only missing attempt `1`; a new attempt number requires an explicit retry decision.
- Customer-session admission permits only one active customer order per kiosk.
  Endpoint active-command admission is a secondary technical backstop and has
  a default limit of one.
- Payment remains paid after provider-confirmed commit.

## Tablet Status Flow

Recommended v1 behavior:

```text
1. Tablet renders QR.
2. Tablet polls Cloud payment/order status every 2-3 seconds.
3. If payment pending, keep QR screen.
4. If payment paid, show payment successful / preparing order.
5. If Edge accepted, show machine accepted order.
6. If robot preparing, show making item.
7. If completed, show ready / pick up.
8. If failed after payment, show staff support / manual refund required.
```

State mapping:

| Cloud state | Tablet screen |
| --- | --- |
| `PaymentTransaction = Pending` | QR payment screen |
| `PaymentTransaction = Paid`, `Order = ReadyForFulfillment` | Payment successful, preparing fulfillment |
| `Order = Accepted` | Machine accepted order |
| `Order = Preparing` | Making item |
| `Order = Completed` | Ready / pick up |
| `Order = ExecutionRejected` / `RefundRequired` | Staff support / manual refund required |

Tablet Status Projection mapping (v1):

| CustomerStatus | CanRetryPayment | RequiresStaffSupport | CustomerStatusMessage | Tablet screen / action |
| --- | --- | --- | --- | --- |
| `WaitingForPayment` | true | false | Waiting for payment. Please scan the QR code. | QR payment screen |
| `PaymentCancelled` | true | false | Payment was cancelled. You can try paying again. | QR payment screen + retry |
| `PaymentExpired` | true | false | Payment session expired. Please retry. | QR payment screen + retry |
| `PaymentFailed` | true | false | Payment failed. You can try paying again. | QR payment screen + retry |
| `Preparing` | false | false | Payment successful. Preparing your order. | Payment successful, preparing order |
| `Delayed` | false | false | Your order is taking longer than expected. Production is still being monitored. | Delayed, keep monitoring |
| `PendingRecovery` | false | false | Connection to the machine was interrupted. We are checking your order. | Connection recovery in progress |
| `SupportRequired` | false | true | We could not confirm production progress. Please contact staff for support. | Staff support required |
| `Ready` | false | false | Your order is ready. Please pick it up! | Ready / pick up |
| `Completed` | false | false | Order completed. Thank you! | Completed |
| `Cancelled` | false | false | Order cancelled. | Order cancelled / aborted |
| `RefundRequired` | false | true | Order cancelled after payment. Please contact staff... / Order execution failed... | Staff support / manual refund required |

Order tracking read model boundary limitations and data exclusions are detailed in [API Surface Rules](../api/API_SURFACE_RULES.md#read-model-api-boundaries).

## Edge Command Flow

```text
1. Cloud has a paid order ready for execution.
2. Cloud creates an executable command.
3. Cloud may publish an MQTT command-available wake-up, and Edge polls on schedule. MQTT reduces latency but does not own the durable command.
4. Edge pulls pending commands from Cloud through its authenticated execution endpoint.
5. Edge deduplicates by commandId/idempotencyKey.
6. Edge performs runtime readiness check.
7. If ready:
   - persist local command/job
   - ack Accepted
   - Cloud moves Order to Accepted
   - create local execution work
8. If not ready:
   - ack ExecutorBusy when capacity is temporarily unavailable; Cloud keeps the order ReadyForFulfillment and redelivers later
   - otherwise ack Rejected
   - include rejection reason and readiness snapshot
   - Cloud moves the order to ExecutionRejected, or RefundRequired when physical output may already have occurred
9. Cloud updates order/execution state.
```

## Execution Timeout Reconciliation

```text
Pending/Delivered command expires before ACK
  -> CommandExpired
  -> Order ExecutionRejected

Accepted without order-summary report past deadline
  -> heartbeat current: Stale / Delayed
  -> heartbeat missing, old, or Offline: Unreachable / PendingRecovery
  -> prolonged Unreachable: Unreachable / SupportRequired

Running without report past deadline
  -> same observation rules
  -> Order remains Preparing; Cloud does not infer physical failure
```

Observation timeout is uncertainty about Edge, not proof that production failed. `SupportRequired` is a customer/support projection only; it does not automatically fail or refund the order. A later sequence-valid order-summary report restores `Fresh` and continues the normal lifecycle. REST polling and `OrderExecutionObservationChanged` SignalR events both expose the same projection so the tablet does not remain on `Preparing` indefinitely.

All command-expiry and missing-report deadlines use Cloud receive time. Edge/controller timestamps remain evidence for diagnostics and bounded future-skew validation; clock rollback on a runtime cannot expire an ACK or make a newly received execution report stale. Store timezone changes that affect configured opening hours require an explicit sales pause first. The pause blocks new admission while already paid/accepted/running fulfillment continues.

## Manual Redispatch

```text
Latest attempt DeliveryFailed
  or Rejected before physical output
    -> authorized operator supplies reason
    -> backend allocates attempt + 1
    -> audit actor/reason
    -> create a new immutable ExecuteOrder command
```

`ExecutorBusy` stays on the same attempt and is redelivered. `RefundRequired`, possible physical output, production `Failed`, and `RequiresManualIntervention` are support/refund paths, not automatic retry paths. The configured maximum attempt count is enforced inside the same order-level transaction.

An Edge, controller, or store-power restart during an accepted production job does not
cancel the paid order and does not authorize automatic replay. The affected job is
reported as `RequiresManualIntervention` with exact unit identity and physical-output
evidence. Completed units and stock evidence remain immutable. The complete recovery
matrix is defined in [Restart And Power Recovery](../operations/RESTART_AND_POWER_RECOVERY.md).

Cloud serializes every payment/fulfillment mutation that can change or authorize use
of an `Order` aggregate by `OrderId`. This includes payment-session creation, signed
payment application, payment reconciliation, cancellation/refund-required decisions,
initial dispatch, Manual/Packaged item events, execute-order ACK, production reports,
and timeout reconciliation. ACK, report, and timeout
mutations are also serialized by `EdgeCommand.Id`, so stale or duplicate transport
events cannot create two execution projections or overwrite a newer command state.
Command pull delivery-attempt allocation is serialized by execution endpoint; a
concurrent pull retry keeps the same `EdgeCommand.Id` and receives the next distinct
delivery-attempt number.

Execution-attempt detail exposes the ordered delivery history for that command, command-expiry timeout provenance, the redispatch actor/reason, and references to the immediately previous and next dispatch attempts. This keeps transport retries inside one dispatch attempt distinct from an operator-created redispatch attempt.

MQTT payloads should stay small. Edge must pull command details from Cloud.

## Runtime Readiness Check

Fast runtime check timeout: 5-10 seconds.

Check:

- Edge process is healthy.
- Robot executor is available.
- Required product variant recipe, recipe execution profile, and robot program/config version exist.
- Required Fairino point/frame references exist locally.
- Required devices are online and not in error.
- Ingredient levels are not below allowed threshold.
- Queue capacity is available.

## Execution Event Sync Flow

```text
1. Robot executor starts/runs/completes/fails a job.
2. Edge records local runtime state and append-only events.
3. Edge batches events for Cloud sync.
4. Cloud ingests events through SyncEventInbox.
5. Cloud deduplicates by eventId/source node.
6. Job/unit reports carry `sourceProductionJobId`, `orderItemId`, `productionUnitNo`, and `productionUnitQuantity`. Cloud rejects overlapping ranges, persists `ProductionExecutionRecord` and optional stock evidence, then derives the effective unit outcome for the machine line.
7. A machine line completes only when every expected unit is effectively complete. Any failed unit moves a paid Order to `FulfillmentIssue` without removing successful-unit or stock evidence. In the same ingestion transaction, failed or manual-intervention job evidence opens one production incident for that immutable job/unit range. The Edge order-summary report (`sourceProductionJobId = null`) updates execution observation and must agree with complete job evidence before it can be final.
8. Cloud appends OrderStatusHistory and typed stock-consumption evidence supplied by Edge.
9. After commit, Cloud publishes OrderItemFulfillmentChanged for changed lines, OrderStatusChanged when the aggregate status changes, OrderExecutionObservationChanged for an applied order summary, and InventoryChanged for stock evidence.
10. Cloud returns accepted/duplicate/rejected result.
```

For Manual/Packaged lines with a configured preparation time, Cloud projects
`ExpectedReadyAt = PaidAt + effective preparation time`. Once overdue, a
background reconciliation job creates at most one durable reminder per eligible
recipient for that payment occurrence. It prefers scoped Staff/Manager accounts
and falls back to the organization OrgAdmin. The reminder does not advance or
fail the item; management fulfillment commands remain authoritative.

Event sync must be idempotent. Retrying a batch must not duplicate robot events, stock movements, or status transitions.

## Inventory Evidence Modes

Inventory does not require a sensor by default. A dispenser in `ManualEstimate`
mode is sellable when staff has established a compatible, non-expired quantity
estimate sufficient for the order. Successful completed production units reduce
that estimate in Cloud. `SensorAssisted` accepts the same sales rule while
retaining sensor observations for reconciliation. Only explicit
`SensorRequired` configuration blocks sale for absent or stale calibrated sensor
evidence. A sensor observation is physical evidence, not proof that Lua consumed
the Recipe amount; it never changes tracking mode by itself.

Production incident handling is a separate operational phase after evidence ingestion. Staff inspect possible output, then choose delivery, discard, exact-unit remake, technical review, no action, or explicitly acknowledged full-order refund/voucher. Remake and compensation identities are linked back to the incident. Successful-unit and stock evidence remain historical truth; resolution does not erase them. See [Production Incident Resolution Flow](PRODUCTION_INCIDENT_RESOLUTION_FLOW.md).

Mixed fulfillment is one aggregate workflow even though individual lines have
different authorities. Concurrent completion of Manual, Packaged, and
MachineProduced lines must reload and aggregate the order under the same order lock;
the last completing line transitions the order to `Completed` exactly once.

Every production report must match the configuration release id and checksum embedded in its accepted execute-order command; Low-cost reports must also match the active artifact-set version and checksum. Cloud rejects future-dated report/evidence timestamps beyond the configured clock-skew allowance. A source production job is permanently bound to the immutable provenance established by its first report: order item, production-unit range, workcell, controller, execution-plan checksum, and active artifact set. Each stock-evidence item identifies the same `OrderItemId` as the job report; Cloud validates the ingredient against that line's immutable recipe or option snapshot. Stock evidence uses its own globally unique event id, so concurrent job reports cannot consume the same evidence twice.

## Real-time Order & Payment Updates

During the checkout and execution flow, state changes (e.g. order placement, cancellation, payment webhook status updates, refund flagging) emit real-time SignalR notifications to subscribed clients:
- **`OrderStatusChanged`** is published on `OrderHub` to group `order:{orderId}` when order status transitions.
- **`OrderItemFulfillmentChanged`** is published to `order:{orderId}` and `kiosk:{kioskId}` when a manual, packaged, or machine-produced line changes status.
- **`PaymentStatusChanged`** is published on `OrderHub` to group `order:{orderId}` when payment transaction status changes.
- **`OrderExecutionObservationChanged`** is published on `OrderHub` when an order-summary report refreshes execution observation or reconciliation changes it to `Delayed`, `PendingRecovery`, or `SupportRequired` without changing `Order.Status`.

These events allow checkout UIs to automatically update payment success/failure screens or execution status without polling.

## Related Docs

- [System Flows](SYSTEM_FLOWS.md)
- [Failure Flow Index](FAILURE_FLOW_INDEX.md)
- [Catalog Runtime Menu Flow](CATALOG_RUNTIME_MENU_FLOW.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
- [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
