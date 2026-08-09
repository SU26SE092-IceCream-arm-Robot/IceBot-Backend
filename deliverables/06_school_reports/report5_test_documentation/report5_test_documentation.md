# CAPSTONE PROJECT REPORT

## REPORT 5 — SOFTWARE TEST DOCUMENTATION

**Project name:** `[Official project name — Needs Team Review]`

**Working product name:** IceBot Backend

**Project code:** `[Project code — Needs Team Review]`

**Group name:** `[Group name — Needs Team Review]`

**Location and date:** `[Location and date — Needs Team Review]`

# I. Record of Changes

*A — Added; M — Modified; D — Deleted*

| Date | A/M/D | In charge | Change Description |
|---|---|---|---|
| `[Date — Needs Team Review]` | A | `[Author/team member — Needs Team Review]` | Initial school-template Software Test Documentation draft prepared from the SRS, RTM, design documents, and repository evidence. |

# II. Testing Documentation

This document defines the proposed test scope, strategy, plan, and high-level test catalogue for IceBot Backend. It is not evidence that any test has run or passed. The existing SRS/RTM status **Supported** means statically code-evidenced, not runtime-tested. Unless explicitly replaced by a dated execution record, every case in Section 4 has status `[Planned]`, and every result in Section 5 is `[To Be Updated After Test Execution]`.

## 1. Scope of Testing

### Target Features and Functions

The proposed scope follows the 133 functional requirements in Report 3 and the RTM. The high-level coverage areas are:

| Test area | Requirement scope | Principal behavior to verify | Planning status |
|---|---|---|---|
| Identity and authorization | FR-001–FR-016 | Authentication, sessions, password/invitation lifecycle, roles, policies, and tenant scope. | `[Planned]` |
| Tenant management | FR-017–FR-021 | Organization/store/kiosk lifecycle, operational state, onboarding, and scope navigation. | `[Planned]` |
| Devices and connectivity | FR-022–FR-032 | Device/endpoint provisioning, credentials, events, telemetry, readiness, and timeout projection. | `[Planned]` |
| Catalog and Sales Catalog | FR-033–FR-047 | Products, variants, options, recipes, menus, publication gates, and runtime projection. | `[Planned]` |
| Inventory | FR-048–FR-056 | Dispenser topology, refill/adjustment/consumption movements, readiness, and calibration. | `[Planned]` |
| Orders | FR-057–FR-067 | Checkout, status/cancellation, dispatch/remake, fulfilment, incidents, reads, and realtime events. | `[Planned]` |
| Payments | FR-068–FR-078 | Session creation, status, signed PayOS callback, reconciliation, diagnostics, and manual refund lifecycle. | `[Planned]` |
| Operations | FR-079–FR-087 | Alerts, tickets, operation logs, notification delivery, and realtime updates. | `[Planned]` |
| Robot Configuration | FR-088–FR-101 | Artifact/program authoring, validation, publication, object storage, import, and cleanup. | `[Planned]` |
| Production Configuration | FR-102–FR-110 | Release/routes, publish gates, deployment/rollback, readiness, and timeout handling. | `[Planned]` |
| Production Packages | FR-111–FR-119 | Package/version authoring, install, materialization, repair, upgrade, cutover, rollback, and reconciliation. | `[Planned]` |
| IoT REST and MQTT | FR-120–FR-127 | Command pull/ack/report, production sync, wake-up, uplink routing, and message guards. | `[Planned]` |
| Realtime, GraphQL, Sync, Dashboard | FR-128–FR-133 | Hub authorization, GraphQL wiring, dispatch/reconciliation, metrics, dead letters, and dashboard invalidation. | `[Planned]` |

### Scope Disposition Register

The following disposition applies to every requirement ID in each inclusive range unless a narrower exception is stated. The detailed Software Test Matrix (STM) must expand the ranges to individual IDs and subclaims before execution.

| Requirement scope | Disposition | Reason / condition | Target level | Owner | Environment dependency |
|---|---|---|---|---|---|
| FR-001–FR-025, FR-027–FR-119, FR-128–FR-133 | In Scope | Backend-owned behavior is represented by repository evidence. Broad cases must be split into executable requirement/subclaim variants. | Unit/Integration/System as assigned in STM | `[Needs Team Review]` | Approved backend/database baseline; external/client dependency where applicable. |
| FR-026 | Conditional | MQTT credential lifecycle is in scope; broker/DB partial-failure acceptance needs an approved environment and rule. | Integration | `[Needs Team Review]` | Isolated broker and credential adapter. |
| FR-120–FR-127 | Conditional | Cloud Edge-contract behavior is in scope; Edge/robot implementation is external. | Integration/System | `[Needs Team Review]` | Approved Edge simulator/runtime and broker. |
| NFR-001–NFR-014 | In Scope / Conditional subclaims | Mechanisms are testable; unresolved transaction, recovery, authorization, and delete-behavior subclaims require decisions or characterization cases. | Unit/Integration/System | `[Needs Team Review]` | Controlled clock, database, provider/Edge dependencies as applicable. |
| NFR-015, NFR-018–NFR-019 | In Scope | Structural dependency/persistence conventions are verified through inspection/static checks and focused integration tests; inferred benefits are not pass criteria. | Unit/Integration | `[Needs Team Review]` | Approved source and EF model baseline. |
| NFR-016–NFR-017, NFR-020–NFR-024 | In Scope / Conditional | Requires broker, database, observability, object-storage, or multi-instance facilities depending on the requirement. | Integration/System | `[Needs Team Review]` | Approved isolated infrastructure. |
| NFR-025 | In Scope (verification) | Verify current absence of native partitioning; any future partitioning requirement is a separate decision. | Integration/inspection | `[Needs Team Review]` | Reconciled schema. |
| Frontend/Tablet/Edge internal unit/UI behavior | Deferred to owning repositories | Implementations are not present in this backend evidence set. | `[Needs Team Review]` | `[Needs Team Review]` | Approved client/Edge builds. |
| External provider internal behavior | Out of Scope | Backend tests may verify adapters/fakes/sandboxes but cannot certify provider internals. | — | `[Needs Team Review]` | Provider-owned. |
| Physical robot quality/safety certification | Out of Scope for backend test evidence | Requires hardware, safety authority, procedures, and acceptance criteria not established here. | Acceptance/HIL only if separately approved | `[Needs Team Review]` | Approved physical rig and safety sign-off. |

### Non-Functional Requirements to Be Tested

| NFR group | Requirements | Proposed verification focus | Status |
|---|---|---|---|
| Retry and offline tolerance | NFR-001–NFR-003 | Duplicate requests, durable command behavior, missed notifications, retry exhaustion, and manual-recovery outcomes. | `[Planned]` |
| Data integrity and audit | NFR-004–NFR-006, NFR-017, NFR-019, NFR-023, NFR-025 | Effective delete behavior, precision, histories, indexes, tenant-consistency constraints, and absence of native partitioning. | `[Planned]`; unresolved model claims require `[Needs Team Review]`. |
| Authorization and transport security | NFR-007–NFR-012 | Scoped RBAC, endpoint authentication, enumeration resistance, lockout, secret storage, and webhook verification. | `[Planned]` |
| Caching, retention, and operational behavior | NFR-013–NFR-014, NFR-020–NFR-021, NFR-024 | ETag/cache interval, bounded deletion, diagnostics separation, metrics, and advisory-lock coordination. | `[Planned]` |
| Architecture and storage constraints | NFR-015–NFR-016, NFR-018, NFR-022 | Dependency boundaries, MQTT shared subscription behavior, and external artifact storage. | `[Planned]`; outcome/rationale claims remain evidence-qualified. |

No approved numerical targets exist for response time, throughput, concurrency, availability, MTBF/MTTR, recovery time, or capacity. Such tests cannot have final pass thresholds until the team approves measurable requirements. `[Needs Team Review]`

### Features Not Tested or Deferred

- Physical robot motion quality, dispensing accuracy, safety certification, and device-level control are outside the backend repository evidence. Hardware-in-the-loop scope, equipment, safety procedure, and acceptance authority are `[Needs Team Review]`.
- Tablet, mobile, management-frontend, and Local Edge Backend internal code are not available in the supplied evidence. Their unit/UI tests are deferred to their owning repositories. Cross-system acceptance and contract tests remain `[Planned]` subject to environment availability.
- PayOS internal processing and customer bank-application behavior are external. Only backend session requests, signed callbacks, reconciliation, and recorded state can be tested here; provider sandbox access is `[Needs Team Review]`.
- Firebase/Google and FCM provider internals are external. Adapter behavior may be tested through approved fakes/sandboxes; credentials and environments are `[Needs Team Review]`.
- Production load, penetration, failover, backup/restore, disaster recovery, and long-duration soak testing are deferred until environments, tools, targets, and authorization are approved.
- Full schema verification against a live database/model snapshot is required before treating the current physical-table count, Cascade exceptions, or all FK/index claims as verified.
- The partial temporary-password onboarding path (FR-009) and limited dead-letter replay (FR-132) must be tested as known limitations unless the requirements are changed.

### Testing Levels to Be Applied

- **Unit:** Domain rules, validation, state transitions, policies, value calculations, and handler behavior with controlled collaborators.
- **Integration:** EF Core/PostgreSQL mappings and constraints, API/application/persistence paths, provider adapters through approved fakes/sandboxes, MQTT/MinIO integration, and hosted-job coordination.
- **System:** Deployed backend behavior across HTTP, GraphQL, SignalR, database, object storage, broker, payment callback, and an approved Edge simulator/runtime.
- **Acceptance:** Product/team validation of business flows, role permissions, recovery/manual operations, and agreed external-system scenarios.

### Testing Assumptions and Constraints

- `[Assumption]` PostgreSQL 17, MinIO, and Mosquitto represent the current configuration baseline; exact test versions and images require environment approval.
- `[Assumption]` External dependencies will be replaced by controlled test doubles or approved sandbox services where production access is inappropriate.
- `[Needs Team Review]` The authoritative source/build revision, database migration state, seeded data set, configuration profile, and test-data reset procedure are unknown.
- `[Needs Team Review]` The normal tablet runtime-menu source (Edge, Cloud, or both) and the exact Edge simulator/device contract must be confirmed.
- `[Needs Team Review]` Test accounts, RBAC scopes, endpoint credentials, certificates/keys, provider secrets, and secret-handling procedure must be approved before execution.
- Static evidence references are design inputs, not expected results from a previously passing system.
- Physical outcome must never be inferred solely from Cloud command state, missing acknowledgement, timeout, or Edge report receipt.

## 2. Test Strategy

### 2.1 Testing Types

| Testing type | Objective and technique | Proposed completion criterion | Status |
|---|---|---|---|
| Unit testing | Exercise domain state machines, validation, handler branches, calculations, and policy logic in isolation with deterministic inputs. | All approved unit cases executed; no unresolved blocking failure; coverage target `[Needs Team Review]`. | `[Planned]` |
| Integration testing | Verify application, EF Core, PostgreSQL constraints/transactions, hosted jobs, and external adapter boundaries using controlled dependencies. | All critical integration cases executed against the approved migration baseline; blocking data/contract defects resolved or accepted. | `[Planned]` |
| API testing | Verify REST/GraphQL request validation, authentication, authorization, status/error contracts, idempotency, and response data. | Critical API cases pass for the approved build; exact status/error-envelope assertions require the approved API contract. | `[Planned]` |
| Database testing | Verify mappings, PK/FK/unique/check/index behavior, precision, JSON mapping, soft delete, tenant consistency, and transaction rollback. | Reconciled model/schema passes approved constraint and lifecycle cases; open DB questions are resolved or recorded. | `[Planned]` |
| System testing | Exercise deployed backend flows across persistence and configured integrations. | Approved end-to-end scenarios complete without blocking defects; physical robot results remain separately scoped. | `[Planned]` |
| Acceptance testing | Confirm business behavior with authorized team/product representatives. | Signed acceptance criteria and authorized sign-off obtained. | `[Needs Team Review]` |
| Security/authorization testing | Test JWT/role/scope combinations, cross-tenant attempts, endpoint credentials, replay/nonces, secret exposure, webhook signatures, and diagnostics access. | No unauthorized access in approved matrix; security thresholds and penetration scope `[Needs Team Review]`. | `[Planned]` |
| Reliability/recovery testing | Inject missed wake-ups, duplicate events, stale work, dependency failures, retries, dead letters, and worker concurrency. | System reaches the coded recovery/manual state without silent data corruption or false physical-outcome assertion. | `[Planned]` |
| Payment webhook testing | Send valid, invalid, duplicate, late, and conflicting callback scenarios through an approved fake/sandbox. | Signature and idempotency rules hold; transaction/reconciliation outcomes match approved decisions. | `[Planned]`; conflict precedence is `[Needs Team Review]`. |
| Edge/robot command contract testing | Verify command pull, acknowledgement, reports, transport guards, provenance, idempotency, timeout observation, and REST/MQTT handler equivalence where supported. | Cloud records and responses match the approved contract; no claim is made about unobserved physical execution. | `[Planned]` |
| Performance/capacity testing | Measure selected API, query, ingestion, job, and database workloads under approved profiles. | Numeric thresholds and workload model `[Needs Team Review]`. | `[Needs Team Review]` |

### 2.2 Test Levels

Legend: **P** = planned at this level; **R** = requires team/environment decision; **—** = not the primary level.

| Testing type | Unit | Integration | System | Acceptance |
|---|:---:|:---:|:---:|:---:|
| Domain/business-rule testing | P | P | P | P |
| API contract testing | P | P | P | P |
| Database mapping/constraint testing | P | P | P | — |
| Authentication/authorization testing | P | P | P | P |
| Payment webhook testing | P | P | P | R |
| MQTT/Edge contract testing | P | P | P | R |
| Background/recovery testing | P | P | P | P |
| Realtime/GraphQL testing | P | P | P | R |
| Performance/capacity testing | — | R | R | R |
| Physical robot/hardware-in-the-loop testing | — | R | R | R |

Level boundaries and evidence rules:

| Level | Subject under test | Dependency boundary | Required evidence |
|---|---|---|---|
| Unit | One domain rule, validator, policy, or handler branch. | In-process with controlled substitutes; no claim of provider/database interoperability. | Runner output plus exact case/data/assertion and source baseline. |
| Integration | Two or more backend components or a real isolated infrastructure adapter, such as API/application/persistence, PostgreSQL, broker, or object storage. | Approved isolated dependencies; external fake and sandbox results are reported separately. | Environment manifest, logs/output, database/adapter observations, and cleanup result. |
| System | Deployed backend flow across its configured components and approved simulators/sandboxes. | Networked runtime boundary; may include client/Edge simulator but not unsupported physical outcome claims. | Build/environment IDs, end-to-end observations, evidence links, and defects. |
| Acceptance | Team/product confirmation of an approved business expectation. | Approved acceptance environment and authorized representatives. | Signed decision, conditions, accepted risks, and linked executed cases. |

One execution record belongs to one primary test level and is counted once in level statistics. If the same scenario is required at several levels, it receives separate case variants/IDs in the detailed workbook.

### 2.3 Supporting Tools

Only infrastructure established by repository evidence is named. Selection of test frameworks and operator tools remains pending.

| Purpose | Supported platform or required tool | Status / note |
|---|---|---|
| Backend build/test runner | `[.NET test framework and runner — Needs Team Review]` | Test project names are referenced by evidence, but framework/version/execution baseline was not established. |
| API/GraphQL testing | `[API/GraphQL test client — Needs Team Review]` | Do not assume a specific commercial/open-source client. |
| Database testing | PostgreSQL/Npgsql/EF Core; `[schema inspection tool — Needs Team Review]` | PostgreSQL integration is supported; exact versions and reset tooling require approval. |
| MQTT contract testing | Mosquitto-compatible broker; `[MQTT test client/harness — Needs Team Review]` | Broker role is supported; test client is unknown. |
| Object-storage testing | MinIO/S3-compatible test instance; `[test harness — Needs Team Review]` | Do not use production buckets or credentials. |
| Payment testing | `[PayOS sandbox or approved fake — Needs Team Review]` | Production webhook replay is not authorized by this plan. |
| Identity/push testing | `[Firebase/Google/FCM sandbox or approved fake — Needs Team Review]` | Credentials and quotas require team approval. |
| Edge/robot testing | `[Edge simulator, Local Edge test build, or approved device rig — Needs Team Review]` | Physical robot hardware and safety procedures are unknown. |
| Performance/security testing | `[Approved load/security tools — Needs Team Review]` | Requires explicit targets, authorization, and isolated environment. |
| Defect/test management | `[Team-selected test/defect tracker — Needs Team Review]` | IDs must remain traceable to TC and requirement IDs. |

External-dependency evidence must identify its boundary. A deterministic fake supports backend branch, validation, retry, and signature-fixture testing; it does not establish live interoperability. An approved provider sandbox supports provider-contract compatibility for the exact sandbox/version/configuration tested. Fake and sandbox executions must use separate case variants and must not be merged into one result.

## 3. Test Plan

### 3.1 Human Resources

| Test responsibility | Assigned person | Responsibilities | Status |
|---|---|---|---|
| Test lead / coordinator | `[Name — Needs Team Review]` | Own scope, schedule, entry/exit criteria, reporting, and sign-off routing. | `[Needs Team Review]` |
| Backend test owner | `[Name — Needs Team Review]` | Unit, handler, API, job, integration, and defect reproduction tests. | `[Needs Team Review]` |
| Database reviewer/tester | `[Name — Needs Team Review]` | Model/schema reconciliation, constraints, migrations, transactions, retention, and restore checks. | `[Needs Team Review]` |
| Security/authorization reviewer | `[Name — Needs Team Review]` | Role/scope matrix, cross-tenant access, credentials, replay, signatures, and diagnostics exposure. | `[Needs Team Review]` |
| Payment integration tester | `[Name — Needs Team Review]` | Session, callback, duplicate/late/conflict, reconciliation, and refund scenarios. | `[Needs Team Review]` |
| Edge/IoT/robot contract tester | `[Name — Needs Team Review]` | REST/MQTT command/evidence contracts, simulator/device setup, and physical-boundary reporting. | `[Needs Team Review]` |
| Frontend/tablet acceptance tester | `[Name — Needs Team Review]` | Cross-client workflows and UI-visible results if client systems are in scope. | `[Needs Team Review]` |
| Product/acceptance representative | `[Name — Needs Team Review]` | Approve business expectations, known limitations, and acceptance disposition. | `[Needs Team Review]` |

### 3.2 Test Environment

| Environment component | Required configuration | Data / isolation requirement | Status |
|---|---|---|---|
| Backend application | Approved IceBot Backend build/commit and test configuration profile. | Isolated secrets and repeatable configuration. | `[Needs Team Review]` |
| Database | PostgreSQL-compatible isolated database with approved migrations/model snapshot. Current configuration indicates PostgreSQL 17. | Repeatable seed/reset; no production data. | `[Needs Team Review]` |
| Object storage | Isolated MinIO/S3-compatible bucket for robot artifacts. | Disposable objects; verify DB/object-store partial failures. | `[Needs Team Review]` |
| MQTT broker | Isolated Mosquitto-compatible broker with test topics/credentials. | No retained command wake-ups; controlled duplicate/malformed/oversize messages. | `[Needs Team Review]` |
| PayOS | Approved sandbox or deterministic fake with signature generation. | Test provider identifiers and callback replay scenarios only. | `[Needs Team Review]` |
| External identity / push | Approved Firebase/Google/FCM sandbox or fakes. | Non-production accounts/tokens/devices. | `[Needs Team Review]` |
| Edge runtime | Approved simulator or Local Edge test build implementing pull/ack/report and uplink contracts. | Deterministic clock, endpoint identity, sequence, retry, and offline controls. | `[Needs Team Review]` |
| Robot/device rig | Optional approved hardware-in-the-loop environment. | Safety owner, physical containment, calibration, cleanup, and observation protocol required. | `[Needs Team Review]` |
| Client applications | Approved tablet/management builds if system/acceptance scope includes them. | Test accounts, kiosk assignment, and reset procedure. | `[Needs Team Review]` |
| Observability | Approved logs, metrics, traces, database inspection, and broker inspection. | Sensitive payload redaction and evidence-retention policy. | `[Needs Team Review]` |

Each execution cycle must attach an immutable environment manifest. All values are `[Needs Team Review]` until the environment is frozen.

| Manifest field | Required value |
|---|---|
| Environment ID and purpose | `[Needs Team Review]` |
| Source commit/build/package checksum | `[Needs Team Review]` |
| OS/container runtime and architecture | `[Needs Team Review]` |
| .NET SDK/runtime and application configuration profile | `[Needs Team Review]` |
| Database engine/image, schema/model-snapshot, and last migration ID | `[Needs Team Review]` |
| Broker and object-storage image/version/configuration | `[Needs Team Review]` |
| External fake/sandbox identity and fixture revision | `[Needs Team Review]` |
| Edge/client simulator or build revision | `[Needs Team Review]` |
| Network topology, endpoint URLs, and isolation controls | `[Needs Team Review]` |
| Clock/time zone/time-control method | `[Needs Team Review]` |
| Seed-data revision, reset/cleanup procedure, and test-data classification | `[Needs Team Review]` |
| Observability/evidence location and retention policy | `[Needs Team Review]` |

### 3.3 Test Milestones

Exact dates depend on the approved capstone schedule.

| Milestone | Planned activities | Entry criterion | Exit evidence | Target phase/status |
|---|---|---|---|---|
| M1 — Test baseline approval | Freeze build, SRS/RTM revision, scope, tools, roles, and environments. | Review package available. | Approved baseline record and assigned owners. | `[Capstone phase — Needs Team Review]` |
| M2 — Unit and component tests | Execute critical domain/handler/policy tests. | Buildable test baseline and deterministic dependencies. | Unit execution report and defects. | `[Milestone — Needs Team Review]` |
| M3 — Database/API integration | Execute EF/schema, REST/GraphQL, persistence, and transaction cases. | Isolated database and migrations ready. | Integration results, schema evidence, defects. | `[Milestone — Needs Team Review]` |
| M4 — External contract testing | Execute PayOS, MQTT, object storage, identity/push, and Edge simulator cases. | Approved fakes/sandboxes/credentials. | Contract results and failure/retry evidence. | `[Milestone — Needs Team Review]` |
| M5 — System and recovery testing | Execute checkout/payment/dispatch/evidence, incidents, jobs, dead letters, and operational recovery. | Integrated environment stable. | System results and known-issue list. | `[Milestone — Needs Team Review]` |
| M6 — Security and NFR testing | Execute authorization, replay, reliability, performance/security tests with approved thresholds. | Scope and authorization approved. | NFR measurements and security findings. | `[Milestone — Needs Team Review]` |
| M7 — Acceptance and regression | Run approved business scenarios and regression suite. | Blocking defects resolved or accepted. | Acceptance record and final coverage. | `[Milestone — Needs Team Review]` |
| M8 — Report finalization | Populate Section 5 and companion spreadsheets from verified data. | Execution evidence frozen. | Signed Report 5 and archived test artifacts. | `[Milestone — Needs Team Review]` |

#### Test Cycle Controls

| Control | Rule | Approval / unresolved value |
|---|---|---|
| Entry criteria | Source/SRS/RTM/STM baseline frozen; environment manifest complete; tools/data/credentials available; safety/security/provider authorization recorded; expected results deterministic or explicitly blocked. | `[Needs Team Review]` |
| Exit criteria | All mandatory cases reach an allowed final status; blocking defects are closed or formally accepted; regression/retest completed; coverage and statistics reconcile to the case register. | Allowed defect/coverage thresholds and approver are `[Needs Team Review]`. |
| Suspension | Suspend affected testing for unsafe hardware state, environment corruption, invalid baseline, exposed credentials/data, nondeterministic expected result, or a blocker preventing reliable evidence. | Test lead/authorization owner `[Needs Team Review]`. |
| Resumption | Root cause/environment/baseline corrected, affected evidence invalidated as necessary, and resumption approved and recorded. | Approver `[Needs Team Review]`. |
| Defect severity | Proposed categories: Blocker, Critical, Major, Minor, Editorial. Definitions and allowed open counts require approval before execution. | `[Needs Team Review]` |
| Retest/regression | Every resolved defect is retested on an identified build; impacted requirements and neighboring workflows receive documented regression selection. | Selection/approval owner `[Needs Team Review]`. |
| Requirement uncertainty | If no approved expected result exists, use Blocked/Needs Decision in the execution register; current repository behavior must not silently become acceptance criteria. | Product/architecture decision owner `[Needs Team Review]`. |
| Change control | SRS/RTM/STM, code, migration, environment, fixture, or case changes create a new revision/baseline; added/removed/deferred cases retain change history. | Change authority `[Needs Team Review]`. |
| Evidence retention | Preserve raw runner output, logs, database/adapter observations, screenshots where applicable, checksums/links, defects, and sign-off under an approved access/retention policy. | Location/duration `[Needs Team Review]`. |
| External/security/hardware authorization | Production credentials/data are prohibited. Sandbox/security testing and physical rig operation require explicit authorized owner and, for hardware, safety sign-off and supervision. | Authorized persons `[Needs Team Review]`. |

## 4. Test Cases

The following catalogue defines high-level cases for later expansion in the university companion spreadsheets. Preconditions and steps are summaries, not executable scripts. No listed case has been executed as part of this documentation task.

**Execution-unit rule:** Rows that map an FR/NFR range are scenario groups, not one pass/fail execution. They must remain `[Planned]` in this document until the companion Software Test Matrix expands every requirement and material subclaim into independently executable level-specific variants with exact data, steps, assertions, cleanup, and result fields. Supported and uncertain subclaims must be separate; an unresolved expected result is marked Blocked/Needs Decision during execution.

Companion-artifact control:

| Artifact | Required content | Approved location / revision | Status |
|---|---|---|---|
| `Report5_Unit Test.xls` | Unit cases and parameter/data rows with exact assertions and evidence. | `[Needs Team Review]` | Submission blocker |
| `Report5_Test Report.xlsx` | Integration/System/Acceptance case register, execution tracking, defects, and statistics. | `[Needs Team Review]` | Submission blocker |
| Software Test Matrix (STM) | Requirement/subclaim → RTM confidence → case/variant → level/type → result → defect → build/environment/evidence. | `[Needs Team Review]` | Submission blocker |

In this documentation package, **RTM** means the existing Requirements Traceability Matrix; **STM** means the future test-specific traceability matrix. The STM must preserve the RTM's weakest-material-component status and may not turn `[Inferred]`, `[Unclear]`, or `Needs Review` subclaims into unconditional acceptance criteria.

| Test Case ID | Requirement ID | Feature / Module | Test Level | Test Type | Preconditions | Test Steps Summary | Expected Result | Priority | Status |
|---|---|---|---|---|---|---|---|---|---|
| TC-IDN-001 | FR-001, NFR-010 | Local authentication | Integration | Security | Active local account exists. | Submit valid credentials; then submit five invalid attempts and retry during lockout. | Valid login issues tokens; invalid credentials are rejected; account is locked for the evidenced interval after the threshold. | High | `[Planned]` |
| TC-IDN-002 | FR-002 | External login | Integration | Security | Approved identity fake/sandbox and account mapping exist. | Submit valid token, first-bind subject, then submit mismatched subject. | Valid identity is accepted/bound as specified; mismatch is rejected. | High | `[Planned]` |
| TC-IDN-003 | FR-003–FR-006, NFR-009, NFR-011 | Session/password lifecycle | Integration | Security | Active account, refresh/reset tokens, controlled clock. | Refresh, revoke, reset, and change password; retry revoked/expired/used tokens. | Only valid tokens succeed; credential changes revoke sessions as specified; forgot-password response does not enumerate accounts. | High | `[Planned]` |
| TC-IDN-004 | FR-009–FR-010 | Invitation onboarding | Integration | Functional | Authorized assigner and valid role/scope. | Create invitation, regenerate, accept, and retry prior/used token; exercise temporary-password variant separately. | One active invitation is enforced; accepted/old tokens behave as specified; temporary-password limitation is recorded. | High | `[Planned]` |
| TC-IDN-005 | FR-013–FR-016, NFR-007 | Scoped RBAC | System | Authorization | Accounts for every role and org/store/kiosk scope. | Exercise allowed, lower/higher role, cross-tenant, mixed-scope, REST, GraphQL, and SignalR access. | Only approved role-plus-scope combinations succeed; cross-scope composition is rejected. Universal matrix remains `[Needs Team Review]`. | Critical | `[Planned]` |
| TC-IDN-006 | FR-007 | Own profile and effective access | Integration | Functional / API / authorization | Authenticated active account. | Read/update allowed profile fields; request effective access; attempt another account or unsupported field change. | Own profile/access is returned or updated within the contract; cross-account/unsupported changes are rejected. | Medium | `[Planned]` |
| TC-IDN-007 | FR-008 | Push-notification registrations | Integration | Functional / API / security | Authenticated account and test installation/push-token values. | Register, refresh, list, and unregister; reuse one push token from another installation. | Registration lifecycle applies and prior active ownership of the same token hash is invalidated as specified. | Medium | `[Planned]` |
| TC-IDN-008 | FR-011–FR-012 | Internal account administration | Integration | Functional / API / authorization | Authorized manager and in-scope/out-of-scope target accounts. | List/view/update/disable an account; set/reset password; attempt unauthorized/out-of-scope operations. | Scope/policy guards apply; disable/password reset revokes sessions where specified. | High | `[Planned]` |
| TC-TEN-001 | FR-017–FR-019 | Tenant lifecycle | Integration | Functional | Authorized tenant administrators. | Create/update/activate/disable organization, store, and kiosk; attempt invalid parent/status transitions. | Valid lifecycle changes persist; invalid or out-of-scope transitions are rejected. | High | `[Planned]` |
| TC-TEN-002 | FR-020, NFR-001 | Franchise onboarding | Integration | Functional / recovery | Valid onboarding request and idempotency key. | Start twice, interrupt, resume, cancel eligible state, and simulate checkpoint failure/concurrent claim. | Duplicate start reuses result; resume does not recreate completed resources; cancellation/lease guards apply. | High | `[Planned]` |
| TC-TEN-003 | FR-021 | Role-scope options and tenant tree | Integration | Functional / API / authorization | Authenticated users with different role/scope assignments. | Query assignable scope options and tenant tree; attempt cross-scope visibility. | Results are limited to caller-assignable/visible tenant scope; removed REST route is not assumed to exist. | Medium | `[Planned]` |
| TC-DEV-001 | FR-022–FR-025 | Device/endpoint management | Integration | Functional / security | Active tenant/kiosk and device catalog. | Register/update/retire/replace device; provision/rotate/revoke endpoint credentials; try incompatible/cross-kiosk data. | Valid state persists; compatibility, uniqueness, active-execution, and tenant guards reject invalid operations. | High | `[Planned]` |
| TC-DEV-002 | FR-027–FR-031 | Heartbeat/events/telemetry/readiness | System | Edge contract | Authenticated endpoint and controlled timestamps. | Ingest valid, duplicate, replayed, malformed, and stale observations; run connectivity reconciliation. | Valid evidence updates records/projections; invalid input is rejected/deduplicated; timeout changes connectivity observation as coded. | High | `[Planned]` |
| TC-DEV-003 | FR-026 | MQTT subscriber credential lifecycle | Integration | Security / contract / recovery | Authorized operator, endpoint, isolated broker, and approved credential adapter. | Provision, rotate, revoke, and reconcile credentials; inject broker-success/DB-failure and DB-success/broker-failure. | Supported lifecycle state is recorded; raw password is returned only as specified. Partial-failure compensation remains `[Needs Team Review]`. | Critical | `[Planned]` |
| TC-DEV-004 | FR-027 | Kiosk heartbeat family | Integration | Contract / API / reliability | Authenticated endpoint and controlled clock. | Submit valid, repeated, stale/skewed, wrong-endpoint, REST, and MQTT-routed heartbeat inputs where supported. | Heartbeat persistence/projection follows its own identity/time rules; no other message-family deduplication rule is inferred. | High | `[Planned]` |
| TC-DEV-005 | FR-028 | Device-event family | Integration | Contract / API / operations | Authenticated endpoint and known device/kiosk. | Submit valid, repeated, malformed, cross-kiosk, and alert-triggering events over supported transports. | Device-event evidence and correlated alert behavior match FR-028; exact duplicate key is asserted only from the owning contract. | High | `[Planned]` |
| TC-DEV-006 | FR-029 | Batched telemetry family | Integration | Contract / API / reliability | Authenticated endpoint and telemetry batch fixtures. | Submit ordered, repeated, malformed, mixed-device, and replay batches over supported transports. | Accepted telemetry persists per its batch/replay contract; failures do not inherit heartbeat/event rules. | High | `[Planned]` |
| TC-DEV-007 | FR-030 | Readiness-snapshot family | Integration | Contract / API | Authenticated execution endpoint. | Submit valid, repeated, stale, malformed, and wrong-endpoint readiness snapshots over supported transports. | Current readiness projection follows its own replacement/time rules; unknown conflict precedence is `[Needs Team Review]`. | High | `[Planned]` |
| TC-DEV-008 | FR-032 | Kiosk status and telemetry reads | Integration | Functional / API / authorization | Scoped users and seeded connectivity/telemetry data. | Query overview/history with scope, filters, and diagnostics permissions. | Curated data is correctly scoped; raw diagnostic data is not exposed through the curated surface. | High | `[Planned]` |
| TC-CAT-001 | FR-033–FR-041 | Catalog and recipe lifecycle | Integration | Functional | Authorized manager and required catalog parents. | Author products/options/recipes; publish/activate/retire; attempt invalid/default-conflicting transitions. | Lifecycle, composition, uniqueness, and preflight rules are enforced. | High | `[Planned]` |
| TC-SC-001 | FR-042–FR-047, NFR-013 | Menu and runtime projection | System | Functional / cache | Scoped catalog, menu, kiosk, and configuration data. | Publish menu/items; request runtime menu with/without ETag; alter availability/configuration. | Projection and option filtering match supported rules; cache/304 behavior respects bounded interval. Runtime-menu source is `[Needs Team Review]`. | High | `[Planned]` |
| TC-INV-001 | FR-048–FR-054 | Dispenser and stock movement | Integration | Functional / database | Provisioned device/container and ingredient. | Refill, adjust, consume, rebind, retire/reactivate; retry or race applicable operations. | Balanced/auditable movements and topology records persist; invalid active-execution/topology operations are rejected. | High | `[Planned]` |
| TC-INV-002 | FR-055–FR-056 | Readiness and calibration | Unit | Functional | Recipe requirements and calibrated dispenser data. | Evaluate sufficient/insufficient/invalid calibration and option requirements. | Readiness result and validation reasons match supported rules; no unsupported stock gate is inferred for runtime menu. | High | `[Planned]` |
| TC-ORD-001 | FR-057, NFR-001 | Checkout | Integration | Functional / idempotency | Active kiosk/store/menu and valid selection. | Submit valid checkout twice with same key; alter price/status/options; submit invalid/cross-kiosk selection. | One logical order is created; server totals/snapshots are authoritative; invalid selection is rejected. | Critical | `[Planned]` |
| TC-ORD-002 | FR-058–FR-060 | Order status/cancellation/redispatch | Integration | Functional / authorization | Orders in unpaid, paid, active, and terminal states. | Read with correct/incorrect order token; cancel as customer/manager; request redispatch in allowed/disallowed states. | Access and state guards are enforced; required refund/support flags and reasons are recorded where specified. | High | `[Planned]` |
| TC-ORD-003 | FR-061–FR-067 | Fulfilment, remake, incident, realtime | System | Functional / recovery | Order items in applicable fulfilment/execution states. | Apply fulfilment events, request remake, open/inspect/resolve incident, subscribe/refetch status. | Idempotent valid transitions persist; inspection precedes resolution; realtime delta agrees with authoritative refetch. | Critical | `[Planned]` |
| TC-PAY-001 | FR-068–FR-069, NFR-001 | Payment session/status | Integration | Payment / idempotency | Eligible unpaid order and approved PayOS fake/sandbox. | Create session twice with same key; query status; use wrong amount/currency/state. | One logical session/result is used; invalid paid-eligibility or amount/currency is rejected. | Critical | `[Planned]` |
| TC-PAY-002 | FR-070, NFR-012 | PayOS webhook | System | Payment / security | Deterministic signed callback generator and eligible transaction. | Send valid, invalid-signature, duplicate, late, and conflicting callbacks. | Invalid signature changes no state; valid callback applies supported transition idempotently. Late/conflict precedence is `[Needs Team Review]`. | Critical | `[Planned]` |
| TC-PAY-003 | FR-071–FR-073, NFR-003 | Payment reconciliation | Integration | Payment / reliability / recovery | Pending/stale sessions and controlled provider responses. | Run manual and scheduled reconciliation; inject provider timeout/failure and retry exhaustion. | Coded retry/intervention states and notifications are recorded without unsupported guaranteed recovery. | High | `[Planned]` |
| TC-PAY-004 | FR-075–FR-078 | Refund lifecycle | Integration | Payment / recovery | Eligible transaction/order/incident and authorized manager. | Request, process, reject, cancel refund; omit mandatory reasons; exercise realtime/push path. | Allowed transitions and reasons are enforced; current manual refund outcome is recorded; provider payout/voucher is not invented. | High | `[Planned]` |
| TC-PAY-005 | FR-074 | Payment-method catalogue | Integration | Functional / API / authorization | Authorized management user and payment-method records. | List/read methods; apply valid/invalid lifecycle/status change; race concurrent changes; create session using enabled/disabled method. | Authorization and lifecycle guards apply; session eligibility reflects the approved method state. | High | `[Planned]` |
| TC-SYNC-001 | FR-125, FR-130 | Dispatch and MQTT wake-up | System | Contract / reliability | ReadyForFulfillment order and target endpoint. | Characterize immediate/concurrent/reconciled dispatch; fail/miss broker publish; let Edge poll. | Durable command remains pullable independently of MQTT; exact duplicate/concurrency outcome is `[Needs Team Review]`; repair may end in support state. | Critical | `[Planned]` |
| TC-SYNC-003 | NFR-002 | Edge/Cloud offline tolerance | System | Reliability / recovery / contract | Edge simulator can disconnect/reconnect; durable command/event data exists. | Disconnect across wake-up, pull, acknowledgement, report, and uplink; reconnect and replay according to approved contract. | Durable Cloud state remains consistent and coded retry/dead-letter/manual states are observable; physical outcome is not inferred. | Critical | `[Planned]` |
| TC-IOT-001 | FR-120 | Edge command pull | Integration | Edge contract / security | Authenticated endpoint with pending commands. | Pull within/max limit, wrong endpoint/tenant, expired/unavailable commands, repeated pull. | Only eligible commands for endpoint are returned; delivery attempt and supported artifact URL enrichment are recorded. | Critical | `[Planned]` |
| TC-IOT-002 | FR-121 | Edge acknowledgement | Integration | Contract / API | Delivered command and controlled clock. | Submit exact statuses `Received`, `Accepted`, `Rejected`, `ExecutorBusy`, and `DeliveryFailed`; vary `PhysicalOutputMayHaveOccurred`, `LocalStatePersisted`, rejection code/message, timestamp/skew, endpoint ownership, and repeat/invalid transitions. | Valid field/state combinations and order projections apply; invalid identity/transition/skew is rejected; acknowledgement remains distinct from execution evidence and does not prove physical result. | Critical | `[Planned]` |
| TC-IOT-003 | FR-122 | Execution report | System | Edge contract / idempotency | Accepted command and known provenance checksum. | Submit valid report over REST/MQTT, duplicate sequence, conflicting checksum, and out-of-order evidence. | Valid report applies once and updates Cloud projections; invalid provenance/sequence is rejected or handled by coded rule. | Critical | `[Planned]` |
| TC-IOT-004 | FR-123–FR-124 | Production sync/checkpoint/state | System | Sync / recovery | Authenticated endpoint with event stream/checkpoint. | Ingest ordered, duplicate, gapped, malformed, and conflicting batches/state summaries. | Durable ingestion/checkpoint/dead-letter behavior matches the owning contract; conflict precedence remains `[Needs Team Review]`. | High | `[Planned]` |
| TC-IOT-005 | FR-123–FR-124 | Production-event batch/checkpoint | Integration | Contract / idempotency / recovery | Authenticated endpoint, checkpoint, and event-batch fixtures. | Query checkpoint; submit ordered, duplicate, overlapping, gapped, malformed, and failed event batches. | Checkpoint/inbox/dead-letter effects follow the production-event contract; exact conflict rule is `[Needs Team Review]` where unevidenced. | High | `[Planned]` |
| TC-IOT-006 | FR-124 | Edge state-summary family | Integration | Contract / synchronization | Authenticated endpoint and state-summary fixtures. | Submit initial, repeated, newer, stale, malformed, and conflicting summaries via each supported transport. | State-summary upsert/validation follows its owning contract; no production-event sequence rule is generalized to this family. | High | `[Planned]` |
| TC-MQTT-001 | FR-126–FR-127, NFR-016 | MQTT uplink guards | Integration | Edge contract | Isolated broker and endpoint credentials. | Publish valid topics plus retained, malformed-topic, unauthorized, oversize, and duplicate messages. | Valid messages reach the same handlers as REST where supported; invalid messages are rejected; broker sharing alone does not assert duplicate-free processing. | High | `[Planned]` |
| TC-ROBOT-001 | FR-088–FR-101, NFR-022 | Robot artifacts/programs | System | Functional / storage | Isolated MinIO and authorized configuration user. | Upload/clone/validate/publish/retire/discard artifacts/programs; inject DB/object-store partial failures and checksum mismatch. | Publication gates and metadata/object consistency behave as specified; cleanup/recovery gaps are recorded. | High | `[Planned]` |
| TC-PC-001 | FR-102–FR-110 | Configuration release/deployment | System | Functional / recovery | Valid catalog/robot routes, endpoint, and inventory readiness data. | Draft/publish/preview/deploy/rollback; fail validation/readiness; omit report and run timeout reconciliation. | Valid records/commands/transitions persist; invalid deployment is gated; timeout does not prove physical installation. | Critical | `[Planned]` |
| TC-PP-001 | FR-111–FR-119 | Package install/upgrade | System | Functional / recovery | Published package/version and target organization. | Preview/install/retry/repair/fork/upgrade/cutover/rollback/abandon; race active upgrades; run stale reconciliation. | Provenance and partial-unique active-upgrade rule hold; reported state does not independently prove physical installation. | High | `[Planned]` |
| TC-OPS-001 | FR-079–FR-087 | Alerts/tickets/logs/notifications | Integration | Functional / authorization | Device/inventory evidence and authorized operators. | Create/correlate/ack/resolve alert; create/work/cancel ticket; query curated/raw logs; fail/requeue notification. | Lifecycle, reason, scope, diagnostics-policy, retry, and realtime behavior match cited paths. | High | `[Planned]` |
| TC-SYNC-002 | FR-132 | Dead-letter operations | Integration | Recovery / authorization | Dead letters of ExecutionReport and other event types. | List/inspect; retry both supported and unsupported types; resolve/ignore with audit note. | ExecutionReport retry follows supported path; unsupported types return documented limitation; terminal actions persist. | High | `[Planned]` |
| TC-JOB-001 | FR-031, FR-065, FR-073, FR-086, FR-101, FR-110, FR-119, FR-130–FR-131 | Hosted/background jobs | Integration | Recovery / concurrency | Controlled clock, stale records, multiple worker instances where applicable. | Trigger each job; inject transient failure, concurrent run, retry exhaustion, and manual-state outcome. | Each job performs only its coded transition; advisory lock works where evidenced; no universal recovery is claimed. | High | `[Planned]` |
| TC-READ-001 | FR-128–FR-129, FR-133 | SignalR/GraphQL/dashboard | System | API / authorization | Scoped users and seeded operational data. | Join allowed/disallowed channels; query scoped aggregates; trigger invalidation and refetch. | Authorization/scope is enforced on cited paths; push is a delta and authoritative query remains consistent with committed state. | High | `[Planned]` |
| TC-DB-001 | NFR-004, DR-11 | Delete behavior | Integration | Database | Reconciled EF model/schema and related parent/child rows. | Inspect model/migration and attempt deletes for default Restrict and explicit Cascade candidates. | Effective behavior is recorded without assumption; protected evidence is not unintentionally deleted. | Critical | `[Planned]` |
| TC-DB-002 | NFR-005, DR-02 | Precision and key generation | Integration | Database | Migrated isolated database. | Persist boundary decimal values and GUID/long-key entities. | Decimal precision and key-generation strategy match mapped model. | Medium | `[Planned]` |
| TC-DB-003 | BR-12, DR-05 | Partial unique business rules | Integration | Database / concurrency | Rows for each of six enumerated invariants. | Insert valid rows, conflicting active/default rows, terminal/retired/deleted alternatives, and concurrent conflicts. | Database enforces each exact predicate; enum-to-numeric mapping is verified and documented. | Critical | `[Planned]` |
| TC-DB-004 | NFR-023, DR-13 | Tenant-consistency FKs | Integration | Security / database | Two organizations with endpoints/devices/releases. | Persist each enumerated matching and cross-tenant composite-FK relationship. | Matching tenant rows persist; cross-tenant rows are rejected for enumerated constraints; non-enumerated coverage is documented. | Critical | `[Planned]` |
| TC-DB-005 | BR-05, DR-14 | Soft-delete visibility/uniqueness | Integration | Database / security | Active/deleted records for filtered and 12 excluded principal types. | Query via applicable APIs/stores; attempt key reuse across deleted rows; inspect filter application. | Visibility and uniqueness match exact conventions/indexes; any missing explicit `WhereNotDeleted()` is reported. | Critical | `[Planned]` |
| TC-DB-006 | NFR-006, NFR-014 | History and retention | Integration | Audit / reliability | History/evidence rows across retention cutoffs. | Change states; attempt applicable update/delete; run bounded retention with >1 batch and failure injection. | Required histories/snapshots persist; retention is bounded; append-only enforcement layer and asymmetries are reported. | High | `[Planned]` |
| TC-NFR-001 | NFR-013 | Runtime-menu cache | Integration | Performance / functional | Stable menu projection and controlled clock. | Request initial resource, conditional request within interval, modify source, and request after invalidation/expiry. | ETag/conditional response and bounded freshness match requirement. | Medium | `[Planned]` |
| TC-NFR-002 | NFR-020 | Diagnostic data separation | Integration | Security | Curated and diagnostics users/endpoints. | Request each cited surface with/without diagnostics permission. | Raw payload/provenance appears only on authorized diagnostics paths; universal coverage gaps are reported. | High | `[Planned]` |
| TC-NFR-003 | NFR-024 | Distributed job coordination | Integration | Concurrency | Two service instances and supported advisory-lock job. | Start same job concurrently; force holder failure/release; retry. | At most one lock holder performs protected work at a time; cleanup/retry behavior is recorded. | High | `[Planned]` |
| TC-NFR-004 | NFR-017, NFR-025 | Index/partition design verification | Integration | Database / performance | Reconciled schema and approved query plans/workload. | Inspect required indexes, run representative range queries, confirm partition metadata. | Present/missing/partial indexes and absence of native partitioning match evidence; performance threshold remains `[Needs Team Review]`. | Medium | `[Planned]` |
| TC-NFR-005 | NFR-015, NFR-018 | Architecture dependency boundaries | Unit | Architecture / static verification | Approved source baseline and dependency rules. | Inspect project references/namespaces and run the team-approved dependency check; separately review the independent-evolution rationale. | Supported layer/context dependency constraints are reported; inferred organizational/evolution benefits are not converted into runtime pass criteria. | High | `[Planned]` |
| TC-NFR-006 | NFR-019 | Global persistence conventions | Integration | Database / static verification | Reconciled EF model and migrated isolated database. | Inspect and persist representative decimal, string, `*Json`, GUID/long key, origin/version, and organization-scoped entities. | Each evidenced global convention maps as specified; exceptions/drift are reported. | High | `[Planned]` |
| TC-NFR-007 | NFR-021, FR-131 | Periodic execution metrics | Integration | Reliability / observability | Controlled clock and stale/unreachable execution records. | Trigger metrics publication over successive intervals and change source states. | Published counts reflect the cited stale/unreachable states at the configured interval; monitoring acceptance thresholds remain `[Needs Team Review]`. | Medium | `[Planned]` |
| TC-SEC-001 | NFR-008 | Edge endpoint authentication | System | Security | mTLS and ECDSA test endpoint profiles, test certificates/keys/nonces. | Send valid/invalid fingerprint or signature, reused nonce, wrong endpoint/profile, and skewed request. | Only correctly authenticated, fresh requests succeed; nonce/profile guards apply. | Critical | `[Planned]` |
| TC-SEC-002 | NFR-011 | Secret persistence | Integration | Security | Token, invitation, reset, and MQTT credential workflows. | Generate credentials; inspect permitted persisted fields/logs/responses; rotate/revoke. | Raw protected secrets are not persisted where prohibited; one-time credential behavior is respected. | Critical | `[Planned]` |

Detailed row-level scripts, test data, actual outputs, timestamps, executor identity, build/database baseline, evidence attachments, and defect links must be maintained in `Report5_Unit Test.xls` and `Report5_Test Report.xlsx` (or the team-approved equivalent preserving the university structure). `[Needs Team Review]`

## 5. Test Reports

### Summary

**Execution status:** `[To Be Updated After Test Execution]`

**Build / source baseline:** `[To Be Updated After Test Execution]`

**Database / migration baseline:** `[To Be Updated After Test Execution]`

**Environment and execution period:** `[To Be Updated After Test Execution]`

**Overall assessment:** `[To Be Updated After Test Execution]`

Minimum provenance required before any case status is updated from `[Planned]`:

- executor and reviewer identity;
- local and UTC timestamp;
- source commit/build/package checksum;
- database model/schema/migration and environment-manifest ID;
- case/variant revision and exact data/fixture revision;
- actual result and observable assertions;
- evidence link and checksum where applicable;
- defect ID for a failure, or decision record for an accepted/blocked outcome;
- cleanup/reset result and retest linkage where applicable.

Execution-status semantics must be approved before use: **Passed** means every assertion for that execution unit met its approved expected result; **Failed** means at least one assertion did not; **Blocked** means execution or deterministic evaluation could not proceed because of an identified dependency/decision; **Not Run** means no execution began; **Retest** is a new linked execution after a change and does not overwrite the prior record. Definitions/approval authority remain `[Needs Team Review]`.

### Test Execution Statistics

| Test level/type | Planned | Executed | Passed | Failed | Blocked | Not run | Pass rate |
|---|---:|---:|---:|---:|---:|---:|---:|
| Unit | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` |
| Integration | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` |
| System | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` |
| Acceptance | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` |
| Total | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` |

No chart shall be inserted until verified totals exist. Any later chart must state the underlying counts and baseline.

### Defect Summary

| Defect ID | Related TC | Requirement | Severity | Status | Summary | Owner | Resolution evidence |
|---|---|---|---|---|---|---|---|
| `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` |

### Requirement Coverage Summary

| Requirement group | Total requirements | Requirements with planned cases | Requirements executed | Requirements passed | Gaps / accepted exclusions |
|---|---:|---:|---:|---:|---|
| FR-001–FR-133 | 133 | `[Needs Team Review]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` |
| NFR-001–NFR-025 | 25 | `[Needs Team Review]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` |
| Business/data requirements | `[Needs Team Review]` | `[Needs Team Review]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` |

The high-level catalogue groups multiple requirements into risk-oriented cases and does not by itself prove complete row-level coverage. A final requirement-to-test matrix must map every approved SRS/RTM row to one or more detailed test cases or an accepted exclusion.

### Known Issues

| Issue ID | Description | Impact | Workaround / disposition | Related requirement/test | Status |
|---|---|---|---|---|---|
| `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` |

Pre-existing open questions in `deliverables/05_team_review/open_questions.md` are planning inputs, not defects, until testing or review demonstrates an actual nonconformance.

### Sign-Off / Acceptance Notes

| Approval role | Name | Decision | Conditions / accepted risks | Date / signature |
|---|---|---|---|---|
| Test lead | `[Needs Team Review]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` |
| Backend owner | `[Needs Team Review]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` |
| Product/acceptance representative | `[Needs Team Review]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` |
| Project manager/report owner | `[Needs Team Review]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` | `[To Be Updated After Test Execution]` |

**Final test disposition:** `[To Be Updated After Test Execution]`

Before conversion to DOCX, the team must approve the test baseline, expand this catalogue into the companion spreadsheets, assign owners, provision isolated environments, define measurable NFR/acceptance thresholds, execute the cases, archive evidence, reconcile defects and requirement coverage, and replace every execution placeholder with verified data.

Pre-submission audit requirements:

- search for every `[Needs Team Review]`, `[Planned]`, and `[To Be Updated After Test Execution]` token and confirm that no unresolved blocker is hidden;
- reconcile planned/executed/status totals by one primary level and controlled test-type tags;
- validate every requirement, subclaim, test case/variant, defect, build, environment, and evidence reference in the STM;
- ensure all reported executions use the declared baseline or explicitly identify a different cycle;
- preserve planned scope/priority/level separately from actual status/executor/date/evidence;
- record added, removed, split, or deferred cases through change control;
- obtain independent reviewer and authorized acceptance sign-off.
