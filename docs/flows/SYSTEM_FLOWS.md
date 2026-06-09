# System Flows

This document captures backend-facing system flows for IceBot. It describes how Cloud Backend, Tablet, Local Edge Backend, MQTT, payment provider, and robot executor coordinate.

Business/user-facing flows live in the project-level `Docs/BUSINESS_FLOWS.md`.

Detailed API and message contracts live in [IoT Contract](../iot/IOT_CONTRACT.md).

## Search Keywords

`system flow`, `checkout to execution`, `post-payment fan-out`, `tablet status`, `edge command flow`, `runtime readiness check`, `execution event sync`, `paid but edge cannot execute`, `edge offline`, `duplicate notification`, `MQTT`, `payment success`, `ready for execution`, `refund required`

## Current Assumptions

- One tablet per kiosk.
- Tablet may use Cloud runtime menu for catalog display, but should prefer Local Edge runtime projection for final device/robot availability when the edge service is available.
- Tablet uses cloud for order/payment.
- Bank transfer QR is the first payment method.
- No inventory reservation before payment.
- Cloud can publish MQTT notifications.
- Edge still pulls from cloud for retry/offline recovery.
- MQTT is notification only, not source of truth.

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
25. If ready, Edge accepts command and persists local RobotJob/queue.
26. Robot executor runs the job through the Fairino SDK/local integration.
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
   - enqueue RobotJob
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

## Failure Flow: Paid But Edge Cannot Execute

Expected causes:

- Robot offline.
- Device error.
- Ingredient level too low.
- Required config/program missing.
- Edge queue unavailable.

Flow:

```text
1. Payment is already verified and committed as paid.
2. Edge rejects executable command after runtime check.
3. Cloud marks order failed/refund-required using current domain states.
4. Cloud creates manual cash refund request/record.
5. Staff handles refund outside payment provider.
6. Staff confirms refund completion in admin UI.
7. Cloud records audit/monitoring evidence.
```

Current phase uses manual cash refund only. Provider refund or auto payout is future work.

## Failure Flow: Edge Offline During Payment

```text
1. Customer pays.
2. Provider callback reaches Cloud.
3. Cloud marks payment/order paid.
4. Cloud creates executable command.
5. MQTT may fail or be missed.
6. Edge reconnects later.
7. Edge pulls pending commands.
8. Edge accepts or rejects after runtime check.
```

## Failure Flow: Duplicate Notifications Or Retries

Expected duplicates:

- tablet order/payment requests after timeout
- provider webhooks
- MQTT notifications
- command pulls
- command acks
- edge event sync batches

Required behavior:

- Tablet to Cloud uses idempotency keys.
- Provider callback deduplicates provider event id.
- Edge command creation deduplicates command id/idempotency key.
- Edge local job creation must not create duplicate RobotJob.
- Edge event sync deduplicates event id.

## Related Docs

- [IoT Contract](../iot/IOT_CONTRACT.md)
- [Local Edge Runtime ERD](../iot/LOCAL_EDGE_RUNTIME_ERD.md)
- [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
- [Data Modeling Rules](../data/DATA_MODELING_RULES.md)
