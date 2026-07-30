# Repo Truth Map

Source priority used: code in `src/` (structure only, not modified) → backend docs in `docs/` → `ARCHITECTURE.md` / `AGENTS.md` → inferred assumptions (marked `[Inferred]`).

## 1. System Purpose

IceBot is an ASP.NET Core backend for a **multi-location automated vending system with robot-arm integration** (ice cream / beverage style kiosks). It supports tablet-based customer checkout, QR/bank-transfer payment, robot-arm order fulfillment at edge kiosks, and centralized back-office management (organizations, stores, kiosks, catalog, robot configuration, inventory, operations).

Evidence: `ARCHITECTURE.md:1-3`.

## 2. Architecture Style

- Clean Architecture boundaries at the project level, organized as a **Modular Monolith** (not microservices).
- Bounded-context grouping for business ownership; Tactical DDD where domain rules matter; CQRS-lite for complex workflows.
- Event-driven integration for sync, robot runtime, payment callbacks, and operational events.
- EF Core is the primary unit-of-work/persistence model, backed by PostgreSQL.
- Compile-time dependency chain: `WebAPI -> Infrastructure -> Application -> Domain` (Domain has no outward dependencies).
- Edge/Cloud split: Edge (local kiosk runtime) owns robot execution, local device communication, telemetry, offline tolerance; Cloud owns org/store/kiosk management, catalog/config templates, reporting, payment integration, central sync coordination. Synchronization is event-oriented (inbox, idempotency keys, correlation/causation ids, retry, dead-letter).

Evidence: `ARCHITECTURE.md:5-37, 137-174`.

## 3. Main Actors

Internal RBAC roles (`docs/api/AUTHORIZATION_RULES.md:21-25`):

| Role | Responsibility |
| --- | --- |
| `SystemAdmin` | System-wide administration, accounts, permissions, security, platform health |
| `Manager` | Business/operations management across kiosks, reports, menus, pricing, maintenance coordination |
| `Staff` | On-site operations: refill, cleaning, status checks, issue reporting, manual support/refund handling |
| `Technician` | Installation, robot/kiosk setup, technical maintenance, troubleshooting, device/robot configuration |
| `OrgAdmin` | Organization admin managing resources within an assigned organization scope |

Other actors (non-internal-account):

- **Customer** — tablet/checkout client, no login; uses public v1 endpoints and an order-scoped `orderAccessToken`. (`docs/api/API_SURFACE_RULES.md:83-111`)
- **Tablet (kiosk client)** — owns transient UI/cart/QR state only, never starts robot execution directly. (`docs/iot/IOT_CONTRACT.md:26-35`)
- **Local Edge Backend / kiosk runtime** — owns runtime execution truth, local queue, telemetry; authenticates via mTLS (Full Edge) or ECDSA P-256 signed requests (low-cost controller). (`docs/iot/IOT_CONTRACT.md:37-48`, `docs/api/API_SURFACE_RULES.md:176-220`)
- **Payment provider (PayOS)** — calls webhook, provides checkout/QR session. (`src/WebAPI/Controllers/Payments/PayOsWebhookController.cs`)

## 4. Main Modules / Bounded Contexts

Per `docs/architecture/BOUNDARY_CONTEXTS.md:16-32` (namespace `Domain.<Context>`):

| Context | Owns |
| --- | --- |
| Identity | accounts, roles, notification devices, refresh tokens, password reset |
| Tenants | organizations, stores, kiosks, tenant scope |
| Catalog | products, variants, options, recipes, ingredients |
| Sales Catalog | menus, menu items, sellable offers, pricing |
| Orders | order lifecycle, order items, historical snapshots, production incidents |
| Payments | payment transactions, callbacks, refunds, payment methods |
| Robot Configuration | robot Lua artifacts, reusable robot manifests (`RobotProgram`, `RobotArtifact`) |
| Production Configuration | configuration releases, routes, robot bindings, deployment records |
| Production Packages | reusable package/version manifests, deterministic installation provenance |
| Production Execution | Cloud execution/audit projections from executor evidence |
| Devices | device catalog, telemetry, heartbeats, kiosk execution endpoints |
| Inventory | dispenser state, stock movements |
| Operations | alerts, maintenance tickets, operation logs |
| Sync | edge-cloud inbox/dead-letters, dispatch-only edge commands |
| Common | base entities, shared abstractions/primitives |

Application-layer folders mirror these contexts (`src/Application/{Catalog,SalesCatalog,Orders,Payments,RobotConfiguration,ProductionConfiguration,ProductionPackages,Devices,Inventory,Operations,Sync,Identity,Tenants,Dashboard,EdgeIntegration}`), confirmed by directory listing.

## 5. Main Business Flows

Flow index: `docs/flows/SYSTEM_FLOWS.md`. Key flows:

1. **Back-office setup** — org/account/catalog/menu provisioning before selling (`docs/flows/BACK_OFFICE_SETUP_FLOW.md`, routed).
2. **Catalog → Sales Catalog → runtime menu → tablet** projection (`docs/flows/CATALOG_RUNTIME_MENU_FLOW.md`, routed).
3. **Robot Lua artifact authoring/release/deployment** — Fairino `.lua` export → artifact/program materialize → configuration release publish → Edge deployment (`docs/flows/ROBOT_LUA_ARTIFACT_FLOW.md`, routed).
4. **Checkout → Payment → Execution** (`docs/flows/CHECKOUT_EXECUTION_FLOW.md:11-72`):
   - Tablet fetches runtime menu from Edge → customer confirms checkout → Cloud re-validates catalog/kiosk/store state, creates `Order`/`OrderItems` (`PendingPayment`) → creates `PaymentTransaction` + provider session → returns QR → provider webhook verifies payment → Cloud sets `PaymentTransaction=Paid`, `Order=ReadyForFulfillment` in one transaction → dispatches `ExecuteOrder` attempt 1 (idempotent by `(OrderId, DispatchAttemptNo)`) → MQTT best-effort wake-up → Edge pulls command, runs fast readiness check, accepts, executes robot program → Edge syncs execution events back to Cloud → Cloud finalizes `Order=Completed`.
   - Payment success and robot execution are explicitly decoupled; a reconciliation worker repairs missed dispatch.
5. **Production incident resolution** — inspection/discard/exact-unit remake/refund-or-voucher when output is defective or outcome unknown (`docs/flows/PRODUCTION_INCIDENT_RESOLUTION_FLOW.md`, routed).
6. **Production Package installation/upgrade** — franchise-oriented deterministic package composition, preview/materialize/cutover/rollback (`docs/flows/PRODUCTION_PACKAGE_INSTALLATION_FLOW.md`, `PRODUCTION_PACKAGE_UPGRADE_FLOW.md`, routed).
7. **Operations support** — telemetry, heartbeat, device events, inventory reporting, manual support/maintenance tickets (`docs/flows/OPERATIONS_SUPPORT_FLOW.md`, `MAINTENANCE_TICKET_FLOW.md`, routed).

## 6. API Surface Summary

Surface categories (`docs/api/API_SURFACE_RULES.md:24-32`):

| Surface | Route pattern | Auth |
| --- | --- | --- |
| Tablet/customer | `/api/v1/kiosks/...`, `/api/v1/orders...` | Public v1 + idempotency/validation, order-scoped bearer token |
| Internal management | `/api/v1/management/...` | JWT + scoped RBAC policy |
| Current account | `/api/v1/me...` | JWT |
| Authentication | `/api/v1/authentication...` | Mixed public/login + token |
| Payment provider webhook | `/api/v1/payments/.../webhook` | Provider signature verification |
| IoT/edge | `/api/v1/iot/...` | mTLS (Full Edge) or ECDSA P-256 signed request (low-cost) |
| Operations health/info | `/health...`, `/info` | Public probe |

Also exposed: GraphQL at `/graphql` (read-only management aggregation, resolvers delegate to CQRS query handlers) and SignalR hubs `/hubs/orders`, `/hubs/operations`, `/hubs/management-dashboard` (push-based UI deltas, not the robot runtime bus). Evidence: `docs/api/API_SURFACE_RULES.md:366-404`.

Controller inventory (by folder, `src/WebAPI/Controllers/`):

- **Catalog**: ManagementIngredients, ManagementProductCategories, ManagementProductOptions, ManagementProducts, ManagementProductTemplateOptions, ManagementProductTemplateRecipes, ManagementProductTemplates, ManagementRecipes
- **Devices**: ManagementDeviceCatalog, ManagementDevices, ManagementExecutionEndpoints, ManagementKioskTelemetry
- **Identity**: Authentication, CurrentAccount, ManagementAccounts, ManagementAuthorization, ManagementRoles
- **Inventory**: ManagementInventory, ManagementInventoryTopology
- **IoT**: BatchEvents, DeviceEvents, ExecutionCommands, ExecutionReadiness, ExecutionReports, KioskHeartbeats, ProductionSync
- **Operations**: ManagementAlerts, ManagementKioskOperationLogs, ManagementMaintenanceTickets, ManagementNotificationDeliveries, ManagementSyncDeadLetters
- **Orders**: ManagementExecutionAttempts, ManagementOrders, ManagementProductionIncidents, Orders (tablet-facing)
- **Payments**: ManagementPaymentDiagnostics, ManagementPaymentMethods, ManagementPaymentOperations, ManagementRefunds, PayOsWebhook
- **ProductionConfiguration**: ManagementConfigurationDeployments, ManagementConfigurationInventoryReadiness, ManagementConfigurationReleases
- **ProductionPackages**: ManagementProductionPackageInstallations, ManagementProductionPackages
- **RobotConfiguration**: ManagementRobotArtifacts, ManagementRobotArtifactTechnicalContracts, ManagementRobotArtifactTemplates, ManagementRobotAuthoringImports, ManagementRobotPrograms
- **SalesCatalog**: KioskRuntimeMenus (tablet), ManagementMenus
- **Tenants**: ManagementFranchiseOnboardings, ManagementKiosks, ManagementOrganizations, ManagementRoleScopeOptions, ManagementStores

IoT/Edge concrete routes (`docs/api/API_SURFACE_RULES.md:183-192`): commands pull/ack/reports, device-events, telemetry-events, production-sync events/checkpoint/state-summaries, heartbeat, readiness — all under `/api/v1/iot/execution-endpoints/{endpointId}/...`.

## 7. Database / Entity Summary

Detailed entity inventory is in `deliverables/00_repo_evidence/database_inventory.md`. Summary:

- PostgreSQL via EF Core `IceBotDbContext` (`ARCHITECTURE.md:88-90`), `src/Infrastructure/Data/`, migrations at `src/Infrastructure/Migrations/`.
- One Domain assembly, one database; entities are namespace-grouped by bounded context (`src/Domain/{Identity,Tenants,Catalog,SalesCatalog,Orders,Payments,RobotConfiguration,ProductionConfiguration,ProductionExecution,ProductionPackages,Devices,Inventory,Operations,Sync,Common}`).
- Multi-tenancy root: `Organization` → `Store` → `Kiosk`; `TenantScopeType` hierarchy `Device > Kiosk > Store > Organization > Global` (`ARCHITECTURE.md:182-194`).
- JSON fields permitted for robot SDK payloads, provider payloads, snapshots, metadata; workflow-critical values must be typed columns (`ARCHITECTURE.md:176-180`, `docs/data/JSON_FIELD_RULES.md`).
- Cross-context references are intentional and documented (Orders→Tenants/SalesCatalog/Catalog, Payments→Orders, Production Configuration→Catalog/RobotConfiguration, Inventory→Devices/Tenants/Catalog, Operations→Accounts/Devices/Orders/Tenants). Evidence: `docs/architecture/BOUNDARY_CONTEXTS.md:283-293`.

## 8. IoT / Robot / Payment / Sync Responsibilities

- **IoT / Edge contract** (`docs/iot/IOT_CONTRACT.md`): Tablet owns transient UX only; Local Edge Backend owns runtime menu projection, inventory/device/robot availability, local execution queue, telemetry; Cloud owns `Order`, `PaymentTransaction`, payment verification, executable-order command creation, final state/analytics/audit. MQTT is notification-only (no large payloads, not source of truth); Edge must still pull commands via API and poll periodically. Security: Full Edge = mTLS cert pinned by SHA-256 fingerprint; low-cost controller = ECDSA P-256 signed request + nonce dedup over TLS.
- **Robot Configuration**: immutable exported Lua artifacts (`RobotArtifact`) and reusable manifests (`RobotProgram`, child `RobotProgramArtifact`); configuration-time only, does not own runtime execution state (`docs/architecture/BOUNDARY_CONTEXTS.md:144-160`).
- **Production Configuration/Execution**: `ConfigurationRelease` binds catalog variant/recipe to robot programs via `ExecutionRoute`/`ExecutionRouteRobotBinding`; Cloud-side execution projections (`OrderExecutionRecord`, `ProductionExecutionRecord`) are audit/read models built from accepted executor evidence — Cloud has no live `RobotJob`/scheduler (`docs/architecture/BOUNDARY_CONTEXTS.md:162-187`, `docs/iot/IOT_CONTRACT.md:112-116`).
- **Payments**: `PaymentMethod`, `PaymentTransaction`, `PaymentCallback`, `Refund` in `Domain.Payments`; PayOS is the current provider (`PayOsWebhookController`); current refund phase is manual cash refund only — no automatic provider refund/payout assumed (`docs/architecture/BOUNDARY_CONTEXTS.md:125-142`). Payment webhook verification is decoupled from Edge dispatch (`docs/flows/CHECKOUT_EXECUTION_FLOW.md:105-113`).
- **Sync**: `SyncEventInbox`, `SyncDeadLetter`, `EdgeCommand`, `EdgeCommandDeliveryAttempt` in `Domain.Sync`; business contexts must not depend on Sync entities directly, only expose idempotency/correlation/causation/version/origin-node fields for sync infrastructure (`docs/architecture/BOUNDARY_CONTEXTS.md:235-248`).

## 9. Evidence References By File Path

- `AGENTS.md` — operational rules, doc routing, workflow, verification commands.
- `ARCHITECTURE.md` — architecture style, layering, edge/cloud model, multi-tenancy, JSON rules, event patterns.
- `docs/README.md` — backend docs folder map and key-doc index.
- `docs/DOCUMENTATION_ROUTING_MAP.md` — topic-to-document routing table.
- `docs/architecture/BOUNDARY_CONTEXTS.md` — bounded-context ownership, entity lists, cross-context references.
- `docs/api/API_SURFACE_RULES.md` — API surface categories, route ownership/naming, GraphQL/SignalR surfaces, validation strategy.
- `docs/api/AUTHORIZATION_RULES.md` — role catalog, permission matrix (partial read, lines 1-154).
- `docs/flows/SYSTEM_FLOWS.md` — flow index.
- `docs/flows/CHECKOUT_EXECUTION_FLOW.md` — checkout/payment/execution flow detail (lines 1-120 read).
- `docs/iot/IOT_CONTRACT.md` — tablet/edge/cloud source-of-truth split, envelope, idempotency, security.
- `deliverables/DELIVERABLES_AGENT.md` — deliverables authoring rules for this folder.
- `src/WebAPI/Controllers/**` — controller inventory (directory listing only, contents not opened).
- `src/Domain/**`, `src/Application/**`, `src/Infrastructure/**` — bounded-context folder structure (directory listing only).

## 10. Open Questions

- Exact request/response DTO shapes per endpoint were not inspected (out of scope per "smallest relevant docs first"); Swagger/OpenAPI or controller source would be the authoritative source if needed.
- Full permission matrix in `docs/api/AUTHORIZATION_RULES.md` was only partially read (lines 1-154); remaining permission codes (beyond `program.manage`) are not enumerated here.
- Database physical details (indexes, constraints) were not inspected in this pass — see `database_inventory.md` for that deliverable.
- `docs/flows/CHECKOUT_EXECUTION_FLOW.md` was read only through line ~120 (Tablet Status Flow onward not captured); later sections (failure/refund detail beyond what's summarized) were not fully reviewed.
- Whether GraphQL exposes mutations beyond the documented read-only phase should be reconfirmed against `src/WebAPI/GraphQL/` if a future deliverable needs exact schema detail.
