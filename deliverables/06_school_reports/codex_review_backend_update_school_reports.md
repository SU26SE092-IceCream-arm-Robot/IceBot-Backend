# Codex Review — Backend Update School Reports

## Review scope and evidence basis

This review covers the post-sync versions of Reports 3–7:

- `report3_srs/report3_srs.md`
- `report4_sdd/report4_sdd.md`
- `report5_test_documentation/report5_test_documentation.md`
- `report6_user_guides/report6_user_guides.md`
- `report7_final_project_report/report7_final_project_report.md`

The comparison basis is the current school-report set, `backend_update_impact_2026-08-11.md`, `codex_review_backend_update_evidence.md`, `codex_review_backend_update_baseline_docs.md`, and the updated baseline SRS/RTM/UML/database-design documents. The older `repo_truth_map.md`, `functional_inventory.md`, and `database_inventory.md` are pre-sync evidence and are not complete evidence for the synchronized backend.

No build, test execution, CI run, deployment, live-schema inspection, provider call, Edge execution, or physical-robot verification was performed. Comments about support refer to static source/document evidence only.

Severity:

- **Critical** — prevents the report set from being a coherent post-sync submission baseline.
- **High** — material requirement, design, test, workflow, or consolidation inconsistency.
- **Medium** — incomplete coverage, stale evidence, or insufficient qualification.
- **Low** — presentation or maintainability concern.

## Overall conclusion

The reports reflect a substantial part of the backend update. Report 3 now reaches FR-135 and includes sessions, organization-owned account administration, inventory observations, raw-Lua import, Production Program Bindings, deployment concurrency, and execute-order schema v5. Report 4 adds the two new persisted concepts and updated configuration/Edge qualifications. Report 5 has planned cases for most important new branches. Report 6 preserves appropriate backend/client/physical boundaries. Report 7 uses the new FR total and database counts and keeps test results unexecuted.

The package is **not yet fully synchronized**. The most important remaining problems are the obsolete fixed 15-second cache requirement, incomplete notification authorization/privacy coverage, post-sync behavior supported only by stale evidence citations, shallow design coverage of the two new major workflows, gaps in the planned tests for changed subclaims, and a final report that summarizes material not yet consistently represented in its owning reports. Report 7 also remains a consolidation scaffold rather than a completed consolidation.

## 1. Report 3 — updated requirements

### SR-01 — High — NFR-013 retains the obsolete fixed 15-second cache claim

**Location:** Report 3, NFR-013, lines 1855–1856.

NFR-013 still requires a 15-second runtime-menu cache and labels it Supported from pre-sync `functional_inventory.md` SC-08. The synchronized baseline instead describes an optional cache, admission checking before cache access, database fallback on cache failure, request metadata outside the cached projection, and unresolved TTL/invalidation/deployment-profile/alert thresholds.

**Review comment:** Replace the fixed-duration statement with the current optional-cache/fallback contract. Keep exact TTL, invalidation, deployment profile, and operational thresholds `[Needs Team Review]`. This stale requirement also affects Report 5 cases TC-SC-001/TC-NFR-001 and Report 7's NFR summary.

### SR-02 — High — FR-086 misses the notification view/manage and safe/diagnostic split

**Location:** Report 3, FR-086, lines 1236–1245.

FR-086 presents list/detail as a diagnostics view and combines it with requeue. The backend update separates `notifications.view` from `notifications.manage`; ordinary reads exclude sensitive message/provider-diagnostic content, while mutation and diagnostic exposure have stronger authorization requirements.

**Review comment:** Split or explicitly distinguish safe list/detail, sensitive diagnostic data, requeue mutation, and background delivery. State the exact policies/actors and DTO boundary. Do not infer a UI screen. Until the controller/policy matrix is carried into the report, the whole compound requirement should not be unqualified Supported.

### SR-03 — Medium — Updated requirements still cite superseded evidence rows

The prose includes new behavior, but its evidence fields frequently cite only the old inventories:

- FR-070 includes the verified-unmatched PayOS branch but cites only PAY-03.
- FR-082 includes `assignee-options` but cites only OPS-08–OPS-10.
- FR-097 includes raw-Lua import and stale-write rejection but cites only RC-10.
- FR-102 and deployment requirements include revision/reason/audit behavior but cite old PC rows.
- FR-120 includes schema-v5 capability arrays but cites only IOT-05.
- FR-126 covers the expanded MQTT family but cites old MQTT-02.

**Review comment:** Add current handler/controller/DTO/test/migration evidence or refreshed inventory IDs. Mark the old inventories as pre-sync where they remain useful background. The presence of updated prose does not make a stale evidence link sufficient traceability.

### SR-04 — Medium — Several changed contracts are described without their identifying evidence details

Report 3 describes raw-Lua stale concurrency input, deployment audit/revision behavior, and bounded unmatched-callback diagnostics, but it does not identify `expectedLastModifiedAt`, the verified-unmatched metric, or the exact deployment reason/client-observed revision fields. These omissions make the new subclaims difficult to test or trace.

**Review comment:** Record the public field/metric names where stable, or mark them `[Needs Review]` if the public contract has not been frozen. Cite `icebot.payment.webhook.verified_unmatched` for the unmatched-callback observability claim and keep alert thresholds/runbook open.

### SR-05 — Medium — Production-package impacts remain implicitly unchanged

The backend impact analysis identifies package-upgrade alignment with Production Configuration as requiring focused comparison. Report 3's FR-111–FR-119 remain largely inherited from the pre-sync evidence and do not show a completed post-sync disposition.

**Review comment:** Compare the current package handlers/contracts and either document the changed behavior or mark affected requirements `[Needs Review]`. Do not imply that the entire package section was verified unchanged.

### SR-06 — Positive finding — New requirement IDs and major safety boundaries are present

FR-134 and FR-135 are present, the requirement total is now 135, inventory observations are not presented as stock movements/consumption proof, and Production Program Bindings/declarations are not presented as certification of Lua behavior or physical safety. Retain these boundaries.

## 2. Report 4 — updated design, database, and flows

### SR-07 — High — New entities are catalogued but their key flows are not designed at sequence level

**Locations:** Report 4 §§2 and 3.3.

`InventorySensorObservation` and `ProductionProgramBinding` now appear in the compact ERD, table catalogue, and design prose. However, no focused sequence/activity design shows:

- inventory observation authentication, validation, duplicate/conflict/stale disposition, optional quantity derivation, projection update, and after-commit notification; or
- Production Program Binding validation/checksum creation, selection during release authoring, snapshot propagation into route bindings, revision conflict, deployment reason/audit, and stale rollback observation.

**Review comment:** Add focused flows or explicitly mark these as omitted detailed-design areas. A paragraph in §3.3 is too shallow for two new cross-module/persistence workflows.

### SR-08 — High — Notification authorization/privacy changes are absent from the design

**Locations:** Report 4 §3.5, especially the notification-delivery rows and lines 654–662.

The design still describes an “authorized diagnostics request” and generic curated/raw separation. It does not model the new `notifications.view` versus `notifications.manage` boundary or explain which list/detail DTO fields are safe and which provider/message fields require diagnostics privilege.

**Review comment:** Update the component/workflow description and authorization boundary. Report 4 must not inherit Report 3's overbroad diagnostics wording.

### SR-09 — High — Database coverage of the new tables is summary-level, not physical-design level

**Locations:** Report 4 lines 256, 283–284 and §2.4 references.

The table catalogue names both new tables and correctly qualifies their business meaning, but it does not expose the material physical constraints needed by the SDD: observation idempotency key/disposition/sequence/timestamps/optional derived quantity and restrictive FKs; binding organization/product/recipe/program FKs, lifecycle, checksum/evidence fields, JSON snapshots, and soft-delete/audit behavior; and the optional binding FK plus capability-array snapshot on execution-route robot bindings.

**Review comment:** Either add an evidence-qualified physical summary or clearly route each detail to a corrected physical database design. The current baseline physical design itself remains incomplete, so a generic cross-reference is not sufficient closure.

### SR-10 — Medium — Report 4 inherits unresolved baseline cardinality and completeness risks

The compact SDD ERD improves Menu/Kiosk optionality and shows zero-to-many Production Incidents, but it delegates exact cardinality to baseline UML/database documents that remain inconsistent. The full ERD still overstates the optional Production Program Binding relationship; the class/full ERD disagree on ProductionIncident; the logical route-binding row is stale.

**Review comment:** State that the compact view is the current corrected summary and list the delegated baseline conflicts as open blockers. Do not describe the external diagrams/designs as “complete” until those conflicts are resolved.

### SR-11 — Medium — Cache design is present but operational semantics are not consistently anchored

Report 4 describes optional cache behavior, admission-before-cache, fallback, and bounded operation more accurately than Report 3. Its evidence chain nevertheless points partly to stale baseline material, and the exact configuration/metrics/invalidation contract remains open.

**Review comment:** Make Report 4's current semantics the design reference, cite post-sync source evidence, and align Report 3/5/7. Keep numerical TTL and alert thresholds unresolved.

### SR-12 — Positive finding — Static database counts and physical-outcome boundaries are responsibly qualified

Report 4 correctly distinguishes 100 `DbSet<T>` declarations, eight non-designer migrations, and 101 cumulative `CreateTable` operations from a live-table count. It also avoids treating reported deployment/package state, declarations, bindings, or Cloud evidence as proof of physical installation, execution, or safety.

## 3. Report 5 — test scope for new and changed features

### SR-13 — High — Broad coverage exists, but several changed subclaims have no explicit test variant

Report 5 adds useful planned coverage for sessions, organization account administration, runtime-menu cache fallback, verified-unmatched PayOS callbacks, inventory observations, raw-Lua import, Production Program Bindings/deployment concurrency, and schema v5. The following changes remain hidden inside broad cases or absent:

- safe notification list/detail versus diagnostic fields and `notifications.view`/`notifications.manage` separation;
- maintenance `assignee-options` scope and eligibility;
- raw-Lua `expectedLastModifiedAt` conflict behavior and import-listing contract;
- exact deployment reason/audit/client-observed revision and stale rollback behavior;
- local seed/bootstrap and robot-authoring reset verification for development environments; and
- current production-package behavior after its alignment with Production Configuration.

**Review comment:** Add explicit variants or separate case IDs. TC-OPS-001 and TC-JOB-001 are too broad to prove the notification and assignee authorization changes; TC-ROBOT-001 and TC-PC-002 need named concurrency/audit assertions.

### SR-14 — High — Cache test cases are tied to an inconsistent requirement

TC-SC-001 correctly tests admission-before-cache and database fallback. TC-NFR-001 only says bounded freshness, while the owning Report 3 NFR still mandates 15 seconds. This prevents an unambiguous pass criterion.

**Review comment:** Correct NFR-013 first, then define variants for cache disabled, hit, miss, expiry, invalidation, failure fallback, ETag/304, and request-specific metadata. Leave exact TTL/profile thresholds pending until approved.

### SR-15 — Medium — CI/CD update evidence is not routed into the test plan

The synchronized source includes .NET 10 PR validation and GHCR publication workflows. Report 5 appropriately does not claim that they ran, but its supporting-tools/environment sections do not record these workflows as available static automation evidence. NetBird/SSH deployment is also relevant to deployment verification, not proof of a successful environment.

**Review comment:** Identify the current workflow files, SDK/image assumptions, triggers, and expected artifacts. Keep secrets, runner environment, approval, run URL, result, and deployment success `[Needs Team Review]` / `[To Be Updated After Test Execution]`.

### SR-16 — Medium — Requirement ranges are not yet an executable STM

The scope register and case table map broad requirement ranges to planned cases, but they do not enumerate every changed subclaim, source test, fixture, environment, or result. Report 5 itself acknowledges that a detailed STM/workbook is still required.

**Review comment:** Before execution, expand the backend-sync requirements into stable requirement/subclaim → test ID → level/type → environment → result/evidence mappings. Do not convert source-test existence into Passed status.

### SR-17 — Positive finding — Execution status and external boundaries are handled correctly

All case rows remain `[Planned]`; report statistics and sign-off remain `[To Be Updated After Test Execution]`. Provider, Edge, client, and physical-robot testing is separated from backend contract testing and is not overclaimed.

## 4. Report 6 — installation and user-workflow accuracy

### SR-18 — High — Operations workflows omit changed notification and assignee behavior

**Locations:** Report 6 §§3.6, “Device and Kiosk Monitoring” and “Background Processing and Escalation.”

The guide tells operators to create/update Maintenance Tickets and describes notification delivery jobs, but it does not explain the new assignee-options lookup, the distinction between safe notification reads and sensitive diagnostics, or the separate permission needed to requeue/manage deliveries.

**Review comment:** Add role/contract-oriented steps for selecting eligible assignees and for safe view, diagnostic inspection, and requeue. Keep screen names, labels, navigation, and screenshots `[Needs Team/UI Review]`.

### SR-19 — Medium — Account administration is accurate at a high level but omits user-visible permission metadata

Report 6 correctly uses organization-owned account administration and states that OrgAdmin cannot grant SystemAdmin or cross organization scope. It does not explain assignable-role options and their `permissionCodes` metadata sufficiently for a management-client workflow.

**Review comment:** Add a contract-level step to load assignable roles/options and honor returned permission metadata before mutation. Do not invent frontend control behavior.

### SR-20 — Medium — Robot/release workflow does not identify all concurrency controls

The guide includes raw-Lua import, Production Program Bindings, schema v5, and a physical-outcome warning. It does not identify the raw-import `expectedLastModifiedAt` guard or provide a clear user recovery path for stale release/deployment revisions and stale rollback observations.

**Review comment:** Explain that the client must submit the observed revision/last-modified value and refetch on conflict. Exact UI prompts remain `[Needs Team/UI Review]`.

### SR-21 — Medium — Installation guide only partially routes current CI/CD evidence

Report 6 mentions validation and GHCR workflows and correctly leaves successful-run evidence unresolved. It does not mention the current .NET 10 workflow baseline or NetBird/SSH deployment workflow. The generic runtime/version placeholder is safe, but reviewers cannot tell whether the workflow SDK is a current source fact or an approved production prerequisite.

**Review comment:** Record these as repository workflow evidence, not guaranteed deployment requirements. Keep runner, secrets, target host, approval, network topology, exact commands, and successful execution `[Needs Team Review]`.

### SR-22 — Medium — Database installation checklist omits the migration count in the release reconciliation

The guide correctly warns against treating 100 DbSets or 101 cumulative create operations as a live-table count. It does not state the current eight non-designer migrations alongside the release-manifest reconciliation, although Report 4 and Report 7 do.

**Review comment:** Include the static migration count/revision only as a manifest cross-check; require actual migration IDs/model snapshot and deployed-schema evidence before installation acceptance.

### SR-23 — Positive finding — Installation and physical-system claims remain appropriately bounded

The guide does not invent ports, domains, credentials, commands, client screens, production topology, provider success, or robot behavior. It clearly separates Cloud contracts from Local Edge/robot installation and keeps unresolved values `[Needs Team Review]`.

## 5. Report 7 — consolidation of updated reports

### SR-24 — Critical — Report 7 is still a consolidation scaffold, not a consolidated final report

Report 7 contains numerous `[Consolidation Required]` instructions for importing Report 3 requirements, NFRs, diagrams, Report 4 designs, Report 5 cases, and Report 6 workflows. This is transparent and safer than inventing content, but it does not satisfy the report's stated final-consolidation purpose.

**Review comment:** Keep the markers until approved content is imported, but do not call the document submission-ready. Resolve owning-report defects first, then perform a controlled consolidation without converting placeholders or planned tests into facts.

### SR-25 — High — Report 7 sometimes summarizes behavior more completely than the owning report

Examples:

- Report 7 states the verified-unmatched PayOS behavior, while Report 3 still cites only old PAY-03 evidence and Report 4 mentions it only in a background-job row rather than the payment-flow explanation.
- Report 7 says notification/operations behavior is summarized in Report 4, but Reports 3/4 omit the new view/manage and safe/diagnostic split.
- Report 7 highlights release/deployment concurrency and new test coverage, while the exact field/evidence mapping remains incomplete in Reports 3–5.

**Review comment:** A final report should summarize approved owner sections, not become the only complete statement of a feature. Correct Reports 3–6 first and then copy the qualified result into Report 7.

### SR-26 — High — Report 7 inherits the stale cache requirement through its consolidation dependency

Report 7 avoids repeating “15 seconds,” but it instructs the team to import NFR-001–NFR-025 from Report 3. Unless NFR-013 is corrected first, final consolidation will reintroduce the obsolete requirement.

**Review comment:** Add NFR-013 to the consolidated blocker list and do not import it unchanged.

### SR-27 — Medium — The updated-feature summary is incomplete

Report 7 names sessions, organization accounts, observations, bindings, raw-Lua import, schema v5, PayOS unmatched handling, and deployment concurrency. It does not clearly call out notification view/manage privacy, maintenance assignee options, local seed/bootstrap, production-package disposition, or CI/CD workflow changes.

**Review comment:** Add these to the appropriate SRS/design/test/installation summaries after their owning reports are corrected. Operational/CI items should be described as source/workflow evidence, not completed execution.

### SR-28 — Positive finding — Totals, counts, placeholders, and test-result status are safe

Report 7 uses FR-001–FR-135, identifies both new database entities, qualifies the 100/8/101 counts, keeps the PMP team-owned, and retains all test results as `[To Be Updated After Test Execution]`. It does not invent team, schedule, supervisor, date, provider, deployment, or physical-robot outcomes.

## 6. Cross-report backend-update coverage matrix

| Backend update area | Report 3 | Report 4 | Report 5 | Report 6 | Report 7 | Result |
|---|---|---|---|---|---|---|
| Owned sessions and transactional revocation | Present | Identity summary | Planned cases | User flow | Summarized | Substantial; detailed transaction verification still planned |
| Organization-owned accounts / OrgAdmin restrictions / assignable roles | Present | Present | Planned | High-level only | Summarized | Partial; `permissionCodes` workflow detail weak |
| Inventory Sensor Observations | FR-134 | Entity/ERD/prose | TC-INV-003 | Monitoring guidance | Summarized | Substantial; detailed design sequence/physical schema incomplete |
| Runtime-menu optional cache/fallback/metrics | Stale 15-second NFR | Mostly current | Good fallback case, inconsistent NFR | Current bounded guidance | Generic summary | **Inconsistent** |
| Raw-Lua import/list/concurrency | Present, fields/evidence incomplete | Prose only | Broad planned case | High-level workflow | Summarized | Partial |
| Production Program Binding | FR-135 | Entity/ERD/prose | TC-PC-002 | Authoring guidance | Summarized | Substantial; design/physical details incomplete |
| Execute-order schema v5 capability arrays | Present | Present | Planned compatibility case | Boundary guidance | Summarized | Substantial; external Edge rollout remains open |
| Verified-unmatched PayOS callback | Present, stale citation | Background row | Planned case | Not operator-facing | Summarized | Partial evidence traceability |
| Maintenance assignee options | Trigger only | Not explicit | Hidden in broad operations case | Not explicit | Not explicit | **Insufficient** |
| Notification view/manage and safe/diagnostic reads | Missing | Missing | Hidden in broad case | Missing | Missing | **Not synchronized** |
| Deployment reason/audit/revision/stale rollback | Present generically | Present generically | Broad planned case | Incomplete recovery guidance | Summarized | Partial |
| Production-package alignment | No clear disposition | Generic | Existing broad case | Generic | Generic | **Needs focused comparison** |
| Local seed/bootstrap/reset | Open scope only | Absent | Absent | Absent | Absent | **Not routed** |
| .NET 10 validation / GHCR / NetBird deployment workflows | Not SRS material | Absent | Absent | GHCR only | Generic installation summary | **Partially routed; no execution claim** |
| New entities/migrations/counts | FRs/open count | 100/8/101 and entities | Schema verification planned | 100/101 warning | 100/8/101 | Counts safe; exact physical catalogue incomplete |

## 7. Outdated or unsafe claims to remove or qualify

1. **Report 3 NFR-013:** fixed “15 seconds” and unconditional Supported status.
2. **Report 3 FR-086 / Report 4 §3.5:** notification reads described as a single diagnostics surface without the new safe-view/manage distinction.
3. **Evidence labels across Reports 3–4:** post-sync behavior cited only to pre-sync inventory rows.
4. **Report 4 “complete ERD/design” routing:** delegated baseline documents still contain cardinality and new-table-detail defects.
5. **Report 5 cache pass criteria:** cannot be finalized while Report 3's cache requirement is contradictory.
6. **Report 7 consolidation statements:** cannot imply the owning reports are fully synchronized while the defects above remain.

No outdated FR count, old 98/99 DbSet count, old migration count, executed-test result, or physical-robot success claim was found in the reviewed school reports.

## 8. Recommended correction order

1. Correct Report 3 NFR-013 and FR-086; refresh evidence for all changed FR subclaims.
2. Resolve the baseline UML/database defects that Report 4 delegates to, then expand Report 4's observation/binding and notification designs.
3. Add explicit Report 5 variants for notification authorization/privacy, assignee options, raw-import concurrency, deployment audit/stale rollback, package alignment, and operational workflow verification.
4. Update Report 6's account, maintenance/notification, concurrency-recovery, and CI/deployment workflow guidance without inventing UI or environment values.
5. Reconcile the RTM/STM and source revision, then consolidate Reports 3–6 into Report 7.
6. Keep test results `[To Be Updated After Test Execution]` and all unapproved team/environment values `[Needs Team Review]` throughout.

## Final disposition

**Status: Needs revision before final consolidation.** The school-report set is materially improved and preserves important uncertainty/physical-boundary qualifications, but it does not yet reflect every backend update consistently across requirements, design, testing, installation/user workflows, and the final report.
