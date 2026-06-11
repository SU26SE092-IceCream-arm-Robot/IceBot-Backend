# Failure Flows

This document describes backend-facing failure and retry flows for checkout, payment, edge execution, and sync.

## Search Keywords

`failure flow`, `paid but edge cannot execute`, `edge offline`, `duplicate notification`, `retry`, `refund required`, `manual refund`, `payment success execution failure`, `provider webhook duplicate`, `MQTT duplicate`, `command ack retry`, `event sync retry`

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

- [System Flows](SYSTEM_FLOWS.md)
- [Checkout Execution Flow](CHECKOUT_EXECUTION_FLOW.md)
- [Operations Support Flow](OPERATIONS_SUPPORT_FLOW.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
- [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
