# RAG Context Map

This document is an optional fallback routing map for humans and AI agents. Use it when a query spans multiple backend docs or when metadata/path filters do not make the right source obvious.

It is not a DDD bounded context map. Domain ownership lives in [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md).

## Search Keywords

`RAG context map`, `docs routing`, `documentation routing`, `which docs to read`, `AI context`, `context selection`, `smallest relevant docs`, `backend docs map`, `documentation index`, `source of truth routing`

## Routing Rules

- Do not read this file for every RAG query.
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
| Deployment runtime config, environment variables, appsettings, health/info | [Deployment Configuration](operations/DEPLOYMENT_CONFIG.md) | [API Surface Rules](api/API_SURFACE_RULES.md) |
| Observability, Serilog, OpenTelemetry, Aspire Dashboard, debug body logging, OTLP | [Observability](operations/OBSERVABILITY.md) | [Deployment Configuration](operations/DEPLOYMENT_CONFIG.md) |
| Manual backend critical rule checks before handoff | [Backend Critical Rule Checklist](process/BACKEND_CRITICAL_RULE_CHECKLIST.md) | [Working Protocol](process/WORKING_PROTOCOL.md) |
| How backend docs should be structured for RAG/search | [Documentation Rules](process/DOCUMENTATION_RULES.md) | this file |
| Domain ownership, entity belongs to which bounded context | [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md) | [Dependency Rules](architecture/DEPENDENCY_RULES.md) |
| Layer dependency, repository, DbContext, application/domain/infrastructure boundary | [Dependency Rules](architecture/DEPENDENCY_RULES.md) | [Architecture](../ARCHITECTURE.md) |
| Route prefixes, API surface, missing API surface, tablet vs management vs auth vs IoT API, GraphQL vs REST | [API Surface Rules](api/API_SURFACE_RULES.md) | [Naming Rules](process/NAMING_RULES.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| Authentication endpoints, forgot/reset/change password, current account routes | [API Surface Rules](api/API_SURFACE_RULES.md) | [Identity Onboarding Rules](api/IDENTITY_ONBOARDING_RULES.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| Admin-created account onboarding, invitation link, accept invitation, temporary password fallback | [Identity Onboarding Rules](api/IDENTITY_ONBOARDING_RULES.md) | [API Surface Rules](api/API_SURFACE_RULES.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| Role policy, scoped RBAC, SystemAdmin/Manager/Staff/Technician/OrgAdmin | [Authorization Rules](api/AUTHORIZATION_RULES.md) | [API Surface Rules](api/API_SURFACE_RULES.md), [Multi-Tenancy Rules](architecture/MULTI_TENANCY_RULES.md) |
| Naming conventions for entities, fields, APIs, application use cases | [Naming Rules](process/NAMING_RULES.md) | [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md), [API Surface Rules](api/API_SURFACE_RULES.md) |
| EF Core indexes, soft delete, unique constraints, snapshots, partitioning | [Data Modeling Rules](data/DATA_MODELING_RULES.md) | [JSON Field Rules](data/JSON_FIELD_RULES.md), [Idempotency and Retry Rules](data/IDEMPOTENCY_RETRY_RULES.md) |
| JSONB fields, payloads, snapshots, robot parameters, schema versions | [JSON Field Rules](data/JSON_FIELD_RULES.md) | [Data Modeling Rules](data/DATA_MODELING_RULES.md) |
| Idempotency, retry fields, dead letters, callback deduplication | [Idempotency and Retry Rules](data/IDEMPOTENCY_RETRY_RULES.md) | [Data Modeling Rules](data/DATA_MODELING_RULES.md) |
| Tenant isolation, Organization/Store/Kiosk scope, override hierarchy | [Multi-Tenancy Rules](architecture/MULTI_TENANCY_RULES.md) | [Authorization Rules](api/AUTHORIZATION_RULES.md), [Data Modeling Rules](data/DATA_MODELING_RULES.md) |
| Tenant tree, tenant scope lookup, RBAC scope selector | [Multi-Tenancy Rules](architecture/MULTI_TENANCY_RULES.md) | [API Surface Rules](api/API_SURFACE_RULES.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| System overview, version roadmap, source-of-truth split, which flow doc to read | [System Flows](flows/SYSTEM_FLOWS.md) | [System Overview Flow](flows/SYSTEM_OVERVIEW_FLOW.md) |
| Back-office setup, tenant/account/catalog/menu preparation | [Back-Office Setup Flow](flows/BACK_OFFICE_SETUP_FLOW.md) | [API Surface Rules](api/API_SURFACE_RULES.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| Management dashboard, GraphQL read model, aggregated management reads | [Management Read Flow](flows/MANAGEMENT_READ_FLOW.md) | [API Surface Rules](api/API_SURFACE_RULES.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| SignalR realtime UI updates, hub routes, event names, smoke test | [SignalR Realtime Contract](api/SIGNALR_REALTIME_CONTRACT.md) | [API Surface Rules](api/API_SURFACE_RULES.md), [SignalR Smoke Test](operations/SIGNALR_SMOKE_TEST.md) |
| Catalog to runtime menu, menu sellability, Cloud menu vs Edge projection | [Catalog Runtime Menu Flow](flows/CATALOG_RUNTIME_MENU_FLOW.md) | [IoT Contract](iot/IOT_CONTRACT.md), [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md) |
| Fairino `.lua` export, global RobotArtifactTemplate, organization clone, RobotArtifact upload, RobotProgram RunOrder, release deployment, presigned download | [Robot Lua Artifact Flow](flows/ROBOT_LUA_ARTIFACT_FLOW.md) | [IoT Contract](iot/IOT_CONTRACT.md), [API Surface Rules](api/API_SURFACE_RULES.md) |
| Robot artifact migration, MinIO setup, operational smoke, Edge/controller integration test | [Robot Artifact Operational Smoke Test](operations/ROBOT_ARTIFACT_OPERATIONAL_SMOKE.md) | [Deployment Configuration](operations/DEPLOYMENT_CONFIG.md), [Robot Lua Artifact Flow](flows/ROBOT_LUA_ARTIFACT_FLOW.md) |
| Tablet/cloud/edge/payment/MQTT/execution flow | [Checkout Execution Flow](flows/CHECKOUT_EXECUTION_FLOW.md) | [IoT Contract](iot/IOT_CONTRACT.md), [API Surface Rules](api/API_SURFACE_RULES.md) |
| MQTT broker, ACL, TLS, Edge subscription setup | [MQTT Operations](operations/MQTT_OPERATIONS.md) | [Deployment Config](operations/DEPLOYMENT_CONFIG.md), [IoT Contract](iot/IOT_CONTRACT.md) |
| MQTT failure, pull latency, ACK latency, report lag, stale execution metrics | [Observability](operations/OBSERVABILITY.md) | [MQTT Operations](operations/MQTT_OPERATIONS.md), [IoT Contract](iot/IOT_CONTRACT.md) |
| Operations support, telemetry, heartbeat, device events, refund support | [Operations Support Flow](flows/OPERATIONS_SUPPORT_FLOW.md) | [API Surface Rules](api/API_SURFACE_RULES.md), [IoT Contract](iot/IOT_CONTRACT.md) |
| Maintenance ticket lifecycle, staff support ticket, technician assignment | [Maintenance Ticket Flow](flows/MAINTENANCE_TICKET_FLOW.md) | [Operations Support Flow](flows/OPERATIONS_SUPPORT_FLOW.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| Failure flows, edge offline, duplicate notifications, retry behavior | [Failure Flows](flows/FAILURE_FLOWS.md) | [Idempotency and Retry Rules](data/IDEMPOTENCY_RETRY_RULES.md), [IoT Contract](iot/IOT_CONTRACT.md) |
| Exact tablet-edge-cloud API/message contract | [IoT Contract](iot/IOT_CONTRACT.md) | [Checkout Execution Flow](flows/CHECKOUT_EXECUTION_FLOW.md), [API Surface Rules](api/API_SURFACE_RULES.md) |
| Current Edge runtime contract and artifact deployment | [IoT Contract](iot/IOT_CONTRACT.md) | [Robot Lua Artifact Flow](flows/ROBOT_LUA_ARTIFACT_FLOW.md), [Data Modeling Rules](data/DATA_MODELING_RULES.md) |

## Common Query Hints

| Query contains | Useful filters or docs |
| --- | --- |
| `auth`, `login`, `forgot password`, `reset password`, `refresh token` | [API Surface Rules](api/API_SURFACE_RULES.md), section `Authentication And Password Recovery APIs` |
| `deploy`, `deployment`, `env`, `appsettings`, `JWT`, `SMTP`, `PayOS`, `Firebase`, `health`, `info` | [Deployment Configuration](operations/DEPLOYMENT_CONFIG.md) |
| `observability`, `Serilog`, `OpenTelemetry`, `Aspire`, `OTLP`, `trace`, `metric`, `debug body logging` | [Observability](operations/OBSERVABILITY.md), [Deployment Configuration](operations/DEPLOYMENT_CONFIG.md) |
| `manual verification`, `critical rule`, `smoke checklist`, `maintenance lifecycle test`, `payment webhook test` | [Backend Critical Rule Checklist](process/BACKEND_CRITICAL_RULE_CHECKLIST.md) |
| `invitation`, `accept invitation`, `admin creates account`, `temporary password`, `CreateInvitation`, `SendInvitationEmail` | [Identity Onboarding Rules](api/IDENTITY_ONBOARDING_RULES.md) |
| `management accounts`, `role scope`, `RBAC`, `policy`, `role catalog`, `permission matrix`, `roles.view`, `role-scope-options.view`, `effective access`, `me access` | [Authorization Rules](api/AUTHORIZATION_RULES.md), [API Surface Rules](api/API_SURFACE_RULES.md) |
| `store`, `organization`, `kiosk`, `tenant scope`, `role scope options` | [Multi-Tenancy Rules](architecture/MULTI_TENANCY_RULES.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| `order paid`, `ready for execution`, `tablet status`, `post-payment fan-out` | [Checkout Execution Flow](flows/CHECKOUT_EXECUTION_FLOW.md), [IoT Contract](iot/IOT_CONTRACT.md) |
| `refund required`, `edge offline`, `duplicate notification`, `retry` | [Failure Flows](flows/FAILURE_FLOWS.md), [Idempotency and Retry Rules](data/IDEMPOTENCY_RETRY_RULES.md) |
| `system overview`, `setup to sale`, `back-office setup`, `catalog to runtime menu`, `operations support`, `management dashboard` | [System Flows](flows/SYSTEM_FLOWS.md), then the matching flow file |
| `version roadmap`, `V2`, `V3`, `future version`, `production readiness`, `robot runtime next` | [System Overview Flow](flows/SYSTEM_OVERVIEW_FLOW.md), section `Version Direction` |
| `management orders`, `order status history`, `execution attempts`, `sourceCommandId`, `production execution record`, `refund`, `manual refund`, `stock movement`, `dispenser state`, `inventory refill` | [API Surface Rules](api/API_SURFACE_RULES.md), [Checkout Execution Flow](flows/CHECKOUT_EXECUTION_FLOW.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| `GraphQL`, `REST`, `read model aggregation`, `dashboard query`, `overview query` | [API Surface Rules](api/API_SURFACE_RULES.md), section `GraphQL Management Reads` |
| `SignalR`, `hub`, `OrderHub`, `OperationsHub`, `ManagementDashboardHub`, `realtime`, `DashboardInvalidated` | [SignalR Realtime Contract](api/SIGNALR_REALTIME_CONTRACT.md), [SignalR Smoke Test](operations/SIGNALR_SMOKE_TEST.md) |
| `heartbeat`, `readiness`, `capability projection`, `device event`, `kiosk event`, `production event checkpoint`, `state revision`, `operations telemetry`, `lost connection` | [API Surface Rules](api/API_SURFACE_RULES.md), [IoT Contract](iot/IOT_CONTRACT.md), [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md) |
| `maintenance ticket`, `support ticket`, `technician assignment`, `maintenance.create`, `maintenance.manage` | [Maintenance Ticket Flow](flows/MAINTENANCE_TICKET_FLOW.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| `alert`, `actionable telemetry`, `acknowledge alert`, `resolve alert`, `alerts.manage` | [Alert Lifecycle Flow](flows/ALERT_LIFECYCLE_FLOW.md), [IoT Contract](iot/IOT_CONTRACT.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| `device management`, `devices`, `retire device`, `device status`, `devices.view`, `devices.manage` | [API Surface Rules](api/API_SURFACE_RULES.md), section `Internal Management APIs`; [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| `missing API`, `future API`, `planned API`, `alert API`, `robot job API`, `offline session`, `audit events`, `reports API` | [API Surface Rules](api/API_SURFACE_RULES.md), section `Planned / Missing API Surfaces` |
| `soft delete`, `unique index`, `DeletedAt IS NULL` | [Data Modeling Rules](data/DATA_MODELING_RULES.md) |
| `PayloadJson`, `SnapshotJson`, `ConfigJson`, `JSONB` | [JSON Field Rules](data/JSON_FIELD_RULES.md) |
| `SyncEventInbox`, `NextRetryAt`, `LockedUntil`, `dead letter`, `retry audit` | [Idempotency and Retry Rules](data/IDEMPOTENCY_RETRY_RULES.md), [API Surface Rules](api/API_SURFACE_RULES.md) |
| `ProductVariant`, `MenuItem` | [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md), then owning context docs/code |
| `product template`, `products/from-template`, `organization products`, `organization menus` | [API Surface Rules](api/API_SURFACE_RULES.md), then [Multi-Tenancy Rules](architecture/MULTI_TENANCY_RULES.md) |
| `RobotArtifact`, `RobotProgramArtifact`, `ConfigurationRelease`, `ExecutionRoute`, `KioskExecutionEndpoint`, `EdgeCommand` | [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md), then [IoT Contract](iot/IOT_CONTRACT.md) |
| `configuration release authoring`, `ProductVariant Recipe RobotProgram lookup`, `authoring options` | [Robot Lua Artifact Flow](flows/ROBOT_LUA_ARTIFACT_FLOW.md), then [API Surface Rules](api/API_SURFACE_RULES.md) |
| `execution endpoint provisioning`, `FullEdgeRuntimeId`, `ControllerId`, `supported robot target`, `credential rotation` | [Robot Lua Artifact Flow](flows/ROBOT_LUA_ARTIFACT_FLOW.md), then [API Surface Rules](api/API_SURFACE_RULES.md) and [IoT Contract](iot/IOT_CONTRACT.md) |
| `Fairino Studio`, `.lua`, `RunOrder`, `presigned artifact download`, `artifact deployment` | [Robot Lua Artifact Flow](flows/ROBOT_LUA_ARTIFACT_FLOW.md) |
| `local edge db`, `ProductionJob`, `artifact cache`, `workcell scheduler` | [IoT Contract](iot/IOT_CONTRACT.md); use [Historical Step-First Local Edge Runtime ERD](iot/HISTORICAL_STEP_FIRST_LOCAL_EDGE_RUNTIME_ERD.md) only when explicitly comparing the removed step-first proposal |

## Related Docs

- [Documentation Rules](process/DOCUMENTATION_RULES.md)
- [Working Protocol](process/WORKING_PROTOCOL.md)
- [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md)
- [API Surface Rules](api/API_SURFACE_RULES.md)
