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

## Related Docs

- [System Flows](SYSTEM_FLOWS.md)
- [Back-Office Setup Flow](BACK_OFFICE_SETUP_FLOW.md)
- [Checkout Execution Flow](CHECKOUT_EXECUTION_FLOW.md)
- [Operations Support Flow](OPERATIONS_SUPPORT_FLOW.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
