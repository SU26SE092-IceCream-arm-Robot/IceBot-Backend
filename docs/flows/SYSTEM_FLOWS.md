# System Flows

This document is the flow index for IceBot backend-facing workflows. Read the smallest flow file that matches the task instead of reading every flow.

Business/user-facing flows live in the project-level `Docs/BUSINESS_FLOWS.md`.

Detailed API and message contracts live in [IoT Contract](../iot/IOT_CONTRACT.md).

## Search Keywords

`system flow`, `system overview`, `flow index`, `which flow doc`, `setup to sale`, `back-office setup flow`, `management read flow`, `catalog runtime menu`, `robot Lua artifact`, `RobotProgram`, `configuration deployment`, `checkout to execution`, `post-payment fan-out`, `tablet status`, `edge command flow`, `runtime readiness check`, `execution event sync`, `paid but edge cannot execute`, `edge offline`, `duplicate notification`, `operations support`, `management dashboard`, `maintenance ticket`

## Flow Lookup

| Need | Read |
| --- | --- |
| Overall system source-of-truth split and current assumptions | [System Overview Flow](SYSTEM_OVERVIEW_FLOW.md) |
| Tenant/account/catalog/menu setup before selling | [Back-Office Setup Flow](BACK_OFFICE_SETUP_FLOW.md) |
| GraphQL/REST read models for management UI | [Management Read Flow](MANAGEMENT_READ_FLOW.md) |
| Catalog -> Sales Catalog -> runtime menu -> tablet | [Catalog Runtime Menu Flow](CATALOG_RUNTIME_MENU_FLOW.md) |
| Fairino `.lua` export, artifact/program management, release, and Edge deployment | [Robot Lua Artifact Flow](ROBOT_LUA_ARTIFACT_FLOW.md) |
| Tablet checkout, payment, edge command, robot execution, status projection | [Checkout Execution Flow](CHECKOUT_EXECUTION_FLOW.md) |
| Telemetry, heartbeat, events, inventory reporting, manual support | [Operations Support Flow](OPERATIONS_SUPPORT_FLOW.md) |
| Manual kiosk/device/order support ticket lifecycle | [Maintenance Ticket Flow](MAINTENANCE_TICKET_FLOW.md) |
| Paid-but-not-executable, edge offline, duplicate retry paths | [Failure Flows](FAILURE_FLOWS.md) |

## Related Docs

- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Authorization Rules](../api/AUTHORIZATION_RULES.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
- [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
- [Data Modeling Rules](../data/DATA_MODELING_RULES.md)
