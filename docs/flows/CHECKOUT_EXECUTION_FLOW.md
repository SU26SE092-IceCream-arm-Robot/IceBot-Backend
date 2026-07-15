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
7. Tablet calls Cloud Backend to place order.
8. Cloud validates kiosk, Store opening hours in `Store.TimeZone`, menu item, and basic idempotency. A Store that closes after a runtime-menu snapshot was issued rejects the order with `409`.
9. Cloud creates:
   - Order
   - OrderItems
   - status PendingPayment / Unpaid
10. Tablet calls Cloud Backend to create payment session for the order.
11. Cloud creates:
   - PaymentTransaction
   - provider payment session
12. Cloud returns:
   - checkoutUrl
   - qrCodePayload
   - expiresAt
13. Tablet renders QR.
14. Customer pays.
15. Payment provider calls Cloud webhook.
16. Cloud verifies provider callback and signature.
17. Cloud updates PaymentTransaction = Paid and Order = ReadyForFulfillment in one DB transaction.
18. Cloud commits payment/order state.
19. After the payment transaction commits, Cloud dispatches execution attempt `1`.
   A reconciliation worker repairs any paid `ReadyForFulfillment` order whose required machine-execution command was not created.
20. Tablet status flow updates payment/order screen.
21. Edge dispatch resolves one active execution endpoint and the active configuration release, then maps every machine-produced order line to an execution route and ordered robot programs.
22. Cloud publishes a best-effort MQTT `CommandAvailable` wake-up after commit. Edge still finds the durable `ExecuteOrder` command through authenticated pull; periodic polling recovers missed wake-ups.
23. Edge pulls executable command from Cloud.
24. Edge performs fast runtime check with 5-10 second timeout.
25. If ready, Edge accepts command and creates its own local execution state.
26. Robot executor runs the approved artifact plan through its local integration.
27. Edge records:
   - execution status
   - estimated inventory deduction
   - telemetry/logs
28. Edge syncs execution events/results to Cloud.
29. Cloud finalizes:
   - Order = Completed
   - analytics
   - audit log
   - monitoring
```

Payment success and robot execution are separate concerns. Tablet can show payment success before Edge accepts the executable command.

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
- Admission counts active `ExecuteOrder` commands per endpoint and rejects dispatch when the configured queue limit is reached.
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
6. Job/unit reports carry `sourceProductionJobId`, `orderItemId`, `productionUnitNo`, and `productionUnitQuantity`; they update production evidence and only the identified machine-produced order line.
7. The Edge order-summary report (`sourceProductionJobId = null`) advances all dispatched machine-produced lines. The Order is then aggregated across every line, including manual and packaged fulfillment; it completes only when every order item is complete.
8. Cloud appends OrderStatusHistory and typed stock-consumption evidence supplied by Edge.
9. After commit, Cloud publishes OrderItemFulfillmentChanged for changed lines, OrderStatusChanged when the aggregate status changes, and InventoryChanged for stock evidence.
10. Cloud returns accepted/duplicate/rejected result.
```

Event sync must be idempotent. Retrying a batch must not duplicate robot events, stock movements, or status transitions.

Every production report must match the configuration release id and checksum embedded in its accepted execute-order command. Cloud rejects future-dated report/evidence timestamps beyond the configured clock-skew allowance. A source production job is permanently bound to its first reported order item and production-unit range. Each stock-evidence item identifies its `OrderItemId`; Cloud validates the ingredient against that line's immutable recipe or option snapshot. Stock evidence uses its own globally unique event id, so concurrent job reports cannot consume the same evidence twice.

## Real-time Order & Payment Updates

During the checkout and execution flow, state changes (e.g. order placement, cancellation, payment webhook status updates, refund flagging) emit real-time SignalR notifications to subscribed clients:
- **`OrderStatusChanged`** is published on `OrderHub` to group `order:{orderId}` when order status transitions.
- **`OrderItemFulfillmentChanged`** is published to `order:{orderId}` and `kiosk:{kioskId}` when a manual, packaged, or machine-produced line changes status.
- **`PaymentStatusChanged`** is published on `OrderHub` to group `order:{orderId}` when payment transaction status changes.
- **`OrderExecutionObservationChanged`** is published on `OrderHub` when execution observation changes to `Delayed`, `PendingRecovery`, or `SupportRequired` without changing `Order.Status`.

These events allow checkout UIs to automatically update payment success/failure screens or execution status without polling.

## Related Docs

- [System Flows](SYSTEM_FLOWS.md)
- [Failure Flows](FAILURE_FLOWS.md)
- [Catalog Runtime Menu Flow](CATALOG_RUNTIME_MENU_FLOW.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
- [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
