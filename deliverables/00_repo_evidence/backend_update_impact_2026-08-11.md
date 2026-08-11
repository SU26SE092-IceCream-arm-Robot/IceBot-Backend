# Backend Update Impact Analysis — 2026-08-11

## Analysis Basis

This document analyzes backend changes merged into the documentation branch on 2026-08-11. It is an impact assessment only; it does not update any existing evidence inventory, baseline deliverable, or school report.

Comparison used:

- deliverables branch before synchronization: `deliverables-agent-work` at `5df7a4b`;
- common backend baseline: `b35ec3a`;
- compared upstream head: `origin/main` at `26ed548`;
- merge commit on the current branch: `3f855e9`;
- upstream change set: 25 commits after the common backend baseline, 240 changed files, approximately 38,674 insertions and 834 deletions.

Primary evidence inspected includes Git history/diffs, current `ARCHITECTURE.md`, changed material under `docs/`, implementation under `src/`, changed tests, migrations, and the existing deliverables. No build, test suite, migration, deployment, or external-provider operation was executed for this analysis.

## 1. Summary of New Backend Changes

The merge materially expands or revises the following backend areas:

1. Current-account session visibility and per-session revocation were added. Password changes now update the password and revoke refresh sessions in one transaction.
2. Internal-account management moved to organization-owned routes and now permits constrained `OrgAdmin` administration. Assignable-role options, permission visibility, tenant filtering, and account mutation rules were tightened.
3. Inventory sensor observations became a persisted, authenticated Edge-to-Cloud evidence flow with sequence/idempotency handling and optional quantity derivation.
4. Runtime-menu projection construction was separated and an optional cache with database fallback and cache metrics was introduced.
5. Robot authoring gained import listing, raw Lua artifact import into Draft programs, recipe-resolution improvements, optimistic concurrency for program artifact replacement, and revised treatment of technical declarations.
6. Production Program Bindings became a first-class organization-owned domain concept and API. Configuration release authoring/deployment now carries stronger capability, concurrency, authorization, idempotency, and audit behavior.
7. Execute-order Edge payload schema advanced to version 5 and changed robot-program capability requirements from one scalar code to a collection.
8. Payment webhook handling now records a bounded metric for verified callbacks that do not match a local transaction, while acknowledging them without creating financial or fulfillment state.
9. Maintenance-ticket assignee lookup and normal notification-delivery diagnostics were added or refined with scoped authorization.
10. Local-development bootstrap/seed automation expanded for role accounts, an execution endpoint, catalog fixtures, and robot-authoring reset support.
11. CI/CD changed to a .NET 10 pull-request build/test/image check and a main-branch GHCR image build followed by production deployment through NetBird/SSH. A `.dockerignore` was added and the earlier workflow was removed.

These changes invalidate the assumption that the previously generated deliverables fully describe the current backend revision.

## 2. Changed Bounded Contexts / Modules

| Context / module | Change summary | Documentation impact |
|---|---|---|
| Identity | Current-session list/revoke; device-name projection; transactional password-change/session revocation; organization-owned account management; role/permission/scope rule changes; local role-account bootstrap. | High: API inventory, actor permissions, identity requirements, use cases, sequences, tests, and user guide. |
| Tenants / Authorization | Organization context is mandatory for account administration; `OrgAdmin` receives constrained account/catalog/menu capabilities; scope checks and assignable-role selection changed. | High: authorization matrix, actors, API routes, SRS FR-013–FR-016/FR-021, Report 3 and Report 6. |
| Inventory | New sensor-observation entity, persistence, ingestion handler, dispenser history integration, realtime inventory change publication, and calibration-derived estimate behavior. | High: new functional requirement or expansion of FR-052/FR-053/FR-056 `[Needs Review]`; data model, Edge contract, tests. |
| Sales Catalog | Runtime-menu builder/cache abstractions, optional cache behavior, revision handling, fallback, and cache observability. OrgAdmin menu/catalog access expanded. | Medium/high: FR-045/FR-046, architecture, NFRs, operations/configuration, test plan. |
| Operations | Maintenance assignee-options query; assignment eligibility revalidation; notification-delivery safe read/diagnostic split and requeue changes. | Medium: API inventory, FR-082/FR-083/FR-086, authorization, user guide. |
| Payments | Verified-but-unmatched webhook behavior and `icebot.payment.webhook.verified_unmatched` metric; related signature/handler tests. | Medium: FR-070, webhook exception flow, observability and test cases. |
| Robot Configuration | Raw Lua import for Draft programs; import listing; recipe resolution; bundle validation/composition/linkage changes; optional technical declaration semantics; optimistic concurrency for program artifact ordering. | High: FR-089/FR-094–FR-100 and authoring workflows/diagrams require re-baselining. |
| Production Configuration | New Production Program Binding lifecycle/API/entity; release route capability requirements; revision tokens; stronger deployment request/audit/concurrency behavior. | Very high: requirements, class/sequence diagrams, DB design, API catalog, Report 3/4/5/6/7. |
| Production Packages | Upgrade behavior was adjusted to align with production configuration changes. Exact public-contract effect is `[Needs Review]`. | Medium: package upgrade requirements and sequence/database references require focused comparison. |
| Edge Integration / Sync | New `inventory-observations` MQTT uplink; execute-order payload schema v5; multiple capability codes; uplink dispatcher changes. | High: IoT contracts, sequence diagrams, functional inventory, tests, user guide. |
| Infrastructure / Operations | Redis/HybridCache-style runtime-menu caching integration, local bootstrap services, migrations, observability, CI, GHCR image publication, and production deployment workflow. | High for Report 6 installation/release content and Report 5 environment/tool evidence. |

The top-level architectural style remains a modular monolith with Clean Architecture boundaries, CQRS-lite, EF Core persistence, and sync-first Edge/Cloud integration. No evidence shows a move to microservices.

## 3. New / Changed APIs

### New API operations

| Method and route / transport | Supported change |
|---|---|
| `GET /api/v1/me/sessions` | Lists the authenticated account's active refresh-token sessions, including current-session marker, timestamps, recorded IP/user agent, and derived device name. |
| `DELETE /api/v1/me/sessions/{sessionId}` | Revokes one session owned by the authenticated account. |
| `GET /api/v1/management/maintenance-tickets/{ticketId}/assignee-options` | Returns eligible active Technician/Manager assignees scoped to the ticket; assignment is revalidated on mutation. |
| `GET /api/v1/management/organizations/{organizationId}/production-program-bindings` | Lists organization-owned production program bindings, optionally filtered by status. |
| `POST /api/v1/management/organizations/{organizationId}/production-program-bindings` | Creates an operator-confirmed Recipe-to-Published-RobotProgram binding with supported option codes and derived declaration evidence. |
| `PATCH /api/v1/management/organizations/{organizationId}/production-program-bindings/{bindingId}/retire` | Retires a production program binding. |
| `GET /api/v1/management/organizations/{organizationId}/robot-authoring-imports` | Lists robot-authoring imports. |
| `POST /api/v1/management/organizations/{organizationId}/robot-programs/{programId}/raw-lua-artifacts` | Imports bounded individual Lua files or an archive into a Draft robot program using multipart form data. |
| MQTT `inventory-observations` | Authenticated Edge uplink for a batch of dispenser-level observations; this is not a management REST write surface. |

### Changed API contracts and authorization

- Internal-account routes changed from `/api/v1/management/accounts...` to `/api/v1/management/organizations/{organizationId}/accounts...`. Requests and handlers now carry organization context and caller scope.
- `accounts.read` and `accounts.manage` now include constrained `OrgAdmin` behavior. An OrgAdmin cannot grant `SystemAdmin`, cross organizations, or mutate an account whose active assignments escape the actor's organization boundary.
- `GET /api/v1/management/accounts/assignable-role-options` replaces the former general roles-catalog use in account authoring. The global permission matrix is restricted to `SystemAdmin` through `permission-matrix.view`.
- `GET /api/v1/me/access` now includes `permissionCodes` for client capability checks. Clients must not infer permissions only from role names.
- OrgAdmin access was added to supported product-template reads, organization product/menu management, and related catalog lookup policies.
- Notification delivery has a `notifications.view` policy distinct from `notifications.manage`; normal views exclude message content/provider diagnostics.
- Configuration deployment requests now require an operator reason and write actor/scope audit evidence. Rollback additionally requires the client-observed active deployment identifier and rejects stale observations.
- Robot-program artifact replacement accepts `expectedLastModifiedAt`; a stale value returns conflict rather than overwriting another editor's ordering.
- Configuration release route authoring and authoring options changed to incorporate production bindings, capability declarations, option candidates, and release revision/concurrency information.
- Runtime-menu retrieval route is not identified as changed, but its result construction/cache behavior and revision data changed internally.
- `[Needs Review]` Generate an endpoint-level before/after contract diff from the current OpenAPI output. The repository diff establishes the operations above but this analysis did not generate an OpenAPI artifact.

## 4. New / Changed Entities and Database Behavior

### New `InventorySensorObservation`

Migration `20260731040709_AddInventorySensorObservations` creates `InventorySensorObservations` with:

- endpoint, executor, source-event, dispenser-state, device, and ingredient identities;
- observation sequence, level, observed/received times, disposition, optional derived quantity, and bounded JSON sensor evidence;
- sync provenance (`OriginNodeId`, `Version`, `SyncedAt`) and audit fields;
- restrictive FKs to `IngredientDispenserStates` and `KioskExecutionEndpoints`;
- unique idempotency on `(SourceExecutorId, SourceEventId)`; and
- indexes for dispenser history, executor/dispenser sequencing, endpoint, and sync provenance.

Applied observations can update the dispenser projection and publish an inventory-change notification. Duplicates are counted without duplicate mutation. Stale/out-of-order observations are retained as evidence but do not overwrite current state. They do not create stock movements or prove recipe consumption.

### New `ProductionProgramBinding`

Migration `20260804031725_AddProductionProgramBindings` creates `ProductionProgramBindings` and links `ExecutionRouteRobotBindings` to them. The entity relates an organization, ProductVariant/Recipe version, RobotProgram manifest, supported option codes, status, checksum, and audit/soft-delete data. FKs use restrictive deletion and the binding checksum is unique.

Migration `20260809035315_DeriveProductionBindingCapabilitiesFromContracts` then:

- adds `Assurance`, `CapabilityEvidenceStatus`, and `RequiredCapabilityCodesJson` to production bindings;
- removes the manually supplied scalar `RequiredWorkcellCapabilityCode` from production bindings;
- adds `RequiredCapabilityCodesJson` to execution-route robot-binding snapshots; and
- migrates the historical route scalar capability into a one-item JSON array where present.

Existing production bindings are intentionally classified as missing trustworthy declaration evidence. Current binding capability codes are derived from optional published technical declarations and are not certified Lua behavior.

### Other persistence behavior

- `ConfigurationRelease`, `ExecutionRoute`, and `ExecutionRouteRobotBinding` gained revision, definition, capability, and binding snapshot behavior used for concurrency and deployment/dispatch.
- The EF model snapshot and `IceBotDbContext` changed for the two new DbSets and related fields/indexes.
- Runtime-menu caching adds an infrastructure cache and configuration, but it is not a new relational entity.
- Identity session functions reuse refresh-token persistence; no new identity migration was observed in this comparison.
- `[Needs Review]` Reconcile cumulative migrations with a real schema and regenerate the database inventory. This analysis inspected migration source only and did not apply migrations.

## 5. New / Changed Business Flows

### Account and session security

Authenticated users can inspect active sessions and revoke a selected session. Password change now performs account mutation and revocation of all refresh sessions transactionally, preventing a committed password change with failed session cleanup.

Account administration is explicitly organization-owned. OrgAdmins may manage eligible accounts only within their assigned organization, while global SystemAdmin provisioning remains bootstrap-owned. Account reads return organization-relevant role scopes rather than an unrestricted cross-organization view.

### Inventory observation flow

Edge submits a batch of 1–100 observations. Cloud validates endpoint identity, source executor, kiosk/device/dispenser ownership, active state, level value, future clock skew, payload size, source-event uniqueness, and observation ordering. It stores applied, duplicate, or out-of-order evidence appropriately; a configured level-to-quantity profile may derive the current estimated quantity.

### Catalog/runtime menu flow

Runtime-menu projection creation is now a dedicated builder with optional caching, revision handling, fallback to the database source, and cache metrics. Cache failure preserves availability by falling back. `[Needs Review]` Exact cache invalidation triggers, deployment configuration keys, and client-visible revision semantics should be extracted into the evidence inventory.

### Robot authoring and binding flow

The normal authoring flow separates:

1. importing/publishing artifact and RobotProgram resources;
2. optional technical declarations, which are operator-declared metadata rather than behavior certification;
3. operator confirmation of a Production Program Binding between Recipe and Published RobotProgram; and
4. configuration release route authoring/deployment.

Raw Lua files or a bounded archive can now be imported directly into an existing Draft program. Program artifact replacement is concurrency-aware. Advanced import-local composition/release linkage remains available but is not the normal management UI binding path.

### Deployment flow

Release authoring carries revision/concurrency information. Deployment and rollback record the actor, authorization scope, reason, and operation outcome. Rollback rejects a stale client view of the active deployment. Capability validation now evaluates a set of required capability codes.

### Payment webhook flow

Signature verification occurs before local payment/order access. A verified callback with no matching `(Provider, ProviderOrderCode)` is acknowledged successfully, creates no `PaymentCallback`, financial state, or fulfillment state, and increments a bounded unmatched-callback metric with safe logging.

### Operations flow

Maintenance-ticket assignee selection is now a scoped lookup limited to eligible active Technician/Manager accounts, with eligibility rechecked during assignment. Normal notification delivery reads are separated from sensitive provider diagnostics and mutation/requeue permission.

## 6. New / Changed Robot, Edge, IoT, Payment, and Sync Behavior

| Area | Confirmed change | Boundary / qualification |
|---|---|---|
| Execute-order command | Payload default advances from schema v4 to v5. Robot-program bindings carry `RequiredCapabilityCodes` rather than one `RequiredWorkcellCapabilityCode`; decoders continue accepting schemas 3, 4, and 5. | Edge consumers must be checked for schema-v5 compatibility. Physical execution remains external. |
| Dispatch selection | A route is rejected if any required capability in a binding is unavailable. | Capability evidence comes from declared contract metadata and is not proof of Lua behavior. |
| Inventory MQTT uplink | Adds `inventory-observations`, authenticated to endpoint/executor identity with idempotency, sequence, time, scope, calibration, and evidence rules. | Does not create stock movement, prove consumption, or gate v1 menu/checkout sellability. |
| Edge uplink dispatcher | Recognizes and dispatches the new inventory message family. | Retry/dead-letter behavior for this family should be traced explicitly in STM/SRS `[Needs Review]`. |
| Robot authoring | Optional declarations, raw Lua import, import listing, recipe resolution, concurrency-aware program ordering, and separate production-binding confirmation. | Declarations are not certification; Cloud still cannot assert physical behavior. |
| Deployment | Adds audit writer, client-observed active-deployment protection for rollback, reasons, revision tokens, and capability-set validation. | Exact compensation behavior under partial external failure requires focused review. |
| Payment | Verified unmatched callbacks receive safe acknowledgement/metric without state creation. | No automatic provider refund behavior was added by this change set. |
| Sync | Inventory observations add sync provenance fields and a new Edge evidence family. | `[Unclear]` Whether all dead-letter/replay operator surfaces support this family must be documented after handler/job inspection. |

## 7. New / Changed Tests or Quality Evidence

### Added tests

- Development Vanilla soft-serve catalog seed integration test with valid ingredient FKs.
- PayOS webhook signature integration tests.
- Robot-authoring import API integration tests.
- Runtime-menu projection cache integration tests.
- Current-account access-permission and session-security unit tests.
- OrgAdmin account-management and assignable-role-options unit tests.
- Inventory sensor observation ingestion unit tests.
- Maintenance-ticket assignee-options unit tests.
- Configuration release authoring-scope and route-authoring tests.
- Deployment operation-audit tests.
- Production Program Binding capability-evidence tests.
- Robot-authoring import listing and recipe-resolver tests.

### Changed tests

Existing Edge payload/uplink, catalog tenant boundary, payment notification, deployment idempotency/lifecycle, robot technical contract/artifact/import/composition/program manifest, and runtime-menu revision tests were updated to the new contracts.

### Automation / pipeline evidence

- `.github/workflows/backend-ci.yml` now runs restore, Release build, tests, and a non-pushed Docker build on pull requests to `main` using .NET `10.0.x`.
- `.github/workflows/backend-image.yml` builds and pushes SHA plus `main` tags to GHCR on `main`, then uses a protected production environment, NetBird connectivity, SSH, a server-side deployment script, and a public API reachability check.
- `.dockerignore` excludes build artifacts from image context.
- The old `.github/workflows/deploy.yml` was removed.

These files are quality/process evidence, not proof that any particular run passed. CI run links, coverage, executed test counts, deployment evidence, rollback evidence, and acceptance remain `[Needs Review]`.

## 8. Impacted Evidence Files

All repository evidence snapshots should be regenerated from the merged revision before editing downstream reports.

| Evidence file | Required update |
|---|---|
| `00_repo_evidence/repo_truth_map.md` | Add session management, inventory observation, Production Program Binding, revised robot authoring/binding boundary, cache/CI/CD, and changed Edge/payment behavior. |
| `00_repo_evidence/functional_inventory.md` | Add/change routes, permissions, message families, handlers, jobs, cache, bootstrap, metrics, and functional ambiguities. |
| `00_repo_evidence/database_inventory.md` | Add two entities/tables, three migrations, fields/FKs/indexes/JSON roles, and revised route-binding capability columns. Recalculate counts. |
| `00_repo_evidence/api-pack.md` | Regenerate because controllers, requests/results, authorization policies, and routes changed. |
| `00_repo_evidence/data-pack.md` | Regenerate because entities, mappings, migrations, and model snapshot changed. |
| `00_repo_evidence/iot-robot-pack.md` | Regenerate for payload schema v5, inventory uplink, production bindings, raw Lua authoring, and deployment behavior. |
| `00_repo_evidence/backend-docs-pack.md` | Regenerate from current `docs/` and source; do not assume its embedded copies match merged files. |
| `00_repo_evidence/docs-pack.md` | Regenerate because API, authorization, flows, IoT, operations, observability, and documentation-routing files changed. |

`backend_update_impact_2026-08-11.md` should remain as the change bridge and audit record after those inventories are refreshed.

## 9. Impacted Baseline Deliverables

| Baseline deliverable | Impact |
|---|---|
| `01_project_introduction/project_introduction.md` | Update main-feature and architecture/integration summaries for inventory sensor evidence, session management, production bindings, and deployment automation. Avoid over-expanding technical details. |
| `02_srs/srs.md` | Revise identity/account requirements; add or refine sensor-observation, production-binding, cache/observability, raw-Lua, deployment-audit/concurrency, and unmatched-webhook behavior. Determine whether new IDs are required or existing FRs should be expanded `[Needs Review]`. |
| `02_srs/requirements_traceability_matrix.md` | Map all changed/new behavior to controllers, handlers, entities, migrations, docs, and tests. Update API routes and test evidence. |
| `03_uml/use_case_diagram.md` | Add session management, inventory observation, production binding, raw Lua import, and maintenance assignee lookup where diagram scope permits. |
| `03_uml/class_diagram.md` | Add `InventorySensorObservation` and `ProductionProgramBinding`; revise configuration route/binding capability structure and session result behavior. |
| `03_uml/sequence_order_flow.md` | Update payload schema/capability-set and payment unmatched-callback exception behavior where represented. |
| `03_uml/sequence_robot_execution.md` | Add/adjust production binding, capability-set resolution, deployment audit/concurrency, and inventory observation uplink. |
| `03_uml/activity_order_flow.md` | Review payment webhook unmatched branch and dispatch compatibility effects. |
| `03_uml/erd.md` | Add the two new persisted entities and revised relationships/fields. |
| `04_database_design/conceptual_database_design.md` | Add the new business concepts and relationships. |
| `04_database_design/logical_database_design.md` | Add entities, attributes, keys, lifecycle/status, JSON evidence, and relationships. |
| `04_database_design/physical_database_design.md` | Add migrations, tables, columns, FKs, indexes, defaults, JSONB, and data-migration semantics. |

The Project Introduction should be updated only after the evidence/SRS changes establish the correct level of product significance.

## 10. Impacted School Reports

| School report | Required impact update |
|---|---|
| Report 1 — Project Introduction | Concisely update features, architecture/integration scope, and limitations. Local bootstrap and CI/CD are implementation/operations details, not product features. |
| Report 3 — SRS | Highest requirements impact: actors/permissions, use cases, non-screen functions, functional requirements, interfaces, NFRs, business rules, messages, and open questions. Preserve or revise IDs through the STM. |
| Report 4 — SDD | Highest design impact: architecture components/cache, packages, class diagrams, sequences, production binding, inventory observation, Edge schema v5, database design, audit/concurrency behavior. |
| Report 5 — Test Documentation | Add/update cases for sessions, OrgAdmin account management, inventory observations, runtime-menu cache/fallback, raw Lua import, production bindings, capability evidence, deployment audit/concurrency, unmatched webhook, CI/image checks. Do not claim executions. |
| Report 6 — User Guides | Update account/session workflows, organization account routes, role options, maintenance assignee flow, raw Lua/program binding workflow, configuration concurrency/deployment reasons, inventory observations, cache/deployment configuration, local-development bootstrap, and current CI/CD release package. Hardware gaps remain team-owned. |
| Report 7 — Final Project Report | Recompile only after Reports 1/3/4/5/6 are updated and reviewed; refresh requirement-range descriptions, design/database summary, test plan, release package, user workflows, glossary, and open items. |

There is no Report 2 source in the listed school reports. CI/CD and configuration-management evidence may inform the future team-owned Report 2, but schedules, assignments, process outcomes, and approvals must not be inferred.

## 11. Recommended Update Order

1. Freeze the synchronization comparison at merged commit `3f855e9` (or replace it with the final reviewed merge commit if the branch changes) and record the source revision in every regenerated evidence pack.
2. Regenerate `api-pack.md`, `data-pack.md`, `iot-robot-pack.md`, `backend-docs-pack.md`, and `docs-pack.md` from the merged tree.
3. Update `repo_truth_map.md`, `functional_inventory.md`, and `database_inventory.md`; independently verify route, entity, migration, index, and test lists.
4. Resolve requirement-ID strategy: expand existing FRs where semantics remain the same and allocate new FRs only for genuinely new user/business capabilities. Then update `srs.md` and the RTM together.
5. Update UML and conceptual/logical/physical database design from the approved SRS/RTM and refreshed database inventory.
6. Update the team open-question register and review checklists with the new uncertainties below.
7. Update school Report 3, then Report 4, then Report 5 and Report 6. Update Report 1 only for stable high-level scope changes.
8. Recompile Report 7 last so it summarizes approved current versions rather than preserving stale intermediate descriptions.
9. Run documentation consistency checks: FR/STM coverage, route diff, entity/migration/index reconciliation, Mermaid rendering, uncertainty-label scan, and cross-report terminology review.
10. After documentation review, attach actual CI/test/deployment evidence only if separately supplied and verified; do not convert pipeline definitions into claimed results.

## 12. Open Questions

1. `[Needs Review]` Should current-session list/revoke be new requirement IDs or extensions of FR-003/FR-004? What user-facing session/IP/user-agent privacy and retention rules apply?
2. `[Needs Review]` Is the organization-owned account route a deliberate breaking client contract with no compatibility period? Have all frontend clients moved from `/management/accounts` to `/management/organizations/{organizationId}/accounts`?
3. `[Needs Review]` What is the final approved permission/role matrix for OrgAdmin account, product, menu, template-read, maintenance-assignee, notification-view, release, and deployment operations?
4. `[Needs Review]` Should inventory sensor observations expand FR-052/FR-053/FR-056 or receive a separate FR? What are their retention, diagnostics access, replay/dead-letter, and reconciliation rules?
5. `[Unclear]` Does every supported Edge implementation accept execute-order payload schema v5 and capability arrays? What is the rollout/compatibility plan for Edge versions still emitting/consuming schema 3 or 4?
6. `[Needs Review]` Are Production Program Bindings the sole normal Recipe-to-Program authoring boundary, and how should existing configuration releases without binding IDs be presented and maintained?
7. `[Needs Review]` What assurance and capability-evidence status values are user-visible, who approves them, and what evidence is required before deployment?
8. `[Needs Review]` Are optional robot technical declarations correctly named in school reports to avoid treating them as verified behavioral contracts or safety certification?
9. `[Needs Review]` What raw-Lua file/archive limits, accepted MIME/extension rules, error messages, object-storage cleanup behavior, and UI workflow are final client contracts?
10. `[Needs Review]` What is the approved runtime-menu cache technology/profile, invalidation strategy, TTL, configuration key set, revision contract, and production failure alert threshold?
11. `[Needs Review]` Should verified unmatched PayOS callbacks be retained only in logs/metrics, and what alert/runbook threshold applies? Confirm that absence of a `PaymentCallback` row is an intentional privacy/data-integrity boundary.
12. `[Needs Review]` What deployment request/rollback reason format, audit retention, stale-active-deployment recovery, and operator UI behavior are required?
13. `[Needs Review]` Are local development bootstrap identities and fixture-reset procedures excluded from all non-development environments through verified configuration controls?
14. `[Needs Review]` The production workflow hard-codes a public verification URL and relies on a server-side deployment script outside this repository. What approved runbook, rollback, migration, health, secret, and ownership evidence is available?
15. `[Needs Review]` Do the new GitHub Actions runs currently pass, and where are immutable run links, test reports, image digests/signatures/SBOMs, deployment records, and acceptance evidence stored?
16. `[Unclear]` The branch comparison shows upstream deployment automation but no Report 2. Which configuration-management and release-process facts may be promoted into the team-owned Project Management Plan?

## Analysis Status

This analysis is based on repository evidence at the revisions listed above. It identifies documentation impact but does not certify runtime correctness, migration safety, Edge compatibility, provider interoperability, CI success, deployment success, or test completion.
