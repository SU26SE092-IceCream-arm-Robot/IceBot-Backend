# Team Review Checklist — IceBot Backend Deliverables

**Purpose**: Practical role-based checks for correcting the evidence baseline before school-template conversion. RTM means Requirements Traceability Matrix. Do not mark an item complete merely because a question was recorded.

## 0. How to Use This Checklist

Record the baseline from `review_guide.md`, then assign a bounded scope to each reviewer.

| Assignment ID | Reviewer/role | Bounded context, FR/NFR range, diagram, or DB section | Due date | Baseline |
|---|---|---|---|---|
|  |  |  |  |  |

For every applicable item, record one result: **Reviewed—No Issue**, **Issue Found**, **Fix Applied**, **Verified**, or **N/A**. `N/A` requires rationale. Each Issue Found requires an issue/open-question ID, severity, owner, and disposition. Each Verified result requires exact evidence/output and a reviewer other than the change author.

| Checklist item/scope | Result | Evidence or output | Issue/question ID | Reviewer/date |
|---|---|---|---|---|
|  |  |  |  |  |

Required outputs may include an endpoint comparison, state/invariant matrix, actor-to-screen/API map, topic inventory, query inventory, count worksheet, test result, or discrepancy list. Review the whole assigned scope; sampling is allowed only when the issue record states the sampling rule and why exhaustive review is unnecessary.

## 1. Backend Developers

- [ ] Confirm each reviewed FR matches the actual controller/consumer, handler, domain rule, and persistence behavior named by its inventory IDs.
- [ ] Separate route existence from behavior, authorization, transactionality, idempotency, retry, and runtime/test verification.
- [ ] Review every `Partial`, `[Inferred]`, `[Unclear]`, and `[Needs Review]` backend claim in the assigned bounded context.
- [ ] Check that universal statements about scoped RBAC are supported for REST, GraphQL, and SignalR or remain open.
- [ ] Verify order/payment/dispatch/reconciliation state transitions, failure paths, terminal intervention states, and transaction boundaries.
- [ ] Verify REST versus MQTT responsibilities; do not infer transport priority from the existence of two handlers.
- [ ] Confirm background-job descriptions state what each job actually detects, retries, transitions, or escalates.
- [ ] Identify compound SRS/RTM rows whose parts require different confidence statuses; list each affected requirement ID and disposition.
- [ ] Check exact endpoint and DTO claims against authoritative source before removing uncertainty labels.
- [ ] Record missing test evidence; do not equate static wiring with a passing runtime workflow.

## 2. Frontend / Mobile / Tablet Developers

- [ ] Confirm the listed UI actors and clients match the actual applications and user journeys.
- [ ] Validate public/customer, authenticated-self-service, and management surface boundaries.
- [ ] Confirm order access-token use, checkout/payment status polling, retry affordances, and staff-support states.
- [ ] Check required request/response fields, enum/state names, pagination, validation errors, and status-code handling.
- [ ] Confirm SignalR channels/events and GraphQL reads used by clients; flag undocumented or unused contracts.
- [ ] Validate that the tablet never initiates robot execution directly and that offline/UI state claims are accurate.
- [ ] Identify UI features implied by the documents but absent from frontend implementation, and mark them `[Assumption]` or `[Open Question]`.
- [ ] Check terminology shown to users against backend state/field terminology and report wording.

## 3. Robot / IoT / Edge Developers

- [ ] Confirm the Cloud/Edge ownership boundary and which runtime behavior is outside this repository's evidence.
- [ ] Validate endpoint authentication modes, identity binding, request signing/mTLS, replay protection, and credential rotation descriptions.
- [ ] Confirm command wake-up, REST pull, acknowledgement, execution-report, telemetry, heartbeat, readiness, and production-sync paths.
- [ ] Validate MQTT topic, retained-message, payload-size, QoS/protocol, and shared-consumer claims against the real runtime contract.
- [ ] Walk through disconnection, duplicate delivery, stale command, timeout, restart, partial physical output, and unknown-outcome cases.
- [ ] Confirm execution evidence cardinality and timing shown in the sequence diagrams.
- [ ] Review dead-letter families and state which are replayable, manually resolvable, or unsupported.
- [ ] Validate deployment, rollback, configuration checksum, artifact provenance, and package-upgrade recovery flows.
- [ ] Flag assumptions that every kiosk has the same robot/device/controller topology.

## 4. Database Reviewer

- [ ] Reconcile mapped entity (`DbSet`), named physical table, migration-creation, model-snapshot, and live-schema counts using explicit counting rules.
- [ ] Verify ERD/logical cardinalities from FK nullability, unique constraints, and application invariants; do not infer mandatory parents from collection navigation alone.
- [ ] Inspect the final EF model/migration output to resolve global `Restrict` versus explicit `Cascade` behavior.
- [ ] Confirm every listed unique/filtered index and check constraint, including exact predicates and enum values.
- [ ] Enumerate composite tenant-consistency FK coverage and distinguish it from application-only validation.
- [ ] Audit soft-delete filters and `WhereNotDeleted()` use for the 12 excluded principal types.
- [ ] Review missing/soft lineage references, JSON schema-version conventions, `PublicKeyPem` length, and connection-string key divergence.
- [ ] Confirm high-volume index coverage, bounded-upsert versus append-only classifications, retention, and partitioning statements.
- [ ] Check history/audit-table consistency and clarify what “immutable” or “append-only” means at each enforcement layer.
- [ ] Validate manual migration steps and operational prerequisites without executing database mutations during documentation review.

## 5. Project Manager / Report Writer

- [ ] Confirm the team-approved business problem, target market, actors, release scope, and exclusions.
- [ ] Decide whether partial features are release commitments or reported limitations.
- [ ] Separate current technology/provider choices from permanent product constraints.
- [ ] Confirm the report boundary for frontend, Edge runtime, infrastructure, bootstrap, migrations, health, and operations.
- [ ] Ensure every assumption and unresolved decision remains visibly labeled; do not polish uncertainty out of the report.
- [ ] Use one glossary for actors, bounded contexts, workflows, states, and conceptual/physical data names.
- [ ] Keep current implementation facts separate from normative “shall” requirements and future roadmap statements.
- [ ] Confirm which conceptual abstractions and diagram detail level are appropriate for the school report.
- [ ] Ensure reported feature/entity/table counts use an approved counting rule and source.
- [ ] Assign owners and deadlines for all material entries in `open_questions.md`.

## 6. Security / Authentication Reviewer

- [ ] Produce an RBAC coverage result for the assigned REST, GraphQL, and SignalR surfaces, including tenant-scope checks.
- [ ] Record JWT, refresh-token, endpoint, MQTT, mTLS/ECDSA, and bootstrap-credential lifecycle rules and gaps.
- [ ] Verify webhook signature, timestamp/replay, duplicate-callback, and sensitive diagnostic exposure claims.
- [ ] Review raw provider/device/sync payload storage, logging, response visibility, and secret-masking claims by role.
- [ ] Independently disposition RP-01–RP-07 and related security questions; do not self-approve solely from implementation ownership.

## 7. QA / Test / Verification Reviewer

- [ ] Map the assigned FR/NFR/BR/DR range to unit, integration, contract, or manual verification evidence; record unmapped items.
- [ ] Record test environment, baseline, command/scenario, result, and evidence location.
- [ ] Cover negative, concurrency, retry, idempotency, stale-state, partial-failure, and recovery cases applicable to the workflow.
- [ ] Verify that static `[Supported]` wording is not presented as executed acceptance evidence.
- [ ] Independently re-review fixes and mark Verified only when wording, evidence, and downstream consistency all pass.

## 8. DevOps / SRE / Deployment Reviewer

- [ ] Record deployment profiles and identify mandatory versus optional hosted jobs for each profile.
- [ ] Verify runtime/design-time connection keys, migrations/manual steps, and evidence of successful deployment execution.
- [ ] Validate health/readiness/info/diagnostic behavior and database, broker, object-storage, and provider dependency handling.
- [ ] Disposition backup/restore, RPO/RTO, availability, monitoring, and incident-response requirement gaps as decisions, not current implementation claims.
- [ ] Confirm startup validation, configuration sources, secret delivery, and partial-failure recovery procedures.

## 9. Product / Business Domain Owner

- [ ] Approve the business problem, actors, target release scope, exclusions, and accepted limitations.
- [ ] Decide payment, late-payment, refund, production-incident, compensation, and manual-intervention semantics.
- [ ] Decide which providers/platform choices are product constraints versus replaceable/current configuration.
- [ ] Distinguish target-release decisions from future roadmap questions and report-presentation choices.
- [ ] Approve Conditional dispositions for unresolved Major product questions.

## 10. Payment / Finance Operations Reviewer

- [ ] Validate primary/duplicate/late payment, callback reconciliation, settlement-disposition, and intervention behavior.
- [ ] Confirm refund request, approval/rejection/cancellation, provider/manual processing, and accounting evidence expectations.
- [ ] Review payment/refund/production-incident state alignment and operator responsibilities.
- [ ] Confirm which financial events require immutable evidence, deduplication, reconciliation, and audit visibility.

## 11. Documentation / Academic Compliance Reviewer

- [ ] Verify school-template sections, required notation, citations, path references, and diagram rendering.
- [ ] Maintain and obtain owner approval for the bilingual glossary and state/actor/context terminology.
- [ ] Confirm technical meaning and uncertainty labels survive formatting conversion.
- [ ] Verify all links, headings, FR/NFR/BR/DR IDs, inventory IDs, and RTM terminology mechanically where possible.
- [ ] Record presentation-only decisions separately from technical correctness blockers.

## 12. Optional Privacy / Data-Governance Reviewer

- [ ] If the report makes privacy, retention, logging, or compliance claims, approve personal-data classification, retention/purge rules, diagnostic exposure, and access/audit expectations.
- [ ] Mark this role N/A with rationale when no such claims are in report scope.

## 13. Whole Team Final Review

### Scope and correctness

- [ ] Introduction, SRS, RTM, UML, and database design describe the same release boundary and actors.
- [ ] No feature, entity, provider guarantee, runtime result, or business goal has been invented.
- [ ] `Partial` features are shown as limitations, not confirmed-complete scope.
- [ ] Normal, failure, retry, partial-success, compensation, and manual-intervention paths agree across text and diagrams.

### Evidence and confidence

- [ ] Every material claim has a usable evidence/decision reference or an uncertainty label.
- [ ] `[Supported]` is understood as static evidence unless runtime/test evidence is explicitly linked.
- [ ] Compound claims use the weakest material status or have been split.
- [ ] Every `[Needs Review]` item is corrected, deliberately deferred, or converted to an owned open question.
- [ ] Every remaining `[Inferred]`, `[Assumption]`, and `[Unclear]` claim is acceptable and visible.

### Traceability and consistency

- [ ] Every SRS requirement appears in the RTM with the correct ID, evidence, related inventory IDs/entities, and confidence.
- [ ] Inventory coverage gaps and the 260/265 count discrepancy are resolved or disclosed.
- [ ] UML actor names, states, interactions, associations, and cardinalities match the reviewed SRS/database baseline.
- [ ] Conceptual, logical, ERD, and physical database names and relationship meanings are consistent at their intended abstraction levels.
- [ ] Cross-document terminology and acronyms use one approved wording.
- [ ] Mechanical verification covers requirement/inventory ID coverage, links/paths, headings, stale terms, count consistency, and Mermaid rendering where applicable.

### Readiness to convert

- [ ] High-impact security, payment/order, robot/Edge, tenancy, and delete-integrity questions have owners and dispositions.
- [ ] Database counts and physical-design limitations are resolved or explicitly stated with their counting/evidence rules.
- [ ] Open questions retained in the final report are genuine disclosed assumptions, not avoidable documentation contradictions.
- [ ] Changes are recorded in `change_log.md`, including evidence impact and downstream follow-up.
- [ ] The team has approved this baseline for formatting conversion; conversion will not silently change technical meaning.

## Review Sign-Off

Allowed results: **Approved**, **Conditional**, or **Rejected**. Conditional requires remaining question IDs, owner/due date, and a written reason each item does not block conversion. Required signers are Product/domain, Backend/architecture, Database, Security, QA/verification, DevOps/SRE, and Documentation/academic. Frontend, Robot/Edge, and Payment/finance are required when their areas are included. The final Documentation/academic approver authorizes school-template conversion only after all required signers reference the same baseline.

| Area | Required? | Reviewer | Date | Baseline | Result | Remaining question IDs / conditional rationale |
|---|---|---|---|---|---|---|
| Product/business domain | Yes |  |  |  | Pending |  |
| Backend/architecture | Yes |  |  |  | Pending |  |
| Frontend/mobile/tablet | If in scope |  |  |  | Pending |  |
| Robot/IoT/Edge | If in scope |  |  |  | Pending |  |
| Database | Yes |  |  |  | Pending |  |
| Security/authentication | Yes |  |  |  | Pending |  |
| QA/verification | Yes |  |  |  | Pending |  |
| DevOps/SRE | Yes |  |  |  | Pending |  |
| Payment/finance operations | If in scope |  |  |  | Pending |  |
| Privacy/data governance | If applicable |  |  |  | Pending |  |
| Documentation/academic | Yes; final conversion approver |  |  |  | Pending |  |
