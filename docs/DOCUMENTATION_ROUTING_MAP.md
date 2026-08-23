# Documentation Routing Map

This document is an optional fallback routing map for humans and AI agents. Use it when a query spans multiple backend docs or when metadata/path filters do not make the right source obvious.

It is not a DDD bounded context map. Domain ownership lives in [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md).

## Search Keywords

`documentation routing map`, `documentation routing`, `docs routing`, `RAG context map`, `which docs to read`, `AI context`, `context selection`, `smallest relevant docs`, `backend docs map`, `documentation index`, `source of truth routing`

## Routing Rules

- Do not read this file for every query.
- Start with direct retrieval using source metadata, path filters, and precise query terms.
- Read this file only when the question spans multiple backend docs or when the right doc is unclear.
- Do not read every linked doc by default.
- Pick the smallest matching row, then inspect code if needed.
- Prefer source-of-truth docs over Vault or personal notes.
- Use `Vault/` only when the user asks about reasoning history, trade-offs, rejected designs, or learning notes.

## Topic To Document Map

| Ask about | Start with | Then read if needed |
| --- | --- | --- |
| High-level backend architecture | [Architecture](../ARCHITECTURE.md) | [Dependency Rules](architecture/DEPENDENCY_RULES.md), [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md) |
| Current work protocol, whether to edit, verification, migrations | [Working Protocol](process/WORKING_PROTOCOL.md) | [Documentation Rules](process/DOCUMENTATION_RULES.md) |
| Wide scan, finding ledger, root-cause remediation, vertical-slice review, invariant checklist, pattern scan, failure scenarios, completion evidence | [Vertical Slice Review](process/VERTICAL_SLICE_REVIEW.md) | [Working Protocol](process/WORKING_PROTOCOL.md), [Backend Critical Rule Checklist](process/BACKEND_CRITICAL_RULE_CHECKLIST.md) |
| Deployment runtime config, environment variables, appsettings, health/info | [Deployment Configuration](operations/DEPLOYMENT_CONFIG.md) | [Startup And Bootstrap Rules](operations/STARTUP_AND_BOOTSTRAP_RULES.md), [API Surface Rules](api/API_SURFACE_RULES.md) |
| Startup gates, bootstrap, demo seed, runtime repair, migration-job ownership | [Startup And Bootstrap Rules](operations/STARTUP_AND_BOOTSTRAP_RULES.md) | [Deployment Configuration](operations/DEPLOYMENT_CONFIG.md), [Local Development Bootstrap](operations/LOCAL_DEVELOPMENT_BOOTSTRAP.md) |
| API error envelope, HTTP status semantics, `503`, client retry behavior | [API Error Contract](api/API_ERROR_CONTRACT.md) | [API Surface Rules](api/API_SURFACE_RULES.md), [Idempotency And Retry Rules](data/IDEMPOTENCY_RETRY_RULES.md) |
| Provider outage, external dependency timeout, readiness versus diagnostics | [Dependency Rules](architecture/DEPENDENCY_RULES.md) | [Startup And Bootstrap Rules](operations/STARTUP_AND_BOOTSTRAP_RULES.md), [Deployment Configuration](operations/DEPLOYMENT_CONFIG.md) |
| Local demo accounts, repeat-safe development role seed, `ICEBOT-DEMO` tenant tree | [Local Development Bootstrap](operations/LOCAL_DEVELOPMENT_BOOTSTRAP.md) | [Deployment Configuration](operations/DEPLOYMENT_CONFIG.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| Observability, Serilog, OpenTelemetry, Aspire Dashboard, debug body logging, OTLP | [Observability](operations/OBSERVABILITY.md) | [Prometheus And Grafana Handoff](operations/PROMETHEUS_GRAFANA_HANDOFF.md), [Deployment Configuration](operations/DEPLOYMENT_CONFIG.md) |
| Manual backend critical rule checks before handoff | [Backend Critical Rule Checklist](process/BACKEND_CRITICAL_RULE_CHECKLIST.md) | [Working Protocol](process/WORKING_PROTOCOL.md) |
| How backend docs should be structured for RAG/search | [Documentation Rules](process/DOCUMENTATION_RULES.md) | this file |
| Which module owns a topic, its primary contract/flow, and its verification entry point | [Documentation Coverage Matrix](DOCUMENTATION_COVERAGE.md) | [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md), [System Flows](flows/SYSTEM_FLOWS.md) |
| Domain ownership, entity belongs to which bounded context | [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md) | [Dependency Rules](architecture/DEPENDENCY_RULES.md) |
| Layer dependency, repository, DbContext, application/domain/infrastructure boundary | [Dependency Rules](architecture/DEPENDENCY_RULES.md) | [Architecture](../ARCHITECTURE.md) |
| Route prefixes, API surface, missing API surface, tablet vs management vs auth vs IoT API, GraphQL vs REST | [API Surface Rules](api/API_SURFACE_RULES.md) | [Management API Surface](api/MANAGEMENT_API_SURFACE.md), [Naming Rules](process/NAMING_RULES.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| Product/template image upload, replacement, removal, delivery URLs, Cloudinary cleanup | [Catalog Image API](api/CATALOG_IMAGE_API.md) | [Management API Surface](api/MANAGEMENT_API_SURFACE.md), [Catalog Runtime Menu Flow](flows/CATALOG_RUNTIME_MENU_FLOW.md), [Deployment Configuration](operations/DEPLOYMENT_CONFIG.md) |
| Self-order tablet provisioning, ClientDevice lifecycle, installation credential, client-device session, runtime bearer | [Client Device API](api/CLIENT_DEVICE_API.md) | [Tablet and Cloud Contract](iot/TABLET_CLOUD_CONTRACT.md), [Authorization Rules](api/AUTHORIZATION_RULES.md), [Deployment Configuration](operations/DEPLOYMENT_CONFIG.md) |
| Authentication endpoints, forgot/reset/change password, current account routes | [API Surface Rules](api/API_SURFACE_RULES.md) | [Identity Onboarding Rules](api/IDENTITY_ONBOARDING_RULES.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| Public landing content, service-registration form, SystemAdmin review, tenant provisioning from an approved registration | [Service Registration And Content Flow](flows/SERVICE_REGISTRATION_AND_CONTENT_FLOW.md) | [API Surface Rules](api/API_SURFACE_RULES.md), [Identity Onboarding Rules](api/IDENTITY_ONBOARDING_RULES.md) |
| Admin-created account onboarding, invitation link, accept invitation, temporary password fallback | [Identity Onboarding Rules](api/IDENTITY_ONBOARDING_RULES.md) | [API Surface Rules](api/API_SURFACE_RULES.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| Role policy, scoped RBAC, SystemAdmin/Manager/Staff/Technician/OrgAdmin | [Authorization Rules](api/AUTHORIZATION_RULES.md) | [API Surface Rules](api/API_SURFACE_RULES.md), [Multi-Tenancy Rules](architecture/MULTI_TENANCY_RULES.md) |
| Naming conventions for entities, fields, APIs, application use cases | [Naming Rules](process/NAMING_RULES.md) | [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md), [API Surface Rules](api/API_SURFACE_RULES.md) |
| EF Core indexes, soft delete, unique constraints, snapshots, partitioning | [Data Modeling Rules](data/DATA_MODELING_RULES.md) | [JSON Field Rules](data/JSON_FIELD_RULES.md), [Idempotency and Retry Rules](data/IDEMPOTENCY_RETRY_RULES.md) |
| EF migration, deterministic backfill, PostgreSQL upgrade baseline, migration rollback | [EF Core Migration Workflow](data/EF_CORE_MIGRATION_WORKFLOW.md) | [Data Modeling Rules](data/DATA_MODELING_RULES.md), [Vertical Slice Review](process/VERTICAL_SLICE_REVIEW.md) |
| JSONB fields, payloads, snapshots, robot parameters, schema versions | [JSON Field Rules](data/JSON_FIELD_RULES.md) | [Data Modeling Rules](data/DATA_MODELING_RULES.md) |
| Idempotency, retry fields, dead letters, callback deduplication | [Idempotency and Retry Rules](data/IDEMPOTENCY_RETRY_RULES.md) | [Data Modeling Rules](data/DATA_MODELING_RULES.md) |
| Tenant isolation, Organization/Store/Kiosk scope, override hierarchy | [Multi-Tenancy Rules](architecture/MULTI_TENANCY_RULES.md) | [Authorization Rules](api/AUTHORIZATION_RULES.md), [Data Modeling Rules](data/DATA_MODELING_RULES.md) |
| Tenant tree, tenant scope lookup, RBAC scope selector | [Multi-Tenancy Rules](architecture/MULTI_TENANCY_RULES.md) | [API Surface Rules](api/API_SURFACE_RULES.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| System overview, source-of-truth split, which flow doc to read | [System Flows](flows/SYSTEM_FLOWS.md) | [System Overview Flow](flows/SYSTEM_OVERVIEW_FLOW.md) |
| Back-office setup, tenant/account/catalog/menu preparation | [Back-Office Setup Flow](flows/BACK_OFFICE_SETUP_FLOW.md) | [API Surface Rules](api/API_SURFACE_RULES.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| Management dashboard, GraphQL read model, aggregated management reads | [Management Read Flow](flows/MANAGEMENT_READ_FLOW.md) | [API Surface Rules](api/API_SURFACE_RULES.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| SignalR realtime UI updates, hub routes, event names, smoke test | [SignalR Realtime Contract](api/SIGNALR_REALTIME_CONTRACT.md) | [API Surface Rules](api/API_SURFACE_RULES.md), [SignalR Smoke Test](operations/SIGNALR_SMOKE_TEST.md) |
| Catalog to runtime menu, menu sellability, Cloud menu vs Edge projection | [Catalog Runtime Menu Flow](flows/CATALOG_RUNTIME_MENU_FLOW.md) | [IoT Contract](iot/IOT_CONTRACT.md), [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md) |
| Ingredient authoring, Recipe lifecycle, RecipeItem replacement, recipe template cloning | [API Surface Rules](api/API_SURFACE_RULES.md), then [Back-Office Setup Flow](flows/BACK_OFFICE_SETUP_FLOW.md) | [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| Inventory topology, device replacement, device retirement, topology warnings, dispenser history, stock before/after, inventory readiness, Recipe ingredient consistency, deploy inventory gate, dispenser provisioning, safe rebind, estimate transfer/discard, rebind history, level-to-quantity mapping, Device capability validation, `IngredientDispenser`, kiosk topology read, dispenser retirement | [API Surface Rules](api/API_SURFACE_RULES.md), then [Back-Office Setup Flow](flows/BACK_OFFICE_SETUP_FLOW.md) | [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md), [Authorization Rules](api/AUTHORIZATION_RULES.md), [Deployment Config](operations/DEPLOYMENT_CONFIG.md), [JSON Field Rules](data/JSON_FIELD_RULES.md) |
| Physical inventory refill, refill task, external lot reference, sensor rebaseline, `inventory.refill.manage` | [Inventory Refill Flow](flows/INVENTORY_REFILL_FLOW.md) | [Alert Lifecycle Flow](flows/ALERT_LIFECYCLE_FLOW.md), [Management API Surface](api/MANAGEMENT_API_SURFACE.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| Fairino `.lua` export, authoring ZIP/sidecar, global RobotArtifactTemplate, organization clone, RobotArtifact upload, RobotProgram RunOrder | [Robot Lua Authoring And Import Flow](flows/ROBOT_LUA_AUTHORING_AND_IMPORT_FLOW.md) | [Robot Lua Artifact Flow](flows/ROBOT_LUA_ARTIFACT_FLOW.md), [API Surface Rules](api/API_SURFACE_RULES.md) |
| Configuration release, execution endpoint provisioning, deployment preview, presigned artifact download, activation, rollback | [Robot Lua Deployment And Activation Flow](flows/ROBOT_LUA_DEPLOYMENT_AND_ACTIVATION_FLOW.md) | [Robot Lua Artifact Flow](flows/ROBOT_LUA_ARTIFACT_FLOW.md), [IoT Contract](iot/IOT_CONTRACT.md), [API Surface Rules](api/API_SURFACE_RULES.md) |
| Fairino `.lua` artifact lifecycle boundary and end-to-end index | [Robot Lua Artifact Flow](flows/ROBOT_LUA_ARTIFACT_FLOW.md) | [IoT Contract](iot/IOT_CONTRACT.md), [API Surface Rules](api/API_SURFACE_RULES.md) |
| Production package, franchise installation, deterministic RobotProgram composition, artifact technical contract, production-definition checksum, deployment acknowledgement | [Production Package Installation Flow](flows/PRODUCTION_PACKAGE_INSTALLATION_FLOW.md) | [Robot Lua Artifact Flow](flows/ROBOT_LUA_ARTIFACT_FLOW.md), [API Surface Rules](api/API_SURFACE_RULES.md) |
| Production Package upgrade, preview checksum, materialization, cutover, rollback, abandonment, stale reconciliation | [Production Package Upgrade Flow](flows/PRODUCTION_PACKAGE_UPGRADE_FLOW.md) | [Production Package Installation Flow](flows/PRODUCTION_PACKAGE_INSTALLATION_FLOW.md), [Idempotency and Retry Rules](data/IDEMPOTENCY_RETRY_RULES.md) |
| Robot artifact migration, MinIO setup, operational smoke, Edge/controller integration test | [Robot Artifact Operational Smoke Test](operations/ROBOT_ARTIFACT_OPERATIONAL_SMOKE.md) | [Deployment Configuration](operations/DEPLOYMENT_CONFIG.md), [Robot Lua Artifact Flow](flows/ROBOT_LUA_ARTIFACT_FLOW.md) |
| Tablet/cloud/edge/payment/MQTT/execution flow | [Checkout Execution Flow](flows/CHECKOUT_EXECUTION_FLOW.md) | [IoT Contract](iot/IOT_CONTRACT.md), [API Surface Rules](api/API_SURFACE_RULES.md) |
| Daily payment collection evidence, provider lookup discrepancy, local refund totals | [Daily Payment Reconciliation Flow](flows/DAILY_PAYMENT_RECONCILIATION_FLOW.md) | [Management API Surface](api/MANAGEMENT_API_SURFACE.md), [Checkout Execution Flow](flows/CHECKOUT_EXECUTION_FLOW.md) |
| Production incident, outcome unknown, defective output, inspection, discard, exact-unit remake, refund or voucher resolution | [Production Incident Resolution Flow](flows/PRODUCTION_INCIDENT_RESOLUTION_FLOW.md) | [Checkout Execution Flow](flows/CHECKOUT_EXECUTION_FLOW.md), [API Surface Rules](api/API_SURFACE_RULES.md), [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md) |
| MQTT broker, ACL, TLS, Edge subscription setup | [MQTT Operations](operations/MQTT_OPERATIONS.md) | [Deployment Config](operations/DEPLOYMENT_CONFIG.md), [Edge Command Contract](iot/EDGE_COMMAND_CONTRACT.md) |
| MQTT failure, pull latency, ACK latency, report lag, stale execution metrics | [Observability](operations/OBSERVABILITY.md) | [MQTT Operations](operations/MQTT_OPERATIONS.md), [Edge Command Contract](iot/EDGE_COMMAND_CONTRACT.md), [Edge Sync and Telemetry Contract](iot/EDGE_SYNC_TELEMETRY_CONTRACT.md) |
| Cloud, database, MQTT, Edge, controller, tablet, or whole-store restart and power-loss recovery | [Restart And Power Recovery](operations/RESTART_AND_POWER_RECOVERY.md) | [Edge Command Contract](iot/EDGE_COMMAND_CONTRACT.md), [Checkout Execution Flow](flows/CHECKOUT_EXECUTION_FLOW.md), [Failure Flow Index](flows/FAILURE_FLOW_INDEX.md) |
| Operations support, telemetry, heartbeat, device events, refund support | [Operations Support Flow](flows/OPERATIONS_SUPPORT_FLOW.md) | [API Surface Rules](api/API_SURFACE_RULES.md), [IoT Contract](iot/IOT_CONTRACT.md) |
| Maintenance ticket lifecycle, staff support ticket, technician assignment | [Maintenance Ticket Flow](flows/MAINTENANCE_TICKET_FLOW.md) | [Operations Support Flow](flows/OPERATIONS_SUPPORT_FLOW.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| Failure ownership, edge offline, duplicate notifications, retry behavior | [Failure Flow Index](flows/FAILURE_FLOW_INDEX.md) | [Idempotency and Retry Rules](data/IDEMPOTENCY_RETRY_RULES.md), [IoT Contract](iot/IOT_CONTRACT.md) |
| Exact tablet-cloud API/message contract | [Tablet and Cloud Contract](iot/TABLET_CLOUD_CONTRACT.md) | [Checkout Execution Flow](flows/CHECKOUT_EXECUTION_FLOW.md), [API Surface Rules](api/API_SURFACE_RULES.md) |
| Current Edge runtime command contract and artifact deployment | [Edge Command Contract](iot/EDGE_COMMAND_CONTRACT.md) | [Robot Lua Artifact Flow](flows/ROBOT_LUA_ARTIFACT_FLOW.md), [Data Modeling Rules](data/DATA_MODELING_RULES.md) |

## Common Query Hints

| Query contains | Useful filters or docs |
| --- | --- |
| `auth`, `login`, `forgot password`, `reset password`, `refresh token` | [API Surface Rules](api/API_SURFACE_RULES.md), section `Authentication And Password Recovery APIs` |
| `deploy`, `deployment`, `env`, `appsettings`, `JWT`, `SMTP`, `PayOS`, `Firebase`, `health`, `info` | [Deployment Configuration](operations/DEPLOYMENT_CONFIG.md) |
| `startup`, `bootstrap`, `seed`, `runtime repair`, `migration job`, `provider health` | [Startup And Bootstrap Rules](operations/STARTUP_AND_BOOTSTRAP_RULES.md), [Deployment Configuration](operations/DEPLOYMENT_CONFIG.md) |
| `API error`, `ApiResult`, `503`, `dependency unavailable`, `retry response`, `mutation state` | [API Error Contract](api/API_ERROR_CONTRACT.md), [API Surface Rules](api/API_SURFACE_RULES.md) |
| `EF migration`, `PostgreSQL migration`, `backfill`, `upgrade baseline`, `nullable unique`, `check constraint` | [EF Core Migration Workflow](data/EF_CORE_MIGRATION_WORKFLOW.md), [Data Modeling Rules](data/DATA_MODELING_RULES.md) |
| `observability`, `Serilog`, `OpenTelemetry`, `Aspire`, `OTLP`, `Prometheus`, `Grafana`, `trace`, `metric`, `debug body logging` | [Observability](operations/OBSERVABILITY.md), [Prometheus And Grafana Handoff](operations/PROMETHEUS_GRAFANA_HANDOFF.md), [Deployment Configuration](operations/DEPLOYMENT_CONFIG.md) |
| `manual verification`, `critical rule`, `smoke checklist`, `maintenance lifecycle test`, `payment webhook test` | [Backend Critical Rule Checklist](process/BACKEND_CRITICAL_RULE_CHECKLIST.md) |
| `wide scan`, `horizontal audit`, `finding ledger`, `root cause`, `pattern scan`, `vertical slice`, `invariant`, `failure path`, `scope freeze`, `completion evidence`, `poison item`, `independent diff review` | [Vertical Slice Review](process/VERTICAL_SLICE_REVIEW.md) |
| `invitation`, `accept invitation`, `admin creates account`, `temporary password`, `CreateInvitation`, `SendInvitationEmail` | [Identity Onboarding Rules](api/IDENTITY_ONBOARDING_RULES.md) |
| `management accounts`, `assignable role options`, `role scope`, `RBAC`, `policy`, `permission matrix`, `permission-matrix.view`, `effective access`, `me access` | [Authorization Rules](api/AUTHORIZATION_RULES.md), [API Surface Rules](api/API_SURFACE_RULES.md) |
| `store`, `organization`, `kiosk`, `tenant scope`, `role scope options` | [Multi-Tenancy Rules](architecture/MULTI_TENANCY_RULES.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| `order paid`, `ready for execution`, `tablet status`, `post-payment fan-out` | [Checkout Execution Flow](flows/CHECKOUT_EXECUTION_FLOW.md), [IoT Contract](iot/IOT_CONTRACT.md) |
| `refund required`, `edge offline`, `duplicate notification`, `retry` | [Failure Flow Index](flows/FAILURE_FLOW_INDEX.md), [Idempotency and Retry Rules](data/IDEMPOTENCY_RETRY_RULES.md) |
| `system overview`, `setup to sale`, `back-office setup`, `catalog to runtime menu`, `operations support`, `management dashboard` | [System Flows](flows/SYSTEM_FLOWS.md), then the matching flow file |
| `management orders`, `order status history`, `execution attempts`, `sourceCommandId`, `production execution record`, `refund`, `manual refund`, `stock movement`, `dispenser state`, `inventory refill` | [Management API Surface](api/MANAGEMENT_API_SURFACE.md), [Checkout Execution Flow](flows/CHECKOUT_EXECUTION_FLOW.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| `GraphQL`, `REST`, `read model aggregation`, `dashboard query`, `overview query` | [API Surface Rules](api/API_SURFACE_RULES.md), section `GraphQL Management Reads` |
| `SignalR`, `hub`, `OrderHub`, `OperationsHub`, `ManagementDashboardHub`, `realtime`, `DashboardInvalidated` | [SignalR Realtime Contract](api/SIGNALR_REALTIME_CONTRACT.md), [SignalR Smoke Test](operations/SIGNALR_SMOKE_TEST.md) |
| `heartbeat`, `readiness`, `capability projection`, `device event`, `kiosk event`, `production event checkpoint`, `state revision`, `operations telemetry`, `lost connection` | [Edge Sync and Telemetry Contract](iot/EDGE_SYNC_TELEMETRY_CONTRACT.md), [API Surface Rules](api/API_SURFACE_RULES.md), [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md) |
| `maintenance ticket`, `support ticket`, `technician assignment`, `maintenance.create`, `maintenance.manage` | [Maintenance Ticket Flow](flows/MAINTENANCE_TICKET_FLOW.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| `alert`, `actionable telemetry`, `acknowledge alert`, `resolve alert`, `alerts.acknowledge`, `alerts.resolve` | [Alert Lifecycle Flow](flows/ALERT_LIFECYCLE_FLOW.md), [IoT Contract](iot/IOT_CONTRACT.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| `device management`, `devices`, `retire device`, `device status`, `DeviceType`, `DeviceModel`, `device catalog`, `device-catalog.read`, `device-catalog.manage`, `devices.view`, `devices.manage` | [Management API Surface](api/MANAGEMENT_API_SURFACE.md); [Authorization Rules](api/AUTHORIZATION_RULES.md); [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md) |
| `current API`, `route ownership`, `REST`, `GraphQL`, `IoT endpoint` | [API Surface Rules](api/API_SURFACE_RULES.md) |
| `soft delete`, `unique index`, `DeletedAt IS NULL` | [Data Modeling Rules](data/DATA_MODELING_RULES.md) |
| `PayloadJson`, `SnapshotJson`, `ConfigJson`, `JSONB` | [JSON Field Rules](data/JSON_FIELD_RULES.md) |
| `SyncEventInbox`, `NextRetryAt`, `LockedUntil`, `dead letter`, `retry audit` | [Idempotency and Retry Rules](data/IDEMPOTENCY_RETRY_RULES.md), [API Surface Rules](api/API_SURFACE_RULES.md) |
| `ProductVariant`, `MenuItem` | [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md), then owning context docs/code |
| `product template`, `products/from-template`, `organization products`, `organization menus` | [API Surface Rules](api/API_SURFACE_RULES.md), then [Multi-Tenancy Rules](architecture/MULTI_TENANCY_RULES.md) |
| `RobotArtifact`, `RobotProgramArtifact`, `ConfigurationRelease`, `ExecutionRoute`, `KioskExecutionEndpoint`, `EdgeCommand` | [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md), then [IoT Contract](iot/IOT_CONTRACT.md) |
| `configuration release authoring`, `ProductVariant Recipe RobotProgram lookup`, `authoring options` | [Robot Lua Artifact Flow](flows/ROBOT_LUA_ARTIFACT_FLOW.md), then [API Surface Rules](api/API_SURFACE_RULES.md) |
| `execution endpoint provisioning`, `FullEdgeRuntimeId`, `ControllerId`, `supported robot target`, `credential rotation` | [Robot Lua Artifact Flow](flows/ROBOT_LUA_ARTIFACT_FLOW.md), then [API Surface Rules](api/API_SURFACE_RULES.md) and [IoT Contract](iot/IOT_CONTRACT.md) |
| `Fairino Studio`, `.lua`, `RunOrder`, `presigned artifact download`, `artifact deployment` | [Robot Lua Artifact Flow](flows/ROBOT_LUA_ARTIFACT_FLOW.md) |
| `local edge db`, `ProductionJob`, `artifact cache`, `workcell scheduler` | [IoT Contract](iot/IOT_CONTRACT.md); use [Historical Step-First Local Edge Runtime ERD](../../Vault/Evolution/EdgeProductionRuntime/HISTORICAL_STEP_FIRST_LOCAL_EDGE_RUNTIME_ERD.md) only when explicitly comparing the removed step-first proposal |

## Related Docs

- [Documentation Rules](process/DOCUMENTATION_RULES.md)
- [Working Protocol](process/WORKING_PROTOCOL.md)
- [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md)
- [API Surface Rules](api/API_SURFACE_RULES.md)
