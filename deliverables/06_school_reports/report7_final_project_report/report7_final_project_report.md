# CAPSTONE PROJECT REPORT

## REPORT 7 — FINAL PROJECT REPORT

### Cover Page

**Project name:** `[Official project name — Needs Team Review]`

**Working product name:** IceBot Backend

**Group name:** `[Group name — Needs Team Review]`

**Group members:**

| No. | Full name | Student ID | Role |
|---:|---|---|---|
| 1 | `[Team member — Needs Team Review]` | `[Student ID — Needs Team Review]` | `[Project role — Needs Team Review]` |

`[Add one approved row for each group member — Needs Team Review]`

**Supervisor:** `[Supervisor name — Needs Team Review]`

**External supervisor:** `[External supervisor name or Not Applicable — Needs Team Review]`

**Capstone project code:** `[Capstone project code — Needs Team Review]`

**Month/year:** `[Month and year — Needs Team Review]`

## Acknowledgement

`[Team-Owned Placeholder]` The project team must provide and approve the acknowledgement. It should identify the people and organizations that materially supported the project, use the university's expected formal style, and avoid inventing names, roles, or contributions.

## Definition and Acronyms

| Term / Acronym | Definition in this report |
|---|---|
| API | Application Programming Interface. |
| Cloud backend | The IceBot backend represented by the evidence in this repository. |
| DBMS | Database Management System. |
| ECDSA | Elliptic Curve Digital Signature Algorithm; used by the evidenced low-cost controller request-authentication profile. |
| Edge / Local Edge Backend | A separate local system that pulls Cloud commands, coordinates local execution, and reports evidence. Its implementation is outside this repository. |
| EF Core | Entity Framework Core, used for persistence and migrations. |
| FCM | Firebase Cloud Messaging. |
| FK | Foreign key. |
| IoT | Internet of Things. |
| JWT | JSON Web Token. |
| Kiosk | A tenant-scoped vending location/unit represented by the backend domain. |
| MQTT | Message Queuing Telemetry Transport; used for best-effort wake-up and supported Edge uplink behavior. |
| mTLS | Mutual Transport Layer Security; used by the evidenced Full Edge authentication profile. |
| NFR | Non-functional requirement. |
| PayOS | The external payment provider integrated by the backend. |
| PK | Primary key. |
| RBAC | Role-Based Access Control. |
| REST | Representational State Transfer. |
| RTM | Requirements Traceability Matrix; the current baseline is `requirements_traceability_matrix.md`. |
| STM | `[Needs Team Review]` The university/team must confirm whether this means Software Test Matrix, Software Traceability Matrix, or another test-traceability artifact. It is not treated as synonymous with the RTM in this draft. |
| SDD | Software Design Document. |
| SRS | Software Requirement Specification. |
| SignalR | Realtime notification technology used by the backend hubs. |
| UML | Unified Modeling Language. |

# I. Project Introduction

## 1. Overview

### 1.1 Project Information

IceBot Backend is the Cloud backend for a multi-location automated vending platform. It provides tenant-aware management, identity and authorization, product and menu configuration, inventory tracking, customer ordering, payment integration, operational monitoring, production configuration, and Cloud-to-Edge contracts. The backend records commands and reported execution evidence; it does not independently prove physical robot motion or product quality.

The formal project identity, participating organization, team information, project code, delivery dates, and approval information are `[Needs Team Review]`.

### 1.2 Project Team

The approved member names, student identifiers, project roles, responsibility assignments, supervisor relationships, and contact/communication details are `[Needs Team Review]`. No team composition is inferred from repository history.

## 2. Product Background

The product addresses the backend coordination required when vending operations combine business administration with automated production. Relevant concerns include scoped access across organizations, stores, and kiosks; consistent menu and pricing data; payment confirmation; durable dispatch; Edge connectivity; device and inventory state; operational intervention; and auditable execution evidence.

The repository establishes the backend side of this platform. Client applications, Local Edge implementation, physical robot control, and final deployment procedures require evidence from their owning teams.

## 3. Existing Systems

The approved competitor or comparable-system study has not been supplied. Names, sources, verified features, advantages, disadvantages, screenshots, and comparison criteria remain `[Needs Team Review]`.

### 3.1 `[Comparable System 1 — Needs Team Review]`

`[Team-Owned Placeholder]` Supply the approved name, source, selection rationale, verified features, advantages, disadvantages, comparison criteria, and properly attributed screenshot/reference.

### 3.2 `[Comparable System 2 — Needs Team Review]`

`[Team-Owned Placeholder]` Supply the approved name, source, selection rationale, verified features, advantages, disadvantages, comparison criteria, and properly attributed screenshot/reference.

The current project can be distinguished at a technical level by the combination of multi-tenant business functions, production configuration, durable Cloud-to-Edge commands, Edge acknowledgement/report contracts, and operations support. This statement describes the evidenced solution and is not a market-superiority claim.

## 4. Business Opportunity

The project provides a centralized backend foundation for managing automated vending locations and coordinating customer, payment, operational, and production workflows. Potential value includes consistent configuration, controlled authorization, traceable order/payment state, operational visibility, and integration with Edge execution environments.

`[Needs Team Review]` The team must validate the target customer, quantified business problem, market evidence, deployment model, commercial assumptions, success indicators, and stakeholder acceptance criteria.

## 5. Software Product Vision

The product vision is to provide a reliable Cloud backend through which authorized operators configure and monitor vending operations while customers place and pay for orders and Edge systems perform local execution under explicit contracts. The Cloud remains authoritative for supported business state and durable dispatch; the Edge remains responsible for local device/robot coordination and physical execution.

## 6. Project Scope & Limitations

### 6.1 Major Features

The documented scope covers:

- identity, session, account, role, and scoped authorization functions;
- organization, store, kiosk, device, and execution-endpoint management;
- catalog, recipe, sales catalog, runtime menu, inventory, and readiness functions;
- checkout, order lifecycle, payment sessions, signed PayOS callbacks, reconciliation, and manual refund handling;
- alerts, maintenance tickets, operational diagnostics, notifications, and dashboards;
- robot artifact/program authoring, configuration releases, deployments, production packages, and related reconciliation;
- durable Edge command pull/acknowledgement, execution evidence, MQTT wake-up/uplink, sync, and background processing; and
- PostgreSQL persistence, object metadata, realtime notifications, GraphQL management reads, and external adapters where evidenced.

### 6.2 Limitations & Exclusions

Limitations and exclusions include:

- no evidenced frontend/mobile/tablet implementation or approved screenshots in this repository;
- no Local Edge Backend or physical robot implementation in this repository;
- no independent Cloud verification of physical robot motion, dispensed quantity, product quality, or safety;
- no approved production topology, credentials, domains, exact environment values, or final installation commands;
- unresolved authorization, database, payment, Edge, operational, and lifecycle questions listed in the team review package; and
- no executed test results or formal acceptance evidence in the current Report 5 draft.

# II. Project Management Plan

> `[Team-Owned Placeholder]` Report 2 — Project Management Plan has not been supplied in the school-report deliverables. This entire section must be completed and approved by the project team. No names, dates, estimates, schedules, assignments, communication commitments, or management outcomes are asserted below.

## 1. Overview

### 1.1 Scope & Estimation

`[Team-Owned Placeholder]` Provide the approved work-breakdown structure, scope baseline, estimation method, complexity, estimated/actual effort, cost assumptions, available capacity, and variance evidence. Do not derive man-days or progress from repository activity.

### 1.2 Project Objectives

`[Team-Owned Placeholder]` Provide approved project and quality objectives with measurable targets, owners, due dates, measurement sources, and acceptance criteria. Include milestone timeliness, effort allocation, test coverage, and defect targets only when supported by team records.

### 1.3 Project Risks

`[Team-Owned Placeholder]` Insert the approved risk register with risk description, cause, impact, probability, priority, prevention/response, contingency, owner, due date, and current status.

## 2. Management Approach

### 2.1 Project Process

`[Team-Owned Placeholder]` Document the process actually selected and used, iteration/release approach, process diagram, scope/change control, decision ownership, review gates, and acceptance process. Distinguish planned practices from practices supported by project records.

### 2.2 Quality Management

`[Team-Owned Placeholder]` Define review and quality gates, test stages, coverage and defect measures, severity rules, acceptance thresholds, corrective actions, reporting cadence, and accountable approvers. Do not copy planned Report 5 cases as executed quality results.

### 2.3 Training Plan

`[Team-Owned Placeholder]` Identify approved skill gaps, training activities, participants, trainer/source, planned/actual dates, expected outcome, and completion evidence.

## 3. Project Deliverables

`[Team-Owned Placeholder]` Insert the approved deliverable/master schedule and reference the detailed Gantt/project-schedule artifact. Exact dates, estimates, completion percentages, and variances require team evidence.

| Deliverable / work package | Planned start/end | Owner | Dependency | Acceptance evidence | Status |
|---|---|---|---|---|---|
| `[Needs Team Review]` | `[Needs Team Review]` | `[Needs Team Review]` | `[Needs Team Review]` | `[Needs Team Review]` | `[Needs Team Review]` |

## 4. Responsibility Assignments

`[Team-Owned Placeholder]` Add the approved organization chart, member roles, supervisor/customer relationships, authority boundaries, and responsibility-assignment matrix using the university D (Do), R (Review), S (Support), and I (Informed) notation.

| Work/deliverable | Team member or role | D/R/S/I | Evidence / approval boundary |
|---|---|:---:|---|
| `[Needs Team Review]` | `[Needs Team Review]` | `[Needs Team Review]` | `[Needs Team Review]` |

## 5. Project Communications

`[Team-Owned Placeholder]` Define meeting types, frequency, participants, channels, records, reporting, escalation, stakeholder feedback, and approval procedures. Do not include personal contact information without team approval.

| Communication / meeting | Purpose | Participants | Frequency / trigger | Channel / record | Owner / escalation |
|---|---|---|---|---|---|
| `[Needs Team Review]` | `[Needs Team Review]` | `[Needs Team Review]` | `[Needs Team Review]` | `[Needs Team Review]` | `[Needs Team Review]` |

## 6. Configuration Management

### 6.1 Document Management

`[Team-Owned Placeholder]` Define document ownership, locations, naming/versioning, review/approval, change records, baselines, access, backup, retention, and Report 7 consolidation control.

### 6.2 Source Code Management

`[Team-Owned Placeholder]` Define repositories, branching, commits/reviews, access, release tagging, build/source traceability, backup, and change-control practices actually used by the team.

### 6.3 Tools & Infrastructures

`[Team-Owned Placeholder]` List approved development, planning, communication, design, testing, CI/CD, hosting, database, broker, object-storage, and monitoring tools with version/purpose/owner where known. Report 6 candidate controls do not establish the team's management process.

# III. Software Requirement Specification

## 1. Product Overview

### Overall Product Description

IceBot Backend is an ASP.NET Core Cloud backend organized around domain/application/infrastructure/WebAPI responsibilities and multiple bounded contexts. It exposes evidenced REST, GraphQL, SignalR, payment webhook, Edge REST, MQTT, synchronization, and hosted-job surfaces. PostgreSQL is the primary relational database; object storage is used for robot artifact binaries while the database stores associated metadata.

### System Context

External participants include customers and client applications, management users, PayOS, Firebase/Google identity, FCM, PostgreSQL, MinIO/S3-compatible storage, an MQTT broker, Local Edge systems, and physical devices/robots outside the backend boundary. Exact operating environments and client/Edge behavior remain `[Needs Team Review]` where not evidenced.

`[Consolidation Required]` Import the approved Report 3 system-context diagram and its caption here without changing system boundaries or uncertainty labels.

### External Actors and Systems

The actor and external-system details remain in Report 3. They must be imported with their approved definitions, responsibilities, interfaces, and evidence qualifications before final submission.

### Known Assumptions and Dependencies

Frontend/tablet, Local Edge, robot/device, provider, production-environment, and measurable quality assumptions remain `[Needs Team Review]` except where the owning source report marks a narrower claim as supported.

## 2. User Requirements

### 2.1 Actors

The actor baseline includes Customer, SystemAdmin, OrgAdmin, Manager, Staff, Technician, Tablet/Kiosk Client, Local Edge Backend, PayOS, and Cloud background processing. Role labels summarize expected participation; the exact backend policy and tenant scope govern authorization and remain subject to the approved authorization matrix.

At a high level, customers browse an approved runtime menu, place and pay for orders, and observe eligible status/cancellation paths. Management users administer scoped business and operational resources. Technical users configure devices, robot artifacts, releases, and deployments where authorized. PayOS supplies payment-session/callback behavior. Edge systems authenticate, pull and acknowledge durable commands, and submit execution evidence.

Concrete UI screens, navigation, client-side authorization presentation, messages, and screenshots are `[Needs Team/UI Review]`; API behavior must not be presented as confirmed UI behavior.

### 2.2 Use Cases

`[Consolidation Required]` Import the approved Report 3 use-case diagram(s) and use-case description table here. Preserve actors, FR mappings, boundary notes, and `[Needs Team/UI Review]` qualifications; do not infer UI flows from backend API routes.

## 3. Functional Requirements

### 3.1 System Functional Overview

#### 3.1.1 Screen Flow

`[Needs Team/UI Review]` Import the approved client screen-flow diagram. No frontend implementation or approved screen navigation is established by this backend repository.

#### 3.1.2 Screen Descriptions

`[Needs Team/UI Review]` Import approved screen names, purposes, actors, fields/actions, validation/error behavior, and UI-build references.

#### 3.1.3 Screen Authorization

`[Needs Team/UI Review]` Import the approved screen/feature authorization matrix. UI visibility must not replace backend policy and tenant-scope enforcement.

#### 3.1.4 Non-Screen Functions

`[Consolidation Required]` Import Report 3's API, provider callback, MQTT, SignalR, GraphQL, synchronization, and hosted/background function table with exact FR mappings.

#### 3.1.5 Entity Relationship Diagram

`[Consolidation Required]` Import the approved Report 3 ERD and entity-description table. Preserve all cardinality, optionality, and database-reconciliation qualifications.

### 3.2 Feature Sections

The synchronized SRS defines FR-001 through FR-135. The consolidated feature coverage is:

| Requirement range | Feature area | Consolidated capability |
|---|---|---|
| FR-001–FR-016 | Identity | Login, current-session listing/revocation, recovery/profile, push registrations, organization-owned accounts, roles, effective access, and scoped RBAC. |
| FR-017–FR-021 | Tenants | Organization, store, kiosk, franchise-onboarding, and scope-tree functions. |
| FR-022–FR-032 | Devices | Device/endpoint lifecycle, credentials, heartbeat, events, telemetry, readiness, reconciliation, and status reads. |
| FR-033–FR-047 | Catalog and Sales Catalog | Ingredients, products, variants/options, recipes, menus, runtime projections, and sellability/routing evaluation. |
| FR-048–FR-056 | Inventory | Dispensers, rebind/refill/adjustment/consumption, histories, summaries, readiness, and calibration. |
| FR-057–FR-067 | Orders | Checkout, customer/management lifecycle, redispatch/remake, fulfillment evidence, incidents, diagnostics, and realtime notices. |
| FR-068–FR-078 | Payments | Sessions, status, PayOS webhook, reconciliation, diagnostics, payment methods, refunds, and notifications. |
| FR-079–FR-087 | Operations | Alerts, tickets, operation logs, delivery diagnostics, and realtime operations events. |
| FR-088–FR-101 | Robot Configuration | Artifact/template/contract/program authoring, import/composition, review, and cleanup. |
| FR-102–FR-110 | Production Configuration | Release authoring, readiness, audited/concurrency-controlled deployment, rollback, monitoring, and timeout reconciliation. |
| FR-111–FR-119 | Production Packages | Package authoring, installation, repair, upgrade, cutover, rollback, abandonment, and reconciliation. |
| FR-120–FR-127 | IoT REST and MQTT | Durable pull/acknowledgement, execution evidence/checkpoints/state sync, wake-up, uplink, and endpoint authentication. |
| FR-128–FR-133 | Realtime, GraphQL, Sync, jobs, and dashboard | SignalR group joins/deltas, GraphQL read wiring, automatic dispatch/timeout reconciliation, execution metrics, inbox/dead-letter operations, and scoped dashboard aggregation/invalidation. |
| FR-134 | Inventory / Edge observations | Idempotent MQTT inventory sensor-observation evidence that applies only newer observations and does not create stock movements or prove consumption. |
| FR-135 | Production Configuration | Immutable Recipe-version-to-Published-RobotProgram binding with snapshotted option and optional declared-capability evidence. |

Every requirement's trigger, actor, flow, validation, related data, exceptions, status, and evidence source remain in Report 3 and the RTM. Report 7 does not replace that traceability baseline.

`[Consolidation Required]` Before final submission, import the approved Report 3 feature sections and all FR-001–FR-135 details; this range table is navigation only.

## 4. Non-Functional Requirements

### 4.1 External Interfaces

The approved Report 3 interface table for REST, GraphQL, SignalR, MQTT, payment, identity/push, database, object storage, Edge, and client boundaries must be imported without strengthening current evidence.

### 4.2 Quality Attributes

The baseline identifies security/authorization, multi-tenancy, idempotency, consistency, auditability, reliability/recovery, observability, performance/capacity, maintainability, data retention/privacy, compatibility, and availability-related concerns. Some mechanisms are supported—for example scoped RBAC, signature verification, endpoint authentication profiles, idempotency/retry fields, sensitive-data masking, and database constraints—but quantified targets and production acceptance thresholds are generally `[Needs Team Review]`.

No unsupported performance, availability, recovery, security-certification, physical-safety, or scale result is claimed. Such statements require approved environments and executed evidence.

`[Consolidation Required]` Import NFR-001–NFR-025 individually with their exact compound statuses and evidence. A general disclaimer does not replace row-level `[Inferred]`, `[Unclear]`, or known-gap qualifiers.

## 5. Other Requirements

### 5.1 Business Rules

`[Consolidation Required]` Import the approved Report 3 business-rule table with stable BR identifiers and evidence/status notes.

### 5.2 Common Requirements

`[Consolidation Required]` Import common validation, idempotency, authorization, tenant-scope, error-handling, audit, and data rules from Report 3.

### 5.3 Application Messages List

`[Needs Team/UI Review]` Import only approved application/user-facing messages. Do not construct message text from status codes or backend exception names.

### 5.4 Open Questions and Assumptions

Other requirements include lifecycle/business rules, validation/error handling, common authorization and tenant-scope rules, application messages, data integrity, external-interface contracts, and unresolved assumptions. Final UI messages, localization/accessibility requirements, legal/privacy retention, provider policies, physical safety rules, and acceptance criteria require team or external-owner input.

# IV. Software Design Description

## 1. System Design

### 1.1 System Architecture

The design follows a modular, layered backend arrangement. WebAPI owns transport/authentication concerns; Application coordinates use cases; Domain owns business concepts and invariants; Infrastructure supplies persistence and external adapters. Logical bounded contexts include Identity, Tenants, Devices, Catalog, Sales Catalog, Inventory, Orders, Payments, Operations, Robot Configuration, Production Configuration, Production Execution, Production Packages, and Sync.

Cross-context workflows prefer identifiers and snapshots rather than unrestricted navigation graphs. `[Inferred]` The architecture is a modular monolith with asynchronous and external integration boundaries; this description does not imply separately deployable services.

#### Runtime and Integration Behavior

The Cloud backend receives HTTP/GraphQL/SignalR traffic, PayOS callbacks, and Edge evidence; persists business data in PostgreSQL; stores robot artifact binaries through an S3-compatible object store; publishes/consumes supported MQTT messages; and calls identity, payment, and notification providers through adapters.

The order-to-execution relationship is deliberately separated:

1. Cloud validates checkout and creates the order/payment state.
2. PayOS payment evidence is signature-verified and applied through supported transitions.
3. Cloud persists a durable Edge command and may publish a best-effort MQTT wake-up.
4. Edge authenticates, pulls and acknowledges the command, performs its external local workflow, and reports evidence.
5. Cloud validates reported evidence and updates its observation/business state.

Cloud records do not independently prove physical robot success. Exact transaction boundaries, retry precedence, Edge physical behavior, and operational recovery remain governed by the detailed design and open-question register.

`[Consolidation Required]` Import the approved Report 4 architecture diagram, component explanations, legend, and boundary/status notes here.

### 1.2 Package Diagram

The detailed package/context ownership model remains in Report 4. Bounded contexts, application capabilities such as EdgeIntegration/Dashboard, and Infrastructure adapters must not be presented as equivalent package types or owners.

`[Consolidation Required]` Import the approved package diagram and package/namespace description table with exact ownership and dependency semantics.

## 2. Database Design

PostgreSQL with EF Core/Npgsql is the evidenced persistence platform. The model spans the bounded contexts above and uses a mix of application-assigned GUID keys (`GuidEntity`) and database-generated long identity/sequence keys (`LongEntity`), with exact strategy determined by entity base type. It also uses audit fields where applicable, restrictive relationship conventions, explicit indexes/constraints, selected JSON/JSONB for configuration, snapshots, external evidence, and metadata, and soft deletion for applicable entities. These conventions are not uniform across every evidence/history type. Robot `.lua` binary content is stored outside PostgreSQL; the database holds metadata such as object reference, checksum, and size.

Conceptual, logical, and physical details are maintained in the database-design deliverables. The synchronized static source contains 100 `DbSet<T>` declarations, eight non-designer migrations, and 101 cumulative `CreateTable` operations, including `InventorySensorObservations` and `ProductionProgramBindings`; these are not verified live-schema counts. Live-schema reconciliation, exact catalogue completion, effective delete behavior, uniqueness, nullability, lifecycle cardinalities, retention, and migration/manual-step operation remain `[Needs Team Review]` or `[Unclear]`.

`[Consolidation Required]` Import the approved Report 4 database relationship diagram, table/entity catalogue, PK/FK/nullability data, constraints/indexes, soft-delete behavior, and open database questions.

## 3. Detailed Design

### 3.1 Order and Payment Flow

Checkout snapshots, payment session/callback handling, durable dispatch, execution observation, incident/remake/refund paths, and realtime invalidation are designed in Report 4. A verified-unmatched PayOS callback creates no payment/order/fulfilment state and contributes only bounded diagnostic evidence. Exact transaction boundaries and late/conflicting matched-callback precedence remain qualified there.

### 3.2 Robot / Edge Execution Flow

Endpoint identity profiles, durable commands, MQTT wake-up, REST pull/acknowledgement, evidence ingestion, and reconciliation are designed in Report 4. Cloud evidence does not independently prove physical execution.

### 3.3 Catalog / Inventory / Production Configuration

Catalog/recipe/menu authoring, bounded runtime-menu caching, dispenser topology and sensor observations, robot artifacts/programs and raw-Lua import, Production Program Bindings, configuration releases, deployments, and packages are summarized in Report 4. Optional technical declarations and bindings must not be described as certification of Lua behavior or physical safety.

### 3.4 Tenant / Identity / Authorization

Tenant hierarchy, accounts, role assignments, effective access, scoped RBAC, sessions, invitations, and recovery are summarized in Report 4. Exact UI and exhaustive authorization coverage remain `[Needs Team/UI Review]`.

### 3.5 Operations / Sync / Background Processing

Alerts, tickets, diagnostics, inbox/dead-letter behavior, retries, retention/cleanup, reconciliation, and dashboard invalidation/read models are summarized in Report 4. Job existence does not guarantee automatic recovery.

The authoritative class and sequence diagrams remain in Report 4 and the UML deliverables. This summary does not introduce new classes or sequence messages.

`[Consolidation Required]` Import the approved Report 4 class diagrams/specifications, sequence diagrams, design explanations, failure paths, and coverage table. The order/payment and robot/Edge flows currently have greater detail than Sections 3.3–3.5; do not present all five areas as equally complete.

### Class Specifications

The design uses domain entities/aggregates, application commands and queries, request/result models, persistence abstractions where present, EF Core configurations, WebAPI endpoints, and external adapter contracts. Major class relationships are documented in the Report 4 class diagrams for order/payment and Edge flows and in the cross-context class baseline.

Because Report 7 is consolidated, it does not repeat every class member. Exact class names, ownership, dependencies, and method behavior must be verified against Report 4 and the repository revision used for submission. Any class not established there is `[Needs Team Review]` rather than inferred here.

# V. Software Testing Documentation

## 1. Scope of Testing

Report 5 defines a planned test-documentation baseline; it is not evidence of completed execution. The intended scope covers identity/authorization, tenancy, catalog, inventory, orders, payments, Edge contracts, operations, production configuration/packages, sync/background processing, database rules, and supported non-functional concerns.

All results, pass rates, defect counts, coverage percentages, environment evidence, and acceptance statements are `[To Be Updated After Test Execution]`.

`[Consolidation Required]` Import Report 5's target-feature/NFR scope, scope disposition register, deferrals, test levels, assumptions, and constraints. Planned scope is not execution evidence.

## 2. Test Strategy

### 2.1 Testing Types

The planned strategy combines unit, integration, API, database, system, acceptance, security/authorization, reliability/recovery, payment-webhook, and Edge/robot command-contract testing as applicable. Assignments, exact tools, test data, provider environments, physical hardware, client builds, schedules, entry/exit criteria, and sign-off authorities are `[Needs Team Review]`.

Test milestones must be aligned with the team's approved capstone plan without inventing dates. Production-provider and physical-device certification must be distinguished from fake, simulator, or contract-level testing.

### 2.2 Test Levels

Unit, Integration, System, and Acceptance are the planned test levels. API, database, security, reliability/recovery, payment-webhook, and Edge-contract testing are test types/concerns rather than additional levels. A case spanning several levels must be split into level-specific procedures/results before execution.

### 2.3 Supporting Tools

`[Needs Team Review]` Import the approved Report 5 tool table only after framework/client/version/environment choices are confirmed. Do not infer a tool from repository technology alone.

## 3. Test Plan

### 3.1 Human Resources

`[Team-Owned Placeholder]` Import assigned test roles and responsibilities only after team approval. No person or acceptance authority is established by this report.

### 3.2 Test Environment

`[Needs Team Review]` Import the approved build/source, database/migration, infrastructure/provider, client/Edge, hardware, test-data, clock, reset, observability, and isolation baseline.

### 3.3 Test Milestones

`[Team-Owned Placeholder]` Import milestones, entry/exit criteria, suspension/resumption, evidence rules, and dates only from the approved project/test plan.

## 4. Test Cases

The Report 5 catalog maps high-level cases to SRS requirements and covers the major backend areas, including:

- authentication, owned-session listing/revocation, organization-owned account administration, scoped authorization, and tenant isolation;
- tenant/device/catalog/inventory lifecycle and validation;
- idempotent checkout, payment sessions, signed/duplicate/conflicting callbacks, and refund handling;
- inventory-observation idempotency/projection ordering, Production Program Binding integrity, release/deployment concurrency, and execute-order schema v5 compatibility;
- durable dispatch, Edge pull/acknowledgement/report, replay, reconciliation, and uncertain physical outcome;
- incidents, redispatch/remake, alerts, maintenance, sync/dead-letter, and background jobs; and
- database constraints, soft-delete visibility/uniqueness, external failures, and selected NFR verification.

Detailed steps, fixtures, expected data changes, cleanup, evidence links, and final statuses must be completed in the approved test-case workbook or controlled equivalent. `[Needs Team Review]`

`[Consolidation Required]` Import the current Report 5 case catalogue and final requirement-to-test matrix. Preserve case IDs, levels/types, priorities, `[Planned]` status, and unresolved expected-result qualifications.

## 5. Test Reports

`[To Be Updated After Test Execution]`

| Report item | Required final evidence | Current status |
|---|---|---|
| Execution summary | Approved build/environment, dates, scope, result, and limitations | `[To Be Updated After Test Execution]` |
| Statistics | Planned/executed/passed/failed/blocked/not-run counts with reconciliation | `[To Be Updated After Test Execution]` |
| Defects | Identifier, severity, requirement, status, owner, evidence, workaround | `[To Be Updated After Test Execution]` |
| Requirement coverage | RTM-linked execution result and justified gaps | `[To Be Updated After Test Execution]` |
| Known issues | Verified release limitations, impact, workaround, acceptance | `[To Be Updated After Test Execution]` |
| Sign-off | Named authorized reviewers and dated acceptance/rejection | `[To Be Updated After Test Execution]` |

# VI. Release Package & User Guides

## 1. Deliverable Package

The candidate package includes backend source, an approved API contract artifact, database migrations/scripts, non-secret configuration templates, Reports 1–7, SRS/RTM, UML/database designs, test documentation and future evidence, deployment notes, the user guide, known issues, and a versioned release manifest. Report 2, presentation materials, final test artifacts, and approved release-known issues are currently `[Needs Team Review]`.

| No. | Final package group | Current consolidated status |
|---:|---|---|
| 1 | Schedule, tracking records, and backlog | `[Team-Owned Placeholder]`; approved baseline/export `[Needs Team Review]`. |
| 2 | Versioned backend source and API contract artifact | Source exists in the workspace; release revision, artifact, license/exclusions, and checksum `[Needs Team Review]`. |
| 3 | Database migrations/scripts and deployment runbook | Source migrations exist; shipped artifact, manual steps, rollback/recovery, and live-schema evidence `[Needs Team Review]`. |
| 4 | Reports 1–7, SRS/RTM, UML, and database design | Drafts/baselines exist except team-owned Report 2; final approvals and rendered figures `[Needs Team Review]`. |
| 5 | Test cases, execution evidence, and coverage report | `[To Be Updated After Test Execution]`. |
| 6 | Defect export and release-known issues | `[To Be Updated After Test Execution]`; accepted limitations and approval `[Needs Team Review]`. |
| 7 | Installation/user guide and deployment notes | Report 6 draft exists; verified runbook, UI evidence, and environment controls `[Needs Team Review]`. |
| 8 | Presentation/slides and final submission index | `[Needs Team Review]`. |

Every released artifact must have an approved packaged path, version, source revision/build, checksum, owner, approval status, confidentiality classification, and supersession relationship. Repository paths alone are not an approved submission manifest.

## 2. Installation Guides

### 2.1 System Requirements

Report 6 provides an installation framework for server/runtime, PostgreSQL, external services, network/API relationships, configuration, migrations, build/package, deployment, verification, and troubleshooting. It intentionally does not invent SDK versions, domains, ports, credentials, secret values, deployment commands, or production topology.

Before operational use, the team must approve:

- the release manifest and supported environment/topology matrix;
- a non-secret configuration catalogue and secret-management procedure;
- exact health/readiness and integration acceptance criteria;
- the shipped migration artifact, manual-step invocation, backup/restore or forward-fix procedure;
- PayOS, identity, FCM, MinIO, MQTT, Edge credential, and network controls;
- reproducible packaging/deployment/rollback instructions; and
- dated installation evidence and authorized sign-off.

All such unresolved values remain `[Needs Team Review]`.

### 2.2 Installation Instruction

`[Consolidation Required]` Import Report 6's approved baseline, repository inspection, prerequisites, configuration, database, build/package, external-integration, deployment/start, verification, and troubleshooting steps. The current source is a framework, not an executable production runbook; exact commands and values remain `[Needs Team Review]` until tested and approved.

## 3. User Manual

### 3.1 Overview

The user guide is currently role- and contract-oriented because approved client screens are not part of the backend evidence. It covers identity self-service; account/access and tenant administration; device/endpoint administration; catalog, inventory, robot artifact, release, and package workflows; customer order/payment; management payment/refund; Edge integration and operator monitoring; dashboard/alerts; incidents/remake/redispatch; sync/dead-letter recovery; and background processing.

Final human-client guidance requires approved screen names, navigation, fields/actions, authorization, success/error/recovery messages, accessibility information, screenshots, captions, build identifiers, and owners. These are `[Needs UI/Team Review]`.

Edge protocol acknowledgements and reports are integration behavior, not manual operator actions. Physical robot installation, calibration, motion safety, cleaning, emergency response, maintenance, and acceptance are owned outside this backend repository and remain `[Needs Team Review]`.

### 3.2 Administration Workflow

`[Consolidation Required]` Import the approved identity self-service, account/access, tenant, and device/execution-endpoint workflow instructions. UI navigation, field labels, messages, and screenshots remain `[Needs Team/UI Review]`.

### 3.3 Catalog and Inventory Workflow

`[Consolidation Required]` Import the approved catalog/sales-catalog, inventory/readiness, and robot artifact/configuration/package authoring workflows with exact actor and backend-boundary qualifications.

### 3.4 Order and Payment Workflow

`[Consolidation Required]` Import the approved customer/tablet and management payment/refund workflows. Preserve the signed-callback boundary, authoritative backend status, manual-refund limitation, runtime-menu-source question, and prohibition on inferring physical output.

### 3.5 Robot / Edge Operation Workflow

`[Consolidation Required]` Import the approved Cloud/Edge sequence and operator observation guidance. Keep integration payload behavior separate from human operator actions and mark Edge/robot implementation `[Needs Team Review]`.

### 3.6 Operations and Incident Handling Workflow

`[Consolidation Required]` Import operational dashboard/alert, device/kiosk monitoring, incident/remake/redispatch/refund, sync/dead-letter, and background-processing/escalation workflows. Missing screen details and support procedures remain `[Needs Team/UI Review]` or `[Needs Team Review]` as owned.

# VII. Appendix

## 1. Glossary

| Term | Meaning |
|---|---|
| Durable command | A Cloud-persisted command that Edge retrieves through the supported contract; MQTT wake-up is not the durable payload/source of truth. |
| Execution evidence | Edge-reported acknowledgement, checkpoint, state, or result data accepted under an owning contract; it is not independent physical proof. |
| Order snapshot | Order-time data retained to preserve the commercial meaning of an order when catalog data later changes. |
| Production configuration | Versioned configuration that binds supported execution routes, programs/artifacts, and target endpoint deployment behavior. |
| Production incident | An operational record for an uncertain, rejected, defective, or otherwise support-requiring production outcome. |
| Runtime menu | A kiosk-oriented menu projection produced from supported catalog, scope, and configuration data. Its normal client source is `[Needs UI/Team Review]`. |
| Scoped RBAC | Authorization requiring an allowed role/policy and a compatible tenant scope; UI visibility is not enforcement. |
| Soft delete | Logical deletion where applicable, with query-filter and uniqueness implications documented in the database design. |
| Tenant | An organization/store/kiosk scope used to partition and authorize business operations. |

## 2. References

The final DOCX must convert these workspace references into numbered citations and package-relative links or appendices tied to approved document versions.

1. `deliverables/06_school_reports/report1_project_introduction/report1_project_introduction.md` — Report 1 draft.
2. `deliverables/06_school_reports/report3_srs/report3_srs.md` — Report 3 SRS draft.
3. `deliverables/06_school_reports/report4_sdd/report4_sdd.md` — Report 4 SDD draft.
4. `deliverables/06_school_reports/report5_test_documentation/report5_test_documentation.md` — Report 5 test-documentation draft.
5. `deliverables/06_school_reports/report6_user_guides/report6_user_guides.md` — Report 6 release/user-guide draft.
6. `deliverables/02_srs/srs.md` and `deliverables/02_srs/requirements_traceability_matrix.md` — requirements baseline and traceability matrix.
7. `deliverables/03_uml/` — use-case, class, sequence, activity, and ERD sources.
8. `deliverables/04_database_design/` — conceptual, logical, and physical database-design baselines.
9. `deliverables/00_repo_evidence/` — repository truth map and functional/database evidence inventories and packs.
10. `deliverables/05_team_review/open_questions.md` — consolidated unresolved-question register.
11. `deliverables/99_templates_reference/template_structure_notes.md` — university template structure notes.

Bibliographic metadata, access dates, university citation style, source revision identifiers, and final package paths are `[Needs Team Review]`.

## 3. Others

### Consolidation and DOCX Controls

`[Needs Team Review]` Before Report 7 is treated as the final consolidated report:

1. Freeze and approve Reports 1–6 and record each source path, revision/version, checksum, owner, and approval date.
2. Import the approved content losslessly under Parts I–VI, preserving heading order, FR/NFR/BR/DR/TC/open-question IDs, tables, figures, decisions, and uncertainty/status labels. The `[Consolidation Required]` markers in this draft identify material content not yet imported.
3. Do not independently correct an owning report only inside Report 7; correct and approve the owning report first, then reconsolidate.
4. Verify that every source heading, table, figure, and material paragraph is represented once and that no `[Inferred]`, `[Unclear]`, `[Needs Team Review]`, `[Needs Team/UI Review]`, `[Team-Owned Placeholder]`, `[Planned]`, or `[To Be Updated After Test Execution]` qualifier is silently removed.
5. Replace workspace-only references with numbered citations or stable package-relative links tied to approved versions.
6. Render Mermaid diagrams to publication-quality figures while retaining editable sources; add figure/table numbers, captions, cross-references, and accessibility text.
7. Generate and refresh the table of contents, lists of figures/tables where used, heading numbering, page numbers, section/page breaks, and landscape layouts for wide material.
8. Compare the final DOCX and PDF against the frozen source reports and record conversion approval.

The university/team must confirm whether Part VII appendices are permitted and how they should be numbered. `[Needs Team Review]`

### Consolidated Open Items

The detailed register remains in `deliverables/05_team_review/open_questions.md`. Before final submission, the team must assign owners and record decisions/evidence for at least:

- official project, team, supervisor, external-supervisor, code, date, acknowledgement, and comparable-system information;
- the complete Report 2 management plan, schedule, assignments, communication, and configuration-management evidence;
- final actors, permission/scope matrix, UI workflows, client/Edge responsibility boundaries, and application messages;
- order/payment/refund/incident state alignment, callback precedence, transaction boundaries, and physical-outcome handling;
- Edge installation and credential lifecycle, robot safety/acceptance, MQTT and REST operating profiles, and recovery ownership;
- database live-schema reconciliation, constraints, delete behavior, uniqueness, retention, migrations, backup, and recovery;
- production deployment topology, configuration, observability, SLO/quality targets, external-provider settings, and operations ownership;
- executed tests, defect/coverage reports, known issues, and acceptance sign-off; and
- final DOCX rendering, diagram numbering, captions, cross-references, accessibility, citation format, pagination, and university approval.

### Document Status

This Report 7 is a school-template consolidation draft. It currently preserves a structured summary and explicit import markers for Reports 1, 3, 4, 5, and 6 and reserves Report 2 for team completion; it is not yet the lossless final consolidation required for submission. It must not be interpreted as proof of implementation completeness, production deployment, physical robot performance, test execution, or stakeholder acceptance.
