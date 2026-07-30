This file is a merged representation of a subset of the codebase, containing specifically included files, combined into a single document by Repomix.

# File Summary

## Purpose
This file contains a packed representation of a subset of the repository's contents that is considered the most important context.
It is designed to be easily consumable by AI systems for analysis, code review,
or other automated processes.

## File Format
The content is organized as follows:
1. This summary section
2. Repository information
3. Directory structure
4. Repository files (if enabled)
5. Multiple file entries, each consisting of:
  a. A header with the file path (## File: path/to/file)
  b. The full contents of the file in a code block

## Usage Guidelines
- This file should be treated as read-only. Any changes should be made to the
  original repository files, not this packed version.
- When processing this file, use the file path to distinguish
  between different files in the repository.
- Be aware that this file may contain sensitive information. Handle it with
  the same level of security as you would the original repository.

## Notes
- Some files may have been excluded based on .gitignore rules and Repomix's configuration
- Binary files are not included in this packed representation. Please refer to the Repository Structure section for a complete list of file paths, including binary files
- Only files matching these patterns are included: AGENTS.md, ARCHITECTURE.md, docs/**, deliverables/DELIVERABLES_AGENT.md
- Files matching patterns in .gitignore are excluded
- Files matching default ignore patterns are excluded
- Files are sorted by Git change count (files with more changes are at the bottom)

# Directory Structure
````
deliverables/
  DELIVERABLES_AGENT.md
docs/
  api/
    API_SURFACE_RULES.md
    AUTHORIZATION_RULES.md
    IDENTITY_ONBOARDING_RULES.md
    MANAGEMENT_API_SURFACE.md
    SIGNALR_REALTIME_CONTRACT.md
  architecture/
    BOUNDARY_CONTEXTS.md
    DEPENDENCY_RULES.md
    MULTI_TENANCY_RULES.md
  data/
    DATA_MODELING_RULES.md
    IDEMPOTENCY_RETRY_RULES.md
    JSON_FIELD_RULES.md
  flows/
    ALERT_LIFECYCLE_FLOW.md
    BACK_OFFICE_SETUP_FLOW.md
    CATALOG_RUNTIME_MENU_FLOW.md
    CHECKOUT_EXECUTION_FLOW.md
    FAILURE_FLOW_INDEX.md
    MAINTENANCE_TICKET_FLOW.md
    MANAGEMENT_READ_FLOW.md
    OPERATIONS_SUPPORT_FLOW.md
    PRODUCTION_INCIDENT_RESOLUTION_FLOW.md
    PRODUCTION_PACKAGE_INSTALLATION_FLOW.md
    PRODUCTION_PACKAGE_UPGRADE_FLOW.md
    ROBOT_LUA_ARTIFACT_FLOW.md
    ROBOT_LUA_AUTHORING_AND_IMPORT_FLOW.md
    ROBOT_LUA_DEPLOYMENT_AND_ACTIVATION_FLOW.md
    SYSTEM_FLOWS.md
    SYSTEM_OVERVIEW_FLOW.md
  iot/
    EDGE_COMMAND_CONTRACT.md
    EDGE_SYNC_TELEMETRY_CONTRACT.md
    IOT_CONTRACT.md
    TABLET_CLOUD_CONTRACT.md
  operations/
    API_SMOKE_TESTS.http
    DEPLOYMENT_CONFIG.md
    MQTT_OPERATIONS.md
    OBSERVABILITY.md
    RESTART_AND_POWER_RECOVERY.md
    ROBOT_ARTIFACT_OPERATIONAL_SMOKE.md
    SIGNALR_SMOKE_TEST.md
  process/
    BACKEND_CRITICAL_RULE_CHECKLIST.md
    DOCUMENTATION_RULES.md
    NAMING_RULES.md
    VERTICAL_SLICE_REVIEW.md
    WORKING_PROTOCOL.md
  DOCUMENTATION_COVERAGE.md
  DOCUMENTATION_ROUTING_MAP.md
  README.md
AGENTS.md
ARCHITECTURE.md
````

# Files

## File: deliverables/DELIVERABLES_AGENT.md
````markdown
# Deliverables Agent Instructions

This folder is for academic/project deliverables based on the IceBot Backend repository.

Source priority:
1. Code in src/
2. Existing backend source-of-truth docs in docs/
3. ARCHITECTURE.md and AGENTS.md
4. Inferred assumptions, clearly marked as inferred

Rules:
- Do not modify src/ unless explicitly asked.
- Do not modify docs/ unless explicitly asked.
- Write deliverables under deliverables/.
- Separate proven repo facts from inferred project interpretation.
- Every functional requirement must map to source evidence: endpoint, flow doc, entity, handler, controller, or architecture doc.
- Every database claim must map to Domain entities, EF configuration, DbContext, migrations, or data docs.
- Use Mermaid for diagrams unless asked for PlantUML.
- Prefer concise academic wording suitable for SRS/report submission.
- When unsure, create an Open Questions section instead of inventing behavior.
````

## File: docs/architecture/DEPENDENCY_RULES.md
````markdown
# Dependency Rules

This document defines dependency boundaries for the current modular monolith. The goal is to keep the codebase easy to split later without paying microservice complexity now.

## Search Keywords

`dependency rules`, `clean architecture`, `modular monolith`, `WebAPI`, `Infrastructure`, `Application`, `Domain`, `DbContext`, `unit of work`, `repository`, `thin repository`, `handler`, `controller`, `bounded context`, `layer boundary`, `EF Core`, `external adapter`, `provider adapter`, `microservice-ready`

## Project Dependencies

Current project direction:

```text
WebAPI -> Infrastructure -> Application -> Domain
```

Rules:

- `Domain` must not reference upper layers.
- `Application` may reference `Domain`.
- `Infrastructure` may reference `Application` and `Domain`.
- `WebAPI` may reference `Application` and `Infrastructure`.

`Infrastructure` owns EF Core and external adapters. `Application` owns use-case orchestration. `Domain` owns business rules.

## Domain Rules

Allowed in Domain:

- Entities.
- Value objects.
- Domain enums.
- Domain methods.
- Domain exceptions.
- Base abstractions such as `IAuditable`, `ISoftDeletable`, and sync/scoping interfaces.

Not allowed in Domain:

- EF Core attributes or DbContext usage.
- HTTP concepts.
- Logging framework calls.
- SDK/provider clients.
- Application DTOs.
- Infrastructure services.

## Application Rules

Application should contain:

- Commands and queries.
- Handlers/use cases.
- DTOs.
- Validators.
- Application interfaces for external dependencies.
- Transaction orchestration.

Application should not contain:

- Controller logic.
- EF Core mapping configuration.
- Provider SDK implementation details.
- Large generic CRUD service hierarchies.

DbContext can be used directly by handlers if the project chooses pragmatic EF access. Add repositories only when they express a real persistence boundary or complex reusable query.

When a repository abstraction exists, keep it thin:

- It may expose query composition entry points such as `IQueryable`.
- It may provide basic persistence operations such as add, update, remove, and soft delete.
- It should not own business decisions, workflow transitions, authorization, or response shaping.
- It should not hard-code one base entity shape if the domain uses multiple entity id/audit patterns.
- It should not hide eager-loading behavior behind string include lists.

Do not delete an existing repository abstraction during cleanup unless removal is explicitly requested or agreed as part of the fix. Prefer reshaping the abstraction to match the current architecture.

Repository/store audit conclusion:

- Current `I*Store` contracts are acceptable as thin persistence boundaries.
- The project direction is rich handler plus thin repository/store.
- `BaseRepository` is not the main Application persistence pattern. It may exist only as an Infrastructure helper when a present use case needs repeated low-level EF mechanics.
- Cross-context lookup methods are allowed when they are read-only validation helpers for the owning handler, such as checking parent store/kiosk/product/recipe existence before a command mutates its aggregate.
- These lookup methods must not grow into workflow orchestration, authorization, or response mapping.
- If a store method starts making business decisions, move that rule back to a handler, domain method, or focused rule helper.
- Do not introduce generic repositories to "standardize" store contracts. Standardize naming and behavior instead.

Do not refactor context stores to inherit from `BaseRepository` just because the helper exists. Current stores may keep direct EF Core access when that keeps the use case explicit.

Reusable techniques from older generic repository implementations:

- Use `AsNoTracking()` for read-only queries and projections.
- Keep soft-delete behavior consistent through EF filters or clearly named active/scoped store methods.
- Separate hard delete from business actions such as disable, revoke, cancel, or archive.
- Centralize low-risk timestamp mechanics in `IceBotDbContext.SaveChangesAsync`, not in generic CRUD methods.

Avoid carrying over these generic repository habits:

- string-based include lists;
- generic `GetAll` / `GetById` / `Update` methods as the default workflow API;
- hidden tenant-scope, authorization, state transition, or response mapping logic;
- base entity constraints that do not match all bounded contexts.

If `BaseRepository` is used later, it must not contain authorization, tenant-scope decisions, use-case validation, status transitions, payment/order/robot workflow, response mapping, or `ApiResult<T>` logic.

## Infrastructure Rules

Infrastructure owns:

- `IceBotDbContext`.
- EF Core migrations.
- Provider adapters.
- Fairino robot SDK integration.
- Payment provider integration.
- Sync workers.
- Background jobs.
- Technical persistence concerns.

Infrastructure should not add business rules that belong in Domain. It should translate external/provider details into application/domain concepts.

## WebAPI Rules

WebAPI owns:

- Controllers.
- Route contracts.
- Middleware.
- Authentication/authorization attributes.
- Swagger.
- HTTP request/response formatting.

Controllers should be thin. They should call application handlers/services and return `ApiResult<T>` or `PagedResult<T>`.

## Bounded Context Rules

- Prefer ids and snapshots across contexts.
- Domain entities do not expose cross-context navigation properties. Keep scalar foreign-key ids and database constraints; query-side Infrastructure code may join or project data from multiple contexts.
- Aggregate children do not expose live navigation properties to independent aggregate roots, even inside the same bounded context. Commands validate referenced aggregate ids explicitly, and published workflows consume immutable snapshots/manifests.
- Parent-child navigation inside one aggregate remains allowed when it represents the aggregate's invariant boundary.
- Keep navigation collections selective.
- Do not load large object graphs for API responses.
- Context-specific enums stay in that context.
- Shared primitives go in `Domain.Common` only when they are genuinely cross-context.
- Sync infrastructure may consume common idempotency/correlation/version fields, but business contexts should not depend on Sync entities.

## Data Rules

- EF Core `DbContext` is the unit of work.
- Use explicit transactions at use-case boundaries when needed.
- Do not hold database transactions across external network calls.
- `GuidEntity` IDs are application-generated UUID v7 values. Keep the database column type as PostgreSQL `uuid`.
- `LongEntity` IDs remain database-generated `long` values for catalog/reference rows.
- Detailed persistence, index, soft-delete, snapshot, and JSON rules live in [Data Modeling Rules](../data/DATA_MODELING_RULES.md).

## Related Docs

- [Architecture](../../ARCHITECTURE.md)
- [Working Protocol](../process/WORKING_PROTOCOL.md)
- [Boundary Contexts](BOUNDARY_CONTEXTS.md)
- [Naming Rules](../process/NAMING_RULES.md)
- [Authorization Rules](../api/AUTHORIZATION_RULES.md)
- [Data Modeling Rules](../data/DATA_MODELING_RULES.md)
- [Multi-Tenancy Rules](MULTI_TENANCY_RULES.md)
- [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
- [JSON Field Rules](../data/JSON_FIELD_RULES.md)
````

## File: docs/flows/FAILURE_FLOW_INDEX.md
````markdown
# Failure Flow Index

This document routes failure questions to the contract that owns the current state transition, retry, or recovery behavior. It does not redefine those rules.

## Search Keywords

`failure flow`, `paid but edge cannot execute`, `edge offline`, `duplicate notification`, `retry`, `partial output`, `outcome unknown`, `power recovery`, `refund required`

## Failure Ownership

| Failure | Owning document |
| --- | --- |
| Payment succeeds but fulfillment cannot start | [Checkout Execution Flow](CHECKOUT_EXECUTION_FLOW.md) |
| Partial, defective, or unknown production output | [Production Incident Resolution Flow](PRODUCTION_INCIDENT_RESOLUTION_FLOW.md) |
| Edge is offline while payment or dispatch completes | [Checkout Execution Flow](CHECKOUT_EXECUTION_FLOW.md) and [Edge Command Contract](../iot/EDGE_COMMAND_CONTRACT.md) |
| Duplicate requests, callbacks, command delivery, ACKs, reports, or sync events | [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md) |
| Cloud, database, MQTT, Edge, controller, tablet, or store power loss | [Restart And Power Recovery](../operations/RESTART_AND_POWER_RECOVERY.md) |
| MQTT delivery and credential failure | [MQTT Operations](../operations/MQTT_OPERATIONS.md) |
| Artifact download, verification, installation, or activation failure | [Robot Lua Deployment And Activation Flow](ROBOT_LUA_DEPLOYMENT_AND_ACTIVATION_FLOW.md) |
| Production Package installation failure | [Production Package Installation Flow](PRODUCTION_PACKAGE_INSTALLATION_FLOW.md) |
| Production Package upgrade materialization, cutover, or rollback failure | [Production Package Upgrade Flow](PRODUCTION_PACKAGE_UPGRADE_FLOW.md) |

## Boundary Rule

- Payment settlement, order fulfillment, machine execution, production incidents, and refunds are separate state machines.
- A downstream failure must not rewrite committed upstream evidence.
- Unknown or partial physical output requires incident resolution; it must not be inferred as a whole-order refund.
- Retry is allowed only under the idempotency and physical-output rules owned by the relevant workflow.

## Related Docs

- [System Flows](SYSTEM_FLOWS.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
- [Operations Support Flow](OPERATIONS_SUPPORT_FLOW.md)
- [Observability](../operations/OBSERVABILITY.md)
````

## File: docs/flows/MANAGEMENT_READ_FLOW.md
````markdown
# Management Read Flow

This document describes how management UI reads backend data through GraphQL read models and focused integration REST endpoints.

## Search Keywords

`management read flow`, `GraphQL read model`, `dashboard`, `tenantTree`, `orderOverview`, `kioskStatusOverview`, `inventorySummary`, `management dashboard`, `read model aggregation`, `REST read endpoint`

## Flow

```text
Management UI
  -> GraphQL read model
  -> Application query handler
  -> scoped store query
  -> DTO/read model result
```

Current GraphQL read model direction:

```text
dashboard
tenantTree
orderOverview
orders
order
orderStatusHistory
orderExecutionAttempts
kioskStatusOverview
inventorySummary
```

## Rules

- GraphQL is a read/query surface in the current phase.
- GraphQL resolvers are transport adapters and must delegate to Application query handlers.
- Domain/Application handlers remain the source of business behavior.
- REST remains the command/integration surface for mutations, tablet actions, webhooks, and IoT/edge contracts.
- Do not duplicate the same read surface in both REST and GraphQL unless there is a deliberate client/integration reason.
- Scoped RBAC still applies to management read models.

## Real-time Dashboard Invalidation

To ensure back-office managers view up-to-date reports without unnecessary polling, significant state mutations (such as orders, payments, maintenance tickets, and inventory updates) broadcast invalidation events:
- **`DashboardInvalidated`** is published on `ManagementDashboardHub` to the relevant dashboard groups (`dashboard:system`, `dashboard:organization:{organizationId}`, or `dashboard:store:{storeId}`).

Upon receiving a `DashboardInvalidated` event, the frontend dashboard client invalidates its local GraphQL query cache and triggers a refetch of:
- `dashboard`
- `orderOverview`
- `orders` and `order`
- `orderStatusHistory` and `orderExecutionAttempts`
- `kioskStatusOverview`
- `inventorySummary`

## Related Docs

- [System Flows](SYSTEM_FLOWS.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Authorization Rules](../api/AUTHORIZATION_RULES.md)
- [Multi-Tenancy Rules](../architecture/MULTI_TENANCY_RULES.md)
````

## File: docs/flows/PRODUCTION_INCIDENT_RESOLUTION_FLOW.md
````markdown
# Production Incident Resolution Flow

## Search Keywords

`production incident`, `outcome unknown`, `partial output`, `defective output`, `inspection`, `discard`, `exact-unit remake`, `compensation`, `manual intervention`

## Purpose

This flow handles production truth after a machine job fails, reports possible output, or requires manual intervention. It preserves successful output and inventory evidence while giving operations an explicit path for inspection, discard, exact-unit remake, or compensation.

## Ownership

- Orders owns `ProductionIncident`, inspection, operational resolution, and audit history.
- Production Execution owns immutable command/job/unit evidence.
- Payments owns refund records and settlement state.
- Inventory owns immutable stock movements. Incident resolution does not reverse consumption automatically.

## Automatic Opening

```text
Authenticated terminal production report
-> validate immutable job/unit provenance
-> persist execution and stock evidence
-> project Order/OrderItem state
-> create one incident for failed/manual-intervention job evidence
-> commit atomically
```

`PhysicalOutputState.No` infers `NotProduced`. `Yes` or `Unknown` requires inspection. Report retries return the existing incident because command/job provenance is unique.

The immutable production job range is the incident resolution granularity. When units in one dispatched line have different outcomes, Edge must report disjoint job ranges so Cloud can preserve and resolve each outcome independently; a mixed range is not silently split from ambiguous evidence.

## Staff Workflow

```text
Work queue
-> incident detail and history
-> inspect exact output range
-> select one idempotent resolution
-> execute linked remake/refund action when applicable
-> complete with staff notes
```

Inspection outcomes are `ConfirmedGood`, `NotProduced`, `Defective`, `PartialOrUncertain`, and `Unknown`. A resolution cannot be selected before inspection is known.

Resolution selection uses a client-generated `resolutionRequestId` and a backend-stored normalized request fingerprint. A retry must use the same complete payload.

Resolution rules:

- `DeliverExistingOutput` requires `ConfirmedGood`.
- `RequestRemake` requires `NotProduced` or `Defective`.
- A normal remake still requires failed evidence with confirmed no physical output.
- A defective-output remake additionally requires the matching incident, exact item/unit range, `Defective` inspection, and selected `RequestRemake` resolution.
- `RequestRefund` and `IssueVoucher` are V1 full-order compensation only and require explicit acknowledgement plus `refunds.manage` authorization.
- No resolution automatically deletes execution records, successful-unit evidence, or stock movements.

## API

```text
GET   /api/v1/management/production-incidents
GET   /api/v1/management/orders/{orderId}/production-incidents/{incidentId}
POST  /api/v1/management/orders/{orderId}/items/{orderItemId}/production-incidents
PATCH /api/v1/management/orders/{orderId}/production-incidents/{incidentId}/inspection
POST  /api/v1/management/orders/{orderId}/production-incidents/{incidentId}/resolution
PATCH /api/v1/management/orders/{orderId}/production-incidents/{incidentId}/complete
```

The manual-open endpoint reports a defective output against existing execution evidence; it cannot create arbitrary command/job provenance. All routes enforce the owning Order tenant scope and use `404` for scope mismatch.

## V1 Limits

- No automatic refund or provider payout.
- No partial-money refund; incident compensation is explicitly full-order.
- No sensor-based inference that an output was delivered or discarded.
- Resolution completion is an explicit staff audit action; automatic completion reconciliation can be added after Edge and provider completion contracts are stable.

## Related Docs

- [Checkout Execution Flow](CHECKOUT_EXECUTION_FLOW.md)
- [Failure Flow Index](FAILURE_FLOW_INDEX.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
````

## File: docs/flows/PRODUCTION_PACKAGE_UPGRADE_FLOW.md
````markdown
# Production Package Upgrade Flow

This document owns preview, materialization, review, cutover, rollback, abandonment, and reconciliation for upgrading an installed Production Package.

## Search Keywords

`production package upgrade`, `upgrade preview`, `previewChecksum`, `materialization`, `cutover`, `rollback`, `abandon upgrade`, `stale upgrade reconciliation`, `menu rollback evidence`, `successor installation`

## Upgrade Lifecycle

### Preview And Materialization

Upgrade applies only to an `Installed`, `PackageManaged` source installation and
a newer Published version of the same ProductionPackage. Preview compares the
immutable source manifest, current organization-owned commercial state, and the
incoming manifest. It returns added, removed, and changed Product source keys,
affected MenuItems, required execution endpoints, blockers, warnings, and a
deterministic `previewChecksum`.

Preview details identify each Product change, MenuItem rebind/deactivation,
Artifact reuse/materialization action, and frozen execution endpoint. Artifact
preview marks a `ReuseExistingCandidate` from package provenance, immutable
checksum, template, and technical-contract metadata. Execute still performs the
authoritative object size/checksum validation before reuse; publication status
alone does not decide reuse.

Execute requires the preview checksum and `Idempotency-Key`. Backend creates a
successor installation with new Catalog identities and reserved staging Product
and RobotProgram codes. It reuses only exact package-managed Artifact content
allowed by normal installation rules. Product/Variant/Option names, prices,
images, display order, and valid defaults are copied from the source graph;
Product/Variant preparation baselines and Recipe duration come from the incoming
package. MenuItem preparation time remains an organization-owned override.

If execution is interrupted, retrying the same payload and `Idempotency-Key`
resumes the same Upgrade and deterministic successor installation. A successor
identity is persisted before preparation evidence is written, so failure after
materialization cannot create a second Catalog or robot graph. Terminal retries
return the existing Upgrade. Reusing the key with another payload returns
`409`.

Execute revalidates the approved `previewChecksum` immediately before successor
materialization and again before preparation evidence is recorded. A changed
source catalog, MenuItem binding, or active endpoint scope is rejected as stale;
it is never silently added to the approved upgrade.

The successor stays independently reviewable. Existing Recipe, Artifact,
RobotProgram, ConfigurationRelease publication, and deployment APIs remain the
only way to publish and activate its technical graph. Upgrade cutover does not
bypass those lifecycle gates.

### Abandonment And Reconciliation

An operator may abandon a `ReadyForReview` or `Failed` upgrade through
`POST .../{upgradeId}/abandon` with a required reason. This keeps the source
installation active, marks the successor installation `Abandoned`, soft-deletes
its Product, RobotProgram, and ConfigurationRelease roots, and preserves
materialization and artifact provenance for audit. The command is idempotent.
Completed upgrades must use rollback instead.
Abandon returns `409` while successor Products are referenced by MenuItems or
the successor release has a non-failed deployment. Operators must first remove
the premature binding or restore deployment to the source release.

A background reconciler changes a `Materializing` upgrade with no persisted
progress within `ProductionPackageUpgrade:Reconciliation:MaterializingTimeoutMinutes`
to `Failed/UpgradeMaterializationTimedOut`. It does not infer a cutover or Edge
failure. The same idempotent execution may resume the successor, or an operator
may abandon it.

### Cutover

Cutover requires:

- the preview/materialization evidence still matches;
- both source and successor installations remain package-managed;
- the successor release is Published;
- every execution endpoint snapshotted by the upgrade points to an Active
  deployment row owned by the same organization, kiosk, endpoint, profile, and
  successor release;
- MenuItem bindings, allowed options, Product codes, and availability still
  match their typed before evidence.

Cutover runs under one database transaction and advisory locks. It moves source
Product codes to reserved historical codes, assigns canonical codes to the
successor, applies preserved availability, rebinds continuing MenuItems by
package source key, marks removed offerings Unavailable, and supersedes the
source installation. New package Products are not inserted into a Menu
automatically.

### Rollback

Rollback is two-phase and requires an operator reason. The first call creates
idempotent rollback deployments through the existing deployment rollback
handler and returns `202` while Edge activation is pending. Unknown, Pending,
Installed, or Active observation is never redispatched automatically. If an
observed rollback deployment is Failed, another call may create the next audited
deployment attempt, up to three attempts per endpoint. Recording each returned
deployment is serialized per upgrade endpoint, so duplicate idempotent dispatch
reuses the existing audit entry rather than creating another attempt. A repeated
call after every latest rollback deployment is Active verifies after-state checksums,
restores typed MenuItem/option bindings, availability, and canonical Product
codes, restores the source installation, and supersedes the successor.
Post-cutover Catalog or Menu binding changes cause `409`; rollback never
overwrites them silently.

Upgrade detail returns frozen endpoint targets, current rollback deployment
status/failure, and every rollback attempt with attempt number, replaced
deployment, actor, reason, and request time. Clients do not reconstruct this
audit history from deployment lists.

### Ownership And Concurrency

One Upgrade owns one source installation, one tenant scope, and a frozen Product,
MenuItem, and endpoint set. Fleet rollout coordinates multiple independent
upgrades outside this aggregate. A source installation can have only one active
upgrade. `OrganizationFork` installations require an explicit manual rebase and
cannot use this workflow.

Forking is rejected while an installation is either the source or successor of
an upgrade in `Materializing`, `ReadyForReview`, or `RollbackPending`. The guard
is rechecked under the same technical-resource mutation lock used by upgrade
execution and cutover. A successor may be forked after completed cutover, but
that technical ownership change invalidates package rollback and rollback then
returns `409` rather than overwriting the fork.

### Endpoint And Authorization Gates

The endpoint gate applies equally to Full Edge and Low-cost Controller
projections. The endpoint pointer is a fast projection, not sufficient evidence
by itself: Cloud also verifies the referenced deployment row and exact release
provenance. Missing, mismatched, Failed, or not-yet-Active deployment evidence
keeps cutover or rollback completion blocked; Cloud does not infer activation
from command delivery.

Upgrade preview derives `StoreId` and `KioskId` from the source installation.
The request cannot retarget an upgrade to another tenant scope. Authorization is
checked against that persisted owner.

Upgrade creates new Draft resources and a new release; it never mutates an
installed or active release. Recompose-after-authoring remains a separate
workflow and is not inferred from package upgrade.

## Related Docs

- [Production Package Installation Flow](PRODUCTION_PACKAGE_INSTALLATION_FLOW.md)
- [Robot Lua Artifact Flow](ROBOT_LUA_ARTIFACT_FLOW.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Management API Surface](../api/MANAGEMENT_API_SURFACE.md)
- [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
````

## File: docs/operations/RESTART_AND_POWER_RECOVERY.md
````markdown
# Restart And Power Recovery

## Search Keywords

`restart recovery`, `power loss`, `Cloud restart`, `Edge restart`, `controller restart`, `MQTT restart`, `database restart`, `restart policy`, `ManualOnly`, `physical output uncertainty`

## Purpose

This document defines recovery authority when Cloud, PostgreSQL, MQTT, Edge, a robot
controller, a tablet, or an entire store restarts. It does not claim that the current
Edge runtime implements controller recovery end to end.

## Production Restart Policy

Every newly published `RobotProgram` manifest snapshots `restartPolicy`. A configuration
release carries that immutable value, and each `ExecuteOrder` command repeats it for the
selected program.

V1 supports only:

```text
ManualOnly
```

Cloud rejects publication or command payloads that request another policy. Existing
schema-1 program manifests and schema-3 execute commands that do not contain the field
are interpreted as `ManualOnly`.

`NotRestartable`, `RestartFromBeginningIfNoPhysicalOutput`, and
`ResumeFromCheckpoint` are reserved typed values, not enabled behavior. In particular,
an accepted command must never be replayed automatically after a runtime restart.

## Runtime Interruption Report

When Edge or the controller restarts during an active production job, Edge reports the
affected job with:

```text
reportType: ProductionExecution
status: RequiresManualIntervention
sourceProductionJobId: <exact interrupted job>
errorCode: RuntimeRestarted | ControllerRestarted | PowerInterrupted
physicalOutputMayHaveOccurred: true | false | null
```

The report must retain the exact order item and production-unit range. `null` means that
physical output is unknown. A value of `false` is evidence, but it does not itself create
a new command. Any remake still uses the explicit remediation flow and its existing
eligibility checks.

Cloud preserves completed unit and inventory evidence. It does not convert an
interruption into a successful completion, automatically refund the order, resume Lua,
or restart the artifact list from the beginning.

## Recovery Matrix

| Failure | Durable authority | Recovery behavior |
| --- | --- | --- |
| Cloud process restart | PostgreSQL committed state | Hosted reconciliation jobs resume after startup. Uncommitted requests fail and callers must retry with the existing idempotency identity. |
| PostgreSQL restart | Last committed database state | In-flight transactions may fail. The application does not claim transparent transaction replay; background jobs retry on later cycles. |
| MQTT broker restart | PostgreSQL `EdgeCommand` state | MQTT is only a wake-up. Periodic authenticated command pull recovers missed notifications without changing `commandId`. |
| Edge restart before command acceptance | Cloud command state plus Edge durable inbox | Edge pulls again and deduplicates by `commandId`. |
| Edge restart after command acceptance | Edge durable production-job ledger plus Cloud report history | Do not redeliver or rerun the accepted command. Report every interrupted active job as `RequiresManualIntervention`. |
| Robot controller restart during Lua | Exact active production job and physical evidence | Default to `ManualOnly`; do not resume at an inferred Lua line or rerun from artifact 1. |
| Tablet/kiosk restart | Cloud order/payment state | Reload through normal read APIs and realtime subscriptions. UI restart does not change fulfillment state. |
| Whole-store power loss | Cloud committed state plus Edge durable local state | Stop new admission until readiness is fresh. Preserve paid and in-progress orders as operational uncertainty; resolve interrupted jobs manually. |
| Edge disk full or local database unhealthy | Last durable local job state plus Cloud report history | Publish `NotReady`, reject before acceptance, and stop admitting work. If failure occurs during an active job, report `RequiresManualIntervention/LocalPersistenceLost`; physical output defaults to unknown when evidence cannot be recovered. |

## Edge Persistence Requirement

Before accepting an `ExecuteOrder` command, Edge must durably retain at least:

- `commandId`, order and release provenance;
- production job identity and unit range;
- selected program, ordered artifacts, and checksums;
- current job lifecycle and the last known physical-output evidence;
- executor event counters needed to preserve idempotency after restart.

The same local transaction must persist the ACK outbox intent. Only after that commit
may Edge send `Accepted` with `localStatePersisted=true`. Failure to commit returns a
storage-specific `Rejected` acknowledgement and must not start the controller.

Readiness requires writable storage, sufficient free bytes, a healthy local database,
and event backlog within its configured maximum. Log retention must be bounded so logs
cannot consume the command/event reserve. The Edge implementation should reserve or
preallocate a minimal emergency journal for active-job identity and failure evidence;
when both local emergency persistence and Cloud delivery are unavailable, Cloud timeout
remains operational uncertainty rather than proof of success or failure.

If this state cannot be recovered, Edge reports uncertainty. It must not manufacture a
successful completion or assume that no product was produced.

## Excluded Checkpoint Resume Gate

`ResumeFromCheckpoint` must remain rejected until a controller integration can provide
a durable, attestable checkpoint containing the production job identity, program
manifest checksum, artifact/run order, checkpoint identity, controller execution
session, and physical-output evidence. `ProductionEventCheckpoint` is telemetry sync
state and is not a robot-motion checkpoint.

## Related Docs

- [Failure Flow Index](../flows/FAILURE_FLOW_INDEX.md)
- [Checkout Execution Flow](../flows/CHECKOUT_EXECUTION_FLOW.md)
- [Production Incident Resolution Flow](../flows/PRODUCTION_INCIDENT_RESOLUTION_FLOW.md)
- [Edge Command Contract](../iot/EDGE_COMMAND_CONTRACT.md)
````

## File: docs/process/NAMING_RULES.md
````markdown
# Naming Rules

This document defines naming conventions for IceBot domain, application, infrastructure, and API code. Prefer names that describe business ownership and runtime behavior instead of technical convenience.

## Search Keywords

`naming`, `code naming`, `entity name`, `GuidEntity`, `LongEntity`, `UUID v7`, `Id fields`, `foreign key`, `Status enum`, `Application names`, `Command`, `Query`, `Handler`, `Controller`, `route naming`, `JSON field names`, `retry names`, `idempotency names`, `file names`

## General Rules

- Use English names in code.
- Use PascalCase for types, methods, properties, enums, and enum values.
- Use camelCase for local variables and parameters.
- Use `Async` suffix for async methods that return `Task` or `ValueTask`.
- Avoid abbreviations unless they are stable domain terms, such as `Id`, `API`, `SDK`, `JWT`, or `URL`.
- Prefer precise domain names over generic names such as `Data`, `Info`, `Manager`, `Helper`, or `Processor`.
- Do not reuse names from old projects if they do not match the current domain model.

## Bounded Context Names

Folder and namespace names should follow bounded context ownership. The current context map lives in [Boundary Contexts](../architecture/BOUNDARY_CONTEXTS.md).

Use singular entity names:

```csharp
Order
OrderItem
Menu
MenuItem
ProductVariant
RobotProgram
RobotArtifact
ConfigurationRelease
EdgeCommand
StockMovement
```

Use plural folder names only for grouping:

```text
Entities/
Enums/
ValueObjects/
```

## Entity Base Names

Use the existing base entity names as semantic boundaries:

- `GuidEntity`: entity with app-generated `Guid` id.
- `LongEntity`: catalog/reference entity with database-generated `long` id.
- `BusinessEntity`: mutable business record with audit and soft delete.
- `CatalogEntity`: stable reference/catalog row with `Code`, `Name`, display order, and audit.
- `AppendOnlyEntity`: append-only audit/event-style record without soft delete.
- `AppendOnlySyncEntity`: append-only record that participates in edge-cloud sync.
- `RobotConfigurationEntity`: versioned robot configuration that can sync to edge.
- `SyncAggregateEntity`: mutable aggregate that participates in edge-cloud sync without becoming robot-runtime-specific.

Do not introduce another generic base such as `BaseFullEntity`. If a new base type is needed, name it after behavior, not inheritance convenience.

## Id Fields

Use `Id` for the primary key.

`GuidEntity` uses UUID v7 by default through `Domain.Common.GuidId.New()`.
This keeps the CLR/database type as `Guid`/PostgreSQL `uuid`, but makes new IDs time-ordered for better B-tree locality, write performance, and operational debugging.

Use UUID v7 for distributed/offline-created records, runtime records, append-only events, sync records, orders, payments, robot jobs, and tenant/topology entities.

Keep `LongEntity` for stable catalog/reference tables that do not need offline/global id creation.

Do not use primary keys as secrets. If a public opaque token is needed, add a dedicated token/code field and hash it when appropriate.

Use `{EntityName}Id` for foreign keys:

```csharp
OrderId
KioskId
SourceCommandId
PaymentTransactionId
```

Use nullable foreign keys only when the relationship is genuinely optional.

Use these external id names consistently:

- `Client...Id`: id created by tablet/POS/client before backend persistence.
- `Provider...Id`: id created by an external payment/provider system.
- `External...Id`: id from a non-owned external system when provider-specific naming is not appropriate.
- `EventId`: source event/message id.
- `SourceEventId`: upstream event that caused a state/ledger record.
- `CorrelationId`: traces one business flow.
- `CausationId`: command/event that caused the current command/event.

See [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md).

## Status And Enums

Use enum names that include the owning concept:

```csharp
OrderStatus
PaymentTransactionStatus
ProductionExecutionStatus
MaintenanceTicketStatus
```

Do not use raw `string Status` for stable domain states.

Use strings only for vendor/extensible values:

```csharp
VendorErrorCode
ExternalEventType
Provider
ProductType
DeviceCategory
```

Context-specific enums should live in the owning context. Shared primitives may live in `Domain.Common.Enums` only when genuinely cross-context.

## Audit, Soft Delete, And Sync Fields

Use audit names from `IAuditable`:

```csharp
CreatedAt
UpdatedAt
CreatedByAccountId
UpdatedByAccountId
```

Use soft-delete names from `ISoftDeletable`:

```csharp
DeletedAt
DeletedByAccountId
```

Do not use old names such as:

```text
ModifiedAt
IsDeleted
DeletedBy
```

Use sync names from `IRobotSyncEntity`:

```csharp
OriginNodeId
Version
SyncedAt
```

## Repository Names

Use repository names only for persistence boundaries:

```csharp
IBaseRepository<TEntity>
BaseRepository<TEntity>
IEdgeCommandStore
EdgeCommandStore
```

Repositories should stay thin. They may expose query composition and persistence operations, but should not contain workflow transitions or business decisions.

Repository placement and dependency rules live in [Dependency Rules](../architecture/DEPENDENCY_RULES.md).

Context-specific persistence ports may use `Store` when the term means a thin persistence boundary, for example:

```text
IOrganizationStore
OrganizationStore
```

Keep these under the owning context namespace such as `Application.Tenants.Abstractions` and `Infrastructure.Tenants.Persistence`. Do not move them into generic `Infrastructure.Persistence.Repositories` unless they are truly shared repository infrastructure.

Do not create generic service/controller names such as:

```text
BaseService<TEntity, TKey>
GenericController<TEntity, TKey>
CrudManager
```

## Application Names

Use feature/use-case names:

```text
PlaceOrder
ProcessPaymentCallback
CreatePaymentSession
DispatchEdgeCommand
RecordStockMovement
RetrySyncEvent
```

Application use cases should name the business action, not the persistence operation. Prefer `PlaceOrder` over `CreateOrder` because the customer is placing an order, not creating a database row. CRUD terms belong in repositories/stores/DbContext when they describe persistence operations.

Recommended type suffixes:

- `Command`: state-changing request.
- `Query`: read request.
- `Handler`: use-case implementation.
- `Request`: API/application input DTO.
- `Response`: API/application output DTO.
- `Validator`: validation class.

Examples:

```csharp
PlaceOrderCommand
PlaceOrderHandler
GetOrderExecutionQuery
OrderExecutionResponse
```

Avoid actor-based organization such as `AdminProductService` or `CustomerOrderService` unless the actor is part of the actual domain concept.

## API Names

Application names describe what the system does. WebAPI names describe what the client sees. Do not mirror Application use-case folders one-to-one in WebAPI.

Controller names should follow resource/capability names:

```csharp
OrdersController
PaymentsController
ConfigurationDeploymentsController
KiosksController
```

Route names should be stable and resource-oriented:

```text
/api/v1/orders
/api/v1/robot-jobs
/api/v1/me
```

API surface categories and route prefix ownership live in [API Surface Rules](../api/API_SURFACE_RULES.md). Naming rules should not duplicate the full route map.

Action method names may map to use cases:

```text
OrdersController.PlaceOrder -> PlaceOrderCommand
OrdersController.Cancel -> CancelOrderCommand
ConfigurationDeploymentsController.Deploy -> RequestConfigurationDeploymentCommand
```

Keep public route changes explicit and intentional.

## JSON Field Names

Use JSON suffixes by role:

- `*ConfigJson`: source-of-truth configuration.
- `*SettingsJson`: source-of-truth settings.
- `*ParametersJson`: command/robot parameters.
- `*SnapshotJson`: immutable historical snapshot.
- `PayloadJson`: external event/provider payload.
- `HeadersJson`: external message headers.
- `Raw*Json`: raw request/response evidence.
- `MetadataJson`: optional extension data only.

Source-of-truth configuration JSON should have a matching schema version:

```csharp
ProgramPayloadJson
ProgramPayloadSchemaVersion
```

See [JSON Field Rules](../data/JSON_FIELD_RULES.md).

## Retry And Idempotency Names

Use `IdempotencyKey` for retried client/API commands.

Use processing retry names for infrastructure/event processing:

```csharp
ProcessingAttempts
MaxProcessingAttempts
LastAttemptAt
NextRetryAt
LastError
LockId
LockedUntil
```

Use business retry names for workflow execution:

```csharp
RetryCount
MaxRetries
NextRetryAt
LastErrorCode
LastErrorMessage
```

## CQRS Handler And Service Names

Use CQRS-style handlers for controller-facing Application use cases.

```text
Controller-facing Application operation -> CommandHandler / QueryHandler
```

Examples:

```text
GetTenantTreeQueryHandler
CreateKioskCommandHandler
UpdateKioskCommandHandler
SetKioskStatusCommandHandler
PlaceOrderCommandHandler
CreatePaymentSessionCommandHandler
```

Handlers are use case boundaries. They should orchestrate the request flow, call stores/repositories, domain methods, policies, calculators, provider clients, or helper services as needed, and return `ApiResult<T>` when the result goes directly to WebAPI.

Use `Service` only when the class is a reusable capability, internal helper, domain policy/calculator, or integration component rather than a controller-facing use case.

Examples:

```text
AccountTokenService
RefreshTokenService
TenantScopeResolver
PriceCalculator
PayOsSignatureService
EmailSender
FirebaseClient
```

Do not create service wrappers that only repeat what a handler can do directly:

```text
Controller -> Handler -> CrudService -> Store
```

Prefer:

```text
Controller -> Handler -> Store/DbContext
```

Current convention: controller-facing Application use cases should be implemented as explicit handlers. If a remaining `*Service` exists, treat it as a reusable capability/internal helper unless direct controller usage proves otherwise.

## Application Feature Folder Names

Small bounded contexts may keep direct `Commands`, `Queries`, `Results`, and `Abstractions` folders.

Large bounded contexts should split by owned sub-feature first, then by use-case type:

```text
RobotConfiguration/
  Artifacts/
    Commands/
    Queries/
    Results/
    Abstractions/
  ArtifactTemplates/
  Programs/
  Storage/
```

Use this shape when a flat `Commands` or `Queries` folder starts mixing different aggregate owners, contracts, or persistence ports. Keep store interfaces in the sub-feature that owns the aggregate they persist, for example `IRobotArtifactStore` under `Artifacts` and `IRobotProgramStore` under `Programs`.

## Application Helper Folder Names

Use helper folder names by responsibility, not by habit.

`Rules` is for focused business or domain-facing decisions with a named business meaning.

Examples:

```text
maintenance access checks
product option selection rules
tenant scope rules
runtime menu sellability rules
```

`Support` is for technical contracts, parsers, normalizers, factories, and small helpers owned by one module. Support code should not orchestrate a workflow or hide a business transition.

Examples:

```text
JSON contract validator
request normalizer
metadata parser
audit factory
```

`Services` is for reusable orchestration, calculation, integration helpers, or multi-step capabilities that are not controller-facing use cases. A service may have dependencies and may coordinate several internal operations, but it should not become a generic CRUD layer.

Examples:

```text
release bundle builder
inventory readiness evaluator
token service
payment signature service
```

When unsure:

- Use `Rules` if the class answers "is this business action allowed?".
- Use `Support` if the class answers "how do we parse/normalize/validate this technical contract?".
- Use `Services` if the class performs a reusable multi-step capability or calculation.
- Keep controller-facing operations as `CommandHandler` / `QueryHandler`, not `Service`.

## Result Wrapper Names

Use `ApiResult<T>` for Application use cases whose result is returned directly by WebAPI controllers.

`ApiResult<T>` is API-facing and may carry response-oriented fields such as:

```text
StatusCode
ValidationErrors
BusinessError
SystemError
```

Use `InternalResult<T>` for internal helper operations inside an Application workflow or provider/orchestration step.

`InternalResult<T>` is not an HTTP response contract. It should stay small:

```text
Succeeded
Message
Data
Exception
Details
```

Do not add `StatusCode` or API response fields to `InternalResult<T>`.

Rule:

```text
Controller-facing use case -> ApiResult<T>
Internal workflow step -> InternalResult<T> or a purpose-built result type
```

For complex flows such as payment, sync, edge dispatch, or robot execution, internal steps may return `InternalResult<T>` and the final Application use case converts that internal result into `ApiResult<T>` at the API boundary.

## File Names

File name should match the primary type name:

```text
Order.cs
OrderStatus.cs
IBaseRepository.cs
BaseRepository.cs
```

Group related small enums by context only when it improves readability. Do not recreate a global `DomainEnums.cs` dumping ground for context-specific states.

## Related Docs

- [Architecture](../../ARCHITECTURE.md)
- [Working Protocol](WORKING_PROTOCOL.md)
- [Boundary Contexts](../architecture/BOUNDARY_CONTEXTS.md)
- [Dependency Rules](../architecture/DEPENDENCY_RULES.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Authorization Rules](../api/AUTHORIZATION_RULES.md)
- [Data Modeling Rules](../data/DATA_MODELING_RULES.md)
- [Multi-Tenancy Rules](../architecture/MULTI_TENANCY_RULES.md)
- [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
- [JSON Field Rules](../data/JSON_FIELD_RULES.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
- [Documentation Rules](DOCUMENTATION_RULES.md)
````

## File: docs/DOCUMENTATION_COVERAGE.md
````markdown
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
| Payments | `src/Application/Payments`, `src/Domain/Payments` | [API Surface Rules](api/API_SURFACE_RULES.md), [Idempotency and Retry Rules](data/IDEMPOTENCY_RETRY_RULES.md) | [Checkout Execution](flows/CHECKOUT_EXECUTION_FLOW.md) | `tests/IceBot.UnitTests/Payments` |
| Devices | `src/Application/Devices`, `src/Domain/Devices` | [Management API Surface](api/MANAGEMENT_API_SURFACE.md), [Edge Sync and Telemetry Contract](iot/EDGE_SYNC_TELEMETRY_CONTRACT.md) | [Operations Support](flows/OPERATIONS_SUPPORT_FLOW.md) | `tests/IceBot.UnitTests/Devices` |
| Inventory | `src/Application/Inventory`, `src/Domain/Inventory` | [Management API Surface](api/MANAGEMENT_API_SURFACE.md) | [Back-Office Setup](flows/BACK_OFFICE_SETUP_FLOW.md), [Operations Support](flows/OPERATIONS_SUPPORT_FLOW.md) | `tests/IceBot.UnitTests/Inventory` |
| Robot Configuration | `src/Application/RobotConfiguration`, `src/Domain/RobotConfiguration` | [Robot Lua Artifact Flow](flows/ROBOT_LUA_ARTIFACT_FLOW.md), [Edge Command Contract](iot/EDGE_COMMAND_CONTRACT.md) | [Robot Lua Authoring and Import](flows/ROBOT_LUA_AUTHORING_AND_IMPORT_FLOW.md), [Robot Artifact Smoke](operations/ROBOT_ARTIFACT_OPERATIONAL_SMOKE.md) | `tests/IceBot.UnitTests/RobotConfiguration` |
| Production Configuration | `src/Application/ProductionConfiguration`, `src/Domain/ProductionConfiguration` | [Management API Surface](api/MANAGEMENT_API_SURFACE.md), [Edge Command Contract](iot/EDGE_COMMAND_CONTRACT.md) | [Robot Lua Deployment and Activation](flows/ROBOT_LUA_DEPLOYMENT_AND_ACTIVATION_FLOW.md) | `tests/IceBot.UnitTests/ProductionConfiguration` |
| Production Packages | `src/Application/ProductionPackages`, `src/Domain/ProductionPackages` | [Management API Surface](api/MANAGEMENT_API_SURFACE.md) | [Package Installation](flows/PRODUCTION_PACKAGE_INSTALLATION_FLOW.md), [Package Upgrade](flows/PRODUCTION_PACKAGE_UPGRADE_FLOW.md) | `tests/IceBot.UnitTests/ProductionPackages` |
| Edge Integration | `src/Application/EdgeIntegration`, `src/Infrastructure/EdgeIntegration` | [IoT Contract](iot/IOT_CONTRACT.md), [Edge Command Contract](iot/EDGE_COMMAND_CONTRACT.md), [Edge Sync and Telemetry](iot/EDGE_SYNC_TELEMETRY_CONTRACT.md) | [Checkout Execution](flows/CHECKOUT_EXECUTION_FLOW.md), [Restart and Power Recovery](operations/RESTART_AND_POWER_RECOVERY.md) | `tests/IceBot.IntegrationTests/EdgeIntegration` |
| Operations | `src/Application/Operations`, `src/Domain/Operations` | [SignalR Contract](api/SIGNALR_REALTIME_CONTRACT.md), [Management API Surface](api/MANAGEMENT_API_SURFACE.md) | [Alert Lifecycle](flows/ALERT_LIFECYCLE_FLOW.md), [Maintenance Ticket](flows/MAINTENANCE_TICKET_FLOW.md), [Operations Support](flows/OPERATIONS_SUPPORT_FLOW.md) | `tests/IceBot.UnitTests/Operations` |
| Sync | `src/Application/Sync`, `src/Domain/Sync` | [Edge Sync and Telemetry](iot/EDGE_SYNC_TELEMETRY_CONTRACT.md), [Idempotency and Retry Rules](data/IDEMPOTENCY_RETRY_RULES.md) | [Failure Flow Index](flows/FAILURE_FLOW_INDEX.md) | `tests/IceBot.UnitTests/Sync` |
| Cross-cutting runtime | `src/WebAPI`, `src/Infrastructure` | [API Surface Rules](api/API_SURFACE_RULES.md), [Deployment Configuration](operations/DEPLOYMENT_CONFIG.md) | [Observability](operations/OBSERVABILITY.md), [MQTT Operations](operations/MQTT_OPERATIONS.md) | `tests/IceBot.IntegrationTests` |

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
````

## File: docs/api/MANAGEMENT_API_SURFACE.md
````markdown
# Management API Surface

This document owns the curated internal management REST and GraphQL route catalog. Controller attributes, generated OpenAPI, and the GraphQL schema remain the exact executable inventory. Cross-cutting route ownership, validation, response, and transport rules remain in [API Surface Rules](API_SURFACE_RULES.md).

## Search Keywords

`management API`, `internal management routes`, `back-office API`, `organization routes`, `kiosk routes`, `catalog management`, `production package API`, `deployment API`, `inventory management`, `GraphQL management reads`

## Route Catalog

Management APIs are for internal operations, not only the `Manager` role.

Current examples:

### Catalog And Sales Catalog Routes

```text
GET /api/v1/management/product-templates
POST/PUT/PATCH/DELETE /api/v1/management/product-templates/{productId}/option-groups/*
GET /api/v1/management/organizations/{organizationId}/products
POST /api/v1/management/organizations/{organizationId}/products/from-template
POST /api/v1/management/organizations/{organizationId}/products/{productId}/option-groups
PUT /api/v1/management/organizations/{organizationId}/products/{productId}/option-groups/{optionGroupId}
PATCH /api/v1/management/organizations/{organizationId}/products/{productId}/option-groups/{optionGroupId}/status
DELETE /api/v1/management/organizations/{organizationId}/products/{productId}/option-groups/{optionGroupId}
POST /api/v1/management/organizations/{organizationId}/products/{productId}/option-groups/{optionGroupId}/options
PUT /api/v1/management/organizations/{organizationId}/products/{productId}/option-groups/{optionGroupId}/options/{productOptionId}
PATCH /api/v1/management/organizations/{organizationId}/products/{productId}/option-groups/{optionGroupId}/options/{productOptionId}/availability
DELETE /api/v1/management/organizations/{organizationId}/products/{productId}/option-groups/{optionGroupId}/options/{productOptionId}
PUT /api/v1/management/organizations/{organizationId}/products/{productId}/option-groups/{optionGroupId}/options/{productOptionId}/ingredient-requirements
GET /api/v1/management/organizations/{organizationId}/menus
```

### Identity, Payments, And Tenant Routes

```text
GET /api/v1/management/accounts
GET /api/v1/management/accounts/{accountId}/effective-access
PUT /api/v1/management/accounts/{accountId}/roles
GET /api/v1/management/payment-methods
GET /api/v1/management/organizations
GET /api/v1/management/organizations/{organizationId}
POST /api/v1/management/organizations
PUT /api/v1/management/organizations/{organizationId}
PATCH /api/v1/management/organizations/{organizationId}/disable
PATCH /api/v1/management/organizations/{organizationId}/activate
GET /api/v1/management/stores
GET /api/v1/management/stores/{storeId}
POST /api/v1/management/organizations/{organizationId}/stores
PUT /api/v1/management/stores/{storeId}
PATCH /api/v1/management/stores/{storeId}/disable
PATCH /api/v1/management/stores/{storeId}/activate
PATCH /api/v1/management/organizations/{organizationId}/stores/{storeId}/sales-pause
PATCH /api/v1/management/organizations/{organizationId}/stores/{storeId}/sales-resume
GET /api/v1/management/kiosks
GET /api/v1/management/kiosks/{kioskId}
POST /api/v1/management/stores/{storeId}/kiosks
PUT /api/v1/management/kiosks/{kioskId}
PATCH /api/v1/management/kiosks/{kioskId}/status
```

### Device And Execution Endpoint Routes

```text
GET /api/v1/management/devices
GET /api/v1/management/kiosks/{kioskId}/devices/{deviceId}
POST /api/v1/management/kiosks/{kioskId}/devices
PUT /api/v1/management/kiosks/{kioskId}/devices/{deviceId}
PATCH /api/v1/management/kiosks/{kioskId}/devices/{deviceId}/status
DELETE /api/v1/management/kiosks/{kioskId}/devices/{deviceId}
PATCH /api/v1/management/kiosks/{kioskId}/execution-endpoints/{endpointId}/credential
POST /api/v1/management/kiosks/{kioskId}/execution-endpoints/{endpointId}/mqtt-credential
PATCH /api/v1/management/kiosks/{kioskId}/execution-endpoints/{endpointId}/mqtt-credential
DELETE /api/v1/management/kiosks/{kioskId}/execution-endpoints/{endpointId}/mqtt-credential
GET /api/v1/management/execution-endpoints
GET /api/v1/management/kiosks/{kioskId}/execution-endpoints/{endpointId}
POST /api/v1/management/kiosks/{kioskId}/execution-endpoints
PUT /api/v1/management/kiosks/{kioskId}/execution-endpoints/{endpointId}/supported-robot-targets
POST /api/v1/management/kiosks/{kioskId}/execution-endpoints/{endpointId}/provision
PATCH /api/v1/management/kiosks/{kioskId}/execution-endpoints/{endpointId}/disable
PATCH /api/v1/management/kiosks/{kioskId}/execution-endpoints/{endpointId}/reactivate
PATCH /api/v1/management/kiosks/{kioskId}/execution-endpoints/{endpointId}/retire
GET /api/v1/management/roles
GET /api/v1/management/role-scope-options
GET /api/v1/management/permission-matrix
```

### Ingredient And Recipe Routes

```text
GET /api/v1/management/ingredients
GET /api/v1/management/ingredients/{ingredientId}
POST /api/v1/management/ingredients
PUT /api/v1/management/ingredients/{ingredientId}
PATCH /api/v1/management/ingredients/{ingredientId}/status
DELETE /api/v1/management/ingredients/{ingredientId}
GET /api/v1/management/organizations/{organizationId}/products/{productId}/variants/{variantId}/recipes
GET /api/v1/management/organizations/{organizationId}/products/{productId}/variants/{variantId}/recipes/{recipeId}
POST /api/v1/management/organizations/{organizationId}/products/{productId}/variants/{variantId}/recipes
PUT /api/v1/management/organizations/{organizationId}/products/{productId}/variants/{variantId}/recipes/{recipeId}
PUT /api/v1/management/organizations/{organizationId}/products/{productId}/variants/{variantId}/recipes/{recipeId}/items
PATCH /api/v1/management/organizations/{organizationId}/products/{productId}/variants/{variantId}/recipes/{recipeId}/status
POST /api/v1/management/organizations/{organizationId}/products/{productId}/variants/{variantId}/recipes/{recipeId}/versions
GET /api/v1/management/product-templates/{productId}/variants/{variantId}/recipes
GET /api/v1/management/product-templates/{productId}/variants/{variantId}/recipes/{recipeId}
POST /api/v1/management/product-templates/{productId}/variants/{variantId}/recipes
PUT /api/v1/management/product-templates/{productId}/variants/{variantId}/recipes/{recipeId}
PUT /api/v1/management/product-templates/{productId}/variants/{variantId}/recipes/{recipeId}/items
PATCH /api/v1/management/product-templates/{productId}/variants/{variantId}/recipes/{recipeId}/status
POST /api/v1/management/product-templates/{productId}/variants/{variantId}/recipes/{recipeId}/versions
```

### Robot And Production Configuration Routes

```text
GET/POST /api/v1/management/robot-artifact-template-contracts
GET/PUT/DELETE /api/v1/management/robot-artifact-template-contracts/{contractId}
POST /api/v1/management/robot-artifact-template-contracts/{contractId}/validation-preview
PATCH /api/v1/management/robot-artifact-template-contracts/{contractId}/publish
PATCH /api/v1/management/robot-artifact-template-contracts/{contractId}/retire
PATCH /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/publish
PATCH /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/retire
DELETE /api/v1/management/robot-artifact-templates/{templateId}
PATCH /api/v1/management/organizations/{organizationId}/robot-programs/{programId}/retire
PATCH /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}/retire
PATCH /api/v1/management/organizations/{organizationId}/robot-programs/{programId}/publish
DELETE /api/v1/management/organizations/{organizationId}/robot-programs/{programId}
PATCH /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}/publish
DELETE /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}
POST /api/v1/management/kiosks/{kioskId}/configuration-deployments/full-edge
POST /api/v1/management/kiosks/{kioskId}/configuration-deployments/low-cost
POST /api/v1/management/kiosks/{kioskId}/configuration-deployments/preview
POST /api/v1/management/organizations/{organizationId}/robot-programs
PUT /api/v1/management/organizations/{organizationId}/robot-programs/{programId}/artifacts
POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports
GET /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}
GET /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/workspace
POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/validate
POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/materialize
POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/preview-composition
POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/confirm-composition
POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/publish-resources
POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/create-release-draft
POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/discard
POST /api/v1/management/organizations/{organizationId}/robot-artifacts
PATCH /api/v1/management/organizations/{organizationId}/robot-artifacts/publish
GET /api/v1/management/organizations/{organizationId}/robot-artifacts
GET /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}
GET /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/usage
POST /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/review-url
DELETE /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}
GET /api/v1/management/kiosks/{kioskId}/configuration-deployments/{deploymentId}/artifacts
GET /api/v1/management/organizations/{organizationId}/robot-programs
GET /api/v1/management/organizations/{organizationId}/robot-programs/{programId}
PUT /api/v1/management/organizations/{organizationId}/robot-programs/{programId}
GET /api/v1/management/organizations/{organizationId}/configuration-releases
GET /api/v1/management/organizations/{organizationId}/configuration-releases/authoring-options
GET /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}
POST /api/v1/management/organizations/{organizationId}/configuration-releases
PUT /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}/routes
GET /api/v1/management/configuration-deployments
GET /api/v1/management/kiosks/{kioskId}/configuration-deployments
GET /api/v1/management/kiosks/{kioskId}/configuration-deployments/{deploymentId}
POST /api/v1/management/kiosks/{kioskId}/configuration-deployments/{deploymentId}/rollback
```

### Orders, Payments, Inventory, And Operations Routes

```text
GraphQL orders
GraphQL order
GraphQL orderStatusHistory
GraphQL orderExecutionAttempts
GraphQL fulfillmentQueue
GraphQL orderItemStatusHistory
POST /api/v1/management/orders/{orderId}/execution-attempts
GET /api/v1/management/production-incidents
GET /api/v1/management/orders/{orderId}/production-incidents/{incidentId}
POST /api/v1/management/orders/{orderId}/items/{orderItemId}/production-incidents
PATCH /api/v1/management/orders/{orderId}/production-incidents/{incidentId}/inspection
POST /api/v1/management/orders/{orderId}/production-incidents/{incidentId}/resolution
PATCH /api/v1/management/orders/{orderId}/production-incidents/{incidentId}/complete
POST /api/v1/management/orders/{orderId}/items/{orderItemId}/manual-fulfillment-events
POST /api/v1/management/orders/{orderId}/items/{orderItemId}/fulfill
POST /api/v1/management/orders/{orderId}/items/{orderItemId}/fail
GET /api/v1/management/orders/{orderId}/execution-attempts/{sourceCommandId}/diagnostics
PATCH /api/v1/management/orders/{orderId}/cancel
PATCH /api/v1/management/orders/{orderId}/refund-required
GET /api/v1/management/refunds
GET /api/v1/management/refunds/{refundId}
POST /api/v1/management/orders/{orderId}/refunds
PATCH /api/v1/management/refunds/{refundId}/mark-processed
PATCH /api/v1/management/refunds/{refundId}/reject
PATCH /api/v1/management/refunds/{refundId}/cancel
GET /api/v1/management/inventory/dispenser-states
GET /api/v1/management/inventory/stock-movements
POST /api/v1/management/kiosks/{kioskId}/inventory/dispenser-states
PUT /api/v1/management/kiosks/{kioskId}/inventory/dispenser-states/{dispenserStateId}
PATCH /api/v1/management/kiosks/{kioskId}/inventory/dispenser-states/{dispenserStateId}/status
DELETE /api/v1/management/kiosks/{kioskId}/inventory/dispenser-states/{dispenserStateId}
POST /api/v1/management/kiosks/{kioskId}/inventory/dispenser-states/{id}/refill
POST /api/v1/management/kiosks/{kioskId}/inventory/dispenser-states/{id}/adjust-estimate
GET /api/v1/management/kiosks/{kioskId}/heartbeats
GET /api/v1/management/kiosks/{kioskId}/operation-logs
GET /api/v1/management/kiosks/{kioskId}/operation-logs/{operationLogId}
GET /api/v1/management/kiosks/{kioskId}/operation-logs/{operationLogId}/diagnostics
GET /api/v1/management/kiosks/{kioskId}/device-events
GET /api/v1/management/alerts
GET /api/v1/management/alerts/{alertId}
PATCH /api/v1/management/alerts/{alertId}/acknowledge
PATCH /api/v1/management/alerts/{alertId}/resolve
GET /api/v1/management/maintenance-tickets
GET /api/v1/management/maintenance-tickets/{ticketId}
POST /api/v1/management/maintenance-tickets
PUT /api/v1/management/maintenance-tickets/{ticketId}
PATCH /api/v1/management/maintenance-tickets/{ticketId}/assign
PATCH /api/v1/management/maintenance-tickets/{ticketId}/start
PATCH /api/v1/management/maintenance-tickets/{ticketId}/resolve
PATCH /api/v1/management/maintenance-tickets/{ticketId}/close
PATCH /api/v1/management/maintenance-tickets/{ticketId}/cancel
```

## Route Boundary Summaries

These summaries describe client-visible scope, authorization, and request/response behavior. Detailed lifecycle and domain invariants remain owned by the linked flow and architecture documents.

### Maintenance Assignment

Maintenance assignment accepts only an active `Technician`, `Manager`, or
`OrgAdmin` whose single role-scope assignment matches the ticket kiosk, store,
or organization. Cross-tenant role and scope composition is rejected. Push-token
registration is not an assignment prerequisite.

### Device Catalog And Lifecycle

- Device and execution-endpoint item operations are kiosk-owned routes: `/api/v1/management/kiosks/{kioskId}/devices/{deviceId}/...` and `/api/v1/management/kiosks/{kioskId}/execution-endpoints/{endpointId}/...`. Handlers must reject mismatched route kiosk and item ownership with `404`.
- `DELETE /api/v1/management/kiosks/{kioskId}/devices/{deviceId}` is a soft retire operation. It sets `DeviceStatus.Retired` and soft-deletes the row; it does not physically delete the device record.
- Device retirement is atomic with Inventory topology retirement and is blocked while the kiosk has an Accepted or Running execution. Active dispenser states are retired with the supplied `reason` query value or the system reason `DEVICE_RETIRED`; estimates remain historical and are not silently discarded.
- `POST /api/v1/management/kiosks/{kioskId}/devices/{deviceId}/replace` requires both `devices.manage` and `inventory.configure` and accepts an already-provisioned replacement Device in the same kiosk. It preserves every active container/ingredient/configuration mapping, transfers positive estimates with balanced stock movements, writes rebind audit records, then retires the source Device in one transaction.
- `PATCH /api/v1/management/kiosks/{kioskId}/devices/{deviceId}/status` must not set `Retired`; use the retire endpoint instead.
- Device lifecycle is `Provisioning -> Online|Offline|Maintenance|Error|Disabled`; operational states may move between each other or to `Disabled`; `Disabled -> Provisioning` is the explicit re-enable path; `Retired` is terminal and is reached only through device retirement.
- A provider-confirmed payment received after local payment expiry or customer cancellation remains authoritative. A pending order becomes execution-ready; an already-cancelled order becomes `RefundRequired` for staff handling and is never dispatched automatically.
- If more than one provider session for the same Order is confirmed paid, every transaction retains provider truth as `Paid`, but exactly one transaction is the primary settlement. Later paid occurrences become `DuplicateRefundRequired`, appear in the payment-intervention queue, and move the Order to `RefundRequired` without changing settlement `PaidAmount` or `PaidAt`; fulfillment is not dispatched for the duplicate event.
- `Device.Status` is a management/operations state for configured hardware. Runtime connectivity and error evidence still come from heartbeat and device-event telemetry.
- Device types and models are a global technical catalog, not tenant-owned records. Authenticated device-management users may read the catalog; only `SystemAdmin` may author it.
- Device catalog routes are `GET/POST /management/device-types`, `GET/PUT /management/device-types/{id}`, `PATCH /management/device-types/{id}/status`, `GET/POST /management/device-types/{id}/models`, and `GET/PUT/DELETE /management/device-models/{id}`.
- Device type codes and device model codes are immutable after creation. A model code is unique within its type. Model delete is a soft retire operation so installed devices retain historical identity.
- New or updated devices may reference only an active DeviceType and a non-retired DeviceModel belonging to that type. Deactivation/retirement prevents future assignments but does not rewrite existing devices.
- Device model capabilities use a typed string list at the API boundary. JSON and capability schema version remain persistence details and are not supplied by FE.
- A capability required by active dispenser topology cannot be removed from its DeviceModel. A DeviceModel cannot be retired while assigned to a non-retired Device.

### Cross-Cutting Management Rules

- Use `/management/...`, not `/admin/...`.
- Access is controlled by authorization policies, not the route prefix.
- Tenant authorization must match role and resource scope on the same `UserRoleScope`; combining a privileged role from one scope with an unrelated scope from another assignment is forbidden.
- It is valid for multiple roles to share the same management endpoint when policy allows it.
- Management APIs can expose configuration/admin fields that tablet APIs should not expose.
- Organization update uses scoped authorization: `SystemAdmin` can update platform-managed fields; `OrgAdmin` can update only basic profile/contact fields for assigned organization scope.
- Product and menu ownership comes from the organization route, never from a body-supplied `OrganizationId`. Generic updates cannot move `OrganizationId`, `ScopeType`, `StoreId`, `KioskId`, or template lineage. Global product templates are managed separately by `SystemAdmin`; `POST .../products/from-template` copies template metadata, variants, options, and the latest Published/Active recipe definitions into a new organization-owned Draft configuration while recording template lineage.
- GraphQL `tenantTree` is a scope/navigation read model, not a dashboard overview. Do not add revenue, alert, inventory, or runtime metrics to it.

### Orders, Fulfillment, And Payments

- Back-office order operations are manual support workflows. Paid orders should be marked `RefundRequired`; they are not cancelled directly.
- Manual lines use `manual-fulfillment-events` for the strict `Pending -> Accepted -> Preparing -> Completed` lifecycle, or `Failed` from a non-terminal state. The request requires a client-generated `fulfillmentEventId`; reusing it with a different payload returns `409`.
- Packaged lines use idempotent `fulfill` and `fail` commands with a client-generated `fulfillmentEventId`. Staff moves them directly from `Pending` to `Completed` when handing the ready-made item to the customer, or from `Pending` to `Failed` with a required reason when fulfillment is impossible. They never enter `Accepted` or `Preparing`.
- Machine-produced lines reject both management flows and advance only from authenticated Edge production reports. Order status is aggregated from all immutable line fulfillment types; completing one production job cannot complete a mixed order while other lines remain incomplete.
- One failed item moves a paid order to `FulfillmentIssue` and requires staff review; it does not automatically refund the whole order and does not prevent remaining non-terminal items from being fulfilled.
- A paid machine order that cannot create its initial Edge command remains retryable only until `OrderExecutionDispatch__InitialDispatchSupportEscalationMinutes`. After that SLA it becomes `FulfillmentIssue`, records order history, and publishes `SupportRequired`; this does not claim that physical execution failed.
- Manual and packaged fulfillment remain line-atomic. Machine-produced lines retain unit/range outcomes; their business line completes only when every expected unit's effective outcome is `Completed`. A failed unit moves the paid order to `FulfillmentIssue` without erasing successful unit or stock evidence.
- `fulfillmentQueue` returns tenant-scoped manual and packaged work; `orderItemStatusHistory` returns the item-level audit trail. Both are GraphQL management reads protected by `orders.view`.
- Packaged variants may expose only `CommercialOnly` options. Physical packaged choices belong in separate product variants. Manual variants may use production-affecting options as staff instructions; machine-produced variants require active-route support for each production-affecting option.
- `FulfillmentType` is management/backend context and is not returned in the customer order-item response.
- Order status history is a back-office audit read model. It exposes order status transitions and a small actor snapshot (`changedByAccountId`, `changedByName`, `changedByEmail`), not full account objects, raw payment callback bodies, or robot telemetry.
- Execution-attempt reads use durable `ExecuteOrder` commands as the list authority, so pending or rejected attempts remain visible before an execution projection exists. Detail combines the optional order-summary projection with job/unit `ProductionExecutionRecord` rows, completed/failed/in-progress/unreported unit counts, ordered delivery-attempt history, timeout provenance, redispatch actor/reason, and previous/next dispatch references. It excludes command payload JSON, raw sync events, and stock payloads. Both routes use `orders.view` and enforce scope through the owning Order.
- The per-order execution-attempt list is paging-only and has no status, endpoint, or time filters. Dispatch attempts are bounded by `OrderExecutionDispatch__MaxDispatchAttempts` (default `3`).
- Accepted commands create a provisional order-execution projection with sequence `0`. Management reads may show it before the first Edge order-summary report. Timeout reconciliation changes only observation/customer projection to `Stale/Delayed`, `Unreachable/PendingRecovery`, or prolonged `Unreachable/SupportRequired`; it must not infer `OrderStatus.Failed` from silence. Customer order/payment polling reads the latest dispatch attempt projection.
- `POST /management/orders/{orderId}/execution-attempts` is the explicit operator redispatch command. Backend allocates `latest DispatchAttemptNo + 1` under the order advisory lock; clients do not choose attempt numbers. It requires `orders.manage`, an authenticated account, and a reason of at most 500 characters.
- GraphQL `orderExecutionAttempts` exposes the normal operational summary. Full command provenance, delivery attempts, executor sequence data, and production evidence are restricted to `GET /management/orders/{orderId}/execution-attempts/{sourceCommandId}/diagnostics` with `operations.diagnostics`; the attempt must belong to the route order.
- `POST /management/orders/{orderId}/items/{orderItemId}/production-remakes` creates an idempotent remake command for an exact failed unit range. `remakeRequestId` is client-generated. The normal endpoint permits it only for a paid `FulfillmentIssue`, complete terminal source evidence, and units whose latest outcome is `Failed` with `physicalOutputMayHaveOccurred=false`. It never replays the whole order.
- Failed or manual-intervention production job evidence creates one Orders-owned production incident in the same ingestion transaction. Unknown or possible physical output remains `AwaitingInspection`; confirmed no-output evidence records `NotProduced` without claiming that output exists.
- `GET /management/production-incidents` is the tenant-scoped operations work queue. Incident detail and mutations are order-owned routes. Manual defect reporting must reference an existing execution job and exact production-unit range; it cannot invent production provenance.
- Inspection is required before selecting a resolution. Supported V1 resolutions are deliver existing output, discard, exact-unit remake, full-order refund, full-order voucher, technical review, or no action. A defective-output remake is allowed only through the matching incident whose inspection is `Defective` and whose selected resolution is `RequestRemake`; this exception does not weaken the normal remake endpoint.
- Resolution selection is idempotent by `resolutionRequestId` plus a stored fingerprint of the normalized resolution payload. Reusing an id with changed resolution, payment target, voucher data, reason, or acknowledgement returns `409`. Remake stores the resulting Edge command id; refund/voucher stores the Payments-owned refund id. Completing an incident is an explicit staff audit action and does not rewrite immutable execution or stock-consumption evidence.
- Refund/voucher incident resolution requires explicit `acknowledgeFullOrderCompensation=true` because V1 has no partial-refund contract. It additionally requires the existing `refunds.manage` scope enforced by the Payments handler. Production incidents never trigger automatic refunds.
- Redispatch is allowed only when the latest execute-order command is `DeliveryFailed`, or `Rejected` while the Order is `ExecutionRejected` (rejection before physical output). `RefundRequired`, `Failed`, active attempts, and possible physical-output cases are not redispatched automatically.
- `OrderExecutionDispatch__MaxDispatchAttempts` limits attempts. The new command stores `CreatedByAccountId`; `OrderStatusHistory` stores actor, attempt number, and reason. Repeating the request by the same operator while that new attempt is active returns the existing attempt rather than allocating another.
- Refund APIs in v1 track manual staff-handled compensation only. Supported methods are `FullMoneyRefund` and `Voucher`. Normal refund-required orders use full-order compensation; duplicate-payment intervention refunds the selected duplicate occurrence while preserving the primary settlement.
- Full money refund of the primary settlement sets `PaymentStatus = Refunded` only when staff confirms the money was actually refunded. Resolving a duplicate occurrence keeps the Order payment `Paid`, marks only that duplicate transaction resolved/refunded, and restores the pre-intervention Order status after all duplicate occurrences are resolved. Voucher compensation does not reverse payment status.
- Rejecting or cancelling a refund keeps `OrderStatus = RefundRequired`; staff may create another refund/compensation record later.
- `POST /api/v1/management/orders/{orderId}/refunds` should use `Idempotency-Key` for safe manual retries. `paymentTransactionId` is optional when there is one unambiguous refund target; it is required when multiple duplicate payments await resolution. The selected transaction must be paid and belong to the route Order.
- Payment-session creation selects `paymentMethodCode` and submits the amount/currency currently displayed by the client. Backend remains authoritative from the stored Order and returns `409` without creating a provider session when the values differ.
- Full-money refund completion requires staff to explicitly submit `moneyWasRefunded`; omission must not be interpreted as a successful money reversal.
- `GET /api/v1/management/orders/{orderId}/payment-diagnostics` is an order-owned diagnostics read protected by `operations.diagnostics`. It exposes provider identity, reconciliation attempts, bounded failure details, and stored provider request/response evidence; normal tablet and order-management responses do not expose those fields.
- Payment-session creation persists a deterministic provider order code before the provider `POST`. Recovery queries that identity instead of repeating the create request. A provider lookup may restore checkout instructions, but only a verified provider webhook may commit `Paid` and trigger fulfillment.
- Provider webhook idempotency is exact: reusing `ProviderEventId` requires the same provider payment identity and raw verified payload. A different identity or payload returns `409`. Verified callbacks rejected by business validation are retained as ignored evidence and do not mutate payment or Order state.
- `GET /api/v1/management/payment-session-interventions` is a tenant-filtered `payments.manage` work queue. It returns bounded payment/order identity, issue code, retry state, and eligibility; it excludes raw provider payloads. `DUPLICATE_PAYMENT_REFUND_REQUIRED` entries are manual refund/compensation work and are not provider-reconciliation candidates.
- `POST /api/v1/management/orders/{orderId}/payment-transactions/{paymentTransactionId}/reconcile` performs one provider lookup for an eligible incomplete session. Eligibility is shared with the intervention queue: a pending provider session has missing checkout instructions or has reached its local expiry, even when an old URL/QR payload remains stored. The command requires `payments.manage`, a reason, exact Order ownership, and writes request/result operation-log audit records. It never repeats the provider create `POST` and never treats lookup-only `PAID` as fulfillment authority.
- Entering payment-session manual intervention enqueues a durable `payment_intervention` push for scoped Staff/Manager recipients, with organization OrgAdmin fallback. Ordinary scheduled retries, restored sessions, explicit cancellation/expiry, and known missing provider sessions do not notify. Repeating the same payment transaction and intervention code is idempotent per recipient.

### Catalog And Sales Catalog

- Menu and menu-item creation always starts in `Draft`; lifecycle changes use the dedicated status commands. Menu-item currency is inherited from its parent menu, and product-variant currency is inherited from its parent product.
- Menu currency can change only while the menu has no items. MenuItem currency is inherited at creation, and historical orders keep their sale-time snapshots.
- A Product or ProductVariant referenced by a non-deleted MenuItem cannot be deleted, and referenced Product currency cannot change, until those references are archived or replaced. MenuItem activation performs static Product, Variant, Recipe, ingredient, currency, and option-satisfiability validation before entering `Active`.
- `UpdateMenuItemRequest.ClearRecipe = true` explicitly removes an optional Recipe binding; it cannot be combined with `RecipeId`. Omission preserves the current binding.
- Runtime-menu responses expose a deterministic content `Revision` as `ETag`. `SnapshotId` remains a per-request identity; clients may use `If-None-Match` and receive `304` while sellable content is unchanged.
- Normal management contracts do not expose generic `MetadataJson` fields for organizations, products, variants, menus, or menu items. Add typed request/read-model fields when a concrete UI use case exists.
- Product and variant creation always starts unavailable; availability changes use the dedicated commands.
- ProductCategory is a global flat reference catalog in V1. `product-categories.read` provides the flat lookup for selecting `CategoryId` during product authoring. `product-categories.manage` creates, updates metadata, activates/deactivates, and deletes only unreferenced categories. The domain and database model do not contain parent/child hierarchy.
- Product options are authored as `Product -> OptionGroup -> ProductOption` and inherit Product tenant scope and currency. Group status and option availability use dedicated endpoints; metadata updates cannot change lifecycle state. Product cloning creates new groups/options. A MenuItem exposes only its configured subset through `productOptionIds`. Runtime menu returns typed active groups and selectable options. Checkout submits unique `selectedOptions[].productOptionId` values; backend validates every active group definition, including required groups with no configured MenuItem membership, plus cardinality, option/ingredient lifecycle, menu membership, and price deltas before storing immutable `OrderItemOption` snapshots. Raw option JSON from clients is not accepted or forwarded to Edge.
- Deleting a ProductOption or OptionGroup is rejected while any MenuItem membership still references it. Setting an option or one of its required ingredients inactive keeps authoring membership but removes the option from runtime-menu output; if an active required group no longer has enough selectable choices, the MenuItem is not sellable. A MenuItem whose attached Recipe references an inactive Ingredient is also not sellable. Catalog edits never rewrite placed-order recipe or option snapshots.
- Cloning a Product creates new OptionGroup and ProductOption identities. Cloned options retain `TemplateProductOptionId` lineage, start unavailable, and can be selected only by MenuItems whose Product is that clone.
- Ingredients are a global reference catalog in V1. `ingredients.read` provides paged lookup with optional active-status filtering. `ingredients.manage` creates, updates, and changes active status. Inactive ingredients cannot be added to Draft recipes. Delete is allowed only while no RecipeItem, dispenser state, or stock movement references the ingredient.
- Recipes are authored under their owning ProductVariant. Organization/store/kiosk scope is inherited from Product and is never accepted from the request body. Recipe code is immutable within a version family; backend allocates the next version number for each variant/code.
- Recipe metadata and ingredient membership can be changed only while status is `Draft`. `PUT .../items` atomically replaces ingredient requirements. `RecipeItem.DisplayOrder` is declaration order, not robot execution order.
- Product options declare required typed `executionImpact`: `CommercialOnly` changes price/customer choice without changing machine execution; `ProductionAffecting` participates in ingredient/artifact composition. Create/update requests must send the field explicitly. Commercial-only options cannot have ingredient execution requirements. `PUT .../ingredient-requirements` accepts non-empty requirements only for production-affecting options. Every requirement uses the catalog ingredient unit and declares its required workcell capability. Each selected production-affecting option snapshots those requirements into the order; dispatch requires an active, online kiosk dispenser and an available matching capability on the chosen endpoint. Estimated quantity remains outside this gate.
- New order recipe snapshots use schema version `2` and include immutable base-recipe ingredient declarations. Existing version `1` snapshots remain historical records.
- Recipe lifecycle is `Draft -> Published -> Active -> Retired`. Publishing requires at least one non-optional ingredient. Published/Active recipe content is immutable; historical Order recipe snapshots are never rewritten.
- `POST .../recipes/{recipeId}/versions` copies a non-Draft recipe and its ingredient requirements into the next backend-allocated version as Draft. The new version is not default automatically. Version allocation is serialized per ProductVariant; concurrent default changes return `409` and the database enforces one non-retired default recipe per ProductVariant.
- Product-template cloning copies the latest Published/Active recipe version for each variant/code into the organization product as a new Draft recipe. It creates new recipe/item identities and retains `TemplateRecipeId` lineage.

### Request And Response Boundaries

- Organization-owned Product, Menu, and cloned Product create contracts do not accept `ScopeType`. Backend derives it from the most-specific supplied scope id: Kiosk, Store, then Organization.
- Organization-owned RobotProgram create contracts also do not accept `ScopeType`; RobotProgram additionally supports Device scope, so backend derives its scope from Device, Kiosk, Store, then Organization.
- Execution endpoint authentication mode is derived from the selected profile: `FullEdge -> MutualTls`, `LowCostController -> SignedCommandTls`.
- Normal device and kiosk management contracts do not expose raw `MetadataJson` or `SettingsJson`. Store opening hours use a typed per-day schedule while persistence continues to serialize schema-versioned JSON internally.
- Store opening hours and the explicit sales-pause lifecycle are online-sale admission gates for both Cloud runtime-menu reads and order placement. An empty schedule means unrestricted hours; a configured schedule treats omitted/closed days as closed and evaluates `[OpensAt, ClosesAt)` in `Store.TimeZone`. `OpensAt > ClosesAt` is an overnight interval: it stays open through midnight until the following day's close time. Closed or manually paused Stores return `409` for new sales admission.
- Sales pause is distinct from disabling a Store. `PATCH /management/organizations/{organizationId}/stores/{storeId}/sales-pause` requires a reason and accepts an optional future `resumeAt`; `PATCH .../sales-resume` resumes immediately. A timed pause stops blocking automatically at `resumeAt`. Neither scheduled close nor sales pause cancels paid, queued, accepted, or running fulfillment.
- Order placement snapshots `paymentDeadlineAt` from `Payments:OrderPaymentWindow:DurationMinutes`. A payment session may be created after the Store closes or pauses only for an already placed Order whose payment deadline is still open. New sessions are rejected after that deadline, provider expiry is capped by it, and customer projections no longer offer payment retry. A later verified provider `Paid` webhook remains financial authority and is not discarded because the local deadline passed.
- Configuration-release route authoring accepts `RecipeId` and derives `ProductVariantId` from the recipe before storing both route identities.
- Setting an internal-account password changes credential material only. Enabling local login remains a separate account-policy update.
- Authentication responses contain tokens, minimal identity, role scopes, and enabled login methods. Full profile fields belong to `/me`.
- Kiosk order creation derives `OrderChannel = Tablet` from the endpoint contract. Anonymous clients cannot choose an analytics/audit channel value.
- Deployment command identifiers are internal transport coordination data. Management responses expose deployment identity and status, not `EdgeCommandId`.

### Inventory

- Inventory owns Cloud dispenser topology in V1. Create binds an immutable `Kiosk + Device + Ingredient + ContainerCode` identity; update changes only capacity, unit, and the typed level-to-quantity profile. Unit cannot change after an estimate or stock history exists. Ingredient/device rebinding requires retiring the old state and creating a new one.
- Dispenser topology is authored directly through Inventory management APIs, not materialized by Configuration Release. `inventory.configure` excludes Staff and owns create/update/status/delete; `inventory.manage` continues to own refill and estimate adjustment.
- `GET /api/v1/management/kiosks/{kioskId}/inventory/topology` returns the kiosk Device -> containers -> Ingredient configuration, including devices with no configured containers. `CanHostDispenser` distinguishes valid unconfigured dispenser hardware from unrelated devices.
- The topology read model retains referenced retired devices and reports `DeviceInactive`, `DeviceUnavailable`, `ContainerInactive`, and `IngredientInactive` warnings instead of silently hiding stale topology references.
- Creating, updating, reactivating, or rebinding a dispenser requires a DeviceModel with `IngredientDispenser`. Devices without a model or with unrelated capabilities cannot own dispenser topology. Categorical level mapping does not require a sensor capability.
- Dispenser topology item operations are kiosk-owned routes: `/api/v1/management/kiosks/{kioskId}/inventory/dispenser-states/{id}/...`. Handlers must reject mismatched `{kioskId}` and dispenser state ownership with `404`.
- `POST /api/v1/management/kiosks/{kioskId}/inventory/dispenser-states/{id}/rebind` never mutates topology identity in place. It retires the source, creates a replacement, records an immutable rebind audit row, and commits estimate movements atomically. Rebind is rejected while the kiosk has an Accepted or Running execution.
- A positive source estimate requires explicit `Discard` or `Transfer`. Transfer is allowed only for the same Ingredient and Unit and records balanced transfer-out/transfer-in movements. Otherwise FE must choose Discard; no estimate is copied or erased silently.
- `GET /api/v1/management/kiosks/{kioskId}/inventory/dispenser-states/{id}/rebind-history` exposes the source/replacement identities, actor, reason, estimate disposition, and quantities without returning raw audit payload JSON.
- `GET /api/v1/management/kiosks/{kioskId}/inventory/dispenser-states/{id}/history` is the paged operational timeline for refill, adjustment, consumption, topology lifecycle, and rebind events. It returns account or execution-endpoint actor identity, reason, quantity delta, exact before/after balance when recorded, and topology before/after state.
- New StockMovement rows persist nullable `BalanceBefore` in addition to `BalanceAfter`; historical rows created before this contract may legitimately return a null before-value.
- Production stock evidence always applies `QuantityConsumed` to the current Cloud estimate and records one matching `CONSUME` movement. `BalanceAfter` is optional. When Cloud already has an estimate, a supplied value must equal `BalanceBefore - QuantityConsumed`; a mismatch rejects the entire execution report. When the estimate is unknown, a valid supplied value establishes the post-consumption estimate; if it is omitted, the estimate and both movement balances remain unknown.
- `StockMovement.SourceEventId` is an immutable evidence identity. A retry with the same dispenser, consumed quantity, order, executor, estimate flag, and supplied post-balance is a no-op; reusing it with different evidence rejects the report.
- `InventoryChanged.EstimatedQuantity` is nullable and preserves an unknown estimate; realtime notifications must not project unknown quantity as zero.
- Refill, estimate adjustment, topology update/status/delete/rebind, device replacement, and execution consumption serialize on the same dispenser-state mutation identity. A mutation must acquire that transaction-scoped lock before loading the mutable state; multi-dispenser reports acquire locks in deterministic dispenser-id order.
- Topology configuration update and status requests require an operator reason. Automatic lifecycle changes use explicit system reason codes rather than an empty audit reason.
- Level-to-quantity mapping supports Edge-reported `Low`, `Medium`, and `Full` only. A non-empty mapping must define all three levels exactly once and quantities must increase strictly in that order. Numeric sensor calibration is not supported in this phase.
- A dispenser state with stock movement history cannot be deleted and must be retired. Retired states reject sensor updates, refill, estimate adjustment, and execution stock evidence. Reactivation requires a non-retired device and active ingredient.
- Inventory estimates still do not decide runtime menu sellability or robot execution availability in V1.
- Reaching a zero estimate updates inventory state and history but does not change V1 topology readiness. Stock thresholds, reservation, and sellability require a separate inventory-availability policy.
- Inventory readiness compares each applicable release Recipe's required ingredients with the target kiosk topology. It returns `Ready`, `MissingIngredient`, `ContainerInactive`, `DeviceUnavailable`, or `CalibrationMissing` per route/ingredient. `CalibrationMissing` means the categorical `Low/Medium/Full` quantity mapping is absent; it does not imply raw sensor calibration support.
- `GET /api/v1/management/kiosks/{kioskId}/configuration-releases/{releaseId}/inventory-readiness` is the operational read model. It is computed from current topology and Recipe requirements; it is not persisted into ConfigurationRelease.
- Release publication and deployment only consume readiness. They never create, reactivate, rebind, or otherwise repair Inventory topology.
- `ProductionInventoryReadiness.PublishPolicy` defaults to `Warn`: publishing succeeds and returns not-ready kiosk details because a reusable release may be published before every kiosk is provisioned. `DeployPolicy` defaults to `Block`: target deployment returns `409` before creating a deployment or EdgeCommand when readiness is not `Ready`.
- Readiness is an operational setup/deployment gate only. Runtime-menu visibility, order creation, inventory reservation, and execution sellability remain unchanged in this phase.

### Operations

- Operations telemetry APIs expose curated heartbeat/event fields only. Do not return raw `PayloadJson` by default.
- Normal telemetry reads also exclude source node ids, heartbeat sequence numbers, and correlation/causation ids. Those ingestion identities remain available to machine contracts.
- Operation-log list/detail reads are kiosk-owned and filter by `deviceId`, `orderId`, `severity`, `from`, and `to`. Their normal response excludes raw payload and sync identities. `GET .../operation-logs/{operationLogId}/diagnostics` exposes only raw payload under `operations.diagnostics`; it is limited to scoped `SystemAdmin` and `Technician` users.
- `DeviceEvent` is immutable log/evidence, not mutable alert state. Newly accepted Error/Critical telemetry creates a separate Open Alert in the same transaction; Warning remains evidence only.
- Alert management uses `/api/v1/management/alerts`: scoped list/get plus acknowledge/resolve. V1 has no general manual create endpoint; alert creation belongs to authenticated telemetry ingestion.
- Maintenance tickets are kiosk-scoped work items with optional evidence links to device, order, device event, or alert. `OperationalImpact` is `None`, `BlocksNewOrders`, or `RequestsEmergencyStop`; starting an impacted ticket atomically moves the kiosk to `Maintenance` or `EmergencyStopRequested`. Resolving or closing a ticket never reopens sales automatically. Configured inventory-empty alert automation may create one linked ticket; chat, reopen, ticket SLA/escalation, and a GraphQL maintenance aggregate remain outside the current contract.

### Robot Configuration, Releases, And Production Packages

- Execution endpoint credential rotation is a maintenance operation. It revokes the current credential binding and activates the new credential reference in one database save. Rotation preserves the endpoint's prior Active or Disabled state; Provisioning and Retired endpoints cannot rotate. Hot credential overlap is not part of V1.
- Robot artifact bulk upload is the only public upload contract. It accepts one to 50 multipart `.lua` files, stores file bytes in S3-compatible object storage, and stores immutable metadata in `RobotArtifact`.
- Bulk robot artifact upload accepts up to 50 files plus a JSON manifest that supplies per-file metadata. Uploaded artifacts remain unassigned Draft inventory and do not change any robot-program sequence. Request-shape errors reject the whole request; upload failures use item-level atomicity and return per-item results without rolling back successful items.
- Robot authoring import is the simplified custom-authoring surface above advanced artifact/program CRUD. Upload accepts one Fairino ZIP plus target scope and requires `Idempotency-Key`; normal FE never sends artifact IDs, contract IDs, storage keys, checksums, or membership IDs. Validation is read-only for authoring resources. Materialization requires both `artifact.upload` and `program.manage`, creates Draft resources only, preserves manifest `RunOrder`, and is serialized by import identity. `POST .../{importId}/publish-resources` is a separate explicit, resumable confirmation that publishes contracts, assigns them, publishes artifacts, then publishes the program. Release attachment and deployment remain separate operations.
- The robot-authoring workspace is a read model, not a write owner. It combines current import progress, existing package ownership, release status, deployment candidates, blockers, and next actions. Package ownership is informational: the workspace does not automatically require a fork. A separate explicit customization workflow decides whether a package-managed resource must be forked before mutation.
- Artifact upload retry identity is organization + normalized artifact code + SHA-256. An exact retry returns the existing artifact id and metadata as success; bulk results expose `uploadedCount`, `existingCount`, and per-item `wasExisting`.
- Artifact review URLs are organization-scoped, short-lived, and generated only after confirming the private object exists. They are not durable download contracts and must not be stored by clients.
- Draft discard hard-deletes metadata only when no `RobotProgramArtifact` references exist. Metadata deletion commits before best-effort object deletion; the orphan cleanup job removes residual objects after its grace period.
- Bulk artifact publish atomically transitions 1-100 unique selected Draft artifacts from one organization. Published items are idempotent no-ops; duplicate ids, missing, cross-organization, Disabled, or Retired selections reject the complete publish request.
- Robot artifact publish makes an uploaded artifact available for programs. Robot program and configuration release publication create immutable definitions from their authored children.
- `RobotArtifactTemplate` is a global authoring source, not a runtime artifact. Only a Published template may be cloned; cloning creates a separate organization-owned Draft `RobotArtifact`, copies immutable bytes to an organization object key, and records `SourceRobotArtifactTemplateId` for lineage. Programs, releases, deployments, and execution endpoints never reference templates directly. An unreferenced Draft template may be hard-discarded; Published and Retired templates remain history.
- Retire lifecycle commands are idempotent and do not hard-delete artifact bytes, manifests, or deployment history. Artifact retirement is blocked by Draft programs, program retirement by Draft releases, and release retirement by Pending/Installed deployments. Published history can retain retired references for audit and rollback.
- Robot programs are created as organization-owned drafts. Store, kiosk, and device scope may narrow that ownership, but all scope ids must belong to the same tenant hierarchy. Global robot-program creation is not exposed because `RobotArtifact` is organization-owned.
- `PUT /management/organizations/{organizationId}/robot-programs/{programId}/artifacts` replaces the complete ordered membership while the program is Draft. `RunOrder` is explicit API data; backend must not derive execution order from an exported filename prefix.
- Every assigned artifact must belong to the program organization. Artifact parameters must be valid JSON. Publishing still requires all assigned artifacts to be Published.
- Artifact and program list endpoints are paged and tenant-scoped. Program lists return summaries without manifest JSON or artifact collections. Program detail includes ordered artifact metadata so management clients can edit/reorder a draft without issuing one request per artifact.
- `PUT /management/organizations/{organizationId}/robot-programs/{programId}` edits draft code, name, and description only. Program scope is immutable after creation; changing ownership scope requires a new draft program.
- `RobotProgramArtifact` is aggregate membership, not an independent management resource. Clients replace the ordered collection through the program endpoint instead of creating or deleting membership rows individually.
- Configuration releases are created as organization-owned drafts with backend-assigned release numbers. Route authoring replaces the complete Draft route/binding collection; `ExecutionRoute` and `ExecutionRouteRobotBinding` are aggregate children, not independent CRUD resources.
- Draft robot programs and Draft configuration releases can be hard-discarded through their `DELETE` endpoints. Published or referenced records are preserved; retirement remains the lifecycle operation for published history.
- `GET /management/organizations/{organizationId}/configuration-releases/authoring-options` is a tenant-scoped UI lookup read model owned by configuration-release authoring. It returns eligible machine-produced ProductVariant options, Published/Active Recipes, and organization-owned Published RobotPrograms with scope and display metadata. `productVariantId`, `search`, and `limit` are optional; `limit` applies independently to each result group. The command handler still revalidates every selected id when routes are submitted.
- Release route authoring requires Published/Active recipes to belong to their product variants and bindings to reference Published robot programs owned by the release organization. Kiosk/device compatibility remains a deployment-time validation.
- Release route authoring also requires an explicit `supportedOptionCodes` collection. Codes must identify unique production-affecting options of the route Product. The immutable route, production-definition checksum, release manifest, runtime-menu filtering, order validation, and dispatch all enforce the same policy.
- Release lists return summaries without manifest JSON or route/binding collections. Release detail remains the review surface for the complete authored graph.
- `GET /management/configuration-deployments` is a read-only global management index for scoped deployment search. Deployment detail and rollback are kiosk-owned routes under `/management/kiosks/{kioskId}/configuration-deployments/...` because deployment affects a physical kiosk execution endpoint.
- Configuration deployment reads unify Full Edge and Low-cost histories behind tenant-scoped, paged management surfaces. Filters include organization, store, kiosk, release, profile, and status for the global index; kiosk-owned reads bind `kioskId` from the route. Profile-specific provenance remains nullable rather than being discarded.
- Deployment preview is kiosk-owned and read-only. Full Edge preview always includes the complete release and rejects route/program selections because the Full Edge command installs the whole release. Low-cost preview may accept route/program selections; otherwise it derives the only binding for each route and reports `ProgramSelectionRequired` for ambiguous routes. It evaluates active endpoint identity, readiness, safety/activity, reported capabilities, robot target compatibility, inventory policy, low-cost capacity, immutable artifact totals, installation modes, risk acknowledgement, and a deterministic preview checksum. It never creates a deployment, Edge command, presigned URL, or Full Edge bundle.
- Configuration rollback selects a previously Active deployment, then creates a new profile-matching deployment and durable command. It does not mutate or reactivate the historical deployment row. Retired releases are eligible only through this validated rollback path.
- The complete Fairino export-to-deployment sequence is owned by [Robot Lua Artifact Flow](../flows/ROBOT_LUA_ARTIFACT_FLOW.md); keep this document focused on route and client boundaries.
- Production package installation is the default simplified franchise contract. It materializes organization-owned Catalog, artifact, program, route, and Draft release resources without accepting technical IDs or ordering fields from normal FE. Existing artifact/program/release authoring endpoints remain an advanced technical surface. See [Production Package Installation Flow](../flows/PRODUCTION_PACKAGE_INSTALLATION_FLOW.md).
- Option-specific robot artifacts use `RequiredOptionCode` in program/release/Edge manifests. The Edge runtime skips the artifact when the order line does not select that option; this is file selection, not parameter injection into Lua.
- Production package V1 requires option codes to be unique within each packaged Product and exactly one required capability code per route. Package replace, publish, preview, and install share the same deterministic validation contract.
- Package management includes package update/retirement and a version-definition read. The definition read returns the complete replaceable Products, artifact sources, program slots, and routes so authoring clients do not reconstruct a PUT payload from metadata.
- Organization installation history is available from `GET /management/organizations/{organizationId}/production-package-installations` with status, store, kiosk, and paging filters.
- Each package route identifies `productSourceKey + productVariantSourceKey + recipeSourceKey`, so recipe codes need to be unique only inside one variant. Recipe materialization source keys include the Product Variant code to preserve that scope. `supportedOptionCodes` explicitly lists production-affecting options supported by that route; an empty list means none. Commercial-only options are never listed. Route programs cannot declare option effects outside this policy.
- `GET /management/organizations/{organizationId}/production-package-installations/{installationId}/workspace` is the package-oriented aggregate read model for one FE workspace. It separates `technicalReadiness` from `commercialReadiness` and returns `requiredActions`, `optionalActions`, and `recoveryActions`. Publish, availability, menu assignment, release, and deployment writes continue through their existing command endpoints.
- Workspace actions are typed guidance only. Their structured context carries required parent resource IDs and, for deployment, the compatible endpoint/profile and low-cost route/program selections. Menu context separates assigned variant IDs from currently sellable variant IDs, so an existing inactive assignment produces deterministic activation/review guidance rather than duplicate assignment. Writes still use the owning command APIs. Installation-specific writes are `retry`, which reuses the persisted selection and original idempotency identity; `fork`, which changes technical ownership and copy-on-writes RobotArtifacts still shared with another package-managed installation when referencing programs remain Draft; and `repair`, which restores soft-deleted materialization targets under their original identities. Fork never rewrites a Published program manifest; later customization of published resources uses a new Draft program/release.
- Definition-changing package-managed technical recovery first requires `POST /management/organizations/{organizationId}/production-package-installations/{installationId}/fork`. Those recovery actions remain blocked with `PackageForkRequired` until ownership changes to `OrganizationFork`. In-place soft-delete repair preserves package ownership and does not require a fork; commercial availability and menu operations also do not require a fork.
- Package installation reuses an exact RobotArtifact only when an Installed or Superseded package-managed installation already owns it and object size/checksum validation succeeds. An organization-authored artifact with the same natural identity returns `409`; package installation never converts that artifact into package ownership implicitly.
- Package-managed Product and ProductVariant technical identity and child structure are immutable until fork. Product/Variant code and technical classification, adding Variants/Recipes/OptionGroups/Options, and changing OptionGroup selection requirements are definition changes. Commercial names, descriptions, prices, availability, display order, images, and menu placement remain mutable.
- `POST /management/organizations/{organizationId}/production-package-installations/{installationId}/repair` restores verified soft-deleted targets for an Installed package-managed installation. It never creates a replacement installation or changes materialization target identities. Workspace and repair derive the same expected materialization set from the immutable package version and persisted product selection; Configuration Release evidence must target the installation's exact `DraftConfigurationReleaseId`. The store validates all targets before writing and restores them atomically. Physical deletion, tenant/scope mismatch, unsupported identity, missing materialization evidence, and restore constraint conflicts return `409`; `details.issues` identifies each affected resource for operator/support handling. Repeating repair after success is a successful no-op.
- Production package version upgrade remains nested under its source installation: `.../{installationId}/upgrades/...`. Preview, paged history, and detail use `package.read`; execute, cutover, and abandon use `package.install`; rollback uses `release.rollback`. Execute requires `Idempotency-Key` plus the exact preview checksum. Abandon accepts only `ReadyForReview` or `Failed`, requires an operator reason, preserves source/audit evidence, and is idempotent. Rollback requires an operator reason; detail exposes typed menu evidence, frozen endpoints, current rollback observation, and rollback-attempt audit. FE never submits successor database IDs, staging codes, menu rollback snapshots, endpoint deployment IDs, or field-ownership choices. Backend derives and persists those as typed evidence. Publication and deployment remain separate existing APIs. Cutover requires package-managed source/successor ownership and an exact Active deployment row for every frozen endpoint; the row must match tenant, kiosk, endpoint, profile, and successor release. Fork is blocked while either installation participates in a `Materializing`, `ReadyForReview`, or `RollbackPending` upgrade. Forking the successor after completed cutover invalidates package rollback rather than allowing rollback to overwrite organization-owned changes.
- Workspace blockers carry typed readiness impact (`Technical`, `Commercial`, or `Both`); the two readiness projections are evaluated independently rather than partitioning blocker codes. Required option-group availability is grouped by stable `OptionGroupId`.
- A required option group below `MinSelections` produces one `RestoreRequiredOptionGroupAvailability` action with `requiredCount` and candidate option IDs. Individual `EnableOption` actions remain optional choices.
- Technical workspace readiness means the release is published, compatible with an active endpoint, inventory topology satisfies base Recipe and required-option-group requirements, and the release is Active on the kiosk. `latestDeploymentStatus` is not treated as informational-only readiness evidence.
- Robot artifact technical contracts are typed, versioned publication records. Metadata JSON and Lua file names are not behavior authority. Normal artifact/template responses expose whether a contract reference is assigned, not whether the referenced contract is currently publishable; publish and clone commands perform authoritative status, checksum, scope, and compatibility validation. Parameterized quantity remains unavailable until the Edge runtime contract consumes it.
- Technical-contract lists are paged and accept `status` and `search`. Organization-scoped lists follow normal tenant ownership. For the global catalog, `SystemAdmin` can inspect all lifecycle states while `OrgAdmin` can read only Published contracts; direct global Draft/Retired reads return not found.
- Deployment validation preview returns a checksum and residual-risk warnings. Deploy requests must echo the current checksum and organization acknowledgement; acknowledgement cannot override objective compatibility, integrity, effect, quantity, ordering, capability, or topology failures.
- Publish commands do not deploy to an execution endpoint.
- Full Edge deployment requests create a durable deployment command for the immutable release. Edge validates downloaded content and reports installation and activation separately.
- Low-cost deployment requests select unique route/program pairs. Backend includes every ordered artifact in each selected published program and enforces the configured controller capacity.
- Normal Full Edge and low-cost deployments require a Published configuration release. A Retired release may be deployed only through the validated rollback endpoint; callers cannot use the normal deployment routes to bypass retirement.
- Full Edge deploy, Low-cost deploy, and rollback requests require `Idempotency-Key`. The key is durable and unique per execution endpoint. An exact retry returns the existing deployment and command; reusing a key for a different release or Low-cost selection returns `409`.
- Robot program, configuration release, and deployment read routes use `program.read`, `release.read`, and `deployment.read`. Authoring/publishing/deployment commands retain their narrower mutation policies.
- When a deployment command expires before acceptance, its still-Pending deployment becomes `Failed/CommandExpired`.
- An accepted deployment that receives no installation report before its deadline becomes `Failed/ExecutionReportTimeout`.
- Artifact bytes are not exposed through a public REST download endpoint. After execution-endpoint authentication, command pull enriches deployment artifact descriptors with short-lived object-storage read URLs. These URLs are not durable API identifiers and must not be stored as release state.

## Related Docs

- [API Surface Rules](API_SURFACE_RULES.md)
- [Authorization Rules](AUTHORIZATION_RULES.md)
- [Multi-Tenancy Rules](../architecture/MULTI_TENANCY_RULES.md)
- [System Flows](../flows/SYSTEM_FLOWS.md)
````

## File: docs/data/DATA_MODELING_RULES.md
````markdown
# Data Modeling Rules

This document captures small data-modeling rules that are easy to miss during ERD and EF Core changes. These rules are practical guardrails for tables, indexes, constraints, and persistence behavior.

## Search Keywords

`data modeling`, `EF Core`, `PostgreSQL`, `soft delete`, `filtered unique index`, `partial unique index`, `DeletedAt IS NULL`, `nullable unique`, `tenant scope`, `historical snapshot`, `DeleteBehavior.Restrict`, `enum status`, `decimal money`, `JSONB`, `high-volume logs`, `partitioning`, `retention`, `SyncEventInbox index`, `KioskHeartbeats`, `DeviceEvents`, `EdgeCommandDeliveryAttempts`, `ProductionExecutionRecords`

## Soft Delete And Unique Indexes

If an entity uses `ISoftDeletable`, unique indexes for reusable business identifiers must filter out deleted rows.

Use:

```csharp
.IsUnique().HasFilter("\"DeletedAt\" IS NULL")
```

For nullable unique fields, combine both conditions:

```csharp
.IsUnique().HasFilter("\"SerialNumber\" IS NOT NULL AND \"DeletedAt\" IS NULL")
```

Apply this to reusable identifiers such as:

- `Account.UserName`
- `Account.Email`
- `Organization.Code`
- `Store.Code`
- `Kiosk.Code`
- `Product.Code`
- `ProductVariant.Code`
- `Recipe.Code`
- `Menu.Code`
- `MenuItem.Code`
- `RobotProgram.Code`
- `RobotArtifact.ArtifactCode + Checksum`
- `RobotProgramArtifact.RobotProgramId + RunOrder`
- `ExecutionRoute.ConfigurationReleaseId + RouteCode`
- `Device.Code`
- `Device.SerialNumber`

Do not apply soft-delete filters to immutable evidence or retry keys:

- `IdempotencyKey`
- `EventId`
- `SourceEventId`
- `OrderNumber`
- `TransactionNumber`
- `RefundNumber`
- `JobNumber`
- `TokenHash`
- payment provider callback ids

Reason: those keys protect audit, deduplication, retry, and historical evidence. They should not be reused after deletion.

## Soft Delete Query Visibility

EF global query filters are not universal. Do not apply one to a soft-deleted
principal when a required dependent remains visible as immutable evidence. EF
otherwise warns about the required navigation and can hide historical rows
through an implicit inner join.

The following principals intentionally use explicit query visibility instead of
an EF global filter:

- `Account`
- `Organization`, `Store`, and `Kiosk`
- `Device`
- `Product` and `Ingredient`
- `IngredientDispenserState`
- `Order` and `PaymentTransaction`
- `ConfigurationRelease`
- `KioskExecutionEndpoint`

Normal operational stores must start their query with
`WhereNotDeleted()`. Evidence, deduplication, retention, object-reference
cleanup, and provider callback paths may deliberately read all rows. Such an
unfiltered path must be named or commented by its evidence/operational purpose;
it is not a convenience bypass.

All other `ISoftDeletable` entities continue to use the DbContext global
filter. When adding a required relationship from a non-soft-deleted dependent,
audit the principal's visibility policy before relying on the global filter.

## Nullable Unique Columns

PostgreSQL allows multiple `NULL` values in a unique index. If uniqueness should apply only when the value exists, use an explicit filter:

```csharp
.HasFilter("\"ProviderTransactionId\" IS NOT NULL")
```

If the entity is also soft-deletable and the identifier is reusable, include `DeletedAt IS NULL`.

## Tenant Scope Consistency

Entities with `OrganizationId`, `StoreId`, `KioskId`, and `ScopeType` need validation that the ids match the declared scope.

Examples:

- `ScopeType = Global`: all scope ids should be null.
- `ScopeType = Organization`: `OrganizationId` should exist; `StoreId` and `KioskId` should be null.
- `ScopeType = Store`: `StoreId` should exist.
- `ScopeType = Kiosk`: `KioskId` should exist.

Database foreign keys only prove the referenced row exists. They do not prove the scope combination is meaningful. Enforce this in admin/application use cases or domain methods.

When a persisted row duplicates a parent scope id for filtering, audit, or
historical projection, enforce the pair at the database boundary as well:

- `(ExecutionEndpointId, KioskId)` must reference the same execution endpoint.
- `(DeviceId, KioskId)` must reference a device installed in that kiosk.
- `(ConfigurationReleaseId, OrganizationId)` must reference a release owned by that organization.
- `(KioskId, OrganizationId)` must reference a kiosk owned by that organization.
- execution records that store both `SourceCommandId` and endpoint id must reference the command and its actual target endpoint together.

Use alternate keys plus composite foreign keys for these invariants. Handler
scope checks remain necessary for authorization, but they are not a substitute
for persistence constraints that prevent cross-tenant rows during concurrency,
background processing, sync ingestion, or future code changes.

## Audit Field Automation

Current v1 audit convention:

- `CreatedAt` and `UpdatedAt` may be auto-filled in `IceBotDbContext.SaveChangesAsync`.
- Timestamp automation belongs in `IceBotDbContext.SaveChangesAsync`, not in generic CRUD repository methods.
- Timestamp automation should not overwrite explicit values already set by handlers, repositories, seed/bootstrap code, or tests.
- Do not auto-fill `CreatedByAccountId` or `UpdatedByAccountId` yet.
- Actor fields remain manually assigned where needed by the use case.
- Do not introduce `ICurrentActorContext` until auth, background worker, payment callback, sync, and system actor requirements are clear.
- Do not inject HTTP/JWT concepts directly into `IceBotDbContext`.

Reason: timestamp automation is a low-risk persistence mechanic, while actor attribution depends on workflow ownership and system/background behavior.

## Historical Snapshots

Orders, payments, robot jobs, stock movements, and audit/event tables should not rely only on mutable foreign rows for historical truth.

Use snapshots for values that must remain true after catalog/menu/configuration changes:

- product/product variant/menu display name at order time
- unit price and discount at order time
- recipe/config version used for execution
- provider raw payload evidence
- robot program/step execution parameters

Foreign keys are still useful for traceability, but snapshots protect reporting and audit.

## Delete Behavior

Default delete behavior should stay restrictive.

Use `DeleteBehavior.Restrict` unless a cascade is explicitly part of the aggregate lifecycle. This avoids accidental deletion across large navigation graphs.

Soft delete is preferred for mutable business records. Append-only event/evidence records should not be soft-deleted by default.

## Status Fields

Stable workflow states should use enums. Vendor-specific or externally extensible values may stay as strings.

See [Naming Rules](../process/NAMING_RULES.md) for enum and status naming.

## Money And Quantity

Money and inventory quantities must use `decimal`, not `double` or `float`.

Current EF convention sets:

```csharp
decimal(18, 4)
```

If a field needs a different precision, configure it explicitly.

## JSON Columns

JSON fields are allowed for snapshots, provider payloads, robot parameters, and extension metadata. They should not replace typed workflow fields used for validation, querying, idempotency, retry, or status transitions.

Every source-of-truth JSON configuration should have a matching schema version field.

See [JSON Field Rules](JSON_FIELD_RULES.md).

## High-Volume Log And Event Tables

Append-only log, event, heartbeat, and sync tables must be designed for growth before production.

High-volume tables include:

- `KioskHeartbeats`
- `DeviceEvents`
- `OperationLogs`
- `EdgeCommandDeliveryAttempts`
- `ProductionExecutionRecords`
- `SyncEventInbox`
- `SyncDeadLetters`
- `ProductionEventCheckpoints`
- `EdgeStateSummaries`

Rules:

- Every high-volume table must have a time-based index aligned with its normal query field, such as `ReportedAt`, `OccurredAt`, `ReceivedAt`, or `FailedAt`.
- Kiosk/device scoped logs should include the scope id before the time field in common query indexes, such as `(KioskId, ReportedAt)` or `(DeviceId, OccurredAt)`.
- Background-worker queues must have indexes matching their scan predicate. For example, `SyncEventInbox` needs `(Status, NextRetryAt, LockedUntil)` for retry/lock scans.
- Current retention policy:
  - raw `KioskHeartbeats`: 30 days;
  - raw `DeviceEvents`: 90 days, except an event referenced by any `MaintenanceTicket` is retained;
  - raw `OperationLogs`: 90 days;
  - `SyncEventInbox` with `Processed` or `Ignored` status: 180 days after processing/receipt, only when no `SyncDeadLetter` references it;
  - `SyncEventInbox` in `Received`, `Processing`, `Failed`, or `DeadLettered`: retained until a separate recovery/manual-resolution policy handles it;
  - expired execution-request nonces: removed on the next retention run.
- Retention deletes bounded batches instead of issuing one unbounded table delete. `BatchSize` limits each SQL delete and `MaxBatchesPerRun` limits total work per scheduled run.
- V1 does not require archive or aggregate tables for raw telemetry.
- Define a PostgreSQL partition plan for high-volume append-only tables before production. Monthly range partitions by the main time field are the default starting point.
- Do not rely on EF Core fluent configuration alone for partition lifecycle. PostgreSQL partition creation/maintenance should be handled by raw SQL migrations, DBA scripts, or scheduled database maintenance.

Partition key direction:

| Table | Partition field |
| --- | --- |
| `KioskHeartbeats` | `ReportedAt` |
| `DeviceEvents` | `OccurredAt` |
| `OperationLogs` | `OccurredAt` |
| `EdgeCommandDeliveryAttempts` | `SentAt` |
| `ProductionExecutionRecords` | `LastExecutorReportedAt` |
| `SyncEventInbox` | `ReceivedAt` or `OccurredAt`, depending on worker/query ownership |
| `SyncDeadLetters` | `FailedAt` |

## Index Review Checklist

Before finishing a new entity or relationship, check:

- Does the entity use soft delete?
- Do unique business identifiers need `DeletedAt IS NULL`?
- Are nullable unique fields filtered with `IS NOT NULL`?
- Are idempotency/event/provider keys intentionally reusable or immutable?
- Are tenant scope indexes aligned with query patterns?
- Are common list/detail queries covered by non-unique indexes?
- Do high-volume log/event tables have time indexes, retention rules, and a partition plan?
- Are FK delete behaviors restrictive unless cascade is intentional?
- Are historical values snapshotted when mutable references can change?

## Related Docs

- [Architecture](../../ARCHITECTURE.md)
- [Working Protocol](../process/WORKING_PROTOCOL.md)
- [Boundary Contexts](../architecture/BOUNDARY_CONTEXTS.md)
- [Dependency Rules](../architecture/DEPENDENCY_RULES.md)
- [Naming Rules](../process/NAMING_RULES.md)
- [Idempotency and Retry Rules](IDEMPOTENCY_RETRY_RULES.md)
- [JSON Field Rules](JSON_FIELD_RULES.md)
````

## File: docs/flows/ROBOT_LUA_DEPLOYMENT_AND_ACTIVATION_FLOW.md
````markdown
# Robot Lua Deployment And Activation Flow

This document owns release authoring, execution endpoint provisioning, deployment, artifact download, verification, activation, rollback, and the artifact-specific deployment failure rules.

## Search Keywords

`configuration release`, `execution route`, `deployment preview`, `Full Edge deployment`, `low-cost deployment`, `artifact download`, `activation`, `rollback`, `deployment checksum`

## Step And API Lookup

| Step | Actor | API / operation | Effect |
| --- | --- | --- | --- |
| 12P. Preview deployment | Management UI | `POST /api/v1/management/kiosks/{kioskId}/configuration-deployments/preview` | Resolves active endpoint candidates, readiness/capabilities, target compatibility, inventory policy, immutable artifact totals, low-cost capacity, installation modes, validation acknowledgement, and a deterministic preview checksum. Full Edge always previews the complete release; only Low-cost accepts route/program selections. It creates no deployment or object-storage bundle. More than one eligible endpoint requires explicit selection. The deploy request must echo the selected candidate's checksum as `deploymentPreviewChecksum`; the command rebuilds the preview and rejects stale or blocked input before creating deployment state. |
| 12. Create release draft | Management UI | `POST /api/v1/management/organizations/{organizationId}/configuration-releases` | Creates a Draft release and allocates the next organization release number. Policy: `release.publish`. |
| 12A. Author routes | Management UI | `PUT /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}/routes` | Atomically replaces Draft execution routes and ordered robot-program bindings after validating product, recipe, organization, and published-program references. |
| 12B. Review releases | Management UI | `GET /api/v1/management/organizations/{organizationId}/configuration-releases`, `GET /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}`, and the organization authoring-options lookup | Returns tenant-scoped release summaries/details and authoring lookup data. Policy: `release.read`. |
| 13. Publish release | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}/publish` | Validates routes, bindings, compatibility, and immutable program/artifact snapshots, then publishes a deployment-profile-neutral content manifest. It does not build a Full Edge ZIP. Policy: `release.publish`. |
| 13R. Retire release | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}/retire` | Stops normal new deployments. Active history and validated rollback remain available; Pending/Installed deployments must finish first. |
| 13X. Discard release draft | Management UI | `DELETE /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}` | Hard-deletes only a Draft release and its route/binding children when no deployment references exist. |
| 13A. Create endpoint | Management UI | `POST /api/v1/management/kiosks/{kioskId}/execution-endpoints` | Creates a Full Edge or Low-cost endpoint in `Provisioning`; Full Edge requires mutual TLS authentication mode. Policy: `devices.manage`. |
| 13B. Set robot compatibility | Management UI | `PUT /api/v1/management/kiosks/{kioskId}/execution-endpoints/{endpointId}/supported-robot-targets` | Replaces the complete runtime-target/machine-model/device compatibility set while the endpoint is not Active or Retired. |
| 13C. Provision endpoint | Management UI / provisioning operator | `POST /api/v1/management/kiosks/{kioskId}/execution-endpoints/{endpointId}/provision` | Full Edge pins a client-certificate SHA-256 fingerprint; low-cost stores an ECDSA P-256 public key/fingerprint. The private key never enters Cloud. The operation also assigns profile identity and activates the endpoint. |
| 13D. Operate endpoint | Management UI | `PATCH .../disable`, `PATCH .../reactivate`, `PATCH .../credential`, `PATCH .../retire` | Controls endpoint lifecycle and credential rotation without changing release or artifact history. |
| 14A. Deploy Full Edge | Management UI | `POST /api/v1/management/kiosks/{kioskId}/configuration-deployments/full-edge` | Materializes or reuses the deterministic Full Edge ZIP from the published content manifest, then creates `KioskConfigurationDeployment` and its durable `DeployConfiguration` command. Policy: `release.deploy`. |
| 14B. Deploy low-cost set | Management UI | `POST /api/v1/management/kiosks/{kioskId}/configuration-deployments/low-cost` | Creates a capacity-limited artifact-set deployment and durable command for a low-cost controller. Policy: `release.deploy`. |
| 14C. Monitor deployments | Management UI | `GET /api/v1/management/configuration-deployments`, `GET /api/v1/management/kiosks/{kioskId}/configuration-deployments`, and `GET /api/v1/management/kiosks/{kioskId}/configuration-deployments/{deploymentId}` | Reads one unified, tenant-scoped history across Full Edge and Low-cost profiles with `Pending`, `Installed`, `Active`, or `Failed` state and failure provenance. The global list is a read-only management index; detail reads are kiosk-owned. Policy: `deployment.read`; idempotency keys are not exposed by these read endpoints. |
| 14C1. Inspect deployed artifacts | Management UI / diagnostics | `GET /api/v1/management/kiosks/{kioskId}/configuration-deployments/{deploymentId}/artifacts` | Returns the immutable artifact/run-order materialization for that deployment. Full Edge reads published program manifests; Low-cost reads the stored controller artifact-set snapshot. |
| 14D. Roll back | Management UI | `POST /api/v1/management/kiosks/{kioskId}/configuration-deployments/{deploymentId}/rollback` | Selects a previously Active deployment for that kiosk as the immutable rollback target and creates a new deployment plus command. Policy: `release.rollback`. |
| 15. Pull command | Execution endpoint | `POST /api/v1/iot/execution-endpoints/{endpointId}/commands/pull` | Authenticates the endpoint. Full Edge receives short-lived URLs for both the complete bundle and individual artifacts; Low-cost receives URLs only for its selected artifact set. URLs are generated at pull time and are not durable command state. |
| 16. Download files | Execution endpoint | Direct HTTPS GET to presigned object-storage URL | Full Edge uses individual files for incremental/cache-aware updates or the ZIP for cold install/full recovery. Low-cost downloads selected `.lua` files. No backend file-proxy route is used. |
| 17. Verify files | Execution endpoint | Local operation | Full Edge verifies bundle SHA-256/size, safely extracts it, then verifies every manifest artifact. Low-cost verifies each selected artifact directly. |
| 18. Acknowledge command | Execution endpoint | `POST /api/v1/iot/execution-endpoints/{endpointId}/commands/{commandId}/ack` | Reports transport/dispatch state: `Received`, `Accepted`, `Rejected`, `ExecutorBusy`, or `DeliveryFailed`. `ExecutorBusy` is temporary and permits redelivery. It does not report installation completion. |
| 19. Report deployment | Execution endpoint | `POST /api/v1/iot/execution-endpoints/{endpointId}/commands/{commandId}/reports` | Reports `Installed`, then `Active`, or reports `Failed`. Installed/Active must echo the command's release id/checksum; Low-cost must also echo active-set version/checksum. Cloud rejects mismatched command, deployment, profile, or provenance before changing observed state. Direct `Pending -> Active` is invalid. |

## Download And Activation Contract

- The object-storage bucket is private.
- Full Edge distribution is hybrid: compare local artifact checksums first, download only missing/changed files when practical, and use the bundle for cold install, cache loss, full recovery, or full rollback.
- A published release stores only its profile-neutral content manifest and checksum. The Full Edge ZIP is a deterministic derived transport object created when a Full Edge deployment is requested; Low-cost publication and deployment do not depend on that ZIP.
- Durable command payloads store bundle/artifact identity and checksums; they do not store presigned URLs.
- An authenticated command pull generates fresh `DownloadUrl` and `DownloadUrlExpiresAt` fields.
- `RobotArtifacts:ObjectStorage:DownloadEndpoint` must be reachable from the Edge/controller network. A Docker-only hostname is not sufficient for a remote endpoint.
- URL expiry, download failure, length mismatch, checksum mismatch, or compatibility mismatch must fail deployment.
- Full Edge must reject unsafe archive paths, unexpected entries, decompression limits, bundle checksum mismatch, or any per-artifact checksum/size mismatch. It must not activate a partial or unverified release.
- Pulling an unacknowledged command again may issue fresh URLs; it does not create new artifact identity.
- A durable deployment payload that cannot be parsed is terminally marked `DeliveryFailed` with `InvalidDurablePayload`; it is not returned to the executor and does not block later commands in the same pull.

## Full Edge And Low-Cost Differences

| Concern | Full Edge | Low-cost Controller |
| --- | --- | --- |
| Deployment unit | Complete immutable `ConfigurationRelease` | Explicit capacity-limited active artifact set derived from a release |
| Local storage | Full artifact/configuration cache according to Edge profile | Small active artifact set that survives reboot |
| Manifest | Full release manifest | Selected route/program/artifact items |
| Transport | Hybrid: immutable ZIP plus individual artifact URLs | Individual selected `.lua` files |
| Download verification | Bundle plus every extracted artifact | Every selected artifact |
| Activation report | `KioskConfigurationDeployment` | `ControllerArtifactSetDeployment` |

Both profiles use the same immutable `RobotArtifact` bytes and checksum identity.

## Failure And Retry Rules

- Re-uploading identical normalized artifact code plus checksum in one organization returns the existing artifact as idempotent success.
- Artifact, program, and release retire commands are idempotent. Retirement preserves immutable bytes, manifests, deployment history, and rollback provenance.
- Low-cost capacity limits are backend configuration and are not request fields.
- Deployment and rollback requests require `Idempotency-Key`. Retry with the same endpoint, key, and payload returns the previously created deployment; the key cannot be reused for a different deployment payload.
- Repeating an execution report with the same `SourceEventId` returns the existing result without applying the transition twice.
- Published parent history may retain retired children. Retirement is blocked only by mutable Draft parent references, or by Pending/Installed deployments for a release.
- Artifact name, runtime target, machine model, and description do not redefine an existing identity on retry; backend returns the stored metadata. Use a different artifact code when the same bytes intentionally represent a distinct artifact identity.
- Command pull may return a delivered but unacknowledged deployment again.
- Command acknowledgement is transport state only; deployment completion belongs to execution reports.
- An expired unaccepted deployment command changes its Pending deployment to `Failed/CommandExpired`.
- Command expiry applies only before executor acceptance. After acceptance, a separate report timeout applies.
- A deployment still Pending after its accepted-report timeout becomes `Failed/ExecutionReportTimeout`.
- A deployment still Installed after its activation timeout becomes `Failed/ActivationReportTimeout`.
- A report arriving after `ExecutionReportTimeout` cannot revive that failed deployment attempt. Operators must inspect endpoint state and request a new deployment or rollback, preserving attempt history.
- A failed deployment keeps the previously active configuration/artifact set. It must not delete a known-good active set merely because a new deployment failed.
- Rollback never mutates the selected deployment, release, artifact set, program manifest, or artifact bytes. It creates a new Pending deployment and a new `DeployConfiguration` command.
- A Retired release may be redeployed only through rollback to a deployment that was previously Active; normal deployment still requires Published status.
- Rollback is rejected when the target is not Active, is already the endpoint's observed active deployment, no longer matches the endpoint profile, or another Pending/Installed deployment blocks the endpoint.

## Related Docs

- [Robot Lua Artifact Flow](ROBOT_LUA_ARTIFACT_FLOW.md)
- [Edge Command Contract](../iot/EDGE_COMMAND_CONTRACT.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
- [Deployment Configuration](../operations/DEPLOYMENT_CONFIG.md)
````

## File: docs/operations/API_SMOKE_TESTS.http
````
### API Smoke Tests Collection

@baseUrl = http://localhost:5000/api/v1
@accessToken = YOUR_ACCESS_TOKEN
@sampleGuid = 00000000-0000-0000-0000-000000000000

# ==========================================
# 1. Health and Probes
# ==========================================

### Liveness Probe
GET http://localhost:5000/health
Accept: application/json

### Readiness Probe
GET http://localhost:5000/health/ready
Accept: application/json

### Service Info
GET http://localhost:5000/info
Accept: application/json


# ==========================================
# 2. Authentication
# ==========================================

### Local Username/Password Login
POST {{baseUrl}}/authentication/login
Content-Type: application/json

{
  "email": "admin@icebot.internal",
  "password": "AdminPassword123!"
}

### Refresh Access Token
POST {{baseUrl}}/authentication/refresh-token
Content-Type: application/json

{
  "refreshToken": "YOUR_REFRESH_TOKEN"
}

### Current Account Access Scope Info
GET {{baseUrl}}/me/access
Authorization: Bearer {{accessToken}}
Accept: application/json


# ==========================================
# 3. Management Accounts & Roles
# ==========================================

### List Management Accounts
GET {{baseUrl}}/management/accounts?pageNumber=1&pageSize=10
Authorization: Bearer {{accessToken}}
Accept: application/json

### Get Account Effective Access Scopes
GET {{baseUrl}}/management/accounts/{{sampleGuid}}/effective-access
Authorization: Bearer {{accessToken}}
Accept: application/json

### List System Roles
GET {{baseUrl}}/management/roles
Authorization: Bearer {{accessToken}}
Accept: application/json


# ==========================================
# 4. Tenant Management (Org, Store, Kiosk)
# ==========================================

### List Organizations
GET {{baseUrl}}/management/organizations
Authorization: Bearer {{accessToken}}
Accept: application/json

### Create Store under Organization
POST {{baseUrl}}/management/organizations/{{sampleGuid}}/stores
Authorization: Bearer {{accessToken}}
Content-Type: application/json

{
  "code": "STORE_CODE_1",
  "name": "First IceBot Store",
  "address": "123 Main St",
  "latitude": 10.762622,
  "longitude": 106.660172
}

### List Kiosks
GET {{baseUrl}}/management/kiosks?status=Active
Authorization: Bearer {{accessToken}}
Accept: application/json


# ==========================================
# 5. Product & Menu
# ==========================================

### Get Customer Kiosk Runtime Menu
GET {{baseUrl}}/kiosks/{{sampleGuid}}/runtime-menu
Accept: application/json

### List Management Products
GET {{baseUrl}}/management/products
Authorization: Bearer {{accessToken}}
Accept: application/json


# ==========================================
# 6. Orders, Payments, & Refunds
# ==========================================

### Place Kiosk Order
POST {{baseUrl}}/orders
Idempotency-Key: PlaceOrder_Unique_Idempotency_Key
Content-Type: application/json

{
  "kioskId": "{{sampleGuid}}",
  "items": [
    {
      "menuItemId": "{{sampleGuid}}",
      "quantity": 1
    }
  ],
  "notes": "No sprinkles please"
}

### Create Order Payment Session
POST {{baseUrl}}/orders/{{sampleGuid}}/payment-sessions
Idempotency-Key: PaymentSession_Unique_Idempotency_Key
Content-Type: application/json

{
  "paymentMethodCode": "payos",
  "expectedAmount": 35000,
  "expectedCurrency": "VND"
}

### Request Refund (Management Support Flow)
POST {{baseUrl}}/management/orders/{{sampleGuid}}/refunds
Idempotency-Key: RefundRequest_Unique_Idempotency_Key
Authorization: Bearer {{accessToken}}
Content-Type: application/json

{
  "refundMethod": "FullMoneyRefund",
  "reason": "Machine failed to dispense ice cream",
  "note": "Refund confirmed via operator"
}


# ==========================================
# 7. Inventory
# ==========================================

### List Dispenser States
GET {{baseUrl}}/management/inventory/dispenser-states
Authorization: Bearer {{accessToken}}
Accept: application/json

### Refill Kiosk Dispenser
POST {{baseUrl}}/management/kiosks/{{sampleGuid}}/inventory/dispenser-states/{{sampleGuid}}/refill
Authorization: Bearer {{accessToken}}
Content-Type: application/json

{
  "quantity": 1000,
  "reasonCode": "BATCH-001"
}


# ==========================================
# 8. Operations Telemetry
# ==========================================

### Get Kiosk Heartbeats History
GET {{baseUrl}}/management/kiosks/{{sampleGuid}}/heartbeats?from=2026-06-13T00:00:00Z&to=2026-06-14T00:00:00Z&pageNumber=1&pageSize=20
Authorization: Bearer {{accessToken}}
Accept: application/json

### Get Kiosk Device Events
GET {{baseUrl}}/management/kiosks/{{sampleGuid}}/events?minSeverity=Warning&from=2026-06-13T00:00:00Z&to=2026-06-14T00:00:00Z&pageNumber=1&pageSize=20
Authorization: Bearer {{accessToken}}
Accept: application/json


# ==========================================
# 9. Maintenance Tickets
# ==========================================

### List Maintenance Tickets
GET {{baseUrl}}/management/maintenance-tickets?status=Open&pageNumber=1&pageSize=10
Authorization: Bearer {{accessToken}}
Accept: application/json

### Create Maintenance Ticket
POST {{baseUrl}}/management/maintenance-tickets
Authorization: Bearer {{accessToken}}
Content-Type: application/json

{
  "organizationId": "{{sampleGuid}}",
  "storeId": "{{sampleGuid}}",
  "kioskId": "{{sampleGuid}}",
  "title": "Arm motor calibration error",
  "description": "Axis 3 motor overheated during dispensing, requires manual check",
  "priority": "High",
  "issueCode": "MOTOR_ERR"
}
````

## File: docs/process/VERTICAL_SLICE_REVIEW.md
````markdown
# Vertical Slice Review

Use this process for backend work that crosses an API, job, event handler,
aggregate, persistence boundary, projection, or external dependency. The unit
of review is the workflow and its invariants, not an individual layer or file.

## Search Keywords

`vertical slice review`, `workflow invariant`, `failure scenario`, `failure path`,
`completion evidence`, `scope freeze`, `independent diff review`, `definition of done`,
`wide scan`, `horizontal audit`, `finding ledger`, `pattern scan`, `root cause`

## When To Use

Use the full gate when a change affects one or more of:

- public API or Edge/event contracts;
- lifecycle or aggregate transitions;
- tenant authorization;
- retried or concurrent commands;
- multiple database writes or external I/O;
- background reconciliation, cleanup, retention, or notification delivery.

For a local mechanical change, use only the applicable checks. Documentation-only
work does not require runtime evidence unless the user explicitly requests it.

## Audit And Remediation Modes

Keep broad inspection separate from implementation.

### Wide Scan

The purpose of a wide scan is coverage and anomaly discovery, not immediate
remediation. Freeze the repository baseline first, then inspect multiple slices
for weak points in:

- API and message contracts;
- validation, authorization, and tenant scope;
- transaction, lifecycle, concurrency, and idempotency behavior;
- mappings, database constraints, logging, and external dependencies;
- missing, misleading, or happy-path-only tests.

Do not interrupt the scan to repair each isolated finding. Record enough evidence
to triage it:

| Field | Required content |
| --- | --- |
| Finding ID | Stable identifier used through remediation |
| Baseline | Commit/worktree state inspected |
| Vertical slice and entry point | API, job, event, or command where it appears |
| Invariant | Expected rule that may be violated |
| Reproduction | Input, state, expected result, actual result, and reproducibility |
| Evidence | File/line, trace, query, test, or database evidence |
| Impact | Financial, physical, tenant, data, operational, or maintainability impact |
| Classification | Suspected, Confirmed, Design Debt, or Deferred |
| Pattern hint | Other slices or query shapes that may share the root cause |

Use impact-based priority:

- `P0`: money movement, physical action, tenant isolation, security, or data loss;
- `P1`: lifecycle corruption, duplicate effects, concurrency, or permanently stuck workflow;
- `P2`: contract mismatch, weak observability/recovery, or material maintenance debt;
- `P3`: naming, layout, or duplication without incorrect behavior.

Do not label a code smell as a confirmed bug without reproduction or a proven
invariant violation.

### Vertical Remediation

After triage, select one finding and freeze its complete affected slice. Trace
from the entry point through validation, authorization, application orchestration,
domain transition, persistence, external I/O, projection, retry, cleanup, and
tests. Determine where the violation begins and which boundary should have
prevented it before choosing patch, refactor, or rewrite.

Once the root cause is known, scan horizontally for the same pattern. For example,
if the root cause is an unscoped `GetById` store method, inspect all stores and
handlers using that query shape; do not repair only the endpoint that exposed it.
Create a shared abstraction only when the instances enforce the same invariant
and have the same ownership boundary.

The operating loop is:

```text
Freeze baseline
  -> wide scan without edits
  -> findings ledger
  -> confirm, classify, and prioritize
  -> select and freeze one vertical slice
  -> trace root cause and failure paths
  -> repair the owning boundary
  -> regression tests
  -> horizontal same-pattern scan
  -> independent final-diff review and preflight
  -> close with evidence or record residual risk
  -> return to the wide scan
```

## Required Sequence

This sequence applies after a finding has been selected for remediation:

1. Freeze the scope before editing.
2. Map the complete workflow from entry point to durable and external effects.
3. Define applicable invariants.
4. Write failure scenarios before changing code.
5. Implement the complete frozen slice in one coherent pass.
6. Run focused verification, then the repository preflight.
7. Review the final diff independently without expanding the architecture.
8. Report completion only when every applicable item has evidence.

Do not replace this sequence with a layer-by-layer review. A controller, handler,
or repository can be correct in isolation while its retry job, retention policy,
or projection still violates the same workflow rule.

## Workflow Map

Trace the smallest applicable form of this chain:

```text
API / GraphQL / Job / Event Ingest
  -> authentication, authorization, and tenant scope
  -> request validation and idempotency identity
  -> application orchestration
  -> aggregate transition and cross-context snapshots/IDs
  -> transaction, locking, and database constraints
  -> external I/O and committed-state boundary
  -> projection, notification, audit, and stock evidence
  -> retry, reconciliation, retention, cleanup, and diagnostics
```

Search every use of the affected status, identity key, failure code, predicate,
and serialized field. Shared behavior must have one typed owner; API handlers,
jobs, and stores must not independently invent equivalent rules.

## Invariant Matrix

Complete this matrix before implementation. Use `N/A` only with a concrete
reason.

| Area | Questions | Expected evidence |
| --- | --- | --- |
| Lifecycle | Which transitions are allowed, terminal, reversible, or forbidden? | Domain/policy unit tests |
| Tenancy | Which Organization/Store/Kiosk owns the resource? Can role and scope be combined across assignments? | Cross-tenant negative integration test |
| Idempotency | What is the identity? What does same-key/different-payload do? How long is evidence retained? | Duplicate and retry tests plus uniqueness/lock review |
| Concurrency | Which resource is locked? What happens on two simultaneous requests or allocators? | PostgreSQL concurrency test |
| Transaction | Which writes commit atomically? Is item-level partial success intentional? | Rollback or partial-failure integration test |
| External I/O | Does I/O happen before or after validation/commit? What compensates orphaned or ambiguous results? | Dependency failure and recovery test |
| Projection | Do read models, customer status, SignalR, audit, and stock evidence agree with source state? | Result/projection assertions |
| Retry | Which failures are retryable? Does cancellation stop immediately? Can retry duplicate physical or financial effects? | Retry exhaustion, cancellation, and duplicate tests |
| Background jobs | Can one poison item stop a bounded batch? Can an ineligible oldest item starve later work? | Poison-item and starvation tests |
| Retention | Can purge remove dedup, audit, or recovery evidence still needed by a live source workflow? | Retention integration test |
| Security | Are secrets/raw payloads excluded from normal responses and logs? | Contract/result assertions |
| Compatibility | Must an existing client/Edge contract remain compatible? | Contract test or explicit pre-deployment `N/A` |

## Failure Scenario Worksheet

Record concrete scenarios, not generic labels. At minimum consider:

- duplicate request before and after the original response is lost;
- same idempotency key with a different payload;
- two backend instances processing the same source identity;
- process crash before commit, after commit, or during external I/O;
- database success with notification/object-storage/provider failure;
- external success with database failure;
- stale state observed by a job after a newer transition;
- oldest batch candidate is invalid, has no recipient, or always throws;
- retry after retention or cleanup;
- cancellation while waiting or retrying;
- cross-tenant ID supplied to an otherwise authorized actor;
- physical output or money movement may already have occurred.

For each applicable scenario, state the expected durable state, retry behavior,
operator visibility, and whether automatic recovery is allowed.

## Temporary Review Record

For a substantial active task, create a short-lived worksheet in
`.project-memory/<SLICE_NAME>_REVIEW.md` using this structure:

```markdown
# <Slice Name> Review

## Frozen Scope
- Included:
- Excluded:
- Public contract changes:
- EF model/migration allowed:

## Workflow Map
- Entry point:
- Authority/aggregate:
- Persistence and locks:
- External effects:
- Reconciliation/retention:

## Invariants And Evidence
| Area | Invariant or N/A reason | Verification |
| --- | --- | --- |

## Failure Scenarios
| Scenario | Expected result | Verification |
| --- | --- | --- |

## Final Diff Review
- Stale/duplicated rules searched:
- Changes outside scope:
- Unverified risks:
```

Delete the temporary worksheet when the task is complete. Promote changed
contracts to their owning backend document and design reasoning to `Vault/`.

## Verification Order

Use the narrowest check that proves each invariant, then broaden:

1. policy/domain unit tests;
2. focused PostgreSQL/MinIO/provider integration tests;
3. concurrency and failure-path tests;
4. full affected test project;
5. backend preflight;
6. EF pending-model check when persistence mapping may have changed;
7. `git diff --check`, stale-identifier search, and independent final diff review.

Build success proves compilation only. A happy-path test does not prove retry,
tenancy, concurrency, cleanup, or retention behavior.

## Completion Report

The final report must distinguish:

- implemented behavior;
- verification evidence with actual pass counts;
- checks not run and why;
- residual risks or scenarios without evidence;
- whether an EF migration was created or required.

Do not state that the slice is complete while a material applicable failure path
has no evidence. State the missing evidence directly instead.

## Related Docs

- [Working Protocol](WORKING_PROTOCOL.md)
- [Backend Critical Rule Checklist](BACKEND_CRITICAL_RULE_CHECKLIST.md)
- [Dependency Rules](../architecture/DEPENDENCY_RULES.md)
- [Multi-Tenancy Rules](../architecture/MULTI_TENANCY_RULES.md)
- [Idempotency And Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
````

## File: docs/flows/CATALOG_RUNTIME_MENU_FLOW.md
````markdown
# Catalog Runtime Menu Flow

This document describes how catalog data becomes a sellable runtime menu for the tablet and edge runtime.

## Search Keywords

`catalog runtime menu`, `runtime menu`, `sales catalog`, `menu item`, `product variant`, `recipe`, `edge runtime projection`, `tablet menu`, `CloudSalesCatalog`, `menu sellability`, `machine readiness`

## Flow

```text
Catalog
  -> Product / ProductVariant / Recipe / Ingredient
  -> SalesCatalog Menu / MenuItem
  -> Cloud runtime menu snapshot
  -> Edge runtime projection
  -> Tablet display and checkout
```

## Rules

- Catalog owns product definitions and recipes.
- Sales Catalog owns sellable menu items and prices.
- Cloud runtime-menu reads require the Store to be open according to its typed schedule and `Store.TimeZone`. A closed Store returns `409` and no sellable snapshot is issued.
- An empty Store schedule means no opening-hours restriction. Once any day is configured, an omitted day is treated as closed; opening is inclusive and closing is exclusive. `OpensAt > ClosesAt` represents an overnight interval that continues until the following day's close time.
- Runtime menu from Cloud is a sales catalog snapshot, not a live machine readiness guarantee.
- Each snapshot has a random request identity and a deterministic content `Revision`. The runtime endpoint returns that revision as `ETag`; clients may revalidate with `If-None-Match` after `ExpiresAt` and receive `304` when sellable content is unchanged.
- Edge projection may include inventory, device, queue, and robot availability.
- Order item snapshots preserve historical sale truth after catalog/menu changes.
- Checkout revalidates Catalog and Sales Catalog under one repeatable-read transaction snapshot before persisting immutable order-item, recipe, and option snapshots.
- Product/ProductVariant deletion and Product currency changes are rejected while a non-deleted MenuItem references them. Menu currency changes are rejected once the Menu contains items. These rules prevent active menu references from retaining deleted catalog definitions or a currency mismatch.
- Activating a MenuItem performs static authoring preflight for Product/Variant/Recipe ownership, recipe lifecycle and ingredients, currency, and option satisfiability. Dynamic route, connectivity, and inventory readiness remain runtime or deployment concerns.
- Inventory V1 is reporting/operations only and does not decide runtime menu sellability.

## Related Docs

- [System Flows](SYSTEM_FLOWS.md)
- [Checkout Execution Flow](CHECKOUT_EXECUTION_FLOW.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Boundary Contexts](../architecture/BOUNDARY_CONTEXTS.md)
````

## File: docs/flows/MAINTENANCE_TICKET_FLOW.md
````markdown
# Maintenance Ticket Flow

This document describes the maintenance/support ticket workflow, including manual creation and the bounded inventory-alert automation entry point.

## Search Keywords

`maintenance ticket`, `support ticket`, `manual support`, `staff support`, `technician assignment`, `kiosk maintenance`, `device issue`, `order issue`, `device event evidence`, `maintenance.create`, `maintenance.manage`

## Scope

Maintenance Ticket is an operations/support work-management aggregate. Most
tickets are created manually; configured inventory-empty alert automation may
create one ticket linked to the alert.

It is not:

- the owner of alert correlation or lifecycle;
- a chat workflow;
- a robot runtime workflow;
- a long-term analytics aggregate.

Tickets are kiosk-scoped work items. They may optionally link supporting evidence such as device, order, device event, or alert references.

Each ticket declares `OperationalImpact`: `None`, `BlocksNewOrders`, or `RequestsEmergencyStop`. Starting a blocking ticket atomically moves the kiosk to `Maintenance`; an emergency-impact ticket moves it to `EmergencyStopRequested`. A normal evidence-only ticket does not affect sales.

`EmergencyStopRequested` only holds new Cloud work and records that immediate safety intervention is required. It does not send a hardware command and does not assert that the robot stopped. Physical `EmergencyStopped` truth belongs to the typed Edge safety projection.

## Flow

```text
Staff / Manager / Technician sees an issue
  -> create maintenance ticket
  -> ticket starts Open
  -> manager / technician assigns owner
  -> technician starts work
  -> technician resolves with notes
  -> manager / authorized actor closes ticket
```

Alternative cancellation path:

```text
Open / Assigned / InProgress ticket
  -> cancelled with reason
  -> no further lifecycle transition in V1
```

Bounded automated entry path:

```text
InventoryAlertReconciler detects INVENTORY_EMPTY
  -> raises or correlates the Alert
  -> optionally creates one linked Open maintenance ticket
  -> later alert recovery does not close the ticket
```

## Status Lifecycle

Allowed V1 transitions:

| From | Action | To |
| --- | --- | --- |
| `Open` | assign | `Assigned` |
| `Open` / `Assigned` | start | `InProgress` |
| `InProgress` | resolve | `Resolved` |
| `Resolved` | close | `Closed` |
| `Open` / `Assigned` / `InProgress` | cancel | `Cancelled` |

Resolving, closing, or cancelling a ticket does not automatically return the kiosk to `Operational`. An authorized operator must verify the kiosk and explicitly resume it. This avoids reopening sales while another ticket, cleaning task, restock, or safety condition remains active.

Forbidden V1 transitions:

- `Resolved -> Cancelled`
- `Closed -> Resolved`
- `Closed -> Cancelled`
- `InProgress -> Assigned`
- `Cancelled -> any other status`

## Permissions

| Policy | Roles | Meaning |
| --- | --- | --- |
| `maintenance.view` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | View tickets within assigned scope |
| `maintenance.create` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | Create tickets within assigned scope |
| `maintenance.manage` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | Assign, start, resolve, close, cancel, or update tickets within assigned scope |

Staff can create and view tickets in assigned scope, but cannot assign, resolve, close, or cancel by default.

An assignee must be an active account with an active `Technician`, `Manager`, or
`OrgAdmin` role assignment that matches the ticket kiosk, store, or organization
on that same role-scope record. An account's role in another tenant does not make
it assignable. A push-notification device is optional and does not determine
assignment eligibility.

## Evidence Links

A ticket may reference:

- `KioskId` as the primary scope anchor;
- `DeviceId` when the issue is tied to a physical device;
- `OrderId` when the issue affects a customer order;
- `DeviceEventId` when the issue is backed by a telemetry/event record.

Evidence links should stay lightweight. Do not embed full order, account, event payload, or raw telemetry objects into ticket responses.

## API Surface

Management REST endpoints are listed in [Management API Surface](../api/MANAGEMENT_API_SURFACE.md).

V1 does not expose a GraphQL maintenance aggregate. REST remains the current maintenance read/write surface.

## Excluded From Current Contract

The current contract excludes:

- chat/comment thread;
- ticket reopen;
- ticket SLA/escalation workflow;
- a GraphQL maintenance aggregate.

Inventory alert automation is implemented separately: when configured, an
`INVENTORY_EMPTY` alert creates one linked maintenance ticket. General device
events do not automatically create tickets, and resolving the alert does not
close its ticket.
- GraphQL maintenance dashboard aggregate;
- robot runtime integration.

## Related Docs

- [Operations Support Flow](OPERATIONS_SUPPORT_FLOW.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Authorization Rules](../api/AUTHORIZATION_RULES.md)
- [Boundary Contexts](../architecture/BOUNDARY_CONTEXTS.md)
````

## File: docs/flows/OPERATIONS_SUPPORT_FLOW.md
````markdown
# Operations Support Flow

This document describes operational visibility and manual support flows after kiosks are running.

## Search Keywords

`operations support`, `operations telemetry`, `heartbeat`, `device event`, `stock movement`, `execution event`, `management dashboard`, `order status history`, `manual refund`, `maintenance support`, `staff support`

## Flow

```text
Kiosk / Edge
  -> heartbeat
  -> device events
  -> local operation logs
  -> stock movements
  -> execution events
  -> Cloud read models
  -> management dashboard / support screens
```

Back-office support example:

```text
Order issue
  -> inspect order overview
  -> inspect order status history
  -> inspect payment status
  -> inspect kiosk heartbeat/events/logs
  -> mark refund required or create refund record when needed
```

## Rules

- Heartbeats, device events, and Edge local operation logs are operational evidence. Operation-log list/detail reads are kiosk-scoped and return curated fields; raw payload is available only through the separate scoped diagnostics permission.
- `DeviceEvent` remains immutable evidence. Error/Critical device-event ingestion creates a separate actionable `Alert`; see [Alert Lifecycle Flow](ALERT_LIFECYCLE_FLOW.md). Maintenance tickets remain separate manual work items.
- Inventory V1 is reporting/operations only and does not control runtime sellability.
- Maintenance tickets are separate from the alert engine. Most are created manually; configured inventory-empty alert automation may create one linked ticket without transferring alert lifecycle ownership to Maintenance.
- Manual refund/compensation is tracked in the backend, but actual money movement can be staff-handled outside provider integration in V1.
- Operations telemetry APIs expose curated heartbeat/event fields only. Do not return raw `PayloadJson` by default.

## Real-time Operations Updates

To support operations personnel, changes to support tickets and inventory emit real-time SignalR notifications:
- **`MaintenanceTicketChanged`** is published on `OperationsHub` to group `kiosk:{kioskId}` when tickets are created, updated, assigned, started, resolved, closed, or cancelled.
- **`InventoryChanged`** is published on `OperationsHub` to group `kiosk:{kioskId}` when a dispenser is refilled or its stock estimate is adjusted.

These events allow operations dashboard screens to reflect live maintenance updates and ingredient levels instantly.

## Related Docs

- [System Flows](SYSTEM_FLOWS.md)
- [Maintenance Ticket Flow](MAINTENANCE_TICKET_FLOW.md)
- [Failure Flow Index](FAILURE_FLOW_INDEX.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Authorization Rules](../api/AUTHORIZATION_RULES.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
````

## File: docs/flows/ROBOT_LUA_AUTHORING_AND_IMPORT_FLOW.md
````markdown
# Robot Lua Authoring And Import Flow

This document owns Fairino export, authoring-bundle import, template/artifact lifecycle, technical contracts, and ordered `RobotProgram` authoring.

## Search Keywords

`Fairino Studio`, `.lua`, `Lua export`, `RobotArtifact`, `RobotArtifactTemplate`, `RobotArtifactTechnicalContract`, `RobotProgram`, `RobotProgramArtifact`, `RunOrder`, `artifact.upload`, `program.manage`, `authoring import`, `sidecar`

The shared source of truth, boundary, and full lifecycle index are in [Robot Lua Artifact Flow](ROBOT_LUA_ARTIFACT_FLOW.md). Release, deployment, download, and activation rules are in [Robot Lua Deployment And Activation Flow](ROBOT_LUA_DEPLOYMENT_AND_ACTIVATION_FLOW.md).

## Primary FE Integration Journey

The normal FE path is one guided authoring workspace. It must not construct a
RobotProgram by calling artifact, technical-contract, and program CRUD routes
one by one after a normal Fairino export.

```text
POST import bundle
-> GET workspace
-> POST validate
-> GET workspace
-> POST materialize (create Draft resources)
-> GET workspace
-> POST preview composition
-> POST confirm composition
-> POST publish resources
-> POST create release draft
-> GET workspace
-> use normal release publication/deployment workflow
```

After every mutation, read `GET .../robot-authoring-imports/{importId}/workspace`.
It is the convergence read model for import status, validation, package
ownership context, release status, deployment preview, blockers, and currently
allowed actions. The client must use returned actions/blockers as guidance but
must still call the typed command route for the action; an action code is not a
generic mutation endpoint and does not grant permission.

| Workspace action | Typed command to call | When it is the normal next step |
| --- | --- | --- |
| `ValidateImport` | `POST .../{importId}/validate` | A bundle was staged. |
| `MaterializeImport` | `POST .../{importId}/materialize` | Validation succeeds. This materializes Draft contracts, artifacts, and program. |
| `PreviewSemanticComposition` | `POST .../{importId}/preview-composition` | Draft resources need a Recipe/production-option compatibility check. |
| `ConfirmSemanticComposition` | `POST .../{importId}/confirm-composition` | The exact composition preview is accepted. |
| `PublishImportResources` | `POST .../{importId}/publish-resources` | Composition has been confirmed and technical resources were reviewed. |
| `CreateConfigurationReleaseDraft` | `POST .../{importId}/create-release-draft` | Imported resources are published. |
| `PublishConfigurationRelease` | Configuration release publish route | The linked release is Draft and has passed release checks. |
| `ConfirmDeployment` | Configuration deployment route | The published release has an eligible endpoint. |

`robot-artifacts`, `robot-artifact-technical-contracts`, and `robot-programs`
are advanced technical-authoring resources. Use them to inspect, repair, clone,
or deliberately build a graph without a normal import. They are not the normal
bundle-import sequence.

The shared product/UI journey is [Robot Authoring Workspace Journey](../../../IceBot-Product/product/journeys/ROBOT_AUTHORING_WORKSPACE.md).

The management result uses `Materialized` and `ResourcesPublished` as public
status values. It exposes `materializedRobotProgramId`, `materializedAt`, and
validation `canMaterialize`. The persistence aggregate retains its internal
`Applied` naming; that storage detail is not part of the API contract.

## Step And API Lookup

| Step | Actor | API / operation | Effect |
| --- | --- | --- | --- |
| 1. Edit project | Fairino-Studio user | No backend API | Saves Blockly/editor state in a local `.fairobot` project. |
| 2. Export Lua | Fairino-Studio user | No backend API | The normal `Export LUA` action produces only one `*-export.zip` containing `export-manifest.json`, ordered `.lua` files under `artifacts/`, and matching `.icebot.json` sidecars under `contracts/`. A separate advanced menu command exports individual Lua/sidecar files for debugging. A normal editor step becomes one file; a paired loop becomes one file whose sidecar merges the semantics of both loop steps and requires one shared execution phase. |
| 2A. Stage authoring bundle | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports` | Uploads one bounded ZIP with `Idempotency-Key`, validates archive structure and target consistency, and creates a durable organization-scoped import session. It does not create or publish runtime resources. |
| 2B. Validate authoring bundle | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/validate` | Rechecks the staged checksum, sidecars, explicit `RunOrder`, existing artifact revisions, technical-contract identities, and program identity. Ambiguous revisions, Retired resources, and a Published artifact bound to another contract block materialization. Sidecar V1 is strictly opaque and may declare only `System`/`Motion` effects without ingredient, option, quantity, or capability semantics. Typed production semantics require V2. `System`/`Motion` effects cannot carry ingredient, option, or quantity fields. `Ingredient` requires `ingredientCode` and may carry `optionCode` when that ingredient consumption is conditional on an option. `Option` requires `optionCode` and may carry `ingredientCode` when the option consumes an ingredient. Composition accepts either typed representation for an option ingredient but still requires an exact option and ingredient match. Errors block materialization; valid V1 artifacts remain warnings during composition. |
| 2C. Materialize authoring bundle | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/materialize` | Requires both `artifact.upload` and `program.manage`. Materializes Draft technical contracts, immutable Draft artifacts, and one ordered Draft RobotProgram in one serialized metadata transaction. It never publishes or deploys. A newly created Draft contract is retained on the import item and is assigned to its artifact only after the contract is explicitly published. |
| 2S. Preview semantic composition | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/preview-composition` | Resolves required Recipe ingredients and selected production-affecting options against imported V2 technical effects. Returns proposed artifact order, conditional option membership, capability suggestions, typed blockers/warnings, and a checksum. It does not write data. V1 opaque artifacts may remain in the proposal with warnings but cannot satisfy typed ingredient/option requirements. |
| 2SC. Confirm semantic composition | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/confirm-composition` | Rebuilds the preview and requires the exact `PreviewChecksum`. With no blockers, atomically replaces membership/order on the imported Draft RobotProgram. It never publishes resources or creates a release. |
| 2P. Confirm import publication | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/publish-resources` | Explicitly publishes/reuses each contract, assigns it to the Draft artifact, verifies and publishes each artifact, then publishes the ordered program. The operation is resumable and stops with the exact resource error; it never creates a release or deployment. |
| 2R. Create release draft from import | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/create-release-draft` | After import publication, selects a Published/Active `Recipe` and supported production option codes. Backend derives the Published program, route code, capability JSON, priority, and binding order, then atomically creates one organization-owned Draft release and route. If artifact contracts declare exactly one capability, it is selected automatically; zero/multiple capabilities require explicit selection. Retry with the same selection returns the same release; a different selection returns conflict. It never publishes or deploys the release. |
| 2W. Read authoring workspace | Management UI | `GET /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/workspace` | Aggregates import progress, package-installation ownership targets, release status, compatible deployment endpoint previews, blockers, and next actions. Package ownership is informational. The workspace does not automatically require or propose a fork; a separate explicit customization workflow decides whether package-managed technical resources must be forked before mutation. |
| 2D. Read/discard import | Management UI | `GET .../robot-authoring-imports/{importId}` and `POST .../{importId}/discard` | Returns import status and lifecycle actions. Only imports that have not reached `Materialized` may be discarded; staged ZIP deletion is best effort. Cleanup retains staging bytes while an import remains `Uploaded`, `Validated`, or `Failed` so its advertised retry actions remain executable. `Materialized`, `ResourcesPublished`, or `Discarded` staging bytes may be removed by retention cleanup. |
| 2T. Manage global templates | SystemAdmin | `POST /api/v1/management/robot-artifact-templates`, then `PATCH /api/v1/management/robot-artifact-templates/{templateId}/publish` | Uploads reusable Lua templates as Draft and publishes reviewed templates. Templates may be listed, inspected, reviewed through a short-lived URL, and retired, but never execute directly. An incorrect unreferenced Draft may be discarded with `DELETE /api/v1/management/robot-artifact-templates/{templateId}`. Platform-owned technical contracts use the distinct `/api/v1/management/robot-artifact-template-contracts` collection. |
| 3. Find existing artifacts | Management UI | `GET /api/v1/management/organizations/{organizationId}/robot-artifacts` | Returns a tenant-scoped, paged artifact list with optional `search` and `status`. |
| 3T. Find global templates | Management UI | `GET /api/v1/management/robot-artifact-templates` | Returns reusable global templates. `SystemAdmin` manages them; `OrgAdmin` may inspect them. |
| 3C. Clone template | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-artifacts/from-template` | Copies one Published template with a still-Published, checksum-consistent technical contract into a separate organization-owned Draft artifact. Compatibility metadata and lineage are inherited. It does not publish or assign program membership. |
| 4. Upload files | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-artifacts` | Uploads up to 50 files with metadata. Each successful item creates one unassigned Draft `RobotArtifact`; no program sequence changes. Policy: `artifact.upload`. |
| 4A. Import technical sidecars | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-artifact-technical-contracts/import-sidecars` | Converts 1-50 reviewed Fairino V1 or V2 `.icebot.json` sidecars into organization-owned Draft technical contracts with item-level results. The persisted technical contract retains the supplied schema version. Re-importing the same code/version replaces that Draft only when schema version and runtime target remain unchanged; Published or Retired versions require a new version. Contracts are not published automatically. |
| 4B. Author technical contract | Management UI | `GET`, `PUT`, validation-preview, publish, retire, and Draft-discard routes under the organization technical-contract resource | Reviews and publishes the typed behavior provenance, then assigns its id to the Draft artifact. |
| 5. Inspect artifact | Management UI | `GET /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}` | Returns artifact metadata only when both artifact and organization match the caller's scope. It does not return a download URL. |
| 5A. Review Lua bytes | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/review-url` | Returns an `artifact.upload`-authorized short-lived presigned URL plus checksum/size metadata. The URL is ephemeral and must not be persisted as artifact identity. |
| 5B. Discard staging artifact | Management UI | `DELETE /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}` | Hard-deletes only an unreferenced Draft artifact. Metadata commits first; object deletion is best-effort and orphan cleanup handles any remaining object. |
| 6. Publish reviewed artifacts | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/robot-artifacts/publish` | Atomically publishes 1-100 unique selected Draft artifacts after staging review. Every Draft requires a compatible Published technical contract and object-storage size/SHA-256 verification. Already Published selections are idempotent success; other states reject the request. Policy: `artifact.upload`. |
| 6A. Publish one artifact | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/publish` | Single-artifact alternative with explicit organization ownership. |
| 6B. Retire artifact | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/retire` | Stops new authoring use without deleting Lua bytes or breaking published manifests/rollback. Draft program references must be removed first. |
| 7. Create program | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-programs` | Creates an organization-owned Draft `RobotProgram`, optionally narrowed to Store, Kiosk, or Device scope. Policy: `program.manage`. |
| 8. Set execution order | Management UI | `PUT /api/v1/management/organizations/{organizationId}/robot-programs/{programId}/artifacts` | Atomically replaces the complete `RobotProgramArtifact` membership and explicit `RunOrder` values while the program is Draft. |
| 9. Edit program metadata | Management UI | `PUT /api/v1/management/organizations/{organizationId}/robot-programs/{programId}` | Updates Draft code, name, and description. Scope remains immutable. |
| 10. Review programs | Management UI | `GET /api/v1/management/organizations/{organizationId}/robot-programs` and `GET /api/v1/management/organizations/{organizationId}/robot-programs/{programId}` | Returns tenant-scoped program data and ordered artifact metadata for review/reordering. Policy: `program.read`. |
| 11. Publish program | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/robot-programs/{programId}/publish` | Validates referenced artifacts and publishes the immutable program definition. |
| 11A. Retire program | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/robot-programs/{programId}/retire` | Stops new release authoring while preserving published release and rollback history. Draft release references must be removed first. |
| 11D. Discard program draft | Management UI | `DELETE /api/v1/management/organizations/{organizationId}/robot-programs/{programId}` | Hard-deletes only a Draft program and its ordered membership. Published or release-referenced programs are preserved. |
| 11B. Load release options | Management UI | `GET /api/v1/management/organizations/{organizationId}/configuration-releases/authoring-options` | Returns eligible machine-produced ProductVariant, Published/Active Recipe, and Published RobotProgram options for release authoring. Optional `productVariantId`, `search`, and per-group `limit` reduce selector payloads. Route submission sends `RecipeId`; backend derives and stores its `ProductVariantId`. |
| 11C. Automated release linkage | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/create-release-draft` | Normal custom-authoring path after import publication. The client does not submit program ID, ProductVariant ID, route code, raw capability JSON, priority, or binding order. Sidecar V1 does not declare a reliable workcell capability, so capability code remains an explicit selection until semantic sidecar composition exists. |

## Fairino Export Mapping

The normal automation input is one ZIP with this fixed archive layout:

```text
export-manifest.json
artifacts/<file>.lua
contracts/<file>.icebot.json
```

`export-manifest.json` owns the explicit positive, unique, contiguous `runOrder`.
The backend does not derive order from filename prefixes. Archive paths are
normalized and bounded; traversal, symbolic links, duplicate normalized paths,
unsafe compression ratios, excessive entry counts, and excessive expanded size
are rejected before an import session is created.

Fairino-Studio currently exports files using human-readable ordered names such as:

```text
01_MoveJ_PrepareTray.lua
02_SetDO_OpenValve.lua
03_Loop_DispenseCycle.lua
```

The mapping is:

```text
one exported .lua file
  -> one RobotArtifact

one reusable ordered execution definition
  -> one RobotProgram

one position inside that program
  -> one RobotProgramArtifact with RunOrder
```

Filename prefixes are not execution authority. The management client must send explicit positive, unique `RunOrder` values. Cloud serializes that order into the program manifest, and Edge executes the manifest order.

Sidecar schema behavior:

- V1 remains valid for existing projects and declares generic `System`/`Motion` effects plus phase order.
- V2 is emitted only when the Fairino step has explicit IceBot semantics. It may declare `IngredientCode`, `OptionCode`, `FixedInArtifact` quantity/unit, workcell capability, phase, and before/after effect constraints.
- Sidecar enum fields use string names; numeric enum values and the currently unsupported `Composite` effect kind are rejected.
- Fairino and Cloud never infer ingredient, option, or physical quantity from a display label or Lua filename.
- `Parameterized` quantity is rejected during bundle validation for the current Fairino runtime.

## Artifact Upload Contract

Global template upload uses the same multipart file/manifest shape and file limits, with `templateCode` and `templateName` replacing organization artifact code/name. Template object keys live under `robot-artifact-templates/`; organization clones receive separate keys under `robot-artifacts/{organizationId}/`.

Bulk upload is the only public upload API and accepts one to 50 files through `multipart/form-data`.

Each manifest item contains:

| Field | Meaning |
| --- | --- |
| `fileName` | Basename matching exactly one uploaded non-empty `.lua` file. |
| `artifactCode` | Stable management code within the organization. |
| `artifactName` | Human-readable name. |
| `runtimeTargetCode` | Runtime compatibility gate, for example a Fairino Lua runtime target. |
| `machineModelCode` | Robot/machine model compatibility gate. |
| `exportedAt` | Optional design-time export timestamp. |
| `description` | Optional management description. |
| `metadataJson` | Optional valid JSON metadata; not a compatibility authority. |

Cloud computes the checksum from uploaded bytes. Clients do not provide or choose the authoritative checksum or storage key.

The multipart request contains:

- `files`: repeated file part, one per exported `.lua` file.
- `manifestJson`: JSON array that matches each file by case-insensitive basename.

Example `manifestJson`:

```json
[
  {
    "fileName": "01_MoveJ_PrepareTray.lua",
    "artifactCode": "PREPARE_TRAY",
    "artifactName": "Prepare tray",
    "runtimeTargetCode": "FAIRINO_LUA_V1",
    "machineModelCode": "FR5",
    "exportedAt": "2026-06-27T10:00:00Z",
    "description": null,
    "metadataJson": null
  },
  {
    "fileName": "02_SetDO_OpenValve.lua",
    "artifactCode": "OPEN_VALVE",
    "artifactName": "Open valve",
    "runtimeTargetCode": "FAIRINO_LUA_V1",
    "machineModelCode": "FR5"
  }
]
```

Request-level rules are validated before any item is written:

- 1 to 50 files, each no larger than 10 MiB.
- File count equals manifest item count.
- Every uploaded basename has exactly one manifest item.
- File names are unique within the request.
- Every file uses the `.lua` extension and required metadata is present.

Execution uses item-level atomicity:

- Successful items remain committed even if another item fails.
- Failed items do not roll back successful uploads.
- All newly created items return HTTP `201`.
- A fully successful request containing one or more existing matches returns HTTP `200`.
- Mixed success/failure returns HTTP `207` with an item result for every file.
- All failure returns HTTP `400` with the complete item result collection.
- Each successful item returns `RobotArtifactId`, artifact metadata, and `wasExisting`. Summary fields distinguish `uploadedCount` from `existingCount`. The client chooses membership and `RunOrder` later through the program-membership `PUT`.

If the HTTP request is interrupted after some items commit, the client may safely retry the same files and metadata identity. Matching organization + normalized artifact code + SHA-256 returns the existing artifact as success instead of creating duplicate metadata.

### Staging Behavior

- Upload does not add an artifact to any `RobotProgram`.
- Upload does not append an artifact to the end of an existing sequence.
- Upload does not publish an artifact or program.
- Draft discard is unavailable after publication and is blocked while any robot program references the artifact.
- Review URLs are short-lived transport data. A discard may invalidate an already-issued review URL before its nominal expiry.
- Management UI should show newly uploaded files as unassigned relative to the selected program.
- The user explicitly drags/inserts an artifact into the ordered list, or chooses an explicit append action.
- Only the subsequent `PUT /management/organizations/{organizationId}/robot-programs/{programId}/artifacts` assigns membership and `RunOrder`.
- Unassigned organization artifacts do not block program publication because they are outside that program aggregate.

### Bulk Publish

Request:

```json
{
  "robotArtifactIds": [
    "11111111-1111-1111-1111-111111111111",
    "22222222-2222-2222-2222-222222222222"
  ]
}
```

Rules:

- All selected artifacts must exist and belong to the selected organization.
- Draft artifacts become Published in one database transaction.
- Already Published artifacts are returned as successful no-op items, making request retry safe.
- Any missing, cross-organization, or Retired artifact rejects the whole request before statuses change.
- Bulk publish does not assign program membership or `RunOrder`.

## Program Ordering Contract

Example membership replacement:

```json
{
  "artifacts": [
    {
      "robotArtifactId": "11111111-1111-1111-1111-111111111111",
      "runOrder": 1,
      "parametersJson": null
    },
    {
      "robotArtifactId": "22222222-2222-2222-2222-222222222222",
      "runOrder": 2,
      "parametersJson": null
    }
  ]
}
```

Rules:

- Replacement is allowed only while `RobotProgram.Status = Draft`.
- The collection must not be empty.
- `RunOrder` starts at a positive value and must be unique. Contiguous numbering is recommended but not required by V1.
- Reusing the same `RobotArtifact` at different run orders is allowed.
- Every artifact must belong to the program organization.
- Publishing requires every referenced artifact to be Published.
- `RobotProgramArtifact` is aggregate membership, not an independent CRUD resource.

## Related Docs

- [Robot Lua Artifact Flow](ROBOT_LUA_ARTIFACT_FLOW.md)
- [Robot Lua Deployment And Activation Flow](ROBOT_LUA_DEPLOYMENT_AND_ACTIVATION_FLOW.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Authorization Rules](../api/AUTHORIZATION_RULES.md)
````

## File: docs/flows/SYSTEM_OVERVIEW_FLOW.md
````markdown
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
````

## File: docs/process/BACKEND_CRITICAL_RULE_CHECKLIST.md
````markdown
# Backend Critical Rule Checklist

Use this checklist only for handoff checks that need deployed infrastructure, provider credentials, a real client, or physical runtime evidence. Domain and persistence invariants belong in automated tests and their owning contract documents.

## Search Keywords

`manual backend verification`, `deployment handoff`, `external provider smoke`, `physical robot smoke`, `MQTT recovery`, `PayOS callback`, `Firebase delivery`, `SMTP invitation`

## Automated Coverage Boundary

Do not manually duplicate these checks during every handoff:

| Rule family | Primary automated evidence |
| --- | --- |
| Manual, Packaged, and MachineProduced fulfillment transitions | `OrderItemFulfillmentTests`, `PaidOrderFulfillmentConcurrencyIntegrationTests` |
| Production incident lifecycle and exact-unit remake | `ProductionIncidentTests`, Edge production integration tests |
| Duplicate payment settlement and refund targeting | `PaymentWebhookConcurrencyIntegrationTests`, `RefundConfirmationTests` |
| Kiosk operational-state sales gating | `KioskOperationalStateTests` and checkout/runtime-menu tests |
| Production Package installation and upgrade invariants | Production Package integration tests |
| MQTT credential lifecycle | MQTT credential unit and integration tests |

If one of these test suites is disabled or failing, report that missing evidence instead of replacing it with an undocumented manual assumption.

## Deployment Environment

Verify against the target environment:

- migrations apply to an empty database and the intended upgrade baseline;
- required object-storage buckets exist or startup fails according to configured bucket ownership;
- PostgreSQL, MinIO, MQTT, SMTP, PayOS, and Firebase settings satisfy startup validation;
- health endpoints distinguish startup readiness from optional dependency degradation;
- secrets are supplied through the deployment secret mechanism and do not appear in responses or logs.

## External Provider Smoke

Verify with non-production provider credentials where available:

- PayOS accepts a valid signed callback and rejects an invalid signature;
- a duplicate PayOS callback does not create a second settlement;
- SMTP invitation delivery produces only the configured invitation URL;
- Firebase sends a critical operational notification to an eligible registered device;
- an invalid Firebase token is retired without retrying it indefinitely.

## Edge And Robot Smoke

Verify only when the real Edge/controller runtime is available:

- MQTT wake-up is followed by durable command pull;
- command ACK and execution reports preserve the original `CommandId`;
- artifact checksum and production-definition provenance are verified before activation;
- restart during execution follows the published restart policy and does not assume an unsafe resume;
- uncertain, partial, or defective physical output opens the expected production incident;
- Edge refuses a command when it cannot persist state before ACK.

## Recovery Smoke

- restart MQTT and confirm pending commands remain pullable;
- temporarily interrupt MinIO reads and confirm immutable artifact recovery behavior;
- restart Cloud/API during a pending workflow and confirm reconciliation resumes without duplicate effects;
- confirm stuck-workflow timeout and retry-failure metrics are emitted and alertable.

## Related Docs

- [Vertical Slice Review](VERTICAL_SLICE_REVIEW.md)
- [Working Protocol](WORKING_PROTOCOL.md)
- [Deployment Configuration](../operations/DEPLOYMENT_CONFIG.md)
- [Robot Artifact Operational Smoke Test](../operations/ROBOT_ARTIFACT_OPERATIONAL_SMOKE.md)
- [Restart And Power Recovery](../operations/RESTART_AND_POWER_RECOVERY.md)
````

## File: ARCHITECTURE.md
````markdown
# Architecture

IceBot is an ASP.NET Core backend for a multi-location automated vending system with robot arm integration. The current architecture is a modular monolith designed for sync-first edge/cloud operation and future service extraction.

## Architecture Style

The project uses:

- Clean Architecture boundaries at project level.
- Modular Monolith organization inside one deployable backend.
- Bounded-context grouping for business ownership.
- Tactical DDD where domain rules matter.
- CQRS-lite for complex workflows.
- Event-driven integration for sync, robot runtime, payment callbacks, and operational events.
- EF Core as the primary unit-of-work and persistence model.

Do not split into microservices yet. Keep module boundaries clear inside the monolith until domain ownership, transaction boundaries, and sync flows are stable.

## Project Layout

```text
src/
  WebAPI/
  Application/
  Domain/
  Infrastructure/
```

The source root is named `src` because the backend is still one deployable modular monolith. Do not use `services` until there are multiple independently deployable services with separate runtime, deployment, and ownership boundaries.

Current compile-time dependency chain:

```text
WebAPI -> Infrastructure -> Application -> Domain
```

This is a pragmatic clean-ish structure. `Domain` remains independent. `Application` owns use cases and contracts. `Infrastructure` owns persistence and external adapters. `WebAPI` owns HTTP concerns.

Detailed dependency rules live in [docs/architecture/DEPENDENCY_RULES.md](docs/architecture/DEPENDENCY_RULES.md).

## Layer Responsibilities

### WebAPI

Owns presentation and HTTP concerns:

- Controllers and route contracts.
- Middleware.
- Authentication and authorization attributes.
- Swagger/API versioning.
- Request/response shaping.

Controllers should delegate to Application use cases. They should not contain domain rules or persistence logic.

### Application

Owns use-case orchestration:

- Command/query handlers for controller-facing use cases.
- Request/response DTOs.
- Validators.
- Reusable Application services for capabilities and internal workflow helpers.
- Transaction boundaries.
- Idempotency checks at API/use-case boundary.
- Contracts for external dependencies.

Use CQRS-lite handlers for controller-facing Application use cases. Reusable services remain appropriate for capabilities such as token issuance, authentication, invitation generation, provider integration, policies, and calculators.

### Domain

Owns business model and invariants:

- Entities.
- Value objects.
- Domain enums.
- Domain methods.
- Base entity abstractions.
- Bounded-context namespaces.

Domain must not depend on WebAPI, Infrastructure, EF Core, logging, messaging, or external SDKs.

Domain context ownership is documented in [docs/architecture/BOUNDARY_CONTEXTS.md](docs/architecture/BOUNDARY_CONTEXTS.md).

### Infrastructure

Owns technical implementation:

- `IceBotDbContext`.
- EF Core mappings and migrations.
- PostgreSQL persistence.
- External provider adapters.
- Fairino robot SDK adapter.
- Payment provider adapters.
- Sync/inbox/dead-letter workers.
- Background jobs and technical integrations.

Infrastructure may reference Application and Domain because it implements application contracts and persists domain entities.

## Request Flow

Typical API flow:

```text
HTTP request
  -> WebAPI controller
  -> Application command/query handler
  -> Domain entity methods/invariants
  -> Infrastructure persistence/adapters
  -> DbContext SaveChangesAsync
  -> ApiResult<T> / PagedResult<T>
```

Cross-cutting WebAPI pipeline:

- `CorrelationIdMiddleware`
- `GlobalExceptionMiddleware`
- `DebugBodyLoggingMiddleware` only when explicitly enabled for payload debugging
- Authentication
- Authorization

## Persistence And Transactions

EF Core `IceBotDbContext` is the primary unit of work.

Default approach:

- Use DbContext directly in application handlers for simple use cases.
- Commit once at the use-case boundary.
- Use explicit transactions when a use case spans multiple writes that must succeed together.
- Add focused repositories only for complex aggregate queries or persistence behavior that repeats across use cases.
- Keep repository abstractions thin when they exist. They should support rich handlers, not replace them.

When external systems are involved, do not hold a database transaction across network calls. Persist intent/state first, call the external provider, then persist result or retry state.

Avoid a global generic repository/service/controller stack. It creates long signatures and hides domain decisions. If an existing generic repository must stay, reshape it into a thin persistence helper instead of adding business behavior to it.

## Edge-Cloud Model

IceBot is sync-first and must tolerate intermittent connectivity.

Edge/kiosk runtime owns:

- Robot execution.
- Local device communication.
- Telemetry capture.
- Temporary operation while offline.
- Kiosk/device-scoped Fairino point/frame snapshot updates.

Cloud/backend owns:

- Organization/store/kiosk management.
- Global catalog/configuration templates.
- Reporting and monitoring.
- Payment integration.
- Central sync coordination.

Synchronization is event-oriented. Inbound processing should use inbox, idempotency keys, correlation ids, causation ids, retry state, and dead-letter handling.

## Event-Driven Patterns

Use events for integration and runtime evidence, not as a blanket replacement for domain state.

Recommended patterns:

- Inbox for incoming edge/provider events.
- Dead letter for failed sync/event processing.
- Append-only event tables for robot/device/runtime evidence.
- Retry with typed status/retry columns.
- Idempotency keys for public API and provider calls.
- Correlation and causation ids for traceability.

Outbox can be added when the system starts publishing reliable integration events from local transactions.

See [Idempotency and Retry Rules](docs/data/IDEMPOTENCY_RETRY_RULES.md).

## JSON Fields

JSON fields are allowed for robot SDK payloads, provider payloads, snapshots, and metadata, but workflow-critical values should be typed columns.

Detailed JSON rules live in [JSON Field Rules](docs/data/JSON_FIELD_RULES.md).

## Multi-Tenancy

`Organization` is the tenant root. `Store` belongs to an organization. `Kiosk` belongs to a store and carries `OrganizationId` for tenant filtering.

Configurable data can use `TenantScopeType`:

```text
Device > Kiosk > Store > Organization > Global
```

Tenant filters should be explicit and safe for admin/platform queries.

See [Multi-Tenancy Rules](docs/architecture/MULTI_TENANCY_RULES.md).

## API And Observability

API conventions:

- Keep route contracts stable unless explicitly changed.
- Use commands for state changes.
- Use queries for reads.
- Use explicit nested routes for parent-child resources.
- Use `ApiResult<T>` for normal responses.
- Use `PagedResult<T>` for paged list responses.

Operational concerns:

- Correlation id per request.
- Global exception handling.
- Request/response logging with sensitive data masking.
- Serilog file/console logs.
- Swagger for API visibility.

Detailed API naming and route conventions live in [Naming Rules](docs/process/NAMING_RULES.md).

## Design Constraints

Prefer:

- Bounded-context placement for new domain concepts.
- Thin WebAPI controllers.
- Application handlers/use cases for orchestration.
- Rich handlers with thin repositories for persistence helper behavior.
- Typed columns for workflow-critical state.
- Snapshots when runtime history must not depend on mutable catalog/configuration.

Avoid:

- Microservices before module boundaries are stable.
- Generic `Repository<TEntity, TKey>` and `BaseService<TEntity, TKey>` everywhere.
- Generic controllers for domain workflows.
- Event sourcing for the whole system.
- Hidden source-of-truth JSON payloads.
- Soft delete for append-only events, logs, and ledgers.
- Loading and serializing large EF navigation graphs.

## Documentation Map

- [Working Protocol](docs/process/WORKING_PROTOCOL.md)
- [Documentation Routing Map](docs/DOCUMENTATION_ROUTING_MAP.md)
- [Documentation Rules](docs/process/DOCUMENTATION_RULES.md)
- [Boundary Contexts](docs/architecture/BOUNDARY_CONTEXTS.md)
- [Dependency Rules](docs/architecture/DEPENDENCY_RULES.md)
- [Naming Rules](docs/process/NAMING_RULES.md)
- [API Surface Rules](docs/api/API_SURFACE_RULES.md)
- [Authorization Rules](docs/api/AUTHORIZATION_RULES.md)
- [Data Modeling Rules](docs/data/DATA_MODELING_RULES.md)
- [System Flows](docs/flows/SYSTEM_FLOWS.md)
- [Checkout Execution Flow](docs/flows/CHECKOUT_EXECUTION_FLOW.md)
- [Multi-Tenancy Rules](docs/architecture/MULTI_TENANCY_RULES.md)
- [Idempotency and Retry Rules](docs/data/IDEMPOTENCY_RETRY_RULES.md)
- [JSON Field Rules](docs/data/JSON_FIELD_RULES.md)
- [IoT Contract](docs/iot/IOT_CONTRACT.md)
````

## File: docs/api/IDENTITY_ONBOARDING_RULES.md
````markdown
# Identity Onboarding Rules

This document is the backend source of truth for internal account onboarding, invitation links, email ownership proof, and temporary password fallback.

## Search Keywords

`identity onboarding`, `account onboarding`, `admin creates account`, `internal account invitation`, `invitation link`, `accept invitation`, `GoogleEmail`, `GoogleSubjectId`, `Google login policy`, `email confirmed`, `EmailConfirmedAt`, `email ownership proof`, `CreateInvitation`, `SendInvitationEmail`, `InitialPassword`, `temporary password`, `Invited account`, `Active account`, `/api/v1/management/accounts`, `/api/v1/authentication/accept-invitation`

## Purpose

Internal accounts are created by authorized management users. Public signup is disabled for internal system accounts.

The default onboarding method is:

```text
admin creates account
  -> backend creates invitation link
  -> user accepts invitation
  -> user completes credential setup required by the admin-enabled login methods
  -> account becomes Active
```

Do not use username + password delivery as the default onboarding flow.

## Authentication Method Ownership

Management chooses which authentication methods an account is allowed to use through `LocalLoginEnabled` and `GoogleLoginEnabled`. Invitation acceptance does not let the invited user enable an authentication method that management did not authorize.

For Google login, `GoogleEmail` is the management-configured identity allowlist value:

- the verified email from Firebase must match `GoogleEmail`;
- first login binds the verified Google subject to `GoogleSubjectId`;
- later logins must match both the configured email and the bound subject;
- authentication must not overwrite `GoogleEmail` from token claims;
- changing `GoogleEmail` through account management clears the old subject binding so the newly authorized identity can bind on its first successful login.

The invited user only supplies credential material for an enabled method, such as choosing a password when local login is enabled. The user does not choose the account's allowed login policy.

The management password-reset/set-password command also changes credential material only. It does not implicitly enable local login; management must change `LocalLoginEnabled` through the account policy update contract.

## Default Flow

Default request behavior:

```text
CreateInvitation = true
SendInvitationEmail = true
```

Flow:

```text
POST /api/v1/management/accounts
  -> create Account
  -> Status = Invited
  -> create AccountInvitation
  -> return invitation link
  -> optionally send invitation email
```

The management response should include invitation details when an invitation is created:

```text
invitationUrl
expiresAt
emailSentAt
```

`Email:InvitationBaseUrl` is required at startup. Management responses return the complete invitation URL and never expose the raw bearer token as a separate field.

## Invitation Generation Vs Delivery

Invitation generation and invitation delivery are separate responsibilities.

Invitation generation owns:

- creating the raw token
- hashing the token before storage
- storing lifecycle fields
- validating token on accept
- expiration and revocation
- activating the account after successful acceptance

Invitation delivery owns:

- sending email when requested
- allowing admin/manual delivery through another channel

Email is only one delivery channel. Admin users may copy and send the invitation link through another approved channel such as email, Zalo, Messenger, Slack, Teams, QR code, printed paper, or an internal message.

## Invitation Accepted Vs Email Confirmed Vs Account Active

Do not collapse these three concepts into one state.

| Concept | Meaning | Stored as |
| --- | --- | --- |
| Invitation accepted | User presented a valid invitation token and completed the accept flow | `AccountInvitation.AcceptedAt` |
| Email confirmed | User proved ownership of the account mailbox | `Account.EmailConfirmedAt` |
| Account active | User is allowed to log in through an enabled auth method | `Account.Status = Active` |

Final rule:

```text
Accept invitation
  -> may activate account

Verified mailbox ownership
  -> may confirm email
```

These states are independent.

For multi-tenant systems, do not infer email ownership from the domain:

```text
@gmail.com
@outlook.com
@company.com
@corp.xyz.vn
```

The same tenant may contain company email, Gmail, Yahoo, contractors, or external accounts. The security criterion is ownership proof, not domain shape.

Valid email ownership proof can come from:

- a separate verify-email link
- Firebase/Google token with `email_verified = true` and email matching the management-configured `GoogleEmail`
- an invitation link sent by the backend to the same mailbox and accepted from that email delivery path

Manual invitation delivery is not email ownership proof.

Examples:

| Case | Result |
| --- | --- |
| Backend sends invitation email to `user@gmail.com`, user clicks and accepts | `Active`, `EmailConfirmedAt` can be set |
| Firebase returns verified `employee@corp.com` matching `GoogleEmail` | `Active`, `EmailConfirmedAt` can be set |
| Admin copies invitation link and sends through Zalo/Messenger/QR/paper | `Active`, `EmailConfirmedAt` remains null |
| Google login invitation where Firebase verified email matches `GoogleEmail` | `Active`, `EmailConfirmedAt` can be set |

## Email Failure

SMTP failure must not make onboarding unrecoverable.

If invitation email delivery fails:

```text
account remains Invited
invitation remains usable
response includes invitation link
EmailSentAt remains null
```

The management UI can show a warning and let the admin copy the link manually or create another invitation later.

Do not leak raw SMTP exception details to the API response. Log the server-side exception instead.

## Create Or Regenerate Invitation

Management can create a new invitation for an account that is still `Invited`.

One account should have at most one active invitation.

Invitation generation is serialized by account. Revoking prior active links and
persisting the replacement link are one transaction; concurrent regeneration
requests cannot leave multiple active invitation records.

Route direction:

```text
POST /api/v1/management/accounts/{accountId}/invitation
```

Request direction:

```json
{
  "sendEmail": true
}
```

Behavior:

```text
create new invitation
  -> optionally send email
  -> revoke previous active invitations
```

This route means "create a new invitation link". It is not only "resend email".

Expired invitations are not extended or revived. Create a new invitation instead.

## Accept Invitation

Invitation acceptance is a public endpoint and does not require an existing login.

Route direction:

```text
POST /api/v1/authentication/accept-invitation
```

Flow:

```text
user submits token and new password
  -> backend hashes token
  -> find active, non-expired, non-revoked invitation
  -> require account Status = Invited
  -> set password for local login when needed
  -> set account Status = Active
  -> mark invitation Accepted
  -> set EmailConfirmed only when mailbox ownership proof exists
  -> revoke existing sessions/refresh tokens
```

Invitation tokens must not activate accounts that are already `Active`, `Disabled`, or `Suspended`.

Acceptance is serialized by token. Account activation, invitation acceptance,
and revocation of existing refresh sessions commit in one transaction. A
failure while revoking sessions rolls back activation instead of leaving a
partially accepted account.

Accepting a valid invitation token proves token possession. It does not always prove mailbox ownership.

Acceptance is not idempotent. If a token has already been accepted, return an explicit error such as:

```text
400 Invitation already accepted.
```

If a token is expired or revoked, return an explicit error and require management to create a new invitation.

Email delivery proof uses:

```text
AccountInvitation.EmailSentAt
```

as the backend-email delivery proof for invitation acceptance. If `EmailSentAt` is null, accepting the invitation must not set `Account.EmailConfirmedAt`.

## Temporary Password Fallback

Temporary password creation is not the default flow.

It is allowed only when:

```text
CreateInvitation = false
```

For local login without invitation:

```text
InitialPassword is required
```

For invitation onboarding:

```text
InitialPassword is not allowed
```

Reason: if admin creates the password, admin knows the user's password. That contradicts the invitation-link onboarding rule.

Current backend behavior does not force password change for active accounts created with `InitialPassword`. Therefore, do not use active account + temporary password as the normal onboarding method.

Temporary-password onboarding is not part of the current contract. It requires
a separate forced-password-change lifecycle and restricted authenticated access;
do not infer that behavior from `InitialPassword`.

## Account Status Rules

| Status | Meaning |
| --- | --- |
| `Invited` | Account exists but cannot log in until invitation is accepted |
| `Active` | Account can log in through enabled auth methods |
| `Disabled` | Account is blocked by management action |
| `Suspended` | Account is blocked due to security or operational policy |

Login must reject non-`Active` accounts.

## Related Docs

- [API Surface Rules](API_SURFACE_RULES.md)
- [Authorization Rules](AUTHORIZATION_RULES.md)
- [Data Modeling Rules](../data/DATA_MODELING_RULES.md)
````

## File: docs/architecture/MULTI_TENANCY_RULES.md
````markdown
# Multi-Tenancy Rules

This document defines tenant isolation and configurable override scope for organizations, stores, kiosks, devices, catalog, menu, recipe, and robot configuration.

## Search Keywords

`multi-tenancy`, `tenant`, `tenant isolation`, `Organization`, `Store`, `Kiosk`, `TenantScopeType`, `Global`, `Organization scope`, `Store scope`, `Kiosk scope`, `Device scope`, `global query filter`, `Product`, `ProductVariant`, `Recipe`, `Menu`, `RobotProgram`, `ConfigurationRelease`, `OrgAdmin`

## Tenant Root

`Organization` is the tenant root.

`Store` belongs to an organization. `Store.OrganizationId` is required (non-nullable) to ensure all stores are bound to an organization.

`Kiosk` belongs to a store. `Kiosk.OrganizationId` and `Kiosk.StoreId` are both required (non-nullable) to ensure all kiosks are bound to an organization and store. When creating or updating a kiosk, validate that `Kiosk.OrganizationId == Store.OrganizationId`. OrganizationId is used for tenant isolation, reporting, and query filters.

## Organization Management

Organization management APIs live under:

```text
/api/v1/management/organizations
```

`SystemAdmin` owns platform-level organization lifecycle:

- create organization
- update all organization fields
- activate organization
- disable organization
- list/view all organizations

`OrgAdmin` is scoped to assigned organizations through `AccountRole`:

```text
RoleCode = OrgAdmin
OrganizationId = organizationId
StoreId = null
KioskId = null
```

`OrgAdmin` can:

- view assigned organization(s)
- update basic profile/contact fields for assigned organization(s)

`OrgAdmin` cannot:

- create organizations
- activate or disable organizations
- change `Code`
- change `Status`
- change legal/platform-managed fields such as `LegalName`, `TaxCode`, or `MetadataJson`
- access organizations outside assigned `AccountRole` scope

Do not infer organization access from email domain. Tenant access must come from scoped roles, not from addresses such as `@gmail.com`, `@company.com`, or `@corp.xyz.vn`.

Organization persistence ports should stay context-specific:

```text
Application.Tenants.Abstractions.IOrganizationStore
Infrastructure.Tenants.Persistence.OrganizationStore
```

Do not place organization-specific persistence in the generic `Infrastructure.Persistence.Repositories` namespace. That namespace is for generic/shared repository infrastructure.

Organization management APIs are implemented as command/query handlers, not a CRUD service.

## Store Management

Store management APIs live under:

```text
/api/v1/management/stores
/api/v1/management/organizations/{organizationId}/stores
```

`SystemAdmin` owns platform-level store operations.
`OrgAdmin` owns store management operations within their assigned organization scope.

- **Create Store:** `OrgAdmin` can create stores under their assigned organization.
- **Update Store:** `OrgAdmin` and `Manager` can update store details within their assigned scope. `Code` is immutable.
- **Disable/Activate Store:** `OrgAdmin` can activate or disable stores. Activating a store requires that its parent organization is active. Disabling a store does not cascade disable kiosks.
- Organization-scoped roles can access all stores in that organization.
- Store-scoped roles can access only the assigned store and must not be expanded to the whole organization.

Store persistence ports should stay context-specific:

```text
Application.Tenants.Abstractions.IStoreStore
Infrastructure.Tenants.Persistence.StoreStore
```

Do not place store-specific persistence in the generic repositories namespace.

Store management APIs are implemented as command/query handlers, not a CRUD service.

## Kiosk Management

Kiosk management APIs live under:

```text
/api/v1/management/kiosks
/api/v1/management/stores/{storeId}/kiosks
```

`SystemAdmin` owns platform-level kiosk operations.
`OrgAdmin`, `Manager`, and `Technician` own kiosk management operations within their assigned scope:
- **Create Kiosk:** Can create kiosks under their assigned store. Validates parent store and organization are active, and Kiosk's OrganizationId matches Store's OrganizationId.
- **Update Kiosk:** Can update kiosk details within scope. `Code`, `StoreId`, and `OrganizationId` are immutable.
- **Status Change:** Can change kiosk status. Setting to `Active` requires parent store and organization to be active.

Kiosk-scoped roles (e.g. Technician with KioskId scope) can access only their assigned kiosk.

Kiosk persistence ports:

```text
Application.Tenants.Abstractions.IKioskStore
Infrastructure.Tenants.Persistence.KioskStore
```

Do not place kiosk-specific persistence in the generic repositories namespace.

Kiosk management APIs are implemented as command/query handlers, not a CRUD service.

## Tenant Tree

Tenant tree is a management read model for scope selection and tenant navigation:

```text
GraphQL tenantTree
```

It returns:

```text
Organization
  -> Store
      -> Kiosk
```

Use it for:

- choosing valid `OrganizationId`, `StoreId`, and `KioskId` values when assigning role scope;
- management UI tenant navigation;
- avoiding invalid cross-tenant scope combinations.

Expose tenant tree through GraphQL to avoid maintaining a duplicated REST read endpoint for the same UI aggregation surface. REST and GraphQL are API adapters; tenant scope rules live in the Application/domain model, not in the transport. Do not turn tenant tree into an operations dashboard.

Do not use `tenant-tree` as an operations overview endpoint. Keep revenue, alerts, runtime state, inventory, and dashboard metrics in separate overview/reporting APIs. See [API Surface Rules](../api/API_SURFACE_RULES.md#read-model-api-boundaries) for read model boundary definitions.

## Role Scope Options

To select valid scopes for a target role being assigned to an account, use:

```text
GET /api/v1/management/role-scope-options?roleCode={roleCode}
```

It enforces scope boundaries based on the current user context, and projects allowed scope types:
- `OrgAdmin` allows Organization scope.
- `Manager` allows Organization and Store scope.
- `Technician` / `Staff` allows Store and Kiosk scope.

## Tenant Scope Enforcement

Current v1 tenant-scope convention:

- Do not use global EF tenant query filters yet.
- Enforce tenant scope explicitly in handlers, stores, and focused rule/helper methods.
- Use clearly named scoped query methods where useful, such as:
  - `GetByIdForTenantAsync`
  - `GetByIdForStoreScopeAsync`
  - `GetByIdForSystemAdminAsync`
- `SystemAdmin`, background workers, payment callbacks, and sync processors must use explicit bypass/system paths. Bypass behavior must not be hidden in generic query methods.
- Global/cross-tenant reads should be visible in method names or handler logic.
- Scoped reads should validate against `CurrentUserContext.AllowedOrganizationIds`, `AllowedStoreIds`, and `AllowedKioskIds` where applicable.

Reason: global EF tenant filters require a mature tenant context and bypass model. Until that is clear, explicit scoped methods are safer and easier to review.

### Management Read-Path Guardrails

The following management read paths enforce explicit tenant scope:

- `ManagementProductsController`: list/detail reads pass `CurrentUserContext` and enforce allowed organization/store/kiosk scope.
- `ManagementMenusController`: list/detail reads pass `CurrentUserContext` and enforce allowed organization/store/kiosk scope.
- `ManagementAccountsController`: list/detail reads use `accounts.read`; non-`SystemAdmin` reads are limited to accounts sharing an active organization/store/kiosk role scope.

Implementation rule:

- pass `CurrentUserContext` from WebAPI controller to the query object for management reads;
- for list queries, intersect requested filters with `AllowedOrganizationIds`, `AllowedStoreIds`, and `AllowedKioskIds`;
- for detail queries, fetch the row and return `403 Forbidden` when the caller does not share an allowed scope;
- keep public/runtime read paths separate from management read paths.

## Scope Model

Use `TenantScopeType` for configurable data that can exist as global defaults and tenant overrides:

- `Global`
- `Organization`
- `Store`
- `Kiosk`
- `Device`

Resolution priority:

```text
Device > Kiosk > Store > Organization > Global
```

When selecting effective configuration, query the most specific matching row first.

## Scoped Entities

### Product

`Product` supports global catalog definitions and tenant-specific overrides.

Use:

- `ScopeType`
- `OrganizationId`
- `StoreId`
- `KioskId`
- `TemplateProductId`

Recommended uniqueness:

- `ScopeType + OrganizationId + StoreId + KioskId + Code`

Global product templates should have all scope IDs null.

Management transport separates platform templates from tenant rows:

- `/management/product-templates/*` can address only `Global` products. `Manager` may read templates for cloning; only `SystemAdmin` may create or mutate them.
- `/management/organizations/{organizationId}/products/*` can address only products owned by that organization.
- `/management/organizations/{organizationId}/menus/*` can address only menus owned by that organization.
- `Menu` has no global authoring/runtime fallback in this API version; an effective menu must belong to the kiosk organization and may be narrowed to Store or Kiosk scope.
- tenant ownership (`OrganizationId`, `ScopeType`, `StoreId`, `KioskId`, and template lineage) is immutable after creation; moving ownership requires an explicit clone/promote use case rather than generic update.
- every product/menu mutation revalidates actor scope and route ownership in the Application handler. Route nesting is not the authorization boundary by itself.

### ProductVariant

`ProductVariant` belongs to a `Product` and represents a sellable/recipe-bearing variant such as size, portion, flavor, or package.

Use:

- `ProductId`
- `Code`
- `VariantType`
- `SizeCode` when the variant is size-based

Recommended uniqueness:

- `ProductId + Code`

Tenant ownership is inherited from the parent product. Do not duplicate tenant scope fields on `ProductVariant` unless variant overrides need independent scope later.

### Recipe

`Recipe` follows product variant scoping and can be overridden per tenant or kiosk.

Use:

- `ScopeType`
- `OrganizationId`
- `StoreId`
- `KioskId`
- `TemplateRecipeId`
- `ProductVariantId`
- `Version`

Recommended uniqueness:

- `ScopeType + OrganizationId + StoreId + KioskId + ProductVariantId + Version`
- or `ScopeType + OrganizationId + StoreId + KioskId + Code + Version`

### ProductOption

`OptionGroup` and `ProductOption` are owned by a Product. They inherit tenant scope and currency from that Product rather than declaring independent scope or currency.

- `Product -> OptionGroup -> ProductOption` is the authoring aggregate.
- Global product templates own their template groups/options. Cloning a Product creates new tenant-owned groups/options and records `TemplateProductOptionId` lineage on cloned options.
- Store/kiosk behavior follows the owning Product scope. Do not add independent option overrides unless a concrete pricing or availability use case requires them.
- A MenuItem chooses the subset of its Product's options that it offers.

### RobotProgram

`RobotProgram` already supports the full override hierarchy.

Use:

- `ScopeType`
- `OrganizationId`
- `StoreId`
- `KioskId`
- `DeviceId`
- `TemplateProgramId`

Robot program resolution should use:

```text
Device > Kiosk > Store > Organization > Global
```

Kiosk/device-scoped robot programs declare ordered robot artifacts. Motion points, calibration and other emulator internals remain outside Cloud configuration persistence.

### Configuration Release

`ConfigurationRelease` is organization-scoped approved configuration. Its release-owned `ExecutionRoute` and robot bindings resolve catalog recipe/variant requirements without exposing a direct robot-program mapping in Catalog.

Use:

- `OrganizationId`
- `StoreId`
- `KioskId`
- `DeviceId`
- `ProductVariantId`
- `RecipeId`
- `RobotProgramId`

Recommended uniqueness:

- `OrganizationId + StoreId + KioskId + DeviceId + RecipeId + Code`

This is the Cloud configuration/backup source that syncs to Edge as a runtime recipe-program binding. Do not put this relationship directly on `Product`, `ProductVariant`, `Recipe`, or `MenuItem`.

## Operational Entities

Operational rows should carry `OrganizationId` when they need direct tenant filtering/reporting without joining through `Kiosk`.

Already applied:

- `Order`
- `StockMovement`
- `Kiosk`
- `MaintenanceTicket`
- `NotificationDelivery`

The following operational evidence currently derives tenant ownership through
its persisted Order, Kiosk, Device, endpoint, or inbox relationship instead of
duplicating `OrganizationId`:

- `OrderExecutionRecord`
- `Alert`
- `OperationLog`
- `KioskHeartbeat`
- `DeviceEvent`
- `SyncEventInbox`
- `SyncDeadLetter`

Queries for these entities must start from a scoped owner or join through that
owner. Adding direct `OrganizationId` later is a denormalization decision for a
measured query/index need, not a prerequisite for tenant enforcement.

## Global Query Filter Guidance

For EF Core, apply tenant filters to entities implementing `IOrganizationScoped`.

Recommended behavior:

```text
Global/shared config:
OrganizationId == null

Tenant-owned data:
OrganizationId == currentOrganizationId

Effective config queries:
OrganizationId == null OR OrganizationId == currentOrganizationId
then order by scope specificity
```

Do not use global filters blindly for admin/platform queries. Platform admin screens need explicit bypass behavior.

## Ownership Boundary

Cloud/platform owns:

- global product templates
- global recipes
- global robot artifact templates
- payment methods
- device types
- roles

Organization owns:

- scoped products
- scoped recipes
- scoped robot programs
- stores
- kiosks
- orders
- stock movements
- operational reports

Kiosk/edge may create or update:

- kiosk/device-scoped robot programs and local Fairino point/frame snapshots
- robot jobs and steps
- ingredient dispenser state
- stock movements
- device/robot events
- sync inbox/dead letter records

When edge creates tenant-owned rows, it must include `OrganizationId` if known. If not known at the edge, cloud ingestion must enrich it from `KioskId` before storing/reporting.

Tenant tree is implemented as a management query/read model handler, not an entity CRUD service.

## Related Docs

- [Boundary Contexts](BOUNDARY_CONTEXTS.md)
- [Authorization Rules](../api/AUTHORIZATION_RULES.md)
- [Data Modeling Rules](../data/DATA_MODELING_RULES.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
````

## File: docs/flows/BACK_OFFICE_SETUP_FLOW.md
````markdown
# Back-Office Setup Flow

This document describes the setup flow that prepares a tenant, kiosk, users, catalog, and menu before a kiosk can sell.

## Search Keywords

`back-office setup flow`, `setup to sale`, `tenant setup`, `organization setup`, `store setup`, `kiosk setup`, `account invitation`, `RBAC scope`, `catalog setup`, `menu publishing`, `kiosk configuration`

## Flow

```text
1. SystemAdmin creates Organization.
2. SystemAdmin or scoped OrgAdmin creates Stores under the Organization.
3. SystemAdmin or scoped manager creates Kiosks under Stores.
4. SystemAdmin creates internal Accounts.
5. System generates invitation link.
6. User accepts invitation and becomes Active.
7. SystemAdmin assigns role scopes:
   - organization
   - store
   - kiosk
8. Catalog/Product manager configures:
   - products
   - variants
   - options
   - recipes
   - ingredients
9. Sales manager configures:
   - menus
   - menu items
   - prices
   - availability windows
10. Inventory topology is provisioned:
   - device model declares `IngredientDispenser`
   - device and container
   - active ingredient
   - capacity, unit, and level-to-quantity profile
   - kiosk topology read shows capable devices with and without configured containers
   - identity changes use rebind: retire old state, resolve its estimate explicitly, and create an audited replacement
   - replacing hardware transfers all mappings to an already-provisioned Device in the same kiosk, then retires the source Device
   - retiring a Device also retires its active dispenser states; stale Device/Ingredient references remain visible as warnings
11. Robot `.lua` artifacts and ordered robot programs are prepared.
12. Kiosk/edge configuration release is prepared for runtime deployment.
13. Inventory readiness compares required Recipe ingredients with the target kiosk topology before deployment.
```

## Rules

- Organization/Store/Kiosk hierarchy is tenant scope, not just UI navigation.
- Internal account onboarding uses invitation links; do not send admin-generated permanent passwords as the default flow.
- Role scopes decide which management data a user can read or manage.
- Menu sellability in Cloud is not the same as live machine readiness at Edge.
- Cloud manages immutable robot artifacts and ordered program manifests; it does not parse or control motion steps inside exported `.lua` files.

## Related Docs

- [System Flows](SYSTEM_FLOWS.md)
- [Identity Onboarding Rules](../api/IDENTITY_ONBOARDING_RULES.md)
- [Authorization Rules](../api/AUTHORIZATION_RULES.md)
- [Multi-Tenancy Rules](../architecture/MULTI_TENANCY_RULES.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Robot Lua Artifact Flow](ROBOT_LUA_ARTIFACT_FLOW.md)
````

## File: docs/flows/SYSTEM_FLOWS.md
````markdown
# System Flows

This document is the flow index for IceBot backend-facing workflows. Read the smallest flow file that matches the task instead of reading every flow.

Business/user-facing flows live in the `IceBot-Product` repository at `product/`.

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
````

## File: docs/process/DOCUMENTATION_RULES.md
````markdown
# Documentation Rules

This document defines how backend docs should be written so humans and RAG tools can find the right context without reading every file.

## Search Keywords

`documentation rules`, `docs standard`, `RAG-friendly docs`, `Search Keywords`, `lookup section`, `Related Docs`, `documentation structure`, `source of truth docs`, `routing hints`, `AI context`, `avoid duplicate docs`

## Purpose

Keep backend docs small, current, routed, and searchable.

Backend docs are operational source of truth. They prioritize information needed to implement, integrate, operate, or verify the behavior that runs now:

- current contracts, invariants, routes, payloads, states, and ownership boundaries
- commands and procedures a developer or operator must execute
- current failure, retry, security, and verification behavior

Keep concise rationale, examples, future constraints, and implementation guidance when readers need them to apply the contract correctly or avoid a known unsafe interpretation. Move extended discussion history, option comparison, rejected alternatives, and standalone decision records to the smallest owning `Vault/Decisions`, `Vault/Discussions`, or `Vault/Evolution` note. Backend docs may link to that note, but must remain understandable without reading Vault.

Do not document implementation work merely because it was performed. Add documentation only when readers need the resulting contract or procedure.

Each doc should answer one ownership question:

- what topic this file owns
- when to read it
- which terms should retrieve it
- where related but separate rules live

Do not duplicate full explanations across docs. Link to the owning doc instead.

Active backend documents must remain at or below 500 lines. When a document
approaches that limit, split it by ownership and retain a short index in the
original owner. A level-two or level-three section must remain at or below 120
lines so retrieval does not receive one oversized mixed-ownership chunk. Files
prefixed `HISTORICAL_`, `DEPRECATED_`, or `PROPOSAL_` belong in Vault rather
than `IceBot-Backend/docs`.

Use [Documentation Routing Map](../DOCUMENTATION_ROUTING_MAP.md) only when the right backend doc is unclear after direct retrieval, metadata filters, or path filters.

## Standard Shape

Use this shape for new backend docs:

```text
# Document Name

Short ownership statement.

## Search Keywords

`keyword`, `related term`, `route`, `entity`, `workflow`

## Purpose / Rules / Lookup / Flow

The actual content owned by the doc.

## Related Docs

- Other related doc name and path
```

For a contract, flow, or operational procedure that will be maintained across
multiple releases, include a short metadata table after `Search Keywords`:

```text
## Metadata

| Field | Value |
| --- | --- |
| Status | Current contract | Partial implementation | Proposal |
| Owner | Owning bounded context or operational module |
| Verification | Code path, contract test, smoke test, or manual check |
```

Do not claim that a document is fully implemented or verified unless the
listed evidence exists. Use `Partial implementation` when the document
describes a current boundary whose remaining behavior is intentionally
excluded. The [Documentation Coverage Matrix](../DOCUMENTATION_COVERAGE.md)
is the cross-module index; do not duplicate that matrix in every document.

`Search Keywords` should be near the top, but not inside the opening paragraph. This keeps overview chunks narrow while still giving RAG a clean keyword chunk.

## Search Keyword Rules

Include keywords that a team member or AI agent is likely to ask for:

- domain names: `Order`, `PaymentTransaction`, `RobotArtifact`, `EdgeCommand`
- route names: `/api/v1/authentication/login`, `/management/accounts`
- workflow phrases: `forgot password`, `edge command pull`, `payment callback`
- common synonyms: `auth`, `authentication`, `login`, `external login`
- policy names: `scoped RBAC`, `soft delete unique index`, `jsonb`

Avoid keyword dumping.

Do not include unrelated hot terms just to make a doc appear in more searches. That makes RAG worse.

## Lookup Sections

For route maps, policies, entities, or table groups, prefer compact lookup tables.

Good lookup sections:

- API surface by client/route.
- Bounded context by namespace/entity.
- Authorization policy by role.
- Local edge table by runtime group.
- JSON field by role.

These tables help humans scan and help RAG retrieve exact chunks.

## Link Rules

Links are routing hints, not required reading order.

Use related docs to point to ownership boundaries:

- API routes -> `API_SURFACE_RULES.md`
- authorization -> `AUTHORIZATION_RULES.md`
- bounded contexts -> `BOUNDARY_CONTEXTS.md`
- persistence/indexes -> `DATA_MODELING_RULES.md`
- JSON columns -> `JSON_FIELD_RULES.md`
- sync/idempotency/retry -> `IDEMPOTENCY_RETRY_RULES.md`
- tablet/edge/cloud contract -> `IOT_CONTRACT.md`
- deployment, diagnostics, observability, and smoke tests -> `operations/`

Do not copy full route maps, entity lists, or rules from the linked doc unless the current doc owns that rule.

## RAG-Friendly Writing

- Keep the first paragraph narrow.
- Put search terms in `Search Keywords`.
- Use precise section headings.
- Prefer tables for lookup data.
- Prefer specific lookup sections and metadata-friendly terms over generic overview prose.
- Keep extended decision history, rejected alternatives, and unrelated proposals out of backend source-of-truth docs.
- Describe the current rule directly. Keep “because”, examples, and future constraints when removing them would make the rule ambiguous or easier to misuse.
- Remove stale behavior when the implementation changes; do not preserve it as history in the contract document.
- Avoid duplicating the same section or rule. Keep one owner and link to it.

## Change Workflow

When backend behavior changes:

1. Update only the owning contract or procedure.
2. Remove superseded behavior from backend docs.
3. Keep locally necessary rationale with the contract; record broader decision history and trade-offs in Vault when worth preserving.
4. Check headings, links, duplicated rules, stale future language, and `git diff --check`.

When a code module gains a public contract, operational procedure, or
integration test family, update the matching row in the
[Documentation Coverage Matrix](../DOCUMENTATION_COVERAGE.md). The matrix is
an ownership and discovery index, not a replacement for the owning contract.

For a documentation cleanup or restructure, preserve the current uncommitted version before editing and compare content coverage afterward. Intentional removals are allowed when the user explicitly requests cleanup, condensation, or deletion; report what was removed or moved.

## Retrieval Priority

RAG should use a lazy retrieval path:

1. Search specific source-of-truth docs with direct query terms and metadata filters.
2. Narrow by path, source type, document type, or lookup section when the query is ambiguous.
3. Use [Documentation Routing Map](../DOCUMENTATION_ROUTING_MAP.md) as a fallback router when the correct doc family is still unclear.
4. Use reranking selectively for hard queries, not as a mandatory fix for weak docs or broad queries.

## Related Docs

- [Working Protocol](WORKING_PROTOCOL.md)
- [Documentation Routing Map](../DOCUMENTATION_ROUTING_MAP.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Boundary Contexts](../architecture/BOUNDARY_CONTEXTS.md)
- [Naming Rules](NAMING_RULES.md)
````

## File: docs/process/WORKING_PROTOCOL.md
````markdown
# Working Protocol

This project is still in discovery and pre-deployment. The domain model, API shape, and integration boundaries are being refined while the team learns from real robot, payment, tablet, and edge behavior.

This document defines how to work during this phase. It is not a frozen engineering process.

## Search Keywords

`working protocol`, `workflow`, `question versus action`, `apply`, `inspect`, `review`, `refactor rules`, `change scope`, `documentation rules`, `verification`, `done criteria`, `pre-deployment`, `do not create migrations`, `build command`, `AI agent workflow`

## Current Phase

Default assumptions:

- Architecture is directional, not fully frozen.
- Domain concepts should be clear, but implementation details may still change.
- Prefer small, reversible changes over broad reshuffles.
- Pre-deployment API compatibility is not required unless explicitly requested.
- Do not create EF Core migrations unless explicitly requested.

## Question Versus Action

Treat these as inspection or design discussion by default:

- "kiem tra"
- "danh gia"
- "vi sao"
- "sao khong"
- "co dang ... khong"
- "can nhac"
- "huong sua"

For those requests:

- inspect the current code/docs first;
- separate facts, assumptions, and recommendations;
- do not edit files unless the user explicitly asks to apply.

Treat these as action requests:

- "ap dung"
- "sua"
- "them"
- "xoa"
- "refactor"
- "tao file"
- "cap nhat doc"

For action requests, implement the smallest change that matches the decision.

## Decision Ownership

The assistant should challenge weak assumptions and present trade-offs. The user decides which option to implement.

When a proposal has multiple reasonable options:

- explain the options briefly;
- recommend one;
- wait for a clear implementation request if the choice changes architecture, data model, public API, or workflow semantics.

Do not simply agree with a statement if there is a stronger or safer alternative.

## Change Scope

Before editing:

- read the current implementation;
- search usages with `rg`;
- assemble the smallest relevant context from the task/module instead of reading all docs or all code;
- identify whether the request is repair, reshape, or removal;
- avoid broad renames, namespace moves, or folder reshuffles unless requested.

During editing:

- keep changes scoped to the requested concept;
- preserve existing abstractions unless removal is explicit;
- update interface and implementation signatures together;
- update DI and docs when contracts change;
- do not add future-facing infrastructure without a current use case.

For a workflow that crosses layers, modules, persistence operations, jobs, or
external dependencies, follow [Vertical Slice Review](VERTICAL_SLICE_REVIEW.md).
Freeze scope and define lifecycle, tenancy, idempotency, concurrency,
transaction, I/O, retry, cleanup, and retention invariants before editing.

## Refactor Rules

Build success does not prove design fit.

Do not justify broad movement, deletion, or new abstraction only because the project compiles.

When refactoring copied code from older projects:

- keep mature pieces that match the current domain;
- remove or reshape legacy assumptions that do not match IceBot;
- do not hide assumptions;
- state which parts were kept, changed, or deferred.

## Working With Other AI Models

Other models such as Gemini can be used for implementation, audit, and checklist work, but their output must be reviewed against this repository's rules before being treated as accepted.

Rules learned from previous collaboration:

- Give Gemini explicit scope, stop conditions, and non-goals.
- Ask for an audit/plan first when the work touches architecture, validation, tenant isolation, lifecycle, retry, or persistence.
- For implementation tasks, state clearly:
  - no EF migrations unless explicitly requested;
  - preserve existing public routes unless explicitly requested;
  - do not add dynamic permission entities, generic repositories, or broad service layers unless the plan says so;
  - build after code changes;
  - report whether migrations were created.
- After Gemini finishes, review `git diff` and code behavior directly. Do not rely only on its summary.
- Do not infer who made a change from `git diff` alone. If package versions, broad file moves, or unrelated edits appear, identify the source before writing a model-specific failure note.
- Treat "build succeeded" as a compile check, not design approval.
- If Gemini claims "warning-free", verify the build output when warnings matter.
- If Gemini creates local audit/checklist files in `.project-memory`, keep them only while they are actively useful.
- Promote durable rules into `docs/`; promote reasoning, trade-offs, and deferred ideas into `../Vault/`; delete completed temporary notes from `.project-memory`.
- When Gemini proposes large foundation work, separate:
  - short-term tasks that can be done in hours/days;
  - long-term topics that need business use cases or integration details.
- Do not let another model implement deferred topics simply because they are architecturally valid.

## API And Contract Rules

Before first deployment:

- do not keep legacy compatibility fields unless requested;
- prefer clean contracts over backward compatibility;
- route names should be business-facing and understandable by the team;
- application use cases may be action-oriented;
- WebAPI routes should stay resource/business-oriented.

After public or deployed clients exist, compatibility decisions must be explicit.

Detailed API naming rules live in [Naming Rules](NAMING_RULES.md).

## Documentation Rules

Docs should reduce repeated reasoning, not duplicate long explanations.

Use the project documentation index first when the task spans multiple repos or document areas:

- [Product Documentation Index](../../../IceBot-Product/README.md)

Do not read all docs by default. Read the smallest relevant set, usually 1-3 files, then inspect code as needed.

Links are routing hints, not mandatory recursive reads. If a linked file was already read in the current task, do not reopen it unless the user asks, the file may have changed, or a specific section is needed.

Use:

- [Documentation Routing Map](../DOCUMENTATION_ROUTING_MAP.md) when the right backend doc is unclear.
- [Documentation Rules](DOCUMENTATION_RULES.md) for RAG-friendly document structure.
- [Architecture](../../ARCHITECTURE.md) for high-level architecture.
- [Boundary Contexts](../architecture/BOUNDARY_CONTEXTS.md) for domain ownership.
- [Dependency Rules](../architecture/DEPENDENCY_RULES.md) for layer boundaries.
- [Naming Rules](NAMING_RULES.md) for naming conventions.
- [Data Modeling Rules](../data/DATA_MODELING_RULES.md) for persistence and ERD checks.
- [System Flows](../flows/SYSTEM_FLOWS.md) for the flow index, then the matching flow-specific document.
- [IoT Contract](../iot/IOT_CONTRACT.md) for tablet-edge-cloud flow.

The project-level `Vault/` folder is a personal reasoning notebook, not implementation truth. Use it only as background context unless a decision has been promoted into `IceBot-Product/` or repository docs.

Do not load `Vault/` by default. Use it only when the user asks about reasoning history, trade-offs, rejected designs, unresolved ideas, or why a decision was considered.

When changing code that affects contracts, domain ownership, or data model rules, update the relevant doc.

Do not run build for documentation-only changes unless explicitly requested.

Do not run RAG ingest automatically after documentation-only changes unless explicitly requested. RAG ingest mutates the local vector database and can be slow on the current machine because embedding runs in small batches. Instead, report the manual command:

```powershell
cd ..\IceBot-Tools
python .\rag\commands\ingest_docs.py
python .\rag\commands\ingest_code.py
```

## Verification

For code changes, run:

```powershell
dotnet build IceBot.slnx
```

Unit tests run without external infrastructure:

```powershell
dotnet test tests\IceBot.UnitTests\IceBot.UnitTests.csproj
```

PostgreSQL and MinIO integration tests are opt-in and require Docker:

```powershell
$env:ICEBOT_RUN_INTEGRATION_TESTS='true'
dotnet test tests\IceBot.IntegrationTests\IceBot.IntegrationTests.csproj
```

Without `ICEBOT_RUN_INTEGRATION_TESTS=true`, integration tests are discovered and skipped without starting containers.

For documentation-only changes, no build is needed.

For EF Core:

- do not create migrations unless requested;
- prefer non-mutating design-time checks when possible;
- never update the database unless requested.

## Done Criteria

A change is done when:

- code compiles, unless the change is documentation-only;
- stale identifiers/usages were scanned;
- docs were updated if the decision changed architecture, contract, data model, or naming;
- any skipped verification is stated explicitly;
- remaining warnings or risks are reported.

For a substantial vertical slice, completion additionally requires failure-path
evidence for every applicable invariant and an independent review of the final
diff against the frozen scope. Build success and happy-path coverage alone are
not sufficient.

## Related Docs

- [Documentation Rules](DOCUMENTATION_RULES.md)
- [Documentation Routing Map](../DOCUMENTATION_ROUTING_MAP.md)
- [Architecture](../../ARCHITECTURE.md)
- [Boundary Contexts](../architecture/BOUNDARY_CONTEXTS.md)
- [Dependency Rules](../architecture/DEPENDENCY_RULES.md)
- [Naming Rules](NAMING_RULES.md)
- [Vertical Slice Review](VERTICAL_SLICE_REVIEW.md)
````

## File: docs/api/SIGNALR_REALTIME_CONTRACT.md
````markdown
# SignalR Realtime Contract

## Search Keywords

`SignalR`, `realtime`, `OrderHub`, `OperationsHub`, `ManagementDashboardHub`, `OrderStatusChanged`, `OrderItemFulfillmentChanged`, `PaymentStatusChanged`, `OrderExecutionObservationChanged`, `KioskStatusChanged`, `KioskOperationalStateChanged`, `ExecutionReadinessChanged`, `DeviceEventCreated`, `AlertChanged`, `MaintenanceTicketChanged`, `InventoryChanged`, `DashboardInvalidated`, `reconnect`, `hub group`

## Scope
SignalR is used exclusively for Cloud-to-Human UI realtime updates, UI deltas, and dashboard invalidation. It is **not** used for Cloud-to-Edge/Kiosk/Robot runtime integration or machine-to-machine command/event flows (which will be handled by MQTT/edge sync). SignalR does not send robot execution commands or device control commands.

## Hub Routes
All SignalR connections are hosted under the following routes:
- `/hubs/orders`: Realtime updates for customer ordering and checkout flows.
- `/hubs/operations`: Realtime updates for staff operations, kiosk status, maintenance, and inventory.
- `/hubs/management-dashboard`: Realtime invalidation triggers for the management dashboard.

## Authentication
SignalR connections require a valid JWT token. Pass the token as a Bearer token during connection setup. Authorization rules apply per hub and per group join method (e.g. you can only join an order group if you own the order, or a kiosk group if your account has access to the organization/store).

## Join Methods
Clients must explicitly join a group to receive targeted events.

### OrderHub
- `JoinOrder(Guid orderId)`: Listen for updates on a specific order.
- `LeaveOrder(Guid orderId)`: Stop listening to an order.

### OperationsHub
- `JoinKiosk(Guid kioskId)`: Listen for operations updates related to a specific kiosk.
- `LeaveKiosk(Guid kioskId)`: Stop listening to a specific kiosk.

### ManagementDashboardHub
- `JoinDashboard(string scope, Guid? organizationId, Guid? storeId)`: Listen for dashboard invalidation events. `scope` must be `system`, `organization`, or `store`.
- `LeaveDashboard(string scope, Guid? organizationId, Guid? storeId)`: Stop listening to the matching dashboard group.

## Events

### OrderHub Events
- `OrderStatusChanged`: Triggered when an order status changes (e.g., placed, preparing, completed).
- `OrderItemFulfillmentChanged`: Triggered after a manual, packaged, or machine-produced line commits a status transition. It carries order/item/kiosk identity, fulfillment type, old/new item status, quantity, and update time.
- `PaymentStatusChanged`: Triggered when payment succeeds or fails.
- `OrderExecutionObservationChanged`: Triggered when Cloud observation changes without claiming a physical order-state transition. The payload contains `ObservationStatus`, `CustomerExecutionStatus`, customer message, support flag, executor evidence time, and last Cloud receive time. Clients should apply it directly to the current order screen; timeout authority is the Cloud receive time.

### OperationsHub Events
- `OrderItemFulfillmentChanged`: The same committed line delta is sent to `kiosk:{kioskId}` so the staff fulfillment workspace can update without waiting for an aggregate order transition.
- `MaintenanceTicketChanged`: Triggered when a maintenance ticket is created, updated, assigned, started, resolved, closed, or cancelled.
- `InventoryChanged`: Triggered when a dispenser is refilled or its stock estimate is adjusted.
- `KioskStatusChanged`: Triggered only after a committed lifecycle or connectivity transition. Management changes populate `oldLifecycleStatus/newLifecycleStatus`; heartbeat and timeout changes populate `oldConnectivity/newConnectivity`. Connectivity never mutates kiosk lifecycle. Duplicate heartbeat ingestion and unchanged projections do not emit this event.
- `KioskOperationalStateChanged`: Triggered after a committed operator or maintenance-driven operational transition. It carries old/new state, actor, reason, optional source maintenance ticket, and transition time. `EmergencyStopRequested` means Cloud intervention intent, not confirmed hardware state. The event is separate from lifecycle/connectivity and is the UI signal for refetching sales/dispatch admission.
- `ExecutionReadinessChanged`: Triggered after a newer typed readiness/capability projection commits. Payload carries endpoint, revision, readiness, activity, and safety; clients refresh detailed capability state from the read model when needed.
- `DeviceEventCreated`: Triggered once after a new warning/error device event commits. Idempotent event retries do not publish it again.
- `AlertChanged`: Triggered after an actionable alert is created, acknowledged, or resolved. It carries a committed state delta; idempotent lifecycle retries do not publish it again.

### ManagementDashboardHub Events
- `DashboardInvalidated`: Triggered when any significant state changes that requires the management dashboard to refresh its aggregated data. Item fulfillment changes use reason `OrderItemFulfillmentChanged`.

## Client Workflow

1. **Load Initial State**: Fetch the initial state using REST/GraphQL APIs.
2. **Connect**: Connect to the appropriate SignalR hub with your JWT.
3. **Join Group**: Call the relevant join method (e.g., `JoinOrder` or `JoinKiosk`).
4. **Apply Events**: Apply event payloads immediately when sufficient information is present.
5. **Refetch**: Refetch from REST/GraphQL on reconnect, refresh, or suspected version gap.

For execution silence, `Stale` maps to `Delayed`; `Unreachable` first maps to `PendingRecovery` and later escalates to `SupportRequired` after the configured threshold. This event does not imply that `Order.Status` changed or that the machine physically failed.

## Reconnect Rule
If the SignalR connection drops, the client should attempt to reconnect. Upon successful reconnection, the client **must** refetch the current state via REST/GraphQL to ensure no events were missed during the downtime. SignalR events are fire-and-forget and do not support durable event history.

## Boundary
SignalR is UI realtime only, not a robot runtime or MQTT bus. Do not use SignalR hubs to send commands to the kiosk hardware or robot.

## Related Docs
- [API Surface Rules](API_SURFACE_RULES.md)
````

## File: docs/flows/PRODUCTION_PACKAGE_INSTALLATION_FLOW.md
````markdown
# Production Package Installation Flow

This document owns package publication, installation preview, materialization, workspace repair, and the handoff into release publication and deployment.

## Search Keywords

`production package`, `package installation`, `franchise setup`, `package manifest`, `artifact materialization`, `installation workspace`, `package-managed`, `fork installation`, `repair installation`

## Status

The code model and API surface are implemented. Each environment must apply the
current EF Core migration chain before using this feature.

## Boundary

`ProductionPackages` hides the technical binding graph from the normal
franchise workflow. It does not replace Catalog, RobotConfiguration,
ProductionConfiguration, deployment, or Edge execution.

```text
Platform authoring
-> global Product template and published Recipe versions
-> published RobotArtifactTemplate
-> published RobotArtifactTechnicalContract
-> ProductionPackage Draft/version
-> immutable package manifest

Organization installation
-> copy Product/Recipe/options into organization scope
-> reuse an exact compatible organization RobotArtifact when already materialized
-> otherwise copy the immutable Lua object and create the organization RobotArtifact
-> validate declared effects and fixed quantities
-> deterministically create one RobotProgram per route and its RunOrder
-> create ConfigurationRelease Draft and routes
-> preserve installation/materialization provenance
```

## Technical Contract

Lua remains an executable artifact. Backend does not parse Lua to infer its
physical behavior. A versioned `RobotArtifactTechnicalContract` declares:

- effects;
- fixed or parameterized quantity mode;
- ingredient/option codes;
- runtime target and machine model;
- capabilities;
- ordering constraints.

`Parameterized` quantity is rejected while the Edge/Fairino runtime contract
does not support it. `FixedInArtifact` effects must match Recipe/option quantity
and unit during composition.

An option-specific Lua file remains static. Its program membership carries
`RequiredOptionCode`; Edge executes that ordered artifact only when the order
line contains the matching selected option. This supports optional topping
artifacts without pretending that Lua accepts runtime quantity parameters.
Option codes must be unique across one packaged Product, and composition
matches option ingredient requirements using the exact option code in addition
to ingredient, quantity, and unit.

Product options explicitly declare their execution boundary:

- `CommercialOnly` affects customer choice or price and is excluded from robot
  composition.
- `ProductionAffecting` changes physical production and must resolve to an
  ingredient requirement or an option-specific artifact effect.

The backend rejects commercial-only options with physical effects and rejects
production-affecting options that have no deterministic production input. This
classification is part of package product snapshot schema V2.

The reader still accepts immutable V1 package snapshots. For V1 only, option
impact is resolved from both ingredient requirements and option codes declared
by the package's artifact technical contracts. New and replaced package
definitions are always written as V2; V1 snapshots are not rewritten.

Package V1 accepts exactly one required capability code per route because one
route currently materializes one RobotProgram binding. Multi-workcell package
routes require a later binding contract instead of silently choosing the first
capability.

One package version may include each source Product only once. Different
`SourceKey` values cannot alias the same `SourceProductId`; V1 rejects that
definition instead of duplicating snapshot identities. Legacy option-effect
resolution is scoped through the owning Product's routes and program
blueprints, so identical option codes in different Products do not affect each
other.

Each route binds one exact source tuple:

```text
ProductSourceKey
+ ProductVariantSourceKey
+ RecipeSourceKey
+ ProgramBlueprintCode
+ SupportedOptionCodes
```

Recipe codes may repeat across variants. `SupportedOptionCodes` contains only
production-affecting options executable by that route; commercial-only options
remain Catalog behavior and an empty list means the route supports no physical
option adjustment. Validation and installation use this route policy rather
than applying every Product option to every RobotProgram.

Fairino-Studio exports a sibling `.icebot.json` sidecar for each `.lua`. The
sidecar is authoring input and must still be reviewed and published as a
technical contract. It is not executable and is not a certification.
Re-importing the same organization, contract code, and version replaces an
existing Draft definition. Published and Retired versions remain immutable and
require a new contract version.

## API

Platform package authoring:

```text
GET   /api/v1/management/production-packages
GET   /api/v1/management/production-packages/{packageId}
POST  /api/v1/management/production-packages
PUT   /api/v1/management/production-packages/{packageId}
PATCH /api/v1/management/production-packages/{packageId}/retire
POST  /api/v1/management/production-packages/{packageId}/versions
GET   /api/v1/management/production-packages/{packageId}/versions/{versionId}/definition
PUT   /api/v1/management/production-packages/{packageId}/versions/{versionId}/definition
PATCH /api/v1/management/production-packages/{packageId}/versions/{versionId}/publish
PATCH /api/v1/management/production-packages/{packageId}/versions/{versionId}/retire
```

Organization package workflow:

```text
GET  /api/v1/management/organizations/{organizationId}/production-packages/catalog
POST /api/v1/management/organizations/{organizationId}/production-package-installations/preview
POST /api/v1/management/organizations/{organizationId}/production-package-installations
GET  /api/v1/management/organizations/{organizationId}/production-package-installations
GET  /api/v1/management/organizations/{organizationId}/production-package-installations/{installationId}
GET  /api/v1/management/organizations/{organizationId}/production-package-installations/{installationId}/workspace
POST /api/v1/management/organizations/{organizationId}/production-package-installations/{installationId}/retry
POST /api/v1/management/organizations/{organizationId}/production-package-installations/{installationId}/fork
POST /api/v1/management/organizations/{organizationId}/production-package-installations/{installationId}/repair
POST /api/v1/management/organizations/{organizationId}/production-package-installations/{installationId}/upgrades/preview
POST /api/v1/management/organizations/{organizationId}/production-package-installations/{installationId}/upgrades
GET  /api/v1/management/organizations/{organizationId}/production-package-installations/{installationId}/upgrades
GET  /api/v1/management/organizations/{organizationId}/production-package-installations/{installationId}/upgrades/{upgradeId}
POST /api/v1/management/organizations/{organizationId}/production-package-installations/{installationId}/upgrades/{upgradeId}/cutover
POST /api/v1/management/organizations/{organizationId}/production-package-installations/{installationId}/upgrades/{upgradeId}/rollback
```

## Workspace, Ownership, And Repair

The workspace endpoint is the normal single-screen read model. It aggregates
materialized Products, Variants, Options, Recipes, applicable organization/store/kiosk
Menus and their assigned variants, Artifacts, ordered Programs, the Draft/Published release,
execution-endpoint readiness,
latest deployment state, separate commercial/technical blockers, and structured
required/optional/recovery action codes. FE invokes
the existing resource-specific command endpoints for those actions; workspace
does not bypass their authorization, validation, or audit boundaries.
Action context supplies the nested parent IDs required by those APIs. Deployment
actions also identify the compatible execution endpoint/profile and the complete
route/program selections required by a Low-cost deployment.
Unavailable options are optional offerings unless they leave an active required
group below `MinSelections`; that condition blocks commercial readiness and its
enable action becomes required. Failed installations keep
their selected-product snapshot and can be retried through the installation
retry endpoint without reconstructing the original request in FE.
For a partial installation, backend materializes only the dependency closure of
the selected products: their routes, referenced program blueprints, and the
artifact slots used by those blueprints. Unselected package artifacts are not
copied, claimed, or allowed to create unrelated organization conflicts.

Each Menu reports assigned and currently sellable ProductVariant IDs separately.
For an unassigned variant, `AssignVariantToMenu` returns eligible Menu IDs as
candidates. If no applicable Menu exists, the workspace returns `CreateMenu`
and blocks assignment with `MenuMissing`; FE does not infer a tenant or kiosk
owner from an unscoped Menu lookup. An existing Draft, Paused, future, or expired
assignment returns `ActivateMenu`, `ReviewMenuAvailability`, `ActivateMenuItem`,
or `ReviewMenuItemAvailability` instead of another assignment action.

Installed resources remain package-managed until the installation is forked.
An exact organization artifact is reused only when it is already managed by an
installed package and its object still passes size/checksum validation. A
manually authored artifact with the same code and checksum is a natural-identity
conflict; installation does not silently take ownership of it.
Publishing the generated Draft Recipe, Artifact, Program, and Release is part
of the normal installation workflow. Commercial organization settings such as
name, price, menu placement, and availability remain organization-managed.
Definition-changing or destructive technical operations on package-managed
Products, ProductVariants, Recipes, ProductOptions, Artifacts, Programs, and
Releases return conflict until the installation is forked. This includes
deleting materialized Products/Variants/Options, changing fulfillment type,
changing Product/ProductVariant technical codes or classification, adding Variants,
Recipes, OptionGroups, or Options to a package-managed Product graph, changing
OptionGroup selection requirements, Recipe definition or ingredients, option code/execution impact/ingredient
requirements, artifact technical contract, program manifest, or release routes,
and retiring/discarding technical resources. Publishing generated Draft
resources, Product/Variant/Option availability, and commercial display/price
fields remain part of the normal organization workflow.

When definition-changing technical recovery is required while ownership is
`PackageManaged`, the workspace returns `ForkInstallation`; downstream replacement/recovery actions
are blocked with `PackageForkRequired` until that fork completes.
Fork uses copy-on-write for RobotArtifacts that are still referenced by another
package-managed installation when this installation's referencing programs are
still Draft. It copies the immutable object under a new organization artifact
identity, retargets those Draft programs and materialization evidence, and then
changes ownership to `OrganizationFork`. Published program manifests remain
immutable; their shared artifact dependencies are not rewritten and subsequent
customization must create a new Draft program/release. Unshared resources retain
their existing identities.

If a package materialization target was soft-deleted, the workspace returns
`RepairMaterializations`. Repair restores the original row and identity in place;
it does not rewrite `TargetKey`, reinstall the package, or create duplicate
Products, Artifacts, Programs, or Releases. The operation is atomic and idempotent.
It is available only for an Installed, package-managed installation.

Automatic repair is rejected when a target was physically deleted, belongs to a
different tenant/scope, has an unsupported target identity, or restoring it would
violate a database constraint. Loss of the materialization evidence itself also
requires operator/support recovery. These cases are not reconstructed from the
current package definition because doing so could silently change an installed
historical graph. Workspace and repair compare evidence against the same expected
set derived from the immutable package version and installation product selection.
Recipe evidence is variant-qualified, and release evidence must point to the exact
`DraftConfigurationReleaseId`. A `409` response exposes affected resources in
`details.issues`.

### Technical Contract Authoring

Technical-contract authoring:

```text
GET/POST /api/v1/management/robot-artifact-template-contracts
GET/PUT/DELETE /api/v1/management/robot-artifact-template-contracts/{id}
POST     /api/v1/management/robot-artifact-template-contracts/{id}/validation-preview
PATCH    /api/v1/management/robot-artifact-template-contracts/{id}/publish|retire
GET/POST /api/v1/management/organizations/{organizationId}/robot-artifact-technical-contracts
GET/PUT/DELETE /api/v1/management/organizations/{organizationId}/robot-artifact-technical-contracts/{id}
POST     /api/v1/management/organizations/{organizationId}/robot-artifact-technical-contracts/import-sidecars
POST     /api/v1/management/organizations/{organizationId}/robot-artifact-technical-contracts/{id}/validation-preview
PATCH    /api/v1/management/organizations/{organizationId}/robot-artifact-technical-contracts/{id}/publish|retire
PUT      /api/v1/management/robot-artifact-templates/{templateId}/technical-contract
PUT      /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/technical-contract
```

Artifact and template publication requires the assigned contract to remain
Published and checksum/target compatible. Publication also verifies the Lua
object size and SHA-256 before changing lifecycle state.

### Idempotency And Artifact Reuse

Installation uses the `Idempotency-Key` header. A retry with the same package,
scope, and manifest returns the existing installation. Reusing the key for a
different payload returns conflict.

Artifact materialization is organization-scoped. Installing the same immutable
package artifact for another Store/Kiosk reuses the existing RobotArtifact when
template lineage, checksum, technical contract, runtime target, machine model,
and content length match. A Retired or incompatible existing identity returns
conflict. Installation serializes template, contract, artifact identity, and
program identity mutations with direct RobotConfiguration authoring; if the
observed artifact identity set changes while waiting, the Failed installation
must be retried instead of silently binding a different resource.

Definition replacement, package publication, installation preview, and actual
installation run the same deterministic validation. An invalid Recipe source,
effect, fixed quantity, option identity, ordering graph, target, or capability
cannot be published as an installable package version.

## Package Version Upgrade

Package upgrade has an independent lifecycle and is owned by [Production Package Upgrade Flow](PRODUCTION_PACKAGE_UPGRADE_FLOW.md). Installation exposes the owning installation and upgrade entry routes, but does not define upgrade materialization, cutover, rollback, or reconciliation behavior.

## Publication And Deployment

Release publication builds one immutable production definition per route. Its
checksum covers Recipe quantities, supported options, option ingredient
requirements, program order, artifact checksums, technical-contract checksums,
capabilities, and package provenance.

Deployment now requires a validation preview:

```text
POST /api/v1/management/kiosks/{kioskId}/configuration-deployments/preview
```

The deploy request echoes the selected endpoint's `deploymentChecksum` as
`deploymentPreviewChecksum` and sets
`acknowledgeRemainingRisk`. This is organization self-acknowledgement, not
third-party certification. Objective failures such as missing effects, wrong
machine target, checksum mismatch, or invalid order cannot be bypassed.

## Frontend Boundary

Normal package installation does not ask FE for artifact IDs, RunOrder,
parameters JSON, route priority, capability codes, program IDs, manifests,
checksums, storage keys, or schema versions. Existing technical APIs remain for
advanced self-authoring workflows.

## Related Docs

- [Production Package Upgrade Flow](PRODUCTION_PACKAGE_UPGRADE_FLOW.md)
- [Robot Lua Artifact Flow](ROBOT_LUA_ARTIFACT_FLOW.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Management API Surface](../api/MANAGEMENT_API_SURFACE.md)
- [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
````

## File: docs/iot/EDGE_SYNC_TELEMETRY_CONTRACT.md
````markdown
# Edge Sync and Telemetry Contract

This document owns Edge-to-Cloud telemetry, production-history replay, state-summary recovery, heartbeat, and readiness/capability projection contracts.

## Search Keywords

`device event`, `telemetry replay`, `production sync`, `checkpoint`, `state summary`, `heartbeat`, `readiness`, `capability projection`, `SyncEventInbox`, `ExecutionReadinessChanged`

## Transport

Typed MQTT uplink is the primary realtime transport for heartbeat, telemetry
replay, readiness, production events, and state summaries:

```text
icebot/execution-endpoints/{endpointId}/uplink/{messageType}
```

The JSON shown in the HTTPS sections below is the `payload` inside the common
MQTT envelope. HTTPS endpoints remain supported for bootstrap, broker outage,
manual diagnostics, and recovery. Both transports invoke the same handlers and
therefore share tenancy validation, idempotency identities, transaction
boundaries, and result semantics. Edge must not allocate a new event ID,
sequence, or revision merely because it changes transport.

The common envelope, application result topic, retry rules, and ACL boundary are
defined in [MQTT Operations](../operations/MQTT_OPERATIONS.md).

### Device Warning/Error Evidence

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/device-events
```

This single-event endpoint accepts authenticated `Warning`, `Error`, or `Critical` evidence for a device attached to the reporting kiosk. `originNodeId` must match the execution endpoint profile identity. `eventId` is globally unique and acts as the idempotency key; a retry returns the existing event and does not publish SignalR again. `occurredAt` uses the Edge telemetry future-skew limit. Optional structured payload is limited to 16384 characters, stored as evidence, and excluded from the normal management read API. After commit, Cloud publishes `DeviceEventCreated` to the kiosk operations group. A newly accepted `Error` or `Critical` event creates an Open Alert in the same transaction and publishes `AlertChanged`; Warning remains evidence only.

### Telemetry Replay

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/telemetry-events
```

`/telemetry-events` is the authenticated replay surface for heartbeat evidence,
device warning/error evidence, and local operation logs. It does not accept
production history.

Request shape:

```json
{
  "originNodeId": "uuid-bound-to-execution-endpoint",
  "events": [
    {
      "eventId": "uuid-envelope-id",
      "eventType": "Heartbeat",
      "heartbeat": {
        "heartbeatSequence": 41,
        "reportedAt": "2026-07-01T10:00:00Z",
        "status": "Online",
        "pendingSyncEventCount": 2
      }
    },
    {
      "eventId": "uuid-device-event-id",
      "eventType": "DeviceEvent",
      "deviceEvent": {
        "deviceId": "uuid",
        "eventType": "MotorOverheat",
        "severity": "Error",
        "message": "Motor exceeded temperature threshold.",
        "occurredAt": "2026-07-01T09:59:30Z",
        "payload": { "temperatureC": 85 }
      }
    },
    {
      "eventId": "uuid-local-log-source-id",
      "eventType": "LocalLog",
      "localLog": {
        "deviceId": "uuid",
        "action": "RuntimeRestarted",
        "category": "EdgeRuntime",
        "severity": "Info",
        "message": "Runtime restarted after local power interruption.",
        "occurredAt": "2026-07-01T09:58:00Z"
      }
    }
  ]
}
```

Rules:

- The batch contains 1 to `EdgeTelemetryIngestion__MaxBatchEventCount` items; default maximum is 100.
- `eventId` values must be non-empty and unique inside one request.
- Exactly one typed payload must match each `eventType`.
- Each item is atomic and independent. Valid items commit even when another item is rejected.
- A fully accepted/duplicate batch returns `200`; a valid envelope with one or more rejected/failed items returns `207 Multi-Status` with per-item status.
- Per-item statuses are `Accepted`, `Duplicate`, `Rejected`, or `Failed`.
- A successful telemetry item records a processed `SyncEventInbox` receipt. Destination tables remain the data source: `KioskHeartbeat`, `DeviceEvent`, or `OperationLog`.
- Heartbeats retain `(kioskId, originNodeId, heartbeatSequence)` destination idempotency. Device events and local logs use the envelope `eventId` as their source identity.
- If processing commits but receipt recording is interrupted, retry reaches destination dedup and then records the missing receipt.
- Retrying an existing processed `eventId` returns `Duplicate` without replaying side effects or SignalR notifications.
- Batch device events retain the same Alert rule: newly accepted Error/Critical evidence creates one Open Alert; Warning does not.
- Local operation logs may reference a device or order only when it belongs to the reporting kiosk. Raw payload remains bounded to 16384 characters.

### Production History Replay

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/production-sync/events
```

This endpoint accepts only durable production-history items. Each item carries
`eventId`, `sequenceNumber`, `eventType`, `schemaVersion`, `edgeCreatedAt`, and
optional order/command/job correlation plus payload. It returns item-level
results and the contiguous acknowledged sequence.

Production-history rules:

- `originNodeId` is the persistent executor identity bound to the authenticated endpoint: `FullEdgeRuntimeId` or `ControllerId`.
- `(originNodeId, eventId)` is the production-event idempotency identity. `sequenceNumber` is positive, monotonic, persistent across ordinary restart, and unique per origin.
- A ProductionEvent is stored directly in `SyncEventInbox` with its sequence. It does not create a second generic receipt.
- Cloud accepts an event received beyond a gap but advances `ProductionEventCheckpoint` only over committed contiguous sequences. The item result returns `acknowledgedSequenceNumber`; Edge retains and retries everything above it.
- Reusing an event id with another sequence, type, correlation identity, schema version, or payload returns conflict for that item. Reusing a sequence for another event id also returns conflict.
- An event at or below the acknowledged checkpoint is an idempotent duplicate even after its old detailed receipt has passed retention.

Checkpoint query:

```http
GET /api/v1/iot/execution-endpoints/{endpointId}/production-sync/checkpoint?sourceExecutorId={id}
```

The authenticated endpoint may query only its bound executor identity. A new stream returns sequence `0`. This endpoint is the reconnect resume cursor; timestamps are not ordering authority.

### Latest-State Summary Channel

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/production-sync/state-summaries
```

```json
{
  "sourceExecutorId": "uuid-bound-to-execution-endpoint",
  "summaries": [
    {
      "summaryKind": "CurrentExecution",
      "stateRevision": 18,
      "summarySchemaVersion": 1,
      "edgeCreatedAt": "2026-07-01T10:00:00Z",
      "payload": { "status": "Running", "sourceCommandId": "uuid" }
    }
  ]
}
```

`stateRevision` is positive and monotonic per `(sourceExecutorId, summaryKind)`. A newer revision replaces the current summary, an exact same revision is `Duplicate`, an older revision is `Stale`, and the same revision with different content is rejected as conflict. Summary ingestion is item-level and may return `207 Multi-Status`.

The summary channel is advisory current state used to recover visibility quickly after reconnect. It is not durable event history, does not create production events, and never advances `ProductionEventCheckpoint`.

For the same persistent `sourceExecutorId`, production sequence numbers, heartbeat sequence numbers, readiness revisions, and state-summary revisions must survive ordinary process and device restart. Resetting a counter causes new records to be treated as stale or conflicting. A genuinely reprovisioned runtime receives a new executor identity and starts new streams; V1 does not infer a reboot epoch from wall-clock timestamps.

### Heartbeat

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/heartbeat
```

Request:

```json
{
  "originNodeId": "uuid-bound-to-execution-endpoint",
  "heartbeatSequence": 123,
  "reportedAt": "2026-05-21T10:00:00Z",
  "status": "Online",
  "appVersion": "1.0.0",
  "firmwareVersion": "farino-x.y",
  "networkStatus": "Online",
  "robotStatus": "Ready",
  "cpuUsagePercent": 10,
  "memoryUsagePercent": 20,
  "diskUsagePercent": 30,
  "pendingSyncEventCount": 0
}
```

The request uses the same HTTPS execution-endpoint authentication as command pull and execution reports. `originNodeId` must equal the endpoint's bound `FullEdgeRuntimeId` or `ControllerId`. `(kioskId, originNodeId, heartbeatSequence)` is the idempotency key; retry returns the existing heartbeat. `reportedAt` cannot exceed `EdgeTelemetryIngestion__MaxFutureClockSkewSeconds` into the future. Cloud stores unique out-of-order heartbeat evidence, but only a heartbeat whose sequence is newer than the latest stored sequence may change current connectivity. `Kiosk.LastOnlineAt` advances only for a newer `Online` or `Degraded` heartbeat and uses Cloud receive time, never the Edge clock.

Connectivity state machine:

- A current heartbeat updates `KioskConnectivityProjection` to `Online`, `Degraded`, or `Unreachable` and never mutates `KioskStatus` lifecycle.
- A stale lower-sequence heartbeat is retained for history and returned with `stale=true`; it never rewinds connectivity or `LastOnlineAt`.
- The reconciliation job transitions a previously observed connectivity projection to `Unreachable` after `EdgeTelemetryIngestion__HeartbeatTimeoutSeconds` without an accepted heartbeat.
- Lifecycle management and connectivity observation are independent; an `Active` kiosk may be `Unreachable`, and a `Maintenance` kiosk may still report `Online`.
- Heartbeat ingest and timeout reconciliation use the same per-kiosk serialized boundary and recheck current state inside it.
- `KioskStatusChanged` is published only after a committed lifecycle or connectivity transition. Duplicate heartbeats and unchanged projections do not publish an event.

### Execution Readiness And Capability Projection

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/readiness
```

The authenticated execution endpoint publishes a complete observed snapshot:

```json
{
  "sourceExecutorId": "uuid",
  "stateRevision": 42,
  "executorReportedAt": "2026-07-01T12:00:00Z",
  "readiness": "Ready",
  "activity": "Idle",
  "safety": "Safe",
  "currentCommandId": null,
  "physicalOutputState": "No",
  "faultCode": null,
  "localPersistenceHealth": {
    "storageWritable": true,
    "freeSpaceBytes": 10737418240,
    "minimumRequiredFreeSpaceBytes": 1073741824,
    "localDatabaseHealth": "Healthy",
    "pendingEventCount": 0,
    "maximumPendingEventCount": 10000
  },
  "capabilities": [
    { "capabilityCode": "ICE_CREAM", "workcellCode": "CELL-A", "isAvailable": true }
  ]
}
```

`stateRevision` is positive and monotonic per source executor. Older revisions
are ignored, exact retries are duplicates, and reuse of one revision with
different content returns conflict. `capabilities` is a complete replacement,
not a patch. Cloud stores typed readiness and capability rows; it does not infer
availability from heartbeat strings or generic summary payloads.

`localPersistenceHealth` is required. `Healthy` database state, writable storage,
free space at or above the reported minimum, and event backlog at or below the
reported maximum are mandatory for command admission. Cloud derives the effective
projection defensively: if any check fails, it persists `NotReady` with one of
`LocalStorageNotWritable`, `InsufficientLocalStorage`, `LocalDatabaseUnhealthy`, or
`EventBacklogLimitExceeded`, even if Edge requested `Ready`. Negative values, a
non-positive threshold, or an unknown database-health value reject the snapshot.
Disk usage and pending-sync values in heartbeat remain operational history; they do
not replace this admission snapshot.

`KioskStatus` remains lifecycle/connectivity. Readiness controls machine
sellability and admission: online menu/order validation requires Ready + Safe
and every declared route capability available; command dispatch also requires
Idle. Busy is temporary executor occupancy, not kiosk Offline. SignalR emits
`ExecutionReadinessChanged` only after a newer projection commits.

Readiness is current-state evidence only for `EdgeTelemetryIngestion__ReadinessTimeoutSeconds` after Cloud receives it. Runtime menu, checkout, deployment preview, and production-package workspace ignore an older projection even when its last reported value was Ready/Safe. Executor wall-clock time is not used for this TTL.

Historical device events remain ingestible and deduplicated for audit. An Error/Critical event older than `EdgeTelemetryIngestion__AlertAutomationMaxEventAgeMinutes` at Cloud receive time does not create/correlate an operational Alert or send a critical push; replay must not masquerade as a new incident.


## Related Docs

- [IoT Contract](IOT_CONTRACT.md)
- [Observability](../operations/OBSERVABILITY.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
````

## File: docs/iot/TABLET_CLOUD_CONTRACT.md
````markdown
# Tablet and Cloud Contract

This document owns the API and state contracts between the tablet, Cloud checkout/payment services, and Cloud customer status projection.

## Search Keywords

`tablet`, `runtime menu`, `sales catalog`, `place order`, `payment session`, `payment callback`, `customer status`, `QR payment`, `order status`

## Tablet To Local Edge

### Get Runtime Menu Projection

```http
GET /api/v1/local/runtime-products?kioskId={kioskId}
```

Purpose: return the menu that can currently be sold from this kiosk.

Response:

```json
{
  "snapshotId": "uuid",
  "kioskId": "uuid",
  "generatedAt": "2026-05-21T10:00:00Z",
  "expiresAt": "2026-05-21T10:00:15Z",
  "runtimeStateTimestamp": "2026-05-21T09:59:59Z",
  "machineAvailable": true,
  "products": [
    {
      "productId": "uuid",
      "productVariantId": "uuid",
      "menuItemId": "uuid",
      "productCode": "VANILLA_CUP",
      "productVariantCode": "M",
      "displayName": "Vanilla Cup",
      "sizeCode": "M",
      "price": 25000,
      "currency": "VND",
      "available": true,
      "unavailableReason": null,
      "recipeId": "uuid",
      "recipeVersion": 3,
      "estimatedIngredientLevels": [
        {
          "ingredientId": "uuid",
          "ingredientCode": "VANILLA_MIX",
          "levelStatus": "Medium"
        }
      ]
    }
  ]
}
```

Projection inputs:

- Menu item snapshot.
- Product variant snapshot.
- Product snapshot.
- Recipe snapshot.
- `IngredientDispenserState`.
- Device state.
- Robot availability.
- Availability policy.

This response is a quote for UX, not a reservation.

## Tablet To Cloud

### Get Kiosk Sales Catalog Snapshot

```http
GET /api/v1/kiosks/{kioskId}/runtime-menu
```

Purpose: return the Cloud Sales Catalog snapshot that is currently sellable for a kiosk.

This endpoint is useful when the tablet needs a Cloud-backed menu snapshot, but it is not a replacement for the Local Edge runtime projection. It does not include live machine availability, ingredient sufficiency, robot status, or local queue state. Read-model boundaries and data exclusions for this endpoint are documented in [API Surface Rules](../api/API_SURFACE_RULES.md#read-model-api-boundaries).

Response:

```json
{
  "snapshotId": "uuid",
  "kioskId": "uuid",
  "generatedAt": "2026-05-21T10:00:00Z",
  "expiresAt": "2026-05-21T10:00:15Z",
  "availabilitySource": "CloudSalesCatalog",
  "containsMachineRuntimeState": false,
  "items": [
    {
      "menuId": "uuid",
      "menuItemId": "uuid",
      "productId": "uuid",
      "productVariantId": "uuid",
      "recipeId": "uuid",
      "menuItemCode": "VANILLA_CUP_M",
      "productCode": "VANILLA_CUP",
      "productVariantCode": "M",
      "displayName": "Vanilla Cup",
      "sizeCode": "M",
      "price": 25000,
      "discountAmount": 0,
      "finalPrice": 25000,
      "currency": "VND",
      "preparationTimeSeconds": 90,
      "imageUrl": null,
      "recipeVersion": 3
    }
  ]
}
```

Rules:

- Use this endpoint only for Cloud Sales Catalog truth.
- Cloud online sales require `KioskStatus.Active`, `KioskOperationalState.Operational`, active parent tenant scope, and connectivity `Online` or `Degraded`.
- Lifecycle, operational admission, and connectivity are separate contracts. `Online` does not imply `AcceptingOrders`; `Unreachable` is not a lifecycle or operational-state value.
- Offline-created order sync is not a current tablet capability. A future offline mode requires an explicit offline session and reconciliation contract; the normal order endpoint must not be treated as offline authority.
- For final runtime availability before checkout, the tablet should still prefer the Local Edge runtime projection when the edge service is available.
- `snapshotId` identifies the runtime-menu response for client cache/debug purposes. Order creation does not accept it as authority; Cloud always reloads the selected menu items and recalculates prices.

### Create Order

```http
POST /api/v1/orders
```

Headers:

```text
Idempotency-Key: create-order:{clientOrderId}
X-Correlation-Id: {correlationId}
```

Request:

```json
{
  "kioskId": "uuid",
  "clientOrderId": "tablet-order-uuid",
  "items": [
    {
      "clientLineId": "uuid",
      "menuItemId": "uuid",
      "quantity": 1,
      "selectedOptions": [
        {
          "productOptionId": "uuid"
        }
      ]
    }
  ],
  "clientTotalAmount": 25000
}
```

`selectedOptions` contains IDs returned by that runtime menu item. Each option may appear at most once in V1. Cloud validates required groups, single/multiple cardinality, availability, menu membership, currency, and price delta. Cloud stores immutable option snapshots and sends typed selected-option snapshots to Edge; arbitrary client JSON is never forwarded.

Response:

```json
{
  "orderId": "uuid",
  "orderAccessToken": "bearer-capability",
  "orderNumber": "ORD-20260521-0001",
  "customerStatus": "WaitingForPayment",
  "customerStatusMessage": "Waiting for payment. Please scan the QR code.",
  "canRetryPayment": true,
  "requiresStaffSupport": false,
  "totalAmount": 25000,
  "currency": "VND"
}
```

Cloud creates:

- `Order`
- `OrderItem`

Cloud must calculate price from backend Sales Catalog `MenuItem.Price`. Tablet totals are used only for comparison and conflict detection.

### Create Payment Session

```http
POST /api/v1/orders/{orderId}/payment-sessions
```

Headers:

```text
Idempotency-Key: payment-session:{orderId}
Order-Access-Token: {orderAccessToken}
X-Correlation-Id: {correlationId}
```

Request:

```json
{
  "paymentMethodCode": "payos",
  "expectedAmount": 25000,
  "expectedCurrency": "VND"
}
```

Response:

```json
{
  "orderId": "uuid",
  "paymentTransactionId": "uuid",
  "checkoutUrl": "https://provider-checkout-url",
  "qrCodePayload": "provider-qr-payload",
  "expiresAt": "2026-05-21T10:05:00Z"
}
```

Cloud creates:

- `PaymentTransaction`
- provider payment session

Do not create `RobotJob` at this stage.

## Provider To Cloud

### Payment Callback

Provider callback is provider-specific and should be handled by the Payments context.

Cloud must:

- Verify signature/provider authenticity.
- Deduplicate provider event by provider event id.
- Update `PaymentTransactionStatus`.
- Set `OrderStatus = ReadyForFulfillment` only after verified payment.
- Commit payment/order state before notifying Tablet or Edge.
- Emit a durable domain/application event after commit, such as `PaymentSucceeded` or `OrderReadyForFulfillment`.

Cloud must not:

- Block the provider webhook response while waiting for Edge acceptance.
- Let Tablet notification depend on Edge dispatch success.
- Create robot runtime state in the payment webhook transaction.

After commit, independent flows run:

```text
Paid order committed
  -> Tablet status notification
  -> ExecuteOrder dispatch attempt 1
  -> reconciliation of a missing initial command
```

The dispatch handler selects exactly one active execution endpoint whose observed active release or low-cost artifact set covers every machine-produced order line. It resolves each line to a release route and ordered robot-program bindings before creating the durable command. Zero matching endpoints defers dispatch; multiple matching endpoints are rejected as ambiguous rather than selected implicitly.

The command identity is `(OrderId, DispatchAttemptNo)`. Repeating the same attempt returns the existing command. The reconciliation worker creates only missing attempt `1`; it does not invent a new attempt after Edge rejection. Command expiry and the active-command admission limit are configured independently from delivery retries. Payment remains paid when dispatch fails because the provider-confirmed payment transaction has already committed.

## Cloud To Tablet Status

Tablet needs fast feedback after the customer pays. Cloud supports this through polling `GET /api/v1/orders/{orderId}` or `GET /api/v1/orders/{orderId}/payment-status` every 2-3 seconds. Both requests send `Order-Access-Token` received from order creation; payment-session creation and customer cancellation use the same header.

Raw order/payment state-machine enums are not serialized by the customer polling contracts. The tablet client consumes the following projected fields on `OrderResult` and `PaymentStatusResult`:

- `CustomerStatus` (string code)
- `CustomerStatusMessage` (client-facing fallback message; frontend may localize by `CustomerStatus`)
- `CanRetryPayment` (boolean indicator)
- `RequiresStaffSupport` (boolean indicator)

Tablet screen mapping based on projections (v1):

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


## Related Docs

- [IoT Contract](IOT_CONTRACT.md)
- [Checkout Execution Flow](../flows/CHECKOUT_EXECUTION_FLOW.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
````

## File: docs/operations/SIGNALR_SMOKE_TEST.md
````markdown
# SignalR Manual Smoke Test Workflow

## Search Keywords

`SignalR smoke test`, `OrderHub`, `OperationsHub`, `ManagementDashboardHub`, `JWT`, `JoinOrder`, `JoinKiosk`, `JoinDashboard`, `ExecutionReadinessChanged`

This guide provides steps to manually verify that the SignalR realtime surface is working correctly.

## 1. Start the Backend

Start the API backend with the local development configuration.

```powershell
# From the IceBot-Backend directory
dotnet run --project src/WebAPI/WebAPI.csproj --launch-profile "WebAPI"
```

## 2. Authenticate and Get JWT

Use a tool like Postman, curl, or a local frontend to authenticate as a user that has both ordering and operations permissions (or perform separately for a customer and an admin).

```bash
# Example authentication request (pseudo-code)
curl -X POST https://localhost:7001/api/v1/authentication/login \
  -H "Content-Type: application/json" \
  -d '{"emailOrUsername":"admin@example.com", "password":"password"}'
```

Extract the `token` from the response.

## 3. Test OrderHub

1. Connect to `https://localhost:7001/hubs/orders` using a SignalR client (like Postman's WebSocket/SignalR feature or a simple HTML/JS page).
2. Configure the connection to send the JWT as a Bearer token.
3. Call the `JoinOrder` method:
   - Target: `JoinOrder`
   - Arguments: `["00000000-0000-0000-0000-000000000000"]` (Replace with a valid Order ID)
4. Trigger an API that changes the order/payment state, such as placing an order, cancelling a pending order, or processing a payment webhook.
   - Expected: You should receive an `OrderStatusChanged` or `PaymentStatusChanged` event in your SignalR client.

## 4. Test OperationsHub

1. Connect to `https://localhost:7001/hubs/operations` with the JWT.
2. Call the `JoinKiosk` method:
   - Target: `JoinKiosk`
   - Arguments: `["00000000-0000-0000-0000-000000000000"]` (Replace with a valid Kiosk ID)
3. Trigger a kiosk lifecycle or connectivity change:
   - Example lifecycle: call `PATCH /api/v1/management/kiosks/{kioskId}/status` with `Disabled`, then restore it to `Active` after the check.
   - Example connectivity: ingest a newer authenticated heartbeat or run timeout reconciliation so status changes between `Online`, `Degraded`, and `Unreachable`.
   - Expected: You should receive a `KioskStatusChanged` event in your SignalR client.
   - Call `PATCH /api/v1/management/stores/{storeId}/kiosks/{kioskId}/operational-state` with a new state and reason.
   - Expected: You should receive a distinct `KioskOperationalStateChanged` event.
4. Trigger a maintenance ticket update:
   - Example: Call `POST /api/v1/management/maintenance-tickets` to create a ticket for that kiosk.
   - Expected: You should receive a `MaintenanceTicketChanged` event.
5. Trigger an inventory operation:
   - Example: Call `POST /api/v1/management/kiosks/{kioskId}/inventory/dispenser-states/{id}/refill`.
   - Expected: You should receive an `InventoryChanged` event.

## 5. Test ManagementDashboardHub

1. Connect to `https://localhost:7001/hubs/management-dashboard` with the JWT.
2. Call `JoinDashboard` with one of these argument sets:
   - System dashboard: `["system", null, null]`
   - Organization dashboard: `["organization", "organization-guid", null]`
   - Store dashboard: `["store", null, "store-guid"]`
3. Trigger an order, payment, kiosk, maintenance, or inventory change.
   - Expected: You should receive a `DashboardInvalidated` event.

## 6. Expected Event Names

- **OrderHub**: `OrderStatusChanged`, `OrderItemFulfillmentChanged`, `PaymentStatusChanged`
- **OperationsHub**: `OrderItemFulfillmentChanged` is sent to `kiosk:{kioskId}` for fulfillment-workspace refresh.
- **OperationsHub**: `KioskStatusChanged`, `KioskOperationalStateChanged`, `ExecutionReadinessChanged`, `DeviceEventCreated`, `AlertChanged`, `MaintenanceTicketChanged`, `InventoryChanged`
- **ManagementDashboardHub**: `DashboardInvalidated`

`DeviceEventCreated` is emitted for a newly committed device event. `AlertChanged` is also emitted when Error/Critical telemetry creates an actionable alert and when that alert is acknowledged or resolved.

## Related Docs

- [SignalR Realtime Contract](../api/SIGNALR_REALTIME_CONTRACT.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Observability](OBSERVABILITY.md)
````

## File: docs/README.md
````markdown
# Backend Docs

This folder contains backend source-of-truth documentation.

Use [Documentation Routing Map](DOCUMENTATION_ROUTING_MAP.md) only when the right doc is unclear. For direct work, start from the matching folder below.

## Folder Map

| Folder | Owns |
| --- | --- |
| `architecture/` | Backend architecture boundaries, bounded contexts, dependency rules, multi-tenancy |
| `api/` | API surfaces, authorization, identity onboarding |
| `data/` | Data modeling, JSON fields, idempotency, retry, indexes |
| `flows/` | Backend/system flows |
| `iot/` | Tablet, cloud, edge, local runtime contracts and ERD |
| `operations/` | Deployment config, observability, diagnostics, smoke tests |
| `process/` | Working protocol, documentation rules, naming rules, handoff checklists |

## Key Docs

| Need | Start With |
| --- | --- |
| Module documentation ownership and verification entry points | [Documentation Coverage Matrix](DOCUMENTATION_COVERAGE.md) |
| Route/API ownership | [API Surface Rules](api/API_SURFACE_RULES.md) |
| Internal management route catalog | [Management API Surface](api/MANAGEMENT_API_SURFACE.md) |
| Role policy and scoped RBAC | [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| Internal account onboarding | [Identity Onboarding Rules](api/IDENTITY_ONBOARDING_RULES.md) |
| Domain ownership | [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md) |
| Layer dependencies | [Dependency Rules](architecture/DEPENDENCY_RULES.md) |
| Tenant scope | [Multi-Tenancy Rules](architecture/MULTI_TENANCY_RULES.md) |
| EF/data modeling rules | [Data Modeling Rules](data/DATA_MODELING_RULES.md) |
| System flow index | [System Flows](flows/SYSTEM_FLOWS.md) |
| Checkout/payment/edge execution flow | [Checkout Execution Flow](flows/CHECKOUT_EXECUTION_FLOW.md) |
| Fairino Lua artifact/program deployment | [Robot Lua Artifact Flow](flows/ROBOT_LUA_ARTIFACT_FLOW.md) |
| Production Package installation | [Production Package Installation Flow](flows/PRODUCTION_PACKAGE_INSTALLATION_FLOW.md) |
| Production Package upgrade | [Production Package Upgrade Flow](flows/PRODUCTION_PACKAGE_UPGRADE_FLOW.md) |
| Tablet/edge/cloud contract | [IoT Contract](iot/IOT_CONTRACT.md) |
| Naming conventions | [Naming Rules](process/NAMING_RULES.md) |
| Deployment/runtime configuration | [Deployment Configuration](operations/DEPLOYMENT_CONFIG.md) |
| Robot artifact operational smoke | [Robot Artifact Operational Smoke Test](operations/ROBOT_ARTIFACT_OPERATIONAL_SMOKE.md) |
| Observability/logging/traces/metrics | [Observability](operations/OBSERVABILITY.md) |
| Manual critical rule checks | [Backend Critical Rule Checklist](process/BACKEND_CRITICAL_RULE_CHECKLIST.md) |

## Related

- [Architecture](../ARCHITECTURE.md)
- [Documentation Routing Map](DOCUMENTATION_ROUTING_MAP.md)
````

## File: AGENTS.md
````markdown
# AGENTS.md

Operational rules for coding agents working in this repository.

## Source Of Truth

- Architecture decisions: [ARCHITECTURE.md](ARCHITECTURE.md)
- Domain context map: [docs/architecture/BOUNDARY_CONTEXTS.md](docs/architecture/BOUNDARY_CONTEXTS.md)
- Dependency boundaries: [docs/architecture/DEPENDENCY_RULES.md](docs/architecture/DEPENDENCY_RULES.md)
- Documentation routing map: [docs/DOCUMENTATION_ROUTING_MAP.md](docs/DOCUMENTATION_ROUTING_MAP.md)
- Documentation structure: [docs/process/DOCUMENTATION_RULES.md](docs/process/DOCUMENTATION_RULES.md)

Do not duplicate long architecture explanations here. This file is for execution rules.

## Workspace Role

This is the main implementation repository for IceBot backend work.

- Use this file as the primary operational guide for backend/API/domain/database/auth/payment/order/sync tasks.
- Use `../IceBot-Tools` only as auxiliary tooling: RAG, MCP, code intelligence, docs checks, diagnostics, and local scripts.
- Use `../IceBot-Product` for shared product/business docs and frontend implementation contracts when the task needs cross-repo context.
- The workspace root `AGENTS.md` is only a router/fallback and does not override this file for backend work.

## Documentation Reading

- Treat links as routing hints, not mandatory recursive reads.
- Do not follow every link in every document.
- If a linked file was already read in the current task, do not reopen it unless the user asks, the file may have changed, or a specific section is needed.
- Prefer reading the smallest relevant set of docs, then inspect code.
- When the right backend doc is unclear after direct retrieval or metadata/path filters, use [docs/DOCUMENTATION_ROUTING_MAP.md](docs/DOCUMENTATION_ROUTING_MAP.md) as a fallback router.

## Working Workflow

- Use current task context first; do not reread docs/code when the answer is already settled in context.
- For concrete symbols/endpoints/handlers, use Code Intelligence before broad search.
- For rules/flows/contracts, use docs/RAG only when current context is insufficient.
- Before implementing a feature, define the complete vertical slice and its affected contracts so the work is not delivered as disconnected file-level patches.
- Make the smallest scoped change, then verify with the narrowest relevant check.
- After meaningful backend code/API/domain changes, run backend preflight as the final check.

## Change Guardrails

- Preserve existing API route contracts unless the user explicitly asks to change them.
- Do not keep backward-compatibility or legacy response/request fields unless the user explicitly asks for compatibility, especially before first deployment.
- Keep changes scoped to the requested work.
- Do not default to preserving the current design. Existing code is evidence, not proof that the boundary is correct.
- For meaningful changes, evaluate three options before choosing:
  - Patch: smallest fix; state whether it increases technical debt.
  - Refactor: preserve behavior but improve structure/boundaries.
  - Rewrite: remove a wrong abstraction or boundary and rebuild it.
- Do not convert a review, challenge, or "why did you do this?" question into code edits. Explain first; wait for an explicit action request before changing files.
- Treat phrases such as "vì sao", "sao không", "có đang ... không", "cân nhắc", "kiểm tra", and "đánh giá" as inspection/explanation by default, not permission to refactor.
- Do not justify broad movement, renaming, or added abstraction only because the build passes. Build success verifies compilation, not design fit.
- State assumptions explicitly when they affect structure, ownership, integration boundaries, or future extensibility. If an assumption is challenged, stop editing and correct the reasoning before proposing more changes.
- Do not hide or dilute a mistaken assumption. Name the wrong assumption, identify the affected files, and separate explanation from proposed remediation.
- When the user asks to inspect or adjust an existing abstraction, preserve it and repair it first. Do not delete it just because it is currently unused or copied from another project.
- Do not perform broad renames, namespace moves, or folder reshuffles unless requested.
- Do not remove files, abstractions, or extension points unless the user explicitly asks for removal, or the file is proven obsolete and the removal is stated as part of the intended fix before editing.
- Do not create EF Core migrations unless the user explicitly asks for migrations. When asked, review and stabilize the complete model change before generating a migration; do not generate successive migrations while the same model change is still being repaired.
- Do not use destructive git commands unless explicitly requested.
- Work with existing uncommitted changes; do not revert user changes.

## Documentation Preservation

- Treat documentation split, reorganize, move, or rename requests as lossless restructures. Do not summarize, condense, merge, omit, or delete content unless the user explicitly asks for that operation.
- Before restructuring a tracked document, inspect Git status/diff and preserve uncommitted content with a Git-visible checkpoint or exact copy.
- Compare headings and content coverage before removing the original; report intentional omissions and replacement decisions first.
- Write user-chosen architecture as Confirmed Decision. Keep agent proposals as Recommendation and unresolved choices as Open Design Choice.
- Do not turn a recommended entity name, split, migration, or ownership boundary into a final architecture decision without explicit user approval.
- Backend docs prioritize current implementation contracts and procedures needed to build, integrate, operate, or verify the system.
- Preserve concise rationale, examples, future constraints, and implementation guidance when they are necessary to apply a contract correctly. Route extended discussion history, option comparisons, and rejected designs to the smallest owning Vault note; route standalone migration direction to `Vault/Evolution`.
- Do not document a completed implementation step unless it changes a contract or creates an operation readers must perform.
- When the user explicitly requests documentation cleanup, remove stale, duplicate, historical, or non-operational content after preserving the pre-cleanup version. Do not retain obsolete text merely to make the restructure lossless.

## Decision Traceability

- When implementation depends on a user-chosen architecture, record the decision in its owning document before relying on a compressed summary elsewhere.
- A Confirmed Decision must state: the problem being solved, the chosen boundary, why it was chosen, rejected or excluded alternatives, and the entity/contract consequences. Recording only the final names or fields is insufficient.
- If the current documentation and task context do not establish one interpretation, ask the user. Do not infer the missing semantic rule from naming patterns, a previous proposal, or a superficially similar model.
- A request to inspect, check, or compare supplied material means compare it against current files and report gaps. Do not copy it into documentation unless the user separately asks to apply it.

## Domain And Application Rules

- Place new domain concepts in the owning bounded context.
- Keep context-specific enums inside that context.
- Put only genuinely shared primitives in `Domain.Common`.
- Prefer ids and snapshots across contexts instead of large navigation graphs.
- Do not introduce generic repository/service/controller layers for domain workflows.
- Use EF Core `DbContext` as the default unit of work; keep repositories thin when they exist.
- Repository abstractions should support handler-composed queries and focused persistence operations. They must not become CRUD service layers with hidden business rules.
- Keep actor concerns in WebAPI/auth, not in Application folder organization.

## Refactor Checklist

- Read the current implementation and search all usages before changing an abstraction.
- Identify whether the request is to repair, reshape, or remove. Default to repair/reshape when removal is not explicit.
- Before moving files, renaming public/internal concepts, or introducing a new abstraction, verify that the user asked for implementation rather than explanation. If not explicit, provide a short recommendation only.
- Prefer the smallest change that resolves the current mismatch. Do not add future-facing infrastructure until there is a present use case or the user asks for it.
- Keep class names, file names, namespaces, and `using` references consistent.
- Update interface and implementation signatures together.
- Update DI registrations when service contracts change.
- Scan for stale identifiers before finishing; prefer `rg`.
- Re-run build after code changes.

## Vertical Slice Completion Gate

Apply this gate before changing a workflow that crosses multiple modules,
aggregate boundaries, persistence operations, or external dependencies.
Use [Vertical Slice Review](docs/process/VERTICAL_SLICE_REVIEW.md) as the
worksheet, invariant matrix, failure-scenario catalog, and evidence standard.

1. Freeze the requested scope before editing. Record what is included, what is
   excluded, and which public contracts or data models may change. Do not expand
   the architecture during the final review pass.
2. Define the complete vertical-slice invariants before implementation. Check at
   least lifecycle/state transitions, concurrency, tenancy, idempotency,
   transaction boundaries, external I/O, compensation/cleanup, and retry
   behavior. Mark an item not applicable only with a concrete reason.
3. Write the relevant failure scenarios before editing code. Include concurrent
   requests, retries after partial success, stale state, external dependency
   failure, and cleanup after database or object-storage failure when applicable.
4. Implement the frozen slice in one coherent pass. Do not deliver a sequence of
   disconnected patches that each handles only the latest observed symptom.
5. After implementation, review the final diff independently against the frozen
   scope, invariants, and failure scenarios. This review is a verification pass,
   not permission to introduce new architecture or broaden the task.
6. Call the slice complete only when every applicable invariant has code or
   contract coverage and every material failure scenario has verification
   evidence. Build success alone is not completion evidence. If any item remains
   unverified, state it explicitly and do not report the slice as complete.

For narrow, local changes, use the smallest applicable subset of this gate. Do
not turn documentation-only work or isolated mechanical fixes into unnecessary
process overhead.

## Verification

Preferred final check in the full workspace after meaningful backend code/API/domain changes:

```powershell
cd ..\IceBot-Tools
python .\backend-preflight\commands\check_backend.py
```

Fallback/direct compile check:

```powershell
dotnet build IceBot.slnx
```

Use focused lookup or docs tools during investigation. Do not run backend preflight at the start of a task or for design-only discussion.

Do not run build for documentation-only changes unless the user explicitly asks for verification.

For EF model checks, prefer design-time commands that do not mutate the database unless the user asked for migration/database changes.

If a required tool is unavailable, report exactly what could not be verified.

## Bulk And Distributed Workflow Rules

- Transaction behavior must be explicit.
- If a bulk requirement says item-level atomicity, rollback only the failed item, commit successful items, and return partial-failure details.
- Public commands that can be retried need idempotency behavior.
- Payment callbacks, sync ingestion, robot events, and stock movements should use typed retry/idempotency fields rather than parsed JSON state.

## Ambiguity

Make reasonable assumptions when they are low risk and consistent with the existing codebase. Ask for clarification only when the choice would change public contracts, data model ownership, migrations, or workflow semantics.

## Request Triage

When a request prescribes a new API, field, entity, event, or file transport,
do not treat that technical shape as the requirement. First use
`../IceBot-Product/delivery/playbooks/REQUEST_TRIAGE.md` to
identify the operating goal, actor, scope, current flow, and any real contract
gap. Explain why the existing boundary does or does not satisfy the goal before
proposing implementation.

## Technical Challenge And Decision Ownership

- Do not simply agree with the user's architectural or design statement. Evaluate it against the current codebase, stated requirements, and likely future constraints.
- When the user's proposal is valid but not the only good option, present the trade-offs and at least one stronger or simpler alternative when one exists.
- Separate facts, assumptions, and recommendations. State which parts are proven from code and which parts are inferred.
- For design choices with meaningful trade-offs, provide options and a recommendation, then let the user decide which option to implement.
- Do not implement a debatable architectural choice immediately after discussing it unless the user explicitly asks to apply that specific option.
- If the user challenges a previous decision, reassess it on technical grounds instead of defending the prior change. Acknowledge mistakes directly, but still explain any parts of the previous approach that were technically reasonable.
````

## File: docs/data/IDEMPOTENCY_RETRY_RULES.md
````markdown
# Idempotency and Retry Rules

This document records domain naming and behavior rules for commands, events, sync, payment, and robot execution. Do not add all fields to every entity. Add them only at boundaries where duplicate requests, duplicate events, or retry after partial success can happen.

## Search Keywords

`idempotency`, `retry`, `IdempotencyKey`, `EventId`, `SourceEventId`, `CorrelationId`, `CausationId`, `ProcessingAttempts`, `NextRetryAt`, `LockedUntil`, `LockId`, `payment callback`, `provider callback`, `sync inbox`, `dead letter`, `robot job retry`, `stock movement`, `device event`, `heartbeat`, `append-only event`

## Naming Rules

Use `IdempotencyKey` for a client/API command that may be retried by the caller.

Examples:

- `Order.IdempotencyKey`
- `PaymentTransaction.IdempotencyKey`
- `Refund.IdempotencyKey`
- `EdgeCommand.Id`

Use `Client...Id` for IDs created by tablet/POS before the backend persists data.

Examples:

- `Order.ClientOrderId`
- `OrderItem.ClientLineId`

Use `Provider...Id` for external payment provider IDs.

Examples:

- `PaymentTransaction.ProviderTransactionId`
- `PaymentTransaction.PaymentIntentId`
- `PaymentCallback.ProviderEventId`
- `Refund.ProviderRefundId`

Use `EventId` when the entity itself is an event/message record.

Examples:

- `SyncEventInbox.EventId`
- executor report ids are deduplicated by endpoint/runtime identity and source event id before updating Cloud projections
- `DeviceEvent.EventId` if events are generated by device SDK/edge with a source ID

Use `SourceEventId` when the entity is a state or ledger record created from an upstream event.

Examples:

- `StockMovement.SourceEventId`
- `OperationLog.SourceEventId`

Use `CorrelationId` for tracing one business flow across services.

Use `CausationId` for the command/event that caused the current command/event.

Avoid using `RequestId` as a domain idempotency key. `RequestId` is usually transport/logging-level and can change between retries.

## Retry Field Names

Use these names for infrastructure/event processing retry:

- `ProcessingAttempts`
- `MaxProcessingAttempts`
- `NextRetryAt`
- `LastAttemptAt`
- `LastError`
- `LockId`
- `LockedUntil`

Use these names for business execution retry:

- `RetryCount`
- `MaxRetries`
- `NextRetryAt`
- `LastErrorCode`
- `LastErrorMessage`

Robot step execution should prefer business retry names because a step retry is part of the robot workflow, not only infrastructure message handling.

## Entities That Need Idempotency

### Order

Recommended fields:

- `IdempotencyKey`
- `ClientOrderId`
- `CorrelationId`

Recommended unique constraints:

- `KioskId + IdempotencyKey`
- `KioskId + ClientOrderId`
- `OrderNumber`

Reason: tablet/POS may retry order creation after timeout.

### OrderItem

Recommended fields:

- `ClientLineId`

Recommended unique constraint:

- `OrderId + ClientLineId`

Reason: tablet/POS may retry adding item lines.

### PaymentTransaction

Recommended fields:

- `IdempotencyKey`
- `ProviderTransactionId`
- `PaymentIntentId`
- `CorrelationId`

Recommended unique constraints:

- `OrderId + IdempotencyKey`
- `Provider + ProviderTransactionId`
- `PaymentIntentId`
- `TransactionNumber`

Reason: payment provider calls can timeout after successful charge.

### PaymentCallback

Recommended fields:

- `ProviderEventId`
- `ProcessingAttempts`
- `MaxProcessingAttempts`
- `NextRetryAt`
- `LastAttemptAt`
- `LastError`

Recommended unique constraint:

- `Provider + ProviderEventId`

Reason: payment providers commonly send duplicate webhooks.

Use a provider-guaranteed event id when one is present. If the provider does not supply one, persist a deterministic SHA-256 fingerprint of the raw signed payload as the deduplication key. Do not substitute an order code, payment-link id, or transaction reference because those can identify multiple legitimate state changes.

### Refund

Current phase uses manual cash refund. Treat auto provider refund and payout as future integration work unless explicitly requested.

Recommended fields:

- `IdempotencyKey`
- `ProviderRefundId`
- `CorrelationId`
- `RetryCount`
- `MaxRetries`
- `NextRetryAt`
- `LastErrorCode`
- `LastErrorMessage`

Recommended unique constraints:

- `PaymentTransactionId + IdempotencyKey`
- `ProviderRefundId`
- `RefundNumber`

Reason: duplicate refund is a high-risk financial bug.

### EdgeCommand And Executor Evidence

`EdgeCommand.Id` is the dispatch identity. A delivery retry keeps the same command id; a new approved execution retry creates a new command id and dispatch attempt number.

Cloud allocates delivery-attempt numbers inside a transaction serialized by the
target execution endpoint. Command ACK, execution report, and timeout reconciliation
serialize by `EdgeCommand.Id`; any of those paths that changes an order also acquires
the shared `OrderId` workflow lock. Manual/Packaged fulfillment and initial dispatch
use that same order lock. This lock ordering prevents duplicate provisional execution
records and lost mixed-order aggregation updates across backend instances.
Payment-session creation, signed payment application, payment reconciliation, and
order cancellation/refund-required decisions use the same order lock, so a payment
cannot be applied from a stale pre-cancellation order snapshot.

Recommended constraints:

- `EdgeCommand.OrderId + DispatchAttemptNo`
- `EdgeCommandDeliveryAttempt.EdgeCommandId + DeliveryAttemptNo`
- executor report identity scoped to its stable runtime/controller identity before Cloud projections are updated

Reason: delivery retry must not rerun physical work, and Cloud projections must not accept duplicated executor evidence.

### Production Package Upgrade

Upgrade execute uses `OrganizationId + IdempotencyKey`. The same key is valid
only for the same source installation, target package version, selected Product
source keys, and `PreviewChecksum`. A different payload returns conflict.

Only one active Upgrade may own a source installation. Its successor installation
uses a deterministic internal idempotency key and a persisted materialization
identity suffix, so retry cannot create different staging Product or RobotProgram
identities.

The Upgrade persists `TargetInstallationId` immediately after successor
materialization and before preparation evidence is finalized. A Failed Upgrade
may resume only with the original payload and reuses that installation. A
terminal retry returns the existing result even after the source installation
has been superseded; it does not rebuild preview from current state.

Rollback uses one deterministic idempotency key per Upgrade, execution endpoint,
and rollback attempt number. Successful endpoint requests are persisted
individually. Retrying a partially requested rollback sends only missing
endpoint requests, then waits for every resulting deployment to become Active
before restoring Cloud menu and Catalog bindings. An observed Failed deployment
permits the next audited attempt; unknown or non-terminal observation does not.
Each endpoint is limited to three attempts.

### DeviceEvent

Recommended fields:

- `EventId`
- `CorrelationId`
- `CausationId`

Recommended unique constraints:

- `OriginNodeId + EventId`
- or `DeviceId + EventType + OccurredAt` only if the SDK cannot provide event IDs

Reason: device/SDK events may be replayed by edge sync.

### StockMovement

Recommended fields:

- `SourceEventId`
- `CorrelationId`
- `CausationId`

Recommended unique constraints:

- `OriginNodeId + SourceEventId`
- or `ReferenceType + ReferenceId + MovementType`

Reason: duplicate movement corrupts ingredient balance. Do not retry by creating a new movement with a new ID. Retry using the same source event.

### SyncEventInbox

For `POST /api/v1/iot/execution-endpoints/{endpointId}/telemetry-events`, each successful heartbeat/device-event/local-log item records one processed inbox receipt keyed by envelope `eventId`. The typed destination is committed first and has its own dedup identity. This ordering deliberately permits safe repair: if destination commit succeeds but receipt recording is interrupted, the next retry observes the destination duplicate and records the missing receipt without repeating side effects. Production history uses `/production-sync/events` and its independent contiguous sequence checkpoint.

ProductionEvent is the exception to the two-stage destination/receipt pattern: its `SyncEventInbox` row is the durable history record. `(SourceNodeId, SequenceNumber)` is unique, and `ProductionEventCheckpoint` advances only across contiguous committed rows. Events beyond a gap may be retained, but the acknowledged sequence does not skip the gap.

For execution reports, `(SourceNodeId, EventId)` identifies one immutable report envelope. A retry is accepted as duplicate only when its command identity and normalized payload are identical. For production history, an existing event id must also match sequence, kiosk/job correlation, type, schema version, and payload. Reusing either identity with different content is a conflict, not an idempotent retry.

Latest-state summaries have a separate idempotency boundary: `(SourceExecutorId, SummaryKind, StateRevision)`. Only a newer revision mutates `EdgeStateSummary`; stale summaries are ignored. Summary revision must never update the production-event checkpoint.

Processed and ignored inbox receipts are retained for 180 days by default. This is the guaranteed Cloud transport-dedup window for raw batch replay; Edge must not expect an event replayed after that window to retain its old receipt. Some typed business destinations have stronger independent dedup rules, but callers must not assume that for every event type. Failed, processing, received, dead-lettered, or dead-letter-referenced inbox rows are not automatically purged.

Already has several required fields.

Recommended fields:

- `EventId`
- `ProcessingAttempts`
- `MaxProcessingAttempts`
- `NextRetryAt`
- `LastAttemptAt`
- `LastError`
- `LockId`
- `LockedUntil`

Recommended unique constraint:

- `SourceNodeId + EventId`

Reason: inbox is the primary deduplication boundary for edge-cloud sync.

### Sync Dead-Letter Recovery

Management endpoints:

```text
GET  /api/v1/management/sync-dead-letters
GET  /api/v1/management/sync-dead-letters/{id}
POST /api/v1/management/sync-dead-letters/{id}/retry
POST /api/v1/management/sync-dead-letters/{id}/resolve
POST /api/v1/management/sync-dead-letters/{id}/ignore
```

V1 is SystemAdmin-only. Every retry creates an immutable
`SyncDeadLetterRetryAttempt` containing actor, reason, attempt number, result,
and timestamps. Retry is allowed only when a registered typed replay contract
can reconstruct the command safely. V1 supports `ExecutionReport.*`; unknown or
receipt-only event types return `422` and remain Open. There is no generic JSON
reflection dispatcher.

Retry reuses the original source event identity and linked `SyncEventInbox`.
Success marks the inbox Processed and resolves the dead letter. Failure returns
the dead letter to Open and records the failed attempt. Resolve/ignore are
manual terminal decisions and require operator notes.

Current audit conclusion:

- `SyncEventInbox` already has the core infrastructure retry shape: processing attempts, max attempts, next retry, last attempt, lock id, locked until, and last error.
- Keep the `(Status, NextRetryAt, LockedUntil)` index for retry worker scans.
- Preserve `EventId` as the deduplication boundary for sync ingestion.
- Raw `PayloadJson` is acceptable as event evidence and transport payload. Business state that needs filtering or lifecycle rules should be projected into typed tables by processors.

### KioskHeartbeat

Recommended fields:

- `HeartbeatSequence`

Recommended unique constraint:

- `NodeId + ReportedAt`
- or `NodeId + HeartbeatSequence`

Reason: duplicate heartbeat is low risk, but dedupe keeps monitoring data clean.

Unique lower-sequence heartbeats may be stored as delayed evidence, but only the highest sequence for an origin may update current connectivity or `LastOnlineAt`. This prevents delayed delivery from rewinding the Cloud projection.

## Entities That Need Retry

Retry is recommended for:

- `SyncEventInbox`: retry event processing.
- `PaymentTransaction`: retry provider calls with the same provider idempotency key.
- `PaymentCallback`: retry internal callback processing.
- `Refund`: the current manual workflow retries only the backend command with the
  same idempotency key. It does not call or retry a provider refund. Provider
  refund retry requires a separately approved provider contract and workflow.
- `EdgeCommand`: retry delivery using the same command id while it remains unexpired.
- `DeviceEvent` and `OperationLog`: retry publish/sync, not the original physical action.

Usually do not retry old `KioskHeartbeat` records. Send the next heartbeat or a compact recent batch instead.

## Entities That Usually Do Not Need Idempotency Fields

These entities should rely on `Code`, `Version`, scope, or natural unique constraints instead of command idempotency fields:

- `Product`
- `ProductVariant`
- `ProductCategory`
- `ProductOption`
- `OptionGroup`
- `Recipe`
- `RecipeItem`
- `Ingredient`
- `DeviceType`
- `DeviceModel`
- `PaymentMethod`
- `Role`
- `Organization`
- `Store`
- `Kiosk`

Notes:

- They still need uniqueness rules such as `Code`, `ScopeType + Code`, `ProductId + VariantCode`, or `ProductVariantId + Version`.
- For artifact-first configuration, use immutable artifact checksum, `RobotProgramId + RunOrder`, release number and release route code constraints.
- They may need optimistic concurrency (`Version`) if edited from both cloud and edge.

## Artifact And Release Concurrency

- Robot artifact upload identity is `OrganizationId + normalized ArtifactCode + SHA-256 Checksum`, enforced by a unique database index.
- The pre-insert lookup is only an optimization. If concurrent uploads pass it together, the unique-index loser reloads and returns the committed winner as idempotent success, then deletes only its own known-uncommitted object-storage key.
- Ambiguous database failures are not treated as uniqueness conflicts and must not trigger immediate object deletion; orphan cleanup handles them after the grace period.
- Configuration release numbers are allocated inside a database transaction protected by a PostgreSQL transaction-scoped advisory lock derived from `OrganizationId`. This preserves `MAX + 1` numbering per organization without serializing unrelated organizations.
- The unique `OrganizationId + ReleaseNumber` index remains the final integrity boundary.
- `RobotArtifactOrphanCleanupJob` uses a PostgreSQL session advisory lock. At most one backend instance scans/deletes orphans per run; another instance skips when the lock is held. A crashed instance releases the lock when its database connection closes.

## External Dependency Resilience

HTTP integrations use `Microsoft.Extensions.Http.Resilience`, which integrates Polly resilience pipelines with `IHttpClientFactory`. Do not construct ad hoc Polly pipelines inside handlers or gateways when the operation is HTTP-based.

- Configure resilience per named or typed dependency client, not as one global backend policy.
- Use timeout, circuit breaker, telemetry, and transient-response handling at the HTTP adapter boundary.
- Retry only when the operation is proven idempotent through a provider idempotency key or equivalent durable identity.
- Unsafe HTTP methods (`POST`, `PUT`, `PATCH`, `DELETE`, `CONNECT`) are not retried by default.
- Validation/authentication failures and ordinary `4xx` responses are not transient failures.
- Non-HTTP boundaries such as MQTT operations or custom background processing may use Polly `ResiliencePipeline` directly when their retry semantics are explicit.

PayOS is the first adopted HTTP resilience boundary. Its typed client uses the standard resilience handler with per-attempt and total timeouts, while retry remains disabled for payment-creation `POST` requests. A future retry policy requires an explicitly verified PayOS idempotency guarantee; deterministic local order-code generation alone is not assumed to prove provider-side idempotency.

MQTT command wake-up uses a direct Polly pipeline because it is not an HTTP operation. It permits one short retry with exponential backoff and jitter. Duplicate wake-up messages are safe because they contain the same committed `EdgeCommand.Id`, use QoS 1, and only tell Edge to pull; command pull and Edge-side command deduplication remain authoritative. Exhausted retries do not roll back the committed command.

SMTP delivery has one operation timeout covering connect, authenticate, send, and disconnect. It does not retry because a timeout can occur after the SMTP server accepted the message. Safe retry requires a durable email outbox and a delivery identity before another attempt is allowed.

Robot artifact object storage retries only transport failures for read-only/control-plane operations: object stat/existence checks, bucket existence checks, and presigned read URL generation. Upload stream, server-side copy, delete, and list/orphan scan are not automatically retried. Retrying upload requires a rewindable stream or staged file together with an immutable object key.

Firebase token verification does not retry invalid, expired, or revoked tokens. It retries only explicit SDK service failures (`Unavailable`, `Internal`, `Unknown`, or `DeadlineExceeded`) and transport/operation-timeout failures. Invalid tokens remain authentication failures, not provider-unavailability failures.

Resilience configuration is owned by each dependency boundary. Do not create one global timeout/retry/circuit-breaker section because payment creation, token verification, SMTP delivery, object storage, and MQTT have different idempotency and failure semantics. Configurable ranges are validated during application startup.

Mosquitto dynamic-security credential commands also use a direct Polly pipeline for transport failures. Provisioning operations are designed as idempotent upserts: a repeated `createClient` enters the existing-client update path, password replacement is deterministic, role assignment tolerates an already-assigned result, and disable is repeatable. Broker business-error responses are not retried; only an exception or command timeout triggers the single short retry.

## Do Not Soft Delete Event Tables

Event, callback, retry, and append-only evidence tables should not use soft delete as a normal lifecycle.

Append-only event/log/ledger tables should not use soft delete:

- `EdgeCommandDeliveryAttempt`
- `DeviceEvent`
- `OperationLog`
- `StockMovement`
- `KioskHeartbeat`
- `PaymentCallback`
- `SyncEventInbox`
- `SyncDeadLetter`

If data is wrong, append a correction/reversal event instead of deleting or soft-deleting the original record.

## Related Docs

- [Data Modeling Rules](DATA_MODELING_RULES.md)
- [JSON Field Rules](JSON_FIELD_RULES.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
- [Naming Rules](../process/NAMING_RULES.md)
````

## File: docs/flows/ALERT_LIFECYCLE_FLOW.md
````markdown
# Alert Lifecycle Flow

## Search Keywords

`alert`, `alert lifecycle`, `acknowledge alert`, `resolve alert`, `dedup`, `occurrence count`, `device event`, `SignalR AlertChanged`, `Firebase critical alert`

## Purpose

`DeviceEvent` is immutable telemetry evidence. `Alert` is the actionable operational state derived from suitable telemetry. `MaintenanceTicket` remains a separate work-management record; inventory-empty automation may create a linked ticket when enabled.

## Creation

```text
Edge submits DeviceEvent
-> Backend authenticates endpoint and validates device/kiosk scope
-> Warning: store evidence only
-> Error or Critical: create or correlate an actionable Alert in one transaction
-> Commit
-> Publish DeviceEventCreated and AlertChanged
```

Rules:

- Only newly accepted `Error` and `Critical` device events create or update alerts automatically.
- `Warning` remains searchable evidence and does not create alert noise by default.
- `Alert.SourceType = DeviceEvent` and `Alert.SourceId` points to the persisted `DeviceEvent.Id`.
- Device-event retry uses the existing `eventId` idempotency boundary and creates neither a duplicate event nor a duplicate alert.
- Alert creation fails atomically with device-event ingestion; the system does not commit one without the other.
- Raw `PayloadJson` remains evidence-only and is not copied into the Alert response.

## Correlation And Deduplication

Repeated events are grouped by `KioskId + DeviceId + normalized AlertCode` within the configured rolling correlation window (`EdgeTelemetryIngestion:AlertCorrelationWindowMinutes`, default 15 minutes).

- An `Open` or `Acknowledged` alert inside the window receives another occurrence instead of creating a new row.
- `OccurrenceCount` increments and `LastOccurredAt` advances; `RaisedAt` remains the first occurrence.
- `SourceId`, title, and message describe the latest occurrence. Severity may increase but does not decrease.
- `Resolved` and `Suppressed` alerts are terminal and are never reopened by correlation. A later event creates a new alert.
- Correlation is serialized with a PostgreSQL advisory transaction lock, so concurrent repeated events cannot create parallel alerts for the same key.
- An event outside the rolling window creates a new alert even when an older non-terminal alert exists.

## Lifecycle

```text
Open
-> Acknowledged
-> Resolved

Open
-> Resolved
```

- Acknowledge records `AcknowledgedByAccountId` and `AcknowledgedAt`.
- Resolve requires resolution notes and records `ResolvedAt`.
- Repeating acknowledge or resolve is idempotent and does not publish another transition.
- Resolved and Suppressed are terminal for the exposed V1 lifecycle.
- Lifecycle mutations serialize by `alertId`; SignalR publishes only after commit.

## API

```http
GET   /api/v1/management/alerts
GET   /api/v1/management/alerts/{alertId}
PATCH /api/v1/management/alerts/{alertId}/acknowledge
PATCH /api/v1/management/alerts/{alertId}/resolve
```

List filters: `status`, `severity`, `organizationId`, `storeId`, `kioskId`, `deviceId`, `from`, `to`, `pageNumber`, and `pageSize`. Date filters and default descending order use `LastOccurredAt`, so a correlated active alert returns to the top of the operational queue.

Creation is intentionally part of authenticated device-event ingestion rather than a general management `POST /alerts`. V1 does not allow operators to fabricate telemetry alerts manually.

## Authorization

| Policy | Roles | Behavior |
| --- | --- | --- |
| `alerts.view` | SystemAdmin, OrgAdmin, Manager, Staff, Technician | Read alerts inside assigned tenant scope |
| `alerts.manage` | SystemAdmin, OrgAdmin, Manager, Technician | Acknowledge or resolve alerts inside assigned tenant scope |

## Realtime

`AlertChanged` is sent to `kiosk:{kioskId}` after creation, correlated occurrence, acknowledgement, or resolution commits. It contains the alert identity, scope, device, severity, old/new status, occurrence count, last occurrence timestamp, update timestamp, and version. Clients use REST for initial state/history and SignalR for committed deltas.

## Critical Alert Push Notification

Problem: a franchise operator may not have the management dashboard open while
an unattended kiosk has a fault that requires human intervention. SignalR is
appropriate for connected clients but does not recall an absent operator.

Confirmed decision: `CriticalOperationalAlertOpened` is the first Firebase
business notification. It is derived from committed `Alert` state, never from
raw `DeviceEvent` payload. A push is attempted only when a new Alert is created
as Critical or an existing correlated Alert increases from Error to Critical.
Duplicate source events and later Critical occurrences do not send another
push.

Recipient policy:

1. Select distinct active Technician and Manager accounts whose active role
   scope matches the Alert kiosk, store, or organization and which have an
   active notification-device registration.
2. If that set is empty, select active OrgAdmin accounts assigned to the exact
   organization and having an active notification-device registration.
3. Do not broadcast to SystemAdmin, Staff, unrelated tenant scopes, or every
   account that can technically read an Alert.

The payload is bounded to notification type, Alert/Kiosk/Device identifiers,
Alert code, and Critical severity. Raw telemetry payload, customer data, and
the complete device error message are excluded. The visible title is the
bounded Alert title.

The Alert and one durable `NotificationDelivery` per recipient are committed in
the same database transaction. A background worker sends Firebase push after
commit and retries transient failures. Delivery failure never rolls back device
evidence or actionable Alert state. SignalR and management reads remain the live
and authoritative operational surfaces. The outbox provides at-least-once send
attempts; clients deduplicate by `deliveryId`.

Excluded from the critical-alert trigger:

- every Error/Warning DeviceEvent: too noisy and bypasses Alert correlation.
- customer/order progress: the kiosk/tablet already uses authoritative polling
  and SignalR while the customer is present.

Operations owns `NotificationDelivery` and recipient selection; Identity owns
account notification-device registrations and Firebase delivery. Inventory
empty uses a separate trigger and delivery key instead of reusing the critical
device-alert trigger.

The same outbox is shared infrastructure for other independently owned events,
including overdue Manual/Packaged fulfillment. Each event keeps its own trigger,
recipient policy, and idempotent delivery key; it does not become an Alert.

Failed configuration deployments are reconciled from committed Full Edge and
Low-cost deployment state, including executor failures and timeout failures.
They notify scoped Technician/Manager accounts and fall back to OrgAdmin.
Candidates from both execution profiles share one failure-time-ordered batch,
so one profile cannot starve the other. A failure with no currently eligible
recipient remains pending and becomes deliverable if a matching account and
notification device are provisioned later; Cloud does not create a synthetic
recipient or mark it notified without a delivery. Recipient-less failures are
excluded before the bounded batch is selected, so they cannot starve later
deliverable failures. One failed candidate is isolated and does not stop the
remaining items in the batch.
Maintenance assignment accepts only an active Technician, Manager, or OrgAdmin
in the ticket tenant scope. It notifies that assignee when an active notification
device exists. Requeueing a permanently failed delivery repeats delivery only;
it never repeats the source business transition.

Payment-session reconciliation creates a `payment_intervention` delivery only
when the session enters manual intervention: retry exhaustion, a provider
identity or amount mismatch, or provider-paid state still awaiting the signed
webhook after retries are exhausted. Scoped Staff and Manager recipients are
preferred, with organization OrgAdmin fallback. Retryable reconciliation,
restored checkout instructions, explicit cancellation/expiry, and known
provider-session absence do not create this notification. Delivery identity is
the payment transaction, intervention code, and recipient, so repeating the
same reconciliation result is idempotent.

The durable delivery-key evidence for `deployment_failed`,
`fulfillment_overdue`, and `payment_intervention` is retained beyond ordinary
notification-delivery retention. Source reconciliation therefore cannot resend
the same occurrence merely because historical outbox rows were purged.

## Excluded From V1

- configurable alert rules or thresholds;
- alert assignment, escalation, snooze, or suppression API;
- configurable inventory thresholds beyond the current Low/Empty state mapping.

## MQTT Credential Operational Alerts

The MQTT credential reconciliation job derives actionable alerts from committed
credential state. It does not create alerts for a transient broker error that
the original request records and returns immediately.

| Alert code | Trigger | Recovery |
| --- | --- | --- |
| `MQTT_CREDENTIAL_OPERATION_TIMEOUT` | Provisioning or rotation remains pending beyond the five-minute operation lease and is marked `Failed` | Resolve after an operator retry activates the credential |
| `MQTT_CREDENTIAL_REVOKE_FAILED` | Automatic stale revocation retry fails and records `RevokeFailed` | Resolve when a later automatic or operator revocation reaches `Revoked` |

Alerts are correlated by execution endpoint source and alert code under a
PostgreSQL advisory lock. A repeated failed revocation retry increments
`OccurrenceCount`; periodic scans do not create synthetic occurrences. Active
alerts are scanned for recovery even after the credential no longer qualifies
as stale, so successful manual repair also resolves the alert. Creation,
occurrence, and resolution publish `AlertChanged` only after commit. These alerts
use Error severity and do not trigger the Critical Firebase notification policy.

## Inventory Alert Automation

The reconciliation job maps active dispenser state to `INVENTORY_LOW` or
`INVENTORY_EMPTY`. It serializes each dispenser with a PostgreSQL advisory lock,
keeps one active alert for the current threshold, resolves stale/duplicate active
alerts, and publishes SignalR only for committed transitions. When
`InventoryAlertAutomation:CreateMaintenanceTicketForEmpty` is enabled, Empty
creates one linked maintenance ticket. Empty also creates a durable push for the
scoped operational recipients. Healthy recovery resolves the alert; it does not
close the maintenance ticket automatically.

## Related Docs

- [Operations Support Flow](OPERATIONS_SUPPORT_FLOW.md)
- [Maintenance Ticket Flow](MAINTENANCE_TICKET_FLOW.md)
- [Edge Sync and Telemetry Contract](../iot/EDGE_SYNC_TELEMETRY_CONTRACT.md)
- [Observability](../operations/OBSERVABILITY.md)
````

## File: docs/operations/ROBOT_ARTIFACT_OPERATIONAL_SMOKE.md
````markdown
# Robot Artifact Operational Smoke Test

## Search Keywords

`robot artifact smoke test`, `Lua upload`, `RobotProgram`, `configuration release`, `deployment`, `execution endpoint`, `MinIO`, `artifact checksum`

Use this workflow to validate PostgreSQL migrations, MinIO object storage, execution-endpoint compatibility, and the complete artifact authoring/deployment path.

The end-to-end business flow and API lookup remain owned by [Robot Lua Artifact Flow](../flows/ROBOT_LUA_ARTIFACT_FLOW.md). This document owns only operational setup and executable verification.

## Local Runtime Dependencies

Backend compose owns PostgreSQL and MinIO for local backend operation:

```powershell
docker compose up -d postgres minio
```

Local defaults are:

```text
MinIO API: http://localhost:9000
MinIO console: http://localhost:9001
Bucket: icebot-robot-artifacts
Access key: minioadmin
Secret key: minioadmin
```

Override `MINIO_ROOT_USER`, `MINIO_ROOT_PASSWORD`, `MINIO_BUCKET_NAME`, and `MINIO_DOWNLOAD_ENDPOINT` outside source control when defaults are unsuitable. `MINIO_DOWNLOAD_ENDPOINT` must be reachable by the actual Edge/controller, not only by the backend container.

Development configuration and the local backend compose set `AutoCreateBucket=true`, so backend startup creates the private bucket when it is absent. Production defaults to `false`: infrastructure must provision the bucket before the API starts. PostgreSQL stores artifact metadata only.

## Apply Migrations To A Test Database

Set a test connection string, then apply migrations explicitly:

```powershell
$env:ConnectionStrings__IceBot_DB = "Host=localhost;Port=5432;Database=IceBotDB;Username=postgres;Password=p@ssw0rd12345"
dotnet ef database update --project src\Infrastructure --startup-project src\WebAPI --context IceBotDbContext
```

Do not point this command at a shared or production database unless that deployment is intentional.

## Automated Operational Smoke

The smoke test uses isolated PostgreSQL and MinIO Testcontainers. It applies all migrations, creates the bucket on upload, and seeds an active Full Edge endpoint with the supported robot target `FAIRINO_LUA_V1 / FR5`.

```powershell
$env:ICEBOT_RUN_INTEGRATION_TESTS = "true"
dotnet test tests\IceBot.IntegrationTests\IceBot.IntegrationTests.csproj --filter FullyQualifiedName~RobotArtifactOperationalSmokeTests
```

The validated flow is:

```text
bulk upload .lua
-> publish RobotArtifact
-> create RobotProgram
-> assign artifact RunOrder
-> publish RobotProgram
-> create ConfigurationRelease
-> author execution route
-> publish release
-> deploy to Full Edge endpoint
-> pull and accept command
-> report Installed
-> report Active
-> create and pay a machine-produced order
-> resolve endpoint/release/route/program
-> create ExecuteOrder dispatch attempt 1
-> repeat attempt 1 and receive the same command
-> acknowledge Accepted and project Order Accepted
-> report Running with typed stock evidence and project Order Preparing
-> report Completed and project Order Completed
-> verify Rejected, ExecutorBusy, and Failed order outcomes
```

Success requires the deployment and endpoint projection to reference the active release, and the paid order to produce exactly one idempotent `ExecuteOrder` command for the selected endpoint.

## Contract Coverage

The broader Edge/controller integration suite additionally verifies presigned download, byte length, SHA-256 checksum, Accepted/Rejected acknowledgement, Installed/Active/Failed reports, and duplicate `SourceEventId` handling:

```powershell
$env:ICEBOT_RUN_INTEGRATION_TESTS = "true"
dotnet test tests\IceBot.IntegrationTests\IceBot.IntegrationTests.csproj
```

## Related Docs

- [Robot Lua Artifact Flow](../flows/ROBOT_LUA_ARTIFACT_FLOW.md)
- [Robot Lua Deployment And Activation Flow](../flows/ROBOT_LUA_DEPLOYMENT_AND_ACTIVATION_FLOW.md)
- [Deployment Configuration](DEPLOYMENT_CONFIG.md)
- [MQTT Operations](MQTT_OPERATIONS.md)
````

## File: docs/data/JSON_FIELD_RULES.md
````markdown
# JSON Field Rules

JSON columns are acceptable in this domain because the system runs across edge kiosks, robot SDKs, payment providers, and cloud sync. They must not all be treated the same. Every JSON field should fall into one of these roles.

## Search Keywords

`JSON fields`, `JSONB`, `ConfigJson`, `SettingsJson`, `ParametersJson`, `SnapshotJson`, `PayloadJson`, `HeadersJson`, `RawRequestJson`, `RawResponseJson`, `MetadataJson`, `schema version`, `source of truth JSON`, `immutable snapshot`, `append-only payload`, `provider payload`, `robot parameters`, `sync payload`, `JSON conflict resolution`

## Roles

### Source of truth configuration

These fields can affect runtime behavior. They are mutable only while the owning aggregate is in a draft/provisioning state. After publish/activation, update by creating a new business version or increasing the entity configuration/version field.

Fields:

- `DeviceModel.CapabilitiesJson` with `CapabilitiesSchemaVersion`
- `Kiosk.SettingsJson` with `SettingsSchemaVersion`
- `Store.OpeningHoursJson` with `OpeningHoursSchemaVersion`
- `Recipe.InstructionsJson` with `InstructionsSchemaVersion`
- `RobotProgramArtifact.ParametersJson` with `ParametersSchemaVersion`
- `RobotArtifact.MetadataJson`
- `ConfigurationRelease.ManifestJson` with `ReleaseManifestSchemaVersion`
- `ExecutionRoute.RequiredCapabilitiesJson`
  - When present, this field must use schema version `1`: `{ "schemaVersion": 1, "requires": [{ "code": "...", "minVersion": "...", "required": true }] }`.
  - Codes must match capability codes already declared by the same route's robot bindings. Unknown fields are rejected.
  - Cloud validates required codes against endpoint readiness. Endpoint readiness does not yet report capability versions, so a required `minVersion` makes the route unavailable to runtime-menu and checkout and produces `CapabilityVersionUnverifiable` for deployment/dispatch instead of being ignored.
- `ExecutionRoute.SupportedOptionCodesJson`
  - Internal JSON storage for the normalized production-affecting option codes supported by one route.
  - Management APIs expose `supportedOptionCodes` as a typed string collection; clients do not send JSON.
  - The value is included in immutable production-definition and release checksums and is enforced by runtime-menu, order, readiness, and dispatch flows.
- `EdgeCommand.PayloadJson`
- `IngredientDispenserState.LevelToQuantityProfileJson` with `LevelToQuantityProfileSchemaVersion`
  - API contracts expose typed `Low`, `Medium`, and `Full` points; FE does not send JSON or schema version.
  - This mapping is categorical and is not a numeric sensor calibration profile.

Rules:

- Do not put query-critical fields only inside JSON. Promote them to typed columns.
- Validate JSON against the matching schema version before publish, activation, or deployment to edge.
- Edge sync conflict resolution must use the owning aggregate version, not JSON diffing.
- Published robot program manifests and ordered artifact descriptors should be shipped to Edge as immutable versioned configuration, not as ad hoc realtime motion/step edits.

### Immutable snapshot

These fields preserve the state used at the time of an order or robot job. They are not source of truth after creation, but they are required for audit, replay, refunds, and reporting.

Fields:

- Product-option selections are stored as typed `OrderItemOption` snapshots. Anonymous checkout payloads and Edge execution commands must not carry arbitrary option JSON.
- `OrderItem.RecipeSnapshotJson` with `RecipeSnapshotSchemaVersion`
- `OrderExecutionRecord` and `ProductionExecutionRecord` keep typed projection fields rather than raw executor payloads.
- `PaymentTransaction.RawRequestJson`
- `PaymentTransaction.RawResponseJson`

Rules:

- Treat as immutable once the order item, robot job, or payment attempt is created.
- If product, product variant, recipe, or option configuration changes later, do not rewrite historical snapshots.
- Reports may read snapshots for historical truth, but current catalog pages should read typed product/product variant/recipe tables.

### Append-only payload/debug

These fields are evidence from external systems, sync, or runtime events. They are useful for troubleshooting and replay, but should not drive core business decisions unless normalized into typed fields first.

Fields:

- `PaymentCallback.PayloadJson`
- `SyncEventInbox.PayloadJson`
- `SyncEventInbox.HeadersJson`
- `SyncDeadLetter.PayloadJson`
- `DeviceEvent.PayloadJson`
- `EdgeCommand.PayloadJson`
- `OperationLog.PayloadJson`
- `KioskHeartbeat.PayloadJson`
- `IngredientDispenserState.SensorPayloadJson`

Rules:

- Treat as append-only or last-observation debug data depending on the owning entity.
- Idempotency must use typed keys such as provider event id, event id, source node id, or correlation id.
- Retry logic must use typed retry/status columns, not parsed JSON state.
- If a value becomes operationally important, add a typed column and backfill from payloads if needed.

### Metadata

Metadata JSON is for non-critical vendor extensions, display hints, and integration-specific extra data.

Fields:

- `Organization.MetadataJson`
- `Product.MetadataJson`
- `ProductOption.MetadataJson`
- `Ingredient.MetadataJson`
- `Device.MetadataJson`
- `DeviceModel.MetadataJson`

Rules:

- Do not use metadata as a hidden domain model.
- Do not require metadata for checkout, robot execution, stock calculation, payment state, tenant isolation, or authorization.
- Promote repeated, queried, indexed, or validated keys to typed columns.

## Naming

- `*ConfigJson`, `*SettingsJson`, `*ParametersJson`, `*InstructionsJson`, `*ProfileJson`: source of truth configuration, must have an explicit schema version.
- `*SnapshotJson`: immutable historical copy.
- `PayloadJson`, `HeadersJson`, `Raw*Json`: external evidence/debug payload.
- `MetadataJson`: optional extension data only.
- Generic metadata columns are persistence extension points, not default frontend editing contracts. Normal organization, catalog, and menu APIs expose typed fields only; technical artifact authoring may expose metadata when the artifact workflow owns a concrete use case.
- `Kiosk.SettingsJson` and `Device.MetadataJson` remain persistence extension points but are not exposed through normal management CRUD until a typed use case exists. `Store.OpeningHoursJson` is an internal serialized representation; frontend contracts use typed day/open/close fields.

## Sync Boundary

For edge-cloud sync, conflict resolution should happen at the aggregate boundary:

- Robot configuration: `RobotArtifact`, `RobotProgram`, `RobotProgramArtifact`, and immutable `ConfigurationRelease` manifests.
- Cloud execution: typed `OrderExecutionRecord` and `ProductionExecutionRecord` projections. Edge-local runtime events are not Cloud entities.
- Stock reporting: typed `StockMovement` quantities; JSON sensor payloads are only supporting evidence.

Never resolve sync conflicts by merging arbitrary JSON payloads from cloud and edge. Either reject stale writes, create a new version, or normalize the changed field into typed columns.

## Related Docs

- [Data Modeling Rules](DATA_MODELING_RULES.md)
- [Idempotency and Retry Rules](IDEMPOTENCY_RETRY_RULES.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
- [Boundary Contexts](../architecture/BOUNDARY_CONTEXTS.md)
````

## File: docs/iot/EDGE_COMMAND_CONTRACT.md
````markdown
# Edge Command Contract

This document owns Cloud-to-Edge command delivery, endpoint provisioning/authentication, command acknowledgement, execution reports, and configuration distribution.

## Search Keywords

`EdgeCommand`, `command pull`, `command ack`, `execution report`, `DeployConfiguration`, `ExecuteOrder`, `MQTT wake-up`, `execution endpoint`, `mTLS`, `signed command`, `configuration sync`

## Cloud To Edge Notification

### MQTT Command-Available Wake-Up

After a durable `ExecuteOrder` or `DeployConfiguration` command commits, Cloud makes a best-effort MQTT publish. The wake-up lowers latency only; periodic authenticated command pull remains the delivery authority and recovers broker outages or missed messages.

Topic:

```text
icebot/execution-endpoints/{executionEndpointId}/commands/available
```

Payload:

```json
{
  "type": "CommandAvailable",
  "commandId": "uuid",
  "commandType": "ExecuteOrder",
  "targetExecutionEndpointId": "uuid",
  "notifiedAt": "2026-05-21T10:00:00Z",
  "version": 1
}
```

Rules:

- MQTT payload is a wake-up signal only.
- Edge must call command pull after receiving this notification.
- The message uses QoS 1 and is not retained. Duplicate delivery is expected.
- MQTT publish failure does not roll back or fail the already committed command.
- Duplicate MQTT messages are expected and must be harmless.
- MQTT notification is best-effort; periodic Edge pull is the delivery authority.
- Broker ACLs bind each MQTT subscriber to its own execution-endpoint topic. Edge calls command pull after every wake-up and also after reconnect/on its polling interval. Operational setup is defined in [MQTT Operations](../operations/MQTT_OPERATIONS.md).
- MQTT subscriber identity is provisioned separately from HTTPS execution authentication. Username and client id equal `executionEndpointId`; the generated password is returned once, held by the broker/Edge secret stores, and never persisted in the application database. Rotation immediately invalidates the old password; revoke disables and disconnects the broker client.

## Edge To Cloud

### Pull Commands

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/commands/pull
```

Full Edge sends its provisioned client certificate during the TLS handshake. Low-cost controllers add the signed-request headers defined under **Execution Endpoint Transport Authentication** below.

Request:

```json
{
  "maxCommands": 10,
  "edgeTime": "2026-05-21T10:00:02Z"
}
```

Response:

```json
{
  "serverTime": "2026-05-21T10:00:03Z",
  "commands": [
    {
      "commandId": "uuid",
      "commandType": "DeployConfiguration",
      "kioskId": "uuid",
      "targetExecutionEndpointId": "uuid",
      "orderId": null,
      "dispatchAttemptNo": null,
      "issuedAt": "2026-05-21T10:00:00Z",
      "expiresAt": "2026-05-21T10:10:00Z",
      "payloadJson": "{... canonical command payload ...}"
    }
  ]
}
```

Rules:

- Edge must deduplicate by `commandId`.
- New `ExecuteOrder` payloads include `SchemaVersion = 4`, `executionIntent`, a `productionUnitStartNo` on each line, and the immutable `restartPolicy` of every selected robot program. V1 emits and accepts only `ManualOnly`; schema-3 payloads without the field are interpreted as `ManualOnly`. Edge must reject unsupported schema versions or restart policies. Cloud may read release provenance from older payloads without treating unsupported payloads as fully executable contracts. Each selected option can include immutable ingredient requirements (`ingredientId`, code/name snapshots, quantity per option, unit, required workcell capability); Edge must use this order snapshot rather than live catalog data.
- An ordered artifact may include `RequiredOptionCode`. Edge executes that artifact only when the same order line contains a selected option with the matching normalized code; otherwise Edge skips it without changing the remaining `RunOrder`. This is conditional file selection, not a Lua runtime parameter.
- Deployment commands include typed Cloud correlation fields for deployment ownership. `PayloadJson` is execution data, not the authoritative link used by timeout reconciliation.
- Pull first materializes any short-lived artifact download URLs. Only after payload enrichment succeeds does it mark returned commands as `Delivered` and record a delivery attempt.
- Retrying command pull can return delivered but unacknowledged commands.
- Runtime execution state is reported through the event/report ingest boundary, not command ack.
- If a deployment command expires before acceptance, Cloud marks the command `Rejected` with `CommandExpired` and marks the linked Pending deployment `Failed`.
- Once accepted, command expiry no longer applies. If no `Installed`, `Active`, or `Failed` report moves the deployment out of Pending within the configured accepted-report timeout, Cloud marks that attempt `Failed/ExecutionReportTimeout` without changing the endpoint's previously active release/artifact set.
- Late reports do not revive a timed-out attempt. Reconciliation requires a new deployment/rollback attempt so Cloud and endpoint history remain explicit.
- A Full Edge deployment materializes or reuses a deterministic bundle from the published profile-neutral release manifest. Its `DeployConfiguration` payload contains both that immutable bundle descriptor and individual artifact descriptors. During authenticated pull, both receive short-lived URLs so Edge may choose cache-aware incremental download or the complete bundle. Low-cost publication and payloads do not require a Full Edge bundle and contain only their selected artifact descriptors.
- The Full Edge ZIP contains `release-content-manifest.json` plus `artifacts/{RobotArtifactId}.lua`. The manifest includes routes, ordered program bindings, parameters, compatibility, entry names, sizes, and artifact checksums needed for installation.
- Rollback uses the same `DeployConfiguration` command contract and includes `RollbackTargetDeploymentId` as provenance. Edge installs it as a new deployment attempt; it does not locally mutate the historical deployment.
- `Installed` and `Active` deployment reports must match the accepted command's typed `DeploymentId` and deployment kind. Full Edge reports must echo `SourceConfigurationReleaseId` and `ReleaseChecksum`; Low-cost reports must additionally echo `ActiveSetVersion` and `ActiveSetChecksum`. Mismatches are rejected without changing deployment or endpoint observed state. `Failed` may omit installed-state provenance because no activation is asserted.
- Presigned download URLs are transport data only. They are not persisted in `EdgeCommand.PayloadJson`, release manifests, or artifact metadata.
- The object-storage bucket remains private. Edge must download before URL expiry and must not treat the URL as an artifact identity.
- `DownloadUrl` must use an endpoint reachable from the execution endpoint. A Docker-internal MinIO hostname is not a valid external Edge download endpoint unless both runtimes share that network.
- For incremental download, Edge verifies each artifact byte length and SHA-256 checksum. For bundle download, Edge first verifies bundle size/checksum, safely extracts expected entries only, then verifies every extracted artifact against `release-content-manifest.json`.
- A failed download, expired URL, size mismatch, or checksum mismatch must fail the deployment attempt. Edge may pull the unacknowledged command again to obtain fresh download URLs; it must not activate partial or unverified files.
- Fairino-Studio currently exports multiple `.lua` files: normally one file per editor step, while a paired loop is exported as one file. Each exported file is stored as one `RobotArtifact`; `RobotProgramArtifact.RunOrder` defines their runtime sequence.
- Filename prefixes such as `01_` are human-facing export hints, not execution authority. Edge executes the ordered program manifest delivered by Cloud.

### Execution Endpoint Provisioning Boundary

Before command pull, Cloud management must create and activate a `KioskExecutionEndpoint`:

1. Create the endpoint in `Provisioning` for one kiosk and one execution profile.
2. Replace its supported robot targets using runtime-target code, machine-model code, and optional same-kiosk device binding.
3. Provision profile-specific authentication material and a profile identity.
4. Activate the endpoint; only an Active endpoint with an Active credential may authenticate command pull or report execution state.

Full Edge uses `FullEdgeRuntimeId` and requires `MutualTls`; provisioning accepts the client certificate SHA-256 fingerprint. Low-cost uses `ControllerId` and requires `SignedCommandTls`; provisioning accepts an ECDSA NIST P-256 public key PEM. The backend stores only the canonical public key and its SHA-256 fingerprint, never the controller private key. Disabling or retiring an endpoint blocks runtime authentication without deleting deployment or execution history.

Full Edge provisioning body:

```json
{
  "profileIdentity": "<full-edge-runtime-id>",
  "clientCertificateSha256Fingerprint": "<64 lowercase hex characters>"
}
```

Low-cost provisioning body:

```json
{
  "profileIdentity": "<controller-id>",
  "ecdsaPublicKeyPem": "-----BEGIN PUBLIC KEY-----\n...\n-----END PUBLIC KEY-----"
}
```

### Execution Endpoint Transport Authentication

All IoT routes require HTTPS. The authenticated `KioskExecutionEndpoint` is
identified by `{endpointId}` in the route; Cloud derives `KioskId` from that
endpoint instead of trusting a second route or header identity.

Persistence uses the same boundary: readiness, supported-target, telemetry,
alert, deployment, and execution projection rows cannot pair an endpoint or
device with a different kiosk. Deployments also require the selected release
and kiosk to belong to the same organization. Production execution reports
must reference the endpoint targeted by their source command. These are
database constraints in addition to transport authentication and handler
authorization checks.

**Full Edge:** Kestrel requests a client certificate and the application compares its SHA-256 fingerprint with the active credential binding using constant-time comparison. Certificate-chain trust is not used as endpoint identity; the provisioned fingerprint is the pinning boundary.

**Low-cost controller:** every request includes:

```text
X-Execution-Timestamp: <Unix seconds>
X-Execution-Nonce: <new UUID per request>
X-Execution-Signature: <Base64 ECDSA P-256 signature>
```

The signature uses SHA-256 and IEEE P1363 fixed-field format (64-byte `r || s`). It signs this UTF-8 canonical string, with one LF between fields and no trailing LF:

```text
UPPERCASE_HTTP_METHOD
REQUEST_PATH
RAW_QUERY_STRING_OR_EMPTY
endpoint-id-in-D-format
unix-timestamp
nonce-in-D-format
lowercase-sha256-of-raw-request-body
```

The request path includes the API version and route exactly as sent. The query string includes its leading `?` when present. The body hash is computed from raw bytes before JSON model binding.

Cloud rejects signatures outside `ExecutionEndpointSecurity:SignedRequestMaxClockSkewSeconds`. After successful signature verification, it atomically stores `(EndpointId, Nonce)`; reuse is rejected even across backend instances. Expired nonce rows are removed by data retention. Clients retry with a new timestamp, nonce, and signature.

### Command Ack

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/commands/{commandId}/ack
```

Request:

```json
{
  "ackStatus": "Accepted",
  "acknowledgedAt": "2026-05-21T10:00:05Z",
  "localStatePersisted": true,
  "rejectionCode": null,
  "rejectionMessage": null,
  "physicalOutputMayHaveOccurred": null
}
```

Allowed `ackStatus` values:

- `Received`
- `Accepted`
- `Rejected`
- `ExecutorBusy`
- `DeliveryFailed`

Command ack owns delivery and executor-admission state. For `ExecuteOrder`, that admission decision also projects to the order lifecycle as defined below. `Running`, `Completed`, `Failed`, and
`RequiresManualIntervention` belong to the execution event/report ingest
boundary, not this endpoint.

`acknowledgedAt` is executor evidence, not ordering or expiry authority. It must not be farther in the future than `ExecutionReportIngestion__MaxFutureClockSkewSeconds`, and must not predate command creation or recorded delivery beyond that same skew allowance. Cloud applies the acknowledgement, command expiry, provisional execution projection, status history, and realtime update at Cloud receive time.

For `ExecuteOrder` commands:

- `Accepted` may move `Order` from `ReadyForFulfillment` to `Accepted` when aggregate line state allows it.
- `Accepted` requires `localStatePersisted=true`. Edge sends it only after one local transaction durably records the command, production/deployment jobs, immutable provenance, and the ACK outbox entry. Persisting after ACK is invalid because a crash would leave Cloud with an accepted command that Edge cannot recover.
- If that local transaction cannot commit, Edge responds `Rejected` with `LocalPersistenceUnavailable`, `LocalDatabaseCorrupt`, `InsufficientLocalStorage`, or `EventBacklogLimitExceeded`. It must not start physical execution.
- `ExecutorBusy` is temporary: the command returns to `PendingDelivery` and the order remains `ReadyForFulfillment`.
- `Rejected` with `physicalOutputMayHaveOccurred` absent or `false` moves the order to `ExecutionRejected`.
- `Rejected` with `physicalOutputMayHaveOccurred=true` moves the paid order to `RefundRequired` because staff support or compensation may be required.
- Order status changes and their `OrderStatusHistory` row commit together with the command acknowledgement.
- Accepting an `ExecuteOrder` command creates a provisional `OrderExecutionRecord` with sequence `0`. This is Cloud correlation state, not a fabricated Edge event; the first order-summary report starts at sequence `1` and replaces it with executor evidence.

Execution timeout reconciliation:

- Before ACK, an expired `ExecuteOrder` command becomes `Rejected/CommandExpired`; a still-`ReadyForFulfillment` order becomes `ExecutionRejected` with status history.
- An Accepted command with no order-summary report beyond the configured deadline becomes `Stale/Delayed` when the executor heartbeat is still current.
- An Accepted or Running execution with no current heartbeat becomes `Unreachable/PendingRecovery`.
- Prolonged unreachable observation becomes `Unreachable/SupportRequired` for customer/support handling without asserting physical failure.
- Reconciliation changes `OrderExecutionRecord.ObservationStatus` and `CustomerExecutionStatus`; it does not claim that the physical job failed and does not change an Accepted/Preparing Order to `Failed`.
- Customer order/payment polling reads the latest dispatch attempt projection. SignalR publishes `OrderExecutionObservationChanged` to the order group for the same projection.
- A later valid executor report restores observation to `Fresh` through normal sequence validation.
- Missing-report deadlines and support escalation use the last Cloud receive time. `LastExecutorReportedAt` remains diagnostics evidence and cannot make a freshly received report immediately stale when an executor clock moves backward.

Redispatch is an explicit management operation, not an Edge-side automatic retry. Backend permits a new attempt only after transport `DeliveryFailed` or a rejection proven to be before physical output. It allocates the next attempt number under the order lock, enforces the configured maximum, and records operator/reason audit. `ExecutorBusy` redelivers the same command; possible physical output, `RefundRequired`, production failure, and manual intervention remain support/compensation flows.

### Execution Reports

Primary MQTT topic:

```text
icebot/execution-endpoints/{endpointId}/uplink/execution-report
```

The MQTT `payload` is the request below plus `commandId`. Cloud returns the
application result on the endpoint `uplink/results` topic. Edge retains the
report in its local outbox until that result is received.

HTTPS recovery fallback:

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/commands/{commandId}/reports
```

Request:

```json
{
  "sourceEventId": "uuid",
  "sequenceNumber": 12001,
  "edgeCreatedAt": "2026-05-21T10:01:58Z",
  "executorReportedAt": "2026-05-21T10:01:59Z",
  "reportType": "ProductionExecution",
  "status": "Running",
  "deploymentId": null,
  "sourceProductionJobId": "uuid",
  "sourceConfigurationReleaseId": "uuid",
  "releaseChecksum": "sha256-hex",
  "physicalOutputMayHaveOccurred": true,
  "errorCode": null,
  "errorMessage": null,
  "payloadJson": null,
  "stockMovements": [
    {
      "sourceEventId": "uuid",
      "orderItemId": "uuid",
      "ingredientDispenserStateId": "uuid",
      "quantityConsumed": 12.5,
      "balanceAfter": 87.5,
      "occurredAt": "2026-05-21T10:01:57Z",
      "isEstimated": false
    }
  ]
}
```

Response:

```json
{
  "succeeded": true,
  "statusCode": 200,
  "message": "Execution report applied successfully.",
  "data": {
    "commandId": "uuid",
    "sourceEventId": "uuid",
    "reportType": "Deployment",
    "status": "Active",
    "applied": true,
    "duplicate": false
  }
}
```

Allowed `reportType` values in V1:

| Report type | Meaning | Target |
| --- | --- |
| `Deployment` | Full Edge configuration deployment or low-cost active artifact-set deployment result | `KioskConfigurationDeployment`, `ControllerArtifactSetDeployment`, `KioskExecutionEndpoint` active snapshot |
| `ProductionExecution` | Execute-order production progress/result | `ProductionExecutionRecord`, `OrderExecutionRecord` when order provenance is present |

Deployment `status` values:

- `Installed`
- `Active`
- `Failed`

Production execution `status` values:

- `Accepted`
- `Running`
- `Completed`
- `Failed`
- `RequiresManualIntervention`

Rules:

- MQTT and HTTPS invoke the same execution-report handler. Retrying through
  another transport reuses `sourceEventId`, `sequenceNumber`, `commandId`, and
  every stock-movement source event ID.
- Command ack is dispatch-only. Execution reports are the current V1 boundary for deployment and production status after a command has been accepted.
- The endpoint deduplicates by `(sourceExecutorId, sourceEventId)` using `SyncEventInbox`. A retry is a duplicate only when command identity and the complete normalized report payload match; reusing the event id for another command or payload returns `409 Conflict`.
- Production reports must repeat the `SourceConfigurationReleaseId` and `ReleaseChecksum` from the accepted execute-order command. Low-cost reports must also repeat the command's `ActiveSetVersion` and `ActiveSetChecksum`; Full Edge reports omit both. Cloud compares this provenance against the immutable command payload before creating execution or stock projections.
- `edgeCreatedAt`, optional `executorReportedAt`, and stock-evidence `occurredAt` cannot be farther in the future than `ExecutionReportIngestion__MaxFutureClockSkewSeconds` relative to Cloud receipt time.
- `sequenceNumber` is executor-local ordering evidence for projection updates.
- A final replay may transition `Accepted` directly to `Completed`, `Failed`, or `RequiresManualIntervention`; `Running` may have been lost while the controller was disconnected.
- `physicalOutputMayHaveOccurred` must be set when reporting failed production execution. It drives customer/support projection: failure before output can be handled differently from failure after possible physical output.
- Deployment report `Active` updates the observed active configuration/artifact-set snapshot on `KioskExecutionEndpoint`.
- Job/unit evidence is the Cloud authority for per-unit production outcomes. Non-overlapping ranges update `ProductionExecutionRecord`, optional stock evidence, effective completed/failed/in-progress counts, and the affected machine line in one transaction. A line completes only after all expected units are covered by effective `Completed` evidence; one failed unit moves a paid order to `FulfillmentIssue` while successful unit and inventory evidence remain intact.
- A report with `sourceProductionJobId=null` remains the Edge-computed order observation and updates `OrderExecutionRecord`. When any job evidence exists, a final summary requires complete coverage and must agree with the Cloud-derived aggregate; contradictory final summaries are rejected. A non-final summary may arrive behind newer unit evidence and cannot rewind the business lifecycle.
- A remake is a new `ExecuteOrder` command with `executionIntent=Remake`, `remakeOfSourceCommandId`, and an exact `productionUnitStartNo`/quantity range. Cloud creates it only for failed units with confirmed no physical output. Evidence from the later remake attempt supersedes the failed outcome for those units; all earlier execution and stock evidence remains immutable.
- If Edge or a controller restarts during an active production job, Edge reports that exact job as `RequiresManualIntervention` with `errorCode` equal to `RuntimeRestarted`, `ControllerRestarted`, or `PowerInterrupted`. The report includes the exact unit range and `physicalOutputMayHaveOccurred`; unknown is represented by `null`. Cloud never automatically replays an accepted command, resumes Lua, or restarts the artifact list. See [Restart And Power Recovery](../operations/RESTART_AND_POWER_RECOVERY.md).
- If local persistence becomes unavailable after acceptance, Edge stops admitting new work and reports each affected active production job as `RequiresManualIntervention/LocalPersistenceLost`. It preserves exact unit identity and reports physical output as `true`, `false`, or `null`; inability to persist evidence must never be translated to `false`.
- Changed lines publish `OrderItemFulfillmentChanged`; aggregate order transitions publish `OrderStatusChanged`; applied order-summary observations publish `OrderExecutionObservationChanged` through SignalR after commit.
- `stockMovements` is typed append-only consumption evidence and is accepted only on a report with `sourceProductionJobId`. Every item must identify the same `OrderItemId` as that job report. Each item has its own globally unique `sourceEventId`; duplicates are serialized by evidence identity and ignored even when two different reports arrive concurrently. The dispenser must belong to the reporting kiosk. Evidence freshness is independent from lifecycle projection freshness, so a sequence-stale job observation can still append previously unseen physical evidence.
- A supplied `balanceAfter` updates the observed dispenser estimate. Without it, Cloud records evidence without guessing a new balance. Inventory evidence does not gate runtime-menu sellability or order creation in V1.
- Applied stock evidence publishes `InventoryChanged` after commit. Do not encode authoritative stock adjustments only inside `payloadJson`.

### Configuration Sync

There is no current `GET /api/v1/iot/execution-endpoints/{endpointId}/configuration` endpoint.

Current production configuration distribution uses the durable command flow:

```text
Published ConfigurationRelease
-> DeployConfiguration EdgeCommand
-> authenticated command pull
-> short-lived artifact download URLs
-> local size/checksum verification
-> Installed report
-> Active report
```

Cloud ships immutable release/program manifests and ordered `RobotArtifact` descriptors. `RobotProgramArtifact.RunOrder` defines artifact execution order, and optional `RequiredOptionCode` controls whether an option-specific file participates for an order line. Cloud does not ship `RobotProgramStep`, motion commands, Blockly trees, teaching points, or realtime robot steps.

A future catalog/menu snapshot endpoint is a separate contract. It must not reintroduce the removed step-first robot configuration model or duplicate the deployment command path.

## Related Docs

- [IoT Contract](IOT_CONTRACT.md)
- [MQTT Operations](../operations/MQTT_OPERATIONS.md)
- [Robot Lua Artifact Flow](../flows/ROBOT_LUA_ARTIFACT_FLOW.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
````

## File: docs/operations/MQTT_OPERATIONS.md
````markdown
# MQTT Operations

## Search Keywords

`MQTT`, `Mosquitto`, `broker`, `execution endpoint`, `MQTT credential`, `topic ACL`, `wake-up notification`, `command pull`, `TLS`

## Ownership

MQTT has two transport roles:

- Cloud-to-Edge command-available wake-up remains best effort. `EdgeCommand` in
  PostgreSQL and authenticated command pull remain authoritative.
- Edge-to-Cloud telemetry, readiness, execution reports, production events, and
  state summaries use typed QoS 1 uplink messages. The owning Application
  handler and committed Cloud state remain authoritative; broker acceptance is
  not business acceptance.

```text
Cloud commits EdgeCommand
-> Cloud publishes CommandAvailable
-> Edge receives endpoint-scoped wake-up
-> Edge calls command pull
-> Edge validates, accepts, and executes the durable command
```

The wake-up contains no executable payload. Duplicate and missing MQTT messages are expected and harmless because Edge deduplicates pulled commands and also polls periodically.

For uplink, Edge keeps every message in its local outbox until Cloud publishes a
matching application result. Missing results are retried with the same message
and evidence identity. HTTPS ingest remains a recovery fallback and invokes the
same handlers.

## Local Broker

Local development uses Mosquitto from the independent `IceBot-Tools` lifecycle:

```powershell
cd ..\IceBot-Tools\docker
$env:MQTT_BACKEND_PASSWORD = "local-backend-secret"
$env:MQTT_DYNSEC_ADMIN_PASSWORD = "local-dynsec-admin-secret"
docker compose --profile mqtt up -d mqtt-init mosquitto
```

Local Mosquitto uses the Dynamic Security plugin. Bootstrap creates a backend
publisher role and a shared endpoint subscriber role whose `%u` username is
restricted to its endpoint topic.

Enable backend credential administration without committing the dynsec admin secret:

```powershell
$env:MqttCredentialProvisioning__Enabled = "true"
$env:MqttCredentialProvisioning__AdminPassword = $env:MQTT_DYNSEC_ADMIN_PASSWORD
$env:MqttCredentialProvisioning__RetryCount = "1"
$env:MqttCredentialProvisioning__RetryDelayMilliseconds = "500"
```

Provision one MQTT subscriber after its execution endpoint is active:

```powershell
.\mqtt\provision-endpoint.ps1 `
  -ExecutionEndpointId "00000000-0000-0000-0000-000000000000" `
  -BearerToken "management-jwt"
```

Enable backend publishing without committing credentials:

```powershell
$env:EdgeCommandMqtt__Enabled = "true"
$env:EdgeCommandMqtt__Host = "localhost"
$env:EdgeCommandMqtt__Port = "1883"
$env:EdgeCommandMqtt__Username = "icebot-backend"
$env:EdgeCommandMqtt__Password = "local-backend-secret"
$env:EdgeCommandMqtt__PublishTimeoutSeconds = "6"
$env:EdgeCommandMqtt__PublishRetryCount = "1"
$env:EdgeCommandMqtt__PublishRetryDelayMilliseconds = "250"
dotnet run --project .\src\WebAPI\WebAPI.csproj
```

Local ACL boundary:

- `icebot-backend` may publish
  `icebot/execution-endpoints/+/commands/available` and
  `icebot/execution-endpoints/+/uplink/results`.
- Cloud uplink consumers use the shared group `icebot-cloud-uplink` and subscribe
  to one exact wildcard topic per allowed message type. They do not subscribe
  to `uplink/results`.
- An Edge MQTT username must equal its `executionEndpointId` UUID.
- That endpoint may subscribe only to its `commands/available` and
  `uplink/results` topics.
- That endpoint may publish only the allowed typed message names below its own
  `icebot/execution-endpoints/{executionEndpointId}/uplink/` prefix. It cannot
  publish `results` or another endpoint's messages.
- No anonymous access is enabled.
- The management API creates or rotates the broker client through Mosquitto
  Dynamic Security. PostgreSQL stores lifecycle metadata only; it never stores
  the MQTT password or broker password hash.
- Provision/rotate returns the generated password once. Rotation invalidates
  the previous password immediately; hot overlap is intentionally not V1.

EMQX may replace Mosquitto locally when its dashboard or richer policy tooling is useful, but it must preserve the same topic and endpoint identity boundary.

## Edge Subscriber

After connecting, Edge subscribes with QoS 1 to exactly:

```text
icebot/execution-endpoints/{executionEndpointId}/commands/available
```

For every notification, including duplicates, Edge calls:

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/commands/pull
```

Edge must also pull on a periodic timer and immediately after reconnect. MQTT receipt does not mark a command Delivered or Accepted; only the command pull/ack contracts do that.

## Edge Uplink

Publish topic:

```text
icebot/execution-endpoints/{executionEndpointId}/uplink/{messageType}
```

Allowed message types:

```text
heartbeat
telemetry-events
readiness
execution-report
production-events
state-summaries
```

Every payload uses this envelope:

```json
{
  "schemaVersion": 1,
  "messageId": "uuid",
  "sentAt": "2026-07-29T10:00:00Z",
  "payload": {}
}
```

Cloud result topic:

```text
icebot/execution-endpoints/{executionEndpointId}/uplink/results
```

Result:

```json
{
  "schemaVersion": 1,
  "messageId": "uuid",
  "endpointId": "uuid",
  "messageType": "readiness",
  "processedAt": "2026-07-29T10:00:01Z",
  "succeeded": true,
  "statusCode": 200,
  "retryable": false,
  "message": "Execution readiness applied.",
  "data": {}
}
```

Rules:

- QoS is 1 and retain is false in both directions.
- Broker PUBACK means only that the broker accepted the message.
- `messageId` correlates transport results; domain idempotency remains the
  event ID, sequence, revision, or command/report identity inside `payload`.
- Edge removes an outbox entry only after a matching successful or
  non-retryable application result.
- `retryable=true`, result timeout, disconnect, or broker failure retries the
  identical envelope and domain evidence identities.
- A `207` batch result is transport success; Edge must inspect per-item results
  and retain only failed/retryable items as defined by the owning contract.
- Message type selects a strict typed payload. Unknown fields, unknown enum
  values, unsupported schema versions, and topic/message mismatches are
  rejected.
- MQTT and HTTPS must use the same persistent source executor identity,
  event IDs, sequence numbers, state revisions, and command IDs.
- MQTT is not used for command pull/ack, checkpoint reads, artifact/file
  transfer, or signed object download.

## Production

Production must use:

- a broker endpoint reachable only from approved backend and Edge networks;
- TLS (`EdgeCommandMqtt__UseTls=true`) with a trusted broker certificate;
- a unique backend client id per backend instance;
- backend publish/subscribe credentials stored in the deployment secret manager;
- one revocable MQTT identity per execution endpoint;
- endpoint-scoped bidirectional ACLs and separate backend publish/subscribe ACLs;
- broker connection, authentication failure, publish failure, and client-session metrics;
- credential rotation coordinated with execution-endpoint provisioning.

HTTPS and MQTT credentials are separate. Never reuse the mTLS certificate or
signed-request credential as the MQTT password. Dynsec administrator and
backend publisher credentials come from deployment secrets, never appsettings
or Git.

Management lifecycle:

```text
POST   /api/v1/management/kiosks/{kioskId}/execution-endpoints/{id}/mqtt-credential   provision
PATCH  /api/v1/management/kiosks/{kioskId}/execution-endpoints/{id}/mqtt-credential   rotate
DELETE /api/v1/management/kiosks/{kioskId}/execution-endpoints/{id}/mqtt-credential   revoke
```

Provision and rotation return the password once together with:

- `subscribeTopic` for command wake-up;
- `uplinkPublishTopicPattern` for the allowed typed uplink message name;
- `uplinkResultTopic` for Cloud application results.

Broker mutation and database audit cannot share a distributed transaction.
The handler commits a durable `PendingProvision`, `PendingRotation`, or
`PendingRevoke` intent before broker I/O, uses idempotent broker mutation, and
finalizes only the credential version that created that intent. Broker failure
is recorded as `Failed` or `RevokeFailed`. A recent pending operation rejects a
concurrent mutation. The reconciliation job processes operations that remain
pending for five minutes: provisioning and rotation become `Failed` because the
one-time secret cannot be recovered after a crash, while revocation is claimed
with a new credential version and retried automatically. A stale `RevokeFailed`
operation is also retried. Provisioning or rotation can then be retried through
the same management endpoint, which replaces any broker credential whose secret
was not returned. Cancellation or a process crash cannot roll back a broker
mutation that already occurred. Durable command polling remains available while
MQTT credentials are repaired.

The job runs only when `MqttCredentialProvisioning:Enabled` is true. Configure
`ReconciliationIntervalSeconds` and `ReconciliationBatchSize` for scan cadence
and bounded work. It reports through the `mqtt_credential_reconciliation`
operational-automation metric and logs endpoint-level outcomes that require an
operator retry.

The dedicated `IceBot.MqttCredentialLifecycle` meter reports stale candidate
count, timeout transitions, reconciliation outcomes, and automatic revocation
retry results. A lease timeout opens or correlates
`MQTT_CREDENTIAL_OPERATION_TIMEOUT`; a failed automatic revocation retry opens
or correlates `MQTT_CREDENTIAL_REVOKE_FAILED`. These Error alerts are visible in
the normal tenant-scoped alert queue and through `AlertChanged`. A successful
manual or automatic repair resolves the active alert during the next scan.
An endpoint with a non-revoked MQTT credential cannot be retired; revoke the
broker client first. Disabling an endpoint blocks command pull/dispatch but does
not rotate or delete its MQTT identity, so reactivation can preserve the same
subscriber setup.

For multiple backend replicas, each replica needs a unique MQTT client id and
the same uplink consumer group. Shared subscriptions distribute each uplink
message to one replica. Retained wake-ups and uplink messages remain disabled;
durable command recovery comes from command pull, while durable uplink recovery
comes from the Edge local outbox and Cloud application result.

Backend wake-up outcomes are exported through
`icebot.mqtt.wakeup.publish.attempts`. Uplink outcomes and processing latency
use `icebot.mqtt.uplink.messages` and
`icebot.mqtt.uplink.processing.latency`. Broker-side
connection/session/authentication metrics remain owned by Mosquitto/EMQX and
should be scraped or exported separately; application metrics cannot observe
disconnects or rejected broker ACL operations that never reach the backend.

## Related Docs

- [Edge Command Contract](../iot/EDGE_COMMAND_CONTRACT.md)
- [Deployment Configuration](DEPLOYMENT_CONFIG.md)
- [Observability](OBSERVABILITY.md)
- [Restart And Power Recovery](RESTART_AND_POWER_RECOVERY.md)
````

## File: docs/operations/OBSERVABILITY.md
````markdown
# Observability

## Search Keywords

`observability`, `Serilog`, `OpenTelemetry`, `OTLP`, `Aspire Dashboard`, `trace`, `metric`, `structured log`, `debug body logging`

This document outlines the observability strategy for IceBot Backend. It uses a combination of **Serilog** for structured logging and **OpenTelemetry** for traces, metrics, and correlation.

## 1. Architecture Strategy

The observability boundary is separated into roles to avoid duplicating log noise and to keep systems focused:

- **Serilog**: The structured logging pipeline. Responsible for application logs, console output, and file-based logs (or forwarding to Seq/Loki).
- **OpenTelemetry (OTel)**: Handles traces, metrics, and correlation.
- **Aspire Dashboard**: The local developer tool for visualizing traces and metrics.
- **Debug Body Logging**: A temporary, config-gated "microscope" for debugging raw HTTP payloads.

> [!WARNING]
> Do not treat OpenTelemetry as a Serilog replacement. Logs remain owned by Serilog. When logs need to flow to OTLP/Aspire/collector, use the Serilog OpenTelemetry sink, not `OpenTelemetryLoggerProvider`.

## 2. Local Aspire Dashboard

To visualize OpenTelemetry traces, metrics, and optional Serilog OTLP logs locally, run the Aspire Dashboard from the sibling `IceBot-Tools` tooling compose:

```powershell
cd ..\IceBot-Tools
.\scripts\start_aspire_dashboard.ps1
```

- **UI endpoint**: `http://localhost:18888`
- **OTLP endpoint**: `http://localhost:18889`
- The dashboard container is owned by `IceBot-Tools` because it is tooling/dev observability, not a backend runtime dependency.
- The backend must still run without cloning or starting `IceBot-Tools`.

> [!NOTE]
> Aspire Dashboard is for local development **only**. Do not expose it to the public network.

## 3. Configuration

Observability settings are managed in `appsettings.json` under the `Observability` block:

```json
"Observability": {
  "ServiceName": "IceBot.WebAPI",
  "Serilog": {
    "OtlpSinkEnabled": false
  },
  "OpenTelemetry": {
    "Enabled": true,
    "OtlpExporterEnabled": false,
    "OtlpEndpoint": "http://localhost:18889",
    "OtlpProtocol": "grpc"
  },
  "DebugBodyLogging": {
    "Enabled": false,
    "LogRequestBody": true,
    "LogResponseBody": false,
    "MaxBodyLength": 1000
  }
}
```

### OTLP Exporter

- Set `OpenTelemetry:OtlpExporterEnabled: true` to export traces and metrics to the Aspire Dashboard or a production OTLP collector.
- Set `Serilog:OtlpSinkEnabled: true` to export structured logs through the Serilog OpenTelemetry sink to the same OTLP endpoint.
- `OtlpEndpoint` defines the destination.
- In `appsettings.Development.json`, both OTLP exporters default to `false` so the app doesn't depend on Aspire being available.

Serilog OTLP logging is separate from OpenTelemetry traces/metrics:

```text
ILogger<T>
  -> Serilog
      -> console/file
      -> optional Serilog OTLP sink

OpenTelemetry SDK
  -> traces/metrics
      -> optional OTLP exporter
```

Do not add `OpenTelemetryLoggerProvider` unless this decision is explicitly revisited. It would create a second logging provider path and blur the boundary that Serilog owns application logs.

### Debug Body Logging

Dashboard and OTel act as your "radar" (showing what failed and where). Debug body logging is your "microscope" (showing the exact payload).

- Body logging is **disabled by default**.
- It is expensive and risks exposing sensitive data.
- When `Enabled: true`, it safely truncates payloads over `MaxBodyLength` and masks sensitive fields (like passwords/tokens).
- Authentication, password, and webhook endpoints are explicitly ignored by the middleware for safety.

## 4. Edge Runtime Metrics

The `IceBot.EdgeIntegration` meter is registered with the existing OpenTelemetry metrics pipeline. These metrics describe machine-integration latency and failure; ASP.NET instrumentation continues to own ordinary HTTP duration/error metrics.

The `IceBot.Payments.PayOS` meter adds provider-specific failure classification on top of ordinary HTTP client metrics. Payment identifiers are intentionally excluded from metric tags.

| Metric | Type | Meaning | Bounded tags |
| --- | --- | --- | --- |
| `icebot.mqtt.wakeup.publish.attempts` | Counter | MQTT wake-up outcomes, including disabled/succeeded/failed | `outcome`, `command.type` |
| `icebot.mqtt.uplink.messages` | Counter | MQTT Edge uplink application outcomes | `outcome`, `message.type` |
| `icebot.mqtt.uplink.processing.latency` | Histogram (seconds) | Broker delivery to application result publication | `message.type` |
| `icebot.mqtt.credentials.reconciliation.outcomes` | Counter | Durable MQTT credential reconciliation outcomes | `outcome` |
| `icebot.mqtt.credentials.operation.timeouts` | Counter | Provisioning or rotation operations that exceeded the five-minute lease | `operation` |
| `icebot.mqtt.credentials.revocation.retry.attempts` | Counter | Automatic stale revocation retries | `outcome` |
| `icebot.mqtt.credentials.stale.candidates` | Observable gauge | Stale credential operations selected by the latest scan | none |
| `icebot.payos.request.failures` | Counter | Final PayOS timeout, open-circuit, or transient failures | `provider`, `operation`, `failure.kind` |
| `icebot.payment_session.reconciliation.outcomes` | Counter | Recovery outcomes for incomplete provider-session creation responses | `outcome` |
| `icebot.payment_session.interventions` | Counter | Sessions requiring operator investigation after terminal reconciliation outcomes | `intervention` |
| `icebot.payment_session.reconciliation.pending_age` | Histogram (seconds) | Age of an incomplete session when a reconciliation attempt starts | none |
| `icebot.production_package.upgrade.preview` | Counter | Upgrade preview outcomes | `outcome`, `has_blockers` |
| `icebot.production_package.upgrade.materialization` | Counter | Successor materialization outcomes | `outcome` |
| `icebot.production_package.upgrade.cutover` | Counter | Cutover outcomes | `outcome` |
| `icebot.production_package.upgrade.rollback_attempt` | Counter | Created rollback deployment attempts | `profile`, `attempt_no` |
| `icebot.production_package.upgrade.rollback` | Counter | Aggregate rollback outcomes | `outcome` |
| `icebot.production_package.upgrade.rollback_pending_age` | Histogram (seconds) | Age of an upgrade waiting for rollback activation | none |
| `icebot.notification_delivery.outcomes` | Counter | Durable push delivery outcomes | `status`, `notification.type` |
| `icebot.notification_delivery.processing_lag` | Histogram (seconds) | Time from scheduled attempt to worker claim | none |
| `icebot.notification_delivery.due_batch_size` | Histogram | Number of due deliveries selected per scan | none |
| `icebot.notification.push.timed_out` | Counter | Firebase push attempts stopped by the per-delivery operation timeout; retry remains owned by the durable delivery worker | none |
| `icebot.automation.runs` | Counter | Outcome of operational reconciliation and retention runs | `automation.job`, `outcome` |
| `icebot.automation.candidate.failures` | Counter | Candidate-level failures isolated so later candidates or stages can continue | `automation.job` |
| `icebot.automation.run.duration` | Histogram (seconds) | Duration of each operational automation run or reconciliation stage | `automation.job`, `outcome` |
| `icebot.automation.last_success.unix_time` | Observable gauge | Unix timestamp of the last fully successful automation run | `automation.job` |
| `icebot.edge.command.pull.latency` | Histogram (seconds) | Durable command creation until it is returned by command pull | `command.type` |
| `icebot.edge.command.ack.latency` | Histogram (seconds) | Command delivery until Cloud receives the first state-changing ACK | `command.type`, `ack.status` |
| `icebot.edge.execution.report.lag` | Histogram (seconds) | Executor-reported timestamp until Cloud receives a new report | `report.type` |
| `icebot.edge.execution.observation.transitions` | Counter | Transitions to stale/unreachable customer observations | `observation.status`, `customer.status` |
| `icebot.edge.execution.stale.age` | Histogram (seconds) | Age of the last executor report at stale/unreachable transition | `observation.status` |
| `icebot.edge.execution.observed` | Observable gauge | Current active execution projections that are Stale or Unreachable | `observation.status` |

Rules:

- Business-state metrics are recorded only after their owning database commit
  succeeds. MQTT transport rejection metrics may be recorded before a handler
  or database transaction exists.
- Duplicate ACKs and duplicate execution reports do not add latency/transition measurements.
- The stale/unreachable gauge is refreshed from PostgreSQL every 30 seconds; it is not an in-memory lifecycle counter.
- IDs such as command, order, kiosk, endpoint, or device must never be metric tags. Use traces/logs for entity-level investigation.
- Production-package upgrade tags are bounded outcomes only. Endpoint and upgrade identities remain in the typed detail/audit read model.
- Notification metrics use bounded status/type tags only. Delivery, account, kiosk, and tenant identifiers remain in diagnostics reads and logs.
- PayOS `failure.kind` is bounded to `timeout`, `circuit_open`, and `transient`; HTTP payment-creation `POST` requests are not retried.
- Payment-session reconciliation uses the persisted provider order code. `AwaitingWebhook` means provider lookup reported paid while Cloud is still waiting for the signed webhook; it must be investigated through order-scoped payment diagnostics rather than treated as fulfillment success.
- Alert on any sustained increase of `icebot.payment_session.interventions`, especially `AwaitingWebhook`, `IdentityMismatch`, and `AmountMismatch`. Use the tenant-scoped intervention queue to identify affected orders; metric tags intentionally contain no payment or order IDs.
- MQTT disabled is an explicit outcome, not a publish failure. Alert only on `outcome=failed` when MQTT is expected to be enabled.
- Alert on sustained MQTT uplink `retryable`, `malformed`, or `invalid_size`
  outcomes. Broker ACL rejection and disconnected clients require broker-side
  metrics because those messages never reach the application consumer.
- MQTT credential metrics use only bounded operation/outcome tags. Endpoint identity remains in structured logs and management alerts.
- Alert when an enabled automation job has no recent `last_success` update, when `partial_failure` is sustained, or when candidate failures grow. Inspect logs by job and candidate ID; IDs deliberately remain out of metric tags.

Suggested initial alerts:

- sustained increase of MQTT `failed` outcomes;
- any sustained non-zero `icebot.mqtt.credentials.stale.candidates` gauge;
- growth of `icebot.mqtt.credentials.operation.timeouts` or failed MQTT credential revocation retries;
- any `icebot.payment_session.interventions{intervention="IdentityMismatch"}` or
  `{intervention="AmountMismatch"}` occurrence;
- sustained non-zero
  `icebot.payment_session.interventions{intervention="AwaitingWebhook"}` or
  `{intervention="RetryExhausted"}`;
- payment-session reconciliation pending-age p95 above the configured stale and
  retry budget;
- p95 pull or ACK latency above the command expiry budget;
- p95 report lag above the report reconciliation threshold;
- non-zero Unreachable gauge for a sustained interval;
- growing Stale gauge combined with low heartbeat freshness.
- no successful operational-automation run within two expected job intervals; and
- sustained `icebot.notification.push.timed_out` growth while notification delivery remains enabled.

Exact thresholds are deployment-specific and should be tuned from observed baselines rather than hardcoded in application code.

## 5. Production Guidance

For production environments:
1. **Logs**: Continue using Serilog. You can add a Serilog sink to export directly to Seq, Loki, or Elasticsearch.
2. **Logs via OTLP**: If using an OTLP collector, enable `Observability:Serilog:OtlpSinkEnabled=true`.
3. **Traces/Metrics**: Enable the OTLP exporter (`Observability:OpenTelemetry:OtlpExporterEnabled=true`) and point `OtlpEndpoint` to an OpenTelemetry Collector or APM ingest endpoint (e.g., Jaeger, Prometheus, Datadog).
4. **Debug Body Logging**: Keep `Observability:DebugBodyLogging:Enabled = false` unless actively diagnosing a live production payload issue.

## Related Docs

- [Deployment Configuration](DEPLOYMENT_CONFIG.md)
- [MQTT Operations](MQTT_OPERATIONS.md)
- [Alert Lifecycle Flow](../flows/ALERT_LIFECYCLE_FLOW.md)
- [Restart And Power Recovery](RESTART_AND_POWER_RECOVERY.md)
````

## File: docs/DOCUMENTATION_ROUTING_MAP.md
````markdown
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
| Deployment runtime config, environment variables, appsettings, health/info | [Deployment Configuration](operations/DEPLOYMENT_CONFIG.md) | [API Surface Rules](api/API_SURFACE_RULES.md) |
| Observability, Serilog, OpenTelemetry, Aspire Dashboard, debug body logging, OTLP | [Observability](operations/OBSERVABILITY.md) | [Deployment Configuration](operations/DEPLOYMENT_CONFIG.md) |
| Manual backend critical rule checks before handoff | [Backend Critical Rule Checklist](process/BACKEND_CRITICAL_RULE_CHECKLIST.md) | [Working Protocol](process/WORKING_PROTOCOL.md) |
| How backend docs should be structured for RAG/search | [Documentation Rules](process/DOCUMENTATION_RULES.md) | this file |
| Which module owns a topic, its primary contract/flow, and its verification entry point | [Documentation Coverage Matrix](DOCUMENTATION_COVERAGE.md) | [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md), [System Flows](flows/SYSTEM_FLOWS.md) |
| Domain ownership, entity belongs to which bounded context | [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md) | [Dependency Rules](architecture/DEPENDENCY_RULES.md) |
| Layer dependency, repository, DbContext, application/domain/infrastructure boundary | [Dependency Rules](architecture/DEPENDENCY_RULES.md) | [Architecture](../ARCHITECTURE.md) |
| Route prefixes, API surface, missing API surface, tablet vs management vs auth vs IoT API, GraphQL vs REST | [API Surface Rules](api/API_SURFACE_RULES.md) | [Management API Surface](api/MANAGEMENT_API_SURFACE.md), [Naming Rules](process/NAMING_RULES.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| Authentication endpoints, forgot/reset/change password, current account routes | [API Surface Rules](api/API_SURFACE_RULES.md) | [Identity Onboarding Rules](api/IDENTITY_ONBOARDING_RULES.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| Admin-created account onboarding, invitation link, accept invitation, temporary password fallback | [Identity Onboarding Rules](api/IDENTITY_ONBOARDING_RULES.md) | [API Surface Rules](api/API_SURFACE_RULES.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| Role policy, scoped RBAC, SystemAdmin/Manager/Staff/Technician/OrgAdmin | [Authorization Rules](api/AUTHORIZATION_RULES.md) | [API Surface Rules](api/API_SURFACE_RULES.md), [Multi-Tenancy Rules](architecture/MULTI_TENANCY_RULES.md) |
| Naming conventions for entities, fields, APIs, application use cases | [Naming Rules](process/NAMING_RULES.md) | [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md), [API Surface Rules](api/API_SURFACE_RULES.md) |
| EF Core indexes, soft delete, unique constraints, snapshots, partitioning | [Data Modeling Rules](data/DATA_MODELING_RULES.md) | [JSON Field Rules](data/JSON_FIELD_RULES.md), [Idempotency and Retry Rules](data/IDEMPOTENCY_RETRY_RULES.md) |
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
| Fairino `.lua` export, authoring ZIP/sidecar, global RobotArtifactTemplate, organization clone, RobotArtifact upload, RobotProgram RunOrder | [Robot Lua Authoring And Import Flow](flows/ROBOT_LUA_AUTHORING_AND_IMPORT_FLOW.md) | [Robot Lua Artifact Flow](flows/ROBOT_LUA_ARTIFACT_FLOW.md), [API Surface Rules](api/API_SURFACE_RULES.md) |
| Configuration release, execution endpoint provisioning, deployment preview, presigned artifact download, activation, rollback | [Robot Lua Deployment And Activation Flow](flows/ROBOT_LUA_DEPLOYMENT_AND_ACTIVATION_FLOW.md) | [Robot Lua Artifact Flow](flows/ROBOT_LUA_ARTIFACT_FLOW.md), [IoT Contract](iot/IOT_CONTRACT.md), [API Surface Rules](api/API_SURFACE_RULES.md) |
| Fairino `.lua` artifact lifecycle boundary and end-to-end index | [Robot Lua Artifact Flow](flows/ROBOT_LUA_ARTIFACT_FLOW.md) | [IoT Contract](iot/IOT_CONTRACT.md), [API Surface Rules](api/API_SURFACE_RULES.md) |
| Production package, franchise installation, deterministic RobotProgram composition, artifact technical contract, production-definition checksum, deployment acknowledgement | [Production Package Installation Flow](flows/PRODUCTION_PACKAGE_INSTALLATION_FLOW.md) | [Robot Lua Artifact Flow](flows/ROBOT_LUA_ARTIFACT_FLOW.md), [API Surface Rules](api/API_SURFACE_RULES.md) |
| Production Package upgrade, preview checksum, materialization, cutover, rollback, abandonment, stale reconciliation | [Production Package Upgrade Flow](flows/PRODUCTION_PACKAGE_UPGRADE_FLOW.md) | [Production Package Installation Flow](flows/PRODUCTION_PACKAGE_INSTALLATION_FLOW.md), [Idempotency and Retry Rules](data/IDEMPOTENCY_RETRY_RULES.md) |
| Robot artifact migration, MinIO setup, operational smoke, Edge/controller integration test | [Robot Artifact Operational Smoke Test](operations/ROBOT_ARTIFACT_OPERATIONAL_SMOKE.md) | [Deployment Configuration](operations/DEPLOYMENT_CONFIG.md), [Robot Lua Artifact Flow](flows/ROBOT_LUA_ARTIFACT_FLOW.md) |
| Tablet/cloud/edge/payment/MQTT/execution flow | [Checkout Execution Flow](flows/CHECKOUT_EXECUTION_FLOW.md) | [IoT Contract](iot/IOT_CONTRACT.md), [API Surface Rules](api/API_SURFACE_RULES.md) |
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
| `observability`, `Serilog`, `OpenTelemetry`, `Aspire`, `OTLP`, `trace`, `metric`, `debug body logging` | [Observability](operations/OBSERVABILITY.md), [Deployment Configuration](operations/DEPLOYMENT_CONFIG.md) |
| `manual verification`, `critical rule`, `smoke checklist`, `maintenance lifecycle test`, `payment webhook test` | [Backend Critical Rule Checklist](process/BACKEND_CRITICAL_RULE_CHECKLIST.md) |
| `wide scan`, `horizontal audit`, `finding ledger`, `root cause`, `pattern scan`, `vertical slice`, `invariant`, `failure path`, `scope freeze`, `completion evidence`, `poison item`, `independent diff review` | [Vertical Slice Review](process/VERTICAL_SLICE_REVIEW.md) |
| `invitation`, `accept invitation`, `admin creates account`, `temporary password`, `CreateInvitation`, `SendInvitationEmail` | [Identity Onboarding Rules](api/IDENTITY_ONBOARDING_RULES.md) |
| `management accounts`, `role scope`, `RBAC`, `policy`, `role catalog`, `permission matrix`, `roles.view`, `role-scope-options.view`, `effective access`, `me access` | [Authorization Rules](api/AUTHORIZATION_RULES.md), [API Surface Rules](api/API_SURFACE_RULES.md) |
| `store`, `organization`, `kiosk`, `tenant scope`, `role scope options` | [Multi-Tenancy Rules](architecture/MULTI_TENANCY_RULES.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| `order paid`, `ready for execution`, `tablet status`, `post-payment fan-out` | [Checkout Execution Flow](flows/CHECKOUT_EXECUTION_FLOW.md), [IoT Contract](iot/IOT_CONTRACT.md) |
| `refund required`, `edge offline`, `duplicate notification`, `retry` | [Failure Flow Index](flows/FAILURE_FLOW_INDEX.md), [Idempotency and Retry Rules](data/IDEMPOTENCY_RETRY_RULES.md) |
| `system overview`, `setup to sale`, `back-office setup`, `catalog to runtime menu`, `operations support`, `management dashboard` | [System Flows](flows/SYSTEM_FLOWS.md), then the matching flow file |
| `management orders`, `order status history`, `execution attempts`, `sourceCommandId`, `production execution record`, `refund`, `manual refund`, `stock movement`, `dispenser state`, `inventory refill` | [Management API Surface](api/MANAGEMENT_API_SURFACE.md), [Checkout Execution Flow](flows/CHECKOUT_EXECUTION_FLOW.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| `GraphQL`, `REST`, `read model aggregation`, `dashboard query`, `overview query` | [API Surface Rules](api/API_SURFACE_RULES.md), section `GraphQL Management Reads` |
| `SignalR`, `hub`, `OrderHub`, `OperationsHub`, `ManagementDashboardHub`, `realtime`, `DashboardInvalidated` | [SignalR Realtime Contract](api/SIGNALR_REALTIME_CONTRACT.md), [SignalR Smoke Test](operations/SIGNALR_SMOKE_TEST.md) |
| `heartbeat`, `readiness`, `capability projection`, `device event`, `kiosk event`, `production event checkpoint`, `state revision`, `operations telemetry`, `lost connection` | [Edge Sync and Telemetry Contract](iot/EDGE_SYNC_TELEMETRY_CONTRACT.md), [API Surface Rules](api/API_SURFACE_RULES.md), [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md) |
| `maintenance ticket`, `support ticket`, `technician assignment`, `maintenance.create`, `maintenance.manage` | [Maintenance Ticket Flow](flows/MAINTENANCE_TICKET_FLOW.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| `alert`, `actionable telemetry`, `acknowledge alert`, `resolve alert`, `alerts.manage` | [Alert Lifecycle Flow](flows/ALERT_LIFECYCLE_FLOW.md), [IoT Contract](iot/IOT_CONTRACT.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
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
````

## File: docs/architecture/BOUNDARY_CONTEXTS.md
````markdown
Exit code: 0
Wall time: 0.1 seconds
Output:
# Boundary Contexts

This project keeps one Domain project, but domain entities are grouped by bounded context. The folder and namespace should describe business ownership, not technical implementation.

## Search Keywords

`bounded context`, `domain ownership`, `Domain.Identity`, `Domain.Tenants`, `Domain.Catalog`, `Domain.SalesCatalog`, `Domain.Orders`, `Domain.Payments`, `Domain.RobotConfiguration`, `Domain.ProductionConfiguration`, `Domain.ProductionExecution`, `Domain.Devices`, `Domain.Inventory`, `Domain.Operations`, `Domain.Sync`, `Domain.Common`, `ProductVariant`, `MenuItem`, `RobotArtifact`, `RobotProgram`, `ConfigurationRelease`, `EdgeCommand`, `SyncEventInbox`

## Bounded Context Ownership

### Ownership Lookup

| Context | Namespace | Owns |
| --- | --- | --- |
| Identity | `Domain.Identity` | accounts, roles, login devices, refresh tokens, password reset requests |
| Tenants | `Domain.Tenants` | organizations, stores, kiosks, tenant scope |
| Catalog | `Domain.Catalog` | product definitions, variants, options, recipes, ingredients |
| Sales Catalog | `Domain.SalesCatalog` | menus, menu items, sellable offers, pricing |
| Orders | `Domain.Orders` | order lifecycle, order items, historical order snapshots |
| Payments | `Domain.Payments` | payment transactions, callbacks, refunds, payment methods |
| Robot Configuration | `Domain.RobotConfiguration` | robot Lua artifacts and reusable robot manifests |
| Production Configuration | `Domain.ProductionConfiguration` | configuration releases, routes, robot bindings and deployment records |
| Production Packages | `Domain.ProductionPackages` | reusable package/version manifests, deterministic installation provenance, and composition audit; references Catalog and RobotConfiguration by IDs/snapshots |
| Production Execution | `Domain.ProductionExecution` | Cloud execution projections from executor evidence |
| Devices | `Domain.Devices` | device catalog, telemetry, heartbeats and kiosk execution endpoints |
| Inventory | `Domain.Inventory` | dispenser state, stock movements |
| Operations | `Domain.Operations` | alerts, maintenance tickets, operation logs |
| Sync | `Domain.Sync` | edge-cloud inbox/dead letters and dispatch-only edge commands |
| Common | `Domain.Common` | base entities, shared abstractions, shared primitives |

### Identity

Namespace: `Domain.Identity`

Owns accounts, roles, notification-device registrations, and refresh tokens.

`AccountNotificationDevice` is an FCM installation registry. Firebase delivery targets active registrations by account and invalidates provider-rejected tokens; recipient policy belongs to the calling product workflow. It is not a trusted-session or login-security model.

Entities:

- `Account`
- `AccountNotificationDevice`
- `PasswordResetRequest`
- `RefreshToken`
- `Role`

`PasswordResetRequest` is separated from `Account` because password recovery has its own token lifecycle, expiry, usage, and audit evidence.

### Tenants

Namespace: `Domain.Tenants`

Owns the business deployment hierarchy: organization, store, kiosk. A kiosk is an edge/business deployment unit, not just a device.

Entities:

- `Organization`
- `Store`
- `Kiosk`

Enums:

- `KioskStatus`
- `TenantScopeType`

`TenantScopeType` is shared by multiple contexts, but it belongs here because it models tenant override scope: global, organization, store, kiosk, device.

### Catalog

Namespace: `Domain.Catalog`

Owns product definitions, product variants, product options, recipes, and ingredient definitions used to describe products.

Entities:

- `Product`
- `ProductVariant`
- `ProductCategory`
- `ProductOption`
- `OptionGroup`
- `Recipe`
- `RecipeItem`
- `Ingredient`

`Product` owns `OptionGroup`, and `OptionGroup` owns `ProductOption`. Options inherit Product tenant scope and currency. `MenuItem` stores only selected option ids as Sales Catalog membership; placed orders store immutable `OrderItemOption` snapshots rather than live Catalog references.

`Recipe` belongs to one `ProductVariant` and inherits the owning Product tenant scope. `RecipeItem` declares ingredient requirements and ordering for recipe data; it is not robot motion or artifact execution orchestration. Ingredients are global reference definitions in V1, while Inventory owns kiosk/device dispenser state and stock movement.

### Sales Catalog

Namespace: `Domain.SalesCatalog`

Owns menus and menu items: the products/recipes currently offered for sale in a tenant/store/kiosk context, including sellable price and availability windows.

Entities:

- `Menu`
- `MenuItem`

`MenuItem` is a domain concept, not just a database mapping. It represents a sellable offer that points to Catalog product variant/recipe data and provides order pricing.

### Orders

Namespace: `Domain.Orders`

Owns customer order lifecycle and order snapshots.

Entities:

- `Order`
- `OrderItem`
- `OrderStatusHistory`
- `OrderItemStatusHistory`
- `ProductionIncident`
- `ProductionIncidentHistory`

Orders may reference catalog, payment, kiosk, and execution evidence by id or snapshot, but should not depend on mutable Edge runtime state for historical truth.
Each order item snapshots its fulfillment type. Manual lines use the strict staff preparation lifecycle; packaged lines use idempotent handoff/failure commands and never enter acceptance or preparation; machine-produced lines advance through authenticated production reports carrying order-item and production-unit identity. Item failure is represented by the Orders-owned `FulfillmentIssue` aggregate state and does not itself decide payment compensation.

Production incident resolution is also Orders-owned. It records immutable execution provenance, exact production-unit range, inspection outcome, selected operational resolution, linked remake/refund identity, and append-only actor history. It may orchestrate a Payments-owned refund request, but it does not own payment settlement or provider behavior. Inventory consumption evidence remains immutable and is not reversed merely because an output is discarded or remade.

### Payments

Namespace: `Domain.Payments`

Owns payment methods, payment attempts, provider callbacks, and refunds.

Entities:

- `PaymentMethod`
- `PaymentTransaction`
- `PaymentCallback`
- `Refund`

Provider payloads are external evidence. Idempotency and retry decisions must use typed columns.

`PaymentMethod` is a reference/status catalog. Provider credentials and provider-specific settings remain in application configuration or secret storage, not database JSON or management DTOs.

Current refund phase is manual cash refund. Auto provider refund or payout integration can be added later, but should not be assumed in the first implementation.

### Robot Configuration

Namespace: `Domain.RobotConfiguration`

Owns immutable exported robot Lua artifacts and reusable declared robot manifests.

Aggregate roots: `RobotProgram`, `RobotArtifact`.

Aggregate child: `RobotProgramArtifact`, owned only by `RobotProgram`.

`RobotProgramArtifact` keeps `RobotArtifactId` but does not hold a live `RobotArtifact` navigation because `RobotArtifact` is an independent aggregate root. Program publication receives explicit published-artifact snapshots and writes immutable `ProgramManifestJson`; release publication, deployment, and execution dispatch consume that manifest instead of traversing a live artifact graph.

Robot configuration entities keep tenant/template provenance ids without exposing cross-context or cross-aggregate navigation properties. Infrastructure preserves database foreign keys and uses explicit projections for management reads.

This context is configuration-time. It should not own runtime execution state.

`RobotProgramArtifact` represents ordered artifact membership. `RobotArtifact` does not own an inverse membership collection. This context does not persist Blockly trees, teaching points, calibration, motion coordinates or live runtime work.

### Production Configuration

Namespace: `Domain.ProductionConfiguration`

Owns organization-scoped configuration releases, release-owned route snapshots, ordered robot bindings and Cloud rollout acknowledgement records.

Aggregate roots: `ConfigurationRelease`, `KioskConfigurationDeployment`, `ControllerArtifactSetDeployment`.

Release-owned children: `ExecutionRoute`, `ExecutionRouteRobotBinding`.

Low-cost deployment child: `ControllerArtifactSetItem`.

Published releases are immutable. A route links catalog variant/recipe requirements to robot programs through bindings, rather than Catalog holding a direct program id. Release publication consumes immutable published-program/artifact snapshots; the release manifest builder does not traverse a live Robot Configuration aggregate. Deployment aggregates keep release ids/checksums and materialized item snapshots rather than owning a release graph.

### Production Execution

Namespace: `Domain.ProductionExecution`

Owns Cloud read/audit projections created from accepted executor evidence.

Entities:

- `OrderExecutionRecord`
- `ProductionExecutionRecord`

This context does not own an Edge queue, scheduler, workcell lease, local `ProductionJob`, or physical safety transition.

### Devices

Namespace: `Domain.Devices`

Owns physical device catalog, installed devices, device events, and edge telemetry.

`DeviceType` and `DeviceModel` form a global technical catalog owned by Devices. They are not tenant-scoped. Tenant-scoped `Device` records reference the catalog by ID; catalog lifecycle changes prevent future assignment without rewriting installed-device history.

Entities:

- `DeviceType`
- `DeviceModel`
- `Device`
- `DeviceEvent`
- `KioskHeartbeat`
- `KioskExecutionEndpoint`

`KioskHeartbeat` lives here because it is telemetry emitted by the edge node/device runtime. `KioskStatus` stays in Tenants because it describes the kiosk lifecycle.

### Inventory

Namespace: `Domain.Inventory`

Owns Cloud ingredient-dispenser topology, current dispenser state, and stock movement reporting.

Entities:

- `IngredientDispenserState`
- `StockMovement`

`Ingredient` remains in Catalog because it defines what a recipe uses. Inventory binds a kiosk device/container to one ingredient, owns that binding lifecycle, replacement/rebind audit, topology-change history, and stock movement history. Device retirement coordinates with Inventory so active bindings cannot outlive their owning hardware. Configuration Release does not create or rebind Cloud dispenser topology in V1.

Inventory also owns the computed operational readiness projection used by Production Configuration. Production Configuration supplies release route/Recipe identities and consumes warnings or blocking results; ownership of dispenser topology remains in Inventory.

### Operations

Namespace: `Domain.Operations`

Owns operational alerts, maintenance tickets, and operation logs.

Entities:

- `Alert`
- `MaintenanceTicket`
- `OperationLog`

### Sync

Namespace: `Domain.Sync`

Owns edge-cloud sync inbox and dead-letter handling.

Entities:

- `SyncEventInbox`
- `SyncDeadLetter`
- `EdgeCommand`
- `EdgeCommandDeliveryAttempt`

Business contexts should not depend on Sync entities. They may expose idempotency, correlation, causation, version, and origin node fields for sync infrastructure to consume.

### Common

Namespace: `Domain.Common`

Owns base entities, domain abstractions, shared exceptions, and truly shared enums.

Allowed here:

- `GuidEntity`
- `LongEntity`
- `GuidId`
- `BusinessEntity`
- `CatalogEntity`
- `RobotConfigurationEntity`
- `SyncAggregateEntity`
- `IAuditable`
- `ISoftDeletable`
- `IRobotSyncEntity`
- `IOrganizationScoped`
- `IStoreScoped`
- `IKioskScoped`
- `DomainRuleException`
- `EntityStatus`
- `SeverityLevel`

`GuidId.New()` is the shared UUID v7 generator used by `GuidEntity`.

Do not place context-specific enums here.

## Dependency Rules

Dependency and cross-layer rules live in [Dependency Rules](DEPENDENCY_RULES.md). This file only defines bounded-context ownership and intentional cross-context references.

## Current Intentional Cross-Context References

- Orders reference Tenants through `OrganizationId`, `StoreId`, and `KioskId`.
- Orders reference Sales Catalog through `MenuItemId`, and keep Catalog references through `ProductId`, `ProductVariantId`, `RecipeId`, and item snapshots.
- Payments reference Orders through `OrderId`.
- Production Configuration references Catalog and Robot Configuration by ids/snapshots.
- Production Execution retains executor evidence by source command, endpoint, release and order ids.
- Inventory references Devices, Tenants, Catalog ingredients, and dispenser state.
- Operations references Accounts, Devices, Orders, execution evidence, and Tenants as operational evidence.

These references are acceptable because the current project uses one database and one Domain assembly. They should still be treated as bounded-context boundaries in application services and APIs.

## Related Docs

- [Architecture](../../ARCHITECTURE.md)
- [Working Protocol](../process/WORKING_PROTOCOL.md)
- [Dependency Rules](DEPENDENCY_RULES.md)
- [Naming Rules](../process/NAMING_RULES.md)
- [Data Modeling Rules](../data/DATA_MODELING_RULES.md)
- [Multi-Tenancy Rules](MULTI_TENANCY_RULES.md)
- [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
- [JSON Field Rules](../data/JSON_FIELD_RULES.md)
````

## File: docs/flows/CHECKOUT_EXECUTION_FLOW.md
````markdown
# Checkout Execution Flow

This document describes tablet checkout, payment, edge dispatch, robot execution, and customer-facing status projection.

Detailed API and message contracts live in [IoT Contract](../iot/IOT_CONTRACT.md).

## Search Keywords

`checkout to execution`, `tablet checkout`, `payment session`, `QR payment`, `post-payment fan-out`, `tablet status`, `edge command flow`, `execution event sync`, `payment success`, `ready for fulfillment`, `OrderReadyForFulfillment`, `MQTT`, `EdgeCommand`, `ProductionExecutionRecord`

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
8. Cloud re-evaluates kiosk lifecycle, `KioskOperationalState.Operational`, connectivity, Store opening hours in `Store.TimeZone`, explicit Store sales pause, Menu/MenuItem lifecycle and scope, Product/Variant availability, Recipe/Ingredient lifecycle, active production route, and every active OptionGroup against the selected option IDs. Checkout calculates server-authoritative prices and stores immutable recipe/option snapshots. A Store, kiosk operational state, or catalog definition that becomes unavailable after a runtime-menu snapshot was issued rejects the order with `409`; a scoped item that does not belong to the kiosk is returned as not found.
9. Cloud creates:
   - Order
   - OrderItems
   - status PendingPayment / Unpaid
   - immutable `paymentDeadlineAt`
10. Tablet calls Cloud Backend to create payment session for the order.
11. Cloud creates:
   - PaymentTransaction
   - provider payment session
   Cloud persists the deterministic provider order code before calling the provider. A retry reconciles that same provider identity and must not create a second provider session.
12. Cloud returns:
   - checkoutUrl
   - qrCodePayload
   - expiresAt
13. Tablet renders QR.
14. Customer pays.
15. Payment provider calls Cloud webhook.
16. Cloud verifies provider callback and signature.
17. Cloud updates PaymentTransaction = Paid and Order = ReadyForFulfillment in one DB transaction.
18. Cloud commits payment/order state.
19. After the payment transaction commits, Cloud dispatches execution attempt `1`.
   A reconciliation worker repairs any paid `ReadyForFulfillment` order whose required machine-execution command was not created.
   If the kiosk is not `Operational`, the paid order remains queued. Cloud neither creates/delivers a new `ExecuteOrder` command nor cancels/refunds the order. Existing accepted/running execution evidence continues through its normal report lifecycle.
20. Tablet status flow updates payment/order screen.
21. Edge dispatch resolves one active execution endpoint and the active configuration release, then maps every machine-produced order line to an execution route and ordered robot programs.
22. Cloud publishes a best-effort MQTT `CommandAvailable` wake-up after commit. Edge still finds the durable `ExecuteOrder` command through authenticated pull; periodic polling recovers missed wake-ups.
23. Edge pulls executable command from Cloud.
24. Edge performs fast runtime check with 5-10 second timeout.
25. If ready, Edge accepts command and creates its own local execution state.
26. Robot executor runs the approved artifact plan through its local integration.
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

Store sales admission and active fulfillment are also separate concerns. Scheduled closing or an explicit sales pause stops runtime-menu access and new order placement, but does not cancel paid queue entries or stop accepted/running production. An Order placed before closure may create its payment session until its snapshotted `paymentDeadlineAt`; provider expiry is capped by that deadline. Once the deadline passes, no new session is created and the tablet must start a new Order. A verified late `Paid` webhook remains authoritative because money may already have moved.

If the provider accepted session creation but the original response was lost, a background reconciliation worker queries the persisted provider order code and restores the checkout URL or QR payload. This read-side recovery never replaces webhook verification: a provider lookup reporting `PAID` remains pending until a signed webhook authoritatively commits payment and order state. Reconciliation failures and exhausted retries are available through the scoped payment diagnostics read.

A known provider rejection marks the payment attempt failed and allows a new customer attempt. A timeout, transport failure, transient provider response, or incomplete successful response has an unknown creation outcome: Cloud keeps the transaction pending, schedules read-side reconciliation, and does not issue another create request. Operators use the scoped intervention queue and audited manual reconcile command when automatic recovery is exhausted or a signed webhook remains missing.

The intervention queue, automatic reconciliation, and manual reconcile command
share one eligibility policy. A pending provider session is eligible when its
checkout instructions are missing or its local expiry has been reached. An old
checkout URL or QR payload does not hide an expired session from the queue.

When reconciliation reaches manual intervention, Cloud also enqueues one durable
`payment_intervention` push per scoped Staff/Manager recipient, falling back to
the organization OrgAdmin. The identity is `(PaymentTransactionId,
InterventionCode, RecipientAccountId)`. The push recalls an absent operator but
does not change payment or Order state; the intervention queue and manual
reconcile API remain authoritative.

## Post-Payment Fan-Out

After payment is verified and committed, Cloud should fan out independently:

```text
Paid order committed
  -> Tablet status notification
  -> ExecuteOrder dispatch attempt 1
  -> reconciliation scan when the initial dispatch was missed
```

Rules:

- Do not wait for Edge acceptance inside the payment webhook transaction.
- Do not make Tablet status depend on Edge dispatch success.
- Dispatch is idempotent by `(OrderId, DispatchAttemptNo)`.
- Reconciliation creates only missing attempt `1`; a new attempt number requires an explicit retry decision.
- Admission counts active `ExecuteOrder` commands per endpoint and rejects dispatch when the configured queue limit is reached.
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
| `PaymentTransaction = Paid`, `Order = ReadyForFulfillment` | Payment successful, preparing fulfillment |
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
| `Delayed` | false | false | Your order is taking longer than expected. Production is still being monitored. | Delayed, keep monitoring |
| `PendingRecovery` | false | false | Connection to the machine was interrupted. We are checking your order. | Connection recovery in progress |
| `SupportRequired` | false | true | We could not confirm production progress. Please contact staff for support. | Staff support required |
| `Ready` | false | false | Your order is ready. Please pick it up! | Ready / pick up |
| `Completed` | false | false | Order completed. Thank you! | Completed |
| `Cancelled` | false | false | Order cancelled. | Order cancelled / aborted |
| `RefundRequired` | false | true | Order cancelled after payment. Please contact staff... / Order execution failed... | Staff support / manual refund required |

Order tracking read model boundary limitations and data exclusions are detailed in [API Surface Rules](../api/API_SURFACE_RULES.md#read-model-api-boundaries).

## Edge Command Flow

```text
1. Cloud has a paid order ready for execution.
2. Cloud creates an executable command.
3. Cloud may publish an MQTT command-available wake-up, and Edge polls on schedule. MQTT reduces latency but does not own the durable command.
4. Edge pulls pending commands from Cloud through its authenticated execution endpoint.
5. Edge deduplicates by commandId/idempotencyKey.
6. Edge performs runtime readiness check.
7. If ready:
   - persist local command/job
   - ack Accepted
   - Cloud moves Order to Accepted
   - create local execution work
8. If not ready:
   - ack ExecutorBusy when capacity is temporarily unavailable; Cloud keeps the order ReadyForFulfillment and redelivers later
   - otherwise ack Rejected
   - include rejection reason and readiness snapshot
   - Cloud moves the order to ExecutionRejected, or RefundRequired when physical output may already have occurred
9. Cloud updates order/execution state.
```

## Execution Timeout Reconciliation

```text
Pending/Delivered command expires before ACK
  -> CommandExpired
  -> Order ExecutionRejected

Accepted without order-summary report past deadline
  -> heartbeat current: Stale / Delayed
  -> heartbeat missing, old, or Offline: Unreachable / PendingRecovery
  -> prolonged Unreachable: Unreachable / SupportRequired

Running without report past deadline
  -> same observation rules
  -> Order remains Preparing; Cloud does not infer physical failure
```

Observation timeout is uncertainty about Edge, not proof that production failed. `SupportRequired` is a customer/support projection only; it does not automatically fail or refund the order. A later sequence-valid order-summary report restores `Fresh` and continues the normal lifecycle. REST polling and `OrderExecutionObservationChanged` SignalR events both expose the same projection so the tablet does not remain on `Preparing` indefinitely.

All command-expiry and missing-report deadlines use Cloud receive time. Edge/controller timestamps remain evidence for diagnostics and bounded future-skew validation; clock rollback on a runtime cannot expire an ACK or make a newly received execution report stale. Store timezone changes that affect configured opening hours require an explicit sales pause first. The pause blocks new admission while already paid/accepted/running fulfillment continues.

## Manual Redispatch

```text
Latest attempt DeliveryFailed
  or Rejected before physical output
    -> authorized operator supplies reason
    -> backend allocates attempt + 1
    -> audit actor/reason
    -> create a new immutable ExecuteOrder command
```

`ExecutorBusy` stays on the same attempt and is redelivered. `RefundRequired`, possible physical output, production `Failed`, and `RequiresManualIntervention` are support/refund paths, not automatic retry paths. The configured maximum attempt count is enforced inside the same order-level transaction.

An Edge, controller, or store-power restart during an accepted production job does not
cancel the paid order and does not authorize automatic replay. The affected job is
reported as `RequiresManualIntervention` with exact unit identity and physical-output
evidence. Completed units and stock evidence remain immutable. The complete recovery
matrix is defined in [Restart And Power Recovery](../operations/RESTART_AND_POWER_RECOVERY.md).

Cloud serializes every payment/fulfillment mutation that can change or authorize use
of an `Order` aggregate by `OrderId`. This includes payment-session creation, signed
payment application, payment reconciliation, cancellation/refund-required decisions,
initial dispatch, Manual/Packaged item events, execute-order ACK, production reports,
and timeout reconciliation. ACK, report, and timeout
mutations are also serialized by `EdgeCommand.Id`, so stale or duplicate transport
events cannot create two execution projections or overwrite a newer command state.
Command pull delivery-attempt allocation is serialized by execution endpoint; a
concurrent pull retry keeps the same `EdgeCommand.Id` and receives the next distinct
delivery-attempt number.

Execution-attempt detail exposes the ordered delivery history for that command, command-expiry timeout provenance, the redispatch actor/reason, and references to the immediately previous and next dispatch attempts. This keeps transport retries inside one dispatch attempt distinct from an operator-created redispatch attempt.

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
6. Job/unit reports carry `sourceProductionJobId`, `orderItemId`, `productionUnitNo`, and `productionUnitQuantity`. Cloud rejects overlapping ranges, persists `ProductionExecutionRecord` and optional stock evidence, then derives the effective unit outcome for the machine line.
7. A machine line completes only when every expected unit is effectively complete. Any failed unit moves a paid Order to `FulfillmentIssue` without removing successful-unit or stock evidence. In the same ingestion transaction, failed or manual-intervention job evidence opens one production incident for that immutable job/unit range. The Edge order-summary report (`sourceProductionJobId = null`) updates execution observation and must agree with complete job evidence before it can be final.
8. Cloud appends OrderStatusHistory and typed stock-consumption evidence supplied by Edge.
9. After commit, Cloud publishes OrderItemFulfillmentChanged for changed lines, OrderStatusChanged when the aggregate status changes, OrderExecutionObservationChanged for an applied order summary, and InventoryChanged for stock evidence.
10. Cloud returns accepted/duplicate/rejected result.
```

For Manual/Packaged lines with a configured preparation time, Cloud projects
`ExpectedReadyAt = PaidAt + effective preparation time`. Once overdue, a
background reconciliation job creates at most one durable reminder per eligible
recipient for that payment occurrence. It prefers scoped Staff/Manager accounts
and falls back to the organization OrgAdmin. The reminder does not advance or
fail the item; management fulfillment commands remain authoritative.

Event sync must be idempotent. Retrying a batch must not duplicate robot events, stock movements, or status transitions.

Production incident handling is a separate operational phase after evidence ingestion. Staff inspect possible output, then choose delivery, discard, exact-unit remake, technical review, no action, or explicitly acknowledged full-order refund/voucher. Remake and compensation identities are linked back to the incident. Successful-unit and stock evidence remain historical truth; resolution does not erase them. See [Production Incident Resolution Flow](PRODUCTION_INCIDENT_RESOLUTION_FLOW.md).

Mixed fulfillment is one aggregate workflow even though individual lines have
different authorities. Concurrent completion of Manual, Packaged, and
MachineProduced lines must reload and aggregate the order under the same order lock;
the last completing line transitions the order to `Completed` exactly once.

Every production report must match the configuration release id and checksum embedded in its accepted execute-order command; Low-cost reports must also match the active artifact-set version and checksum. Cloud rejects future-dated report/evidence timestamps beyond the configured clock-skew allowance. A source production job is permanently bound to the immutable provenance established by its first report: order item, production-unit range, workcell, controller, execution-plan checksum, and active artifact set. Each stock-evidence item identifies the same `OrderItemId` as the job report; Cloud validates the ingredient against that line's immutable recipe or option snapshot. Stock evidence uses its own globally unique event id, so concurrent job reports cannot consume the same evidence twice.

## Real-time Order & Payment Updates

During the checkout and execution flow, state changes (e.g. order placement, cancellation, payment webhook status updates, refund flagging) emit real-time SignalR notifications to subscribed clients:
- **`OrderStatusChanged`** is published on `OrderHub` to group `order:{orderId}` when order status transitions.
- **`OrderItemFulfillmentChanged`** is published to `order:{orderId}` and `kiosk:{kioskId}` when a manual, packaged, or machine-produced line changes status.
- **`PaymentStatusChanged`** is published on `OrderHub` to group `order:{orderId}` when payment transaction status changes.
- **`OrderExecutionObservationChanged`** is published on `OrderHub` when an order-summary report refreshes execution observation or reconciliation changes it to `Delayed`, `PendingRecovery`, or `SupportRequired` without changing `Order.Status`.

These events allow checkout UIs to automatically update payment success/failure screens or execution status without polling.

## Related Docs

- [System Flows](SYSTEM_FLOWS.md)
- [Failure Flow Index](FAILURE_FLOW_INDEX.md)
- [Catalog Runtime Menu Flow](CATALOG_RUNTIME_MENU_FLOW.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
- [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
````

## File: docs/flows/ROBOT_LUA_ARTIFACT_FLOW.md
````markdown
# Robot Lua Artifact Flow

This is the entry point for the current backend flow for Fairino `.lua` artifacts. It indexes the authoring and deployment contracts without duplicating their detailed API and operational rules.

## Search Keywords

`Fairino Studio`, `.lua`, `Lua export`, `RobotArtifact`, `RobotProgram`, `RobotProgramArtifact`, `RunOrder`, `artifact.read`, `artifact.upload`, `program.read`, `program.manage`, `release.read`, `release.publish`, `deployment.read`, `release.deploy`, `configuration deployment`, `presigned download URL`, `artifact checksum`, `Full Edge`, `Low-cost Controller`

## Ownership Boundary

```text
Fairino-Studio
  owns project editing and .lua generation

Cloud backend
  owns global authoring templates, immutable organization artifact metadata, object storage references,
  ordered RobotProgram manifests, configuration releases, and deployment commands

Object storage
  owns .lua file bytes

Execution endpoint
  downloads, verifies, installs, activates, and executes deployed artifacts
```

The `.fairobot` project file remains a design-time Fairino-Studio file. It is not a `RobotArtifact` and is not uploaded through the runtime artifact API.

`RobotArtifactTemplate` is also design-time input. It is globally managed and cannot be added to a program, release, or deployment. An organization must clone a Published template into its own Draft `RobotArtifact`, review it, and publish it before production use. The clone records `SourceRobotArtifactTemplateId`, but owns separate metadata and object-storage bytes so later template retirement cannot break organization runtime history.

## End-To-End Flow

```text
Fairino-Studio project
  -> export one or more .lua files
  -> upload each file as RobotArtifact
  -> publish each RobotArtifact
  -> create RobotProgram draft
  -> replace ordered RobotProgramArtifact membership
  -> review RobotProgram
  -> publish RobotProgram manifest
  -> author and publish ConfigurationRelease
  -> create and configure a kiosk execution endpoint
  -> provision its credential and profile identity
  -> request deployment for the kiosk execution endpoint
  -> endpoint pulls DeployConfiguration command
  -> Full Edge chooses bundle or changed .lua files; Low-cost downloads selected .lua files
  -> endpoint verifies byte length and SHA-256 checksum
  -> endpoint installs and activates configuration
  -> endpoint reports deployment result to Cloud
```

## Flow Documents

- [Robot Lua Authoring And Import Flow](ROBOT_LUA_AUTHORING_AND_IMPORT_FLOW.md): Fairino export bundle, sidecars, templates, artifact lifecycle, and ordered `RobotProgram` authoring.
- [Robot Lua Deployment And Activation Flow](ROBOT_LUA_DEPLOYMENT_AND_ACTIVATION_FLOW.md): release authoring, endpoint provisioning, preview, deployment, download, activation, rollback, and retry rules.

## Documentation Ownership

- This file owns the shared boundary and the end-to-end index.
- The authoring and import document owns authoring-side APIs and artifact/program lifecycle rules.
- The deployment and activation document owns release-to-endpoint deployment rules and artifact-specific activation behavior.
- [API Surface Rules](../api/API_SURFACE_RULES.md) owns route placement, request/response boundaries, paging, and transport-facing behavior.
- [IoT Contract](../iot/IOT_CONTRACT.md) owns Edge/controller authentication, command pull/ack, report envelopes, and retry semantics.
- [Deployment Configuration](../operations/DEPLOYMENT_CONFIG.md) owns environment variables, secrets, storage endpoints, and timeout settings.
- [Robot Artifact Operational Smoke Test](../operations/ROBOT_ARTIFACT_OPERATIONAL_SMOKE.md) owns runnable migration, MinIO, and integration verification commands.

Other documents should summarize only the rules they own and link to the smallest owning flow instead of maintaining a second copy of this lifecycle.

## Related Docs

- [System Flows](SYSTEM_FLOWS.md)
- [Back-Office Setup Flow](BACK_OFFICE_SETUP_FLOW.md)
- [Robot Artifact Operational Smoke Test](../operations/ROBOT_ARTIFACT_OPERATIONAL_SMOKE.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Authorization Rules](../api/AUTHORIZATION_RULES.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
- [Deployment Configuration](../operations/DEPLOYMENT_CONFIG.md)
- [Boundary Contexts](../architecture/BOUNDARY_CONTEXTS.md)
````

## File: docs/iot/IOT_CONTRACT.md
````markdown
Exit code: 0
Wall time: 0.1 seconds
Output:
# IoT Contract

This document owns the shared boundary, source-of-truth split, common message envelope, and cross-cutting rules for the IceBot IoT contracts.

The Cloud artifact-first schema is implemented by the current backend migration. Edge-local runtime persistence remains an external implementation. Do not interpret the historical `RobotJob` examples below as Cloud backend entities.

The contract is written for the current pre-deployment system:

- One tablet per kiosk.
- Tablet payment uses bank transfer QR/payment session.
- No inventory reservation before payment.
- Cloud can publish MQTT notifications.
- Edge still pulls commands from cloud for retry and offline recovery.
- Edge owns runtime execution and local machine state.
- Cloud owns payment verification, central order state, reporting, and monitoring.

## Search Keywords

`IoT contract`, `edge-cloud contract`, `tablet`, `local edge`, `cloud backend`, `MQTT`, `payment session`, `QR payment`, `order execution`, `ready for execution`, `executable order`, `pull commands`, `command ack`, `fast runtime check`, `sync events batch`, `heartbeat`, `configuration sync`, `runtime menu projection`, `payment callback`, `refund required`

## Source Of Truth

### Tablet

The tablet owns only transient user interaction state:

- Current menu view.
- Temporary cart/session.
- Payment QR display.
- Local UX status after checkout.

The tablet must not start robot execution directly.

### Local Edge Backend

The local edge backend owns runtime machine truth:

- Runtime menu/product projection.
- Estimated inventory availability.
- Device and robot availability.
- Local execution queue.
- Local production execution state.
- Runtime telemetry and event capture.

Edge can reject execution after payment if the machine cannot fulfill the order.

### Cloud Backend

Cloud owns central business truth:

- `Order`
- `PaymentTransaction`
- Payment provider session/callback verification.
- Executable order command creation.
- Final order state, analytics, audit, and monitoring.

Payment success does not guarantee robot execution. Execution still requires edge acceptance.

### MQTT

MQTT is notification only. It is not the source of truth and must not contain large executable payloads.

Edge must pull commands from cloud after receiving an MQTT notification. Edge must also poll/pull periodically in case MQTT is missed while offline.

MQTT is the machine-to-machine runtime integration boundary. It is separate from SignalR, which is used for Cloud-to-human-UI realtime updates. When Edge or robot state changes, Cloud may project the validated state to UI through SignalR, but SignalR must not drive robot execution directly.

## System Flow

End-to-end checkout, payment, edge dispatch, and robot execution flow lives in [Checkout Execution Flow](../flows/CHECKOUT_EXECUTION_FLOW.md). Failure routing lives in [Failure Flow Index](../flows/FAILURE_FLOW_INDEX.md).

This document focuses on API/message contract shape, source-of-truth boundaries, state mapping, and idempotency requirements.

Backend API surface categories and route ownership live in [API Surface Rules](../api/API_SURFACE_RULES.md). This document only expands the IoT/tablet/edge contracts that need integration detail.

## State Mapping

Use current domain states where possible.

### Order

Current enum: `Domain.Orders.Enums.OrderStatus`

Current mapping:

| Business state | Current enum |
| --- | --- |
| Created, waiting for payment | `PendingPayment` |
| Payment verified, ready for all line fulfillment modes | `ReadyForFulfillment` |
| Edge accepted executable command | `Accepted` |
| Robot job running | `Preparing` |
| Robot execution completed | `Completed` |
| Edge rejected execution after payment | `ExecutionRejected` |
| Paid order needs manual refund/support | `RefundRequired` |
| Payment failed, cancelled, or non-refundable execution failure | `Failed` / `Cancelled` |

`Paid` remains a coarse payment-confirmed state, but current orchestration should move fully paid orders to `ReadyForFulfillment`.

### Payment

Current enum: `Domain.Payments.Enums.PaymentTransactionStatus`

Use:

- `Pending` when QR/payment session is created.
- `Paid` after provider callback is verified.
- `Failed`, `Cancelled`, or `Expired` based on provider result.
- `Refunded` after refund completion.

### Cloud Execution Projection

Cloud has no runtime `RobotJob` entity. An accepted executor report creates `OrderExecutionRecord` and `ProductionExecutionRecord`; these retain executor status, observation state, physical-output evidence and source command identity for customer/support decisions.

The Edge runtime may have local `ProductionJob` records, but it owns their scheduler and status transitions.

## Common Envelope Rules

All edge-cloud commands and events should use UTC timestamps and stable ids.

Required common fields:

```json
{
  "messageId": "uuid",
  "correlationId": "uuid-or-order-id",
  "causationId": "uuid-or-command-id",
  "originNodeId": "kiosk-edge-node-id",
  "occurredAt": "2026-05-21T10:00:00Z",
  "contractVersion": 1
}
```

Rules:

- `messageId` identifies this transport message.
- `eventId` identifies a business/runtime event and must be deduplicated.
- `commandId` identifies a command and must be idempotent.
- `correlationId` traces the whole checkout/execution flow.
- `causationId` points to the command/event that caused the current message.
- `originNodeId` identifies the edge node that produced the message.
- All timestamps are UTC ISO 8601.


## Contract Map

| Need | Read |
| --- | --- |
| Tablet runtime menu, Cloud checkout/payment, or customer status | [Tablet and Cloud Contract](TABLET_CLOUD_CONTRACT.md) |
| Cloud-to-Edge commands, endpoint authentication, ACK/report, or configuration distribution | [Edge Command Contract](EDGE_COMMAND_CONTRACT.md) |
| Device evidence, replay, checkpoint, heartbeat, readiness, or capability projection | [Edge Sync and Telemetry Contract](EDGE_SYNC_TELEMETRY_CONTRACT.md) |

## Idempotency And Retry Rules

Required unique keys:

| Boundary | Key |
| --- | --- |
| Tablet checkout to Cloud | `Idempotency-Key` |
| Provider callback | provider event id |
| Cloud executable command | `commandId`, `idempotencyKey` |
| Edge local job creation | `commandId` or `orderId` unique |
| Edge event sync | `eventId` |
| Heartbeat | `kioskId`, `originNodeId`, `heartbeatSequence` |

Retry behavior:

- Retrying payment session creation must return the same payment session if the idempotency key matches.
- Retrying command pull can return already unacked commands.
- Retrying command ack must not create duplicate state transitions.
- Retrying event batch must classify duplicates item-by-item.

## Failure Paths

Failure ownership is routed by [Failure Flow Index](../flows/FAILURE_FLOW_INDEX.md).

Contract-level rules:

- Payment success and robot execution are separate concerns.
- Payment webhook handling must not wait for Edge acceptance.
- Edge deduplicates commands returned after duplicate MQTT notifications.
- Paid execution failures use the staff-managed refund or voucher compensation workflow.
- The default flow does not call provider refund or automatic payout APIs.

## Security

Do not use admin/internal account JWT for kiosk runtime.

Current security contract:

- Tablet to Edge: local network trust plus short-lived local token if needed.
- Tablet to Cloud: public checkout endpoint with idempotency and validation.
- Edge to Cloud: execution-endpoint credentials; `FullEdge` uses mutual TLS and `LowCostController` uses signed-command TLS.
- MQTT: per-kiosk credential/topic authorization.

Excluded From Current Contract:

- Per-device key rotation.
- Command payload checksum/signature.

## Implementation Notes

- Keep IoT request/response DTOs separate from EF entities.
- Do not expose domain entities directly as IoT contracts.
- Use typed columns for idempotency, retry, status, and timestamps.
- JSON payloads are allowed for robot SDK/config/provider evidence, but source-of-truth workflow fields must be typed.
- `StockMovement` should record estimated consumption after accepted/completed execution. Future sensor conversion can refine quantity handling later.
- `IngredientDispenserState` can remain hardware-level `Low` / `Medium` / `Full` for availability checks.

## Related Docs

- [Architecture](../../ARCHITECTURE.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Checkout Execution Flow](../flows/CHECKOUT_EXECUTION_FLOW.md)
- [Failure Flow Index](../flows/FAILURE_FLOW_INDEX.md)
- [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
- [JSON Field Rules](../data/JSON_FIELD_RULES.md)
````

## File: docs/api/AUTHORIZATION_RULES.md
````markdown
# Authorization Rules

This document records backend API authorization direction for internal users.

Customer ordering is anonymous in the current system. Customer is a business actor, not an internal account role.

## Search Keywords

`authorization`, `authz`, `RBAC`, `scoped RBAC`, `role`, `roles`, `SystemAdmin`, `Manager`, `Staff`, `Technician`, `OrgAdmin`, `Organization Admin`, `policy`, `permissions`, `account roles`, `organization scope`, `store scope`, `kiosk scope`, `accounts.manage`, `products.manage`, `menus.manage`, `robot-config.manage`

## Route Surface

API surface ownership, route categories, and examples live in [API Surface Rules](API_SURFACE_RULES.md).

This document only defines authorization direction for those surfaces.

## Internal Roles

| Role code | Meaning |
| --- | --- |
| `SystemAdmin` | System-wide administration, accounts, permissions, security, and platform health |
| `Manager` | Business/operations management across kiosks, reports, menus, pricing, and maintenance coordination |
| `Staff` | On-site operations such as refill, cleaning, status checks, issue reporting, and manual support/refund handling |
| `Technician` | Installation, robot/kiosk setup, technical maintenance, troubleshooting, and device/robot configuration |
| `OrgAdmin` | Organization admin who can view and manage resources within their assigned organization scope |

## OrgAdmin Flow

OrgAdmin is created through internal account onboarding, not public signup.

Recommended flow:

```text
SystemAdmin creates Organization
  -> SystemAdmin creates internal account
  -> assign RoleCode = OrgAdmin with OrganizationId
  -> backend creates invitation link
  -> OrgAdmin accepts invitation
  -> OrgAdmin can access assigned organization scope
```

OrgAdmin scope must be stored through `AccountRole`:

```text
RoleCode = OrgAdmin
OrganizationId = organizationId
StoreId = null
KioskId = null
```

OrgAdmin access must be checked against role scope. Do not infer tenant access from email domain.

## Permission Entity Decision

Do not add a `Permission` entity for v1.

Current authorization uses:

```text
RoleCode
+ AccountRole scope
+ ASP.NET policy name
```

Policy names are treated as permission-like constants for now. This keeps v1 authorization explicit while roles and business flows are still being finalized.

Add `Permission` and `RolePermission` entities only when there is a concrete need for dynamic permission management, such as:

- admins configuring permissions from UI,
- tenant-specific custom roles,
- many custom roles beyond the current internal role set,
- hardcoded policies becoming too large to maintain safely,
- permission changes needing their own audit and lifecycle.

## Implemented RBAC APIs

These APIs are implemented to make RBAC and tenant scope selection easier to manage in FE/admin screens:

```http
GET /api/v1/management/roles
GET /api/v1/management/role-scope-options
GET /api/v1/management/permission-matrix
```

## Implemented Account Access APIs

These account-specific access APIs are implemented:

```http
PUT /api/v1/management/accounts/{accountId}/roles
GET /api/v1/management/accounts/{accountId}/effective-access
GET /api/v1/me/access
```

Tenant scope/resource lookup lets admins choose valid assignment scopes:

```http
GraphQL tenantTree
GET /api/v1/management/role-scope-options
```

GraphQL `tenantTree` returns the management-visible tenant hierarchy:

```text
Organization
  -> Store
      -> Kiosk
```

Use it for role scope selection and tenant navigation. This is not dynamic permission management. The previous REST tenant-tree route is intentionally removed to avoid a duplicated API surface with GraphQL.

When assigning roles, the backend must validate scope hierarchy:

```text
Organization exists
Store exists
Kiosk exists
Store.OrganizationId == OrganizationId
Kiosk.StoreId == StoreId
Kiosk.OrganizationId == OrganizationId
```

This is a tenant RBAC usability implementation. It does not add `Permission` or `RolePermission` tables.

## Policy Direction

Register backend authorization policies in `src/WebAPI/Authorization/AuthorizationPolicyExtensions.cs`; do not add feature-specific policy registrations directly to `Program.cs`.

| Policy | Allowed roles | Notes |
| --- | --- | --- |
| `roles.view` | `SystemAdmin`, `OrgAdmin`, `Manager` | View roles catalog and static permission matrix |
| `role-scope-options.view` | `SystemAdmin`, `OrgAdmin`, `Manager` | View valid organizational scope options for a target role |
| `dashboard.view` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | View management dashboard metrics within assigned scope |
| `accounts.read` | `SystemAdmin`, `OrgAdmin`, `Manager` | Read internal accounts. SystemAdmin can read all accounts; OrgAdmin and Manager are scope-filtered |
| `accounts.manage` | `SystemAdmin` | Create, update, disable, assign/update roles, set password, and send invitations for internal accounts |
| `organizations.manage` | `SystemAdmin` | Platform-level organization management: create, activate, disable organizations |
| `organizations.view` | `SystemAdmin`, `OrgAdmin` | View organizations. OrgAdmin can view/read only their assigned organization(s) |
| `organizations.update` | `SystemAdmin`, `OrgAdmin` | Update organizations. OrgAdmin can update only basic profile/contact info for assigned organization(s); SystemAdmin can update platform-managed fields |
| `stores.view` | `SystemAdmin`, `OrgAdmin`, `Manager` | View stores. Scoped to assigned organization/store |
| `stores.manage` | `SystemAdmin`, `OrgAdmin` | Create, disable, and activate stores. Scoped to assigned organization |
| `stores.update` | `SystemAdmin`, `OrgAdmin`, `Manager` | Update store details. Scoped to assigned organization/store |
| `kiosks.view` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | View kiosks. Scoped to assigned organization/store/kiosk |
| `kiosks.manage` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | Create and change status of kiosks. Scoped to assigned organization/store/kiosk |
| `kiosks.update` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | Update kiosk details. Scoped to assigned organization/store/kiosk |
| `devices.view` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | View devices/hardware details within assigned scope |
| `devices.manage` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | Create, update, status-change, replace, or retire devices/hardware; create, configure, provision, disable/reactivate, rotate credentials, or retire execution endpoints within assigned scope |
| `device-catalog.read` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | Read the global DeviceType/DeviceModel lookup catalog; no tenant scope is required |
| `device-catalog.manage` | `SystemAdmin` | Create/update/deactivate DeviceType and create/update/retire DeviceModel records |
| `artifact.read` | `SystemAdmin`, `OrgAdmin` | List and inspect metadata for organization-owned robot Lua artifacts |
| `artifact.upload` | `SystemAdmin`, `OrgAdmin` | Upload, request short-lived Lua review URLs, discard Draft, publish, and retire organization-owned robot Lua artifacts |
| `artifact-template.read` | `SystemAdmin`, `OrgAdmin` | List and review global robot Lua templates; templates cannot execute directly |
| `artifact-template.manage` | `SystemAdmin` | Upload, discard Draft, publish, and retire global robot Lua templates |
| `program.read` | `SystemAdmin`, `OrgAdmin`, `Manager` | Read robot programs within the actor's matching organization/store/kiosk scope |
| `program.manage` | `SystemAdmin`, `OrgAdmin`, `Manager` | Author, publish, and retire robot programs within the actor's matching organization/store/kiosk scope |
| `release.read` | `SystemAdmin`, `OrgAdmin`, `Manager` | Read production configuration releases and authoring options within the actor's matching organization scope |
| `release.publish` | `SystemAdmin`, `OrgAdmin` | Author, publish, and retire organization-owned production configuration releases |
| `deployment.read` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | Monitor configuration deployment state and failure details within assigned kiosk scope |
| `release.deploy` | `SystemAdmin`, `OrgAdmin`, `Manager` | Request configuration deployment to assigned kiosks |
| `package.read` | `SystemAdmin`, `OrgAdmin`, `Manager` | Read published package catalog and installation state within tenant scope |
| `package.manage` | `SystemAdmin` | Author and publish global production package versions |
| `package.install` | `SystemAdmin`, `OrgAdmin`, `Manager` | Preview and install published packages within tenant scope |
| `package.fork` | `SystemAdmin`, `OrgAdmin` | Convert package-managed technical configuration into an explicit organization fork |
| `release.rollback` | `SystemAdmin`, `OrgAdmin`, `Manager` | Request a new deployment from a previously Active Full Edge release or low-cost artifact set within assigned scope |
| `tenant-tree.view` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | View tenant hierarchy for RBAC scope selection and management navigation |
| `product-templates.read` | `SystemAdmin`, `Manager` | Browse global product templates for cloning into an assigned organization |
| `product-templates.manage` | `SystemAdmin` | Manage global product templates; tenant roles cannot author or mutate global catalog rows |
| `products.manage` | `SystemAdmin`, `Manager` | Manage organization-owned products and variants within assigned organization/store/kiosk scope |
| `product-categories.read` | `SystemAdmin`, `Manager` | Browse the global flat ProductCategory catalog used by product authoring |
| `product-categories.manage` | `SystemAdmin` | Create, update, activate/deactivate, and safely delete unreferenced global ProductCategory definitions |
| `ingredients.read` | `SystemAdmin`, `Manager` | Browse the global ingredient reference catalog used by recipe authoring |
| `ingredients.manage` | `SystemAdmin` | Create, update, activate/deactivate, and safely delete unreferenced global ingredient definitions |
| `menus.manage` | `SystemAdmin`, `Manager` | Manage organization-owned menus, prices, promotions, and sellable offers within assigned scope |
| `payments.manage` | `SystemAdmin`, `Manager` | Tenant payment operations and intervention workflows |
| `payment-methods.manage` | `SystemAdmin` | Global payment-method catalog status management |
| `refunds.manage` | `SystemAdmin`, `Manager`, `Staff` | Manual support/refund workflow. Auto provider refund is future work |
| `inventory.view` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | View dispenser states and stock movements within assigned scope |
| `inventory.manage` | `SystemAdmin`, `Manager`, `Staff`, `Technician` | Refill dispenser state and adjust inventory estimates within assigned scope |
| `inventory.configure` | `SystemAdmin`, `Manager`, `Technician` | Provision and configure dispenser topology, activate/retire states, and delete only unused states within assigned scope |
| `operations.view` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | View kiosk heartbeat history, device events, and curated operation logs within assigned scope |
| `operations.diagnostics` | `SystemAdmin`, `Technician` | View raw operation-log payloads and order execution diagnostics within assigned kiosk scope |
| `notifications.manage` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | Requeue permanently failed notification deliveries within assigned scope; reason and actor are audited |
| `maintenance.view` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | View maintenance tickets within assigned scope |
| `maintenance.create` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | Create maintenance tickets within assigned scope |
| `maintenance.manage` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | Manage, assign, resolve, and close maintenance tickets within assigned scope. Staff can create/view tickets but cannot assign or resolve by default |
| `sync-dead-letters.manage` | `SystemAdmin` | Inspect retry audit, replay supported sync event types, and resolve/ignore Cloud dead letters. Raw replay control is intentionally not tenant-admin self-service in V1 |
| `alerts.view` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | View actionable telemetry alerts within assigned scope |
| `alerts.manage` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | Acknowledge and resolve actionable telemetry alerts within assigned scope |
| `robot-config.manage` | `SystemAdmin`, `Technician` | Robot program/config/profile setup |
| `reports.view` | `SystemAdmin`, `Manager`, `OrgAdmin` | Scope filtering must be enforced when scoped authorization is implemented |

## Current Implementation Notes

- Current `ScopedRoleAuthorizationHandler` checks role presence only.
- Scoped authorization evaluates role and resource scope from the same `UserRoleScope`. A privileged role in one tenant cannot borrow organization, store, or kiosk ids assigned to another role.
- Management list queries pass role-specific effective scope sets into persistence filters. Sensitive read-by-id and mutation queries should include the same scope predicate and return `404` when the resource is outside that scope.
- Route/resource authorization must validate requested scope before returning scoped tenant data or applying a state transition.
- Account read APIs use `accounts.read` and must remain scope-filtered for non-`SystemAdmin` callers. Account mutation APIs use `accounts.manage` and remain `SystemAdmin` only.
- `GET /management/accounts/{accountId}/effective-access` uses `accounts.read` and returns the target account's active role scopes plus the effective ids used by current scoped authorization rules.
- Effective access does not expand organization scope into store/kiosk ids. Use GraphQL `tenantTree` or REST `role-scope-options` for UI tree display.
- `GET /me/access` is a self-inspection endpoint based on the current access token claims. Refresh the token after role changes to see updated access.
- `/me/notification-devices` is authenticated self-service only; callers can register, inspect, or invalidate only their own FCM installations.
- `PUT /management/accounts/{accountId}/roles` replaces active role assignments for the target account. `POST /management/accounts/{accountId}/roles` remains an add/upsert single-role operation.
- Do not add `Staff` or `Technician` to product/menu pricing policies unless the business explicitly gives them that responsibility.

## Related Docs

- [Product Overview](../../../IceBot-Product/product/OVERVIEW.md)
- [API Surface Rules](API_SURFACE_RULES.md)
- [Identity Onboarding Rules](IDENTITY_ONBOARDING_RULES.md)
- [Dependency Rules](../architecture/DEPENDENCY_RULES.md)
- [Naming Rules](../process/NAMING_RULES.md)
````

## File: docs/operations/DEPLOYMENT_CONFIG.md
````markdown
# Deployment Configuration

This document classifies backend deployment configuration by priority and source. Not every setting must be repeated as an environment variable: safe, environment-independent defaults may remain in `appsettings.json`.

## Search Keywords

`deployment`, `backend config`, `environment variables`, `appsettings`, `JWT`, `database connection`, `Firebase`, `SMTP`, `PayOS`, `MinIO`, `S3`, `robot artifact storage`, `PORT`, `health`, `info`

## Configuration Source

The WebAPI loads configuration in this order:

```text
appsettings.json
appsettings.{Environment}.json
environment variables
```

Use environment variables or a deployment secret store for credentials and environment-specific addresses. Values marked **Use appsettings default** below do not need to be repeated unless the deployment intentionally changes them.

## Docker Compose Boundary

Backend docker compose, when added, should contain only backend app runtime dependencies such as PostgreSQL, Redis, and backend-owned infrastructure. Do not require `IceBot-Tools` to run the backend.

Tooling infrastructure such as Qdrant, RAG services, local model caches, and agent automation belongs in the `IceBot-Tools` compose lifecycle. If backend and tools need to communicate locally, use environment variables such as `RAG_API_URL` or an explicitly shared Docker network.

## Configuration Priority

| Priority | Meaning |
| --- | --- |
| **P0 Core** | Required for every deployed backend. Deployment must provide or explicitly verify it. |
| **P0 Feature** | Required when the named feature is enabled or used in that environment. |
| **P1** | Production-sensitive. A default exists, but the operator must review it for the target environment. |
| **P2** | Operational tuning or optional integration. Use the `appsettings` default until there is a reason to override it. |

`Secret/env required` means the checked-in value is a placeholder, local credential, or environment-specific address and must not be used as a deployed value. `Use appsettings default` means omission from environment variables is intentional and supported.

## Core And Feature Credentials

| Area | Configuration key | Priority | Deploy action |
| --- | --- | --- | --- |
| Database connection | `ConnectionStrings__IceBot_DB` | **P0 Core** | **Secret/env required.** Never deploy the checked-in local connection string. |
| JWT signing secret | `Authentication__Jwt__Secret` | **P0 Core** | **Secret/env required.** Use a strong environment-specific secret. |
| JWT issuer | `Authentication__Jwt__Issuer` | **P1** | Use appsettings default only if `IceBotApp` is the intended issuer. |
| JWT audience | `Authentication__Jwt__Audience` | **P1** | Use appsettings default only if `IceBotUsers` is the intended audience. |
| Public order token key ring | `PublicOrderAccess__KeyRingDirectory` | **P0 Core** | **Required in Production.** Persistent shared filesystem path for Data Protection keys; mount the same protected directory into every API instance. Tokens survive restarts but remain invalid after the 24-hour lifetime. |
| Browser frontend origins | `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, ... | **P0 Core** | **Env required for browser deployments.** Production does not use the Development allow-any fallback. |
| Public hosting port | `PORT` | **P0 Feature** | Provide only when the hosting platform injects a public port; otherwise use normal ASP.NET hosting configuration. |
| Diagnostics API key | `Diagnostics__ApiKey` | **P0 Feature** | **Secret/env required** before exposing management diagnostics outside Development. |

## Initial SystemAdmin Bootstrap

These values are required only while an environment has no active `SystemAdmin` role assignment. They are not normal long-lived application settings.

| Area | Configuration key | Direct environment alias | Priority | Deploy action |
| --- | --- | --- | --- | --- |
| Admin username | `BootstrapAdmin__UserName` | `BOOTSTRAP_ADMIN_USERNAME` | **P0 Core, first deployment** | Environment-specific value required for initial bootstrap. |
| Admin email | `BootstrapAdmin__Email` | `BOOTSTRAP_ADMIN_EMAIL` | **P0 Core, first deployment** | Environment-specific value required for initial bootstrap. |
| Admin password | `BootstrapAdmin__Password` | `BOOTSTRAP_ADMIN_PASSWORD` | **P0 Core, first deployment** | **Secret store/env required. Never put this value in appsettings or source control.** |
| Admin display name | `BootstrapAdmin__FullName` | `BOOTSTRAP_ADMIN_FULLNAME` | **P2** | Optional; defaults to `Bootstrap System Admin`. |

Bootstrap lifecycle:

1. Supply username, email, and a strong generated password through the deployment secret store.
2. Start the backend and confirm that the account has the active `SystemAdmin` role.
3. Log in and rotate the password through the normal account flow.
4. Remove all bootstrap admin values from deployment configuration.

The hosted bootstrap exits without reading these values when an active `SystemAdmin` already exists. Keeping them after bootstrap is still discouraged: if all active SystemAdmin assignments are later removed, a restart could match/create the configured account, reset its password, and grant `SystemAdmin` again.

## Email And Identity Providers

| Area | Configuration key | Priority | Deploy action |
| --- | --- | --- | --- |
| Email host | `Email__Host` | **P0 Feature** | Required when invitation/password-reset email delivery is enabled. |
| Email username | `Email__UserName` | **P0 Feature** | **Secret/env required** when SMTP authentication is used. |
| Email password | `Email__Password` | **P0 Feature** | **Secret/env required** when SMTP authentication is used. |
| Email sender | `Email__From` | **P0 Feature** | Required for email delivery; do not use the sample address. |
| Password reset frontend URL | `Email__PasswordResetBaseUrl` | **P0 Feature** | Required before password-reset links are issued. |
| Invitation frontend URL | `Email__InvitationBaseUrl` | **P0 Feature** | Required before invitation links are issued. |
| Email port | `Email__Port` | **P1** | Review for the SMTP provider; appsettings defaults to `587`. |
| Email TLS mode | `Email__EnableSsl` | **P1** | Review with the selected SMTP port/provider. |
| SMTP operation timeout | `Email__OperationTimeoutSeconds` | **P2** | Defaults to `30` seconds for connect/authenticate/send/disconnect. Email delivery is not retried without an outbox delivery identity. |
| Email display name | `Email__DisplayName` | **P2** | Use appsettings default or override branding. |
| Firebase enabled flag | `Firebase__Enabled` | **P1** | Explicitly set `false` when Google/Firebase login is not deployed. |
| Firebase credentials path | `Firebase__CredentialsPath` | **P0 Feature** | **Secret-mounted path required** when Firebase is enabled outside an environment that supplies application-default credentials. |
| Firebase auth resilience | `Firebase__Resilience__OperationTimeoutSeconds`, `__RetryCount`, `__RetryDelayMilliseconds`, `__CircuitBreakerFailureRatio`, `__CircuitBreakerMinimumThroughput`, `__CircuitBreakerSamplingDurationSeconds`, `__CircuitBreakerBreakDurationSeconds` | **P2** | Defaults are suitable initially. Only transport and explicit Firebase service failures retry; invalid/expired/revoked tokens never retry. Settings are startup-validated. |
| Firebase push operation timeout | `Firebase__PushDelivery__OperationTimeoutSeconds` | **P1** | Default `30` seconds. Bounds one FCM send attempt. Do not add sender-level retry: durable `NotificationDelivery` owns retry and delivery identity. Keep this lower than `NotificationDelivery__ProcessingTimeoutSeconds`. |

## Payment Provider

| Area | Configuration key | Priority | Deploy action |
| --- | --- | --- | --- |
| PayOS client id | `PayOS__ClientId` | **P0 Feature** | **Secret/env required** when PayOS payment is enabled. |
| PayOS API key | `PayOS__ApiKey` | **P0 Feature** | **Secret/env required** when PayOS payment is enabled. |
| PayOS checksum key | `PayOS__ChecksumKey` | **P0 Feature** | **Secret/env required** for request/webhook integrity. |
| PayOS return URL | `PayOS__ReturnUrl` | **P0 Feature** | Environment-specific public URL required for checkout. |
| PayOS cancel URL | `PayOS__CancelUrl` | **P0 Feature** | Environment-specific public URL required for checkout cancellation. |
| PayOS base URL | `PayOS__BaseUrl` | **P2** | Use the appsettings provider URL unless PayOS changes the endpoint or a test stub is used. |
| PayOS resilience | `PayOS__Resilience__AttemptTimeoutSeconds`, `__TotalTimeoutSeconds`, `__CircuitBreakerFailureRatio`, `__CircuitBreakerMinimumThroughput`, `__CircuitBreakerSamplingDurationSeconds`, `__CircuitBreakerBreakDurationSeconds` | **P2** | Dependency-specific timeout and circuit settings. Payment-creation `POST` retry remains disabled. Settings are startup-validated. |
| Enable payment-session reconciliation | `Payments__SessionReconciliation__Enabled` | **P1** | Keep the appsettings default `true` when PayOS checkout is enabled. The worker repairs responses lost after provider session creation without repeating the create `POST`. |
| Payment-session reconciliation timing | `Payments__SessionReconciliation__IntervalSeconds`, `__StaleAfterSeconds`, `__RetryDelaySeconds` | **P2** | Start with the appsettings defaults. Increase the interval or delay only when provider rate limits require it. Settings are startup-validated. |
| Payment-session reconciliation batch | `Payments__SessionReconciliation__BatchSize` | **P2** | Maximum pending transactions queried per scan; appsettings default is `50`. |
| Order payment window | `Payments__OrderPaymentWindow__DurationMinutes` | **P1** | Server-authoritative time allowed to start payment after Order placement, including across Store closing or a manual sales pause. Default `15`; startup validation allows `1-120`. PayOS session expiry is capped by this deadline. |

## Robot Artifact Storage

| Area | Configuration key | Priority | Deploy action |
| --- | --- | --- | --- |
| Object-storage endpoint | `RobotArtifacts__ObjectStorage__Endpoint` | **P0 Feature** | Environment-specific internal S3/MinIO endpoint required for artifact upload. |
| Edge-reachable download endpoint | `RobotArtifacts__ObjectStorage__DownloadEndpoint` | **P0 Feature** | Environment-specific endpoint required; it must be reachable by Edge/controllers. |
| Object-storage access key | `RobotArtifacts__ObjectStorage__AccessKey` | **P0 Feature** | **Secret/env required.** Do not deploy `minioadmin`. |
| Object-storage secret key | `RobotArtifacts__ObjectStorage__SecretKey` | **P0 Feature** | **Secret/env required.** Do not deploy `minioadmin`. |
| Object-storage bucket | `RobotArtifacts__ObjectStorage__BucketName` | **P1** | Appsettings default is acceptable only when the deployment uses the same private bucket name. |
| Auto-create bucket | `RobotArtifacts__ObjectStorage__AutoCreateBucket` | **P0 Feature** | Keep `false` in production and provision the bucket through infrastructure. Development and the local backend compose may set `true`. |
| Read-only storage resilience | `RobotArtifacts__ObjectStorage__ReadRetryCount`, `__ReadRetryDelayMilliseconds` | **P2** | Defaults to two retries with 200 ms base delay for stat, bucket check, and presigned URL only. Upload streams are not retried. |
| Storage TLS toggle | `RobotArtifacts__ObjectStorage__UseSsl` | **P1** | Review for the internal storage endpoint; production usually requires TLS. |
| Download TLS toggle | `RobotArtifacts__ObjectStorage__DownloadUseSsl` | **P1** | Review for the Edge-facing endpoint; production should use TLS. |
| Presigned URL lifetime | `RobotArtifacts__ObjectStorage__DownloadUrlExpirySeconds` | **P2** | Use appsettings default `900` seconds unless deployment latency requires tuning. |
| Enable orphan cleanup | `RobotArtifacts__ObjectStorage__OrphanCleanupEnabled` | **P1** | Keep the appsettings default `true` unless cleanup is owned externally. |
| Orphan grace period | `RobotArtifacts__ObjectStorage__OrphanGracePeriodHours` | **P2** | Use appsettings default `24`. |
| Orphan cleanup interval | `RobotArtifacts__ObjectStorage__OrphanCleanupIntervalHours` | **P2** | Use appsettings default `24`. |
| Cleanup delete limit | `RobotArtifacts__ObjectStorage__OrphanCleanupMaxDeletesPerRun` | **P2** | Use appsettings default `100`. |
| Authoring import staging retention | `RobotArtifacts__ObjectStorage__AuthoringImportRetentionHours` | **P2** | Use appsettings default `168` hours. The window applies to Applied import staging; Uploaded, Validated, and Failed imports retain staging while retry actions remain available. Discarded imports are eligible for cleanup after the orphan grace period. Import metadata/provenance remains in PostgreSQL after staging ZIP removal. |

Object storage is validated before background jobs start. Connection failure, invalid credentials, or a missing bucket while `AutoCreateBucket=false` stops application startup. This prevents the API from reporting healthy while artifact upload and deployment are unavailable.

## Runtime Safety And Capacity

| Area | Configuration key | Priority | Deploy action |
| --- | --- | --- | --- |
| Enable deployment reconciliation | `DeploymentTimeoutReconciliation__Enabled` | **P1** | Keep the appsettings default `true` for active Edge/controller deployments. |
| Reconciliation interval | `DeploymentTimeoutReconciliation__IntervalSeconds` | **P2** | Use appsettings default `60`. |
| Commands per reconciliation run | `DeploymentTimeoutReconciliation__MaxCommandsPerRun` | **P2** | Use appsettings default `100`. |
| Accepted-report timeout | `DeploymentTimeoutReconciliation__AcceptedReportTimeoutMinutes` | **P1** | Review against expected download/install duration; default is `30`. |
| Installed-activation timeout | `DeploymentTimeoutReconciliation__InstalledActivationTimeoutMinutes` | **P1** | Review against expected activation duration; default is `30`. |
| Signed-request clock skew | `ExecutionEndpointSecurity__SignedRequestMaxClockSkewSeconds` | **P1** | Review device clock quality; default is `300`. |
| Signed-request nonce retention | `ExecutionEndpointSecurity__NonceRetentionSeconds` | **P1** | Must remain longer than the accepted replay window; default is `900`. |
| IoT request body limit | `ExecutionEndpointSecurity__MaxRequestBodyBytes` | **P1** | Review against command/report payload size; default is 1 MiB. |
| Low-cost artifact count | `LowCostControllerCapacity__MaxArtifactCount` | **P1** | Configure from the supported controller profile; default is `50`. |
| Low-cost artifact bytes | `LowCostControllerCapacity__MaxArtifactStorageBytes` | **P1** | Configure from controller storage capacity; default is 50 MiB. |
| Release publish inventory policy | `ProductionInventoryReadiness__PublishPolicy` | **P1** | `Warn` by default. Use `Block` only when every applicable kiosk must be provisioned before release publication. |
| Release deploy inventory policy | `ProductionInventoryReadiness__DeployPolicy` | **P1** | `Block` by default. `Warn` permits deployment while returning detailed readiness warnings. |
| Upgrade reconciliation enabled | `ProductionPackageUpgrade__Reconciliation__Enabled` | **P1** | Keep enabled so crashed or abandoned materialization work cannot lock a source installation indefinitely. |
| Upgrade materialization timeout | `ProductionPackageUpgrade__Reconciliation__MaterializingTimeoutMinutes` | **P1** | Maximum interval without persisted progress before a Materializing upgrade is marked Failed; default is `15`. |
| Upgrade reconciliation schedule | `ProductionPackageUpgrade__Reconciliation__IntervalSeconds`, `__BatchSize` | **P2** | Defaults are `60` seconds and `100` candidates. Settings are startup-validated. |
| Enable order execution dispatch | `OrderExecutionDispatch__Enabled` | **P1** | Keep enabled when paid machine-produced orders must be dispatched to Edge. |
| Execute-order command expiry | `OrderExecutionDispatch__CommandExpiryMinutes` | **P1** | Review against kiosk queue and customer-wait policy; default is `30`. |
| Active commands per endpoint | `OrderExecutionDispatch__MaxActiveCommandsPerEndpoint` | **P1** | Set from real Edge capacity; default is `20`. |
| Dispatch reconciliation interval | `OrderExecutionDispatch__ReconciliationIntervalSeconds` | **P2** | Use appsettings default `10` unless database load requires tuning. |
| Dispatch reconciliation batch size | `OrderExecutionDispatch__ReconciliationBatchSize` | **P2** | Use appsettings default `50` unless recovery volume requires tuning. |
| Initial dispatch support escalation | `OrderExecutionDispatch__InitialDispatchSupportEscalationMinutes` | **P1** | Paid machine orders with no initial command become `FulfillmentIssue` after this duration; default is `15` minutes. |
| Execution-timeout reconciliation interval | `OrderExecutionDispatch__TimeoutReconciliationIntervalSeconds` | **P2** | Use appsettings default `30`. |
| Execution-timeout batch size | `OrderExecutionDispatch__TimeoutReconciliationBatchSize` | **P2** | Use appsettings default `100`. |
| Accepted report timeout | `OrderExecutionDispatch__AcceptedReportTimeoutMinutes` | **P1** | Maximum time after ACK before missing order-summary evidence becomes stale; default is `5`. |
| Running report timeout | `OrderExecutionDispatch__RunningReportTimeoutMinutes` | **P1** | Maximum silence while running before observation becomes stale; default is `30`. |
| Heartbeat unreachable threshold | `OrderExecutionDispatch__HeartbeatUnreachableMinutes` | **P1** | Missing, Offline, or older heartbeat changes stale observation to unreachable; default is `2`. |
| Unreachable support escalation | `OrderExecutionDispatch__UnreachableSupportEscalationMinutes` | **P1** | Prolonged silence escalates customer projection from `PendingRecovery` to `SupportRequired` without changing the business order to Failed; default is `15`. |
| Maximum order dispatch attempts | `OrderExecutionDispatch__MaxDispatchAttempts` | **P1** | Hard ceiling across initial dispatch and operator redispatch; default is `3`. |
| Execution-report future clock skew | `ExecutionReportIngestion__MaxFutureClockSkewSeconds` | **P1** | Maximum accepted future offset for report and stock-evidence timestamps; default is `300` seconds. |
| Edge telemetry future clock skew | `EdgeTelemetryIngestion__MaxFutureClockSkewSeconds` | **P1** | Maximum accepted future offset for heartbeat and future device-event timestamps; default is `300` seconds. |
| Kiosk heartbeat timeout | `EdgeTelemetryIngestion__HeartbeatTimeoutSeconds` | **P1** | Maximum Cloud receive-time silence before an Active kiosk becomes Offline/Unreachable; default is `90` seconds. |
| Execution readiness timeout | `EdgeTelemetryIngestion__ReadinessTimeoutSeconds` | **P1** | Maximum Cloud receive-time age for a Ready/Safe projection used by menu, checkout, deployment preview, and workspace; default is `120` seconds. |
| Alert automation event age | `EdgeTelemetryIngestion__AlertAutomationMaxEventAgeMinutes` | **P1** | Maximum age at Cloud receive time for replayed Error/Critical device evidence to create/correlate an Alert or push; default is `60` minutes. Older events remain audit history. |
| Connectivity reconciliation interval | `EdgeTelemetryIngestion__ConnectivityReconciliationIntervalSeconds` | **P1** | Background scan interval for heartbeat timeout transitions; default is `15` seconds. |
| Connectivity reconciliation batch size | `EdgeTelemetryIngestion__ConnectivityReconciliationBatchSize` | **P2** | Maximum Active kiosk candidates checked per scan; appsettings default is `100`. |
| Edge batch event count | `EdgeTelemetryIngestion__MaxBatchEventCount` | **P1** | Maximum items accepted by one telemetry replay or production-history replay request; default is `100`. Request body size remains bounded separately by execution-endpoint security. |
| Enable retention purge | `DataRetention__Enabled` | **P1** | Default `true`; disable only for controlled maintenance/debugging. |
| Retention schedule | `DataRetention__IntervalHours` | **P2** | Default `24` hours. The job runs once at startup, then on this interval. |
| Raw telemetry retention | `DataRetention__HeartbeatDays`, `DataRetention__DeviceEventDays`, `DataRetention__OperationLogDays` | **P1** | Defaults: heartbeat `30`, device events `90`, operation logs `90` days. Ticket-referenced device events are protected. |
| Processed inbox retention | `DataRetention__ProcessedSyncInboxDays` | **P1** | Default `180` days. Applies only to Processed/Ignored rows without a dead-letter reference. |
| Expired identity credential retention | `DataRetention__ExpiredIdentityCredentialDays` | **P1** | Default `30` days after expiry for refresh tokens, password-reset requests, and account invitations. Active credentials are never purged. |
| Notification delivery retention | `DataRetention__NotificationDeliveryDays` | **P1** | Default `90` days. Ordinary Delivered/PermanentFailure outbox rows are purged; pending/retryable rows and durable idempotency evidence for `deployment_failed`, `fulfillment_overdue`, and `payment_intervention` are retained. |
| Retention work limits | `DataRetention__BatchSize`, `DataRetention__MaxBatchesPerRun` | **P1** | Defaults `1000` rows per SQL delete and `20` batches per entity per run. Tune against database load. A failed retention category is logged and retried on the next scheduled run; other categories continue. |
| Inventory alert automation | `InventoryAlertAutomation__Enabled`, `__IntervalSeconds`, `__BatchSize`, `__MaxBatchesPerRun` | **P1** | Enabled by default. Reconciles Low/Empty dispenser states with actionable alerts using bounded rotating scan windows. A failed dispenser candidate is logged without blocking later candidates. |
| Empty inventory ticket creation | `InventoryAlertAutomation__CreateMaintenanceTicketForEmpty` | **P1** | Default `true`; creates one alert-linked maintenance ticket for each Empty incident. |
| Durable push delivery | `NotificationDelivery__Enabled`, `__IntervalSeconds`, `__BatchSize` | **P1** | Enabled by default. Disable only when Firebase delivery is intentionally unavailable; pending rows remain durable. |
| Push retry policy | `NotificationDelivery__ProcessingTimeoutSeconds`, `__BaseRetryDelaySeconds` | **P1** | Defaults `120` and `30` seconds. Processing rows older than the timeout are reclaimed; transient failures use exponential delay. |
| Fulfillment overdue reminders | `FulfillmentReminder__Enabled`, `__IntervalSeconds`, `__BatchSize` | **P1** | Enabled by default. Scans paid Manual/Packaged items with configured preparation time and enqueues one durable reminder per eligible recipient. It never changes order state. |
| Deployment failure notifications | `DeploymentFailureNotification__Enabled`, `__IntervalSeconds`, `__BatchSize` | **P1** | Enabled by default. Reconciles committed failed Full Edge/Low-cost deployments into one durable notification per eligible recipient. |
| Enable MQTT command wake-up | `EdgeCommandMqtt__Enabled` | **P1** | Default `false`; enable only when a broker and endpoint subscriptions are configured. Polling remains authoritative. |
| MQTT broker host/port | `EdgeCommandMqtt__Host`, `EdgeCommandMqtt__Port` | **P0 Feature** | Required when MQTT wake-up is enabled. Defaults are `localhost:1883` for local development only. |
| MQTT TLS | `EdgeCommandMqtt__UseTls` | **P0 Secret/Security** | Enable for production broker connections. Certificate trust uses the host OS trust store. |
| MQTT credentials | `EdgeCommandMqtt__Username`, `EdgeCommandMqtt__Password` | **P0 Secret** | Supply through deployment secrets when broker authentication is enabled; do not commit values. |
| MQTT client/topic | `EdgeCommandMqtt__ClientId`, `EdgeCommandMqtt__TopicPrefix` | **P1** | Client id must be unique per backend instance; topic prefix defaults to `icebot`. |
| MQTT publish resilience | `EdgeCommandMqtt__ConnectTimeoutSeconds`, `EdgeCommandMqtt__PublishTimeoutSeconds`, `EdgeCommandMqtt__PublishRetryCount`, `EdgeCommandMqtt__PublishRetryDelayMilliseconds` | **P2** | Defaults to 5-second connect timeout, 6-second attempt timeout, one retry, and 250 ms base delay. Keep retries low because periodic pull is authoritative. |
| Enable MQTT Edge uplink | `EdgeUplinkMqtt__Enabled` | **P1** | Default `false`; enables the shared Cloud consumer for typed Edge telemetry/readiness/execution evidence. HTTPS remains the recovery fallback. |
| MQTT uplink connection | `EdgeUplinkMqtt__Host`, `__Port`, `__UseTls`, `__Username`, `__Password`, `__ClientId` | **P0 Secret/Security** | Use a backend broker identity that can subscribe only to the shared uplink filter and publish endpoint result topics. Client id must be unique per backend replica. |
| MQTT uplink routing | `EdgeUplinkMqtt__TopicPrefix`, `__ConsumerGroup` | **P1** | Defaults to `icebot` and `icebot-cloud-uplink`. All replicas must use the same consumer group so one message is processed by one consumer. |
| MQTT uplink limits | `EdgeUplinkMqtt__ConnectTimeoutSeconds`, `__PublishTimeoutSeconds`, `__ReconnectDelaySeconds`, `__MaxPayloadBytes`, `__MaxConcurrentMessages` | **P1** | Defaults to 5 seconds, 6 seconds, 5 seconds, 256 KiB, and 16 concurrent messages. Keep payload bounded; use object storage for files. |
| MQTT credential provisioning | `MqttCredentialProvisioning__Enabled`, `__Provider`, `__Host`, `__Port`, `__UseTls` | **P0 Feature/Security** | Enables execution-endpoint subscriber provisioning through Mosquitto Dynamic Security. Disabled by default. Production requires TLS. |
| MQTT dynsec administrator | `MqttCredentialProvisioning__AdminUsername`, `__AdminPassword` | **P0 Secret** | Broker-control identity used only for client credential lifecycle. Supply from secret manager; never reuse backend publisher or endpoint credentials. |
| MQTT endpoint subscriber role | `MqttCredentialProvisioning__SubscriberRole` | **P1** | Existing configuration key retained for compatibility; the role is now bidirectional. Restrict `%u` to subscribe its `commands/available` and `uplink/results` topics and publish only allowed typed messages below its own `uplink/` prefix. |
| MQTT credential resilience | `MqttCredentialProvisioning__TimeoutSeconds`, `__RetryCount`, `__RetryDelayMilliseconds`, `__ReconciliationIntervalSeconds`, `__ReconciliationBatchSize` | **P2** | Defaults to 10-second command timeout, one transport retry, 500 ms base delay, a 60-second reconciliation scan, and 100 candidates per batch. Stale provision/rotation requires operator retry; stale revocation is retried automatically. |

Broker startup, endpoint-scoped ACL provisioning, Edge subscription behavior, and production TLS rules are defined in [MQTT Operations](MQTT_OPERATIONS.md).

## Observability And Diagnostics

| Area | Configuration key | Priority | Deploy action |
| --- | --- | --- | --- |
| Expose stack traces | `ErrorHandling__ExposeStackTrace` | **P1** | Use the safe appsettings default `false`. |
| Serilog OTLP sink | `Observability__Serilog__OtlpSinkEnabled` | **P2** | Default `false`; enable when an OTLP log collector is deployed. |
| OpenTelemetry OTLP export | `Observability__OpenTelemetry__OtlpExporterEnabled` | **P2** | Default `false`; enable when an OTLP collector is deployed. |
| OpenTelemetry endpoint | `Observability__OpenTelemetry__OtlpEndpoint` | **P0 Feature** | Required when either OTLP exporter is enabled; environment-specific. |
| Debug body logging | `Observability__DebugBodyLogging__Enabled` | **P1** | Keep the safe appsettings default `false`; enable only for controlled debugging. |
| Diagnostics external ping | `Diagnostics__EnableExternalPing` | **P2** | Default `false`; enable only for controlled provider diagnostics. |
| External ping timeout | `Diagnostics__ExternalPingTimeoutSeconds` | **P2** | Use appsettings default `5` unless provider latency requires tuning. |

## Operational Endpoints

Use these for deployment checks:

```text
GET /health
GET /health/ready
GET /management/diagnostics/health
GET /info
```

For the PostgreSQL migration, MinIO, endpoint seed, and artifact-to-active-deployment verification workflow, use [Robot Artifact Operational Smoke Test](ROBOT_ARTIFACT_OPERATIONAL_SMOKE.md).

`/info` may include build metadata if these values are provided:

```text
BUILD_COMMIT
BUILD_TIME
```

For CI/CD diagnostics:

```http
GET /management/diagnostics/health
X-Diagnostics-Key: <Diagnostics__ApiKey>
```

This endpoint returns safe checks for database connectivity, migration status, and required config presence. It does not return secret values.

Realtime SMTP, Firebase, and PayOS checks are disabled by default. Enable them only for CI/CD or controlled diagnostics:

```text
Diagnostics__EnableExternalPing=true
Diagnostics__ExternalPingTimeoutSeconds=5
```

When enabled, diagnostics performs provider reachability checks without sending email or creating payment sessions. `/health/ready` still checks database readiness only.

## Notes

- CORS allows any origin only in Development when no origin is configured. Deployed environments must set `Cors__AllowedOrigins__0` and additional indexed values as needed.
- Firebase can be disabled with `Firebase__Enabled=false`, but Google/Firebase login paths will then return service-unavailable behavior.
- SMTP failures must not make account onboarding unrecoverable; admins can resend invitations.
- PayOS webhook/payment behavior depends on correct public return/cancel URLs and checksum key.
- Robot artifact uploads store Lua files in S3-compatible object storage. Use MinIO for local/dev and S3-compatible cloud object storage in production. PostgreSQL stores metadata only.
- Set `ErrorHandling__ExposeStackTrace=false` and `Observability__DebugBodyLogging__Enabled=false` in deployed environments.
- For production observability, set `Observability__OpenTelemetry__OtlpExporterEnabled=true` for traces/metrics and `Observability__Serilog__OtlpSinkEnabled=true` for structured logs, then configure the OTLP endpoint to point to your collector.
- Set `Diagnostics__ApiKey` outside Development before using `/management/diagnostics/health`.
- Keep `Diagnostics__EnableExternalPing=false` unless the deployment check intentionally needs live SMTP/Firebase/PayOS reachability.
- IoT runtime endpoints require HTTPS. Full Edge client certificates are accepted at the TLS handshake and authenticated by the provisioned SHA-256 fingerprint in WebAPI; do not terminate mTLS at an untrusted proxy.
- After applying the execution transport-security migration, rotate any pre-existing low-cost credential binding that has no ECDSA public key and any Full Edge binding whose reference is not the normalized certificate SHA-256 fingerprint.

## Related Docs

- [Observability](OBSERVABILITY.md)
- [MQTT Operations](MQTT_OPERATIONS.md)
- [Robot Artifact Operational Smoke Test](ROBOT_ARTIFACT_OPERATIONAL_SMOKE.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
````

## File: docs/api/API_SURFACE_RULES.md
````markdown
# API Surface Rules

This document defines the backend API surface categories for IceBot. It is an ownership map, not a full endpoint contract. Detailed request/response contracts belong in Swagger, feature docs, or integration docs such as [IoT Contract](../iot/IOT_CONTRACT.md).

Controller attributes, generated OpenAPI, and the GraphQL schema are the exact
executable endpoint inventory. Documentation route lists are curated
ownership/usage indexes. Specialized flow documents may repeat a route only to
explain behavior owned by that flow.

## Search Keywords

`API surface`, `route prefix`, `tablet API`, `customer API`, `management API`, `current account`, `me API`, `authentication`, `auth`, `login`, `external login`, `Firebase Google login`, `refresh token`, `forgot password`, `reset password`, `change password`, `invitation`, `accept invitation`, `account onboarding`, `payment webhook`, `IoT API`, `edge API`, `health`, `info`

## Purpose

Use separate API surfaces for separate client workflows.

Do not reuse an endpoint only because it can return similar data. Tablet/customer, internal management, current account, provider webhook, and IoT/edge APIs have different security, stability, payload, and ownership needs.

Application services and stores may still reuse lower-level query/persistence logic.

## Surface Categories

| Surface | Route pattern | Primary clients | Auth direction |
| --- | --- | --- | --- |
| Tablet/customer | `/api/v1/kiosks/...`, `/api/v1/orders...` | Flutter tablet/customer checkout flow | Public v1 endpoints with idempotency and validation |
| Internal management | `/api/v1/management/...` | Back-office UI for SystemAdmin, Manager, Staff, Technician, OrgAdmin depending on policy | JWT + scoped RBAC policy |
| Current account | `/api/v1/me...` | Logged-in internal user managing own profile/security | JWT |
| Authentication | `/api/v1/authentication...` | Internal login/password recovery clients | Mixed public/login and token flows |
| Payment provider webhook | `/api/v1/payments/.../webhook` | Payment provider callbacks | Provider signature verification |
| IoT/edge | `/api/v1/iot/...` | Local edge backend/kiosk runtime | Full Edge mTLS certificate pinning or low-cost ECDSA P-256 signed request over TLS |
| Operations health/info | `/health...`, `/info` | Load balancer, deployment monitor, developer tooling | Public operational probe |

## API Lookup

| Area | Main routes | Read when asking about |
| --- | --- | --- |
| Authentication and password recovery | `/api/v1/authentication/*` | login, external login, Firebase Google login, refresh token, forgot password, reset password, accept invitation |
| Current account | `/api/v1/me`, `/api/v1/me/profile`, `/api/v1/me/password`, `/api/v1/me/access`, `/api/v1/me/notification-devices` | own profile, edit profile, change password, inspect current token access, and manage the caller's FCM registrations |
| Account management | `/api/v1/management/accounts/*` | create internal account, invitation link generation, assign/update roles, effective access, disable account, set password |
| Organization management | `/api/v1/management/organizations/*` | create/update/activate/disable organizations, list and view organizations |
| Store management | `/api/v1/management/stores/*`, `/api/v1/management/organizations/*/stores` | create/update/activate/disable stores, list and view stores |
| Kiosk management | `/api/v1/management/kiosks/*`, `/api/v1/management/stores/*/kiosks` | create/update/set status of kiosks, list and view kiosks |
| Device management | global index: `/api/v1/management/devices`; kiosk-owned operations: `/api/v1/management/kiosks/{kioskId}/devices/*` | create/update/set management status/retire devices, list and view devices |
| Device catalog | `/api/v1/management/device-types/*`, `/api/v1/management/device-models/*` | lookup device type/model IDs; SystemAdmin authors the global hardware catalog |
| Execution endpoint management | global index: `/api/v1/management/execution-endpoints`; kiosk-owned operations: `/api/v1/management/kiosks/{kioskId}/execution-endpoints/*` | create, provision, inspect, configure compatibility, disable/reactivate, rotate credentials, and retire Full Edge or low-cost execution endpoints |
| Tenant scope lookup | GraphQL `tenantTree`, `/api/v1/management/role-scope-options` | select valid organization/store/kiosk scopes for RBAC and management navigation |
| Global product templates | `/api/v1/management/product-templates/*` | SystemAdmin-only platform template authoring |
| Organization product and menu management | `/api/v1/management/organizations/{organizationId}/products/*`, `/api/v1/management/organizations/{organizationId}/menus/*` | tenant-scoped catalog/menu/pricing operations |
| Robot configuration management | `/api/v1/management/organizations/{organizationId}/robot-authoring-imports/*`, `/api/v1/management/organizations/{organizationId}/robot-artifacts`, `/api/v1/management/organizations/{organizationId}/robot-programs/*`, `/api/v1/management/organizations/{organizationId}/configuration-releases/*`, `/api/v1/management/kiosks/{kioskId}/configuration-deployments/*` | stage and validate one Fairino authoring bundle, materialize Draft artifacts/programs, publish immutable robot configuration, and request/read/rollback Full Edge or low-cost controller deployment |
| Global robot artifact templates | `/api/v1/management/robot-artifact-templates/*`, `/api/v1/management/robot-artifact-template-contracts/*`, `/api/v1/management/organizations/{organizationId}/robot-artifacts/from-template` | manage reusable global Lua templates and their platform-owned technical contracts, then clone a Published template into an organization-owned Draft artifact |
| Back-office order operations | GraphQL `orders`, `order`, `orderStatusHistory`, `orderExecutionAttempts`; REST `/api/v1/management/orders/*`, `/api/v1/management/refunds/*` | scoped order reads in GraphQL; cancellation, redispatch, refund-required, manual refund commands, and restricted execution diagnostics in REST |
| Inventory management | `/api/v1/management/inventory/*`, `/api/v1/management/kiosks/{kioskId}/inventory/*`, `/api/v1/management/kiosks/{kioskId}/configuration-releases/{releaseId}/inventory-readiness` | dispenser topology, release readiness, state, stock movement history, refill, estimate adjustment |
| Operations telemetry | `/api/v1/management/kiosks/{kioskId}/heartbeats`, `/api/v1/management/kiosks/{kioskId}/device-events`, `/api/v1/management/kiosks/{kioskId}/operation-logs` | kiosk connectivity history, device warnings/errors, and Edge local operation logs |
| Sync dead-letter operations | `/api/v1/management/sync-dead-letters` | SystemAdmin inspection, typed retry, retry audit, resolve, and ignore |
| Maintenance support | `/api/v1/management/maintenance-tickets/*` | manual operations/support tickets for kiosk/device/order/event issues |
| Tablet checkout | `/api/v1/kiosks/...`, `/api/v1/orders...` | runtime menu, place order, payment session, payment status |
| Edge integration | `/api/v1/iot/...` | command pull, command ack, execution reports, event replay, heartbeat, configuration sync |
| Operations probes | `/health`, `/health/ready`, `/info` | liveness, readiness, build/service info |

## Management Route Ownership

Management routes should expose the resource owner in the path when the owner is required for validation, scope checks, or safe mutation.

- Use global `/management/{resource}` routes for platform-wide catalogs or cross-scope management indexes. Examples: organizations, stores, kiosks, devices, accounts, roles, device types/models, product categories, ingredients, product templates, robot artifact templates, alerts, refunds, maintenance tickets, and sync dead letters. These routes must still apply scoped filtering for non-SystemAdmin users.
- Use `/management/organizations/{organizationId}/...` for tenant-owned authoring and configuration such as products, menus, robot artifacts, robot programs, configuration releases, product options, and recipes.
- Use `/management/stores/{storeId}/...` when the resource is created or listed under a store owner, such as kiosks.
- Use `/management/kiosks/{kioskId}/...` for physical kiosk ownership: devices, execution endpoints, telemetry reads, inventory topology, dispenser topology actions, release deployment, deployment reads, rollback, and inventory readiness.
- Use `/management/orders/{orderId}/...` for order-owned workflow commands and audit reads such as refunds, redispatch, status history, execution attempts, cancellation, and refund-required transitions.

Create and mutation routes should prefer the parent owner path. Global list/search routes are acceptable when they are management indexes and the handler/store applies explicit scoped filtering. Do not choose a global route only because the child id is globally unique; if the parent owner changes validation meaning or reduces operator mistakes, put the parent in the route.

## Management Route Naming

- Name collection routes after the owned resource. Use `POST` on the collection when one request creates one or more resources of that collection; transport cardinality does not belong in route names. For example, multipart upload uses `POST .../robot-artifacts`, not `/bulk`.
- Put a lifecycle action after the item or collection it changes: `/{id}/publish`, `/{id}/retire`, or `/publish` for an atomic selected set. Do not alternate between `publish-bulk` and `bulk-publish`.
- Name workflow commands after their observable result. Use `materialize`, `publish-resources`, and `create-release-draft`; avoid generic actions such as `apply` or `process`.
- Use one verb-object order across related commands. Robot composition uses `preview-composition` and `confirm-composition`.
- Distinguish platform templates from tenant-owned resources in the noun itself when both are exposed. Platform contracts use `/robot-artifact-template-contracts`; tenant contracts use `/organizations/{organizationId}/robot-artifact-technical-contracts`.
- A global route without an owner is allowed for a read-only cross-scope index, such as `/configuration-deployments`. Item mutation and detail routes still include the physical or tenant owner.
- Do not retain legacy aliases before first production deployment. Controller attributes, generated OpenAPI, flow docs, and frontend operation catalogs must change together.

## Tablet / Customer APIs

Tablet/customer APIs model the checkout and order-status workflow. They must not use `/management/...`.

Current examples:

```text
GET /api/v1/kiosks/{kioskId}/runtime-menu
POST /api/v1/orders
GET /api/v1/orders/{orderId}
POST /api/v1/orders/{orderId}/payment-sessions
GET /api/v1/orders/{orderId}/payment-status
POST /api/v1/orders/{orderId}/cancel
```

Rules:

- Keep payloads small and UX-oriented.
- Do not expose internal management fields or back-office-only metadata.
- Use idempotency for retried checkout/payment commands.
- `Idempotency-Key` is required for order placement, payment-session creation, and refund requests. The backend scopes it to the kiosk, order, or payment transaction; clients must not reuse one key for a different request body.
- `POST /orders` returns an `orderAccessToken` bearer capability. `GET /orders/{orderId}`, payment-session creation, payment-status polling, and customer cancellation require that token in the `Order-Access-Token` header. The token is scoped to one order and expires after 24 hours.
- Online sales require `KioskStatus.Active`, `KioskOperationalState.Operational`, active parent tenant scope, and a current kiosk connectivity projection of `Online` or `Degraded`.
- `KioskStatus` is lifecycle state. `KioskOperationalState` controls whether an otherwise active kiosk accepts new work. Connectivity is a separate observed projection and never changes either state automatically.
- `PATCH /api/v1/management/stores/{storeId}/kiosks/{kioskId}/operational-state` requires `kiosks.manage`, a typed state, and an audit reason. `Maintenance`, `Cleaning`, and `Restocking` are rejected while an execution is running; `EmergencyStopRequested` remains available to hold new work and request immediate safety intervention.
- `EmergencyStopRequested` is Cloud intent, not evidence that the robot physically stopped. Only a typed Edge safety projection may report `ExecutionSafetyState.EmergencyStopped`; V1 does not send a hardware stop command.
- Pausing a kiosk holds paid queued work. It does not cancel/refund orders or assert that an accepted/running execution failed. `ExecuteOrder` commands are not created or delivered until the kiosk returns to `Operational`; deployment and recovery commands remain deliverable.
- Offline-created order sync is not part of the current API. It requires a separate offline-session authority, snapshot, payment, quota, expiry, replay, and reconciliation contract before an ingest endpoint is added.
- Cloud sales catalog snapshots do not replace Local Edge runtime truth for inventory/device/robot availability.

## Internal Management APIs

The complete internal management REST and GraphQL route catalog is owned by [Management API Surface](MANAGEMENT_API_SURFACE.md). This file retains only cross-cutting API categories, ownership rules, and client-facing contracts.

## Current Account APIs

Use `/me` only for the authenticated user's own account/profile/security surface.

Current examples:

```text
GET /api/v1/me
GET /api/v1/me/access
PUT /api/v1/me/profile
PUT /api/v1/me/password
GET /api/v1/me/notification-devices
PUT /api/v1/me/notification-devices/{installationId}
DELETE /api/v1/me/notification-devices/{installationId}
```

Rules:

- Do not use `/me` for business resources such as orders, kiosks, reports, or maintenance tickets.
- Password recovery is not `/me` because the user may be logged out.
- `/me/access` reports the caller's current token roles and effective scoped ids. It is not a fresh database authorization recalculation.
- Notification-device routes are self-service FCM registration only. They never accept `AccountId`, expose a push token/hash, or grant trusted-session behavior. Registration is serialized by both account installation and token identity. Reassigning or invalidating a registration removes the stored raw provider token while retaining its hash as audit correlation. Delivery selects registrations only while their owning account is Active.

## Authentication And Password Recovery APIs

Search keywords: `authentication`, `auth`, `local login`, `username password login`, `Firebase Google login`, `external login`, `refresh token`, `revoke refresh token`, `forgot password`, `reset password`, `change password`, `accept invitation`, `invitation link`, `current account password`, `management accounts`.

Management owns the allowed authentication methods for an internal account. Google login resolves and validates the verified provider email against the configured `GoogleEmail`, then binds `GoogleSubjectId` on first successful login. It must not fall back to `Account.Email` or overwrite the configured Google email from token claims.

Current examples:

```text
POST /api/v1/authentication/login
POST /api/v1/authentication/external-login
POST /api/v1/authentication/refresh-token
POST /api/v1/authentication/revoke-refresh-token
POST /api/v1/authentication/forgot-password
POST /api/v1/authentication/reset-password
POST /api/v1/authentication/accept-invitation
```

Rules:

- Login and forgot/reset password endpoints can be public.
- Account management remains under `/management/accounts`.
- Change password for a logged-in user stays under `/me/password`.
- Refresh rotation rechecks persisted `AccountStatus` inside the token transaction. A non-Active account has its remaining refresh sessions revoked and receives no replacement token.
- Account onboarding and invitation lifecycle rules live in [Identity Onboarding Rules](IDENTITY_ONBOARDING_RULES.md).

## Provider Webhook APIs

Provider webhook routes are provider-specific integration endpoints.

Rules:

- Verify provider signature/authenticity.
- Deduplicate provider events.
- Do not put webhooks under management or tablet surfaces.

## IoT / Edge APIs

IoT/edge APIs are for local edge backend and kiosk runtime integration.

Current direction:

```text
POST /api/v1/iot/execution-endpoints/{endpointId}/commands/pull
POST /api/v1/iot/execution-endpoints/{endpointId}/commands/{commandId}/ack
POST /api/v1/iot/execution-endpoints/{endpointId}/commands/{commandId}/reports
POST /api/v1/iot/execution-endpoints/{endpointId}/device-events
POST /api/v1/iot/execution-endpoints/{endpointId}/telemetry-events
POST /api/v1/iot/execution-endpoints/{endpointId}/production-sync/events
GET /api/v1/iot/execution-endpoints/{endpointId}/production-sync/checkpoint
POST /api/v1/iot/execution-endpoints/{endpointId}/production-sync/state-summaries
POST /api/v1/iot/execution-endpoints/{endpointId}/heartbeat
POST /api/v1/iot/execution-endpoints/{endpointId}/readiness
```

Rules:

- Do not use internal account JWT as the long-term kiosk runtime credential.
- IoT routes no longer accept plaintext `X-Execution-Credential`. Full Edge endpoints authenticate with a directly presented client certificate pinned by SHA-256 fingerprint. Low-cost endpoints authenticate each raw HTTP request with ECDSA NIST P-256, timestamp, and a database-deduplicated nonce over TLS.
- Execution endpoint reads are tenant-scoped and never return credential material. Full Edge provisioning accepts `ClientCertificateSha256Fingerprint`; low-cost provisioning accepts `EcdsaPublicKeyPem`. Both require at least one supported robot target and assign exactly one profile identity: `FullEdgeRuntimeId` or `ControllerId`.
- Cryptographic transport verification belongs to WebAPI. Application handlers retain endpoint/kiosk/status/credential-binding checks but do not receive HTTP certificates, signatures, or plaintext credentials.
- Heartbeat ingest derives trust from the authenticated execution endpoint, validates `originNodeId` against its bound profile identity, and deduplicates by `(kioskId, originNodeId, heartbeatSequence)`. Unique stale sequences remain history-only; only the newest `Online`/`Degraded` sequence advances `Kiosk.LastOnlineAt` using Cloud receive time or changes current connectivity.
- `KioskStatus` owns management lifecycle (`Provisioning`, `Active`, `Disabled`, `Retired`). `KioskOperationalState` independently owns sales/dispatch admission (`Operational`, `PausedByOperator`, `Maintenance`, `Cleaning`, `Restocking`, `EmergencyStopRequested`, `OutOfService`). `KioskConnectivityProjection` separately owns observed connectivity (`Unknown`, `Online`, `Degraded`, `Unreachable`). Heartbeats and timeout reconciliation never mutate lifecycle or operational state.
- Heartbeat ingestion and timeout reconciliation serialize by kiosk. `KioskStatusChanged` carries lifecycle fields for management transitions or connectivity fields for observed transitions, and is never emitted for duplicate heartbeat delivery or an unchanged projection.
- Readiness ingest is a typed complete snapshot per execution endpoint. `stateRevision` is monotonic and persistent across ordinary reboot for the same executor identity; a newer revision replaces readiness/activity/safety and the complete capability set. It does not mutate `KioskStatus`.
- Machine-produced menu/order readiness requires at least one Active endpoint whose latest projection is Ready and Safe, was received by Cloud within `EdgeTelemetryIngestion__ReadinessTimeoutSeconds`, and whose available capability set covers every route binding. Deployment preview and production-package workspace use the same freshness rule. Dispatch additionally requires the selected endpoint to be Idle.
- Execution route `RequiredCapabilitiesJson` is optional. When supplied, it must use the V1 bounded schema with `schemaVersion = 1` and `requires[]` capability objects. Requirement codes must be declared by that route's robot bindings; unknown JSON fields are rejected. Cloud validates required capability codes against endpoint readiness. A required `minVersion` is accepted into the immutable contract but makes the route unavailable to runtime-menu and checkout and blocks deployment/dispatch with `CapabilityVersionUnverifiable` until endpoint readiness reports comparable capability versions; it is never silently ignored.
- Device-event ingest accepts one `Warning`, `Error`, or `Critical` evidence record, verifies device/kiosk ownership, deduplicates globally by `eventId`, and publishes `DeviceEventCreated` only after a new row commits. Raw payload remains excluded from management reads.
- Newly accepted current `Error` or `Critical` device events also create one Open Alert atomically and publish `AlertChanged` after commit. Events older than `EdgeTelemetryIngestion__AlertAutomationMaxEventAgeMinutes` remain audit history but do not trigger alert/push automation. Warning events do not auto-create alerts.
- Supported robot targets are a complete replacement contract and may change only while the endpoint is `Provisioning` or `Disabled`. A device-specific target must reference a device attached to the same kiosk.
- Endpoint activation, credential rotation, disable/reactivate, and retirement are management operations. They do not install artifacts; release deployment remains a separate command flow.
- MQTT subscriber credentials have a separate endpoint-scoped lifecycle: `POST/PATCH/DELETE /management/kiosks/{kioskId}/execution-endpoints/{id}/mqtt-credential`. Provision and rotation return a generated password once; normal reads expose only username, status, and credential version. HTTPS transport credentials are never reused for MQTT.
- `POST /api/v1/iot/execution-endpoints/{endpointId}/commands/{commandId}/reports` is the current V1 execution/deployment report ingest endpoint. It records an immutable `SyncEventInbox` envelope; the same source event id with a different command or payload returns conflict.
- Order-level reports update `OrderExecutionRecord` and must agree with available job evidence before a final summary is accepted. Job-level reports require immutable `sourceProductionJobId`, `orderItemId`, `productionUnitNo`, and `productionUnitQuantity`; they update the matching `ProductionExecutionRecord`, derive effective unit counts, and advance the item/order lifecycle when evidence is decisive. A source job cannot be rebound, and unit ranges cannot overlap within one command. Stock evidence is job-scoped and commits with the unit projection. The ingestion coordinator keeps one database transaction while deployment, order/job projection, and stock persistence use separate aggregate ports.
- `POST /api/v1/iot/execution-endpoints/{endpointId}/telemetry-events` replays only typed Heartbeat, DeviceEvent, and LocalLog items with item-level atomicity and per-item results.
- `POST /api/v1/iot/execution-endpoints/{endpointId}/production-sync/events` owns durable ProductionEvent replay and contiguous sequence acknowledgement. Production history does not replace typed command reports or stock movements.
- Production events are ordered by monotonic `(originNodeId, sequenceNumber)`. Cloud may store a later event across a gap, but acknowledges only `ProductionEventCheckpoint.LastContiguousSequenceNumber`.
- Latest-state summaries use a separate `(sourceExecutorId, summaryKind, stateRevision)` channel. Cloud applies only newer revisions; summaries never advance the production-history checkpoint or prove that historical events were received.
- Successful telemetry items receive processed `SyncEventInbox` receipts after their typed destination commits. A ProductionEvent is itself stored in `SyncEventInbox` with its sequence and advances its checkpoint in the same transaction.
- Keep IoT DTOs separate from EF entities.
- After an `EdgeCommand` commits, MQTT publishes a best-effort endpoint-scoped `CommandAvailable` wake-up for `ExecuteOrder` and `DeployConfiguration`. MQTT is notification only; Edge pulls command details through the API and periodic polling remains authoritative.

## Operations Health APIs

Health APIs are operational probes, not business APIs.

Current examples:

```text
GET /health
GET /health/ready
GET /management/diagnostics/health
GET /info
```

Rules:

- `/health` is a lightweight public liveness probe and does not check database or provider connectivity.
- `/health/ready` is a public/internal-safe readiness probe. In V1 it checks PostgreSQL database connectivity only. SMTP, Firebase, and PayOS network connectivity do not block readiness in V1.
- Database failures return a generic `"Database unavailable"` reason. Raw connection strings, credentials, or exception details must not be exposed.
- `/management/diagnostics/health` is a CI/CD and dev/ops diagnostics probe. It checks PostgreSQL, migration status, and safe config presence for JWT, SMTP, Firebase, and PayOS.
- Realtime SMTP, Firebase, and PayOS pings are opt-in through `Diagnostics:EnableExternalPing=true`. They must not block `/health/ready`.
- Diagnostics responses must not expose secret values, connection strings, raw provider exceptions, SMTP passwords, PayOS checksum keys, or Firebase credentials.
- `Diagnostics:ApiKey` controls diagnostics access. In non-development environments, configure it and send `X-Diagnostics-Key`.
- `/info` exposes non-sensitive service/build metadata.
- Do not require user JWT for health probes.

## Read Model API Boundaries

To ensure stability, performance, and security, read-model endpoints are strictly scoped to their intended UI or integration workflows. They must not be expanded to aggregate cross-cutting operational or reporting details.

### 1. Tenant Navigation & Scope Selection Boundaries
* **Endpoints:** 
  - GraphQL `tenantTree`
  - `GET /api/v1/management/role-scope-options`
* **Purpose:** Administrative layout navigation and validation of scopes when creating/assigning user roles.
* **Includes:** Hierarchy structural identifiers (Organization -> Store -> Kiosk) and scope codes.
* **EXCLUDES:** Revenue metrics, active alerts, device health, inventory levels, or machine runtime logs.
* **Ownership:** Excluded metrics belong to dashboard or reporting-specific APIs.

### 2. Kiosk Sales Menu Boundaries
* **Endpoint:** `GET /api/v1/kiosks/{kioskId}/runtime-menu`
* **Purpose:** Rendering customer-facing catalog pricing and availability on the order tablet.
* **Includes:** Product name, variant codes, prices, discount figures, images, and recipe versions.
* **EXCLUDES:** Recipe preparation details (coordinates, Fairino robot points), manufacturing cost margin data, and live dispenser levels.
* **Ownership:** Deep robot configuration lives in IoT sync profiles, while cost metrics belong to product inventory reporting.

### 3. Customer Order Tracking Boundaries
* **Endpoints:**
  - `GET /api/v1/orders/{orderId}`
  - `GET /api/v1/orders/{orderId}/payment-status`
* **Purpose:** Real-time customer receipt and preparation status tracking.
* **Includes:** Quantity, billing totals, payment confirmation, preparation state, and tablet-friendly status projections (`CustomerStatus`, `CustomerStatusMessage`, `CanRetryPayment`, `RequiresStaffSupport`).
* **EXCLUDES:** Internal order-item, order-payment, payment-transaction, and order state-machine enums; raw payment provider callback bodies; device error codes; robot joint telemetry.
* **Ownership:** System error analytics are scoped to maintenance/operations portals, not client order details.

## Franchise Onboarding Workflow

Franchise onboarding is an organization-owned command workflow, not a global
setup endpoint. It creates/checkpoints Store and Kiosk provisioning and may
install a Production Package, but deliberately stops at `ReadyForActivation`.
Activation, publication, and deployment remain explicit existing commands.

```http
POST /api/v1/management/organizations/{organizationId}/franchise-onboardings
GET  /api/v1/management/organizations/{organizationId}/franchise-onboardings
GET  /api/v1/management/organizations/{organizationId}/franchise-onboardings/{onboardingId}
POST /api/v1/management/organizations/{organizationId}/franchise-onboardings/{onboardingId}/resume
POST /api/v1/management/organizations/{organizationId}/franchise-onboardings/{onboardingId}/cancel
```

Start requires `Idempotency-Key`. Reusing a key with the same payload resumes or
returns the same workflow; reusing it with a different payload returns conflict.
Checkpoints allow retry after a process interruption without recreating completed
resources. Cancel applies only to Pending/Failed workflows and does not delete
resources already provisioned. Running or ReadyForActivation workflows reject it.

The collection read supports `status`, `pageNumber`, and `pageSize`. It is
always scoped by the organization in the route.

Notification-delivery diagnostics are organization-owned operations reads.
They expose delivery state and bounded failure details, never the raw FCM
payload:

```http
GET /api/v1/management/organizations/{organizationId}/notification-deliveries
GET /api/v1/management/organizations/{organizationId}/notification-deliveries/{deliveryId}
POST /api/v1/management/organizations/{organizationId}/notification-deliveries/{deliveryId}/requeue
```

The two GET endpoints require `operations.diagnostics` and enforce the caller's
effective organization/store/kiosk scope in the database query.

The requeue command requires `notifications.manage`, a 3-500 character reason,
and a `PermanentFailure` delivery. It preserves `DeliveryKey`, resets the retry
budget, and appends a `NotificationDeliveryRequeued` operation-log record. It
does not replay or mutate the source Alert, deployment, ticket, or Order.

## API Result And Error Handling

Controller-facing Application handlers return `ApiResult<T>` or `PagedResult<T>`, and controllers should preserve the wrapper status code:

```csharp
return StatusCode(result.StatusCode, result);
```

Rules:

- `ApiResult<T>.StatusCode` must match the HTTP response status code.
- `InternalResult<T>` is not an API response contract and must not be returned directly by controllers.
- `AppException` subclasses must preserve their intended HTTP status through `GlobalExceptionMiddleware`.
- Middleware must not collapse `NotFoundException`, `ForbiddenException`, or `ConflictException` into `400 Bad Request`.
- Provider/system failures may include `SystemError` for diagnostics, but public responses must not expose secrets or sensitive config.

Recommended status use:

| Case | Status |
| --- | --- |
| Read/update success | `200 OK` |
| Created success | `201 Created` |
| Validation failure | `400 Bad Request` |
| Unauthorized | `401 Unauthorized` |
| Forbidden/scoped denied | `403 Forbidden` |
| Resource not found | `404 Not Found` |
| Business conflict/duplicate | `409 Conflict` |
| Provider/system failure | `500 Internal Server Error` unless a more specific application status is intentionally returned |

## Validation Strategy

Current v1 validation convention:

- Do not introduce FluentValidation yet.
- **Request DTO / DataAnnotations (Format & Syntax):** Use DataAnnotations for simple request DTO shape validation, such as required fields, string length, numeric range, and basic format.
- **Enum Inputs:** Send enum values as strings. JSON request bodies do not accept integer enum values.
- **Application Validators / Rule Helpers (Cross-Field / Request-Level):** Use static `RequestValidator` / rule helper classes for cross-field or request-level rules that do not need database access.
- **Handlers & Stores (Business constraints & Database-dependent):** Use handlers and stores for database-dependent validation, such as uniqueness, parent existence, active parent checks, and tenant-scope ownership.
- **Domain Methods (Invariants):** Use domain methods for entity invariants and state transitions.
- **Failure Returns & Exceptions:**
  - Handlers should return `ApiResult<T>.Fail(..., 400)` (or `409 Conflict` / `404 Not Found` as appropriate) for business rule / database-dependent validation failures, rather than throwing exceptions, to preserve clean control flow.
  - `ValidationException` is strictly reserved for automatic request DTO binding and DataAnnotations validation failures caught at the controller level before the handler is invoked.
  - Domain entities throw `DomainRuleException` if invariants are violated during processing.
- **Controller Cleanup:** Gradually remove repeated controller `EnsureValidModel()` helpers by relying on `[ApiController]` plus centralized `InvalidModelStateResponseFactory`.
- **Response Shape:** Keep the current validation response shape unless a separate API contract decision changes it.

Do not move business validation into controllers. Controllers should validate transport/request shape and then call Application handlers.

## GraphQL Management Reads

GraphQL is exposed at `/graphql` as an internal read/query surface for frontend UI aggregation.

- **Scope:** Read/query only. No mutations are implemented in this phase.
- **REST Surface:** REST remains the existing contract for commands, tablet actions, payment integrations, webhooks, and IoT edge communication.
- **Implementation:** GraphQL resolvers are thin adapters that delegate execution directly to Application CQRS query handlers. No database queries are performed directly inside the resolvers.
- **Code Organization:** Keep GraphQL feature/domain-first, not GraphQL-artifact-first. Although `/graphql` is hosted from WebAPI and frontend may see one large query surface, backend code should still be organized around the owning Application/domain features such as Tenants, Orders, Devices, Inventory, and Dashboard. GraphQL root/query classes are transport composition only, similar to controllers.
- **Wiring:** Register GraphQL query extensions in `src/WebAPI/GraphQL/GraphQLEndpointExtensions.cs`; do not add feature-specific GraphQL registrations directly to `Program.cs`.
- **Authorization:** Reuses JWT authentication and tenant-scoped RBAC rules. Endpoints require authentication via the standard `[Authorize]` attribute.

## SignalR Realtime Surface

SignalR is used for push-based real-time UI notifications. It operates as a delta and invalidation stream alongside REST/GraphQL.

SignalR is not the robot runtime bus. Cloud-to-Edge and Edge-to-Cloud runtime integration should use the IoT/MQTT/sync boundary documented in [System Overview Flow](../flows/SYSTEM_OVERVIEW_FLOW.md#integration-transport-boundaries) and [IoT Contract](../iot/IOT_CONTRACT.md).

### Routes and Hubs

| Hub | Route | Scope | Events |
| --- | --- | --- | --- |
| `OrderHub` | `/hubs/orders` | Order-specific updates | `OrderStatusChanged`, `OrderItemFulfillmentChanged`, `PaymentStatusChanged`, `OrderExecutionObservationChanged` |
| `OperationsHub` | `/hubs/operations` | Kiosk & telemetry status | `OrderItemFulfillmentChanged`, `KioskStatusChanged`, `KioskOperationalStateChanged`, `ExecutionReadinessChanged`, `DeviceEventCreated`, `AlertChanged`, `MaintenanceTicketChanged`, `InventoryChanged` |
| `ManagementDashboardHub` | `/hubs/management-dashboard` | Scoped dashboards (System, Org, Store) | `DashboardInvalidated` |

### Subscription Groups

Clients must join relevant groups to receive scoped events:
- `order:{orderId}`
- `kiosk:{kioskId}`
- `dashboard:system` (SystemAdmin only)
- `dashboard:organization:{organizationId}` (OrgAdmin/Manager/Staff/Technician with appropriate scope)
- `dashboard:store:{storeId}` (OrgAdmin/Manager/Staff/Technician with appropriate scope)

### Client Behavior Rules

1. **Initial Snapshot:** Call REST or GraphQL API on page load.
2. **Real-time Delta:** Apply updates immediately when a SignalR event is received.
3. **Re-sync / Fallback:** Re-fetch full REST/GraphQL payload on connection loss, reconnect, refresh, or version gap.

## Related Docs

- [Management API Surface](MANAGEMENT_API_SURFACE.md)
- [SignalR Realtime Contract](SIGNALR_REALTIME_CONTRACT.md)
- [SignalR Smoke Test Workflow](../operations/SIGNALR_SMOKE_TEST.md)
- [Authorization Rules](AUTHORIZATION_RULES.md)
- [Identity Onboarding Rules](IDENTITY_ONBOARDING_RULES.md)
- [Naming Rules](../process/NAMING_RULES.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
- [System Flows](../flows/SYSTEM_FLOWS.md)
- [Checkout Execution Flow](../flows/CHECKOUT_EXECUTION_FLOW.md)
- [Management Read Flow](../flows/MANAGEMENT_READ_FLOW.md)
- [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
````
