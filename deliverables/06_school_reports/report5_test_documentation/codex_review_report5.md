# Codex Review — Report 5 Software Test Documentation

## Review scope

This file contains review comments only. `report5_test_documentation.md` and all comparison baselines were left unchanged.

The review compares Report 5 with the university template notes, SRS, requirements traceability matrix (RTM), UML and database designs, Reports 3 and 4, repository-evidence inventories, and the team open-question register. Static repository evidence is treated as test-design input, not proof that a test ran or passed.

Severity labels:

- **Critical** — could create false execution evidence or invalidate requirement-level traceability.
- **Major** — material template, coverage, or test-design gap.
- **Moderate** — ambiguity or overstatement that can mislead execution/reporting.
- **Minor** — editorial or maintainability improvement.

## Overall assessment

Report 5 follows all required top-level university headings and is unusually careful about distinguishing planned testing from executed results. It does **not** falsely claim that any test has passed. Unknown people, dates, tools, environments, builds, and thresholds are visibly marked, and Section 5 is clearly reserved for later verified results.

It is not yet execution-ready or submission-complete. Section 4 is a high-level risk catalogue rather than the detailed cases required by the companion workbooks; several FRs and NFRs have no explicit case mapping; broad requirement ranges prevent requirement-level pass/fail reporting; and test levels and test types are mixed in both column names and values. The report is a strong test-plan draft, not an executed Software Test Documentation package.

## 1. University template compliance

### R5-01 — Positive finding — Required document sections are present

The report contains the university structure recorded in `template_structure_notes.md`: Record of Changes; Scope of Testing; Test Strategy with Testing Types, Test Levels, and Supporting Tools; Test Plan with Human Resources, Environment, and Milestones; Test Cases; and Test Reports.

Retain this structure when converting to DOCX.

### R5-02 — Major — Required companion test-case/report workbooks are not supplied

The template notes require unit cases in `Report5_Unit Test.xls` and integration/system/acceptance cases and tracking in `Report5_Test Report.xlsx` (noting the older filename in the DOCX). Report 5 acknowledges that its rows are summaries and directs later expansion, but the reviewed package contains no evidence that the detailed workbooks exist.

**Recommendation:** Treat the workbooks as submission blockers. Record their approved filenames, locations, revision/baseline, and linkage convention in Report 5. Do not call Section 4 an executable test suite until the detailed rows include test data, exact steps, observable assertions, cleanup, executor, build, timestamp, evidence, and defect linkage.

### R5-03 — Major — The scope matrix does not explicitly distinguish inclusion from exclusion per requirement

Section 1 lists every broad FR group as planned, then separately lists deferred capabilities. That is not yet the template’s included/excluded feature matrix, because a reader cannot tell which individual requirements, NFR subclaims, client components, or external-provider behaviors are included, conditionally included, or excluded.

**Recommendation:** Add a scope disposition for every approved FR/NFR: In Scope, Conditional, Deferred, or Out of Scope, with reason, owner, target level, and environment dependency. Keep external provider internals and physical robot certification explicitly out of backend scope.

### R5-04 — Moderate — Change record and submission metadata remain placeholders

Official project metadata, initial change date, and accountable author are unresolved. These are correctly marked, but the document is not submission-ready while they remain blank.

**Recommendation:** Obtain values from the project/report owner; do not infer them from Git history.

### R5-05 — Moderate — Test-management controls are under-specified

The plan has milestone entry/exit summaries but no unified test-cycle entry criteria, exit criteria, suspension/resumption rules, defect severity definitions, retest/regression policy, evidence-retention rules, or change-control rule for a moving SRS/build.

**Recommendation:** Add these controls or place them in the companion workbook/test-plan appendix and cross-reference them. Define what happens when an open requirement prevents a deterministic expected result.

## 2. SRS, RTM/STM, and coverage

### R5-06 — Critical — The catalogue does not cover all 133 FRs despite claiming that scope

The requirement column in Section 4 omits at least:

| Missing ID | RTM capability |
|---|---|
| FR-007 | View/update own profile and effective access |
| FR-008 | Manage own push-notification device registrations |
| FR-011 | List/view/update/disable internal accounts |
| FR-012 | Administrator set/reset account password |
| FR-021 | Role-scope options lookup / tenant-tree navigation |
| FR-026 | MQTT subscriber credential lifecycle and reconciliation |
| FR-032 | Kiosk status overview and curated telemetry history |
| FR-074 | Payment-method catalogue management |

These behaviors may be incidentally touched by another broad case, but they have no explicit test-to-requirement link. Consequently, the statement that the proposed scope follows all 133 FRs is a scope intention, not demonstrated planned coverage.

**Recommendation:** Add explicit cases or accepted exclusions for each missing FR. Recalculate “requirements with planned cases” from machine-checkable mappings before populating Section 5.

### R5-07 — Major — Several NFRs have no explicit test-case mapping

At least NFR-002, NFR-015, NFR-018, NFR-019, and NFR-021 are named in scope but absent from the Section 4 requirement column:

- NFR-002 — Edge/Cloud offline tolerance;
- NFR-015 — modular-monolith boundary discipline and its inferred rationale;
- NFR-018 — layered compile-time dependency discipline;
- NFR-019 — global persistence conventions;
- NFR-021 — periodic operational metrics publication.

Some behavior overlaps existing recovery, database, or job cases, but overlap is not traceability.

**Recommendation:** Add dedicated verification cases or map the IDs explicitly to existing cases. For architecture NFRs, use static architecture/dependency tests or review checks and distinguish a verified structural constraint from an untestable/inferred benefit.

### R5-08 — Critical — Broad range mappings cannot support requirement-level results

Cases such as TC-CAT-001, TC-ORD-003, TC-ROBOT-001, TC-PC-001, TC-PP-001, TC-OPS-001, and TC-JOB-001 bundle many requirements, endpoints, transitions, and failure modes into one row. A single status cannot show which requirement passed or failed. This also makes the planned coverage total misleading: mapping one case to a range does not demonstrate that every requirement has an assertion.

**Recommendation:** Split cases into independently executable scenarios with one principal outcome. A requirement may map to several cases, and one focused case may map to several requirements, but every requirement subclaim must have at least one explicit assertion and result.

### R5-09 — Major — There is no complete Software Test Matrix

Report 5 maps case rows directly to requirement IDs and promises a later requirement-to-test matrix. The university request refers to an STM, while the baselines provide an RTM. No complete matrix currently connects Requirement → evidence status → test case → level/type → result → defect → build.

**Recommendation:** Define whether “STM” means the final test traceability matrix or the existing RTM. Add the full mapping in the companion workbook or an appendix. Preserve the RTM’s weakest-component status so an `[Inferred]` or `[Unclear]` requirement is not converted into an unconditional expected result.

### R5-10 — Major — Compound requirements need subclaim-level assertions

Several RTM rows deliberately carry compound or weakest-component statuses. A high-level case often expects the entire range to “behave as specified,” masking partial/inferred portions such as FR-009 temporary-password onboarding, FR-052 execution-driven consumption, FR-110/FR-119 recovery, NFR-003 recovery outcomes, NFR-004 delete behavior, and NFR-007 authorization coverage.

**Recommendation:** Split supported and uncertain subclaims in the test matrix. Mark a case Blocked/Needs Decision when no approved expected result exists; do not let repository behavior silently become the product acceptance criterion.

### R5-11 — Moderate — Business and data requirement coverage is selective

The catalogue includes valuable DR/BR database cases, but Section 5 admits that the total and coverage are unknown. Other business rules, API contracts, state transitions, data-retention rules, and application messages are not systematically mapped.

**Recommendation:** Inventory every BR and DR from the approved SRS, map each to a case or accepted exclusion, and identify requirements verified by inspection rather than runtime execution.

## 3. Test levels and test types

### R5-12 — Major — Level and type are mixed in the case table

Section 2 correctly defines four levels—Unit, Integration, System, Acceptance—but Section 4 places `API` and `Database` in the **Test Level** column (for example, `Unit/Integration/API` and `Integration/API/Database`). API and database testing are defined as test types in Section 2.1, not levels. Conversely, type values combine concerns such as `Functional/recovery`, `Recovery/concurrency`, and `Database/performance` without a controlled vocabulary.

**Recommendation:** Restrict Test Level to Unit, Integration, System, or Acceptance. Give test types separate normalized columns/tags: functional, API, database, security, reliability/recovery, performance, contract, and so forth.

### R5-13 — Major — One case spanning multiple levels has no level-specific procedure or result

A row marked Unit/Integration/System is not one executable case: each level uses different subject-under-test boundaries, dependencies, data, tools, and evidence. It will also be impossible to aggregate statistics accurately by level.

**Recommendation:** Create separate case IDs or variants per level, such as unit state-transition tests, integration persistence/adapter tests, and system workflow tests. Link them under a common scenario/requirement group if useful.

### R5-14 — Moderate — API testing appears at every level without boundary definitions

The level matrix marks API contract testing as planned at Unit, Integration, System, and Acceptance. That can be valid, but the document does not explain what “API” means at each level—controller/unit serialization, in-memory host, deployed endpoint, or user acceptance.

**Recommendation:** Define the subject, dependency boundary, and evidence for each cell. Avoid counting the same execution in multiple level totals.

### R5-15 — Moderate — Completion criteria remain too qualitative

Phrases such as “all critical cases pass,” “no unresolved blocking failure,” and “behavior matches approved decisions” require definitions for criticality, blocking severity, allowed known failures, and approval authority. Performance/capacity thresholds are correctly left unresolved, but other exit criteria are also not measurable yet.

**Recommendation:** Approve severity taxonomy, minimum coverage, allowed open-defect counts, retest requirements, and sign-off authority before execution.

## 4. Planned versus executed results

### R5-16 — Positive finding — No false execution claim was found

The introduction expressly says Report 5 is not execution evidence, every Section 4 case is `[Planned]`, and all Section 5 results are `[To Be Updated After Test Execution]`. It also correctly says that RTM `Supported` means statically evidenced, not runtime-tested. Retain these safeguards.

### R5-17 — Positive finding — Result tables are visibly future-facing

Build, database baseline, environment, period, overall assessment, statistics, defects, coverage, known issues, sign-off, and final disposition are all placeholders. The warning not to generate charts before verified totals exist is appropriate.

### R5-18 — Moderate — The “Planned” statistics value should be derivable now, not manually invented later

Section 5 leaves even planned totals blank. Because the current rows group multiple levels and requirements, a simple row count would be misleading.

**Recommendation:** After splitting level-specific cases, generate planned totals from the controlled case register. Define whether a parameterized case counts once or per data row and ensure totals reconcile across levels and types.

### R5-19 — Moderate — Future result updates need provenance rules

The document requests build and database baselines but does not define the minimum evidence needed to change a case from Planned to Passed/Failed/Blocked or who may approve such a change.

**Recommendation:** Require executor identity, UTC/local timestamp, source commit/build ID, migration/schema ID, environment ID, exact data set, actual result, evidence link/checksum, and defect ID where applicable. Define Passed, Failed, Blocked, Not Run, and Retest semantics.

## 5. Robot, Edge, sync, and payment realism

### R5-20 — Positive finding — Physical robot outcomes are not overclaimed

The report correctly excludes physical motion quality, dispensing accuracy, and safety certification from backend evidence; makes hardware-in-the-loop conditional on equipment and safety approval; and repeatedly says that Cloud acknowledgement, timeout, or report receipt does not prove physical execution. Retain this boundary.

### R5-21 — Positive finding — Command transport design reflects repository evidence

TC-SYNC-001 correctly treats MQTT as a best-effort wake-up and the durable REST pull as the command source. TC-IOT-001’s artifact-URL enrichment is supported by functional inventory IOT-05. TC-IOT-002 should use the exact supported acknowledgement values: `Received`, `Accepted`, `Rejected`, `ExecutorBusy`, and `DeliveryFailed`.

### R5-22 — Major — Dispatch idempotency expected result closes an open question

TC-SYNC-001 says “attempt identity prevents unintended duplicate dispatch.” The open-question register still asks for exact candidate selection, idempotency, concurrency, timeout, and terminal-support behavior for dispatch reconciliation. A proposed test must not assume the unresolved rule it is meant to discover or validate.

**Recommendation:** Split this into (a) characterization of current behavior and (b) acceptance against a team-approved idempotency rule. Until the rule is approved, mark the expected duplicate/concurrency outcome `[Needs Team Review]`.

### R5-23 — Major — Multi-family Edge ingestion cases overgeneralize deduplication

TC-DEV-002 and TC-IOT-003/004 group heartbeat, device events, telemetry, readiness, REST/MQTT execution reports, checkpoints, and state summaries. These families have different keys, ordering, replay, and conflict behavior. “Rejected/deduplicated” or “applies once” is not a universal contract.

**Recommendation:** Create separate cases per message family and transport. State each family’s authentication, idempotency key, sequence/checkpoint rule, clock-skew behavior, conflict response, and persistence effect. Keep unknown precedence `[Needs Team Review]`.

### R5-24 — Moderate — Acknowledgement testing needs full field semantics

TC-IOT-002 proposes “each documented AckStatus,” but the executable matrix must also test `PhysicalOutputMayHaveOccurred`, `LocalStatePersisted`, rejection code/message, acknowledgement timestamp/skew, endpoint ownership, repeat acknowledgements, and transition-specific order projection. Acknowledgement must remain distinct from later execution evidence.

**Recommendation:** Build an explicit state/field combination matrix from the exact request DTO and handler contract. Do not normalize `ExecutorBusy` to “Busy” or `DeliveryFailed` to “Failed.”

### R5-25 — Positive finding with required follow-up — Payment callback scenarios are realistic

TC-PAY-002 appropriately includes valid, invalid-signature, duplicate, late, and conflicting callbacks and does not invent the unresolved conflict precedence. TC-PAY-003 also avoids promising guaranteed reconciliation.

**Required follow-up:** Define exact signature fixtures, canonical payload handling, replay/dedup key, provider timestamp/skew policy, transaction boundaries, post-commit dispatch failure, and deterministic provider responses before execution. Keep late/conflicting precedence blocked pending OP-09.

### R5-26 — Moderate — Payment coverage omits payment-method catalogue management

FR-074 is absent. Session, callback, reconciliation, diagnostics, and refund cases do not test manager/staff viewing or changing payment-method catalogue status.

**Recommendation:** Add authorization, list/read, valid status transition, invalid transition, concurrency, and impact-on-session-eligibility cases for FR-074.

### R5-27 — Moderate — External fake versus sandbox outcomes must be reported separately

A deterministic fake can verify backend branching and signatures, but it cannot establish real provider compatibility. The plan treats both as approved alternatives without specifying which claims each supports.

**Recommendation:** Label cases as adapter/component tests with a fake or provider-contract tests with an approved sandbox. Do not report fake-only success as PayOS/Firebase/FCM interoperability.

## 6. Tools, people, dates, and environments

### R5-28 — Positive finding — Unknown planning data is properly marked

Names, dates, milestones, framework/client choices, credentials, environment identifiers, test data, acceptance authority, and performance/security tools are all visible placeholders. No unknown commercial tool or team member is invented.

### R5-29 — Moderate — PostgreSQL 17 should remain a configuration baseline, not an approved test requirement

The repository evidence confirms PostgreSQL 17 in the current compose configuration. Report 5 generally marks it as an assumption/current indication, which is appropriate because the open-question register asks whether it is a binding deployment constraint.

**Recommendation:** Record the exact tested image/version only after environment freeze, and distinguish “repository default” from “supported production/test matrix.” Apply the same treatment to MinIO and Mosquitto versions/configuration.

### R5-30 — Major — The environment lacks a reproducible baseline schema

The environment table correctly says migrations/model snapshot need approval, but no fields identify OS/container runtime, .NET SDK/runtime, application configuration, database schema/migration, broker/object-store versions, network topology, clock/time zone, seed revision, or reset mechanism.

**Recommendation:** Add an environment manifest with immutable identifiers and retain it with execution evidence. Clock control is especially important for token expiry, callback timing, acknowledgement skew, retries, leases, and reconciliation.

### R5-31 — Moderate — Security and hardware testing require explicit authorization gates

The report marks tools and hardware as pending, but execution controls should also name the person authorized to approve penetration/security testing, provider sandbox use, production-like payloads, and physical rig operation.

**Recommendation:** Add authorization and safety sign-off as entry criteria. Prohibit production credentials/data and unsupervised physical execution.

## 7. Future-update readiness

### R5-32 — Positive finding — Section 5 is clearly reserved for verified future data

All result subsections are marked for later update, open questions are explicitly distinguished from defects, and the closing checklist requires execution evidence before finalization. This directly satisfies the requested future-update safeguard.

### R5-33 — Moderate — Placeholder replacement needs an auditable completion check

The final instruction says to replace every execution placeholder, but there is no review control preventing partial replacement, stale totals, mismatched case IDs, or charts based on another baseline.

**Recommendation:** Add a pre-submission check that searches for placeholder/status tokens, reconciles totals, validates every requirement/case/defect reference, confirms all results share the declared baseline, and records reviewer sign-off.

### R5-34 — Minor — Preserve planned and actual data separately

Replacing planned rows in place can erase the approved plan and obscure scope changes.

**Recommendation:** Preserve planned priority/level/scope and add actual status, executor, date, evidence, and defect fields. Record added/removed/deferred cases through change control.

## 8. Recommended additions to `open_questions.md`

Add these if they are not already represented by an equivalent entry:

| Topic | Question |
|---|---|
| STM meaning and owner | Is the required STM a new requirement-to-test-result matrix, an extension of the RTM, or a university workbook view, and who owns it? |
| Test baseline freeze | Which source commit/build, SRS/RTM revision, migration/schema, configuration, and data revision define each execution cycle? |
| Test framework and projects | What frameworks, versions, test projects, coverage method, and CI runner are approved? |
| Case granularity/counting | How are parameterized cases, retries, and multi-level variants counted in planned/executed totals? |
| Exit criteria | What coverage, severity, failure, blocked-case, and known-risk thresholds permit acceptance? |
| Edge contract simulator | What exact Edge version/simulator implements REST/MQTT payloads, timing, idempotency, and report behavior? |
| Physical test authority | Is hardware-in-the-loop in scope, and who approves equipment, safety procedure, observation method, and acceptance? |
| Provider verification | Which PayOS/Firebase/FCM claims require a real sandbox rather than a fake? |
| Performance/security authorization | What workloads, thresholds, tools, environment, and authorizer apply? |
| Result evidence retention | Where are logs, payloads, screenshots, database snapshots, reports, and checksums stored, and for how long? |

## 9. Revision priority

1. Build a complete FR/NFR/BR/DR-to-test matrix and add the explicitly missing requirements.
2. Split broad multi-requirement and multi-level rows into independently executable cases.
3. Normalize test level versus test type and define measurable entry/exit/result rules.
4. Resolve or block expected results that depend on open dispatch, ingestion, callback, or database semantics.
5. Create the university companion workbooks and reproducible environment/baseline manifest.
6. Execute only after tools, roles, dates, environments, provider access, and safety/authorization gates are approved.
7. Populate Section 5 solely from traceable execution records and run a final placeholder/total/link audit.

## Final disposition

**Suitable as a test-planning draft; not ready as final or executed Report 5.** Its planning/execution distinction and physical robot boundary are strong. Final readiness requires complete requirement mapping, executable level-specific cases in the required companion artifacts, an approved reproducible environment, resolved expected-result semantics, and verified execution/defect/coverage evidence.
