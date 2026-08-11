# Codex Review — Backend Update Evidence After Fixes

## Review scope

Reviewed only the evidence-fix result represented by:

- `backend_update_evidence_fix_checklist.md`
- `backend_update_impact_2026-08-11.md`
- `repo_truth_map.md`
- `functional_inventory.md`
- `database_inventory.md`
- `open_questions.md`

The review compares the current files with the 45 checklist actions. No source, build, test, migration, pipeline, live schema, external integration, or physical Edge/robot execution was run. “Applied” therefore means the documentation edit is present and consistent with the cited static evidence; it does not mean runtime verification succeeded.

Severity used in this review:

- **Critical** — the core evidence baseline is still materially unreliable.
- **Major** — a significant backend-sync capability or contract remains missing, contradicted, or untracked.
- **Minor** — a qualification, citation, terminology, or maintenance problem remains.

## Overall result

**Disposition: Partial fix set; not ready to be treated as a synchronized evidence baseline.**

The narrow safe edits were generally applied correctly: organization-owned account route shapes, owned-session operations, current access `permissionCodes`, raw-Lua import/listing/concurrency, selected truth-map summaries, and database migration/count corrections are now present.

The three Critical whole-file reconciliation actions, EFX-001–EFX-003, were not completed. Most Major `Needs Human Input` actions were also not applied, and most were not added to `open_questions.md`. The result is a hybrid baseline: new summaries coexist with obsolete detailed rows and catalogues. No clearly invented new feature was found, but several retained or newly recomposed claims are too strong or internally contradicted.

## 1. Critical/Major safe-fix verification

The table below covers every Critical or Major item whose checklist value is `Safe to Auto-Fix? = Yes`.

| Fix ID | Result | Evidence in current files | Review comment |
|---|---|---|---|
| EFX-004 | **Applied for route shape** | `functional_inventory.md` IDN-15 and IDN-17–IDN-25 now use organization-owned account routes. | The mechanical route correction is correct. Actor cells remain deliberately deferred to EFX-005, so these rows are not fully corrected requirements. |
| EFX-006 | **Applied** | IDN-05a and IDN-05b add `GET /api/v1/me/sessions` and owned `DELETE /api/v1/me/sessions/{sessionId}`; Identity and total counts were updated. | The operations, ownership boundary, and static evidence references are consistent with the impact report. Runtime success is not established. |
| EFX-008 | **Applied** | IDN-11 includes `permissionCodes`; IDN-26 uses assignable-role options; IDN-27 uses `permission-matrix.view` and SystemAdmin. | The former general roles-catalog/`roles.view` wording was replaced. Exact account actor rules remain open under EFX-005. |
| EFX-010 | **Applied at summary level** | `repo_truth_map.md` adds the inventory-observation flow and its no-stock-movement/no-consumption/no-sellability boundary. | The summary is consistent with the impact report, but the owning functional/database inventories remain absent, so this is not end-to-end evidence consistency. |
| EFX-013 | **Applied at summary level** | `repo_truth_map.md` adds Production Program Binding to flows, database summary, controller inventory, and Production Configuration responsibilities. | The physical/entity catalogue and functional/API rows were not added, leaving the summary ahead of its detailed evidence. |
| EFX-017 | **Applied** | `functional_inventory.md` RC-10 now identifies `expectedLastModifiedAt`; RC-21 adds raw-Lua import; RC-22 adds import listing. | The additions are appropriately bounded and do not claim physical/Lua certification. Existing RC/package declaration semantics still require the broader EFX-016 audit. |
| EFX-036 | **Applied** | `database_inventory.md` now states 100 DbSet declarations, eight non-designer migrations, and 101 cumulative `CreateTable` calls, explicitly distinguishing them from live-table counts. | This is the strongest completed database correction. |
| EFX-037 | **Applied at migration-summary level** | The three post-sync migration identifiers and a concise description of their effects were added to §7. | The migration list is correct as static evidence, but the entity/attribute/FK/index/JSON catalogues were not updated, so the database inventory remains materially incomplete. |

## 2. Critical fixes not completed

### AFX-01 — Critical — EFX-001 repo truth map regeneration was not completed

The truth map received selective additions, but it was not re-audited as a whole:

- it contains no concrete `/me/sessions`, assignable-role-options, `permissionCodes`, schema-v5, capability-array, raw-Lua concurrency, notification-policy, assignee-options, verified-unmatched metric, or CI/CD route details;
- §9 still says controller contents were not opened and source folders were only directory-listed, while new text cites “current management controller/feature/tests” and “current MQTT consumer and observation handler”; and
- §10 still says exact DTOs and much of the permission matrix were not inspected.

**Required disposition:** Keep EFX-001 open. The file is a selective delta overlay, not a regenerated truth map.

### AFX-02 — Critical — EFX-002 functional inventory regeneration was not completed

Only Identity and two Robot Configuration additions were materially updated. Major post-sync areas remain absent:

- no `InventorySensorObservation` or `inventory-observations` functional row;
- no `ProductionProgramBinding` functional/API row;
- no schema-v5 or `RequiredCapabilityCodes` dispatch description;
- no runtime-menu cache/fallback/metric rows;
- no maintenance assignee-options row;
- no verified-unmatched callback/metric row;
- no complete notification safe-view/manage split;
- no post-sync release/deployment contract rewrite; and
- no focused Production Package disposition.

The old MQTT-02 row still says “all 6 message types,” contradicting the truth map and impact report that add `inventory-observations` as another uplink family.

**Required disposition:** Keep EFX-002 open. The inventory cannot yet be used as the current functional source of truth.

### AFX-03 — Critical — EFX-003 database inventory reconciliation was not completed

The counts and migration summary changed, but the owning catalogues remain pre-sync:

- the Inventory entity list omits `InventorySensorObservation`;
- the Production Configuration entity list omits `ProductionProgramBinding`;
- attribute, relationship, constraint/index, JSON-role, soft-delete, and multi-tenancy sections omit both new concepts;
- `ExecutionRouteRobotBinding.RequiredCapabilityCodesJson`, optional Production Program Binding FK, and binding checksum are absent; and
- the only occurrences of the new concepts are migration names/summary prose.

This directly conflicts with the updated truth map, which says both entities are post-sync additions.

**Required disposition:** Keep EFX-003, EFX-011, EFX-014, and EFX-015 open. Counts alone do not synchronize a database inventory.

## 3. Skipped-fix tracking

### AFX-04 — Major — The checklist still tracks skipped fixes, but no completion state exists

The checklist remains present and therefore preserves all 45 actions. However, it has no `Status`, `Disposition`, `Applied Evidence`, or `Open Question ID` column. A reader cannot distinguish:

- applied fixes;
- partially applied fixes;
- intentionally deferred fixes;
- fixes waiting for human input; and
- fixes overlooked during the pass.

The evidence files occasionally refer to EFX-003 or EFX-005, but this is inconsistent and does not substitute for checklist disposition tracking.

**Review comment:** Skipped work is not lost, but it is not operationally tracked well enough for closure. At minimum, future tracking needs a completion/disposition and linked question/evidence reference for every non-applied row.

### Critical/Major checklist disposition summary

| Category | Fix IDs | Current disposition |
|---|---|---|
| Applied safe fixes | EFX-004, EFX-006, EFX-008, EFX-010, EFX-013, EFX-017, EFX-036, EFX-037 | Applied or applied at summary level as qualified above |
| Critical whole-file work | EFX-001–EFX-003 | Not completed; selectively patched only |
| Account actor audit | EFX-005, EFX-007 | Deferred; RP-08 added |
| Inventory observation detail | EFX-009, EFX-011 | Not applied; no dedicated open-question mapping |
| Production Program Binding detail | EFX-012, EFX-014, EFX-015 | Not applied; no dedicated open-question mapping |
| Robot declaration/boundary audit | EFX-016, EFX-018 | Not applied; not explicitly mapped to an open question |
| Edge schema/capability update | EFX-019, EFX-020 | Not applied; truth map only carries a generic compatibility qualifier |
| Runtime-menu cache evidence | EFX-021, EFX-022 | Not applied; no dedicated open-question mapping |
| Release/deployment/package changes | EFX-023, EFX-024 | Not applied; RI-06 only partially covers recovery questions |
| Notification privacy/authorization | EFX-026 | Not applied; RP-06 only partially covers sensitive data, not the exact policy/DTO contract |

## 4. Needs Human Input versus `open_questions.md`

### AFX-05 — Major — Only the account actor matrix was explicitly moved

`open_questions.md` adds RP-08 for EFX-005. This is a good, actionable question with current source owners and prevents the old Manager/SystemAdmin actor labels from being silently treated as confirmed.

AB-06 was also updated for the 264-versus-269 inventory-row discrepancy. Existing questions provide partial background coverage for some other fixes:

- RP-06 covers sensitive-data exposure generally;
- RI-03 covers dead-letter replay generally;
- RI-06 covers deployment/package recovery generally;
- DB-01/DB-16 cover count/model reconciliation;
- DB-08 covers JSON versioning;
- DB-12 covers retention; and
- AB-08/RP-07 cover bootstrap concerns.

These are not explicit disposition links for most `Needs Human Input` checklist rows.

### AFX-06 — Major — Human-input items missing dedicated or explicit mappings

The following checklist decisions are not clearly moved to `open_questions.md` with their EFX identifiers and required decision:

- EFX-009: inventory-observation ingestion/recovery contract;
- EFX-011: observation retention/index/physical mapping confirmation;
- EFX-012, EFX-014, EFX-015: Production Program Binding API/entity/snapshot semantics;
- EFX-016 and EFX-018: optional declaration versus certification and advanced import boundary audit;
- EFX-019 and EFX-020: schema-v5 external Edge rollout compatibility and capability-array contract;
- EFX-021, EFX-022, and EFX-041: exact runtime-menu cache stack/configuration/TTL/invalidation/metrics;
- EFX-023: release revision, reason, audit, rollback-observation, and partial-failure rules;
- EFX-024: exact Production Package public-contract impact;
- EFX-026: `notifications.view`/`notifications.manage` actor and safe/diagnostic DTO boundary;
- EFX-038 and EFX-039: new tenant-constraint and post-sync database-question disposition; and
- EFX-044: `inventory-observations`-specific dead-letter/replay/operator recovery.

**Review comment:** Generic older questions may be cross-linked, but each skipped human-input fix needs an explicit question or an explicit “covered by” mapping. At present, the requirement to move human-input items was only partially satisfied.

## 5. Unsupported, overconfident, or contradictory claims

### AFX-07 — Major — Known-stale account actors remain in Implemented rows

IDN-18, IDN-19, IDN-23, IDN-24, and IDN-25 still list Manager; IDN-15, IDN-17, and IDN-20–IDN-22 still list SystemAdmin-only actors. Several rows say actor correction is deferred to EFX-005, yet their Status remains `Implemented` under a definition of “working code wired end-to-end.”

This is not an invented feature, but it is an internally contradictory evidence claim. A known-unverified actor cell cannot coexist cleanly with an unqualified row-level `Implemented` status.

**Required qualification:** Mark the affected actor/policy subclaim `[Needs Team Review]` or use a compound status until RP-08 is resolved.

### AFX-08 — Major — Functional status semantics remain too strong

EFX-032 was not applied. `functional_inventory.md` still defines `Implemented` as “working code wired end-to-end.” The current review did not execute code, providers, migrations, CI, or Edge runtimes. Static inspection supports “implementation and wiring found in source,” not working runtime behavior.

This overconfidence affects both old and newly added rows, including sessions and raw-Lua operations.

### AFX-09 — Major — MQTT evidence contradicts itself

The truth map states that current source consumes typed MQTT uplinks including `inventory-observations`, while functional MQTT-02 still says it subscribes to “all 6 message types” and maps only to the old IOT-01–IOT-09 family. The new message has no functional row.

**Required correction:** Replace the old count/family statement and add the owning inventory-observation row before treating either file as current.

### AFX-10 — Major — Database evidence contradicts the truth map

The truth map names `InventorySensorObservation` and `ProductionProgramBinding` as post-sync entities. The database inventory's actual entity/table catalogues omit them and only mention their migrations. This is a direct cross-file mismatch, not merely an omitted detail.

### AFX-11 — Minor — Migration default claim lacks the requested two-part citation

The database inventory says existing binding rows use the migration's missing-evidence default, but it does not cite both `CapabilityEvidenceStatus = 1` in the migration and `Missing = 1` in the enum. The impact checklist explicitly identified this proof requirement as EFX-042.

The claim is supported by the earlier review, but its refreshed evidence citation remains incomplete.

### AFX-12 — Major — Truth-map evidence methodology conflicts with its new claims

The truth map now cites current handlers, controllers, features, and tests for new flows, but §9 still says controller contents were not opened and `src/Domain`, `src/Application`, and `src/Infrastructure` were directory-listed only. It also retains open questions saying exact controller/permission content was not inspected.

This does not prove the new claims false, but it makes the file's stated evidence method unreliable and violates EFX-001/EFX-034's requested re-audit/citation refresh.

### AFX-13 — Major — The old checkout flow still overstates physical execution

`repo_truth_map.md` §5 still says Edge “executes robot program” and Cloud “finalizes `Order=Completed`” without the qualification now added elsewhere. The updated §8 correctly says Cloud projections are built from accepted executor evidence and do not independently certify physical output.

The same file therefore contains both an overcompressed physical-sounding claim and its necessary qualification. EFX-035 remains unapplied in the owning flow text.

### AFX-14 — Minor — Capability totals remain deliberately inconsistent

The functional summary says 269 rows, while AB-06 records 264 identifiable rows and a retained five-row overcount. The discrepancy is now transparently tracked, so it is not hidden, but the inventory itself still publishes a total known not to be an authoritative row count.

**Required qualification:** Until corrected, downstream documents should not cite 269 as a verified number of distinct capabilities.

## 6. Evidence-file consistency assessment

| Evidence area | Consistency result | Reason |
|---|---|---|
| Account route ownership | **Partial** | Route shapes are current; actor/policy cells remain knowingly stale. |
| Sessions | **Consistent at static-source level** | Functional rows and impact report align; truth-map detail remains sparse. |
| Assignable roles and permission codes | **Mostly consistent** | Functional inventory updated; truth map does not carry the concrete operations. |
| Inventory observations | **Inconsistent** | Impact/truth map include the feature; functional and database catalogues omit it. |
| Production Program Binding | **Inconsistent** | Impact/truth map include it; functional and database catalogues omit its owning rows. |
| Robot raw-Lua/list/concurrency | **Partial** | Functional rows updated; truth map and broader declaration/package semantics remain stale. |
| Edge schema v5/capability arrays | **Not synchronized** | Present in impact report only; detailed inventories remain old. |
| Runtime-menu cache | **Not synchronized** | Present in impact report only; functional/truth inventories omit the contract. |
| Operations notification/assignee changes | **Not synchronized** | Old rows remain; exact policy/privacy and assignee option changes are absent. |
| Verified-unmatched PayOS callback | **Not synchronized** | Present in impact report only; core inventories omit the alternative flow/metric. |
| Release/deployment/package changes | **Not synchronized** | Impact report identifies them; functional rows were not re-baselined. |
| Database counts/migrations | **Consistent but incomplete** | 100/8/101 qualification is correct; new table/entity details are absent. |
| Bootstrap/CI/CD | **Not synchronized** | Impact report contains the evidence; truth/functional inventories do not route it. |
| Open-question traceability | **Partial** | RP-08 and AB-06 updated; most Needs Human Input fixes lack explicit mappings. |

## 7. Applied-fix acceptance and remaining disposition

### Accepted as correctly applied

- EFX-004 route-shape portion;
- EFX-006 session rows;
- EFX-008 assignable-role/permission-code contract;
- EFX-010 inventory-observation truth-map summary;
- EFX-013 Production Program Binding truth-map summary;
- EFX-017 raw-Lua/import-list/concurrency additions;
- EFX-036 static database counts and live-count qualification; and
- EFX-037 migration identifiers and summary-level semantics.

### Not accepted as complete

- EFX-001–EFX-003 whole-file synchronization;
- all Major owning rows for inventory observations, Production Program Bindings, schema v5, runtime-menu cache, notification privacy, deployment, and package changes;
- full human-input-to-open-question migration; and
- cross-file consistency.

## Final disposition

**Needs further evidence repair.** The applied safe changes should be retained, but the evidence package must continue to be labeled a partial post-sync update. Downstream writers should use `backend_update_impact_2026-08-11.md` as a delta bridge and must not treat the three core inventories as fully current until the remaining Critical/Major checklist items are completed or explicitly deferred with open-question mappings.
