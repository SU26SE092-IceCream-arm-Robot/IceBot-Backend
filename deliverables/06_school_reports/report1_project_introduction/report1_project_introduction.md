# CAPSTONE PROJECT REPORT

## REPORT 1 — PROJECT INTRODUCTION

**Project name:** `[Official project name — to be confirmed by the team]`  
**Working product name:** IceBot Backend  
**Project code:** `[Project code]`  
**Group name:** `[Group name]`  
**Institution:** FPT University  
**Location and date:** `[Location, month, year]`

# I. Record of Changes

*A — Added; M — Modified; D — Deleted*

| Date | A/M/D | In charge | Change Description |
|---|---|---|---|
| `[dd/mm/yyyy]` | A | `[Author/team member]` | Initial school-template draft prepared from the approved backend documentation baseline. |

# II. Project Introduction

## 1. Overview

### 1.1 Project Information

| Item | Information |
|---|---|
| Project name | `[Official project name — to be confirmed by the team]` |
| Working product name | IceBot Backend |
| Project code | `[Project code]` |
| Group name | `[Group name]` |
| Software type | Server-side web API and integration backend for an automated vending platform |
| Customer / project requester | `[Customer or project requester]` |
| Supervisor / lecturer | `[Supervisor or lecturer]` |
| Target release | `[Target release or semester milestone]` |

IceBot Backend is the Cloud-side server application for a multi-location automated vending platform with robot-arm order fulfilment. It supports tablet-originated customer checkout, payment processing, robot execution coordination, kiosk operations, and centralized administration of organizations, stores, kiosks, catalogs, menus, devices, inventory, and production configuration.

The backend is implemented as an ASP.NET Core modular monolith. Its internal design applies Clean Architecture boundaries, bounded-context organization, CQRS-lite application handlers, event-oriented integration, Entity Framework Core, and PostgreSQL persistence. The system communicates with kiosk Edge runtimes through REST and MQTT contracts, publishes user-interface updates through SignalR, supports management reads through REST and GraphQL, integrates with PayOS for payment sessions and callbacks, and stores robot program artifacts in MinIO.

The evidence used for this draft establishes implemented source structure and wiring. It does not by itself prove that every workflow has passed runtime, integration, or production verification. Detailed requirements and confidence classifications are maintained in the Software Requirements Specification and Requirements Traceability Matrix.

### 1.2 Project Team

| Full Name | Role | Email | Mobile |
|---|---|---|---|
| `[Supervisor or lecturer name]` | Supervisor / Lecturer | `[Email]` | `[Mobile]` |
| `[Team member name]` | Team Leader | `[Email]` | `[Mobile]` |
| `[Team member name]` | Member | `[Email]` | `[Mobile]` |
| `[Team member name]` | Member | `[Email]` | `[Mobile]` |
| `[Team member name]` | Member | `[Email]` | `[Mobile]` |
| `[Add or remove rows to match the official team]` | `[Role]` | `[Email]` | `[Mobile]` |

All names, roles, contact details, and academic identifiers in this section must be supplied and approved by the team before submission.

## 2. Product Background

IceBot addresses the coordination needs of an automated retail environment in which a customer order begins in a tablet interface but is fulfilled by physical equipment at a kiosk. The Cloud backend and the kiosk-local Edge runtime have different responsibilities. The Cloud backend manages business and configuration data, payment integration, central monitoring, and synchronization. The Edge runtime owns local robot execution, device communication, telemetry capture, and temporary operation during loss of Cloud connectivity.

The implemented data and application model supports a hierarchy in which an organization manages stores and kiosks. Internal users receive role assignments with organization, store, or kiosk scope. The platform also manages product and recipe definitions, sellable menus, ingredient dispensers, robot program artifacts, deployable configuration releases, and reusable production packages.

The central operational flow begins when a tablet submits a customer order. The Cloud validates current kiosk, menu, recipe, option, and sales conditions; records the order and payment attempt; and obtains a provider checkout session. After a verified payment callback, the Cloud makes the paid order eligible for fulfilment and dispatches an execution command. The Edge runtime pulls and acknowledges the command, performs local readiness and execution work, and returns execution evidence. The Cloud then updates its order, production, inventory, and operational projections. Reconciliation processes address selected missed, delayed, or stale workflow states.

The repository evidence indicates that the product is designed for multi-organization and repeatable kiosk deployment. `[Assumption]` The commercial rationale, target market, customer organization, and reason for selecting a franchise-oriented operating model have not been established by the backend evidence and require confirmation from the project team or project requester.

## 3. Existing Systems

The university template requires an evaluation of comparable or predecessor systems. The current IceBot backend deliverables contain no approved market research, competitor analysis, customer legacy-system description, or cited comparison of external products. This section must therefore be completed by the team from approved external or project-owner sources.

### 3.1 `[Comparable System 1 — name to be supplied]`

| Evaluation Item | Team Input Required |
|---|---|
| System name and source/link | `[Name and reliable source]` |
| Intended users and actors | `[Actors]` |
| Relevant functions | `[Functions comparable to IceBot]` |
| Advantages | `[Evidence-based advantages]` |
| Limitations | `[Evidence-based limitations]` |
| Lessons applicable to IceBot | `[Design or product lessons]` |

### 3.2 `[Comparable System 2 — name to be supplied]`

| Evaluation Item | Team Input Required |
|---|---|
| System name and source/link | `[Name and reliable source]` |
| Intended users and actors | `[Actors]` |
| Relevant functions | `[Functions comparable to IceBot]` |
| Advantages | `[Evidence-based advantages]` |
| Limitations | `[Evidence-based limitations]` |
| Lessons applicable to IceBot | `[Design or product lessons]` |

`[Add further comparable systems if required by the supervisor. Remove all university-template example content before submission.]`

## 4. Business Opportunity

`[Team approval required]` The backend evidence does not contain a customer-approved business case, market size, competitor study, financial projection, or formal opportunity statement. The following is therefore an `[Inferred]` operational opportunity derived from the capabilities implemented in the repository; it must be reconciled with the original project brief before submission.

Automated vending combines digital commerce with physical production. This creates operational demands that are not addressed by a simple online ordering application: payment confirmation must remain separate from robot execution; kiosks may temporarily lose connectivity; robot programs and configuration must be versioned and safely deployed; ingredient readiness must affect whether an item can be sold or produced; and staff require evidence for incidents, maintenance, refunds, and uncertain physical outcomes.

IceBot provides a centralized backend boundary for these concerns. Its multi-organization model can support repeated deployment across stores and kiosks, while reusable configuration and production packages can reduce variation during rollout. Centralized catalog, device, inventory, payment, alert, maintenance, and reporting capabilities can give operators a consistent view of geographically distributed kiosks. Edge/Cloud synchronization and explicit reconciliation workflows can reduce dependence on uninterrupted connectivity and make delayed or failed work visible for operational intervention.

The final report must add the team-approved customer problem, target market, expected users, measurable benefits, and comparison with existing alternatives. No commercial benefit, cost saving, market demand, or competitive advantage should be presented as confirmed until the team supplies supporting evidence.

## 5. Software Product Vision

**Draft vision — `[Assumption: requires approval by the team or project requester]`:**

> For organizations that operate automated vending kiosks and need to coordinate customer ordering, digital payment, robot-based production, and distributed kiosk operations, IceBot is a Cloud-and-Edge platform whose backend centralizes business configuration, payment coordination, deployment, monitoring, and operational evidence while allowing kiosk-local runtimes to execute physical work and tolerate temporary disconnection. Unlike an ordering system that assumes immediate fulfilment by a continuously connected service, IceBot separates payment from physical execution and provides explicit synchronization, reconciliation, incident, and maintenance workflows for automated production.

The current repository directly supports the Cloud backend and its contracts with external clients and kiosk runtimes. The tablet, management frontend, and Edge runtime implementations are outside this repository and must not be described as verified deliverables here unless the team supplies evidence from their owning repositories.

## 6. Project Scope & Limitations

### 6.1 Major Features

The following feature groups summarize the statically code-evidenced backend scope. Detailed functional requirements and evidence mappings belong to Report 3 rather than this introduction.

1. **FE-01 — Identity and Scoped Access Control**  
   Support local and Google/Firebase authentication, invitation-based internal-account onboarding, session and refresh-token management, internal roles, permissions, and organization/store/kiosk-scoped access assignments.

2. **FE-02 — Organization, Store, and Kiosk Management**  
   Manage the lifecycle of organizations, stores, and kiosks; control store sales pauses and kiosk operational states; provide tenant-scope navigation; and coordinate franchise-onboarding records.

3. **FE-03 — Product, Recipe, and Menu Management**  
   Manage ingredients, product categories, reusable product templates, tenant products and variants, options, recipes, menus, menu items, and kiosk-specific runtime-menu projections with sellability checks.

4. **FE-04 — Ingredient Inventory and Production Readiness**  
   Provision and configure dispenser/container topology; record refill, consumption, adjustment, retirement, and hardware rebind activity; calculate inventory summaries; and evaluate readiness for production and deployment.

5. **FE-05 — Customer Checkout and Order Lifecycle**  
   Accept and validate tablet-originated orders, expose order-scoped status access, support permitted cancellation and management intervention, dispatch production work, record fulfilment evidence, and manage production incidents and remakes.

6. **FE-06 — Payment and Refund Operations**  
   Create PayOS payment sessions, process signed provider callbacks, expose payment status and diagnostics, reconcile selected pending payment sessions, and manage internal refund requests and their manual lifecycle.

7. **FE-07 — Robot Artifact and Program Authoring**  
   Upload, review, validate, publish, retire, and discard Fairino Lua artifacts; manage reusable artifact templates and technical contracts; build ordered robot programs; and process authoring-import bundles.

8. **FE-08 — Production Configuration and Deployment**  
   Author versioned configuration releases and execution routes, validate deployment readiness, deploy configuration to Full Edge or low-cost controller profiles, inspect deployment state, and support rollback and timeout reconciliation.

9. **FE-09 — Reusable Production Packages**  
   Author versioned production packages, preview and install them for tenant scopes, inspect and repair installation materialization, fork package-managed resources, and coordinate upgrade, cutover, rollback, abandonment, and stale-upgrade reconciliation.

10. **FE-10 — Device, Connectivity, and Edge Integration**  
    Manage device types, models, devices, replacements, and execution endpoints; provision endpoint and MQTT credentials; ingest heartbeat, telemetry, readiness, device-event, execution-report, and production-sync data; and reconcile connectivity state.

11. **FE-11 — Operations and Maintenance**  
    Manage operational alerts, automatic alert correlation, maintenance tickets, operation logs, notification delivery and requeue, payment/order intervention views, and management dashboard metrics.

12. **FE-12 — Synchronization and Realtime Communication**  
    Provide durable Edge command pull and acknowledgement, REST/MQTT uplink handling, sync inbox and dead-letter management, selected retry/reconciliation processes, SignalR updates for human-facing clients, and GraphQL management reads.

These features are derived from 260 identifiable capability rows in the functional inventory. The inventory's summary table states 265 because its Operations total exceeds the identifiable rows by four and its Payments total exceeds them by one. The draft therefore avoids using 265 as a confirmed capability count. “Implemented” in the inventory means that the route or consumer, application handling, and domain/persistence wiring were statically inspected; it does not mean that the capability has passed runtime acceptance testing.

### 6.2 Limitations & Exclusions

1. **LI-01 — Repository boundary**  
   This report is based on the IceBot backend repository. Frontend, tablet, mobile, and kiosk Edge-runtime implementation details are excluded unless separately supplied and verified by their owning teams.

2. **LI-02 — Local execution ownership**  
   The Cloud backend does not run the live robot job scheduler. The Edge runtime owns robot execution and local device communication; the Cloud stores commands, coordination state, and accepted audit/read-model evidence.

3. **LI-03 — Provider-side refund automation**  
   The evidenced release does not implement automatic provider refunds or payouts. Refund records and operator transitions support a manual process. `[Unclear]` Business-process references to voucher compensation are not matched by a clearly identified voucher field or entity in the reviewed data evidence.

4. **LI-04 — Partial temporary-password onboarding path**  
   An administrator-set initial-password code path exists, but its forced-password-change and restricted-first-login lifecycle is not part of the current documented contract. Invitation-based onboarding is the supported baseline.

5. **LI-05 — Partial dead-letter replay coverage**  
   Automated retry of sync dead letters is limited to `ExecutionReport.*` event types. Production-event and Edge state-summary dead letters can be reviewed and dispositioned but have no evidenced automated replay path.

6. **LI-06 — Dashboard interface**  
   Management dashboard aggregation is exposed through the GraphQL `dashboard` query and SignalR invalidation. No standalone REST dashboard controller was identified.

7. **LI-07 — Database partitioning**  
   PostgreSQL-native table partitioning is not implemented in the evidenced release.

8. **LI-08 — Tenant filtering model**  
   The persistence layer does not apply a universal tenant-scoped EF Core query filter. Tenant scope is enforced by application handlers and selected structural constraints. Complete authorization and tenant-scope coverage across every REST action, GraphQL resolver, and SignalR method remains `[Unclear]` pending an exhaustive audit.

9. **LI-09 — Runtime verification**  
   The current evidence set does not map requirements to executed unit, integration, system, acceptance, performance, reliability, or security tests. This draft therefore describes statically evidenced implementation, not verified production performance.

10. **LI-10 — Unresolved operational requirements**  
    API version/deprecation policy, structured error responses, rate limits, CORS, request-size limits, backup/restore procedures, disaster-recovery targets, required deployment profiles, and the mandatory/optional status of background jobs are not established by the reviewed evidence.

11. **LI-11 — Scope and roadmap decisions**  
    `[Open Question]` The team must confirm the target release boundary, the disposition of the two partial capabilities, provider permanence, business priorities, and whether any listed exclusion is relevant to the submitted project scope. This report does not convert those matters into roadmap commitments.

---

**Draft completion notice:** Before DOCX conversion, the team must replace all placeholders, approve the business background/opportunity and product vision, provide comparable-system research, confirm scope and limitations, and update the Record of Changes. Repository paths and internal evidence labels may remain during review but should be converted into the final citation and disclosure style required by the supervisor.
