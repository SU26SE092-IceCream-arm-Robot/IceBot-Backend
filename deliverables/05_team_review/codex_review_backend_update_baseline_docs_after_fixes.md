# Codex Review — Backend Update Baseline Documents After Fixes

## 1. Review Scope and Method

This review evaluates only the baseline-document fixes represented by `backend_update_baseline_docs_fix_checklist.md`. It compares the current Project Introduction, SRS, requirements traceability matrix (RTM), UML documents, database-design documents, evidence inventories, backend impact analysis, and open-question register.

No reviewed file was modified. No build, test suite, migration, external Edge runtime, payment provider, or deployment workflow was executed. “Supported” observations in this review therefore refer to inspected documentation/source evidence, not runtime acceptance.

The Critical/Major items marked **Safe to Auto-Fix: Yes** are:

- BFX-002
- BFX-006
- BFX-007
- BFX-012
- BFX-029
- BFX-030
- BFX-031
- BFX-032
- BFX-036
- BFX-037
- BFX-039

## 2. Overall Result

**Result: Major corrections remain required.**

The baseline set contains useful post-sync additions—FR-134, FR-135, current database counts, observation/binding prose, and parts of the Production Configuration behavior—but the checklist-approved Critical/Major fixes were not completed consistently. Of the eleven applicable fixes, BFX-012 is only partially applied; the other ten remain open in their target sections.

The SRS and RTM still contain a direct NFR-013 contradiction. Multiple UML cardinalities conflict with the logical/database evidence. The logical database design still carries the obsolete `ExecutionRouteRobotBinding` row. These issues prevent the baseline from being described as fully synchronized.

## 3. Critical/Major Safe-Fix Verification

| Fix ID | Result | Review evidence |
|---|---|---|
| BFX-002 | **Not applied** | `srs.md` §6.2 still lists Inventory without `InventorySensorObservation` and Production Configuration without `ProductionProgramBinding`, although FR-134/FR-135 and RTM DR-04/DR-10 include them. |
| BFX-006 | **Not applied** | RTM NFR-013 still states “15-second cache” and `Supported`, while SRS NFR-013 defines optional bounded caching, admission-before-cache, database fallback, request-metadata separation, and unresolved TTL/invalidation/profile/thresholds. |
| BFX-007 | **Not applied** | `sequence_order_flow.md` still returns `Runtime menu snapshot (ETag, 15s cache)` and its evidence note repeats “ETag/15s cache.” No optional-cache miss/disabled/failure fallback fragment is present. |
| BFX-012 | **Partially applied** | SRS FR-102 and FR-105–FR-107 describe revision tokens, operator reasons, audit scope, and stale rollback observation. Their Evidence fields still cite only pre-sync `functional_inventory.md` PC rows and do not map the current request/result fields, handlers/audit writer, concurrency tests, or impact evidence requested by the fix. |
| BFX-029 | **Not applied** | `erd.md` uses `PRODUCTION_PROGRAM_BINDING ||--o{ EXECUTION_ROUTE_ROBOT_BINDING`, which makes a Production Program Binding mandatory from the route-binding side. The FK is optional; the route binding must reference zero-or-one binding. |
| BFX-030 | **Not applied** | `class_diagram.md` still shows `OrderItem "1" --> "0..1" ProductionIncident`, while logical design states schema-level zero-to-many and retains a separate open question about concurrent/open incidents. |
| BFX-031 | **Not applied** | `erd.md` still shows `ORDER_ITEM ||--o| PRODUCTION_INCIDENT`, conflicting with the zero-to-many physical/logical relationship. |
| BFX-032 | **Not applied** | `erd.md` still shows `KIOSK ||--o{ MENU : offers`, implying mandatory Kiosk ownership on the Menu side. `Menu.KioskId` is nullable and organization/store scopes exist. |
| BFX-036 | **Not applied** | `class_diagram.md` renders `DerivedEstimatedQuantity` as non-nullable and omits material observation identities/timestamps/provenance without an explicit selective-field qualification for this post-sync class. |
| BFX-037 | **Not applied** | The `ProductionProgramBinding` class omits the program checksum, binding checksum, supported-option snapshot, retirement, soft-delete, and audit fields requested by the checklist. The existing generic readability note is insufficient because the post-sync class is presented as the new evidence-bearing design. |
| BFX-039 | **Not applied** | `logical_database_design.md` still gives `ExecutionRouteRobotBinding` an `[Unclear]` identifier and only `ExecutionRouteId`, `RobotProgramId`, despite the following prose referring to optional Production Program Binding and capability snapshots. |

## 4. SRS and RTM Alignment

### Major finding — NFR-013 conflicts directly

SRS NFR-013 correctly uses optional bounded-cache language and retains exact TTL/invalidation/profile/alert threshold as `[Needs Review]`. RTM NFR-013 still fixes the behavior at 15 seconds, maps only pre-sync SC-08 evidence, and labels the row wholly `Supported`. This violates the weakest-component status rule and leaves the two requirement baselines inconsistent.

### Major finding — entity coverage is internally inconsistent

RTM DR-04 and DR-10 include `InventorySensorObservation` and `ProductionProgramBinding`, but SRS §6.2 omits both from the authoritative entity-group list. FR-134 and FR-135 therefore do not reconcile with the SRS data-requirement catalogue.

### Major finding — Production Configuration evidence is incomplete

FR-102 and FR-105–FR-107 contain post-sync behavior in the SRS, but their evidence remains the old PC rows. The RTM likewise leaves FR-105–FR-107 on pre-sync evidence and does not expose the new independently testable reason/audit/concurrency/stale-observation subclaims. FR-102 is better qualified in the RTM, but the family is not fully aligned.

### Alignment that remains intact

- Both SRS and RTM contain FR-134 and FR-135.
- The RTM data rows recognize the two new persisted concepts.
- The SRS and RTM preserve uncertainty around external Edge compatibility and observation operations in several notes, although FR-120/FR-134 status granularity remains a separate human-input checklist issue.

## 5. UML Alignment with SRS and Evidence

### Major findings

1. Runtime-menu sequence behavior contradicts SRS NFR-013 by asserting a fixed 15-second cache and omitting fallback/admission ordering.
2. Production Program Binding optionality is incorrect in the ERD.
3. ProductionIncident cardinality is incorrect in both ERD and class diagram.
4. Menu ownership/scope is incorrect in the ERD because nullable Kiosk scope is drawn as mandatory.
5. The two post-sync class definitions remain materially incomplete; `InventorySensorObservation.DerivedEstimatedQuantity` is incorrectly non-nullable.

### Supported alignment

- UML documents include `InventorySensorObservation` and `ProductionProgramBinding` and retain the no-stock-movement/no-consumption and non-certification boundaries.
- Inventory observation relationships to dispenser and execution endpoint are represented.
- The class diagram retains an external Edge schema-v5 rollout `[Needs Review]` qualification.

The diagrams should therefore be treated as a hybrid post-sync draft, not an authoritative schema view.

## 6. Database-Design Alignment with `database_inventory.md`

### Major finding — logical route-binding definition is self-contradictory

The logical entity table says `ExecutionRouteRobotBinding` has an unclear identifier and only the two pre-sync references. Immediately following prose says it may reference a Production Program Binding and snapshot its checksum/capability array. BFX-039 remains open.

### Evidence limitation

The current `database_inventory.md` now has corrected 100/8/101 static counts and the three post-sync migration summaries, but its full entity/attribute/relationship/index sections remain only partially synchronized under the separate evidence checklist. Consequently, matching a database-design statement to that inventory is not sufficient by itself to prove complete agreement with the current EF model.

### Supported alignment

- Logical design includes `InventorySensorObservation` with optional derived quantity, unique source executor/event identity, and observation relationships.
- Logical design includes `ProductionProgramBinding` with major ownership, checksum, lifecycle, and capability-evidence concepts.
- Physical design uses the current static counts and lists both new tables and migrations, while retaining live-schema qualification.

Complete physical specifications for the new tables and route-binding migration remain intentionally unresolved by BFX-040–BFX-042, which are marked unsafe for automatic repair.

## 7. Skipped Fix and Human-Input Tracking

### Tracked adequately or partially

- BFX-019 / BFX-033 actor authorization uncertainty is explicitly represented by `open_questions.md` RP-08.
- Production-package uncertainty from BFX-015/BFX-016 is partially represented by PQ-06, RI-06, and RI-07.
- Sensitive notification data concerns related to BFX-003–BFX-005 are partially represented by RP-06 and DO-02.
- Edge rollout and conflicting evidence concerns related to BFX-023–BFX-026 are partially represented by RI-01, RI-02, RI-03, RI-09, and RI-10.
- Tenant-integrity uncertainty related to BFX-043 is represented generally by DB-04 and DB-15.

### Major tracking gap

The open-question register does not map the human-input checklist items explicitly enough to establish that every skipped item was moved and remains owned. In particular, there is no direct question for:

- the approved notification safe-read versus diagnostics/manage policy and response boundary (BFX-003–BFX-005);
- the focused current Production Package contract comparison and affected requirement disposition (BFX-015/BFX-016);
- the Cloud-only versus external-Edge subclaim/status split for FR-120 (BFX-023/BFX-024);
- the normative boundary of FR-134 versus replay/dead-letter/retention/diagnostics requirements (BFX-025/BFX-026);
- the decision whether session/notification behavioral UML is required or explicitly omitted (BFX-035);
- the authoritative Production Program Binding tenant catalogue and structural-versus-application enforcement boundary (BFX-043).

Existing broad questions are related, but they do not replace explicit checklist traceability. Therefore checks 5 and 6 are only partially satisfied.

## 8. Unsupported or Overstated Claims

1. **Fixed runtime-menu cache duration** — RTM and order sequence assert 15 seconds although the synchronized SRS keeps the exact TTL unresolved. This is unsupported in the approved baseline and directly contradicts its uncertainty label.
2. **Production Configuration `Supported` evidence strength** — FR-102 and FR-105–FR-107 describe new concurrency/audit behavior while citing only old functional-inventory rows. The behavior may be supported by current source, but the baseline evidence mapping requested by BFX-012 is absent; the present document-level support claim is therefore under-evidenced.
3. **UML cardinalities** — mandatory Production Program Binding, zero-or-one ProductionIncident, and mandatory Kiosk-owned Menu relationships overstate or contradict the persistence evidence.
4. **Inventory observation nullability** — non-nullable `DerivedEstimatedQuantity` contradicts the logical design's optional quantity and the backend-update evidence.
5. **Diagram evidence notes** — `class_diagram.md` says no source files were opened and relies on `database_inventory.md`, while the diagram includes post-sync fields not yet fully represented in that inventory. The impact report partially bridges this gap, but the note should not imply complete inventory-derived coverage.

No unsupported physical robot-success claim was identified in the reviewed post-sync additions; the non-certification and no-independent-physical-proof boundaries are generally preserved.

## 9. Recommended Correction Order

1. Apply BFX-002 so the SRS entity catalogue matches FR-134/FR-135 and RTM DR-04/DR-10.
2. Apply BFX-006 and BFX-007 together to remove the fixed cache contradiction across RTM and sequence UML.
3. Apply BFX-029–BFX-032 to correct ERD/class cardinalities.
4. Apply BFX-036 and BFX-037 to correct the post-sync class specifications.
5. Apply BFX-039 to reconcile the logical `ExecutionRouteRobotBinding` row with its own relationship prose.
6. Complete BFX-012 evidence mappings for FR-102 and FR-105–FR-107.
7. Add explicit open-question entries or checklist-to-question mappings for every Needs Human Input item listed in §7.
8. Re-run SRS↔RTM, UML↔logical design, and logical/physical design↔evidence consistency checks before promoting the baseline.

## 10. Review Conclusion

The baseline fixes are **not ready for approval as complete**. The new backend concepts are visible across the documentation system, but the checklist-approved Critical/Major corrections remain mostly unapplied. The principal blockers are the SRS/RTM cache contradiction, missing SRS entity-group entries, incorrect UML cardinalities/nullability, incomplete Production Configuration evidence mapping, and the stale logical route-binding row.

This review file records findings only and does not modify the reviewed baseline deliverables.
