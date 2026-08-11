# Codex Review — Backend Update School Reports After Fixes

## Review scope

Reviewed only the school-report fix result represented by:

- `backend_update_school_reports_fix_checklist.md`
- `backend_update_impact_2026-08-11.md` and the current evidence files
- Reports 1, 3, 4, 5, 6, and 7
- `open_questions.md`

No source, baseline deliverable, report, build, test, migration, pipeline, deployment, live schema, external integration, or physical Edge/robot execution was modified or run. “Applied” below means the report edit is present and consistent with the cited static evidence; it does not mean runtime verification succeeded.

Severity:

- **Critical** — blocks treating the school-report package as a coherent submission baseline.
- **Major** — material requirement, design, test, workflow, consolidation, or traceability problem.
- **Minor** — qualification, mapping, citation, or maintenance issue.

## Overall result

**Disposition: The applied safe fixes are mostly correct, but the school-report package remains only partially synchronized.**

Five Critical/Major safe fixes were applied:

- Report 3 NFR-013 now uses the optional bounded-cache contract.
- Report 4 now exposes upstream ERD/logical-design blockers instead of calling the full ERD complete.
- Report 5 adds planned cases for maintenance assignee revalidation and robot-program concurrency/import listing.
- Report 7 blocks consolidation of the obsolete cache requirement.

The university heading structure and protected PMP/team/schedule/test-result placeholders remain intact. Reports 1 and 6 were not changed in this pass. Major notification, detailed-design, physical-database, test-matrix, installation/workflow, and Report 7 consolidation work remains outstanding.

Two new inconsistencies require correction before accepting the applied test changes as fully traceable:

1. Report 1 still cites the old 260/265 functional-inventory counts, while the current tracked discrepancy is 264/269.
2. TC-ROBOT-002 maps import listing to FR-100, but Report 3 FR-100 currently covers create-release-draft and get import/workspace—not the organization import-list operation.

## 1. Critical/Major safe-fix verification

This table covers each checklist row with severity Critical/Major and `Safe to Auto-Fix? = Yes`.

| Fix ID | Result | Current evidence | Review comment |
|---|---|---|---|
| SFX-002 | **Applied correctly** | Report 3 NFR-013 now states optional bounded cache, admission-before-cache, request metadata separation, database fallback, checkout revalidation, and unresolved configuration/thresholds. | Consistent with the updated baseline SRS and impact report. No fixed duration or runtime-performance result is claimed. |
| SFX-017 | **Applied with one governance concern** | Report 4 now labels its compact ERD as the corrected summary and lists full-ERD/logical-design blockers. | The cardinality qualifications are correct. Saying the compact school-report view “takes precedence” should be limited to Report 4 presentation; it should not replace correction of the owning baseline UML/database artifacts. |
| SFX-021 | **Applied correctly** | Report 5 adds TC-OPS-002 for active role/scope eligibility and mutation-time revalidation. | The case is `[Planned]`, maps to FR-082–FR-083, and stays within supported backend behavior without inventing UI steps. |
| SFX-022 | **Partially correct** | Report 5 adds TC-ROBOT-002 for `expectedLastModifiedAt`, raw-import atomicity, and organization-scoped import listing. | The planned behavior is supported, but the FR mapping is incomplete: Report 3 FR-100 does not list the organization import-list operation. Correct the owning requirement or map the case to an approved requirement/subclaim before accepting traceability. |
| SFX-038 | **Applied correctly** | Report 7 adds an explicit NFR-013 consolidation blocker and preserves unresolved TTL/profile/thresholds. | The blocker accurately prevents reintroduction of the fixed cache duration. It remains necessary because Report 5 has not yet expanded every requested cache variant. |

## 2. Reports 1, 3, 4, 5, 6, and 7 consistency

### AF-SR-01 — Major — Report 1 capability counts are stale

**Location:** Report 1, line 154.

Report 1 still says there are 260 identifiable rows and a 265-row summary. The updated functional inventory now reports 269, while `open_questions.md` AB-06 records 264 identifiable rows and the same five-row overcount.

This does not affect the 135 SRS requirement count, but it makes Report 1 inconsistent with the current evidence package.

**Required correction:** Use the current 264/269 qualified discrepancy or avoid quoting either capability count until AB-06 is resolved. Do not present 269 as 269 distinct verified capabilities.

### AF-SR-02 — Major — Report 3 and TC-ROBOT-002 do not agree on import-listing traceability

Report 5 TC-ROBOT-002 maps robot-program concurrency and import listing to FR-097 and FR-100. FR-097 supports `expectedLastModifiedAt` and raw-Lua import. Report 3 FR-100 lists only:

- create configuration release draft from import; and
- get import/workspace.

It does not list `GET /api/v1/management/organizations/{organizationId}/robot-authoring-imports`.

**Required correction:** Add the list operation to the appropriate Report 3 requirement/evidence and RTM/STM mapping, or assign the test variant to another approved requirement. A test case must not become the only normative source for a feature.

### AF-SR-03 — Major — Cache semantics are improved but Report 5 is not fully aligned yet

Report 3, Report 6, and the Report 7 blocker now agree on optional caching, admission ordering, database fallback, ETag/revision behavior, and unresolved operational values. Report 5 TC-SC-001 covers most of that behavior, but TC-NFR-001 remains broad and does not explicitly enumerate disabled-cache and request-metadata variants.

**Disposition:** Cross-report cache wording is no longer contradictory, but SFX-026 remains incomplete. The Report 7 blocker correctly stays in place.

### AF-SR-04 — Major — Notification authorization/privacy remains inconsistent across Reports 3–7

No owning report was corrected for the `notifications.view`, diagnostic-data, and `notifications.manage` split:

- Report 3 FR-086 retains the old compound diagnostics/requeue model.
- Report 4 §3.5 retains a generic authorized-diagnostics description.
- Report 5 still has only broad TC-OPS-001/TC-JOB-001 coverage.
- Report 6 does not explain safe reads, privileged diagnostics, or requeue authorization.
- Report 7 does not summarize the updated policy/privacy boundary.

RP-09 now tracks the decision, which is good, but the reports remain inconsistent with the backend impact until it is resolved and propagated.

### AF-SR-05 — Major — Reports 4 and 7 still summarize design that is not complete in its owning artifacts

Report 4 now responsibly lists upstream ERD/logical blockers. However, it still lacks focused observation and Production Program Binding/deployment sequences and complete physical table specifications. Report 7 continues to summarize those areas and correctly marks consolidation required.

**Disposition:** The summary is acceptable only as qualified navigation. It is not evidence that SFX-012–SFX-016 or the upstream baseline fixes are complete.

### AF-SR-06 — Major — Report 6 remains behind the applied Report 3/5 changes

Report 6 was unchanged. It still omits:

- maintenance assignee-options workflow detail;
- safe notification view versus diagnostic/manage behavior;
- assignable-role/`permissionCodes` client guidance;
- `expectedLastModifiedAt` and stale revision recovery;
- .NET 10 and NetBird/SSH workflow evidence; and
- the complete 100/8/101 manifest reconciliation wording.

Existing physical/UI/environment qualifications remain safe, but SFX-030–SFX-034 are still open.

### AF-SR-07 — Minor — Report 5 case catalogue changed without an explicit case-count revision

Two planned cases were added. Report 5 wisely does not publish a fixed total case count and Report 6 says the count/version must come from an approved manifest. This avoids a false number, but the Record of Changes and future STM/workbook must capture the additions before execution.

## 3. Report 7 summary accuracy

### AF-SR-08 — Major — Report 7 remains a scaffold and does not yet consolidate the new cases

Report 7 correctly:

- states FR-001–FR-135;
- preserves Inventory Sensor Observation and Production Program Binding summaries;
- qualifies 100 DbSets, eight migrations, and 101 creation calls;
- describes tests as planned;
- adds the NFR-013 blocker; and
- preserves the physical-outcome boundary.

It does not yet import TC-ROBOT-002 or TC-OPS-002, and it still contains `[Consolidation Required]` markers across Parts III–VI. That is safe and transparent, but it means Report 7 does not yet summarize the current Report 5 catalogue losslessly.

### AF-SR-09 — Major — Report 7 appropriately defers, but its owner-report statements remain ahead of Reports 3–6

Report 7 mentions verified-unmatched callbacks, deployment concurrency, raw-Lua import, observation ordering, and Production Program Binding integrity. Several exact evidence/status details remain incomplete in the owning reports.

**Required disposition:** Retain the qualifications and consolidation markers. Do not expand Report 7 further until Reports 3–6 and the baseline artifacts are corrected.

### AF-SR-10 — Minor — Report 7 test summary remains accurate but incomplete

The test summary says Report 5 covers maintenance, robot configuration, and concurrency at a high level, so the two new cases do not make the summary false. It remains incomplete as a final consolidation because it does not preserve the new case IDs and exact planned variants.

## 4. University template structure

### Structure result: Preserved

The applied changes did not remove, rename, or reorder the principal university-template headings:

- Report 1 retains Record of Changes and Project Introduction sections.
- Report 3 retains Product Overview, User Requirements, Functional Requirements, NFRs, and Other Requirements.
- Report 4 retains System Design, Database Design, and Detailed Design.
- Report 5 retains Scope, Strategy, Plan, Test Cases, and Test Reports.
- Report 6 retains Deliverable Package, Installation Guides, and User Manual.
- Report 7 retains Parts I–VII, including the PMP, SRS, SDD, Testing, User Guides, and Appendix.

The additions are contained within existing sections and should remain suitable for later DOCX conversion. No structural regression was found.

## 5. PMP, team, schedule, and test-result placeholder safety

### Placeholder result: Preserved

Report 7 continues to mark the entire Project Management Plan as `[Team-Owned Placeholder]` and does not invent:

- team members or roles;
- supervisor/customer identities;
- schedules, dates, estimates, effort, or completion percentages;
- communication commitments;
- tools actually approved/used by the team; or
- acceptance outcomes.

Report 5 keeps all cases, including TC-ROBOT-002 and TC-OPS-002, as `[Planned]`. Execution statistics, defects, requirement pass coverage, known issues, and sign-off remain `[To Be Updated After Test Execution]`. Report 7 preserves the same test-result placeholders.

No false Passed result, pass rate, defect count, execution date, deployment success, or stakeholder acceptance was introduced.

## 6. Needs Human Input routing to `open_questions.md`

### AF-SR-11 — Major — Key human-decision clusters were moved correctly

The following new questions provide useful consolidated ownership:

| Open question | Checklist coverage | Result |
|---|---|---|
| RP-09 — notification authorization/privacy matrix | SFX-003, SFX-014, SFX-020, notification portion of SFX-030 | Good explicit mapping |
| AB-11 — deployment/release concurrency contract freeze | SFX-007, SFX-023, SFX-032 | Good explicit mapping |
| AB-12 — runtime-menu cache acceptance profile | SFX-002, SFX-026, SFX-038 | Good explicit mapping |
| RI-12 — post-sync Production Package disposition | SFX-010, SFX-025 | Good explicit mapping |
| SR-11 — shared post-sync evidence baseline | SFX-041 | Good explicit mapping |

Existing AB-08/RP-07 also cover bootstrap/security questions relevant to SFX-024, and RP-08 covers the account actor matrix relevant to Report 6 account guidance.

### AF-SR-12 — Minor — Not every Needs Human Input row has an explicit disposition link

The following items remain only indirectly covered:

- SFX-024 development fixture/reset verification is related to AB-08, but AB-08 does not cite SFX-024 or distinguish test-fixture verification from release bootstrap scope.
- SFX-030 maintenance-assignee UI/workflow completion is only partially covered by RP-09, which is about notifications, not assignee presentation.
- SFX-039 Report 7 summary completion has no direct question/disposition; its component decisions are spread across RP-09, AB-11, AB-12, RI-12, AB-08, and SR-11.

**Review comment:** Human-input routing is substantially improved but not complete. Add explicit “covered by” links rather than creating duplicate questions where an existing question already owns the decision.

## 7. Unsupported or overconfident claims introduced

### AF-SR-13 — Major — TC-ROBOT-002 currently overstates requirement traceability

The test behavior itself is supported by post-sync evidence. The unsupported part is the claim that FR-100 currently covers import listing. It does not, based on Report 3's trigger and flow text.

**Required correction:** Repair the requirement mapping before treating TC-ROBOT-002 as traced.

### AF-SR-14 — Minor — Report 4 should not globally supersede owning baseline artifacts

The new Report 4 wording says its compact view “takes precedence” where the full baseline conflicts. This is understandable as a temporary report-local safeguard, but architecture/UML/database baselines remain the owning design artifacts.

**Required qualification:** Say “for interpretation of this Report 4 draft” and retain the upstream correction blocker. Do not establish a school-report excerpt as the global architecture/database source of truth.

### AF-SR-15 — Positive finding — No unsupported runtime outcome was introduced

The new NFR and test rows do not claim:

- a successful cache performance result;
- completed test execution;
- successful migration/deployment/CI execution;
- live provider compatibility;
- external Edge schema-v5 rollout success; or
- physical robot/dispensing/safety success.

The maintenance assignee test expectations and raw-Lua concurrency/atomicity expectations are presented as planned verification, not executed proof.

## 8. Applied-fix acceptance and remaining disposition

### Accepted

- SFX-002 — Report 3 optional cache contract;
- SFX-017 — Report 4 blocker/cardinality qualification, subject to report-local precedence wording;
- SFX-021 — planned maintenance assignee test;
- SFX-038 — Report 7 cache consolidation blocker; and
- SFX-022 — planned robot concurrency/import test behavior, subject to correcting its FR mapping.

### Still open

- SFX-001 and SFX-042 package-wide approval gates;
- Report 3 notification, deployment evidence, package, and stale citation fixes;
- Report 4 detailed observation/binding flows, notification design, and physical database detail;
- Report 5 notification, deployment, package, cache variants, CI/environment routing, and executable STM;
- Report 6 workflow and installation updates;
- Report 7 lossless consolidation and updated-feature summaries; and
- consistent source revision/evidence freshness across Reports 3–7.

## Final disposition

**Needs another report-fix pass before consolidation approval.** The applied safe edits should be retained after correcting TC-ROBOT-002 traceability and narrowing Report 4's precedence wording. Template structure, PMP/team/schedule placeholders, and non-executed test status remain safe. Report 7 must continue to be labelled a consolidation draft until the owning reports and upstream baseline artifacts are corrected.
