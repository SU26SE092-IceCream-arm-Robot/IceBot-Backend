# System Flows

This document is the flow index for IceBot backend-facing workflows. Read the smallest flow file that matches the task instead of reading every flow.

Detailed API and message contracts live in [IoT Contract](../iot/IOT_CONTRACT.md).

## Search Keywords

`system flow`, `system overview`, `flow index`, `which flow doc`, `setup to sale`, `back-office setup flow`, `management read flow`, `catalog runtime menu`, `robot Lua artifact`, `RobotProgram`, `configuration deployment`, `checkout to execution`, `post-payment fan-out`, `tablet status`, `edge command flow`, `runtime readiness check`, `execution event sync`, `production incident`, `defective output`, `remake`, `paid but edge cannot execute`, `edge offline`, `duplicate notification`, `operations support`, `management dashboard`, `maintenance ticket`

## Flow Lookup

| Need | Read |
| --- | --- |
| Overall system source-of-truth split and current assumptions | [System Overview Flow](SYSTEM_OVERVIEW_FLOW.md) |
| Tenant/account/catalog/menu setup before selling | [Back-Office Setup Flow](BACK_OFFICE_SETUP_FLOW.md) |
| GraphQL/REST read models for management UI | [Management Read Flow](MANAGEMENT_READ_FLOW.md) |
| Catalog -> Sales Catalog -> runtime menu -> tablet | [Catalog Runtime Menu Flow](CATALOG_RUNTIME_MENU_FLOW.md) |
| Fairino `.lua` export, artifact/program management, release, and Edge deployment | [Robot Lua Artifact Flow](ROBOT_LUA_ARTIFACT_FLOW.md) |
| Franchise-oriented Production Package publication and installation | [Production Package Installation Flow](PRODUCTION_PACKAGE_INSTALLATION_FLOW.md) |
| Production Package preview, materialization, cutover, rollback, and abandonment | [Production Package Upgrade Flow](PRODUCTION_PACKAGE_UPGRADE_FLOW.md) |
| Tablet checkout, payment, edge command, robot execution, status projection | [Checkout Execution Flow](CHECKOUT_EXECUTION_FLOW.md) |
| Outcome unknown, partial/defective output inspection, discard, exact-unit remake, or compensation | [Production Incident Resolution Flow](PRODUCTION_INCIDENT_RESOLUTION_FLOW.md) |
| Telemetry, heartbeat, events, inventory reporting, manual support | [Operations Support Flow](OPERATIONS_SUPPORT_FLOW.md) |
| Manual kiosk/device/order support ticket lifecycle | [Maintenance Ticket Flow](MAINTENANCE_TICKET_FLOW.md) |
| Route a failure to its owning workflow | [Failure Flow Index](FAILURE_FLOW_INDEX.md) |

## Related Docs

- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Authorization Rules](../api/AUTHORIZATION_RULES.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
- [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
- [Data Modeling Rules](../data/DATA_MODELING_RULES.md)
