# Documentation Coverage Matrix

This is the cross-module discovery index for current backend documentation.
It shows where a contributor starts for ownership, public contracts, workflow
behavior, and verification. It does not replace the source documents listed in
each row.

## Search Keywords

`documentation coverage`, `module documentation`, `bounded context matrix`, `contract coverage`, `flow coverage`, `test entry point`, `where is this documented`, `documentation owner`

## Metadata

| Field | Value |
| --- | --- |
| Status | Current documentation index |
| Owner | Backend architecture and documentation maintainers |
| Verification | `docs-ops` structural and link checks; referenced test paths are discovery hints, not coverage claims |

## Coverage Matrix

| Module / boundary | Code owner | Contract and API | Flow / operations | Verification entry point |
| --- | --- | --- | --- | --- |
| Identity | `src/Application/Identity`, `src/Domain/Identity` | [Authorization Rules](api/AUTHORIZATION_RULES.md), [Identity Onboarding](api/IDENTITY_ONBOARDING_RULES.md) | [Back-Office Setup](flows/BACK_OFFICE_SETUP_FLOW.md) | `tests/IceBot.UnitTests/Identity` |
| Tenants | `src/Application/Tenants`, `src/Domain/Tenants` | [Multi-Tenancy Rules](architecture/MULTI_TENANCY_RULES.md), [Management API Surface](api/MANAGEMENT_API_SURFACE.md) | [Back-Office Setup](flows/BACK_OFFICE_SETUP_FLOW.md) | `tests/IceBot.IntegrationTests/Tenancy` |
| Catalog | `src/Application/Catalog`, `src/Domain/Catalog` | [Management API Surface](api/MANAGEMENT_API_SURFACE.md), [Data Modeling Rules](data/DATA_MODELING_RULES.md) | [Back-Office Setup](flows/BACK_OFFICE_SETUP_FLOW.md) | `tests/IceBot.UnitTests/Catalog` |
| Sales Catalog | `src/Application/SalesCatalog`, `src/Domain/SalesCatalog` | [API Surface Rules](api/API_SURFACE_RULES.md) | [Catalog Runtime Menu](flows/CATALOG_RUNTIME_MENU_FLOW.md) | `tests/IceBot.UnitTests/SalesCatalog` |
| Orders | `src/Application/Orders`, `src/Domain/Orders` | [API Surface Rules](api/API_SURFACE_RULES.md), [Management API Surface](api/MANAGEMENT_API_SURFACE.md) | [Checkout Execution](flows/CHECKOUT_EXECUTION_FLOW.md), [Production Incident Resolution](flows/PRODUCTION_INCIDENT_RESOLUTION_FLOW.md) | `tests/IceBot.UnitTests/Orders` |
| Payments | `src/Application/Payments`, `src/Domain/Payments` | [API Surface Rules](api/API_SURFACE_RULES.md), [Management API Surface](api/MANAGEMENT_API_SURFACE.md), [Idempotency and Retry Rules](data/IDEMPOTENCY_RETRY_RULES.md) | [Checkout Execution](flows/CHECKOUT_EXECUTION_FLOW.md), [Daily Payment Reconciliation](flows/DAILY_PAYMENT_RECONCILIATION_FLOW.md) | `tests/IceBot.UnitTests/Payments` |
| Devices | `src/Application/Devices`, `src/Domain/Devices` | [Management API Surface](api/MANAGEMENT_API_SURFACE.md), [Edge Sync and Telemetry Contract](iot/EDGE_SYNC_TELEMETRY_CONTRACT.md) | [Operations Support](flows/OPERATIONS_SUPPORT_FLOW.md) | `tests/IceBot.UnitTests/Devices` |
| Inventory | `src/Application/Inventory`, `src/Domain/Inventory` | [Management API Surface](api/MANAGEMENT_API_SURFACE.md) | [Back-Office Setup](flows/BACK_OFFICE_SETUP_FLOW.md), [Inventory Refill](flows/INVENTORY_REFILL_FLOW.md), [Operations Support](flows/OPERATIONS_SUPPORT_FLOW.md) | `tests/IceBot.UnitTests/Inventory` |
| Robot Configuration | `src/Application/RobotConfiguration`, `src/Domain/RobotConfiguration` | [Robot Lua Artifact Flow](flows/ROBOT_LUA_ARTIFACT_FLOW.md), [Edge Command Contract](iot/EDGE_COMMAND_CONTRACT.md) | [Robot Lua Authoring and Import](flows/ROBOT_LUA_AUTHORING_AND_IMPORT_FLOW.md), [Robot Artifact Smoke](operations/ROBOT_ARTIFACT_OPERATIONAL_SMOKE.md) | `tests/IceBot.UnitTests/RobotConfiguration` |
| Production Configuration | `src/Application/ProductionConfiguration`, `src/Domain/ProductionConfiguration` | [Management API Surface](api/MANAGEMENT_API_SURFACE.md), [Edge Command Contract](iot/EDGE_COMMAND_CONTRACT.md) | [Robot Lua Deployment and Activation](flows/ROBOT_LUA_DEPLOYMENT_AND_ACTIVATION_FLOW.md) | `tests/IceBot.UnitTests/ProductionConfiguration` |
| Production Packages | `src/Application/ProductionPackages`, `src/Domain/ProductionPackages` | [Management API Surface](api/MANAGEMENT_API_SURFACE.md) | [Package Installation](flows/PRODUCTION_PACKAGE_INSTALLATION_FLOW.md), [Package Upgrade](flows/PRODUCTION_PACKAGE_UPGRADE_FLOW.md) | `tests/IceBot.UnitTests/ProductionPackages` |
| Edge Integration | `src/Application/EdgeIntegration`, `src/Infrastructure/EdgeIntegration` | [IoT Contract](iot/IOT_CONTRACT.md), [Edge Command Contract](iot/EDGE_COMMAND_CONTRACT.md), [Edge Sync and Telemetry](iot/EDGE_SYNC_TELEMETRY_CONTRACT.md) | [Checkout Execution](flows/CHECKOUT_EXECUTION_FLOW.md), [Restart and Power Recovery](operations/RESTART_AND_POWER_RECOVERY.md) | `tests/IceBot.IntegrationTests/EdgeIntegration` |
| Client Devices | `src/Application/ClientDevices`, `src/Domain/Devices/ClientDevices` | [Client Device API](api/CLIENT_DEVICE_API.md), [API Surface Rules](api/API_SURFACE_RULES.md) | [Tablet and Cloud Contract](iot/TABLET_CLOUD_CONTRACT.md) | `tests/IceBot.IntegrationTests/Devices/ClientDevice*` |
| Operations | `src/Application/Operations`, `src/Domain/Operations` | [SignalR Contract](api/SIGNALR_REALTIME_CONTRACT.md), [Management API Surface](api/MANAGEMENT_API_SURFACE.md) | [Alert Lifecycle](flows/ALERT_LIFECYCLE_FLOW.md), [Maintenance Ticket](flows/MAINTENANCE_TICKET_FLOW.md), [Operations Support](flows/OPERATIONS_SUPPORT_FLOW.md) | `tests/IceBot.UnitTests/Operations` |
| Sync | `src/Application/Sync`, `src/Domain/Sync` | [Edge Sync and Telemetry](iot/EDGE_SYNC_TELEMETRY_CONTRACT.md), [Idempotency and Retry Rules](data/IDEMPOTENCY_RETRY_RULES.md) | [Failure Flow Index](flows/FAILURE_FLOW_INDEX.md) | `tests/IceBot.UnitTests/Sync` |
| Cross-cutting runtime | `src/WebAPI`, `src/Infrastructure` | [API Surface Rules](api/API_SURFACE_RULES.md), [API Error Contract](api/API_ERROR_CONTRACT.md), [Deployment Configuration](operations/DEPLOYMENT_CONFIG.md) | [Startup And Bootstrap Rules](operations/STARTUP_AND_BOOTSTRAP_RULES.md), [Observability](operations/OBSERVABILITY.md), [MQTT Operations](operations/MQTT_OPERATIONS.md) | `tests/IceBot.IntegrationTests` |
| EF schema evolution | `src/Infrastructure/Migrations`, `src/Infrastructure/Data/Configurations` | [Data Modeling Rules](data/DATA_MODELING_RULES.md), [EF Core Migration Workflow](data/EF_CORE_MIGRATION_WORKFLOW.md) | [Startup And Bootstrap Rules](operations/STARTUP_AND_BOOTSTRAP_RULES.md) | PostgreSQL migration and integration-test suites |

## Maintenance Rules

- A row names the smallest stable code owner, not every dependency used by a workflow.
- Add a row only for a bounded context or a cross-cutting runtime concern with its own contract or operational procedure.
- Use existing test directories only as entry points. Do not treat their presence as proof of exhaustive coverage.
- If a row has no owning contract or flow, treat it as a documentation gap and add the narrowest missing document rather than expanding an unrelated overview.

## Related Docs

- [Documentation Rules](process/DOCUMENTATION_RULES.md)
- [Documentation Routing Map](DOCUMENTATION_ROUTING_MAP.md)
- [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md)
- [System Flows](flows/SYSTEM_FLOWS.md)
