# Codex Review — Backend Update Baseline Documents

## Review scope and evidence basis

This review covers the updated baseline set:

- `deliverables/02_srs/srs.md`
- `deliverables/02_srs/requirements_traceability_matrix.md` (referred to below as the RTM/STM candidate)
- `deliverables/03_uml/*.md`
- `deliverables/04_database_design/*.md`

The post-sync comparison basis is `deliverables/00_repo_evidence/backend_update_impact_2026-08-11.md`, `deliverables/05_team_review/codex_review_backend_update_evidence.md`, the current baseline diffs, and the merged backend revision identified by the impact analysis. The three older core inventories—`repo_truth_map.md`, `functional_inventory.md`, and `database_inventory.md`—remain pre-sync and are not treated as complete current evidence.

No build, test execution, migration, live-schema inspection, OpenAPI generation, provider call, Edge execution, CI run, or deployment was performed for this review. “Supported” comments therefore concern static source/document evidence only unless a cited artifact proves otherwise.

Severity labels:

- **Critical** — prevents the documents from functioning as a coherent post-sync baseline.
- **High** — material requirement, traceability, UML, or database inconsistency.
- **Medium** — incomplete qualification, stale evidence, or important coverage gap.
- **Low** — presentation or maintenance issue.

## Overall conclusion

The baseline update incorporates many major backend changes: session list/revoke, organization-owned account routes, inventory observations, Production Program Bindings, raw Lua import/concurrency, execute-order schema v5, deployment audit/concurrency, unmatched PayOS callbacks, maintenance assignee options, and new migrations/counts.

It is **not yet a reliable post-sync baseline**. The documents mix refreshed claims with pre-sync evidence rows, omit several operational/API changes, disagree about cache behavior and requirement status, and contain UML/database cardinality mismatches. The RTM has full FR/NFR identifier presence but is not yet an STM in the test-traceability sense: it has no stable requirement-to-test-case/result mapping.

## 1. SRS consistency

### BUB-01 — Critical — SRS entity catalogue omits both post-sync entities

**Location:** `srs.md` §6.2, line 1782.

The persistence-platform section correctly reports 100 `DbSet<T>` declarations and eight migrations, and FR-134/FR-135 describe the new concepts. However, the bounded-context entity catalogue still lists pre-sync Inventory and Production Configuration groups:

- Inventory omits `InventorySensorObservation`.
- Production Configuration omits `ProductionProgramBinding`.

This conflicts with the SRS’s own FRs, RTM DR-04/DR-10, UML, and database designs.

**Required correction:** Add both entities to §6.2 and cite the post-sync impact/migrations rather than the stale database inventory alone.

### BUB-02 — High — Runtime-menu cache requirement and RTM disagree

**Locations:** `srs.md` NFR-013; RTM NFR-013; `sequence_order_flow.md` lines 23 and 74.

The updated SRS correctly says the cache is optional, database fallback preserves the read path, admission is checked before cache access, request-specific snapshot identity is not cached, and exact TTL/invalidation/profile/alert thresholds require review. The RTM and order sequence still assert a supported “15-second cache” based only on pre-sync SC-08.

**Required correction:** Update the RTM and sequence to the new optional-cache/fallback semantics. Mark exact TTL, invalidation, deployment profile, and thresholds `[Needs Review]`; do not preserve 15 seconds as an unconditional current fact without current configuration evidence.

### BUB-03 — High — Notification-delivery authorization/privacy update is missing

**Locations:** `srs.md` FR-086; RTM FR-086; use-case diagram operations area.

The backend update separates `notifications.view` from `notifications.manage`, and normal reads exclude message content/provider diagnostics. FR-086 still describes only a diagnostics view and requeue behavior for Technician/Manager, with old functional-inventory evidence. The RTM is unchanged and the use case merely says “notification outbox.”

**Required correction:** Distinguish normal safe reads, sensitive diagnostics, and requeue mutation; state their exact policies/actors and response-data boundary. Do not infer a UI screen.

### BUB-04 — Medium — Updated requirements retain stale evidence fields

The SRS prose incorporates new behavior but several `Evidence`/`Related` entries still point only to superseded pre-sync rows:

- FR-070 unmatched PayOS callback branch cites only PAY-03.
- FR-082 assignee options cites only OPS-08–OPS-10.
- FR-097 raw Lua import and optimistic concurrency cites only RC-10.
- FR-102 and FR-105–FR-107 include new revision/reason/audit/stale-observation behavior but cite old PC rows.
- FR-120 schema-v5 behavior cites only IOT-05.
- FR-126 seven message types cites old MQTT-02.

**Required correction:** Add current symbols/tests/migrations or refreshed inventory IDs. Label old inventory rows “pre-sync/superseded” where retained for historical continuity.

### BUB-05 — High — FR-086 and operations actors may conflict with the new policy split

FR-086 states Technician/Manager for diagnostics/requeue, but the update evidence says notification viewing and management are separate capabilities. The exact actor matrix is not carried into the SRS or RTM.

**Required correction:** Read the current policy declarations and controller actions, then state actors separately for safe list/detail, provider diagnostics, and requeue. Keep exhaustive coverage `[Needs Review]` until audited.

### BUB-06 — Medium — Production-package impact remains unresolved but is not visible in the baseline

The impact analysis says package-upgrade behavior changed to align with Production Configuration and requires focused comparison. FR-111–FR-119 and RTM rows remain pre-sync, with no post-sync qualification except adjacent Production Configuration changes.

**Required correction:** Compare the current package upgrade handlers/contracts. Mark affected rows `[Needs Review]` until the public-contract impact is known rather than implying the whole package section is unchanged.

### BUB-07 — Medium — Local bootstrap and CI/CD changes are absent from baseline routing

Development role/account/endpoint/catalog seeds, robot-authoring reset support, .NET 10 PR checks, GHCR image publishing, NetBird/SSH deployment, and `.dockerignore` are not product FRs, so omission from functional requirements is reasonable. They are nevertheless backend-update impacts that require an explicit destination.

**Required correction:** Record them as development/operational constraints or references for Report 2 configuration management, Report 5 tools/environment evidence, and Report 6 installation/release documentation. Do not claim a run passed or deployment succeeded.

### BUB-08 — Medium — Payment unmatched-callback observability needs explicit evidence and operational limits

The alternative flow is correctly described, but the SRS says “bounded unmatched-callback observability” without naming the metric or citing the new handler/test. Alert thresholds, retention, and operator response remain unresolved.

**Required correction:** Cite `icebot.payment.webhook.verified_unmatched` and the current source/test evidence. Keep thresholds/runbook `[Needs Review]`.

### BUB-09 — Medium — New requirement numbering is internally complete but needs governance

FR-134 and FR-135 are consistently added to the SRS and RTM, preserving prior IDs. The decision to create new FRs rather than extend existing requirements is reasonable, but no change-control note identifies who approved the requirement-set expansion.

**Required correction:** Record the baseline revision and change decision. Update downstream school reports and any stated “133 FR” totals.

## 2. STM / traceability coverage

### BUB-10 — Critical — The RTM is not a complete Software Test Matrix

Every SRS FR (FR-001–FR-135) and NFR (NFR-001–NFR-025) appears in the matrix. However, it has no `Verification Method`, stable `Test ID`, test level/type, execution baseline, or verification result columns. Generic phrases such as “session tests,” “observation tests,” and “payload tests” are not traceable test identifiers.

The backend update added/changed tests for session security, OrgAdmin accounts, inventory observations, runtime-menu cache, PayOS signatures, robot imports, production bindings/capabilities, deployment audit, maintenance assignees, and Edge payload/uplink behavior. Those tests are not systematically mapped.

**Required correction:** Either rename this artifact strictly as an RTM and create an STM, or extend it with requirement → test case/file/method → level/type → planned/executed status → build/result evidence. Test source existence must not be labelled Passed.

### BUB-11 — High — NFR-013 status violates the matrix’s weakest-component rule

The RTM declares NFR-013 `Supported` with a fixed 15-second cache, while the SRS marks exact TTL/invalidation/profile/thresholds `[Needs Review]`. This directly contradicts the matrix’s stated weakest-material-component rule.

**Required correction:** Align the requirement text/evidence/status with the SRS; likely `Needs Review` if exact operational cache semantics remain part of the requirement.

### BUB-12 — High — FR-120 SRS and RTM status differ without clear scope separation

The SRS labels Cloud schema-v5 pull behavior Supported; the RTM labels FR-120 Needs Review because external Edge rollout compatibility is unknown. Both can be defensible only if the requirement is split into:

- Cloud payload production/decoding behavior; and
- external Edge compatibility/rollout acceptance.

**Required correction:** Split the subclaims or explicitly define the RTM row as the broader end-to-end contract. Do not let a Cloud-only status imply deployed Edge compatibility.

### BUB-13 — Medium — FR-134 status mixes core implementation with tangential operations questions

The SRS says the core ingestion requirement is Supported and separately flags replay/dead-letter/retention/diagnostics policy. The RTM sets the whole row to Needs Review. If those policies are not part of FR-134’s normative text, this over-demotes the requirement; if they are required, the SRS description is incomplete.

**Required correction:** Define the requirement boundary and create separate NFR/operational rows where appropriate.

### BUB-14 — High — Production Configuration RTM rows are only partially updated

FR-102 includes revision/binding evidence, but FR-103–FR-109 remain tied to old PC rows and omit richer authoring options, operator reasons, audit scope/outcome, binding/capability arrays, and stale rollback observation. The matrix therefore cannot prove coverage of all new contract subclaims.

**Required correction:** Update each affected row and map current tests. Avoid hiding several independently testable controls inside one Supported status.

### BUB-15 — Medium — New tests are evidence of planned/verifiable behavior, not execution results

The matrix correctly states that Supported means static evidence, not runtime verification. Preserve this. When tests are added to the STM, use statuses such as Test Located/Planned/Not Executed unless immutable CI/test output for the exact baseline is attached.

## 3. UML consistency

### BUB-16 — High — ERD overstates the optional Production Program Binding relationship

**Location:** `erd.md` line 70.

The ERD shows:

`PRODUCTION_PROGRAM_BINDING ||--o{ EXECUTION_ROUTE_ROBOT_BINDING`

This makes a Production Program Binding mandatory for each route robot binding. The impact analysis, logical design, and physical design say `ExecutionRouteRobotBinding.ProductionProgramBindingId` is optional.

**Required correction:** Show each route robot binding referencing zero-or-one Production Program Binding, while one Production Program Binding may be referenced by zero-to-many route bindings.

### BUB-17 — High — Old ProductionIncident cardinality remains inconsistent

The updated logical database design correctly models `OrderItem → ProductionIncident` as zero-to-many because no unique constraint limits incidents to one. `class_diagram.md` still shows `0..1`, and `erd.md` still uses `||--o|`.

**Required correction:** Align both UML diagrams with the physical zero-to-many cardinality, while retaining the open question about the intended maximum concurrently-open incidents.

### BUB-18 — Medium — Menu/Kiosk ERD optionality remains overstated

`erd.md` shows `KIOSK ||--o{ MENU`, implying every Menu has exactly one Kiosk parent. Existing database evidence and the conceptual design support nullable `Menu.KioskId` and broader organization/store scope.

**Required correction:** Show optional Kiosk scope and explain alternate scope ownership.

### BUB-19 — High — Use-case actor links do not reflect account authorization changes

The use-case diagram adds session and production-binding cases, but:

- OrgAdmin is not connected to internal-account management or role assignment despite the new constrained organization-owned behavior.
- SystemAdmin is not connected to Production Program Binding despite FR-135 naming SystemAdmin/OrgAdmin.
- Permission-matrix restriction versus assignable-role options is not visible.

**Required correction:** Update actor associations and notes to show constrained organization scope and prohibited SystemAdmin granting/cross-organization mutation.

### BUB-20 — Medium — Behavioral UML coverage is incomplete for major new flows

The robot sequence mentions schema v5 and inventory observations only in prose. No sequence/activity diagram shows:

- inventory observation validation, duplicate/conflict/out-of-order disposition, optional quantity derivation, and after-commit notification;
- Production Program Binding creation and use during release authoring/dispatch;
- revision-token conflict and stale rollback observation;
- session list/revoke; or
- safe versus diagnostic notification reads.

**Required correction:** Add focused sequences for at least inventory observations and production binding/release deployment. Minor lookup/session flows may remain use-case-level if intentionally scoped out.

### BUB-21 — Medium — Order sequence retains obsolete fixed-cache wording

The sequence still returns “ETag, 15s cache.” It omits cache hit/miss/failure fallback and admission-before-cache behavior.

**Required correction:** Use optional bounded cache wording and an `alt` fragment if cache behavior is design-relevant. Keep exact TTL/invalidation `[Needs Review]`.

### BUB-22 — Medium — New class attributes are incomplete or imprecise

The post-sync class block omits material fields/relationships:

- `InventorySensorObservation.DerivedEstimatedQuantity` is optional but drawn as non-nullable `decimal`.
- Its Device/Ingredient identities, timestamps, payload/provenance, and endpoint relationship semantics are trimmed without an explicit per-field omission note.
- `ProductionProgramBinding` omits binding/program checksums, supported options, retirement/soft-delete/audit fields.

Selective UML is acceptable, but optionality and constraint-relevant fields should not be misstated.

### BUB-23 — Low — UML evidence notes still lean on stale inventories

The diagrams state that post-sync additions come from the impact analysis, but most other attributes/cardinalities continue citing the pre-sync inventories. Until those inventories are regenerated, label the diagrams as hybrid pre/post-sync baselines and avoid claiming a complete current model.

## 4. Database design consistency

### BUB-24 — High — Logical ExecutionRouteRobotBinding row remains pre-sync

**Location:** `logical_database_design.md` line 177.

The entity row still lists only `ExecutionRouteId` and `RobotProgramId` and says the identifier is unclear. The following relationship paragraph acknowledges the optional Production Program Binding and snapshot, creating an internal contradiction.

**Required correction:** Add the current identifier, `ProductionProgramBindingId?`, binding checksum snapshot, required capability-code JSON snapshot, and any current ordering/revision fields supported by the model.

### BUB-25 — High — Physical design does not fully catalogue the new tables

The physical design lists both new tables and some indexes, but does not provide a complete column/FK/nullability/delete/check/index treatment comparable to its asserted physical-design role. Missing detail includes:

- exact restrictive FKs for Inventory Sensor Observation;
- its disposition, sequence, timestamps, provenance, optional derived quantity, and bounded payload behavior;
- Production Program Binding’s organization/product/recipe/program FKs, lifecycle, assurance/evidence fields, supported-option/capability JSON, audit/soft-delete behavior; and
- exact optional FK behavior and scalar-to-array migration effect on route robot bindings.

**Required correction:** Add complete post-sync table specifications sourced from current configurations/migrations/model snapshot.

### BUB-26 — Medium — Multi-tenancy catalogue is not visibly updated

The impact analysis explicitly says Production Program Binding must join the required-`OrganizationId` entity list. Conceptual/logical descriptions mention organization ownership, but the cross-cutting multi-tenancy section was not shown as updated.

**Required correction:** Add it to the authoritative tenant-ownership/constraint catalogue and state which relationships have structural composite-FK protection versus application validation only.

### BUB-27 — Medium — Counts are responsibly qualified but source revision is missing

The physical design correctly separates 100 DbSets, eight migrations, and 101 cumulative `CreateTable` operations from a live table count. However, it does not record the exact merged commit beside those counts.

**Required correction:** Add the reviewed source revision and `current as of` date to SRS, RTM, UML, and database documents. Reconcile against the current model snapshot/live schema before promoting counts beyond static evidence.

### BUB-28 — Medium — Existing database open questions were carried forward without post-sync disposition

Delete behavior, soft-delete query coverage, high-volume indexes, JSON schema versioning, credential length, and history asymmetry remain useful, but should be marked confirmed, still open, superseded, or resolved against the new snapshot. New observation retention/index priorities also need inclusion.

### BUB-29 — Positive finding — Physical outcome and declaration boundaries are preserved

The conceptual/logical/UML text correctly states that inventory observations do not create stock movements or prove consumption, and that capability declarations/bindings do not certify Lua behavior or physical safety. Retain these qualifications.

## 5. Backend update impact coverage matrix

| Backend update area | SRS | RTM/STM | UML | Database designs | Review result |
|---|---|---|---|---|---|
| Session list/revoke and transactional password revocation | Reflected | Reflected, no stable test IDs | Use case only | Reuses RefreshToken; no new entity expected | Partial but acceptable; test mapping missing |
| Organization-owned account routes/OrgAdmin rules/permission codes | Reflected | Reflected | Actor links incomplete | No new entity expected | Partial |
| Inventory Sensor Observations | Reflected as FR-134 | Reflected as FR-134/DR-04 | Use case/class/ERD; no sequence | Added concept/entity/table/indexes | Substantial, but SRS entity list and physical detail incomplete |
| Runtime-menu optional cache/fallback/metrics | SRS updated | RTM stale 15-second claim | Order sequence stale | No relational entity expected | Inconsistent |
| Robot import/list/concurrency/declaration changes | Mostly reflected | Partially refreshed | Broad authoring use case only | No new table for most changes | Partial |
| Production Program Binding | Reflected as FR-135 | Reflected as FR-135/DR-10 | Use case/class/ERD; ERD optionality wrong | Added across all levels | Substantial but inconsistent |
| Release/deployment revision, reason, audit, stale rollback | Reflected | Only FR-102 materially updated | No focused sequence | Some changed fields noted | Partial |
| Execute-order schema v5/capability arrays | Reflected with compatibility question | Reflected | Robot-sequence prose only | Route-binding snapshot noted | Partial |
| Verified unmatched PayOS callback | Reflected | Reflected | Activity note only | No new entity expected | Mostly reflected; evidence citation stale |
| Maintenance assignee options | Reflected | Reflected | Generic maintenance use case | No new entity expected | Mostly reflected |
| Notification safe-read/manage separation | Missing/stale | Missing/stale | Missing | No schema change expected | Not reflected |
| Production package alignment | Not revalidated | Not revalidated | Not revalidated | Not clearly revalidated | Unclear |
| Local bootstrap/seeds/reset | Missing from routing | Not applicable as product FR | Not applicable | No required design entity impact identified | Operational documentation gap |
| CI/CD/.NET 10/GHCR/NetBird/.dockerignore | Missing from routing | No test-run evidence | Not applicable | Physical deployment notes omit it | Operational/quality documentation gap |

## 6. Old or outdated claims that remain

1. Fixed 15-second runtime-menu cache in RTM and order sequence.
2. SRS §6.2 pre-sync entity lists excluding Inventory Sensor Observation and Production Program Binding.
3. FR-086’s old notification diagnostics/view authorization model.
4. Multiple SRS/RTM evidence cells citing only superseded functional-inventory rows for changed contracts.
5. Class/ERD zero-or-one Production Incident cardinality despite logical/physical zero-to-many evidence.
6. ERD mandatory Kiosk ownership of Menu despite nullable/broader scope.
7. ERD mandatory Production Program Binding for every route robot binding despite optional FK.
8. Logical `ExecutionRouteRobotBinding` attributes from the pre-sync model.
9. Use-case actor links reflecting old account-management ownership.
10. Claims that the baseline is fully current while core evidence inventories remain pre-sync; all such documents should be labelled with source revision and hybrid/outdated evidence status.

## 7. Required remediation order

1. Regenerate `repo_truth_map.md`, `functional_inventory.md`, and `database_inventory.md` from the chosen merged source revision.
2. Correct SRS entity lists, notification behavior, cache qualification, evidence citations, and package-impact status.
3. Convert the RTM into or pair it with a real STM using stable test identifiers and non-executed statuses.
4. Correct ERD/class cardinalities and actor links; add focused inventory-observation and binding/deployment sequences.
5. Complete logical/physical specifications for new and changed persistence fields/relationships.
6. Record baseline commit/date in every document and rerun all older database/open-question findings against the new snapshot.
7. Route bootstrap and CI/CD facts to Reports 2, 5, and 6 without claiming run success.
8. Perform an independent cross-document check before updating school Reports 3–7.

## Final disposition

**Needs another correction pass before approval as the post-sync baseline.** Major product and persistence additions are present, and important physical-outcome qualifications remain sound. Approval is blocked by stale core evidence, incomplete STM/test traceability, SRS entity and notification omissions, cache/status inconsistencies, UML optionality/cardinality errors, and incomplete logical/physical representation of the changed route-binding model.
