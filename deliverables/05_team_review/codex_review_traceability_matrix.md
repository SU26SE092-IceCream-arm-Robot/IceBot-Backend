# Codex Review — Requirements Traceability Matrix

## Scope and Method

Reviewed without modifying:

- `deliverables/02_srs/srs.md`
- `deliverables/02_srs/requirements_traceability_matrix.md`
- `deliverables/00_repo_evidence/functional_inventory.md`
- `deliverables/00_repo_evidence/database_inventory.md`
- `deliverables/00_repo_evidence/repo_truth_map.md`

This document contains review comments only.

Checks performed:

- exact SRS FR/NFR ID comparison against matrix rows;
- functional-inventory ID comparison against matrix evidence references;
- review of matrix Status semantics and compound-status rows;
- review of database-inventory coverage by DR/NFR/BR rows;
- review of overlapping responsibilities and requirement granularity.

## Executive Findings

- The SRS contains **133 FRs and 25 NFRs (158 total)**, and all 158 have a corresponding matrix row. There are no orphan FR/NFR IDs in either direction.
- The functional inventory contains **260 identifiable capability rows**. No feature is clearly absent from the SRS/matrix at the conceptual level, but **21 inventory IDs are not written explicitly in the matrix** and are recoverable only by expanding ranges such as `TEN-05–TEN-10`. This weakens auditability.
- The matrix has only two `Needs Review` rows even though numerous `Supported` rows contain material `[Inferred]`, `[Unclear]`, incomplete-inspection, or known-gap qualifications.
- Data coverage is broad at entity-group level but shallow at attribute, relationship, constraint, lifecycle, JSON-schema, tenancy, and physical-schema level.
- Several FRs combine independently testable APIs, jobs, transitions, or transports. Several NFR/DR rows combine a supported fact with an assumption or unresolved claim, making one Status cell misleading.

## 1. SRS FRs or NFRs Missing from the Matrix

### Result: none

Mechanical comparison found:

- SRS FR headings: `FR-001` through `FR-133` — 133 unique rows.
- SRS NFR headings: `NFR-001` through `NFR-025` — 25 unique rows.
- Matrix FR/NFR rows: the same 158 unique IDs.
- No matrix FR/NFR exists without a corresponding SRS requirement.

### Review qualification

ID presence is not the same as complete traceability. A row can exist while:

- omitting one of the inventory capabilities consolidated into the SRS requirement;
- citing a range instead of each exact source ID;
- carrying a stronger Status than its evidence permits;
- combining multiple independently testable behaviors under one result.

The matrix is complete as an **SRS-ID index**, but not yet complete as a **one-to-one evidence and verification matrix**.

## 2. Functional-Inventory Features Missing from the SRS or Matrix

### No clearly missing feature at conceptual level

The 260 functional rows appear to be assigned to an FR, directly or through a range. The newer matrix fixes most traceability gaps identified in the earlier SRS review.

### Twenty-one IDs are not explicit tokens in the matrix

The following inventory IDs do not appear literally and depend on range expansion:

- Devices: `DEV-05`, `DEV-06`, `DEV-07`
- Operations: `OPS-12`, `OPS-13`, `OPS-14`
- Orders: `ORD-13`, `ORD-14`, `ORD-15`, `ORD-16`, `ORD-17`, `ORD-21`
- Tenants: `TEN-06`, `TEN-07`, `TEN-08`, `TEN-09`, `TEN-12`, `TEN-13`, `TEN-14`, `TEN-17`, `TEN-18`

They are apparently covered by:

- `FR-018` (`TEN-05–TEN-10`)
- `FR-019` (`TEN-11–TEN-15`)
- `FR-020` (`TEN-16–TEN-19`)
- `FR-023` (`DEV-04–DEV-08`)
- `FR-064` (`ORD-12–ORD-18`)
- `FR-066` (`ORD-20–ORD-24`)
- `FR-083` or adjacent Operations ranges, depending on the exact OPS mapping

### Review comment

The matrix states that every inventory ID is reachable, but range notation prevents simple automated validation and conceals accidental holes. Evidence columns should enumerate IDs or provide a separate 260-row inventory-to-FR appendix. At minimum, add a machine-readable expansion table.

### Cross-cutting feature references remain asymmetric

- `FR-067` cites `ORD-25`, while its actual SignalR mechanism is also `SIG-04`.
- `FR-078` cites `PAY-15`, `PAY-16`, while order/payment push mechanism evidence also comes from `SIG-04`.
- `FR-133` cites `DASH-01`, `DASH-02`, while dashboard push uses `SIG-06`.
- Device/IoT ingestion FRs cite owning device rows plus IoT rows inconsistently: some IoT IDs are in Evidence, others only in Notes.

These are not missing functions, but the matrix should distinguish:

- owning business capability;
- transport/entry-point capability;
- shared implementation mechanism;
- duplicate exposure of the same handler.

## 3. Requirements with Weak or Missing Evidence

### Weakly evidenced FRs currently marked `Supported`

| Requirement | Evidence issue | Recommended matrix treatment |
| --- | --- | --- |
| `FR-052` | Inventory consumption domain logic was read, but the calling trigger in Orders/EdgeIntegration was not opened as part of that evidence row. | `Inferred` for end-to-end execution-driven consumption; `Supported` only for the domain operation. Split if necessary. |
| `FR-067` | Business event row and SignalR mechanism are split across `ORD-25` and uncited `SIG-04`. | Add `SIG-04`; retain `Supported` only for specifically evidenced events. |
| `FR-078` | Combines SignalR payment events and intervention notification delivery; `SIG-04` is not in the evidence cell. | Add shared transport evidence and split realtime versus durable push if their guarantees differ. |
| `FR-101` | Orphan cleanup is supported, but object-storage startup validation is a separate repository capability absent from the requirement set. | Keep cleanup supported; add a separate deployment/readiness requirement for startup validation. |
| `FR-110` | `PC-12` was confirmed by wiring/registration, not full behavioral inspection. | `Inferred` or `Needs Review` for failure-notification semantics; split from the supported timeout reconciler. |
| `FR-119` | Job wiring was confirmed, but reconciliation service internals were not read line by line. | `Inferred`, not fully `Supported`. |
| `FR-122` | Says HTTPS is fallback and MQTT is primary. Transport priority is a contract claim beyond mere shared-handler evidence. | Cite explicit transport contract or mark priority `[Inferred]`. |
| `FR-126` | Shared subscription and handler dispatch are evidenced; end-to-end duplicate-free processing is not. | Limit to broker consumption/dispatch; keep idempotency separate. |
| `FR-129` | Proves GraphQL server wiring, not all schema fields, authorization, read-only status, paging, or resolver behavior. | Keep narrow wiring requirement only; do not use it as evidence for the full GraphQL API. |
| `FR-130` | Combines initial dispatch and timeout reconciliation, which have different inputs, states, side effects, and failure semantics. | Split; evidence and verification should be independent. |

### Weakly evidenced NFRs

| Requirement | Evidence issue |
| --- | --- |
| `NFR-001` | The listed endpoints do not establish a universal idempotency contract. Key scope, request checksum behavior, retention, concurrent retry behavior, and response replay are not uniformly specified. |
| `NFR-002` | Architecture/docs plus entity/job existence support offline-tolerance mechanisms, not a measurable disconnection duration or recovery objective. |
| `NFR-003` | Notes admit “recovery” is inferred and several paths terminate in manual intervention. The Status should not remain unqualified `Supported`. |
| `NFR-004` | Core claim contains an unresolved cascade-overwrite question. A row with a materially unresolved referential behavior should be split or marked `Needs Review`. |
| `NFR-006` | Supported for three history tables, inferred for Alerts/Maintenance/Operations, and known to have audit-column asymmetry. Compound status is hidden by `Supported`. |
| `NFR-007` | Only cited endpoints were checked; universal management/GraphQL/SignalR coverage remains open. |
| `NFR-008` | The endpoint-profile authentication mapping is partly architecture-contract evidence. It needs direct authenticator selection/configuration evidence for a system-wide security requirement. |
| `NFR-014` | Batch constants and job implementation are supported; retention values as product requirements versus defaults are not distinguished. |
| `NFR-015` | Boundary structure is supported; independent evolution is rationale. Matrix notes this but Status collapses both. |
| `NFR-016` | Broker-level shared delivery is supported; end-to-end processing semantics are inferred. |
| `NFR-017` | Claims indexed high-volume tables while acknowledging a known documented index gap. Scope must explicitly exclude the missing table or Status becomes `Needs Review`. |
| `NFR-020` | Applies only to cited endpoints; system-wide completeness was not audited. |
| `NFR-022` | Storage split is supported; scaling/backup benefit is inferred and no backup procedure is evidenced. |
| `NFR-023` | Structural protection is supported only for enumerated relationships; schema-wide tenant integrity remains unclear. |
| `NFR-024` | “Singleton/distributed background jobs shall use advisory locks” is broader than the example orphan-cleanup job. Enumerate all jobs covered or narrow the requirement. |
| `NFR-025` | Current absence of partitioning is supported; future plan is unclear. These are two different statements and should not share one `Unclear` Status. |

### No verification/test evidence

The matrix correctly says `Supported` means statically code-evidenced, not runtime/test-verified. However, it has no columns for:

- unit-test evidence;
- integration/API test evidence;
- migration/model validation;
- security/authorization coverage;
- concurrency/idempotency failure scenarios;
- manual acceptance evidence.

For a final traceability matrix, add `Verification Method`, `Test/Evidence ID`, and `Verification Status`. Current `Status` reports source confidence, not requirement satisfaction.

## 4. Incorrect or Misleading Status Labels

### Status-selection rule is structurally flawed

The matrix says that for compound statuses it uses the **first-listed word** and relegates all other qualifiers to Notes. This makes the Status column systematically optimistic. A scan/filter for `Supported` hides material uncertainty.

The row status should represent the weakest material component, or compound requirements should be split.

### Rows that should not remain plain `Supported`

| Row | Current | Recommended |
| --- | --- | --- |
| `FR-052` | Supported | Inferred for end-to-end trigger; Supported for domain method only |
| `FR-110` | Supported | Needs Review or split into Supported `PC-11` and Inferred `PC-12` |
| `FR-119` | Supported | Inferred |
| `NFR-003` | Supported | Inferred or split fact from recovery interpretation |
| `NFR-004` | Supported | Needs Review unless split |
| `NFR-006` | Supported | Needs Review unless narrowed to the supported entities |
| `NFR-007` | Supported | Needs Review for universal coverage; Supported only for cited endpoints |
| `NFR-015` | Supported | Split supported structure from inferred rationale |
| `NFR-016` | Supported | Split supported broker behavior from inferred end-to-end outcome |
| `NFR-017` | Supported | Supported for listed indexes; Needs Review if the requirement includes the known missing index |
| `NFR-020` | Supported | Needs Review for system-wide claim; Supported for cited endpoints |
| `NFR-022` | Supported | Split supported storage fact from inferred benefits |
| `NFR-023` | Supported | Supported only for enumerated FKs; Unclear for general coverage |
| `DR-13` | Supported | Needs Review or split, because it incorporates `NFR-004` and `NFR-023` uncertainties |

### Rows that should be split instead of assigning one status

- `NFR-025`: “no current partitioning” = Supported; “whether/when planned” = Open Question/Unclear.
- `DR-14`: `*Json` → `jsonb` mapping = Supported; four-role taxonomy = Inferred.
- `DR-15`: Organization→Store→Kiosk hierarchy = Supported; universal scope-resolution order = Inferred/Unclear; application-handler scoping completeness = Open Question.
- `DR-16`: 98 DbSets = Supported; PostgreSQL 17/`IceBotDB` as binding requirement = Assumption; soft-delete/retention/object-storage facts have their own evidence statuses.

### Correct `Needs Review` labels

- `FR-009` correctly surfaces partial `IDN-15b`, though splitting the supported invitation flow from the partial temporary-password path would be clearer.
- `FR-132` correctly surfaces partial `SYNC-05`, though splitting retry from list/resolve/ignore would allow accurate independent statuses.

## 5. Database and Data Requirements Not Adequately Represented

The matrix adds `DR-01`–`DR-16`, which is a good start, but most rows are entity lists rather than testable data requirements.

### Missing or underrepresented database requirements

1. **Entity attributes and types**: IDs, status fields, money/quantity fields, snapshots, schema versions, timestamps, and required/nullable semantics are not traceable.
2. **Per-relationship evidence**: cardinality, requiredness, FK columns, delete behavior, and cross-context ownership are compressed into `DR-13`.
3. **Exact unique/index predicates**: six different partial unique indexes are merged under BR-12/DR-13, hiding distinct status predicates and nullable behavior.
4. **Check constraints**: execution-endpoint profile/identity consistency and package-installation kiosk/store consistency are not independently represented.
5. **JSON contracts**: versioned versus unversioned JSON fields and supported schema versions are not mapped individually.
6. **Tenant-scope variants**: full override hierarchy, required-organization ownership, and ownership derived through a parent are merged into `DR-15`.
7. **Append-only versus mutable/upsert semantics**: `SyncEventInbox`, `EdgeStateSummary`, `ProductionEventCheckpoint`, history rows, callbacks, and delivery attempts need separate retention/update rules.
8. **Soft-delete exceptions**: the 12 principal types and required `WhereNotDeleted()` responsibility are not connected to specific query/verification evidence.
9. **Migration/manual-step behavior**: five migrations and manual preflight SQL steps are not represented as deployment/data-integrity requirements.
10. **Known model discrepancies** are only in notes/open questions, not traceable review items:
   - missing `ProductOption.TemplateProductOptionId` FK;
   - `RobotProgram` rejecting Global scope;
   - history base-class inconsistency;
   - missing `EdgeCommandDeliveryAttempts` time index;
   - possible cascade override;
   - unversioned/asymmetric JSON fields;
   - connection-string key divergence;
   - public-key length concern.
11. **Table/model counting rule**: `DR-16` correctly states 98 DbSets, but no independently verified current physical-table count or model-snapshot counting rule is represented.
12. **Data retention outcomes**: batch sizes/default days are listed, but deletion eligibility, legal/audit exclusions, failure/retry, and operator visibility are absent.

### Review recommendation

Create testable DRs by invariant, not merely by bounded-context entity group. Entity-list rows can remain as a data dictionary index, but should not be labeled as if entity existence alone were a complete data requirement.

## 6. Duplicate or Overlapping Requirements

| Requirements | Overlap | Recommendation |
| --- | --- | --- |
| `FR-024` and `FR-050` | Both describe hardware/device replacement and inventory rebind. | Clarify orchestration ownership. If one endpoint performs both atomically, use one primary FR with sub-requirements; otherwise distinguish device replacement from standalone container rebind. |
| `FR-027`–`FR-030`, `FR-122`, `FR-124` and `FR-126` | Same application handlers may be reached through REST and MQTT. | Model business ingestion once, with separate interface/transport requirements for REST and MQTT. Avoid implying duplicate business capabilities. |
| `FR-045`, `FR-046`, `FR-047` | Runtime menu output depends on sellability/selectability/route filtering. | Keep evaluator rules as sub-requirements or business rules unless independently callable/tested services need separate FRs. |
| `FR-055` and `FR-109` | Inventory readiness service versus management/deployment query/gate. | Distinguish calculation rule from exposed query and deployment usage; cross-reference explicitly. |
| `FR-067`, `FR-078`, `FR-087`, `FR-133`, `SIG-04`–`SIG-06` | Business events and SignalR/dashboard push overlap across contexts. | Separate event production from realtime transport delivery and durable notification delivery. |
| `FR-110` and `NFR-003` | Deployment reconciliation appears as both functional job and cross-cutting recovery quality. | NFR should state measurable timing/reliability target, while FR owns behavior. |
| `FR-119` and `NFR-003` | Package-upgrade reconciliation duplicated similarly. | Same treatment. |
| `FR-125`, `FR-126`, `NFR-016`, BR-04 | MQTT transport behavior is repeated as FR, NFR, and BR. | Keep FRs for publish/consume behavior, NFR for scalability target, BR for source-of-truth rule; remove repeated mechanism wording. |
| `FR-128` and `FR-067`/`FR-087`/`FR-133` | Hub join versus messages delivered through the same hubs. | Retain separately but define subscription authorization versus event delivery clearly. |
| `FR-130` and BR-03 | Payment/execution decoupling and dispatch reconciliation overlap. | BR-03 should express the invariant; FR-130 should express jobs/transitions. |
| `FR-132` and Operations dead-letter management references | Same controller is owned/cross-referenced in Operations and Sync. | Keep one owning FR and reference it from Operations without duplicating capability. |
| `NFR-001` and BR-13 | Same idempotency behavior repeated. | BR-13 should define semantic rule; NFR-001 should add measurable coverage/retention/concurrency properties or merge them. |
| `NFR-004`, `NFR-017`, `NFR-023`, BR-12 and `DR-13` | Data constraints are repeated across three requirement types. | Use DR rows as canonical invariants; NFRs only when they state a quality goal; BR only for domain semantics. |
| `NFR-006` and `DR-06` | History/audit semantics appear in NFR and Orders data group. | DR should own schema invariants; NFR should own audit/retention quality and completeness. |
| BR-02, `DR-15`, `NFR-023` | Tenant hierarchy, scope resolution, and structural consistency overlap. | Split hierarchy, legal entity scopes, write integrity, and read authorization into separate requirements. |
| `NFR-014` and `DR-16` | Retention batching/defaults appear twice. | Canonicalize the retention policy in one DR/NFR and reference it. |

## 7. Requirements That Should Be Split or Merged

### Split

1. `FR-009`: invitation onboarding versus temporary-password fallback. Different completeness statuses and security lifecycle.
2. `FR-020`: start/resume/list/get/cancel onboarding. At least separate workflow mutation from read APIs if independently accepted.
3. `FR-023`: device create/read/update/status/retire. Lifecycle guards differ materially.
4. `FR-025`: endpoint create, target configuration, provisioning, lifecycle, and transport-credential rotation. These have different prerequisites and security impact.
5. `FR-032`: GraphQL status overview versus REST heartbeat/device-event history.
6. `FR-041`: recipe CRUD/item replacement/lifecycle/versioning. Draft mutation and published-version creation are distinct invariants.
7. `FR-053`: topology, rebind history, and unified history timeline are different queries.
8. `FR-064`: seven GraphQL order read capabilities should have independent schema/evidence/test mappings.
9. `FR-066`: incident list/get, inspection, resolution selection, progress, and completion should be traceable by lifecycle transition.
10. `FR-078`: SignalR payment status versus durable intervention notification.
11. `FR-086`: diagnostics reads, manual requeue, and automatic delivery job.
12. `FR-098`–`FR-100`: verify import upload/validate/materialize/publish/composition/release-draft boundaries; long workflows should not hide partial states.
13. `FR-110`: deployment timeout reconciliation versus failure notification.
14. `FR-118`: list/get, cutover, rollback, and abandon package upgrades.
15. `FR-124`: event ingestion versus state-summary upsert.
16. `FR-128`: three hubs with different actors, group scopes, and authorization.
17. `FR-130`: initial dispatch versus timeout observation reconciliation.
18. `FR-132`: list/get, retry, resolve, and ignore; retry is partial while others are supported.
19. `FR-133`: dashboard query aggregation versus realtime invalidation.
20. Compound NFR/DR rows identified in the Status section (`NFR-003/004/006/007/015/016/017/020/022/023/025`, `DR-14/15/16`).

### Merge or subordinate

1. `FR-046` and `FR-047` can become acceptance rules/sub-requirements of `FR-045` if they have no independent interface or actor goal.
2. `FR-055` can be a domain/business-rule requirement used by `FR-109`, rather than two peer user-facing FRs, unless the evaluator is independently invoked by multiple workflows and needs its own acceptance suite.
3. `FR-127` can be a security/robustness sub-requirement of `FR-126`; it is internal to the MQTT consumer rather than a separate user goal.
4. `FR-129` is endpoint/framework wiring, better placed under external-interface/design constraints than as a user-visible functional requirement.
5. `NFR-001` and BR-13 should be consolidated or differentiated by semantic rule versus measurable coverage.
6. Entity-list `DR-01`–`DR-12` can be a data dictionary section, while actual data invariants become separately numbered DRs.

## Final Recommendation

The matrix is now complete as a 158-row SRS requirement index, but it should not yet be treated as a final verification traceability matrix.

Before final use:

1. Expand all 260 inventory IDs explicitly or add a one-row-per-inventory-ID appendix.
2. Replace the “first word wins” Status rule with weakest-material-component status, or split compound requirements.
3. Add verification/test columns and distinguish static evidence from verified satisfaction.
4. Split mixed-status rows, especially `FR-009`, `FR-110`, `FR-119`, `FR-132`, `NFR-004`, `NFR-006`, `NFR-025`, and `DR-14`–`DR-16`.
5. Convert entity-group DR rows into a data dictionary and add testable data invariants for relationships, constraints, tenancy, JSON schemas, retention, and migrations.
6. Normalize ownership versus transport/mechanism cross-references so shared handlers and SignalR/MQTT routes do not look like missing or duplicate business features.

