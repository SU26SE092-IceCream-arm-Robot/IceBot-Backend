# Checkout Execution Flow

This document describes tablet checkout, payment, edge dispatch, robot execution, and customer-facing status projection.

Detailed API and message contracts live in [IoT Contract](../iot/IOT_CONTRACT.md).

## Search Keywords

`checkout to execution`, `tablet checkout`, `payment session`, `QR payment`, `post-payment fan-out`, `tablet status`, `edge command flow`, `execution event sync`, `payment success`, `ready for execution`, `OrderReadyForExecution`, `MQTT`, `EdgeCommand`, `ProductionExecutionRecord`

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
8. Cloud validates kiosk/menu item/basic idempotency.
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
17. Cloud updates PaymentTransaction = Paid and Order = ReadyForExecution in one DB transaction.
18. Cloud commits payment/order state.
19. Cloud emits post-commit events:
   - PaymentSucceeded
   - OrderReadyForExecution
20. Tablet status flow updates payment/order screen.
21. Edge dispatch flow creates executable command and publishes MQTT notification.
22. Edge receives MQTT notification or finds command by polling.
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
PaymentSucceeded / OrderReadyForExecution
  -> Tablet status notification
  -> Edge executable command dispatch
```

Rules:

- Do not wait for Edge acceptance inside the payment webhook transaction.
- Do not make Tablet status depend on Edge dispatch success.
- Retry failed fan-out handlers independently.
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
| `PaymentTransaction = Paid`, `Order = ReadyForExecution` | Payment successful, preparing order |
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
| `Ready` | false | false | Your order is ready. Please pick it up! | Ready / pick up |
| `Completed` | false | false | Order completed. Thank you! | Completed |
| `Cancelled` | false | false | Order cancelled. | Order cancelled / aborted |
| `RefundRequired` | false | true | Order cancelled after payment. Please contact staff... / Order execution failed... | Staff support / manual refund required |

Order tracking read model boundary limitations and data exclusions are detailed in [API Surface Rules](../api/API_SURFACE_RULES.md#read-model-api-boundaries).

## Edge Command Flow

```text
1. Cloud has a paid order ready for execution.
2. Cloud creates an executable command.
3. Cloud publishes MQTT wake-up notification.
4. Edge receives MQTT or polls on schedule.
5. Edge pulls pending commands from Cloud.
6. Edge deduplicates by commandId/idempotencyKey.
7. Edge performs runtime readiness check.
8. If ready:
   - persist local command/job
   - ack Accepted
   - create local execution work
9. If not ready:
   - ack Rejected
   - include rejection reason and readiness snapshot
10. Cloud updates order/execution state.
```

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
6. Cloud applies accepted events to domain state.
7. Cloud stores failed processing in SyncDeadLetter.
8. Cloud returns accepted/duplicate/rejected item-level result.
```

Event sync must be idempotent. Retrying a batch must not duplicate robot events, stock movements, or status transitions.

## Real-time Order & Payment Updates

During the checkout and execution flow, state changes (e.g. order placement, cancellation, payment webhook status updates, refund flagging) emit real-time SignalR notifications to subscribed clients:
- **`OrderStatusChanged`** is published on `OrderHub` to group `order:{orderId}` when order status transitions.
- **`PaymentStatusChanged`** is published on `OrderHub` to group `order:{orderId}` when payment transaction status changes.

These events allow checkout UIs to automatically update payment success/failure screens or execution status without polling.

## Related Docs

- [System Flows](SYSTEM_FLOWS.md)
- [Failure Flows](FAILURE_FLOWS.md)
- [Catalog Runtime Menu Flow](CATALOG_RUNTIME_MENU_FLOW.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
- [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
