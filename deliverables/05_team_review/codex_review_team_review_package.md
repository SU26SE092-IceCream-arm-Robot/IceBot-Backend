# Codex Review — Team Review Package

## Review Scope

Reviewed without modifying:

- `deliverables/05_team_review/review_guide.md`
- `deliverables/05_team_review/open_questions.md`
- `deliverables/05_team_review/change_log.md`
- `deliverables/05_team_review/team_review_checklist.md`

Compared against:

- `deliverables/01_project_introduction/project_introduction.md`
- `deliverables/02_srs/srs.md`
- `deliverables/02_srs/requirements_traceability_matrix.md`
- all Markdown deliverables under `deliverables/03_uml/`
- all Markdown deliverables under `deliverables/04_database_design/`
- all Markdown evidence files under `deliverables/00_repo_evidence/`
- existing Codex review notes under `deliverables/05_team_review/` and `deliverables/05_review_checklists/`

This document contains review comments only.

## Executive Assessment

The package is a good consolidation of earlier findings. Its strongest file is `open_questions.md`, which captures most known uncertainty across product, API, workflows, IoT, database, deployment, and report presentation. The guide also states sound evidence principles.

The package is not yet operationally ready for a team review, however. It tells reviewers what to think about but does not define enough of the review machinery needed to finish:

- no frozen baseline/version or review scope per reviewer;
- no severity/priority model;
- no per-question owner, due date, status, decision type, or blocker;
- no required evidence/output for most checklist items;
- no issue disposition and conflict-resolution workflow;
- no verification or re-review gate;
- no approval authority, quorum, or conditional-approval rules;
- missing specialist reviewer roles.

There are also a few invented/future-looking questions mixed with current correctness issues, plus terminology and source-reference errors.

## 1. Missing Important Open Questions

The register is broad, but the following material questions from the evidence and prior reviews are absent or insufficiently explicit.

### Order, payment, and transaction semantics

1. **Payment confirmation atomic boundary**: Are `PaymentTransaction`, `Order`, history rows, and initial `EdgeCommand` created/updated in one database transaction, and which parts occur after commit?
2. **Late payment semantics**: What happens when payment succeeds after the payment deadline, after customer/management cancellation, or after another transaction became Primary?
3. **Duplicate/late callback semantics**: What exact key deduplicates PayOS callbacks, and how are conflicting provider events reconciled?
4. **Payment/refund/incident state alignment**: What invariant relates `Order.PaymentStatus`, transaction status, settlement disposition, refund status, and production-incident resolution?
5. **External-I/O transaction boundary**: Which payment, MQTT, object-storage, push, and email calls occur inside versus outside database transactions, and what compensation applies after partial failure?

`OP-01` and `OP-02` touch failure/recovery generally, but they do not make these high-impact invariants independently reviewable.

### Edge, execution, and source of truth

6. **Conflicting execution evidence**: Which source wins when Cloud command state, execution report, production-event stream, and Edge state summary disagree?
7. **Command expiry and stale acknowledgement**: How are ack/report events handled after command expiry, redispatch, remake, or support escalation?
8. **Idempotency retention**: For each retryable API/event family, what is the key scope, request checksum policy, concurrency behavior, and retention window?
9. **SignalR delivery semantics**: Are messages best-effort deltas, and what authoritative refetch/recovery behavior is required after disconnect or missed events?
10. **MQTT credential recovery**: What happens when broker provisioning succeeds but database persistence fails, or vice versa?

`RI-02`, `RI-03`, and `RI-05` cover transport/cardinality, but not these conflict and compensation rules.

### Security and privacy

11. **Credential lifecycle**: What are issuance, expiry, rotation, revocation, overlap/grace-period, and emergency-recovery rules for JWTs, refresh tokens, endpoint credentials, MQTT credentials, and mTLS certificates?
12. **Sensitive-data exposure**: Which raw provider/device/sync payloads, signatures, public keys, tokens, email addresses, and diagnostic fields may be stored, logged, or returned to each role?
13. **Secret bootstrap**: Does identity bootstrap create privileged credentials, how are they delivered, and how is first-use rotation enforced?
14. **Webhook replay window**: Beyond signature verification, what timestamp/replay protection exists for payment callbacks?

These should not be left implicit under authorization coverage or HTTP contracts.

### Data integrity and lifecycle

15. **Append-only enforcement layer**: Are history/callback/evidence tables protected against update/delete by database permissions/triggers, repository behavior, or convention only?
16. **Physical deletion/restoration**: Which soft-deleted records may be restored or physically purged, and how do unfiltered business keys affect restoration/reuse?
17. **Data ownership across contexts**: Which cross-context IDs are enforced FKs, deliberate soft references, or immutable snapshots, and who owns correction when referenced data changes?
18. **Current-model reconciliation**: Does the EF model snapshot agree with entity/configuration source and all five migrations, not merely with table counts?
19. **Manual migration execution**: Are manual-step classes automatically invoked by deployment tooling or separate operator procedures?

`DB-01`–`DB-12` cover parts of these concerns but not the full enforcement/operation questions.

### Review/report governance

20. **Release/baseline identity**: Which commit, branch, date, or document revision is the review approving?
21. **Materiality threshold**: Which unresolved items block school-template conversion, which allow conditional approval, and which are informational?
22. **External product evidence**: Which questions require `IceBot-Product`, frontend repositories, team decisions, or supervisor instructions rather than backend-repository evidence?
23. **Final language and format compliance**: Who verifies school-template structure, required sections, notation, citations, Vietnamese/English consistency, and diagram rendering?

## 2. Review Guide Is Too Vague

The guide provides principles and a nine-step order, but lacks executable review instructions.

### Missing scope controls

- No baseline commit/hash, document revision, or “files frozen for review” rule.
- No distinction between full-package review and bounded-context assignment.
- No mechanism to prevent documents changing while reviewers work.
- No list of known blocking issues versus optional improvements.
- No statement of whether raw `*-pack.md` files are mandatory evidence or fallback evidence.

### Missing review outputs

For each stage, the guide should specify what the reviewer must produce, for example:

- approved / rejected / conditional result;
- issue IDs and severity;
- exact file/section/claim;
- evidence checked;
- proposed disposition;
- open-question ID;
- verification evidence;
- downstream documents affected.

The suggested change proposal format is useful, but it is not connected to a named issue register, status workflow, or approval record.

### Missing acceptance criteria by stage

“Validate,” “agree,” “audit,” and “walk through” do not say when a step is complete. Examples:

- Evidence baseline is complete when all inventory row counts reconcile and admitted pattern/wiring-only claims are labeled.
- SRS review is complete when every FR/NFR has evidence confidence and verification disposition.
- UML review is complete when every state/actor/message/cardinality maps to an owning requirement or declared abstraction.
- Database review is complete when count rules, FK optionality, unique predicates, and effective delete behavior have dispositions.

### Missing conflict-resolution procedure

The guide says to escalate conflicts but does not define:

- who decides product semantics;
- who decides architecture/data ownership;
- who decides observed repository fact;
- what happens when code and docs disagree;
- how a rejected proposal is recorded;
- how to record “code is current, docs stale” versus “docs are intended contract, code gap.”

### Missing verification loop

There is no explicit process to:

1. apply approved corrections;
2. run searches/traceability checks;
3. re-review affected downstream documents;
4. close or retain uncertainty labels;
5. record verification evidence;
6. obtain final sign-off on the changed baseline.

### Terminology error

The repository artifact is `requirements_traceability_matrix.md`, normally abbreviated **RTM**. The guide, open-question register, and checklist repeatedly use **STM**. This is inconsistent with the filename and common terminology. Choose one abbreviation and define it once.

## 3. Checklist Items That Are Not Actionable

Many checklist items use broad verbs without defining evidence, scope, or completion output.

### Representative non-actionable items

| Checklist wording | Why it is not actionable | Needed refinement |
| --- | --- | --- |
| “Confirm each reviewed FR matches…” | No assigned FR range or record of what was compared. | Assign bounded context/FR IDs; require controller/handler/persistence/test references and result. |
| “Review every Partial, Inferred…” | “Every” is not bounded per reviewer and has no inventory. | Generate a list of labeled claims and require disposition per ID. |
| “Verify order/payment… state transitions…” | No canonical transition table or expected output. | Require a state/invariant matrix and discrepancy list. |
| “Check exact endpoint and DTO claims…” | No endpoint checklist or source authority. | Use OpenAPI/controller/DTO/validator fields and record endpoint-level result. |
| “Confirm the listed UI actors…” | Frontend repository/version is unspecified. | Name repositories/builds and require actor-to-screen/API evidence. |
| “Validate MQTT topic… claims…” | No contract/version or topic inventory is named. | Require comparison against the authoritative IoT contract and runtime configuration. |
| “Reconcile mapped entity… counts…” | Good goal, but no required counting method/output. | Require separate counts from DbSets, model snapshot, migration operations, and live schema. |
| “Audit soft-delete filters…” | No query inventory or completeness criterion. | Enumerate 12 principal types and all query entry points; record missing filters. |
| “Use one glossary…” | No glossary artifact or owner exists. | Create/navigate to a glossary and require term-owner approval. |
| “Every material claim has…” | “Material” is undefined. | Define severity/materiality and sampling versus exhaustive rules. |
| “Team has approved…” | No approval authority, quorum, or allowed result values. | Define required signers and Approved/Conditional/Rejected criteria. |

### Checklist structure gaps

- No item ID, owner, due date, evidence/result field, severity, or linked question.
- A checkbox can be marked after merely recording an open question, even when the item is a release blocker.
- No `Not Applicable` option requiring rationale.
- No distinction between review performed, issue found, fix applied, and fix verified.
- No per-document completion table.
- No checklist version/baseline.
- No automated checks for ID coverage, links, headings, Mermaid rendering, stale terminology, or count consistency.

## 4. Unsupported or Invented Concerns

Most concerns are grounded in prior deliverables/reviews. The following require reframing so they do not become invented product scope.

| Item | Concern | Recommended treatment |
| --- | --- | --- |
| `PQ-05` customer accounts/loyalty | Future loyalty is not a current requirement or repository gap. | Keep only as an explicit roadmap question if a product owner requested it; otherwise state current anonymous boundary as supported. |
| `PQ-03` permanent versus deferred exclusions | Asking whether every exclusion is permanent can manufacture roadmap work. | Ask only which exclusions affect the target report/release. |
| `AB-04` missing/planned REST dashboard | Evidence establishes GraphQL dashboard and no REST surface found. | Record current fact; ask for REST only if a product requirement exists. |
| `RP-04` who may “restore” soft-deleted records | No restoration feature is evidenced. | Separate current deleted-data visibility from a new restoration requirement. |
| `DO-03` RPO/RTO/availability | Valid SRS gaps, but not existing implemented facts. | Label as new non-functional requirements requiring product/operations decisions, not unresolved repo behavior. |
| `SR-04` presentation abstraction | A report-format choice, not a database correctness question. | Assign to report owner; do not block technical approval unless school guidance requires it. |
| `DB-10` protection against enum reordering | Legitimate risk derived from numeric predicates, but “protected” may imply new infrastructure. | Ask whether enum numeric values are explicitly stable/contractual and verified against migrations. |
| `PQ-04` groups providers, PostgreSQL version, and database name | These have different owners and decision types. | Split product-provider portability from deployment platform/version/naming assumptions. |
| `RI-04` every kiosk has robot/device/endpoint | Current nullability/provisioning can be reviewed as fact. | Separate current legal persistence state from product requirement for an activated kiosk. |

The register should classify each item as one of:

- factual verification;
- code/docs discrepancy;
- product decision;
- architecture decision;
- operational requirement gap;
- report presentation decision;
- future roadmap question.

Without this type, speculative roadmap questions can receive the same weight as confirmed factual errors.

## 5. Missing Reviewer Roles

The existing roles cover backend, frontend, Edge, database, and project/report. Important independent responsibilities are missing.

### Security / authentication reviewer

Owns:

- RBAC coverage;
- token and credential lifecycles;
- mTLS/ECDSA/MQTT authentication;
- webhook verification/replay;
- diagnostic/raw-payload exposure;
- bootstrap privileged access;
- secret storage/logging.

Security should not be self-approved solely by the backend implementer.

### QA / test / verification reviewer

Owns:

- mapping requirements to unit/integration/contract tests;
- acceptance criteria;
- negative, concurrency, retry, and partial-failure scenarios;
- distinguishing static evidence from executed verification;
- recording test environment and result.

This role is essential because the package repeatedly acknowledges that `[Supported]` is not runtime verified.

### DevOps / SRE / deployment reviewer

Owns:

- environment profiles and mandatory hosted jobs;
- migrations/manual steps;
- connection configuration;
- health/readiness/diagnostics;
- broker/object storage/database dependencies;
- backup/restore, RPO/RTO, monitoring, and incident response.

### Product/business domain owner

The current “Project Manager / Report Writer” combines decision ownership with document production. Separate a product/domain owner who can approve:

- business goal and actors;
- release scope;
- payment/refund/incident semantics;
- exclusions and roadmap;
- provider constraints;
- acceptable operational limitations.

### Payment/finance operations reviewer

PayOS callbacks, duplicate settlement, manual refund, reconciliation, accounting evidence, and intervention workflows need a business/operations reviewer, not only backend review.

### Documentation/academic compliance reviewer

Owns:

- school-template completeness;
- notation and diagram readability;
- citation/path correctness;
- glossary and bilingual terminology;
- consistency after formatting conversion.

### Optional privacy/data-governance reviewer

Needed if the final report makes retention, personal-data, logging, or compliance claims. This role should approve retention and diagnostic exposure decisions rather than treating them as purely database choices.

## 6. Missing Review Process Steps

Add these stages to make the package executable.

1. **Freeze baseline** — record commit/hash, branch, date, file manifest, and known untracked files.
2. **Triage issues** — assign type, severity, priority, owner, due date, and blocking status.
3. **Allocate review scope** — assign bounded contexts/FR ranges/diagrams/database sections to named reviewers.
4. **Declare authorities** — repository fact owner, product decision owner, architecture owner, operations owner, report owner.
5. **Perform evidence review** — record exact evidence checked and whether review is static, test-based, or decision-based.
6. **Create issue/disposition record** — Accepted, Rejected, Deferred, Duplicate, Not Applicable, or Needs Evidence, with rationale.
7. **Approve proposed change** — especially for public contracts, workflow semantics, ownership, and architecture decisions.
8. **Apply coherent corrections** — update owning and downstream deliverables together.
9. **Run mechanical verification** — requirement-ID coverage, inventory-ID coverage, link/path validation, count checks, stale-term search, Mermaid rendering.
10. **Run technical verification** — focused tests/model inspection where a claim requires runtime or generated-model evidence.
11. **Independent re-review** — reviewer other than author verifies the resulting wording and evidence.
12. **Update question register and change log** — link resolution, affected files, residual uncertainty, and evidence.
13. **Conditional-approval gate** — list accepted open items and why they do not block conversion.
14. **Final sign-off** — identify required approvers, result, date, baseline revision, and remaining question IDs.
15. **Conversion integrity check** — compare school-template output against approved baseline to ensure formatting did not change technical meaning.

## 7. Inconsistency with Existing Deliverables

### Incorrect abbreviation

- Package files repeatedly use `STM`.
- The actual document is `requirements_traceability_matrix.md` and is described as a Requirements Traceability Matrix.
- Use `RTM` consistently unless the team explicitly defines another term.

### Invalid section reference

- `open_questions.md` `SR-04` cites `conceptual_database_design.md` §2.14.
- The current conceptual design ends its domain subsections at §2.13 before §3.
- This source reference is invalid.

### Missing evidence-review routing clarity

- The package table lists existing `05_team_review/codex_review_*.md` notes but does not list `deliverables/05_review_checklists/evidence_review_final.md` explicitly.
- `SR-01` correctly notes that documents expected `00_repo_evidence/evidence_review_final.md`, which does not exist.
- The guide should identify the actual review source path and its authority instead of leaving reviewers to discover it.

### Counts remain unresolved but are used as workflow assumptions

- The package correctly carries the 260/265 functional count question.
- It should also state the known decomposition: 260 identifiable rows, while the inventory summary overcounts Operations by four and Payments by one.
- Database count language must continue distinguishing 98 DbSets from physical tables and cumulative migration creates.

### Open-question status model conflicts with usage

- The register says every entry remains `[Open Question]`.
- Some entries are known factual corrections or verification tasks, not decisions: incorrect `DeviceType` key, capability-projection relationship, invalid cardinalities, count arithmetic, and invalid citations.
- Known corrections should be issues with a required fix, not questions whose answer is optional.

### Change log is empty despite a developed review baseline

It is reasonable that no approved corrections have been recorded, but the log should clarify whether creation of the SRS, RTM, UML, database designs, and review package establishes the initial baseline. Otherwise reviewers cannot tell what version the future entries modify.

### Change-log template gaps

Missing:

- change/issue ID;
- baseline revision before and after;
- approval date and approver signatures;
- linked open-question resolution;
- verification performed/result;
- rejected/superseded status;
- link to exact decision record;
- rollback/supersession note;
- explicit confirmation that all downstream files were checked.

### Sign-off table is underspecified

- Result vocabulary is undefined.
- No required versus optional signers.
- No baseline revision being signed.
- No conditional approval/defer rationale.
- No independent QA/security/operations sign-offs.
- No final approver for school-template conversion.

## File-by-File Review Summary

### `review_guide.md`

Strengths:

- Correct evidence hierarchy mindset.
- Good separation of supported/inferred/assumed/unclear.
- Good warning that route evidence does not prove behavior.
- Sensible high-level review order.

Required improvements:

- Define baseline, outputs, severity, ownership, conflict resolution, verification, and approval mechanics.
- Replace STM with RTM.
- Explicitly route the evidence review at its actual path.
- Define material/blocking criteria.

### `open_questions.md`

Strengths:

- Broad and well-sourced consolidation.
- Stable category/ID structure.
- Good resolution template.

Required improvements:

- Add owner, type, severity, status, due date, blocker, dependencies, and resolution link per item.
- Add the missing transaction/security/execution-conflict questions above.
- Separate factual corrections from product decisions and future roadmap ideas.
- Correct invalid source references and STM terminology.

### `change_log.md`

Strengths:

- Captures reason, evidence impact, reviewer, downstream files, and follow-up.

Required improvements:

- Establish the initial baseline.
- Add unique IDs, before/after revision, verification, decision link, and supersession/rejection handling.
- Define who may mark an entry approved.

### `team_review_checklist.md`

Strengths:

- Covers the main implementation disciplines.
- Includes failure paths, evidence confidence, data integrity, and final consistency.
- Provides a sign-off table.

Required improvements:

- Add security, QA, DevOps/SRE, product-domain, payment-operations, and academic/documentation roles.
- Give items stable IDs and required evidence/output.
- Add N/A, issue-found, fix-applied, and verified states rather than one checkbox.
- Define scope assignments and completion criteria.
- Replace STM with RTM.
- Define sign-off result vocabulary and approval authority.

## Final Recommendation

Use the package as a strong **review issue source**, but not yet as the authoritative review procedure.

Before the team begins formal approval:

1. Freeze and identify the baseline.
2. Correct RTM/section-reference inconsistencies.
3. Convert the open-question list into an owned, prioritized status register.
4. Separate known factual corrections, verification tasks, product decisions, and roadmap questions.
5. Add the missing reviewer roles.
6. Make checklist items produce concrete evidence and dispositions.
7. Define conflict resolution, re-review, conditional approval, and final sign-off.
8. Add the missing high-impact payment, execution-source-of-truth, credential, and external-I/O questions.

