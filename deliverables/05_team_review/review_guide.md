# Team Review Guide — IceBot Backend Deliverables

**Purpose**: Help the team correct and approve the current evidence-based documentation baseline before it is converted into school-template reports. This package is for review and maintenance; it is not the final report. **RTM** means Requirements Traceability Matrix (`02_srs/requirements_traceability_matrix.md`).

## 0. Freeze the Review Baseline

Before assigning review work, record the following in the review sign-off table and do not silently change the reviewed files:

| Baseline field | Value |
|---|---|
| Repository branch |  |
| Commit/hash or named workspace snapshot |  |
| Review start date |  |
| Included file manifest |  |
| Known uncommitted/untracked files |  |
| Review coordinator |  |

A reviewer may receive the full package or a bounded assignment (bounded context, FR/NFR range, diagram, or database section). Record that scope before review. If an included file changes, mark the affected assignment **Re-review required** and record the new baseline rather than reviewing a moving target.

Raw `00_repo_evidence/*-pack.md` files are fallback evidence for targeted source verification. Reviewers should begin with the authored inventories and use raw packs only when the inventory does not settle the exact claim.

## 1. Review Principles

1. Review claims against the evidence named in the document, not against memory or the apparent intent of a class or field name.
2. Treat current code and repository documentation as evidence of implementation, not proof of runtime success, test coverage, production configuration, or business approval.
3. Do not remove an uncertainty label merely to make a document sound final. Resolve the underlying question, record the evidence or decision, and then change the label.
4. Keep current implementation, normative requirements, architecture rationale, team assumptions, and future plans separate.
5. Review cross-document consistency. A correction to scope, terminology, a requirement, cardinality, or data constraint may affect several downstream documents.

## 2. Purpose of Each Deliverable

| Deliverable | Review purpose |
|---|---|
| `00_repo_evidence/repo_truth_map.md` | High-level map of architecture, actors, contexts, flows, surfaces, and known evidence limits. Use it to orient reviewers. |
| `00_repo_evidence/functional_inventory.md` | Code-evidenced capability inventory. Use its IDs and status values when checking feature coverage; do not treat `Partial` as complete. |
| `00_repo_evidence/database_inventory.md` | Evidence baseline for entities, EF mappings, relationships, indexes, constraints, migrations, and discrepancies. Use it for database claims. |
| Other `00_repo_evidence/*-pack.md` files | Raw source packs supporting targeted verification. They are evidence inputs, not polished requirements or design decisions. |
| `01_project_introduction/project_introduction.md` | Team baseline for product context, actors, scope, major features, architecture, and integrations. Business owners should validate inferred motivation and scope. |
| `02_srs/srs.md` | Working requirements baseline: interfaces, FRs, NFRs, data requirements, and business rules. It describes what the documented release is expected to do and the evidence confidence behind each claim. |
| `02_srs/requirements_traceability_matrix.md` | Maps SRS requirements back to inventory IDs, entities, and evidence. Use it to find missing, compound, weak, or unsupported requirements. |
| `03_uml/use_case_diagram.md` | Actor-to-capability view. Check actor ownership, permissions, and missing/extra use cases. |
| `03_uml/activity_order_flow.md` | Decision-oriented order lifecycle. Check states, alternate paths, incidents, cancellations, and compensation. |
| `03_uml/sequence_order_flow.md` | Interaction order across checkout, payment, dispatch, Edge, robot execution, and Cloud finalization. Check boundaries and transaction/external-I/O claims. |
| `03_uml/sequence_robot_execution.md` | Robot command delivery, acknowledgement, evidence, timeout, and recovery view. Check REST/MQTT roles and uncertain physical outcomes. |
| `03_uml/class_diagram.md` | Domain-oriented structural view. Check class ownership and conceptual associations; it intentionally omits some join-entity detail. |
| `03_uml/erd.md` | Data relationship and cardinality view. Check it against logical/physical database design rather than assuming navigation properties prove cardinality. |
| `04_database_design/conceptual_database_design.md` | Business-readable data concepts and subject areas. Validate terminology and abstraction choices. |
| `04_database_design/logical_database_design.md` | Entities, attributes, relationships, optionality, and logical constraints. Validate semantics independently of physical implementation. |
| `04_database_design/physical_database_design.md` | PostgreSQL/EF implementation baseline: table names, indexes, checks, FKs, conventions, retention, and migration notes. Validate against database evidence. |
| Existing `05_team_review/codex_review_*.md` notes | Prior review findings. Use them as issue sources, not as automatically approved decisions. |
| `05_team_review/open_questions.md` | Consolidated unresolved decisions and verification tasks. Record an owner and resolution evidence before closing an item. |
| `05_team_review/change_log.md` | Lightweight history of documentation changes and evidence impact. |
| `05_team_review/team_review_checklist.md` | Role-specific review gates and the whole-team approval checklist. |

## 3. Suggested Review Order

1. **Agree on evidence semantics**: read `DELIVERABLES_AGENT.md`, then the truth map and the status/label rules in this guide.
2. **Validate evidence baselines**: backend owners review the functional inventory; the database reviewer reviews the database inventory. Resolve count discrepancies before copying figures into final reports.
3. **Validate product framing**: review the Project Introduction for business goal, actors, scope, exclusions, and external-provider assumptions.
4. **Validate requirements**: review the SRS by bounded context. Separate supported implementation facts from desired requirements and product decisions.
5. **Audit traceability**: for every SRS item, confirm its RTM evidence, inventory IDs, status, and notes. Pay special attention to compound rows and `Needs Review` items.
6. **Walk through workflows**: compare use-case, activity, and sequence diagrams with the SRS. Review normal, failure, retry, partial-success, and manual-intervention paths.
7. **Validate data design**: review conceptual, logical, ERD, and physical documents in that order. Reconcile terminology, cardinality, tenancy, delete behavior, and constraints.
8. **Resolve the question register**: assign each open question to a decision owner or evidence owner. Record decisions in the owning deliverable before closing the question.
9. **Run the whole-team checklist**: approve terminology, scope, evidence coverage, remaining uncertainty, and readiness for school-template conversion.

## 4. Status and Uncertainty Labels

| Label | Meaning | Reviewer action |
|---|---|---|
| `[Supported]` | Directly supported by the cited repository evidence. In the SRS/RTM this normally means statically code-evidenced, not runtime- or test-verified. | Confirm the citation supports the exact scope and wording. Narrow universal words such as “all,” “always,” or “exactly once” unless exhaustively proven. |
| `[Inferred]` | Reasonable interpretation from indirect evidence, wiring, naming, or an incompletely inspected implementation. | Verify against authoritative code/tests or retain the label. Do not promote it because it seems likely. |
| `[Assumption]` | Working premise not established as a product decision or binding requirement. | Ask the decision owner to approve, reject, or reframe it; record the decision in the owning document. |
| `[Unclear]` | Available evidence is ambiguous, conflicting, or insufficient to choose an interpretation. | Identify the missing evidence or owner. Do not choose an interpretation from naming alone. |
| `[Open Question]` | Explicit unresolved question requiring a decision or verification. | Add it to `open_questions.md`, assign an owner, and link the resolution evidence when closed. |
| `[Needs Review]` | A technical claim or compound requirement has a material gap that a focused code inspection, test, or rewrite can address. | Review the weak component. Split compound claims when one status cannot describe them safely. |

When a statement has several material parts, use the weakest applicable status or split it into independently reviewable statements. A supported route does not automatically support authorization, transactionality, error behavior, retry semantics, or runtime success.

## 5. How to Propose Changes Safely

1. Identify the exact claim and its owning document; include the section, requirement ID, inventory ID, or diagram node.
2. State whether the proposal is a factual correction, evidence update, terminology correction, product decision, or presentation-only change.
3. Cite the evidence. If the evidence is incomplete, preserve or add the correct uncertainty label.
4. List affected downstream documents. Typical chains are Introduction → SRS → RTM → UML → database design, or database inventory → logical/physical design → ERD → SRS data requirements.
5. Do not change a public contract, workflow meaning, entity ownership, or cardinality just to make documents agree. Escalate the conflict to the responsible team member.
6. Apply the smallest coherent documentation change and update all affected references in the same review item.
7. Add an entry to `change_log.md`, including whether evidence changed and whether follow-up remains.
8. Before approval, search for the old term/claim and check that no stale contradiction remains.

Suggested proposal format:

```text
Claim/section:
Change type:
Current wording:
Proposed wording:
Evidence or decision owner:
Affected files:
Uncertainty label after change:
Follow-up:
```

## 6. Issue Triage and Required Review Output

Every finding must have an ID and record:

- type: factual verification, code/docs discrepancy, product decision, architecture decision, operational requirement gap, report presentation decision, or roadmap question;
- severity: **Blocker** (unsafe or impossible to approve), **Major** (materially changes scope/contract/workflow/integrity), **Minor** (localized correctness/consistency), or **Editorial**;
- status: Open, Needs Evidence, Proposed, Accepted, Rejected, Deferred, Duplicate, Not Applicable, Fixed, or Verified;
- owner, due date, blocking status, exact claim/location, evidence checked, proposed disposition, affected files, and linked open-question ID.

`Not Applicable` and `Deferred` require a rationale. `Fixed` means wording was changed; `Verified` means someone other than the author checked the resulting wording and evidence. A Blocker or unresolved Major issue blocks approval unless every required approver explicitly accepts it under Conditional approval.

Each assigned review must produce: its baseline and scope; issue records; exact evidence checked; Approved, Conditional, or Rejected result; verification evidence; and remaining question IDs.

## 7. Decision Authority and Conflict Resolution

| Conflict | Decision authority |
|---|---|
| Observed repository fact | Responsible implementation reviewer, independently verified from source/generated model/test evidence |
| Product meaning, actor, release scope, workflow outcome | Product/business domain owner |
| Architecture, bounded-context ownership, public contract | Architecture/backend owner with affected-team approval |
| Database integrity and migration behavior | Database owner; generated-model/live-schema evidence takes precedence over narrative inference |
| Deployment and operational requirement | DevOps/SRE owner with product owner for service targets |
| School format, notation, language, and citations | Documentation/academic compliance owner |

When code and documentation disagree, record one of: **code current/docs stale**, **docs intended/code gap**, or **insufficient evidence**. Do not edit the documents until the appropriate authority accepts the disposition. Record rejected proposals and rationale in the issue/change history; rejection does not erase the issue.

The actual prior evidence-review note is `deliverables/05_review_checklists/evidence_review_final.md`. Earlier deliverables requested `deliverables/00_repo_evidence/evidence_review_final.md`, which does not exist. Treat the actual note as review commentary, not as a replacement source-of-truth evidence file, until the team explicitly resolves SR-01.

## 8. Stage Completion and Verification Loop

| Stage | Complete when |
|---|---|
| Evidence baseline | Counting discrepancies are resolved or disclosed; partial/pattern/wiring-only evidence is labeled; each material claim has a source or issue. |
| Introduction | Product owner disposes business goal, actor, release-scope, exclusion, and provider assumptions. |
| SRS/RTM | Every FR/NFR/BR/DR has evidence confidence and a disposition; inventory coverage gaps and compound rows are recorded. |
| UML | Every material actor, state, message, and cardinality maps to a requirement/evidence item or is declared as a presentation abstraction. |
| Database design | Count rules, FK optionality, unique predicates, tenancy coverage, soft-delete behavior, and effective delete behavior have dispositions. |
| Final package | All Blockers are Verified; unresolved Major items are explicitly accepted for Conditional approval; required signers approve the same baseline. |

After an accepted correction: update the owning and downstream deliverables; search for stale terminology/IDs; run link/path, requirement-ID, inventory-ID, count-consistency, and Mermaid-rendering checks where applicable; perform focused technical verification when static evidence is insufficient; have a reviewer other than the author re-review it; update the open-question register and change log; then sign the new baseline.

## 9. Approval Standard Before School-Template Conversion

Allowed results are **Approved**, **Conditional**, and **Rejected**. Approval requires sign-off from product/domain, backend/architecture, database, QA/verification, security, DevOps/SRE, and documentation/academic owners for the same baseline. Payment/finance and frontend/Edge owners are required when their areas are in report scope. Conditional approval must list accepted unresolved question IDs, owners, and why they do not block conversion. The baseline is ready to convert only when product scope and terminology are agreed; all Blockers are Verified; Major items are resolved or explicitly accepted conditionally; every requirement has traceable evidence or a visible uncertainty label; diagrams agree with workflow and data documents; database counts and high-impact integrity questions are resolved or clearly disclosed; and remaining questions are visible report assumptions rather than hidden contradictions.
