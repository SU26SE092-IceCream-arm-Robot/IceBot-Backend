# SEP490 Template Structure Notes

**Purpose**: Read-only structural summary of the university templates in `D:\Temp\SEP490_templates\`, with a mapping to the current IceBot backend deliverables. This is a conversion reference, not a completed university report and not evidence that missing team/project information exists.

**Templates inspected**: the eight DOCX files named below. The original files were not modified.

## 1. Shared Template Conventions

Reports 1–6 use a capstone-report cover followed by:

1. **I. Record of Changes**
2. **II. Report-specific content**

Their change table has the columns `Date`, `A/M/D`, `In charge`, and `Change Description`, where A/M/D means Added/Modified/Deleted. Report 7 instead acts as the consolidated final document and begins with cover information, Acknowledgement, and Definitions and Acronyms before incorporating the final content of Reports 1–6.

Common conversion rules:

- Preserve the university DOCX as the formatting authority. Populate a copy later; do not rebuild the cover, headers, footers, section breaks, numbering, or styles from Markdown alone.
- Map Markdown headings to the existing Word heading styles so the table of contents and numbering can be regenerated in Word.
- Replace all bracketed instructions, angle-bracket examples, sample names, sample systems, and placeholder rows. Do not leave template guidance in the submitted report.
- Convert Mermaid diagrams to high-resolution SVG or PNG before insertion. Confirm fonts, arrow labels, line wrapping, page width, and grayscale readability after Word/PDF rendering.
- Convert Markdown tables into native Word tables. Repeat header rows across pages and avoid splitting a logical row where possible.
- Add captions and cross-references to figures and tables even where the old template only shows an example image.
- Keep requirement, use-case, business-rule, and evidence IDs stable during conversion.
- Add source/citation handling appropriate to the university rules. Repository paths are useful during drafting but are not automatically suitable as final academic citations.
- Populate the Record of Changes from the approved documentation baseline/change log, not from Git history alone.

## 2. Report 1 — Project Introduction

Template: `Report1_Project Introduction.docx`

### Required major sections

- I. Record of Changes
- II. Project Introduction
  - 1. Overview
    - 1.1 Project Information
    - 1.2 Project Team
  - 2. Product Background
  - 3. Existing Systems
    - one subsection per comparable system
  - 4. Business Opportunity
  - 5. Software Product Vision
  - 6. Project Scope & Limitations
    - 6.1 Major Features
    - 6.2 Limitations & Exclusions

### Important tables/diagrams expected

- Record of Changes table.
- Project Team table: full name, role, email, and mobile.
- Numbered major-feature list with stable feature IDs.
- Comparable-system analysis should cover link/source, actors, features, advantages, and disadvantages. The template does not prescribe one table, but a comparison table is the clearest format.

### Inputs already available from `deliverables/`

- A template-oriented draft already exists at `06_school_reports/report1_project_introduction/report1_project_introduction.md`.
- `01_project_introduction/project_introduction.md` provides the evidence-based overview, background, project context, problem interpretation, proposed solution, actors, scope, exclusions, main features, architecture summary, integrations, assumptions, and open questions.
- `00_repo_evidence/repo_truth_map.md`, `functional_inventory.md`, and `database_inventory.md` support technical claims.
- `05_review_checklists/evidence_review_final.md` and `05_team_review/` provide review findings and unresolved decisions.

### Inputs still missing from the team

- Official project name/code, group name, software type, institution/location/date, supervisor, and full member contact details.
- Customer/project-requester identity and the approved original business problem.
- Market/business opportunity evidence and strategic motivation; current backend evidence only supports an inferred technical problem statement.
- Named comparable systems with team-approved sources, evaluation criteria, screenshots if desired, and pros/cons.
- Approved product-vision statement.
- Final release scope, feature prioritization, and disposition of partial or excluded features.

### Markdown-to-DOCX notes

- Reorder the current introduction into the six template sections rather than copying its twelve working-draft sections verbatim.
- Keep `[Inferred]`, `[Assumption]`, `[Unclear]`, and `[Open Question]` visible until team decisions replace them.
- Convert the broad capability inventory into a concise numbered major-feature list; retain detailed FR coverage in Report 3.
- Insert project/team metadata directly into the template fields and native team table.

## 3. Report 2 — Project Management Plan

Template: `Report2_Project Management Plan.docx`

### Required major sections

- I. Record of Changes
- II. Project Management Plan
  - 1. Overview
    - 1.1 Scope & Estimation
    - 1.2 Project Objectives
    - 1.3 Project Risks
  - 2. Management Approach
    - 2.1 Project Process
    - 2.2 Quality Management
    - 2.3 Training Plan
  - 3. Project Deliverables
  - 4. Responsibility Assignments
  - 5. Project Communications
  - 6. Configuration Management
    - 6.1 Document Management
    - 6.2 Source Code Management
    - 6.3 Tools & Infrastructures

### Important tables/diagrams expected

- WBS/scope-estimation table with complexity and estimated man-days.
- Project objective/quality targets, including milestone timeliness, effort allocation, test coverage, and defect targets.
- Test-stage quality table: coverage, defect count/rate, and notes.
- Risk register: description, impact, probability, and response plan.
- Software-development process diagram and explanation.
- Training plan table.
- Deliverable/master schedule with due dates.
- Responsibility assignment matrix using D (Do), R (Review), S (Support), I (Informed).
- Communication plan table.
- Tools and infrastructure table.
- The template points to `Report2_Sample Project Schedule.pdf` for a more detailed schedule; RUP is recommended in the guide but not mandatory.

### Inputs already available from `deliverables/`

- The evidence inventories and SRS provide a candidate feature/WBS breakdown, but not estimates or assignments.
- `05_team_review/review_guide.md`, `team_review_checklist.md`, `change_log.md`, and `open_questions.md` provide documentation review, quality-control, issue, decision, and change-management inputs.
- `ARCHITECTURE.md`-derived content in the evidence files provides the current backend technology and architecture baseline.

### Inputs still missing from the team

- No Report 2 draft currently exists under `deliverables/06_school_reports/`.
- Approved development process, iteration plan, schedule, milestones, start/end dates, and project-tracking source.
- Work estimates, actual/available effort, cost assumptions, and allocation by activity.
- Measurable quality objectives and acceptance thresholds.
- Project risk register and response owners.
- Member skills/training needs and training schedule.
- Named responsibility assignments, communication cadence/tools, escalation route, and stakeholder list.
- Document/source-code management policies actually used by the team and the definitive tools/infrastructure list.

### Markdown-to-DOCX notes

- Do not derive man-days, risk probability, dates, or member responsibility from repository activity; these require team confirmation.
- Build WBS, risk, RACI-style responsibility, communications, and deliverables data as Markdown tables that match the DOCX columns exactly.
- Export the approved schedule from the scheduling tool as a legible image/PDF excerpt and keep the editable schedule as a separate release artifact.

## 4. Report 3 — Software Requirement Specification

Template: `Report3_Software Requirement Specification.docx`

### Required major sections

- I. Record of Changes
- II. Software Requirement Specification
  - 1. Product Overview
  - 2. User Requirements
    - 2.1 Actors
    - 2.2 Use Cases
      - 2.2.1 Diagram(s)
      - 2.2.2 Descriptions
  - 3. Functional Requirements
    - 3.1 System Functional Overview
      - 3.1.1 Screen Flow
      - 3.1.2 Screen Descriptions
      - 3.1.3 Screen Authorization
      - 3.1.4 Non-Screen Functions
      - 3.1.5 Entity Relationship Diagram
    - 3.2 onward: one section per feature, with function-level details
  - 4. Non-Functional Requirements
    - 4.1 External Interfaces
    - 4.2 Quality Attributes, including measurable usability, reliability, performance, and other applicable attributes
  - 5. Requirement Appendix
    - 5.1 Business Rules
    - 5.2 Common Requirements
    - 5.3 Application Messages List
    - 5.4 Other Requirements

### Important tables/diagrams expected

- System context diagram.
- Actor table.
- Use-case diagram(s) and use-case description table.
- Screen-flow diagram.
- Screen-description table and UI mock-ups/screenshots.
- Screen/feature authorization matrix by role and activity.
- Non-screen function table for services, APIs, batch/cron/background jobs, and other non-UI behavior.
- ERD and entity-description table.
- Per-function trigger, actors/roles, purpose, interface, processing, validation, business rules, normal/abnormal behavior, and screen layout where applicable.
- Business-rule table and application-message catalog.

### Inputs already available from `deliverables/`

- A template-oriented draft exists at `06_school_reports/report3_srs/report3_srs.md`.
- `02_srs/srs.md` provides product overview, actors, interfaces, 133 FRs, 25 NFRs, data requirements, business rules, and uncertainty notes.
- `02_srs/requirements_traceability_matrix.md` provides evidence/status traceability.
- `03_uml/use_case_diagram.md` and `03_uml/erd.md` provide initial use-case and entity-relationship diagrams.
- Other UML flow diagrams support order/payment/robot behaviors.
- Evidence inventories support endpoint, background-function, entity, and integration descriptions.

### Inputs still missing from the team

- Frontend/tablet/mobile repositories or approved UI scope, screen inventory, screen flow, screen descriptions, mock-ups/screenshots, and screen-level authorization.
- Full narrative use-case specifications with preconditions, postconditions, main flow, alternate/error flows, and business-rule references.
- Team-approved measurable usability, availability, MTBF/MTTR, performance, throughput, capacity, and defect targets.
- Approved application-message catalog and exact user-facing wording.
- Final API DTO/error/status-code contracts and complete permission coverage where currently marked uncertain.
- Runtime/test evidence needed to promote static `[Supported]` claims to verified behavior.

### Markdown-to-DOCX notes

- The university template is UI-oriented; preserve backend APIs, workers, MQTT consumers, webhooks, and reconciliation jobs under **Non-Screen Functions** rather than forcing them into screen sections.
- Group FRs by feature while retaining `FR-xxx`, `NFR-xxx`, and `BR-xx` IDs and RTM links.
- Insert the context/use-case/ERD diagrams as rendered figures and provide native Word actor/entity tables.
- Do not fabricate screen flows from API routes. Mark UI sections as team/frontend inputs until evidence is supplied.

## 5. Report 4 — Software Design Document

Template: `Report4_Software Design Document.docx`

### Required major sections

- I. Record of Changes
- II. Software Design Document
  - 1. System Design
    - 1.1 System Architecture
    - 1.2 Package Diagram
  - 2. Database Design
  - 3. Detailed Design
    - one subsection per feature/function
    - class diagram/class specifications
    - one or more sequence diagrams

### Important tables/diagrams expected

- Overall architecture diagram including subsystems, external systems, and their relationships, plus component explanations.
- Package diagram(s) per subsystem and package/namespace description table.
- Database relationship diagram and table-description table with primary and foreign keys.
- Feature-level class diagrams, class specifications, and sequence diagrams. Shared structures may be shown once and cross-referenced.

### Inputs already available from `deliverables/`

- A template-oriented draft exists at `06_school_reports/report4_sdd/report4_sdd.md`.
- Architecture evidence is available in `00_repo_evidence/repo_truth_map.md` and the project introduction/SRS.
- `03_uml/class_diagram.md`, `sequence_order_flow.md`, `sequence_robot_execution.md`, `activity_order_flow.md`, and `erd.md` provide core diagrams.
- `04_database_design/conceptual_database_design.md`, `logical_database_design.md`, and `physical_database_design.md` provide database design at three abstraction levels.
- `00_repo_evidence/database_inventory.md` supports table, key, constraint, index, and EF mapping claims.

### Inputs still missing from the team

- Approved system-boundary diagram covering all deployed applications/components, not only this backend repository.
- Frontend/mobile/tablet/Edge package and class design, where included in project scope.
- Complete feature-level class specifications and sequence coverage beyond the current order and robot-execution examples.
- Final database model reconciliation and resolution of documented cardinality/delete/count discrepancies.
- Team decisions on which implementation details belong in the academic SDD versus appendices.

### Markdown-to-DOCX notes

- Render Mermaid diagrams and verify that large class/ERD diagrams remain readable; split by bounded context if a single page is illegible.
- Convert database design into the template's relationship diagram and table-description format; distinguish conceptual entities, logical entities, and physical tables.
- Retain evidence qualifications. A diagram should not silently strengthen optionality, cardinality, transaction, or runtime claims.
- Use Word cross-references when a shared class/sequence design is reused by several features.

## 6. Report 5 — Test Documentation

Template: `Report5_Test Documentation.docx`

### Required major sections

- I. Record of Changes
- II. Testing Documentation
  - 1. Scope of Testing
  - 2. Test Strategy
    - 2.1 Testing Types
    - 2.2 Test Levels
    - 2.3 Supporting Tools
  - 3. Test Plan
    - 3.1 Human Resources
    - 3.2 Test Environment
    - 3.3 Test Milestones
  - 4. Test Cases
  - 5. Test Reports

### Important tables/diagrams expected

- Scope matrix identifying included/excluded features and NFRs.
- Test type versus level matrix (Unit, Integration, System, Acceptance).
- For each test type: objective, technique, and completion criteria.
- Supporting-tools table.
- Testing roles/responsibilities table.
- Test-environment table covering software, hardware, and infrastructure with versions.
- Test-milestone table.
- Unit-test cases in the companion `Report5_Unit Test.xls` template.
- Integration/System/Acceptance cases and tracking in `Report5_Test Report.xlsx` (the DOCX text uses the older `.xls` name).
- Test results, statistics, defect data, and analysis.

### Inputs already available from `deliverables/`

- The SRS and RTM provide requirement IDs, evidence status, and a basis for coverage planning.
- UML workflows and database constraints provide candidate integration, state-transition, concurrency, retry, and integrity scenarios.
- `05_team_review/team_review_checklist.md` and `open_questions.md` identify high-risk verification areas.
- Static repository evidence distinguishes implemented/partial capabilities, but it is not test execution evidence.

### Inputs still missing from the team

- No Report 5 draft, test-case workbook, executed test report, or defect list currently exists under `deliverables/`.
- Approved test scope, levels/types, acceptance/completion criteria, assigned testers, environment versions, tools, and milestones.
- Requirement-to-test mapping and actual unit/integration/system/acceptance test cases.
- Executed results, dates, build/baseline, pass/fail/blocked totals, coverage measurements, defect severity/status, evidence attachments, and analysis.
- Performance, reliability, security, IoT/Edge, payment-webhook, retry/idempotency, and partial-failure test evidence where claimed.

### Markdown-to-DOCX notes

- Keep detailed test cases and execution tracking in the provided spreadsheets; summarize scope, strategy, environment, milestones, results, and analysis in the DOCX.
- Preserve a stable mapping among FR/NFR IDs, test-case IDs, defect IDs, and build/baseline IDs.
- Do not convert static `[Supported]` status into “Passed.” Only recorded execution results may do that.
- Generate charts from verified test data only, then insert them as figures with the underlying totals stated in text/tables.

## 7. Report 6 — Software User Guides

Template: `Report6_Software User Guides.docx`

### Required major sections

- I. Record of Changes
- II. Release Package & User Guides
  - 1. Deliverable Package
  - 2. Installation Guides
    - 2.1 System Requirements
    - 2.2 Installation Instruction
  - 3. User Manual
    - 3.1 Overview
    - 3.2 onward: one subsection per workflow

### Important tables/diagrams expected

- Deliverable-package inventory with item description and version. Template examples include schedule/tracking, backlog, source code, database scripts, final report, test cases, defects, issues, and slides.
- System requirements/configuration list.
- Installation and configuration steps.
- User workflow diagrams.
- Step-by-step instructions with UI screenshots.

### Inputs already available from `deliverables/`

- Project Introduction, SRS, UML workflows, and architecture/database material can support the product overview and backend workflow explanation.
- Evidence inventories identify current integrations and technical components that may inform system prerequisites after deployment owners confirm them.
- The team-review package identifies unresolved deployment, operational, authorization, and UI-contract questions.

### Inputs still missing from the team

- No Report 6 draft or approved release-package manifest currently exists under `deliverables/`.
- Release version/build, source/database artifacts, backlog/schedule, test/defect/issue artifacts, slides, checksums, and package locations.
- Supported deployment profile, prerequisites, secrets/configuration procedure, database migration process, broker/object-storage/payment/Firebase setup, health checks, rollback, and troubleshooting instructions.
- Frontend/tablet/mobile/admin/Edge user workflows, screenshots, role-specific steps, and operational support procedures.
- Verified installation run and user-acceptance evidence.

### Markdown-to-DOCX notes

- Separate operator installation/runbook content from end-user workflow instructions.
- Use numbered procedural steps, prerequisites, expected results, warnings, and troubleshooting notes; do not rely on narrative paragraphs alone.
- Capture screenshots from the approved release build and redact secrets/personal data.
- Keep the release manifest version-specific so Report 6 can be reproduced and checked against Report 7's final package.

## 8. Report 7 — Final Project Report

Template: `Report7_Final Project Report.docx`

### Required major sections

- Cover/project metadata
- Acknowledgement
- Definition and Acronyms
- I. Project Introduction — final Report 1 content
- II. Project Management Plan — final Report 2 content
- III. Software Requirement Specification — final Report 3 content
- IV. Software Design Description — final Report 4 content
- V. Software Testing Documentation — final Report 5 content
- VI. Release Package & User Guides — final Report 6 content

The subsection structure within Parts I–VI mirrors the corresponding individual report templates.

### Important tables/diagrams expected

- Cover metadata: group, members/student identifiers, supervisor/external supervisor, and capstone project code.
- Definitions/acronyms table.
- All final approved tables and diagrams required by Reports 1–6.
- Final deliverable-package table listing schedule/tracking, backlog, source, database scripts, final report, test cases, defects, issues, and slides.

### Inputs already available from `deliverables/`

- Template-oriented drafts exist for Reports 1, 3, and 4 under `06_school_reports/`.
- Their source baselines and supporting evidence exist under `01_project_introduction/`, `02_srs/`, `03_uml/`, `04_database_design/`, and `00_repo_evidence/`.
- Review and open-question material exists under `05_review_checklists/` and `05_team_review/`.

### Inputs still missing from the team

- Approved final versions of Reports 1–6; Reports 2, 5, and 6 are not yet drafted in `deliverables/`.
- Cover metadata, acknowledgements, supervisor details, and approved glossary/acronyms.
- Final schedule/tracking, backlog, source/database release package, test cases/results, defects/issues, slides, and version identifiers.
- Resolution or explicit conditional acceptance of material open questions.
- Final academic citations, language review, diagram rendering, and supervisor/team approval.

### Markdown-to-DOCX notes

- Do not independently rewrite content during consolidation. Import the approved final content of Reports 1–6 and preserve IDs, decisions, figures, tables, and uncertainty disclosures.
- Resolve duplicated introductions/glossaries and normalize cross-references only after the six source reports are frozen.
- Regenerate the table of contents, lists of figures/tables if used, page numbers, and cross-references in Word.
- Perform a conversion-integrity comparison between Report 7 and the approved source-report baseline before PDF submission.

## 9. SEP490 Student Guide — Fall 2023

Template/reference: `SEP490 StudentGuide_Fall 2023.docx`

This file is a regulation and delivery guide rather than a report to complete.

### Required major sections in the guide

- I. General Introduction
  - Capstone Project description
  - The seven Capstone Project products
- II. Capstone Project Regulation
  - Common regulations
  - Conditions to apply
  - Reports of the Capstone Project
  - Milestone table
  - Evaluation of the Capstone Project
- III. Project Document Templates
  - Reports 1–7
  - Project schedule/task tracking
  - Project information tracking

### Important tables/diagrams/artifacts expected

- Milestone table covering weeks 1–15 and staged/iterative deliverables.
- Separate project schedule maintained in Microsoft Project/ProjectLibre or an equivalent scheduling workflow.
- Tracking data for WBS/work, issues, defects, and Q&A; the guide points to `Report3_Project Tracking.xlsx` but permits convenient team tools.
- Report 5 companion unit-test and integration/system/acceptance test workbooks.
- Iterative software packages with source code, database scripts, updated requirements/design/test documents, defects, and tracking.
- Final package with Report 7, product artifacts, test/defect records, schedule/tracking, and defense slides.

### Inputs already available from `deliverables/`

- Evidence-based and template-oriented documentation for substantial parts of Reports 1, 3, and 4.
- Team review/checklist/open-question/change-log artifacts that can support issue, Q&A, review, and documentation-change tracking.
- Repository evidence for backend functionality, architecture, data, API, IoT/robot, and documentation claims.

### Inputs still missing from the team

- Confirmation that the Fall 2023 guide and the 2021/2019-dated templates remain the applicable versions for the current cohort.
- Project schedule and week-by-week submission history, WBS/task tracker, backlog, issue/defect/Q&A records, iteration baselines, and supervisor feedback.
- Team membership eligibility/administrative data and meeting/attendance records where required operationally.
- Reports 2, 5, and 6; completed Reports 1, 3, and 4; software packages; database scripts; test artifacts; presentation; and final approvals.
- Decision on how the guide's “Report 7: Software Product/Implementation” terminology relates to the supplied `Report7_Final Project Report.docx`, which consolidates Reports 1–6 while the evaluation section calls Report 7 Implementation/Software Product.

### Markdown-to-DOCX notes

- Treat guide rules as constraints on all conversions: documentation and presentation are in English; reports are iterative and should be updated at later milestones; tracking and executable product artifacts remain separate deliverables.
- Preserve milestone/build identities so each report revision can be tied to the corresponding software package and test result.
- Do not infer compliance from the existence of Markdown files. Compliance requires the team to supply schedules, trackers, release artifacts, executed tests, and approvals.
- Verify current supervisor/course instructions before relying on historical dates, filenames, percentages, or workflow recommendations.

## 10. Current Readiness Summary

| Report | Existing baseline in `deliverables/` | Main remaining dependency |
|---|---|---|
| Report 1 | Template-oriented draft plus evidence-based introduction | Team/business metadata, comparable systems, approved opportunity/vision/scope |
| Report 2 | Supporting review/process material only | Entire team-owned management plan, estimates, schedule, roles, risks, communications |
| Report 3 | Template-oriented draft, SRS, RTM, UML/evidence | UI artifacts, detailed use cases, measurable quality targets, final contract decisions |
| Report 4 | Template-oriented draft, UML, database designs/evidence | Full-system/component coverage, detailed feature design, unresolved DB decisions |
| Report 5 | Requirements and candidate review scenarios only | Test plan/cases, environment, execution results, defects, analysis |
| Report 6 | Architecture/workflow background only | Versioned release manifest, installation runbook, screenshots, verified user workflows |
| Report 7 | Partial source content from Reports 1, 3, and 4 | Approved Reports 1–6 and complete final product/review metadata |
| Student Guide compliance | Some documentation/review artifacts | Current applicability confirmation, schedule/tracking, iterative packages, tests, presentation, approvals |
