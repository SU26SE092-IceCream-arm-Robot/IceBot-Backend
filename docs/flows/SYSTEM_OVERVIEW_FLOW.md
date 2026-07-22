# System Overview Flow

This document describes the high-level backend workflow groups and source-of-truth split for IceBot.

## Search Keywords

`system overview`, `system flow overview`, `source of truth split`, `setup to sale`, `customer runtime`, `operations support`, `Cloud Backend`, `Local Edge Backend`, `tablet`, `management UI`, `GraphQL read model`

## Workflow Groups

IceBot has three main workflow groups:

```text
Back-office setup
  -> tenant hierarchy
  -> accounts / RBAC scopes
  -> products / recipes / menus
  -> kiosk configuration

Customer runtime
  -> tablet menu
  -> order
  -> payment
  -> edge execution
  -> robot serving

Operations support
  -> telemetry / heartbeat / events
  -> order status history
  -> inventory reporting
  -> manual refund / maintenance support
```

## Backend Source-Of-Truth Split

| Flow | Source of truth | Main docs |
| --- | --- | --- |
| API surface and route ownership | WebAPI + Application handlers | [API Surface Rules](../api/API_SURFACE_RULES.md) |
| Role/scope authorization | JWT policies + scoped Application handlers | [Authorization Rules](../api/AUTHORIZATION_RULES.md) |
| Tenant hierarchy | Tenants context | [Multi-Tenancy Rules](../architecture/MULTI_TENANCY_RULES.md) |
| Order/payment state | Orders and Payments contexts | [Checkout Execution Flow](CHECKOUT_EXECUTION_FLOW.md), [IoT Contract](../iot/IOT_CONTRACT.md) |
| Edge/cloud integration | IoT/edge contracts | [IoT Contract](../iot/IOT_CONTRACT.md) |
| Data/idempotency/retry | EF model + typed retry/idempotency fields | [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md) |

## Rules

- Do not treat one UI screen as one backend source of truth.
- UI screens may aggregate data from several contexts, especially through GraphQL management read models.
- Payment success and robot execution are separate concerns.
- Cloud owns central business truth; Edge owns local runtime machine truth.
- MQTT is notification only, not source of truth.

## Integration Transport Boundaries

Use transport by receiver and durability need, not by the broad label "realtime".

| Boundary | Preferred method | Purpose |
| --- | --- | --- |
| Cloud to human UI | SignalR | Realtime UI deltas, ephemeral state, dashboard invalidation, order/payment/ticket/status updates |
| Cloud to Edge/Kiosk/Robot runtime | MQTT plus command pull / durable sync | Wake-up notifications, runtime command availability, device/robot event stream |
| Edge/Kiosk to Cloud | REST batch sync or MQTT event notification | Heartbeats, device events, execution results, production-event checkpoints, and latest-state summaries |
| Cloud to payment/external providers | HTTP SDK/webhook | Provider session creation, callback verification, external identity/email/payment operations |
| Cloud internal async dispatch | Database-backed workflow records and background workers | Post-commit MQTT wake-up, provider reconciliation, notification delivery, and bounded retry |
| Snapshot/query/CRUD | REST/GraphQL | Initial state, detail reads, search/filter/list, commands, audit/history/reporting |

Rules:

- SignalR is for UI clients, not robot execution commands.
- MQTT is for machine-to-machine runtime integration, not management UI state delivery.
- REST/GraphQL remain the recovery path after reconnect, refresh, or missed realtime events.
- Robot runtime messages should include ids, correlation/causation, timestamp, schema/contract version, and idempotency keys.
- Payment/provider callbacks must not depend on SignalR or MQTT success.
- Durable machine-command truth remains in PostgreSQL; MQTT only wakes the receiver. Best-effort SignalR is acceptable for UI notification because REST/GraphQL provide recovery snapshots.

## Related Docs

- [System Flows](SYSTEM_FLOWS.md)
- [Back-Office Setup Flow](BACK_OFFICE_SETUP_FLOW.md)
- [Checkout Execution Flow](CHECKOUT_EXECUTION_FLOW.md)
- [Operations Support Flow](OPERATIONS_SUPPORT_FLOW.md)
- [Maintenance Ticket Flow](MAINTENANCE_TICKET_FLOW.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
