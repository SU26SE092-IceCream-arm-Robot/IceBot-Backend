# Codex Review — Project Introduction and SRS

## Review Scope

Reviewed without modifying the source documents:

- `deliverables/01_project_introduction/project_introduction.md`
- `deliverables/02_srs/srs.md`
- `deliverables/00_repo_evidence/repo_truth_map.md`
- `deliverables/00_repo_evidence/functional_inventory.md`
- `deliverables/00_repo_evidence/database_inventory.md`
- `deliverables/05_review_checklists/evidence_review_final.md`

The requested path `deliverables/00_repo_evidence/evidence_review_final.md` does not exist. The review used the existing prior review at `deliverables/05_review_checklists/evidence_review_final.md`. This path discrepancy should be corrected in the document set or explicitly explained in the final report.

This file contains review comments only. It does not propose edits to `src/`, `docs/`, or any reviewed deliverable.

## Overall Assessment

The project introduction is a useful high-level summary, and the SRS is unusually comprehensive. However, both documents inherit known defects from the evidence files and frequently convert an implementation inventory into normative “shall” requirements without preserving evidence confidence. The SRS claims every requirement is traceable, but 45 functional-inventory IDs are not cited anywhere in it. Many of those capabilities are conceptually grouped into broader FRs, yet the missing IDs make the claimed traceability incomplete and conceal detailed behavior.

The most important corrections before finalization are:

1. Replace the incorrect `~130 DbSet<T>` count with 98.
2. Reconcile the claimed 265 functional capabilities with the 260 identifiable inventory rows.
3. Stop treating `Partial` as “confirmed implemented.”
4. Reclassify requirements derived only from documentation, wiring, patterns, or indirect evidence.
5. Add a complete inventory-ID-to-FR traceability matrix.
6. Separate current implementation facts from intended requirements and desired non-functional outcomes.

## 1. Unsupported Claims

| Severity | Location / claim | Review comment |
| --- | --- | --- |
| Critical | `srs.md:1749`: “~130 `DbSet<T>` properties.” | False against the repository and the prior evidence review. `IceBotDbContext.cs:109-214` exposes **98** public DbSets. The SRS copied a known error from `database_inventory.md:19`. |
| High | `project_introduction.md:80`: “Confirmed as implemented (Status = Implemented/Partial…).” | `Partial` does not mean confirmed complete. It explicitly means incomplete, stubbed, or narrower than documentation. IDN-15b and SYNC-05 must be presented as limitations, not as confirmed implemented scope. |
| High | `project_introduction.md:80`, `srs.md:58`: 265 inventoried capabilities. | Mechanical inspection finds **260 identifiable capability rows**. The functional inventory summary reports Operations=26 but contains 22 `OPS-*` rows, and Payments=17 but contains 16 `PAY-*` rows: a five-row overcount. Until reconciled, use neither count as settled fact. |
| High | `srs.md:16-20`: “every requirement traceable to a concrete evidence path.” | Not true as written. Forty-five inventory IDs do not appear anywhere in the SRS. Several FRs cite broad ranges or sections instead of the exact rows needed for their detailed flows. |
| High | SRS legend: `Supported` means “directly observed working code (route/consumer → handler → domain/persistence).” | The evidence review already found that multiple `Implemented` rows were based only on controller wiring, documentation, or sibling-pattern assumptions. “Working” also implies runtime/test evidence that static source inspection does not establish. |
| High | `FR-016` and `NFR-007`: scoped RBAC is enforced on “every management endpoint.” | The evidence base did not exhaustively inspect every controller/action, full permission matrix, handler scope check, GraphQL resolver, and SignalR hub. This universal claim should be `[Unclear]` until an authorization coverage audit proves it. |
| High | `srs.md:154`: GraphQL is “read-only management aggregation.” | `repo_truth_map.md` explicitly left mutation absence as an open question, and the prior review says read-only is likely but was not verified by the evidence pass. The SRS contradicts its own §8.2 open question. |
| High | `srs.md:1673-1674`: background jobs recover stuck workflows “without requiring manual intervention in the common case.” | Job existence proves periodic attempts, not recovery success or an operational guarantee. Several paths deliberately end in support/intervention states. “Recover” should be limited to the concrete transitions each job performs. |
| High | `NFR-006`: Orders, Payments, Alerts, Maintenance Tickets, and Operations have append-only history tables. | The database inventory lists explicit history tables for orders/items and production incidents, but not a uniform dedicated append-only history table for every named aggregate. Alerts and maintenance tickets store lifecycle fields and operation logs may provide evidence, but that is not the same claim. |
| High | `NFR-023`: composite FKs make cross-tenant persistence structurally invalid. | The prior review warns that composite tenant-consistency FKs cover selected relationships, not every cross-context/tenant reference. The SRS wording is partly qualified (“for the relationships they cover”) but its consequence is still too broad without an enumerated coverage map. |
| Medium | `project_introduction.md:54`: “this is a working backend.” | Static source and wiring evidence do not prove the whole backend is runnable in the target environment, correctly configured, or passing integration tests. Mark `[Inferred]` or say “implementation repository containing wired backend code.” |
| Medium | `srs.md:73`: exactly one payment provider, identity provider, object store, MQTT broker, and push channel. | The repository demonstrates current adapters/configuration, not necessarily a product constraint of exactly one provider or deployed instance. Use “current integration” and mark exclusivity `[Assumption]`. |
| Medium | `srs.md:95`: PostgreSQL 17 and database name `IceBotDB` as a product requirement. | These may be current environment/configuration defaults rather than stable requirements. Cite configuration/container evidence and distinguish “current deployment baseline” from “shall use.” |
| Medium | `srs.md:133`: MQTT “v5-style” and QoS1. | This requires direct client/options/topic evidence. A “style” is vague; state the actual MQTT protocol version configured, if proven. |
| Medium | `srs.md:107` and BR-04: Edge “must still pull/ack over REST (or equivalent MQTT uplink handler).” | “Must still pull over REST” conflicts with “or equivalent MQTT uplink handler.” The evidence says MQTT notification-only for command availability, while uplink messages can dispatch to the same application handlers. Separate downlink pull/ack rules from uplink ingestion. |
| Medium | `NFR-022`: object storage keeps the database lean and allows independent scaling/backup. | External storage is proven; the stated benefits are architectural rationale, not verified behavior. Mark the benefit wording `[Inferred]`. |
| Medium | `NFR-015`: bounded contexts allow independent evolution. | This is architectural intent/rationale. Compile-time boundaries are evidence; independent evolution is an expected benefit and should be `[Assumption]` or rationale, not a verified result. |
| Medium | `NFR-020`: dedicated `operations.diagnostics`-class policy universally separates curated and diagnostic reads. | Several example endpoints support the pattern, but the evidence does not establish complete, uniform application across all raw-payload/provenance surfaces. |

## 2. Missing Major Features from the Evidence Files

The prior evidence review identifies material backend/platform capabilities that are absent or insufficiently visible in the introduction and/or SRS:

| Feature | Project introduction | SRS | Review comment |
| --- | --- | --- | --- |
| Data-retention job and retention policy | Mentioned only indirectly in database notes/out-of-scope discussion | Captured as NFR-014 and §6.7 | The SRS includes it, but the introduction should mention lifecycle/retention as a platform operation if operational scope is summarized. |
| Identity/bootstrap seeding | Missing | Missing as a functional/deployment requirement | `IdentityBootstrapHostedService` establishes initial security/reference data. Include under deployment assumptions or explicitly exclude bootstrap behavior from SRS scope. |
| Payment-method bootstrap | Missing | Payment catalog management exists, startup seeding is absent | `PaymentMethodCatalogHostedService` is operationally required plumbing. State whether startup seeding is contractual or deployment-only. |
| Robot-artifact object-storage startup validation | Missing | FR-101 covers orphan cleanup, not startup validation | Object storage validation affects startup/readiness and should be represented under availability/deployment requirements. |
| MQTT credential reconciliation | Broadly included | FR-026 includes it | Good functional inclusion, but final evidence should cite the job and its enabled/configured conditions. |
| Payment-session background reconciliation | Broad flow only | FR-073/NFR-003 | Represented, but “recovery” semantics and terminal intervention behavior need precision. |
| Order dispatch reconciliation | Broad flow only | FR-130/NFR-003 | Represented; exact idempotency, candidate selection, and failure terminal states need explicit evidence. |
| Deployment-failure notification | Not explicitly listed | Combined into FR-110 | The combined FR obscures notification recipients, deduplication, retry, and delivery failure behavior. |
| Production-package upgrade reconciliation | Broad package capability | FR-119/NFR-003 | Represented but should specify stale criteria, lease/concurrency behavior, and terminal outcomes. |
| Health/readiness/management diagnostics | Only API surface context | Listed as a route surface, no functional requirement | Add explicit health/readiness/info/management-diagnostics contract requirements or mark them operationally out of SRS scope. |
| API versioning and error contract | Missing | Route versioning is shown, but no versioning behavior/problem response requirement | Add supported version policy, deprecation behavior, validation/error envelope, and status-code rules. |
| Rate limits, CORS, request/body limits | Missing | Missing | These are major public/IoT/artifact-upload interface constraints. If not configured or evidenced, mark `[Open Question]`. |
| Backup/restore and disaster recovery | Missing | Missing | A final SRS should either define RPO/RTO/backup responsibility or identify them as open non-functional requirements. |
| Audit visibility and soft-delete recovery | Broad audit mention | Structural soft-delete notes only | Define who can see deleted records, whether restore exists, retention of deleted data, and audit actor requirements. |

## 3. Requirements Without Adequate Evidence

The following requirement patterns are not adequately supported by the cited evidence level:

### Universal authorization requirements

- `FR-016` and `NFR-007` require every management endpoint to enforce scoped RBAC.
- Evidence does not include a complete authorization matrix or exhaustive endpoint/resolver/hub audit.
- Required classification: `[Open Question]` pending an authorization coverage check.

### Runtime quality guarantees inferred from static code

- “Working,” “atomically,” “exactly once,” “without duplicate processing,” “recover,” “guarantee,” and “without requiring manual intervention” require transaction, concurrency, retry, and test evidence.
- A unique index or handler check may support deduplication, but does not automatically prove exactly-once external effects.
- Requirements using these words should cite the transaction boundary and relevant tests, or be marked `[Inferred]`.

### Provider and infrastructure specificity

- PostgreSQL 17, `IceBotDB`, MinIO, Mosquitto Dynamic Security, Firebase/Google, FCM, PayOS, QoS1, and advisory locks are implementation/deployment claims.
- Some are supported by the database inventory or functional rows; others are inherited indirectly from the introduction.
- Each must be classified as either a required constraint, current implementation choice, or replaceable adapter. The current SRS mixes these categories.

### NFRs that are implementation descriptions, not quality requirements

`NFR-004`, `NFR-005`, `NFR-015`, `NFR-017`, `NFR-018`, `NFR-019`, `NFR-022`, `NFR-023`, and `NFR-024` mostly specify architecture/persistence mechanisms. They may be legitimate design constraints, but they are not measurable quality targets. Move them to design constraints or add measurable acceptance criteria.

### Missing test evidence

The SRS acknowledges no mapping to unit/integration tests. Therefore `Supported` should mean “statically code-evidenced,” not “working” or “verified.” No FR should imply runtime acceptance until test or execution evidence is linked.

### Detailed API assertions inherited from incomplete API evidence

Exact response DTOs, validation, error status, public/authenticated classification, GraphQL nullability/paging, SignalR payloads, and health endpoint disclosure were not exhaustively verified in the underlying evidence. Requirements containing these details need direct controller/DTO/schema evidence or `[Unclear]`.

## 4. Functional Inventory Features Not Represented or Not Traceable in the SRS

Mechanical comparison found 45 inventory IDs not cited anywhere in `srs.md`:

`CAT-02`, `CAT-03`, `CAT-08`, `CAT-16`, `CAT-18`, `DEV-02`, `DEV-05`, `DEV-06`, `DEV-07`, `DEV-11`, `DEV-12`, `DEV-13`, `DEV-24`, `INV-10`, `IOT-02`, `IOT-03`, `OPS-02`, `OPS-05`, `OPS-09`, `OPS-12`, `OPS-13`, `OPS-14`, `OPS-20`, `ORD-09`, `ORD-13`, `ORD-14`, `ORD-15`, `ORD-16`, `ORD-17`, `ORD-21`, `PP-13`, `PP-14`, `RC-20`, `SIG-02`, `TEN-02`, `TEN-03`, `TEN-06`, `TEN-07`, `TEN-08`, `TEN-09`, `TEN-12`, `TEN-13`, `TEN-14`, `TEN-17`, `TEN-18`.

These fall into two groups.

### Group A — Conceptually grouped into an FR but missing exact traceability

The following appear to be subsumed by broader requirements, but their inventory IDs and important rules are absent from the FR evidence mapping:

- `TEN-02`, `TEN-03` under FR-017.
- `TEN-06`–`TEN-09` under FR-018.
- `TEN-12`–`TEN-14` under FR-019.
- `TEN-17`, `TEN-18` under FR-020.
- `DEV-02` under FR-022.
- `DEV-05`–`DEV-07` under FR-023.
- `DEV-11`–`DEV-13` under FR-025.
- `DEV-24` under FR-032.
- `CAT-02`, `CAT-03` under FR-033.
- `CAT-08` under FR-036.
- `CAT-16`, `CAT-18` under FR-041.
- `INV-10` under FR-053.
- `ORD-09` under FR-062.
- `ORD-13`–`ORD-17` under FR-064.
- `ORD-21` under FR-066.
- `OPS-02` under FR-079.
- `OPS-05` under FR-084/FR-080.
- `OPS-20` under FR-086.
- `SIG-02` under FR-128.

Review action: add these IDs to each FR’s `Related`/evidence list and preserve their specific role, route, guard, filter, and failure semantics. Without this, the SRS is not fully traceable even when the feature name appears.

### Group B — Requires manual confirmation of representation

The following uncited IDs should be checked individually because broad FR titles may not preserve their distinct behavior:

- `IOT-02`, `IOT-03`: command acknowledgement/report or heartbeat ingestion may be duplicated across Edge/Devices requirements, but the entry-point contract must remain independently traceable.
- `OPS-09`, `OPS-12`, `OPS-13`, `OPS-14`: alert/maintenance subflows can disappear inside combined lifecycle FRs, especially actor, reason, transition, and terminal-state constraints.
- `PP-13`, `PP-14`: package-upgrade cutover/rollback/abandon or reconciliation subflows need explicit failure and recovery semantics.
- `RC-20`: a robot-configuration capability is not cited and must not be assumed covered by neighboring authoring FRs.

Because the inventory contains 260 identifiable rows and the SRS consolidates them into 133 FRs, a formal many-to-one traceability table is required. Narrative grouping is insufficient.

## 5. Database and Entity Conflicts

| Location | Conflict |
| --- | --- |
| `srs.md:1749` | Claims ~130 DbSets; repository has 98. |
| `srs.md:1749` | Treats “~99 tables created across 5 migrations” as current schema truth. The prior review says current mapped-table count must be verified against the model snapshot with a defined counting rule; cumulative `CreateTable` calls are evolution evidence, not automatically current truth. |
| `project_introduction.md:31` | Repeats ~99 tables without distinguishing current mapped tables, cumulative migration creations, or the implicit `AccountStores` table. |
| `srs.md:1754` | Entity grouping is mostly consistent, but combines Production Configuration, Production Execution, and Production Packages into one group. These are separate ownership concepts in the truth map and should remain distinguishable. |
| `srs.md:1757`, `NFR-023` | Composite tenant-consistency FKs are examples, not a global tenant-integrity guarantee. |
| `srs.md:1764` | “Unique forever” is stronger than an unfiltered unique index. Soft deletion does not free the key, but physical deletion or migration changes could. Say “unique across retained rows, including soft-deleted rows.” |
| `srs.md:1765` | “At most one default ProductOption per group” should be checked against the exact filtered index semantics and nullable behavior. Do not generalize beyond the filter predicate. |
| `srs.md:1769-1770` | JSON “roles” are interpretive categories. The prior evidence review says mark them `[Inferred]` unless explicit code/docs state the purpose. |
| `srs.md:1772-1773`, BR-02 | The shared `TenantScopeType` order is not uniformly valid for every entity. `RobotProgram` rejects Global even though the enum includes it; each entity’s allowed scopes must be stated separately. |
| `srs.md:1776`, BR-05 | Listing 12 soft-delete filter exceptions is structural evidence, but “require explicit `WhereNotDeleted()`” needs enforcement/audit evidence. A convention exception creates developer responsibility; it does not guarantee every query complies. |
| `NFR-004` | The SRS declares explicit cascade exceptions while simultaneously acknowledging it is unresolved whether a later global `Restrict` loop overwrites them. Until resolved, the cascade portion is `[Unclear]`. |
| `NFR-006` | Claims a consistent append-only audit model despite the documented base-class inconsistency and lack of uniform history tables. |
| `BR-12` | Combines multiple partial unique indexes into a universal business-rule sentence. Each exact status/filter predicate should be retained; “active” is not the same predicate across aggregates. |

## 6. Vague or Overconfident Wording

Replace or qualify these patterns throughout:

| Wording | Problem | Better classification |
| --- | --- | --- |
| “working backend” | Implies executed/tested system health. | `[Inferred] implementation appears wired` |
| “confirmed as implemented” for `Partial` | Erases known incompleteness. | `Implemented` and `Partial` listed separately |
| “every requirement traceable” | False until all inventory IDs and direct evidence are mapped. | `[Open Question] traceability incomplete` |
| “every management endpoint” | Universal security claim without exhaustive audit. | `[Open Question]` |
| “atomically” | Needs explicit transaction boundary and failure tests. | `[Inferred]` unless directly evidenced |
| “exactly once” | Usually not established for distributed side effects. | Prefer idempotent/at-most-once/unique-at-DB-boundary as actually proven |
| “without duplicate processing” | Shared subscriptions do not alone prove no duplicate delivery/processing. | State broker load-sharing plus application idempotency separately |
| “recover” | Some jobs only detect, transition, retry, or escalate. | Use the exact action |
| “immutable” | May mean API/domain guarded, database constrained, or retained snapshot. | State the enforcement layer |
| “unique forever” | Overstates index behavior and retention assumptions. | “unique across retained rows including soft-deleted rows” |
| “read-only GraphQL” | Not verified in the original evidence pass. | `[Unclear]` |
| “one external provider/broker/store” | Confuses current adapter with architectural cardinality. | `[Assumption] current configured integration` |
| “allows independent scaling/backup/evolution” | Benefit/rationale, not observed requirement satisfaction. | `[Inferred rationale]` |
| “never” / “always” | Requires exhaustive path and configuration evidence. | Restrict to the verified component/path |

## 7. Items to Mark `[Inferred]`, `[Assumption]`, or `[Open Question]`

### Mark `[Inferred]`

- The repository is a fully working backend rather than a substantially implemented/wired backend.
- Product-specific “ice cream / beverage” positioning and franchise commercial intent.
- JSON-field purpose categories.
- Benefits attributed to modularity, MinIO, indexing, bounded batches, or external storage.
- “Current management UI” and “tablet UI” characteristics inferred from API surfaces.
- Atomicity for workflows where the evidence citation does not identify the transaction boundary.
- Complete behavior of handlers previously verified only through wiring, docs, or sibling patterns.
- Uniform query compliance with `WhereNotDeleted()` for excluded principal types.

### Mark `[Assumption]`

- PayOS, Firebase/Google, MinIO, Mosquitto, and FCM are sole or permanent providers.
- PostgreSQL 17 and database name `IceBotDB` are binding production constraints rather than current defaults.
- Every deployment enables every hosted reconciliation/cleanup job.
- Edge runtime behavior beyond the Cloud-facing contract.
- The final report’s system boundary includes or excludes bootstrap, migration manual steps, health endpoints, and infrastructure operations.
- “Common case” recovery needs no operator intervention.

### Mark `[Open Question]`

- Is the authoritative functional capability count 260, or are five rows missing from the inventory body?
- Is the current physical-table count 99 under an explicit model-snapshot counting rule?
- Is GraphQL guaranteed mutation-free for the target release?
- Does every management REST action, GraphQL resolver, and SignalR hub method enforce the intended policy and tenant scope?
- Are explicit cascade configurations actually preserved after global delete-behavior conventions run?
- Which inventory features are intentionally consolidated, and where is the full inventory-ID-to-FR mapping?
- What test evidence supports each `Supported` requirement?
- What are the exact API error envelope, status codes, version policy, rate limits, CORS policy, and request-size limits?
- Which background jobs are mandatory for correctness, and in which deployment profiles are they enabled?
- What are backup/restore, RPO, RTO, availability, monitoring, and incident-response requirements?
- What are the exact retention and deletion semantics for append-only/evidence records?
- Which scopes are legal for each entity using `TenantScopeType`, especially `RobotProgram`?
- Is `ProductOption.TemplateProductOptionId` intentionally unenforced by an FK?
- Are manual migration-step classes mandatory and how are they executed?
- Is temporary-password onboarding part of the release contract, given that code exists but the cited identity rules exclude it?
- What is the operator workflow for dead-letter event families that cannot be replayed?

## Final Review Recommendation

Do not treat the current SRS as an evidence-complete baseline yet. It should remain a working draft until:

1. The evidence-count and DbSet-count errors are resolved.
2. Every inventory row maps to one or more FRs, including status/confidence.
3. Each FR separates route evidence, behavior evidence, persistence evidence, authorization/tenancy evidence, and test evidence.
4. `Supported` is redefined as static code evidence unless runtime/test proof exists.
5. The SRS distinguishes implemented behavior, normative requirement, architecture constraint, assumption, and unresolved product decision.

