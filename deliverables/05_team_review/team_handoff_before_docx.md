# Team Handoff Checklist Before DOCX Conversion

## 1. What Has Been Completed

- [x] Reports 1, 3, 4, 5, 6, and 7 follow the university's major section structure.
- [x] The backend evidence baseline, SRS, RTM, UML, and database-design drafts exist.
- [x] Report 3 contains FR-001–FR-135, including Inventory Sensor Observations and Production Program Bindings.
- [x] Report 4 contains architecture, package, database, and detailed-design sections.
- [x] Report 5 contains a planned test strategy and test-case catalogue; no execution result is claimed.
- [x] Report 6 contains installation and role-based workflow drafts without inventing UI screens or environment values.
- [x] Report 7 contains the final-report structure, protected PMP placeholders, qualified database counts, and consolidation markers.
- [x] Unknown UI, deployment, team, test, and physical robot details remain visibly labeled.

**Current gate:** The package is suitable for team review and draft DOCX layout testing, but it is **not ready for final DOCX submission**. Resolve or formally defer the blockers in `final_documentation_readiness_audit.md` first.

## 2. What Each Team Member Should Review

Assign names in the team tracker; do not add names to reports until confirmed.

### Backend developer

- [ ] Confirm current APIs, actors, policies, DTO boundaries, background jobs, payment callbacks, and error/status behavior in Reports 3 and 6.
- [ ] Resolve stale evidence references and the notification view/manage/privacy boundary.
- [ ] Confirm release/deployment concurrency, reason, audit, and rollback contracts.

### Frontend/mobile/tablet developer

- [ ] Supply the real screen inventory, navigation flow, role visibility, fields, validations, messages, and screenshots.
- [ ] Confirm runtime-menu source and ETag/cache client behavior.
- [ ] Validate account, catalog, order, payment, operations, and incident workflows against the implemented UI.

### Robot/IoT/Edge developer

- [ ] Confirm schema-v5 compatibility, MQTT/REST behavior, inventory-observation handling, command acknowledgement/reporting, and reconnect/recovery behavior.
- [ ] Review robot/configuration workflows without treating Cloud evidence as proof of physical output or safety.
- [ ] Provide approved device/Edge installation, configuration, verification, and troubleshooting procedures.

### Database reviewer

- [ ] Reconcile the EF model snapshot/live schema and approve table, FK, index, nullability, delete, JSON, and migration details.
- [ ] Correct remaining UML/database cardinality conflicts and the route-binding catalogue.
- [ ] Confirm migration order, manual steps, backup, rollback/forward-fix, and deployed-schema verification.

### Tester/QA reviewer

- [ ] Convert Report 5's broad planned catalogue into an executable STM with stable requirement/subclaim-to-test mappings.
- [ ] Approve tools, environment, fixtures, cleanup, entry/exit criteria, priorities, and acceptance thresholds.
- [ ] Execute tests and attach immutable result/defect/coverage evidence.

### Project manager/report owner

- [ ] Complete Report 2/PMP and all project/team metadata.
- [ ] Assign owners and dispositions to every material open question.
- [ ] Approve the evidence baseline/commit, report revision history, final terminology, references, and consolidation into Report 7.

## 3. Placeholders Requiring Team Input

- [ ] Official project name, group name, project code, software type, institution, location, month/year.
- [ ] Supervisor, external supervisor, lecturer/customer/requester where applicable.
- [ ] Member names, student identifiers, roles, emails, and phone numbers required by the template.
- [ ] Approved business problem, opportunity, target users/market, comparable systems and sources, vision, scope, limitations, and priorities.
- [ ] Acknowledgement, approved definitions/acronyms, references, and appendix items.
- [ ] Record-of-Changes dates, versions, reasons, reviewers, and approval owners.
- [ ] Approved baseline commit/workspace snapshot and final template version for the current cohort.

Keep unresolved entries as `[Needs Team Review]`, `[Needs Team/UI Review]`, or `[Team-Owned Placeholder]`; do not silently delete them.

## 4. Report 2 / PMP — Team-Owned Items

- [ ] Project overview, objectives, success criteria, assumptions, constraints, and dependencies.
- [ ] Management/development approach and lifecycle/process.
- [ ] Scope/WBS, estimation method, effort/cost assumptions, milestones, and master schedule.
- [ ] Team organization, responsibilities, assignments, decision authority, and external stakeholders.
- [ ] Risk register with probability, impact, mitigation, contingency, owner, and review cadence.
- [ ] Quality plan, review gates, testing responsibility, acceptance/sign-off, and defect handling.
- [ ] Communication plan: meetings, channels, reporting, escalation, minutes, and approvals.
- [ ] Configuration/change management: repositories, branches, versions, baselines, releases, documents, and change approval.
- [ ] Training/resource/procurement needs and monitoring/control approach.

All Report 2 content remains `[Team-Owned Placeholder]` until supplied and approved by the team.

## 5. Screenshots or UI Images Still Needed

- [ ] Approved system/context and screen-flow figure where required.
- [ ] Authentication and current-session management.
- [ ] Organization/account/role administration.
- [ ] Catalog, recipe, menu, runtime-menu, inventory, and readiness workflows.
- [ ] Checkout, PayOS payment/status, order tracking, cancellation, and refund/incident handling.
- [ ] Device/kiosk monitoring, alerts, maintenance assignment, notification delivery, and diagnostics where authorized.
- [ ] Robot artifact/program authoring, import, binding, release, deployment, rollback, and monitoring.
- [ ] Installation verification and representative error/troubleshooting states.

For every image: confirm it comes from the approved build, add a figure number/caption/source, crop irrelevant content, and redact credentials, tokens, personal data, internal hosts, and sensitive diagnostics.

## 6. Test Execution Data Still Needed

- [ ] Approved build/commit, environment ID, database/migration baseline, configuration profile, and test date.
- [ ] Tester and reviewer assignments.
- [ ] Executable steps, fixtures/test data, expected results, cleanup/reset, and evidence location for every approved case.
- [ ] Actual result, pass/fail/blocked status, logs/screenshots/API evidence, duration, and defect link.
- [ ] Requirement/subclaim coverage matrix and untested/deferred justification.
- [ ] Defect summary by severity/status, retest/regression evidence, and known issues.
- [ ] Acceptance criteria evaluation and authorized sign-off.

Keep all unexecuted cases `[Planned]` and all report statistics, defects, coverage results, and sign-off `[To Be Updated After Test Execution]`.

## 7. Deployment and Installation Details Still Needed

- [ ] Supported runtime/SDK, operating system/container platform, PostgreSQL, broker, object storage, and Edge/device requirements.
- [ ] Approved deployment topology, domains, ports, TLS/certificates, firewall/network rules, and health/readiness criteria.
- [ ] Non-secret configuration catalogue and secret provisioning/rotation/recovery procedure.
- [ ] PayOS, Firebase/FCM, MQTT, MinIO, and Edge/robot configuration procedures using placeholders—not real secrets.
- [ ] Versioned release manifest: source revision, artifacts/images, checksums/digests, reports, migrations, owner, and approval.
- [ ] Exact migration IDs/order, manual-step procedure, preflight, backup, rollback/forward-fix, and schema reconciliation.
- [ ] Tested build/package/deploy/start/stop/rollback instructions and expected verification output.
- [ ] CI/GHCR/NetBird/SSH workflow ownership, runners, approvals, secrets, targets, run evidence, and failure recovery.
- [ ] Monitoring, logs, alerts, support contacts, escalation, backup/restore, RPO/RTO, and incident response.

Repository workflow definitions are not proof that deployment succeeded. Keep unverified environment and execution details `[Needs Team Review]`.

## 8. Checks Required Before DOCX Conversion

- [ ] Close or explicitly defer every Critical/Major item in `final_documentation_readiness_audit.md`.
- [ ] Stop using pre-sync generated evidence packs as current evidence.
- [ ] Align SRS, RTM, UML, database design, and Reports 3–7 on cache behavior, actors, entities, cardinalities, notification policy, and Production Configuration.
- [ ] Correct Report 1 capability-count wording and approve the authoritative count/disposition.
- [ ] Confirm Report 7 has imported approved Reports 1, 3, 4, 5, and 6 content; remove each `[Consolidation Required]` marker only after import verification.
- [ ] Keep PMP placeholders until Report 2 is supplied; keep unexecuted test results unchanged.
- [ ] Confirm the university template/student-guide version with the supervisor or report owner.
- [ ] Resolve headings, numbering, table/figure captions, cross-references, glossary terms, citations, and reference formatting.
- [ ] Render every Mermaid diagram to an approved image and visually inspect labels, cardinalities, line breaks, and readability.
- [ ] Check page breaks, margins, fonts, cover pages, headers/footers, table of contents, lists of figures/tables, and appendix numbering in DOCX.
- [ ] Run a final secret/personal-data scan and verify all placeholders and uncertainty labels have an explicit disposition.

## 9. Recommended Final Review Order

1. **Freeze evidence:** approve the backend commit/snapshot and current evidence set; mark superseded packs.
2. **Resolve decisions:** triage `open_questions.md`, assign owners, and record accepted/deferred outcomes.
3. **Correct baselines:** align Project Introduction, SRS/RTM, UML, and database design.
4. **Approve Report 1:** metadata, business content, scope, and capability-count wording.
5. **Approve Report 3:** requirements, actors, UI placeholders, NFRs, messages, evidence, and RTM/STM links.
6. **Approve Report 4:** architecture, detailed flows, UML, database specifications, and physical boundaries.
7. **Execute and approve Report 5:** environment, cases, evidence, defects, coverage, and sign-off.
8. **Test and approve Report 6:** release manifest, installation/runbook, screenshots, workflows, and troubleshooting.
9. **Complete Report 2/PMP:** team-owned management content and approvals.
10. **Consolidate Report 7:** import only approved owning-report content and preserve all qualifications.
11. **Convert to DOCX:** render diagrams, apply the confirmed template, and perform layout/citation/privacy checks.
12. **Whole-team sign-off:** verify content, attachments, version/checksum, submission package, and supervisor requirements.
