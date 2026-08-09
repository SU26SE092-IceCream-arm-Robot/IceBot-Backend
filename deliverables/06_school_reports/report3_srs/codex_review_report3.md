# Codex Review — Report 3 Software Requirement Specification

## Review Scope

Reviewed without modifying:

- `deliverables/06_school_reports/report3_srs/report3_srs.md`

Compared against the university-template mapping, the evidence-based SRS and Requirements Traceability Matrix (RTM), the required UML documents, conceptual/logical/physical database designs, repository-evidence files, and the current open-question register named in the review request.

This document contains review comments only. It does not approve product decisions, UI behavior, or runtime requirement satisfaction.

## Executive Assessment

Report 3 is structurally close to the university SRS template and is substantially complete as a backend-focused school-report draft. Its strongest part is functional-requirement preservation:

- all 133 SRS functional requirements (`FR-001`–`FR-133`) are present exactly once;
- every FR has a function description, actor/role, trigger, precondition/validation field, main flow, exception flow, related references, evidence, and status;
- every FR has an evidence field pointing to one or more functional-inventory IDs;
- the primary FR confidence status matches the current RTM for all 133 rows, including the corrected `Needs Review`/`Inferred` statuses for FR-009, FR-016, FR-052, FR-110, FR-119, and FR-132;
- no UI screens are invented; screen-owned sections are visibly marked `[Needs Team/UI Review]`;
- the non-screen overview covers APIs, GraphQL, webhooks, scheduled/background work, SignalR, sync/dead letters, Edge REST, MQTT, and payment behavior.

The draft is not ready for final DOCX conversion. Three material corrections are required first:

1. Ten NFRs retain the source SRS's older optimistic status wording instead of the RTM's weakest-material-component status.
2. Several copied cross-references still use the source SRS numbering and point to sections that do not exist or mean something different in this school report.
3. Section 3.1.5 contains a compact ERD but omits the entity-description table expected by the university template; it also differs from the existing ERD on ProductionIncident cardinality and must explain that evidence-based correction explicitly.

## Priority Findings

| Priority | Finding | Required disposition |
|---|---|---|
| High | NFR-003, NFR-004, NFR-006, NFR-007, NFR-015, NFR-016, NFR-017, NFR-020, NFR-022, and NFR-023 do not expose the RTM status as their controlling status. | Replace the compound/optimistic status wording with the RTM status, while retaining supported sub-claims in explanatory notes. |
| High | Internal references such as `§4.3`, `see §8`, `§6.6`, and `§6.4` are stale after restructuring. | Repoint them to Report 3 sections or qualify them as explicit source-document references. |
| High | The required entity-description table is missing from Section 3.1.5. | Add a concise entity/data-group table based on `database_inventory.md`; do not invent attributes or cardinalities. |
| Medium | The use-case actor columns aggregate roles over broad FR ranges and can be mistaken for authorization grants. | State that role lists are summaries only and require per-FR/policy verification; use defined actor names consistently. |
| Medium | The field titled “Related data/entities and implementation references” often contains controllers/handlers/inventory IDs but no explicit data entities. | Add actual related entities/data groups per FR or rename/split the field so it does not claim information it lacks. |
| Medium | The compact ERD shows `ORDER_ITEM ||--o{ PRODUCTION_INCIDENT`, while the current `03_uml/erd.md` shows `||--o|`. | Record that the database evidence found no unique constraint enforcing at most one incident per line; align the baseline ERD or mark the discrepancy `[Needs Review]`. |
| Minor | Twenty-four FR titles differ from RTM titles only in capitalization, punctuation, backticks, or explanatory wording. | Normalize titles if exact textual consistency is desired; IDs and statuses already match. |

## 1. University SRS Template Structure

### Result: substantially compliant, with one missing required artifact

The report contains the required major structure:

- I. Record of Changes;
- II. Software Requirement Specification;
- 1. Product Overview;
- 2. User Requirements with Actors, Use-Case Diagram(s), and Descriptions;
- 3. Functional Requirements with Sections 3.1.1–3.1.5 and bounded-context feature sections;
- 4. Non-Functional Requirements with External Interfaces and Quality Attributes;
- 5. Other Requirements / Requirement Appendix with Business Rules, Common Requirements, Application Messages, and Open Questions/Assumptions.

The Report 3 template expects the ERD together with entity descriptions. Section 3.1.5 provides only a diagram and qualification paragraph. It needs a native table such as `# / Entity or data group / Description / Evidence / Review status`. The full physical table inventory does not need to be copied into the SRS, but the principal entities shown in the compact ERD should be described.

The Report 3 template also expects function-level screen layouts when a function is a screen. The explicit UI placeholders are the correct treatment for this backend-only evidence set; they are not a structural defect. They remain completion blockers for any final submission whose system scope includes the tablet or management clients.

## 2. Actors

### Supported actor coverage

ACT-01 through ACT-09 correspond to actors or external systems established in `repo_truth_map.md`, the baseline SRS, and `use_case_diagram.md`:

- Customer;
- Tablet/Kiosk Client;
- SystemAdmin;
- OrgAdmin;
- Manager;
- Staff;
- Technician;
- Local Edge Backend;
- PayOS.

ACT-10, System Scheduler / Background Worker, is not listed as a main external actor in `repo_truth_map.md` and was intentionally omitted from the existing use-case diagram because it has no human/external trigger. Its inclusion as an internal system actor is nevertheless supported by the scheduled jobs represented by FR-031, FR-065, FR-073, FR-086, FR-101, FR-110, FR-119, FR-130, and FR-131. It should remain identified as an internal trigger, not be presented as a user class.

Firebase/Google/FCM, PostgreSQL, MinIO, and Mosquitto are correctly shown as external dependencies/systems in the context section. They should not be mixed into the human-role authorization model.

### Actor terminology and permission risks

- UC-03 and UC-07 use `Edge`, while the actor table defines `Local Edge Backend`. Use one name consistently.
- UC-12 uses the undefined phrase `operations users`. Replace it with the exact evidenced roles or explicitly label it as a role summary.
- UC-01–UC-12 aggregate role lists across wide FR ranges. For example, not every SystemAdmin/OrgAdmin/Technician action in UC-10 is authorized identically. The current table may be used as a use-case overview, but it must not be treated as the authorization matrix.
- The existing `use_case_diagram.md` states that its edges show only primary/most-restrictive actors and that exact roles live in each FR. Report 3 should carry the same qualification directly beside its use-case table.

## 3. Use Cases

### Evidence support

The twelve use cases are defensible consolidations of the existing SRS ranges:

- UC-01 through UC-06 cover Identity, Tenants, Devices, Catalog, Sales Catalog, and Inventory;
- UC-07 and UC-08 cover Orders/Dispatch and Payments;
- UC-09 covers Operations;
- UC-10 and UC-11 cover Robot/Production Configuration and Packages;
- UC-12 covers IoT, realtime, GraphQL, Sync, and Dashboard.

Each use case points to an existing FR range. No wholly new user-facing capability was found.

### Qualifications

- UC-07 adds FR-130 to the order range, which is appropriate because automatic dispatch/reconciliation is part of the complete order fulfilment slice.
- UC-12 is broad: it combines machine-to-machine Edge behavior, operator dead-letter management, realtime client subscriptions, GraphQL wiring, metrics, and dashboard aggregation. This is acceptable as a diagram-level grouping but should not be used as a single testable use case.
- The use-case descriptions meet the university template's summary-table expectation, but they are not full use-case specifications with preconditions, postconditions, main flow, and alternatives. Those details exist at FR level. The report should explicitly state that FR records are the detailed specification, as it currently does.
- Background jobs are correctly absent from the use-case diagram itself, even though ACT-10 appears in the actor table. Their authoritative location is the non-screen function table and detailed FRs.

## 4. Functional Requirements and Traceability

### Completeness and duplication

Mechanical checks found:

| Check | Result |
|---|---:|
| FR headings | 133 |
| Unique FR IDs | 133 |
| Missing IDs from FR-001–FR-133 | 0 |
| Duplicate IDs | 0 |
| FR evidence fields | 133 |
| FR status fields | 133 |
| FR trigger/main/exception/validation fields | 133 each |

No functional requirement is missing or duplicated relative to `srs.md` and the RTM. Report 3 preserves the same consolidation level as the baseline: 260 identifiable functional-inventory rows grouped into 133 FRs. It does not create a second, incompatible requirement set.

### Status comparison with the RTM

All 133 Report 3 FRs use the same primary status as the RTM. In particular:

- FR-009, FR-016, and FR-132 are `Needs Review`;
- FR-052, FR-110, and FR-119 are `Inferred`;
- the remaining FRs retain the RTM status.

Twenty-four title comparisons differ textually, but the differences are editorial rather than semantic: title case, slash/comma choices, backticks around `.lua`, and expansions such as “Dispenser (Container).” Requirement identity is preserved by the FR ID.

### Evidence and data mapping

Every FR cites one or more `functional_inventory.md` IDs, providing a usable route to the RTM and detailed evidence. However, direct RTM row references are not included in each FR. This is acceptable because IDs are stable, but a conversion note or references section should identify `requirements_traceability_matrix.md` as the authoritative confidence/evidence index.

The mechanically renamed field **Related data/entities and implementation references** is not consistently populated with data entities. Many rows contain only inventory IDs, controllers, handlers, services, or routes. This satisfies implementation traceability but not the requested related-data/entity field. Recommended correction:

- split it into **Related data/entities** and **Implementation references**, or
- add the actual entities/data groups while retaining the existing references.

Do not infer entities solely from an endpoint or handler name; use `database_inventory.md` and the logical database design.

### Known compound requirements

The report inherits the baseline's deliberate many-to-one consolidation. Existing reviews already identify compound rows such as FR-023, FR-025, FR-041, FR-064, FR-066, FR-098–FR-100, FR-110, FR-118, FR-128, FR-130, and FR-132 as candidates for future splitting. This is not a new omission in Report 3, and the report should not split them independently without first revising the owning SRS and RTM.

## 5. UI Screen Boundary

### Result: correctly handled

The report does not invent screens. Sections 3.1.1, 3.1.2, 3.1.3, and 5.3 are clearly marked `[Needs Team/UI Review]` and explain why backend routes do not prove screen existence, layout, navigation, authorization visibility, or message wording.

References such as “Management UI client” inside imported FR actor fields come from the baseline SRS/functional inventory and identify the expected caller class; they do not describe a concrete screen. The report should preserve that distinction during final editing.

The minimum proposed UI flows in Section 3.1.1 are review prompts, not claimed implemented screens. Their wording is appropriately bounded, but the team must replace them with evidence from the owning frontend/tablet repositories before submission.

## 6. Non-Screen Functions

### Result: comprehensive and correctly categorized

Section 3.1.4 includes all categories requested by the university mapping and this review:

- public/customer APIs;
- authenticated management REST and GraphQL behavior;
- PayOS webhook handling;
- IoT REST ingestion and command APIs;
- MQTT publishing, consumption, validation, and credentials;
- scheduled/background reconciliation, retention, cleanup, metrics, and notification work;
- SignalR publication;
- sync/dead-letter processing.

The table correctly treats these as backend functions even when a human UI triggers or observes them. It also correctly keeps Cloud-to-Edge MQTT wake-up separate from durable command pull/ack behavior.

One wording refinement is needed: the broad “Management APIs/queries — FR-001–FR-119” range contains customer, provider, and system-triggered FRs as well as management functions. Either list the exact management subranges or label the range as a broad feature-area reference rather than an exclusive classification.

## 7. ERD and Database Claims

### Supported aspects

The compact ERD uses entity names and principal relationships found in `database_inventory.md` and the database-design documents. It appropriately states that the detailed ERD and database designs remain authoritative for omitted entities, exact keys, optionality, indexes, and constraints. It also warns readers not to infer mandatory cardinality from a collection navigation.

The following simplifications are reasonable for an SRS overview:

- omitting most audit, retry, projection, package-child, and join tables;
- representing RobotProgram–RobotArtifact as a conceptual many-to-many relationship;
- representing only principal payment, configuration, Edge, alert, and maintenance relationships.

### Required corrections or qualifications

1. **Entity descriptions are missing.** The university template explicitly expects them.
2. **ProductionIncident cardinality conflicts with the current ERD deliverable.** Report 3 shows one OrderItem to zero-or-many ProductionIncidents; `03_uml/erd.md` shows zero-or-one. The conceptual/database review notes that no unique constraint proving “at most one” was found, so zero-or-many is the safer physical interpretation. The report should state that it intentionally corrects or supersedes the old ERD cardinality; otherwise the document set remains inconsistent.
3. **Join-entity semantics are hidden.** `RobotProgramArtifact` carries ordering information, and other omitted joins may carry quantities or configuration meaning. The diagram should label conceptual many-to-many edges as simplified joins, as `class_diagram.md`/database reviews do.
4. **Tenant-scope relationships are omitted.** This is acceptable for readability, but the entity-description table should identify Organization as the tenant root and avoid implying universal database-level tenant enforcement.
5. **Delete behavior remains unresolved.** The report's caution is correct: it must not state that explicit Cascades are effective until the final EF model/generated migration behavior is inspected.

## 8. Non-Functional Requirements

### High-priority status mismatch

Report 3 copied the original SRS NFR paragraphs but did not apply the RTM's corrected controlling status. The prose often contains the weaker qualifier, but its explicit `Status:` clause begins with `Supported`, which can mislead readers and filters.

| NFR | Report-leading status | RTM controlling status | Reason |
|---|---|---|---|
| NFR-003 | Supported + Inferred sub-claim | Inferred | Job existence is supported; general recovery semantics are not. |
| NFR-004 | Supported + Unclear exception | Needs Review | Effective Restrict/Cascade behavior is unresolved. |
| NFR-006 | Supported + Inferred/inconsistency | Needs Review | Audit/history coverage is not uniform and parallel history tables differ. |
| NFR-007 | Supported for cited endpoints | Needs Review | Universal authorization coverage was not audited. |
| NFR-015 | Supported + inferred rationale | Inferred | Independent evolution is architectural rationale, not observed quality evidence. |
| NFR-016 | Supported + inferred effect | Inferred | Broker load sharing does not prove end-to-end duplicate-free processing. |
| NFR-017 | Supported with known gap | Needs Review | The stated high-volume index scope includes a documented missing index. |
| NFR-020 | Supported for cited endpoints | Needs Review | Universal diagnostics/curated-read separation was not audited. |
| NFR-022 | Supported + inferred rationale | Inferred | Storage split is supported; scaling/backup benefits are not measured. |
| NFR-023 | Supported for examples + Unclear coverage | Unclear | Composite tenant-FK protection is enumerated, not universal. |

NFR-025 already exposes its uncertainty. The remaining NFRs are consistent with the RTM at their stated scope.

### Requirement-quality qualification

Several NFRs are architecture or persistence constraints rather than measurable quality targets, notably NFR-004, NFR-005, NFR-015, NFR-017–NFR-019, NFR-022–NFR-024. This is inherited from the baseline SRS. For the final school report, either:

- classify them explicitly as design constraints under Quality Attributes; or
- add measurable acceptance criteria where a quality outcome is intended.

No runtime performance, availability, MTBF/MTTR, capacity, defect-rate, recovery, or security-test evidence is currently mapped. The report correctly records these as missing/open rather than inventing targets.

## 9. Cross-Reference Defects

The school report changed the source SRS section numbers but retained several source-local references. These are not harmless citations because they appear without a source filename and therefore point readers to the wrong place in Report 3.

| Current reference | Problem | Expected treatment |
|---|---|---|
| IoT note at approximately line 1587: `§4.3` | In Report 3, Section 4.3 does not exist; Devices is Section 3.2.3. | Change to Section 3.2.3 or explicitly cite `srs.md` §4.3. |
| NFR-004, NFR-006, NFR-017, NFR-025: `see §8` | Report 3 has no Section 8. | Point to Section 5.4 or the precise open-question ID/source. |
| NFR-007: “endpoints cited in §4” | Section 4 is NFRs in Report 3, not functional requirements. | Point to Section 3.2 / FR-016. |
| Business-rule introduction: requirements in `§4` | Same numbering drift. | Point to Section 3.2. |
| BR-02: `§6.6, §8` | Neither section exists in Report 3. | Point to Section 5.4 and an explicit data-design source. |
| BR-12: `§6.4` | Section does not exist in Report 3. | Cite `srs.md` §6.4 or the physical database design/index section explicitly. |

Repository evidence citations such as `repo_truth_map.md` §6 and `database_inventory.md` §3 are valid because the source file is named. Only unqualified references need renumbering or source qualification.

## 10. Placeholders and Team Completion

### Clear placeholders

- cover metadata: official project name, code, group, location/date;
- Record of Changes author/date;
- screen flow;
- screen descriptions;
- screen authorization;
- application messages;
- open product, UI, operations, deployment, data-integrity, and verification decisions.

The `[Needs Team/UI Review]` label is explicit and consistently explains why content is absent. The draft completion notice also identifies the main pre-conversion actions.

### Missing or insufficient completion prompts

- Section 3.1.5 needs an entity-description-table placeholder with clear owner/evidence instructions.
- The use-case actor table needs a note requiring role/policy verification before it is reused as authorization evidence.
- The detailed FR data/entity field needs a completion method, not only an implementation-reference list.
- NFRs need a consistent visible status field so the team can review them mechanically as it reviews FRs.
- The final report needs an explicit approved-baseline/RTM revision placeholder, because the current Open Questions section identifies this but the cover/change record does not capture it.

## Final Recommendation

Treat Report 3 as a strong complete draft of the backend requirement baseline, but not as an approval-ready school SRS. Before DOCX conversion:

1. Apply the RTM controlling status to the ten mismatched NFRs.
2. Repair all stale internal section references.
3. Add the required entity-description table and explain the ProductionIncident-cardinality discrepancy.
4. Qualify aggregated use-case actors and normalize actor terminology.
5. Separate actual related entities from implementation references in each FR.
6. Retain the current UI placeholders until the frontend/tablet teams provide evidence.
7. Obtain independent review against one frozen SRS/RTM/evidence baseline.
