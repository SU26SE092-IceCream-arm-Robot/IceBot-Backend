# Software Requirements Specification — IceBot Backend

**Document type**: Team-facing SRS baseline (working draft for internal alignment). This is **not** the final school-formatted report — it is a shared technical reference to be adapted into the formal SRS deliverable later.

**Source basis**: Derived strictly from `deliverables/00_repo_evidence/repo_truth_map.md`, `deliverables/00_repo_evidence/functional_inventory.md`, `deliverables/00_repo_evidence/database_inventory.md`, and `deliverables/01_project_introduction/project_introduction.md`, per the authoring rules in `deliverables/DELIVERABLES_AGENT.md`. No `src/` or `docs/` files were modified. A fifth requested input, `deliverables/00_repo_evidence/evidence_review_final.md`, does not exist in the repository at the time of writing; this document was produced from the four evidence/introduction files that do exist (see §8).

**Status legend** used throughout (per functional_inventory.md methodology, `functional_inventory.md:9-12`):
- **Supported** — directly observed in source code (route/consumer → handler → domain/persistence chain read in `src/`). This means **statically code-evidenced**, not runtime-verified: no test-execution, integration-test, or production-monitoring evidence backs this status anywhere in this document, since the evidence base does not map to `tests/IceBot.UnitTests`/`tests/IceBot.IntegrationTests` (see §8). Treat "Supported" as "the code exists and is wired," not "confirmed working at runtime."
- **Inferred** — reasonable reading of code/naming/docs where the exact mechanism was not independently re-verified line-by-line (e.g. confirmed only via controller/job wiring or a sibling-pattern match rather than a full method read).
- **Unclear** — described only in `docs/` with no corresponding code found, or a genuine gap/discrepancy flagged in the evidence, or a universal/absolute claim that was not checked exhaustively across every affected surface.

**A note on wording**: this document, following `functional_inventory.md`'s own requirement-candidate phrasing, uses words like "atomically," "exactly once," "idempotently," "recover," "guarantee," and "immutable" where the cited evidence source uses them. These describe the mechanism as coded/documented (e.g. a unique index, a single transaction scope, a status-machine guard) — they are **not** claims independently verified by concurrency testing, chaos testing, or production observation, none of which exist in the evidence base. Where this SRS's own wording (rather than the evidence file's) makes such a claim, it is called out individually below.

---

## 1. Introduction

### 1.1 Purpose

This Software Requirements Specification (SRS) documents the functional and non-functional requirements of the IceBot Backend as they exist in the current codebase, for use by the project team as a shared baseline before producing the final academic SRS report. It translates the repo-evidence inventories already gathered (`repo_truth_map.md`, `functional_inventory.md`, `database_inventory.md`) into standard SRS structure (functional requirements, non-functional requirements, data requirements, business rules), with each requirement traceable to one or more `functional_inventory.md` row IDs and their underlying evidence paths. `[Open Question]` A team review (`deliverables/05_team_review/codex_review_project_intro_srs.md`) found that several `functional_inventory.md` row IDs were originally cited only via an en-dash range (e.g. `TEN-01`–`TEN-04`) in a consolidated FR's `Related`/`Evidence` fields, making the interior IDs of that range non-obvious on a literal ID search; those ranges have been expanded to explicit, individually-listed IDs throughout §4 in response. The consolidation itself (260 inventory rows into 133 FRs) remains a many-to-one grouping, not a formal row-by-row traceability matrix — producing the latter is flagged as a follow-up in §8 rather than attempted here.

### 1.2 Scope

IceBot Backend is the **Cloud-side** ASP.NET Core backend of a multi-location automated vending platform with robot-arm order fulfillment. This SRS covers:

- Tablet/customer-facing checkout and order tracking (public v1 API).
- Internal management operations (organizations, stores, kiosks, catalog, menus, inventory, robot configuration, production configuration/packages, devices, operations, payments) via REST + GraphQL.
- The Edge/Cloud IoT contract (REST + MQTT) as consumed by this backend — the Edge/kiosk runtime itself is a separate system and is out of scope except where its contract with Cloud is documented.
- Payment provider integration (PayOS) and its webhook contract.
- Realtime push (SignalR) as produced by this backend.

Out of scope for this SRS (see also `project_introduction.md` §8 "Out-of-Scope Items"): the Edge runtime's internal implementation, any frontend/tablet/mobile client implementation details, automated provider refunds/payouts, a live Cloud-side robot scheduler, and database-native partitioning — none of these are implemented in this repository.

### 1.3 Definitions, Acronyms, and Abbreviations

| Term | Meaning |
|---|---|
| Cloud | The centralized backend (this repository): org/store/kiosk management, catalog/config authoring, payment integration, central coordination. |
| Edge | The local kiosk runtime that owns robot execution, local device communication, telemetry, and tolerates offline operation. |
| Kiosk | A single physical vending unit belonging to a Store, running a robot arm and supporting devices. |
| Execution Endpoint | A kiosk's registered runtime connection point (Full Edge or Low-Cost Controller profile) used for command dispatch/telemetry. |
| RBAC | Role-Based Access Control — role + organization/store/kiosk scope enforced per management endpoint. |
| CQRS-lite | Command/query separation at the Application layer without full event sourcing. |
| EF Core | Entity Framework Core, the ORM used for PostgreSQL persistence. |
| Recipe | A versioned, ingredient-based production instruction for a product variant. |
| Robot Artifact | An immutable exported Lua script (`.lua`) with checksum/metadata, used to build a Robot Program. |
| Robot Program | An ordered, reusable manifest of Robot Artifacts bound to a product/recipe context. |
| Configuration Release | An immutable, versioned manifest binding catalog variants/recipes to Robot Programs via Execution Routes, deployable to kiosks. |
| Production Package | A platform-level, reusable manifest (products, artifacts, programs, routes) that can be installed/upgraded per organization/store/kiosk for franchise rollout. |
| Dispenser / Container | A physical ingredient-holding unit tracked by an `IngredientDispenserState` entity. |
| PayOS | The current payment provider integrated via webhook. |
| Sync Inbox / Dead Letter | Durable staging and failure-handling tables for Edge→Cloud event ingestion. |
| Full Edge | An execution endpoint profile authenticated via mutual TLS (certificate fingerprint pinning). |
| Low-Cost Controller | An execution endpoint profile authenticated via ECDSA P-256 signed requests + nonce dedup. |

### 1.4 References

- `deliverables/DELIVERABLES_AGENT.md` — authoring rules for this deliverables folder.
- `deliverables/00_repo_evidence/repo_truth_map.md` — architecture, actors, modules, flows, API surface, database summary.
- `deliverables/00_repo_evidence/functional_inventory.md` — functional capability inventory with per-row evidence paths (primary FR source for §4). Its own Summary table totals 265 rows; a direct count of `ID`-prefixed rows yields 260 (Operations is short 4 rows against its stated 26, Payments is short 1 row against its stated 17). This SRS uses the mechanically-verified 260 figure and flags 265 as the file's own uncorrected total — see §8.
- `deliverables/00_repo_evidence/database_inventory.md` — entity list, relationships, constraints/indexes, JSON field roles, multi-tenancy fields, physical database notes (primary source for §6).
- `deliverables/01_project_introduction/project_introduction.md` — team-facing project overview and scope baseline.
- Backend source-of-truth docs cited transitively via the above (e.g. `ARCHITECTURE.md`, `docs/api/API_SURFACE_RULES.md`, `docs/api/AUTHORIZATION_RULES.md`, `docs/architecture/BOUNDARY_CONTEXTS.md`, `docs/architecture/MULTI_TENANCY_RULES.md`, `docs/flows/*.md`, `docs/iot/IOT_CONTRACT.md`, `docs/data/*.md`) — not re-read directly for this document; inherited via the evidence files above.

### 1.5 Document Overview

Section 2 gives the overall product description (perspective, functions, user classes, environment, constraints, assumptions). Section 3 specifies external interfaces (user, software, hardware/IoT, communication). Section 4 lists functional requirements (FR-001…) grouped by bounded context, each traced to `functional_inventory.md` evidence. Section 5 lists non-functional requirements (NFR-001…). Section 6 summarizes data requirements from `database_inventory.md`. Section 7 lists cross-cutting business rules. Section 8 collects assumptions and open questions carried from the evidence base.

---

## 2. Overall Description

### 2.1 Product Perspective

IceBot Backend is not a standalone product but the Cloud half of a two-tier system: Cloud (this repository) and Edge (kiosk-local runtime, external to this repo). It is built as a Modular Monolith with Clean Architecture layering (`WebAPI → Infrastructure → Application → Domain`), organized into bounded contexts rather than microservices. As currently configured, it integrates with one payment provider (PayOS), one identity provider (Firebase/Google), one object store (MinIO), one MQTT broker (Mosquitto), and one push-notification channel (FCM). `[Assumption]` The evidence establishes only the current configured adapter for each integration, not that the product architecturally requires exactly one instance of each or that these are permanent/sole providers — see `project_introduction.md` §11. Evidence: `repo_truth_map.md` §1–§2; `project_introduction.md` §3, §11.

### 2.2 Product Functions

At a high level (detailed in §4 and in `project_introduction.md` §9):
- Tenant management (organizations/stores/kiosks) and scoped RBAC identity.
- Catalog and sales-catalog (menu) authoring, with a runtime-menu projection served to kiosks.
- Inventory (dispenser) tracking feeding into deployment/menu readiness gating.
- Checkout, payment, and robot-execution order lifecycle, including incident and refund handling.
- Robot Lua artifact/program authoring and import pipeline.
- Production configuration release authoring and deployment (Full Edge / Low-Cost Controller).
- Franchise-oriented production package installation/upgrade.
- Device/telemetry ingestion, alerting, and maintenance-ticket operations.
- Edge/Cloud synchronization, dead-letter recovery, and realtime UI push.

### 2.3 User Classes and Characteristics

See `project_introduction.md` §6 for the full actor table (SystemAdmin, OrgAdmin, Manager, Staff, Technician, Customer, Tablet client, Local Edge Backend, PayOS). Internal roles are distinguished by scope breadth (platform-wide vs. one organization vs. operational) and by technical vs. business responsibility (Technician/SystemAdmin skew technical; Manager/Staff skew operational-business). Customers are anonymous, single-session actors interacting only through a scoped order-access token. Evidence: `repo_truth_map.md` §3.

### 2.4 Operating Environment

- **Runtime**: ASP.NET Core Web API (WebAPI project), containerized (`docker/Dockerfile`, `docker/docker-compose.yml`).
- **Database**: PostgreSQL 17 via Npgsql/EF Core, database name `IceBotDB`, per the current `docker/docker-compose.yml` configuration. `[Assumption]` This is the current deployment baseline observed in configuration, not necessarily a binding "shall use PostgreSQL 17" product requirement independent of environment. Evidence: `database_inventory.md` §7.
- **Object storage**: MinIO (S3-compatible), sibling container, for robot artifact binaries. Evidence: `database_inventory.md` §7.
- **Messaging**: MQTT broker (Mosquitto with Dynamic Security plugin) for Edge uplink/wake-up traffic and per-endpoint credential provisioning. Evidence: `functional_inventory.md` MQTT section.
- **External network dependencies**: PayOS (payment), Firebase (Google login verification, and implicitly FCM for push). Evidence: `project_introduction.md` §11.

### 2.5 Design and Implementation Constraints

- Strict compile-time dependency direction `WebAPI → Infrastructure → Application → Domain`; `Domain` has no outward dependencies. Evidence: `repo_truth_map.md` §2.
- Every FK's `DeleteBehavior` is force-set to `Restrict` by a global convention after per-entity configuration runs, except a small set of explicitly-configured `Cascade` parent-owns-child relationships (with one open question over whether the global loop silently reverts those — see §8). Evidence: `database_inventory.md` §3, §9 item 6.
- `GuidEntity.Id` is application-generated (`ValueGeneratedNever()`); `LongEntity.Id` is DB-generated. Evidence: `database_inventory.md` §4.
- Soft-delete is a global EF query filter (`DeletedAt IS NULL`) except for 12 principal types with required non-deleted dependents (`Account`, `Organization`, `Store`, `Kiosk`, `Device`, `Product`, `Ingredient`, `IngredientDispenserState`, `Order`, `PaymentTransaction`, `ConfigurationRelease`, `KioskExecutionEndpoint`), which require an explicit `WhereNotDeleted()` call. Evidence: `database_inventory.md` §7.
- No PostgreSQL native table partitioning exists yet despite being planned in `docs/data/DATA_MODELING_RULES.md`. Evidence: `database_inventory.md` §4, §7.
- MQTT is explicitly a notification/best-effort channel only, never the durable source of truth for execution state; Edge must still pull/ack over REST (or the equivalent MQTT uplink handler) for the durable record. Evidence: `repo_truth_map.md` §8.
- Two different configuration keys are used for the DB connection string across runtime vs. design-time paths (`CONNECTIONSTRING` vs. `ConnectionStrings:IceBot_DB`), a real operational divergence to be aware of. Evidence: `database_inventory.md` §9 item 10.

### 2.6 Assumptions and Dependencies

Carried from `project_introduction.md` §12; full list also repeated in §8 of this document. Key dependencies: PayOS availability for payment sessions/webhooks; Firebase availability for Google login; MinIO availability for robot artifact storage/retrieval; MQTT broker availability for Edge wake-up and uplink traffic (with REST as the durable fallback path).

---

## 3. External Interface Requirements

### 3.1 User Interfaces

This repository is a backend; it does not implement or ship any UI. Per `project_introduction.md`, no frontend/tablet/mobile implementation is asserted beyond what the API contract implies. The backend exposes:
- A public **tablet/customer** API surface (`/api/v1/kiosks/...`, `/api/v1/orders...`) consumed by an unspecified tablet client. `[Inferred]` — the existence and shape of a tablet UI is implied by the API surface and by `docs/iot/IOT_CONTRACT.md`'s "Tablet" actor, but no tablet frontend code is in this repository.
- A **management** API surface (REST + GraphQL + SignalR) consumed by an unspecified internal management UI. `[Inferred]` for the same reason.

Evidence: `repo_truth_map.md` §6; `functional_inventory.md` (all management-prefixed routes).

### 3.2 Software Interfaces

| Interface | Direction | Protocol/Format | Evidence |
|---|---|---|---|
| PayOS payment provider | Outbound (session create) + Inbound (webhook) | HTTPS REST, JSON, signature-verified webhook | `functional_inventory.md` PAY-01, PAY-03 |
| Firebase (Google Identity) | Outbound (token verification) | HTTPS, ID token verification | `functional_inventory.md` IDN-02 |
| MinIO object storage | Outbound (put/get/presigned URL) | S3-compatible API | `database_inventory.md` §7; `functional_inventory.md` RC-05 |
| MQTT broker (Mosquitto Dynamic Security) | Outbound (publish wake-up, credential provisioning) + Inbound (uplink subscribe) | Topic-based pub/sub, QoS1 for the wake-up publish (`functional_inventory.md` MQTT-01); protocol version not established in the evidence — `[Open Question]` | `functional_inventory.md` MQTT-01, MQTT-02, MQTT-03, MQTT-04 |
| PostgreSQL | Outbound | Npgsql/EF Core | `database_inventory.md` §7 |
| Push notification channel (FCM-style) | Outbound | Device push token delivery | `functional_inventory.md` IDN-12, OPS-07, OPS-21 |

### 3.3 Hardware / IoT / Robot Interfaces

- **Robot arm / Fairino runtime**: this backend does not control the robot arm directly. It authors and distributes robot programs as exported Lua artifacts (`.lua` files, produced by Fairino Studio tooling) plus `.icebot.json` technical-contract sidecars, and receives execution evidence back from the Edge runtime that actually drives the robot. Evidence: `functional_inventory.md` Robot Configuration section, RC-02/RC-08.
- **Kiosk devices** (dispensers, sensors): represented in the Devices bounded context (`DeviceType`, `DeviceModel`, `Device`) with capability declarations (e.g. `IngredientDispenser`); telemetry and events are ingested from Edge, not read directly from hardware by this backend. Evidence: `database_inventory.md` §2 Devices; `functional_inventory.md` DEV-18–DEV-21.
- **Execution Endpoints** (Full Edge / Low-Cost Controller): the addressable unit this backend dispatches commands to and receives reports from; authentication differs by profile (mTLS certificate fingerprint vs. ECDSA P-256 signed request). Evidence: `functional_inventory.md` DEV-10–DEV-17.

### 3.4 Communication Interfaces

| Surface | Route pattern | Auth | Evidence |
|---|---|---|---|
| Tablet/customer | `/api/v1/kiosks/...`, `/api/v1/orders...` | Public v1 + idempotency/validation, order-scoped bearer token | `repo_truth_map.md` §6 |
| Internal management | `/api/v1/management/...` | JWT + scoped RBAC policy | `repo_truth_map.md` §6 |
| Current account | `/api/v1/me...` | JWT | `repo_truth_map.md` §6 |
| Authentication | `/api/v1/authentication...` | Mixed public/login + token | `repo_truth_map.md` §6 |
| Payment provider webhook | `/api/v1/payments/.../webhook` | Provider signature verification | `repo_truth_map.md` §6 |
| IoT/edge (REST) | `/api/v1/iot/execution-endpoints/{endpointId}/...` | mTLS (Full Edge) or ECDSA P-256 signed request (Low-Cost Controller) | `repo_truth_map.md` §6 |
| IoT/edge (MQTT) | `icebot/execution-endpoints/{endpointId}/...` topics | MQTT broker credential (per-endpoint, Mosquitto Dynamic Security) | `functional_inventory.md` MQTT section |
| GraphQL | `/graphql` | JWT + scoped RBAC policy, management aggregation. Documented as read-only in `repo_truth_map.md` §6, but this was not independently re-verified against `src/WebAPI/GraphQL/` in the evidence pass — `[Unclear]`, see §8.2. | `repo_truth_map.md` §6 |
| SignalR | `/hubs/orders`, `/hubs/operations`, `/hubs/management-dashboard` | JWT + scoped group-join authorization | `repo_truth_map.md` §6; `functional_inventory.md` SignalR section |
| Health/info probes | `/health...`, `/info` | Public probe | `repo_truth_map.md` §6 |

---

## 4. System Features and Functional Requirements

Requirements below are grouped by bounded context, matching `functional_inventory.md`'s organization, and numbered sequentially FR-001…. Each FR's "Related" and "Evidence" fields map to the corresponding row ID(s) (e.g. `IDN-01`) in `functional_inventory.md` for full endpoint/handler/line-number traceability; this SRS restates them at requirement level rather than duplicating every file/line citation. Where several closely related inventory rows form one coherent requirement (e.g. create+list+update of the same resource), they are consolidated into a single FR with sub-flows, as noted.

### 4.1 Identity

#### FR-001 — Local Login
- **Description**: The system shall authenticate an Active account via email/username and password.
- **Actor**: Anonymous.
- **Trigger**: `POST /api/v1/authentication/login`.
- **Preconditions**: Account exists, is Active, and has local login enabled.
- **Main Flow**: 1) Validate credentials against stored hash. 2) On success, issue access/refresh token pair and account summary.
- **Alternative/Exception Flow**: On 5 cumulative failed attempts, lock the account for 15 minutes.
- **Related**: `IDN-01`; `AuthenticationController.Login`, `AccountAuthenticationService.LoginAsync`.
- **Evidence**: `functional_inventory.md` IDN-01.
- **Status**: Supported.

#### FR-002 — Google (Firebase) Login
- **Description**: The system shall authenticate via a verified Firebase Google ID token.
- **Actor**: Anonymous.
- **Trigger**: `POST /api/v1/authentication/google`.
- **Preconditions**: Verified token email matches the account's configured `GoogleEmail`.
- **Main Flow**: 1) Verify ID token via `IExternalIdentityProvider`. 2) Bind `GoogleSubjectId` on first login. 3) Issue token pair.
- **Alternative/Exception Flow**: Reject if a later login presents a mismatched subject identity for the same email.
- **Related**: `IDN-02`; `AccountAuthenticationService.LoginWithExternalProviderAsync`.
- **Evidence**: `functional_inventory.md` IDN-02.
- **Status**: Supported.

#### FR-003 — Refresh Access Token
- **Description**: The system shall reissue an access token from a valid, non-revoked refresh token.
- **Actor**: Authenticated (via refresh token).
- **Trigger**: `POST /api/v1/authentication/refresh`.
- **Preconditions**: Refresh token unexpired and unrevoked.
- **Main Flow**: 1) Validate refresh token. 2) Re-check persisted account status. 3) Issue new token pair.
- **Alternative/Exception Flow**: If the account is no longer Active, revoke remaining sessions instead of issuing tokens.
- **Related**: `IDN-03`.
- **Evidence**: `functional_inventory.md` IDN-03.
- **Status**: Supported.

#### FR-004 — Revoke Refresh Token (Logout) / Revoke All Sessions
- **Description**: The system shall let a token holder revoke one specific refresh token, and let an authenticated account revoke all of its own active sessions.
- **Actor**: Anonymous (token holder) / logged-in account.
- **Trigger**: `POST /api/v1/authentication/revoke`; `POST /api/v1/authentication/revoke-all`.
- **Preconditions**: Token exists (single-revoke) or caller is authenticated (revoke-all).
- **Main Flow**: 1) Locate refresh token(s). 2) Mark revoked with reason.
- **Alternative/Exception Flow**: Single revoke returns 404 if the token is not found.
- **Related**: `IDN-04`, `IDN-05`.
- **Evidence**: `functional_inventory.md` IDN-04, IDN-05.
- **Status**: Supported.

#### FR-005 — Forgot Password / Reset Password
- **Description**: The system shall issue a time-limited password-reset token by email and allow the holder to set a new password.
- **Actor**: Anonymous.
- **Trigger**: `POST /api/v1/authentication/forgot-password`; `POST /api/v1/authentication/reset-password`.
- **Preconditions**: Account is Active with local login enabled (forgot-password); reset token valid, unused, unexpired (reset-password).
- **Main Flow**: 1) Issue 30-minute reset token, email it. 2) On reset, verify token, set new password. 3) Revoke all existing refresh sessions.
- **Alternative/Exception Flow**: Forgot-password always returns a generic success message regardless of account existence, to prevent enumeration.
- **Related**: `IDN-06`, `IDN-07`.
- **Evidence**: `functional_inventory.md` IDN-06, IDN-07.
- **Status**: Supported.

#### FR-006 — Change Own Password
- **Description**: The system shall let a logged-in account change its own password after verifying the current password.
- **Actor**: Logged-in account.
- **Trigger**: `PUT /api/v1/me/password`.
- **Preconditions**: Current password matches stored hash.
- **Main Flow**: 1) Verify current password. 2) Set new password. 3) Revoke all refresh sessions.
- **Alternative/Exception Flow**: Reject if current password is incorrect.
- **Related**: `IDN-08`.
- **Evidence**: `functional_inventory.md` IDN-08.
- **Status**: Supported.

#### FR-007 — View / Update Own Profile and Effective Access
- **Description**: The system shall let a logged-in account view and update its own profile fields, and inspect its own token-embedded roles/scope.
- **Actor**: Logged-in account.
- **Trigger**: `GET /api/v1/me`; `PUT /api/v1/me/profile`; `GET /api/v1/me/access`.
- **Preconditions**: Valid JWT.
- **Main Flow**: 1) Return/update profile fields (`FullName`, `PhoneNumber`, `Address`, `Gender`, `ImageUrl`). 2) `/me/access` returns roles/scope from token claims without DB recomputation.
- **Alternative/Exception Flow**: None material.
- **Related**: `IDN-09`, `IDN-10`, `IDN-11`.
- **Evidence**: `functional_inventory.md` IDN-09, IDN-10, IDN-11.
- **Status**: Supported.

#### FR-008 — Manage Own Push-Notification Device Registrations
- **Description**: The system shall let a logged-in account register/refresh, list, and unregister its own push-notification (FCM-style) device installations.
- **Actor**: Logged-in account.
- **Trigger**: `PUT /api/v1/me/notification-devices/{installationId}`; `GET /api/v1/me/notification-devices`; `DELETE /api/v1/me/notification-devices/{installationId}`.
- **Preconditions**: Valid JWT.
- **Main Flow**: 1) Register/refresh installation. 2) Invalidate any other active registration already owning the same push-token hash. 3) List/unregister on request.
- **Alternative/Exception Flow**: None material.
- **Related**: `IDN-12`, `IDN-13`, `IDN-14`.
- **Evidence**: `functional_inventory.md` IDN-12, IDN-13, IDN-14.
- **Status**: Supported.

#### FR-009 — Invitation-Based Internal Account Onboarding
- **Description**: The system shall let a SystemAdmin create an internal account as `Invited` by default with a single-use invitation link, optionally emailed, or (a narrower variant) set an admin-assigned initial password without an invitation.
- **Actor**: SystemAdmin.
- **Trigger**: `POST /api/v1/management/accounts`.
- **Preconditions**: Requested role/scope assignment is valid for the caller.
- **Main Flow**: 1) Create account `Invited`. 2) Generate single-use invitation link. 3) Optionally send invitation email.
- **Alternative/Exception Flow**: With `CreateInvitation=false` + `InitialPassword`, the account is created with a password set directly and no invitation — this variant's surrounding lifecycle (forced password change, restricted first login) is stated in `docs/api/IDENTITY_ONBOARDING_RULES.md` as not part of the current contract.
- **Related**: `IDN-15`, `IDN-15b`.
- **Evidence**: `functional_inventory.md` IDN-15, IDN-15b.
- **Status**: Supported (main path); Partial (temporary-password variant, IDN-15b).

#### FR-010 — Accept Invitation / Regenerate Invitation
- **Description**: The system shall activate an `Invited` account from a valid invitation token, and let a SystemAdmin regenerate an invitation.
- **Actor**: Anonymous (invitation-token holder) / SystemAdmin.
- **Trigger**: `POST /api/v1/authentication/accept-invitation`; `POST /api/v1/management/accounts/{accountId}/invitation`.
- **Preconditions**: Invitation token valid, unexpired, unrevoked, unaccepted.
- **Main Flow**: 1) Validate token. 2) Set password only if local login is enabled. 3) Mark `EmailConfirmed` only if backend-emailed. 4) Revoke prior sessions. Regeneration revokes any previously active invitation and requires `Invited` status.
- **Alternative/Exception Flow**: At most one active invitation per account is allowed.
- **Related**: `IDN-16`, `IDN-17`.
- **Evidence**: `functional_inventory.md` IDN-16, IDN-17.
- **Status**: Supported.

#### FR-011 — List / View / Update / Disable Internal Accounts
- **Description**: The system shall let authorized roles list, view, update, and disable internal accounts scoped to their organization/store/kiosk role assignment.
- **Actor**: SystemAdmin / OrgAdmin / Manager.
- **Trigger**: `GET /api/v1/management/accounts[/{id}]`; `PUT /api/v1/management/accounts/{accountId}`; `PATCH /api/v1/management/accounts/{accountId}/disable`.
- **Preconditions**: Caller has `accounts.read`/`accounts.manage` policy and target account is within scope (non-SystemAdmin callers).
- **Main Flow**: 1) List/view filtered by search/status and scope. 2) Update profile/auth-method toggles (requires an existing password before enabling local login; clears `GoogleSubjectId` when `GoogleEmail` changes). 3) Disable sets `Disabled` and revokes sessions.
- **Alternative/Exception Flow**: None material.
- **Related**: `IDN-18`, `IDN-19`, `IDN-20`, `IDN-21`.
- **Evidence**: `functional_inventory.md` IDN-18, IDN-19, IDN-20, IDN-21.
- **Status**: Supported.

#### FR-012 — Admin Set/Reset Account Password
- **Description**: The system shall let a SystemAdmin set an internal account's password directly.
- **Actor**: SystemAdmin.
- **Trigger**: `PUT /api/v1/management/accounts/{accountId}/password`.
- **Preconditions**: Caller has `accounts.manage` policy.
- **Main Flow**: 1) Set new password (credential material only, not an auth-method toggle). 2) Revoke that account's refresh sessions.
- **Alternative/Exception Flow**: None material.
- **Related**: `IDN-22`.
- **Evidence**: `functional_inventory.md` IDN-22.
- **Status**: Supported.

#### FR-013 — Assign / Replace Account Role Assignments
- **Description**: The system shall let authorized roles assign one role+scope to an account, or atomically replace its full active role-assignment set.
- **Actor**: SystemAdmin / OrgAdmin / Manager (per role hierarchy).
- **Trigger**: `POST /api/v1/management/accounts/{accountId}/roles`; `PUT /api/v1/management/accounts/{accountId}/roles`.
- **Preconditions**: Caller's own role can assign the target role (SystemAdmin > OrgAdmin > Manager hierarchy); requested scope is valid for that role and within the assigner's own scope.
- **Main Flow**: 1) Validate role-assignment permission and scope. 2) Assign or atomically replace role set, rejecting duplicate role/scope entries.
- **Alternative/Exception Flow**: Reject if the caller cannot assign the target role or the scope is invalid.
- **Related**: `IDN-23`, `IDN-24`.
- **Evidence**: `functional_inventory.md` IDN-23, IDN-24.
- **Status**: Supported.

#### FR-014 — View Account Effective Access
- **Description**: The system shall return a target account's active role scopes to a caller sharing an active scope with that account.
- **Actor**: SystemAdmin / OrgAdmin / Manager.
- **Trigger**: `GET /api/v1/management/accounts/{accountId}/effective-access`.
- **Preconditions**: Caller shares an active scope with the target account.
- **Main Flow**: 1) Resolve target account's active role scopes and effective org/store/kiosk ids.
- **Alternative/Exception Flow**: Reject if the caller has no shared scope with the target.
- **Related**: `IDN-25`.
- **Evidence**: `functional_inventory.md` IDN-25.
- **Status**: Supported.

#### FR-015 — List Assignable Roles / View Permission Matrix
- **Description**: The system shall list roles the caller is permitted to assign, and expose a static read-only policy→allowed-roles matrix.
- **Actor**: SystemAdmin / OrgAdmin / Manager.
- **Trigger**: `GET /api/v1/management/roles`; `GET /api/v1/management/permission-matrix`.
- **Preconditions**: Caller has `roles.view` policy.
- **Main Flow**: 1) Filter active roles by role hierarchy. 2) Return hardcoded policy/description/allowed-roles matrix for UI display.
- **Alternative/Exception Flow**: None material.
- **Related**: `IDN-26`, `IDN-27`.
- **Evidence**: `functional_inventory.md` IDN-26, IDN-27.
- **Status**: Supported.

#### FR-016 — Enforce Scoped RBAC on Management Endpoints
- **Description**: The system shall enforce scoped RBAC (role + matching organization/store/kiosk scope from the same `AccountRole`) on management endpoints decorated with an authorization policy.
- **Actor**: System (cross-cutting).
- **Trigger**: Any request to a `[Authorize(Policy=...)]`-decorated endpoint.
- **Preconditions**: Caller presents a JWT with role + `role_scope` claims.
- **Main Flow**: 1) Match required policy against caller's roles/scope. 2) Allow if a matching `AccountRole` exists.
- **Alternative/Exception Flow**: Return 401 (unauthenticated) or 403 (authenticated but out-of-scope/role); reject cross-scope composition (e.g., a role valid for one org combined with a different org's resource).
- **Related**: `IDN-28`.
- **Evidence**: `functional_inventory.md` IDN-28.
- **Status**: Supported for the endpoints cited throughout §4 (each was read against its declared policy). `[Open Question]` Whether this holds for *every* management REST action, GraphQL resolver, and SignalR hub method — including any not individually cited in `functional_inventory.md` — was not established by an exhaustive authorization-coverage audit; treat the universal form of this requirement as unverified until such an audit is performed.

### 4.2 Tenants

#### FR-017 — Organization Lifecycle Management
- **Description**: The system shall let SystemAdmin create organizations and let SystemAdmin/OrgAdmin view/update/activate/disable them per role scope.
- **Actor**: SystemAdmin / OrgAdmin.
- **Trigger**: `POST/GET/PUT/PATCH /api/v1/management/organizations[/{id}]`.
- **Preconditions**: Unique uppercase `Code` (create); organization not soft-deleted (update).
- **Main Flow**: 1) Create with unique code. 2) SystemAdmin views/updates all fields; OrgAdmin views/updates only Name/Email/Phone/Address for their assigned organization. 3) SystemAdmin-only activate/disable.
- **Alternative/Exception Flow**: Reject updates to a soft-deleted organization.
- **Related**: `TEN-01`, `TEN-02`, `TEN-03`, `TEN-04`.
- **Evidence**: `functional_inventory.md` TEN-01, TEN-02, TEN-03, TEN-04.
- **Status**: Supported.

#### FR-018 — Store Lifecycle and Sales-Pause Management
- **Description**: The system shall let authorized roles create, view, update, activate/disable, pause, and resume stores under an organization.
- **Actor**: SystemAdmin / OrgAdmin / Manager.
- **Trigger**: `POST/GET/PUT/PATCH /api/v1/management/(organizations/{organizationId}/)stores[/{storeId}][/activate|disable|sales-pause|sales-resume]`.
- **Preconditions**: Parent organization active (create, activate); store active (pause).
- **Main Flow**: 1) Create store with unique code within organization, validated opening-hours/time zone. 2) Update details (`Code` immutable; time-zone change requires sales-paused state first). 3) Pause requires a reason and optional auto-resume time, without cancelling existing orders; resume clears pause state immediately.
- **Alternative/Exception Flow**: Disabling a store does not cascade-disable its kiosks.
- **Related**: `TEN-05`, `TEN-06`, `TEN-07`, `TEN-08`, `TEN-09`, `TEN-10`.
- **Evidence**: `functional_inventory.md` TEN-05, TEN-06, TEN-07, TEN-08, TEN-09, TEN-10.
- **Status**: Supported.

#### FR-019 — Kiosk Lifecycle, Details, and Operational State
- **Description**: The system shall let authorized roles create, view, update, and independently manage a kiosk's lifecycle status and operational (sales-admission) state.
- **Actor**: SystemAdmin / OrgAdmin / Manager / Technician.
- **Trigger**: `POST/GET/PUT/PATCH /api/v1/management/(stores/{storeId}/)kiosks[/{kioskId}][/status|operational-state]`.
- **Preconditions**: Parent store/organization active (create, or set `Active` lifecycle status).
- **Main Flow**: 1) Create in `Provisioning` status, code unique across organization, inheriting `OrganizationId`. 2) Update details keeping `Code`/`StoreId`/`OrganizationId` immutable. 3) Change lifecycle status (`Provisioning`/`Active`/`Disabled`/`Retired`), publishing `KioskStatusChanged`. 4) Change operational state (`Operational`/`PausedByOperator`/`Maintenance`/`Cleaning`/`Restocking`/`EmergencyStopRequested`/`OutOfService`) independently, with a required audit reason.
- **Alternative/Exception Flow**: Reject `Maintenance`/`Cleaning`/`Restocking` while an execution is running.
- **Related**: `TEN-11`, `TEN-12`, `TEN-13`, `TEN-14`, `TEN-15`.
- **Evidence**: `functional_inventory.md` TEN-11, TEN-12, TEN-13, TEN-14, TEN-15.
- **Status**: Supported.

#### FR-020 — Franchise Onboarding Workflow
- **Description**: The system shall run an idempotent, checkpointed workflow that provisions a Store, then a Kiosk, then optionally installs a production package, and shall support listing, resuming, and cancelling it.
- **Actor**: OrgAdmin / SystemAdmin.
- **Trigger**: `POST .../franchise-onboardings`; `POST .../{onboardingId}/resume`; `GET .../franchise-onboardings[/{id}]`; `POST .../{onboardingId}/cancel`.
- **Preconditions**: `Idempotency-Key` header supplied for start; only Pending/Failed onboardings may be cancelled.
- **Main Flow**: 1) Start with idempotency key, store/kiosk requests, optional package selection. 2) Progress through checkpoints, stopping deliberately at `ReadyForActivation` without auto-activating. 3) Resume from last completed checkpoint using a claim/lease to prevent concurrent runs, without recreating already-provisioned resources.
- **Alternative/Exception Flow**: Cancel requires a reason and does not delete already-provisioned resources; Running/ReadyForActivation onboardings cannot be cancelled.
- **Related**: `TEN-16`, `TEN-17`, `TEN-18`, `TEN-19`.
- **Evidence**: `functional_inventory.md` TEN-16, TEN-17, TEN-18, TEN-19.
- **Status**: Supported.

#### FR-021 — Role Scope Options Lookup and Tenant Tree Navigation
- **Description**: The system shall return valid organization/store/kiosk scope choices for a target role, and expose the management-visible tenant hierarchy via GraphQL only.
- **Actor**: SystemAdmin / OrgAdmin / Manager / Technician.
- **Trigger**: `GET /api/v1/management/role-scope-options?roleCode=`; GraphQL `tenantTree`.
- **Preconditions**: Caller can assign the target role (scope lookup); caller has `tenant-tree.view` policy (tree).
- **Main Flow**: 1) Filter allowed org/store/kiosk scope to the caller's own allowed scope. 2) Return nested org/store/kiosk hierarchy for RBAC scope selection and navigation.
- **Alternative/Exception Flow**: The REST route for tenant tree was intentionally removed in favor of GraphQL-only.
- **Related**: `TEN-20`, `TEN-21`.
- **Evidence**: `functional_inventory.md` TEN-20, TEN-21.
- **Status**: Supported.

### 4.3 Devices

#### FR-022 — Device Type / Model Catalog Authoring and Read
- **Description**: The system shall let SystemAdmin author a global, tenant-independent device-type and device-model catalog, readable by any device-management role.
- **Actor**: SystemAdmin (author) / all device-management roles (read).
- **Trigger**: `POST/PUT/PATCH /api/v1/management/device-types[/{id}]`; `POST/PUT/DELETE .../models[/{id}]`; `GET` variants.
- **Preconditions**: Immutable, unique type/model code; models only under an active DeviceType.
- **Main Flow**: 1) Author type with capability/active flag. 2) Author model with capability list. 3) Any authenticated device-management user reads without tenant scope.
- **Alternative/Exception Flow**: Block retiring a model still assigned to any non-retired device.
- **Related**: `DEV-01`, `DEV-02`, `DEV-03`.
- **Evidence**: `functional_inventory.md` DEV-01, DEV-02, DEV-03.
- **Status**: Supported.

#### FR-023 — Device Registration, Update, Status, and Retirement
- **Description**: The system shall let authorized roles register a physical device under a kiosk, view/update it, change its operational status, and retire it.
- **Actor**: SystemAdmin / OrgAdmin / Manager / Staff / Technician.
- **Trigger**: `POST/GET/PUT/PATCH/DELETE /api/v1/management/kiosks/{kioskId}/devices[/{deviceId}][/status]`.
- **Preconditions**: Active, compatible DeviceType/DeviceModel; kiosk-unique code and globally unique serial number.
- **Main Flow**: 1) Register in `Provisioning` status. 2) List/view scoped to caller's assignment. 3) Update details re-validating type/model/serial. 4) Change status among non-terminal states. 5) Retire (soft-delete), atomically retiring active dispenser topology states.
- **Alternative/Exception Flow**: `Retired` status must use the dedicated retire endpoint, not the status-change endpoint; retire is blocked while the owning kiosk has an Accepted/Running execution.
- **Related**: `DEV-04`, `DEV-05`, `DEV-06`, `DEV-07`, `DEV-08`.
- **Evidence**: `functional_inventory.md` DEV-04, DEV-05, DEV-06, DEV-07, DEV-08.
- **Status**: Supported.

#### FR-024 — Device Replacement (Hardware Swap)
- **Description**: The system shall transfer active container/ingredient mappings and positive estimates from a source device to an already-provisioned replacement device in the same kiosk, then retire the source.
- **Actor**: SystemAdmin / OrgAdmin / Manager / Technician.
- **Trigger**: `POST /api/v1/management/kiosks/{kioskId}/devices/{deviceId}/replace`.
- **Preconditions**: Caller holds both `devices.manage` and `inventory.configure`; replacement device already provisioned in the same kiosk.
- **Main Flow**: 1) Transfer container/ingredient mappings and estimates with balanced stock movements and rebind audit records. 2) Retire source device. All in one transaction.
- **Alternative/Exception Flow**: None material beyond the transactional guarantee.
- **Related**: `DEV-09`.
- **Evidence**: `functional_inventory.md` DEV-09.
- **Status**: Supported.

#### FR-025 — Execution Endpoint Provisioning and Lifecycle
- **Description**: The system shall let authorized roles create an execution endpoint, configure its supported robot targets, provision/activate its transport credential, manage its lifecycle (disable/reactivate/retire), and rotate its credential.
- **Actor**: SystemAdmin / OrgAdmin / Manager / Technician.
- **Trigger**: `POST/PUT/PATCH .../kiosks/{kioskId}/execution-endpoints[/{endpointId}][/supported-robot-targets|provision|disable|reactivate|retire|credential]`.
- **Preconditions**: Kiosk-unique endpoint code; endpoint Provisioning/Disabled for target replacement; at least one supported robot target and a unique profile identity before activation.
- **Main Flow**: 1) Create in `Provisioning`, binding Full Edge to mTLS or Low-Cost Controller to signed-command auth. 2) Replace supported robot target set. 3) Provision credential (fingerprint or ECDSA public key) and activate. 4) Manage Active↔Disabled↔Retired lifecycle. 5) Rotate credential, revoking the previous binding.
- **Alternative/Exception Flow**: Retirement requires the MQTT credential to already be revoked; activation is blocked without a valid credential/profile identity.
- **Related**: `DEV-10`, `DEV-11`, `DEV-12`, `DEV-13`, `DEV-14`, `DEV-17`.
- **Evidence**: `functional_inventory.md` DEV-10, DEV-11, DEV-12, DEV-13, DEV-14, DEV-17.
- **Status**: Supported.

#### FR-026 — MQTT Subscriber Credential Lifecycle and Reconciliation
- **Description**: The system shall manage a separate endpoint-scoped MQTT credential lifecycle (provision/rotate/revoke) via broker provisioning calls, and periodically reclaim stale pending operations.
- **Actor**: SystemAdmin / OrgAdmin / Manager / Technician (credential ops); System (reconciliation job).
- **Trigger**: `POST/PATCH/DELETE .../execution-endpoints/{endpointId}/mqtt-credential`; `MqttCredentialReconciliationJob` (periodic).
- **Preconditions**: Endpoint exists.
- **Main Flow**: 1) Provision/rotate returns a one-time generated password plus username/topics. 2) Revoke confirms removal. 3) Job reclaims operations left pending past a lease, marking failed provision/rotation for manual retry and completing/retrying interrupted revocations.
- **Alternative/Exception Flow**: The generated password is returned only once and not persisted.
- **Related**: `DEV-15`, `DEV-16`.
- **Evidence**: `functional_inventory.md` DEV-15, DEV-16.
- **Status**: Supported.

#### FR-027 — Kiosk Heartbeat Ingestion
- **Description**: The system shall ingest a kiosk heartbeat, deduplicated by `(kioskId, originNodeId, heartbeatSequence)`, advancing connectivity only for the newest sequence.
- **Actor**: Edge runtime / execution endpoint.
- **Trigger**: `POST /api/v1/iot/execution-endpoints/{endpointId}/heartbeat` (also reachable via MQTT, see MQTT-02).
- **Preconditions**: Authenticated execution endpoint; origin node matches the endpoint's bound profile identity.
- **Main Flow**: 1) Validate origin node. 2) Deduplicate by sequence. 3) Advance `LastOnlineAt`/connectivity for the newest sequence and publish `KioskStatusChanged` on connectivity change.
- **Alternative/Exception Flow**: Stale/duplicate sequences are accepted but do not advance connectivity state.
- **Related**: `DEV-18`.
- **Evidence**: `functional_inventory.md` DEV-18.
- **Status**: Supported.

#### FR-028 — Device Event Ingestion and Automatic Alerting
- **Description**: The system shall ingest one Warning/Error/Critical device-event record, globally deduplicated by `eventId`, and atomically raise or update an Open Alert for current Error/Critical events.
- **Actor**: Edge runtime / execution endpoint.
- **Trigger**: `POST /api/v1/iot/execution-endpoints/{endpointId}/device-events` (also via MQTT).
- **Preconditions**: Authenticated execution endpoint.
- **Main Flow**: 1) Deduplicate by `eventId`. 2) Persist event. 3) Raise/correlate an Open Alert within the alert-automation age window, with critical push notification.
- **Alternative/Exception Flow**: Non-Error/Critical events are recorded but do not raise alerts.
- **Related**: `DEV-19`.
- **Evidence**: `functional_inventory.md` DEV-19.
- **Status**: Supported.

#### FR-029 — Batched Telemetry Replay
- **Description**: The system shall replay a batch of typed heartbeat/device-event/local-log items with item-level atomicity and idempotent status per item.
- **Actor**: Edge runtime / execution endpoint.
- **Trigger**: `POST /api/v1/iot/execution-endpoints/{endpointId}/telemetry-events` (also via MQTT).
- **Preconditions**: Authenticated execution endpoint.
- **Main Flow**: 1) Deduplicate each item by event id via a durable receipt store. 2) Delegate each item to its dedicated ingest handler. 3) Return per-item accepted/duplicate/rejected result.
- **Alternative/Exception Flow**: Partial success returns HTTP 207 with mixed per-item outcomes.
- **Related**: `DEV-20`.
- **Evidence**: `functional_inventory.md` DEV-20.
- **Status**: Supported.

#### FR-030 — Execution Readiness Snapshot Ingestion
- **Description**: The system shall apply a complete, monotonically-revisioned readiness/activity/safety/capability snapshot per execution endpoint.
- **Actor**: Edge runtime / execution endpoint.
- **Trigger**: `POST /api/v1/iot/execution-endpoints/{endpointId}/readiness` (also via MQTT).
- **Preconditions**: Reported `SourceExecutorId` matches the authenticated endpoint's bound profile identity.
- **Main Flow**: 1) Validate profile identity match. 2) Apply snapshot only if `StateRevision` is newer than stored. 3) Publish `ExecutionReadinessChanged`.
- **Alternative/Exception Flow**: Stale/duplicate revisions are ignored.
- **Related**: `DEV-21`.
- **Evidence**: `functional_inventory.md` DEV-21.
- **Status**: Supported.

#### FR-031 — Kiosk Connectivity Timeout Reconciliation
- **Description**: The system shall periodically mark an active kiosk's connectivity `Unreachable` once its last observed heartbeat exceeds a configured timeout.
- **Actor**: System (background job).
- **Trigger**: `KioskConnectivityReconciliationJob` (periodic).
- **Preconditions**: Kiosk previously marked online with a stale last-heartbeat timestamp.
- **Main Flow**: 1) Scan kiosks serialized per kiosk. 2) Mark `Unreachable` and publish `KioskStatusChanged` (connectivity only, not lifecycle/operational state).
- **Alternative/Exception Flow**: None material.
- **Related**: `DEV-22`.
- **Evidence**: `functional_inventory.md` DEV-22.
- **Status**: Supported.

#### FR-032 — Kiosk Status Overview and Curated Telemetry History
- **Description**: The system shall provide a tenant-scoped kiosk lifecycle/operational/connectivity overview, and curated, kiosk-scoped heartbeat and device-event history.
- **Actor**: SystemAdmin / OrgAdmin / Manager / Staff / Technician.
- **Trigger**: GraphQL `getKioskStatusOverview`; `GET /api/v1/management/kiosks/{kioskId}/heartbeats`; `GET .../device-events`.
- **Preconditions**: Caller holds `operations.view` policy.
- **Main Flow**: 1) Aggregate kiosk state for dashboards. 2) Return paged, curated heartbeat/event history without raw payload by default.
- **Alternative/Exception Flow**: None material.
- **Related**: `DEV-23`, `DEV-24`, `DEV-25`.
- **Evidence**: `functional_inventory.md` DEV-23, DEV-24, DEV-25.
- **Status**: Supported.

### 4.4 Catalog

#### FR-033 — Ingredient Master Data Authoring
- **Description**: The system shall let a catalog manager create, update, search/page, activate/deactivate, and guardedly delete ingredient master data.
- **Actor**: Catalog/Product manager.
- **Trigger**: `POST/PUT/GET/PATCH/DELETE /api/v1/management/ingredients[/{id}][/status]`.
- **Preconditions**: Unique code (create).
- **Main Flow**: 1) Create/update with unique code. 2) Toggle `IsActive` idempotently. 3) Delete only if unreferenced by any recipe or inventory data.
- **Alternative/Exception Flow**: Reject deletion (409) if referenced.
- **Related**: `CAT-01`, `CAT-02`, `CAT-03`, `CAT-04`.
- **Evidence**: `functional_inventory.md` CAT-01, CAT-02, CAT-03, CAT-04.
- **Status**: Supported.

#### FR-034 — Product Category Authoring and Lifecycle
- **Description**: The system shall let a manager create, edit, activate/deactivate, and delete product categories with unique codes.
- **Actor**: Catalog/Product manager.
- **Trigger**: `GET/POST/PUT/PATCH/DELETE /api/v1/management/product-categories[/{id}][/status]`.
- **Preconditions**: Unique code.
- **Main Flow**: 1) CRUD + status toggle per standard lifecycle.
- **Alternative/Exception Flow**: None material beyond code uniqueness.
- **Related**: `CAT-05`.
- **Evidence**: `functional_inventory.md` CAT-05.
- **Status**: Supported.

#### FR-035 — Global Product Template Authoring
- **Description**: The system shall maintain organization-agnostic global product templates that tenant products can be cloned from.
- **Actor**: SystemAdmin / template author.
- **Trigger**: `GET/POST/PUT/PATCH/DELETE /api/v1/management/product-templates[/{id}]`.
- **Preconditions**: `StoreId`/`KioskId` forced null; `ScopeType=Global`.
- **Main Flow**: 1) Author template with same field set as a tenant product but global scope.
- **Alternative/Exception Flow**: None material.
- **Related**: `CAT-06`.
- **Evidence**: `functional_inventory.md` CAT-06.
- **Status**: Supported.

#### FR-036 — Tenant-Scoped Product Authoring, Availability, and Deletion
- **Description**: The system shall let a manager author products scoped to Organization/Store/Kiosk with validated tenant scope, toggle availability, and guardedly delete.
- **Actor**: OrgAdmin / Product manager.
- **Trigger**: `GET/POST/PUT/PATCH/DELETE /api/v1/management/organizations/{organizationId}/products[/{productId}][/availability]`.
- **Preconditions**: Tenant scope is global XOR org/store/kiosk-consistent.
- **Main Flow**: 1) Create/update with scope validation. 2) Toggle `IsAvailable` independent of lifecycle scope validation. 3) Delete only if not referenced by a non-deleted MenuItem, cascading soft-delete to variants under a mutation lock.
- **Alternative/Exception Flow**: Reject deletion (409) if referenced by a MenuItem.
- **Related**: `CAT-07`, `CAT-08`, `CAT-09`.
- **Evidence**: `functional_inventory.md` CAT-07, CAT-08, CAT-09.
- **Status**: Supported.

#### FR-037 — Clone Product From Global Template
- **Description**: The system shall let a manager materialize a tenant-scoped product (variants, option groups/options, ingredient requirements, latest published default-safe recipe per variant) from a global template in one operation.
- **Actor**: Product manager.
- **Trigger**: `POST /api/v1/management/organizations/{organizationId}/products/from-template`.
- **Preconditions**: Source template exists and is eligible for cloning.
- **Main Flow**: 1) Copy template structure into a new tenant-scoped product. 2) Default the clone to unavailable.
- **Alternative/Exception Flow**: None material.
- **Related**: `CAT-10`.
- **Evidence**: `functional_inventory.md` CAT-10.
- **Status**: Supported.

#### FR-038 — Product Variant Authoring and Lifecycle
- **Description**: The system shall let a manager add, edit, toggle availability of, and delete product variants under a definition-mutation ownership guard.
- **Actor**: Product manager.
- **Trigger**: `POST/PUT/PATCH/DELETE .../products/{productId}/variants[/{variantId}][/availability]`.
- **Preconditions**: Owning product exists and is mutable.
- **Main Flow**: 1) CRUD + availability toggle per variant.
- **Alternative/Exception Flow**: None material.
- **Related**: `CAT-11`.
- **Evidence**: `functional_inventory.md` CAT-11.
- **Status**: Supported.

#### FR-039 — Option Group Authoring and Lifecycle
- **Description**: The system shall let a manager define option groups per product with a selection cardinality (Single/Multiple, min/max) enforced at creation.
- **Actor**: Product manager.
- **Trigger**: `POST/PUT/PATCH/DELETE .../products/{productId}/option-groups[/{optionGroupId}]` (also under product-templates).
- **Preconditions**: Owning product/template exists.
- **Main Flow**: 1) Create/update group with `SelectionType`, `MinSelections`, `MaxSelections`.
- **Alternative/Exception Flow**: None material.
- **Related**: `CAT-12`.
- **Evidence**: `functional_inventory.md` CAT-12.
- **Status**: Supported.

#### FR-040 — Product Option Authoring, Lifecycle, and Ingredient Requirements
- **Description**: The system shall let a manager define selectable product options with a price delta and an execution-impact classification, and attach ingredient execution requirements for production-affecting options.
- **Actor**: Product manager.
- **Trigger**: `POST/PUT/PATCH/DELETE .../option-groups/{optionGroupId}/options[/{productOptionId}]`; `PUT .../options/{productOptionId}/ingredient-requirements`.
- **Preconditions**: Ingredient requirements restricted to `ProductionAffecting` options; each ingredient active with matching unit; no duplicate ingredients within one option.
- **Main Flow**: 1) CRUD + availability toggle per option. 2) Replace ingredient requirement list under validation.
- **Alternative/Exception Flow**: Reject inactive ingredients or unit mismatches (400/409).
- **Related**: `CAT-13`, `CAT-14`.
- **Evidence**: `functional_inventory.md` CAT-13, CAT-14.
- **Status**: Supported.

#### FR-041 — Recipe Authoring, Item Replacement, Status Lifecycle, and Versioning
- **Description**: The system shall let a manager author a Draft recipe per product variant, replace its ingredient list while Draft, enforce a strict Draft→Published→Active→Retired lifecycle, and create new Draft versions from an existing recipe.
- **Actor**: Product/Recipe manager.
- **Trigger**: `POST/PUT/GET .../recipes[/{recipeId}][/items|status|versions]` (also under product-templates).
- **Preconditions**: At most one active/non-retired default recipe per variant; ≥1 required ingredient to publish; item replacement only while Draft; versioning only from a Published/Active/Retired source.
- **Main Flow**: 1) Create Draft recipe. 2) Replace 1–100 validated recipe items. 3) Transition status per lifecycle rules. 4) Create a new Draft version copying items from a non-Draft source.
- **Alternative/Exception Flow**: Recipes are retired, not deleted — there is no dedicated delete endpoint.
- **Related**: `CAT-15`, `CAT-16`, `CAT-17`, `CAT-18`, `CAT-19`.
- **Evidence**: `functional_inventory.md` CAT-15, CAT-16, CAT-17, CAT-18, CAT-19.
- **Status**: Supported.

### 4.5 Sales Catalog

#### FR-042 — Menu Authoring, Status Lifecycle, and Deletion
- **Description**: The system shall let a sales manager author menus scoped to Organization/Store/Kiosk, transition menu status with an activation preflight, and soft-delete a menu with its items together.
- **Actor**: Sales manager.
- **Trigger**: `GET/POST/PUT/PATCH/DELETE .../menus[/{menuId}][/status]`.
- **Preconditions**: Validated tenant scope, code uniqueness, effective window.
- **Main Flow**: 1) Create/update with scope validation. 2) On activation, re-validate every currently Active MenuItem's authoring preflight (product/variant/recipe ownership, currency match, option satisfiability), rejecting (409) if any fails. 3) Soft-delete cascades to items.
- **Alternative/Exception Flow**: Reject activation if any active item fails preflight.
- **Related**: `SC-01`, `SC-02`, `SC-03`.
- **Evidence**: `functional_inventory.md` SC-01, SC-02, SC-03.
- **Status**: Supported.

#### FR-043 — Menu Item Authoring, Status Lifecycle, and Deletion
- **Description**: The system shall let a sales manager add/update menu items referencing an existing product variant and same-product options, transition item status with an activation preflight, and soft-delete items.
- **Actor**: Sales manager.
- **Trigger**: `POST/PUT/PATCH/DELETE .../menus/{menuId}/items[/{menuItemId}][/status]`.
- **Preconditions**: Referenced variant/options belong to the same product; distributed mutation lock on Menu+Product.
- **Main Flow**: 1) Add/update item under lock. 2) Activation preflight: product/variant existence and currency match, machine-produced variants require a Published/Active recipe with only active ingredients, option groups statically satisfiable. 3) Soft-delete preserving historical order references.
- **Alternative/Exception Flow**: Reject activation (409) on preflight failure.
- **Related**: `SC-04`, `SC-05`, `SC-06`.
- **Evidence**: `functional_inventory.md` SC-04, SC-05, SC-06.
- **Status**: Supported.

#### FR-044 — Menu / Menu Item List and Detail Read
- **Description**: The system shall allow searching and paging menus and menu items by store/kiosk scope.
- **Actor**: Sales manager.
- **Trigger**: `GET .../menus[/{menuId}]`.
- **Preconditions**: None beyond `menus.view`-equivalent access.
- **Main Flow**: 1) Return paged/filtered results.
- **Alternative/Exception Flow**: None material.
- **Related**: `SC-07`.
- **Evidence**: `functional_inventory.md` SC-07.
- **Status**: Supported.

#### FR-045 — Kiosk Runtime Menu Projection
- **Description**: The system shall produce a per-kiosk sellable runtime menu snapshot with a deterministic content revision usable as an HTTP ETag, gated on store opening hours and kiosk online-sales availability.
- **Actor**: Kiosk tablet / anonymous.
- **Trigger**: `GET /api/v1/kiosks/{kioskId}/runtime-menu`.
- **Preconditions**: Store within opening hours and kiosk online-sales-eligible.
- **Main Flow**: 1) Compute snapshot valid for 15 seconds. 2) Return with `SnapshotId`/`Revision`/ETag; support `If-None-Match` → 304.
- **Alternative/Exception Flow**: Return 409 if the store is closed or the kiosk is offline for sales.
- **Related**: `SC-08`.
- **Evidence**: `functional_inventory.md` SC-08.
- **Status**: Supported.

#### FR-046 — Menu Item Sellability and Option Selectability Evaluation
- **Description**: The system shall exclude non-sellable menu items from the runtime menu and validate/derive satisfiability of customer product-option selections.
- **Actor**: Kiosk tablet / Sales manager (via checkout and activation).
- **Trigger**: Internal to runtime-menu projection (FR-045) and checkout order placement.
- **Preconditions**: Menu Active and in effective window and tenant-scope-matched to kiosk.
- **Main Flow**: 1) Exclude items whose product/variant/recipe/route conditions are not met. 2) Validate selected options against group min/max cardinality and availability; separately determine whether a group's cardinality is currently satisfiable.
- **Alternative/Exception Flow**: Machine-produced variants additionally require an active recipe with only active ingredients and an active production route.
- **Related**: `SC-09`, `SC-10`.
- **Evidence**: `functional_inventory.md` SC-09, SC-10.
- **Status**: Supported.

#### FR-047 — Machine-Produced Option Filtering by Production Route
- **Description**: The system shall, for machine-produced menu items, expose only production-affecting options that the kiosk's active production route declares as supported, while always exposing commercial-only options for Packaged items and all options for Manual items.
- **Actor**: Kiosk tablet (via runtime-menu).
- **Trigger**: Internal to runtime-menu projection (FR-045).
- **Preconditions**: Kiosk has an active production route for the variant/recipe.
- **Main Flow**: 1) Filter `MenuItemProductOption` set by the active route's supported option codes.
- **Alternative/Exception Flow**: Inventory stock is explicitly not consulted for sellability — the system is reporting/operations-only for inventory in this respect.
- **Related**: `SC-11`.
- **Evidence**: `functional_inventory.md` SC-11.
- **Status**: Supported.

### 4.6 Inventory

#### FR-048 — Dispenser (Container) Provisioning and Configuration Update
- **Description**: The system shall provision a dispenser state binding a device+container to an active ingredient, and let a configurator update its capacity/unit/calibration profile subject to guards.
- **Actor**: Kiosk technician / Inventory configurator.
- **Trigger**: `POST/PUT /api/v1/management/kiosks/{kioskId}/inventory/dispenser-states[/{id}]`.
- **Preconditions**: Device declares `IngredientDispenser` capability and is not Retired; container code unique per device.
- **Main Flow**: 1) Provision with capacity/unit/calibration profile. 2) Update, auditing before/after capacity and unit.
- **Alternative/Exception Flow**: Reject a unit change once the dispenser has an estimated quantity or stock history (409) — requires retirement and a new state instead.
- **Related**: `INV-01`, `INV-02`.
- **Evidence**: `functional_inventory.md` INV-01, INV-02.
- **Status**: Supported.

#### FR-049 — Dispenser Retire/Reactivate and Guarded Deletion
- **Description**: The system shall let a configurator retire/reactivate a dispenser state (subject to device/ingredient/capability checks) and delete only unused states.
- **Actor**: Inventory configurator.
- **Trigger**: `PATCH .../dispenser-states/{id}/status`; `DELETE .../dispenser-states/{id}`.
- **Preconditions**: Reactivation blocked if device Retired, ingredient inactive, or device model lacks dispenser capability.
- **Main Flow**: 1) Toggle active status with audit. 2) Delete only if no stock-movement or rebind history exists.
- **Alternative/Exception Flow**: Reject deletion (409) if history exists.
- **Related**: `INV-03`, `INV-04`.
- **Evidence**: `functional_inventory.md` INV-03, INV-04.
- **Status**: Supported.

#### FR-050 — Device/Container Rebind (Hardware Replacement)
- **Description**: The system shall, on hardware/container replacement, retire the source dispenser state, create a new replacement, and require an explicit Discard/Transfer disposition of any positive source estimate.
- **Actor**: Inventory configurator.
- **Trigger**: `POST .../dispenser-states/{dispenserStateId}/rebind`.
- **Preconditions**: Transfer disposition permitted only for same ingredient+unit; kiosk has no accepted/running execution.
- **Main Flow**: 1) Retire source. 2) Create replacement. 3) Apply disposition and record audited rebind history.
- **Alternative/Exception Flow**: Block rebind while an execution is Accepted/Running.
- **Related**: `INV-05`.
- **Evidence**: `functional_inventory.md` INV-05.
- **Status**: Supported.

#### FR-051 — Dispenser Refill and Estimate Adjustment
- **Description**: The system shall let an operator increase a dispenser's estimated quantity by a refill amount or manually set an absolute estimate, recording a stock movement and a real-time inventory-changed notification each time.
- **Actor**: Kiosk technician / Manager.
- **Trigger**: `POST .../dispenser-states/{id}/refill`; `POST .../{id}/adjust-estimate`.
- **Preconditions**: Refill amount must not exceed capacity.
- **Main Flow**: 1) Refill increments estimate, records `REFILL` movement. 2) Adjust sets absolute estimate, records `ADJUST_ESTIMATE` movement capturing the delta.
- **Alternative/Exception Flow**: Reject refill exceeding capacity.
- **Related**: `INV-06`, `INV-07`.
- **Evidence**: `functional_inventory.md` INV-06, INV-07.
- **Status**: Supported.

#### FR-052 — Dispenser Consumption Recording (Execution-Driven)
- **Description**: The system shall decrement a dispenser's estimated quantity on production consumption, reject over-consumption, and reconcile against an optionally reported balance.
- **Actor**: Edge runtime / execution engine (via Orders/EdgeIntegration pipeline).
- **Trigger**: Domain method invocation (`Consume`/`ConsumeWithEvidence`) during order execution.
- **Preconditions**: Sufficient current estimate.
- **Main Flow**: 1) Decrement estimate. 2) Record stock movement. 3) Reconcile against reported balance if provided.
- **Alternative/Exception Flow**: Raise a domain error on balance mismatch; reject consumption exceeding the current estimate.
- **Related**: `INV-08`.
- **Evidence**: `functional_inventory.md` INV-08.
- **Status**: Supported (domain logic verified directly; the calling trigger lives in the Orders/EdgeIntegration pipeline).

#### FR-053 — Kiosk Inventory Topology, Rebind History, and Unified History Timeline
- **Description**: The system shall present, per kiosk, every dispenser-capable device with computed warnings, and shall expose per-dispenser rebind history and a merged reverse-chronological history timeline.
- **Actor**: Manager / Technician.
- **Trigger**: `GET .../inventory/topology`; `GET .../dispenser-states/{id}/rebind-history`; `GET .../dispenser-states/{id}/history`.
- **Preconditions**: Caller holds `inventory.view`.
- **Main Flow**: 1) Topology view flags inactive devices/containers/ingredients. 2) Rebind history returns full audited disposition/transfer detail. 3) Unified timeline merges stock movements, topology changes, and rebinds, resolving human actors where available.
- **Alternative/Exception Flow**: None material.
- **Related**: `INV-09`, `INV-10`, `INV-11`.
- **Evidence**: `functional_inventory.md` INV-09, INV-10, INV-11.
- **Status**: Supported.

#### FR-054 — Dispenser/Stock-Movement Listing and Inventory Summary Rollup
- **Description**: The system shall let a manager list dispenser states and stock movements filtered by tenant scope and activity, and view a scoped low-stock/empty rollup via GraphQL.
- **Actor**: Manager.
- **Trigger**: `GET /api/v1/management/inventory/dispenser-states`; `GET .../stock-movements`; GraphQL `getInventorySummary`.
- **Preconditions**: Caller holds `inventory.view`.
- **Main Flow**: 1) Return paged, filtered lists. 2) Return `TotalDispenserCount`/`LowStockCount`/`EmptyCount`/per-item detail.
- **Alternative/Exception Flow**: The summary rollup has no REST equivalent — GraphQL only.
- **Related**: `INV-12`, `INV-13`.
- **Evidence**: `functional_inventory.md` INV-12, INV-13.
- **Status**: Supported.

#### FR-055 — Inventory Readiness Evaluation
- **Description**: The system shall classify each required recipe ingredient's readiness (Ready, MissingIngredient, ContainerInactive, DeviceUnavailable, CalibrationMissing) and derive kiosk-level readiness from the worst-precedence blocking ingredient.
- **Actor**: System (deployment/production-route gating).
- **Trigger**: Internal service call (`IInventoryReadinessEvaluator.EvaluateKioskAsync`/`EvaluateOrganizationAsync`), consumed by Production Configuration deployment gating (see FR-XXX PC section).
- **Preconditions**: A kiosk or organization scope and route inputs (recipe, supported option codes) are supplied.
- **Main Flow**: 1) Evaluate each required ingredient. 2) Evaluate required option groups separately. 3) Return worst-precedence classification.
- **Alternative/Exception Flow**: None material.
- **Related**: `INV-14`.
- **Evidence**: `functional_inventory.md` INV-14.
- **Status**: Supported.

#### FR-056 — Dispenser Level-to-Quantity Calibration Profile Validation
- **Description**: The system shall, when a calibration profile is supplied, require exactly Low/Medium/Full points with strictly increasing quantities not exceeding capacity.
- **Actor**: Inventory configurator (via provisioning/update/rebind).
- **Trigger**: Embedded in create/update/rebind requests (FR-048, FR-050).
- **Preconditions**: Profile supplied.
- **Main Flow**: 1) Validate point set and ordering. 2) Serialize/store profile.
- **Alternative/Exception Flow**: Reject `Unknown` or duplicate levels.
- **Related**: `INV-15`.
- **Evidence**: `functional_inventory.md` INV-15.
- **Status**: Supported.

### 4.7 Orders

#### FR-057 — Place Order (Checkout)
- **Description**: The system shall let a customer place a kiosk order with idempotent, server-priced checkout.
- **Actor**: Customer (Tablet).
- **Trigger**: `POST /api/v1/orders` (+ `Idempotency-Key` header).
- **Preconditions**: Kiosk online for sales; items resolvable against the current runtime menu.
- **Main Flow**: 1) Re-price items server-side. 2) Create `Order`/`OrderItems` in `PendingPayment`. 3) Return `OrderAccessToken` and totals.
- **Alternative/Exception Flow**: Repeated requests with the same idempotency key return the original result rather than creating a duplicate order.
- **Related**: `ORD-01`.
- **Evidence**: `functional_inventory.md` ORD-01.
- **Status**: Supported.

#### FR-058 — Get Order Status and Cancel Pending Order (Customer)
- **Description**: The system shall let a customer poll their order's status via an access token, and cancel it only while Draft/PendingPayment and unpaid.
- **Actor**: Customer (Tablet).
- **Trigger**: `GET /api/v1/orders/{orderId}`; `POST /api/v1/orders/{orderId}/cancel`.
- **Preconditions**: Valid `Order-Access-Token`; order unpaid and non-terminal (cancel).
- **Main Flow**: 1) Return status incl. `CustomerStatus`/`CanRetryPayment`/`RequiresStaffSupport`. 2) Cancel transitions order to Cancelled.
- **Alternative/Exception Flow**: Reject cancellation once payment has progressed beyond PendingPayment.
- **Related**: `ORD-02`, `ORD-03`.
- **Evidence**: `functional_inventory.md` ORD-02, ORD-03.
- **Status**: Supported.

#### FR-059 — Cancel Order (Management) and Flag Refund-Required
- **Description**: The system shall let authorized staff cancel a non-paid, non-terminal order, or flag a paid, non-terminal order as requiring refund, both with an audited reason.
- **Actor**: Manager / OrgAdmin / Staff.
- **Trigger**: `PATCH /api/v1/management/orders/{orderId}/cancel`; `PATCH .../refund-required`.
- **Preconditions**: Reason required for refund-required flag.
- **Main Flow**: 1) Validate order state. 2) Apply transition with audit reason.
- **Alternative/Exception Flow**: Reject if the order is already terminal or (for cancel) already paid.
- **Related**: `ORD-04`, `ORD-05`.
- **Evidence**: `functional_inventory.md` ORD-04, ORD-05.
- **Status**: Supported.

#### FR-060 — Manual Redispatch of Order Execution
- **Description**: The system shall allow an authorized operator to redispatch a failed/rejected order's execution as a new attempt, up to a configured attempt limit.
- **Actor**: Manager / Technician.
- **Trigger**: `POST /api/v1/management/orders/{orderId}/execution-attempts`.
- **Preconditions**: Prior attempt ended in transport delivery failure or a pre-physical-output rejection; reason ≤500 chars.
- **Main Flow**: 1) Validate eligibility and attempt-limit. 2) Create a new dispatch attempt with audited actor/reason.
- **Alternative/Exception Flow**: Reject if the attempt limit is reached or the prior attempt is post-physical-output.
- **Related**: `ORD-06`.
- **Evidence**: `functional_inventory.md` ORD-06.
- **Status**: Supported.

#### FR-061 — Request Production Remake for Order Item
- **Description**: The system shall let staff request an idempotent, exact-unit remake for a failed/defective production output with confirmed no physical output, or an authorized defective-output incident.
- **Actor**: Manager / Technician.
- **Trigger**: `POST /api/v1/management/orders/{orderId}/items/{orderItemId}/production-remakes`.
- **Preconditions**: Confirmed no physical output, or an approved incident resolution requiring remake.
- **Main Flow**: 1) Validate precondition. 2) Produce a scoped, idempotent remake command.
- **Alternative/Exception Flow**: Reject if physical output cannot be ruled out without an incident authorization.
- **Related**: `ORD-07`.
- **Evidence**: `functional_inventory.md` ORD-07.
- **Status**: Supported.

#### FR-062 — Manual and Packaged Order-Item Fulfillment Events
- **Description**: The system shall let staff record idempotent lifecycle events for manually-fulfilled items, and mark packaged items as fulfilled or failed.
- **Actor**: Staff.
- **Trigger**: `POST .../items/{orderItemId}/manual-fulfillment-events`; `POST .../fulfill`; `POST .../fail`.
- **Preconditions**: `Fail` requires a reason.
- **Main Flow**: 1) Record event idempotently by `FulfillmentEventId`. 2) Aggregate order status.
- **Alternative/Exception Flow**: Duplicate event ids are no-ops (idempotent).
- **Related**: `ORD-08`, `ORD-09`, `ORD-10`.
- **Evidence**: `functional_inventory.md` ORD-08, ORD-09, ORD-10.
- **Status**: Supported.

#### FR-063 — Execution-Attempt Diagnostics
- **Description**: The system shall expose full command/delivery/production provenance for one dispatch attempt to diagnostics-scoped staff.
- **Actor**: Technician / Manager (`operations.diagnostics`).
- **Trigger**: `GET /api/v1/management/orders/{orderId}/execution-attempts/{sourceCommandId}/diagnostics`.
- **Preconditions**: Caller holds diagnostics policy.
- **Main Flow**: 1) Return delivery attempts, production executions, provenance, and adjacent attempts.
- **Alternative/Exception Flow**: None material.
- **Related**: `ORD-11`.
- **Evidence**: `functional_inventory.md` ORD-11.
- **Status**: Supported.

#### FR-064 — Management Order Reads (List, Detail, Overview, Queue, Histories)
- **Description**: The system shall let authorized staff search/filter/paginate orders, view detail, view a dashboard overview, view a fulfillment work queue, and view order/item status and execution-attempt history — all via GraphQL.
- **Actor**: Manager / Staff.
- **Trigger**: GraphQL `orders`, `order`, `orderOverview`, `fulfillmentQueue`, `orderStatusHistory`, `orderItemStatusHistory`, `orderExecutionAttempts`.
- **Preconditions**: Caller holds `orders.view` and tenant scope.
- **Main Flow**: 1) Query with filters (search/status/paymentStatus/org/store/kiosk). 2) Return scoped results.
- **Alternative/Exception Flow**: `fulfillmentQueue` and `orderItemStatusHistory` are implemented but not mentioned in `docs/flows/MANAGEMENT_READ_FLOW.md`.
- **Related**: `ORD-12`, `ORD-13`, `ORD-14`, `ORD-15`, `ORD-16`, `ORD-17`, `ORD-18`.
- **Evidence**: `functional_inventory.md` ORD-12, ORD-13, ORD-14, ORD-15, ORD-16, ORD-17, ORD-18.
- **Status**: Supported.

#### FR-065 — Automatic Overdue-Fulfillment Reminder
- **Description**: The system shall notify scoped staff at most once per overdue manual/packaged item without altering its fulfillment state.
- **Actor**: System job.
- **Trigger**: `FulfillmentReminderJob` (periodic).
- **Preconditions**: Item past `ExpectedReadyAt` and not yet fulfilled.
- **Main Flow**: 1) Scan overdue items. 2) Issue a durable push `NotificationDelivery` per recipient, once.
- **Alternative/Exception Flow**: None material.
- **Related**: `ORD-19`.
- **Evidence**: `functional_inventory.md` ORD-19.
- **Status**: Supported.

#### FR-066 — Production Incident Lifecycle
- **Description**: The system shall let staff open a production incident against exact-matching production evidence, list/view incidents, record an inspection outcome before any resolution, select a resolution (deliver/discard/remake/refund/voucher/review/no-action), and explicitly close the incident.
- **Actor**: Staff / Manager.
- **Trigger**: `POST/GET/PATCH /api/v1/management/orders/{orderId}/items/{orderItemId}/production-incidents[/{incidentId}][/inspection|resolution|complete]`.
- **Preconditions**: Inspection outcome must be recorded before resolution selection.
- **Main Flow**: 1) Open incident. 2) Record inspection outcome. 3) Select resolution (idempotently), cross-invoking remake dispatch (FR-061) or refund flows (FR-076) as needed. 4) Close with audit notes.
- **Alternative/Exception Flow**: Resolution selection is rejected until inspection is recorded.
- **Related**: `ORD-20`, `ORD-21`, `ORD-22`, `ORD-23`, `ORD-24`.
- **Evidence**: `functional_inventory.md` ORD-20, ORD-21, ORD-22, ORD-23, ORD-24.
- **Status**: Supported.

#### FR-067 — Real-Time Order/Fulfillment/Execution Notifications
- **Description**: The system shall broadcast committed order/item/execution-observation state changes in real time to subscribed SignalR clients.
- **Actor**: System (SignalR).
- **Trigger**: Commit of `OrderStatusChanged`, `OrderItemFulfillmentChanged`, `OrderExecutionObservationChanged`.
- **Preconditions**: Client has joined the relevant `order:{orderId}`/`kiosk:{kioskId}` group.
- **Main Flow**: 1) Publish event to `OrderHub` group on commit.
- **Alternative/Exception Flow**: None material.
- **Related**: `ORD-25`.
- **Evidence**: `functional_inventory.md` ORD-25.
- **Status**: Supported.

### 4.8 Payments

#### FR-068 — Create Payment Session
- **Description**: The system shall create an idempotent PayOS payment session for a paid-eligible order matching the client-displayed amount/currency.
- **Actor**: Customer (Tablet).
- **Trigger**: `POST /api/v1/orders/{orderId}/payment-sessions` (+ `Idempotency-Key` + access token).
- **Preconditions**: Order in a paid-eligible state; requested amount/currency matches server totals.
- **Main Flow**: 1) Validate order eligibility and amount match. 2) Create PayOS session. 3) Return checkout URL/QR payload/expiry.
- **Alternative/Exception Flow**: Reject if the displayed amount/currency does not match server-computed totals.
- **Related**: `PAY-01`.
- **Evidence**: `functional_inventory.md` PAY-01.
- **Status**: Supported.

#### FR-069 — Get Order Payment Status
- **Description**: The system shall let a customer poll the current payment transaction status for their order.
- **Actor**: Customer (Tablet).
- **Trigger**: `GET /api/v1/orders/{orderId}/payment-status`.
- **Preconditions**: Valid access token.
- **Main Flow**: 1) Return current `PaymentTransaction` status.
- **Alternative/Exception Flow**: None material.
- **Related**: `PAY-02`.
- **Evidence**: `functional_inventory.md` PAY-02.
- **Status**: Supported.

#### FR-070 — PayOS Webhook Ingestion
- **Description**: The system shall verify and idempotently apply signed PayOS payment notifications, committing payment/order state atomically and dispatching machine execution when applicable.
- **Actor**: PayOS webhook.
- **Trigger**: `POST /api/v1/payments/payos/webhook`.
- **Preconditions**: Valid `x-payos-signature`.
- **Main Flow**: 1) Verify signature. 2) Idempotently apply notification. 3) Set `PaymentTransaction=Paid`, `Order=ReadyForFulfillment` in one transaction. 4) Dispatch `ExecuteOrder` (attempt 1).
- **Alternative/Exception Flow**: Reject on signature mismatch; duplicate notifications are no-ops.
- **Related**: `PAY-03`.
- **Evidence**: `functional_inventory.md` PAY-03.
- **Status**: Supported.

#### FR-071 — Manual Payment-Session Reconciliation and Intervention Queue
- **Description**: The system shall let authorized operators trigger an audited, on-demand reconciliation of a stuck payment session, and provide a scoped work queue of sessions requiring manual intervention.
- **Actor**: Manager / Staff (`payments.manage`).
- **Trigger**: `POST .../payment-transactions/{paymentTransactionId}/reconcile`; `GET /api/v1/management/payment-session-interventions`.
- **Preconditions**: Session eligible for reconciliation; reason required.
- **Main Flow**: 1) Reconcile against provider. 2) List interventions filtered by provider/code/tenant scope.
- **Alternative/Exception Flow**: None material.
- **Related**: `PAY-04`, `PAY-05`.
- **Evidence**: `functional_inventory.md` PAY-04, PAY-05.
- **Status**: Supported.

#### FR-072 — Order Payment Diagnostics
- **Description**: The system shall expose full payment-transaction diagnostics (provider identity, retries, raw evidence) to diagnostics-scoped staff only.
- **Actor**: Manager / Technician (`operations.diagnostics`).
- **Trigger**: `GET /api/v1/management/orders/{orderId}/payment-diagnostics`.
- **Preconditions**: Caller holds diagnostics policy.
- **Main Flow**: 1) Return raw request/response and retry state per transaction.
- **Alternative/Exception Flow**: None material.
- **Related**: `PAY-06`.
- **Evidence**: `functional_inventory.md` PAY-06.
- **Status**: Supported.

#### FR-073 — Automatic Payment-Session Reconciliation (Background)
- **Description**: The system shall periodically reconcile pending PayOS sessions with the provider and schedule retries or flag manual intervention.
- **Actor**: System job.
- **Trigger**: `PaymentSessionReconciliationJob` (periodic).
- **Preconditions**: Session pending/stale past a threshold.
- **Main Flow**: 1) Query provider for status. 2) Update transaction state or schedule retry. 3) Flag for manual intervention if unresolved.
- **Alternative/Exception Flow**: None material.
- **Related**: `PAY-07`.
- **Evidence**: `functional_inventory.md` PAY-07.
- **Status**: Supported.

#### FR-074 — Payment Method Catalog Management
- **Description**: The system shall list configured payment methods and let authorized managers enable/disable one.
- **Actor**: Manager / Staff (`payments.manage`).
- **Trigger**: `GET /api/v1/management/payment-methods`; `PATCH .../{id}/status`.
- **Preconditions**: Caller holds `payment-methods.manage` (status change).
- **Main Flow**: 1) List, optionally active-only. 2) Toggle `IsActive`.
- **Alternative/Exception Flow**: None material.
- **Related**: `PAY-08`, `PAY-09`.
- **Evidence**: `functional_inventory.md` PAY-08, PAY-09.
- **Status**: Supported.

#### FR-075 — Refund Listing and Detail
- **Description**: The system shall provide a scoped, searchable, filterable list of refund records and full refund detail.
- **Actor**: Manager / Staff (`refunds.manage`).
- **Trigger**: `GET /api/v1/management/refunds[/{refundId}]`.
- **Preconditions**: Caller holds `refunds.manage`.
- **Main Flow**: 1) Return paged/filtered refund list or single detail.
- **Alternative/Exception Flow**: None material.
- **Related**: `PAY-10`.
- **Evidence**: `functional_inventory.md` PAY-10.
- **Status**: Supported.

#### FR-076 — Request Refund
- **Description**: The system shall let staff request a full-order refund or voucher compensation for a paid order flagged RefundRequired, idempotently.
- **Actor**: Manager / Staff.
- **Trigger**: `POST /api/v1/management/orders/{orderId}/refunds` (+ `Idempotency-Key`).
- **Preconditions**: Order is paid and flagged RefundRequired.
- **Main Flow**: 1) Select `FullMoneyRefund` or `Voucher`. 2) Create `Refund` record idempotently.
- **Alternative/Exception Flow**: Repeated requests with the same idempotency key do not create duplicate refunds.
- **Related**: `PAY-11`.
- **Evidence**: `functional_inventory.md` PAY-11.
- **Status**: Supported.

#### FR-077 — Mark Refund Processed, Reject, or Cancel
- **Description**: The system shall let staff confirm a refund/voucher was completed, reject a pending refund with a mandatory reason, or cancel a not-yet-processed refund.
- **Actor**: Manager / Staff.
- **Trigger**: `PATCH /api/v1/management/refunds/{refundId}/mark-processed|reject|cancel`.
- **Preconditions**: Reject requires a reason; mark-processed/cancel apply only to non-terminal refunds.
- **Main Flow**: 1) Apply the requested transition. 2) Update order/payment status accordingly, including duplicate-payment resolution on mark-processed.
- **Alternative/Exception Flow**: Rejecting leaves the order RefundRequired.
- **Related**: `PAY-12`, `PAY-13`, `PAY-14`.
- **Evidence**: `functional_inventory.md` PAY-12, PAY-13, PAY-14.
- **Status**: Supported.

#### FR-078 — Real-Time Payment Notifications and Intervention Push
- **Description**: The system shall broadcast committed payment status transitions and refund-affecting dashboard invalidations in real time, and push-notify scoped staff exactly once per payment transaction/intervention-code occurrence when automatic reconciliation cannot resolve a session.
- **Actor**: System (SignalR / notification job).
- **Trigger**: Commit of `PaymentStatusChanged`; reconciliation reaching manual intervention.
- **Preconditions**: None beyond the triggering event.
- **Main Flow**: 1) Publish `PaymentStatusChanged` to `order:{orderId}`. 2) Publish `DashboardInvalidated` on refund changes. 3) Issue durable push notification on intervention.
- **Alternative/Exception Flow**: None material.
- **Related**: `PAY-15`, `PAY-16`.
- **Evidence**: `functional_inventory.md` PAY-15, PAY-16.
- **Status**: Supported.

### 4.9 Operations

#### FR-079 — Alert Listing, Detail, Acknowledgement, and Resolution
- **Description**: The system shall provide a scoped, filterable, paginated list of operational alerts and full detail, and let authorized staff acknowledge (idempotently) or resolve (with mandatory notes) an open alert.
- **Actor**: Staff / Manager / Technician (`alerts.view`/`alerts.manage`).
- **Trigger**: `GET /api/v1/management/alerts[/{alertId}]`; `PATCH .../acknowledge`; `PATCH .../resolve`.
- **Preconditions**: Alert Open (acknowledge/resolve).
- **Main Flow**: 1) List/view ordered by latest occurrence. 2) Acknowledge recording actor/timestamp. 3) Resolve with mandatory resolution notes, terminating the lifecycle.
- **Alternative/Exception Flow**: None material.
- **Related**: `OPS-01`, `OPS-02`, `OPS-03`.
- **Evidence**: `functional_inventory.md` OPS-01, OPS-02, OPS-03.
- **Status**: Supported.

#### FR-080 — Automatic Alert Creation and Correlation
- **Description**: The system shall automatically create or correlate an actionable Alert from Error/Critical device telemetry within a rolling correlation window, and shall similarly derive alerts from inventory thresholds and MQTT-credential operation failures.
- **Actor**: Edge runtime (via telemetry ingestion) / System job.
- **Trigger**: Error/Critical `DeviceEvent` ingestion (FR-028); `InventoryAlertReconciliationJob`; MQTT-credential reconciliation reaching `RevokeFailed`/timeout-`Failed`.
- **Preconditions**: Event severity within Error/Critical, or inventory/credential state crosses a threshold.
- **Main Flow**: 1) Create or correlate one Open alert per source/threshold. 2) Raise/resolve `INVENTORY_LOW`/`INVENTORY_EMPTY` or `MQTT_CREDENTIAL_*` alerts as state changes.
- **Alternative/Exception Flow**: Maintains exactly one active alert per threshold rather than duplicating.
- **Related**: `OPS-04`, `OPS-05`, `OPS-06`.
- **Evidence**: `functional_inventory.md` OPS-04, OPS-05, OPS-06.
- **Status**: Supported.

#### FR-081 — Critical Alert Push Notification
- **Description**: The system shall push-notify scoped operational staff exactly once when a new or escalated Critical alert is committed.
- **Actor**: System job.
- **Trigger**: New/escalated Critical alert commit.
- **Preconditions**: Alert severity is Critical.
- **Main Flow**: 1) Issue durable push `NotificationDelivery` to scoped Technician/Manager (OrgAdmin fallback), exactly once.
- **Alternative/Exception Flow**: None material.
- **Related**: `OPS-07`.
- **Evidence**: `functional_inventory.md` OPS-07.
- **Status**: Supported.

#### FR-082 — Maintenance Ticket Creation, Listing, and Update
- **Description**: The system shall let authorized staff open a kiosk-scoped maintenance ticket (optionally linked to device/order/event evidence) with a unique ticket number, list/view tickets, and edit descriptive fields.
- **Actor**: Staff / Manager / Technician.
- **Trigger**: `POST /api/v1/management/maintenance-tickets`; `GET [/{id}]`; `PUT /{id}`.
- **Preconditions**: Caller holds `maintenance.create`/`.view`/`.manage`.
- **Main Flow**: 1) Create with generated ticket number. 2) List/view filtered by tenant/priority/status/assignee/date. 3) Edit fields and evidence links.
- **Alternative/Exception Flow**: None material.
- **Related**: `OPS-08`, `OPS-09`, `OPS-10`.
- **Evidence**: `functional_inventory.md` OPS-08, OPS-09, OPS-10.
- **Status**: Supported.

#### FR-083 — Maintenance Ticket Work Lifecycle
- **Description**: The system shall let authorized staff assign, start, resolve, close, and cancel a maintenance ticket, atomically moving the kiosk to Maintenance/EmergencyStopRequested when the ticket's operational impact requires it.
- **Actor**: Manager / Technician / OrgAdmin.
- **Trigger**: `PATCH .../maintenance-tickets/{id}/assign|start|resolve|close|cancel`.
- **Preconditions**: Ticket in the appropriate prior state (Open→Assigned→InProgress→Resolved→Closed; Cancel from Open/Assigned/InProgress).
- **Main Flow**: 1) Assign to an eligible Technician/Manager/OrgAdmin in scope. 2) Start work, applying kiosk state change if required. 3) Resolve with mandatory notes. 4) Close. 5) Or cancel with a mandatory reason.
- **Alternative/Exception Flow**: None material beyond state-machine enforcement.
- **Related**: `OPS-11`, `OPS-12`, `OPS-13`, `OPS-14`, `OPS-15`.
- **Evidence**: `functional_inventory.md` OPS-11, OPS-12, OPS-13, OPS-14, OPS-15.
- **Status**: Supported.

#### FR-084 — Automatic Maintenance Ticket from Inventory-Empty Alert
- **Description**: The system shall optionally auto-create one linked maintenance ticket when an ingredient goes Empty.
- **Actor**: System job.
- **Trigger**: `INVENTORY_EMPTY` alert raised (if configured).
- **Preconditions**: Feature configured on for the tenant.
- **Main Flow**: 1) Create Open ticket linked to the alert.
- **Alternative/Exception Flow**: None material.
- **Related**: `OPS-16`.
- **Evidence**: `functional_inventory.md` OPS-16.
- **Status**: Supported.

#### FR-085 — Operation Log Listing and Diagnostics
- **Description**: The system shall provide a kiosk-scoped, filterable, curated operation-log feed excluding raw payload by default, and expose raw payload only to diagnostics-scoped staff.
- **Actor**: Staff / Manager (curated); Technician / Manager (diagnostics).
- **Trigger**: `GET /api/v1/management/kiosks/{kioskId}/operation-logs[/{operationLogId}][/diagnostics]`.
- **Preconditions**: Caller holds `operations.view` (curated) or `operations.diagnostics` (raw).
- **Main Flow**: 1) Return curated feed. 2) Return raw `PayloadJson` only for diagnostics-scoped callers.
- **Alternative/Exception Flow**: None material.
- **Related**: `OPS-17`, `OPS-18`.
- **Evidence**: `functional_inventory.md` OPS-17, OPS-18.
- **Status**: Supported.

#### FR-086 — Notification Delivery Diagnostics, Requeue, and Automatic Delivery
- **Description**: The system shall provide a scoped diagnostics view of the notification outbox, let authorized staff requeue a permanently-failed delivery, and periodically claim/attempt due deliveries with retry/backoff.
- **Actor**: Technician / Manager (diagnostics/requeue); System job (delivery).
- **Trigger**: `GET .../notification-deliveries[/{deliveryId}]`; `POST .../{deliveryId}/requeue`; `NotificationDeliveryJob` (periodic).
- **Preconditions**: Requeue applies only to permanently-failed deliveries and requires a reason (3–500 chars).
- **Main Flow**: 1) List/view outbox status/attempts/errors. 2) Requeue without repeating the source business transition. 3) Job claims and attempts due deliveries up to a max-attempt limit.
- **Alternative/Exception Flow**: None material.
- **Related**: `OPS-19`, `OPS-20`, `OPS-21`.
- **Evidence**: `functional_inventory.md` OPS-19, OPS-20, OPS-21.
- **Status**: Supported.

#### FR-087 — Real-Time Operations Notifications
- **Description**: The system shall broadcast committed alert and maintenance-ticket changes in real time to operations dashboards.
- **Actor**: System (SignalR).
- **Trigger**: Commit of `AlertChanged`, `MaintenanceTicketChanged`.
- **Preconditions**: Client has joined the relevant `kiosk:{kioskId}` group on `OperationsHub`.
- **Main Flow**: 1) Publish event to subscribed clients.
- **Alternative/Exception Flow**: None material.
- **Related**: `OPS-22`.
- **Evidence**: `functional_inventory.md` OPS-22.
- **Status**: Supported.

### 4.10 Robot Configuration

#### FR-088 — List/Get Robot Artifacts and Usage
- **Description**: The system shall let authorized users list and inspect organization-scoped robot artifacts by status/search, and report which robot programs reference a given artifact.
- **Actor**: OrgAdmin / Manager (`artifact.read`).
- **Trigger**: `GET .../organizations/{organizationId}/robot-artifacts[/{artifactId}][/usage]`.
- **Preconditions**: Caller holds `artifact.read`.
- **Main Flow**: 1) List/filter by status/search. 2) Return usage report referencing programs.
- **Alternative/Exception Flow**: None material.
- **Related**: `RC-01`.
- **Evidence**: `functional_inventory.md` RC-01.
- **Status**: Supported.

#### FR-089 — Bulk Upload Robot Artifacts (Fairino .lua)
- **Description**: The system shall accept a bounded batch of `.lua` files with per-file metadata and create unassigned Draft RobotArtifacts, deduplicating by organization+code+checksum.
- **Actor**: OrgAdmin (`artifact.upload`).
- **Trigger**: `POST .../robot-artifacts` (multipart, 1–50 files + manifest JSON).
- **Preconditions**: Each item declares artifact code/name, runtime target, machine model.
- **Main Flow**: 1) Validate manifest per file. 2) Create Draft artifacts, or return existing (`wasExisting`) on exact match.
- **Alternative/Exception Flow**: Partial batch failure returns HTTP 207 with per-item outcomes.
- **Related**: `RC-02`.
- **Evidence**: `functional_inventory.md` RC-02.
- **Status**: Supported.

#### FR-090 — Clone Artifact from Global Template
- **Description**: The system shall let an organization clone a Published global RobotArtifactTemplate (with its published technical contract) into an owned Draft RobotArtifact.
- **Actor**: OrgAdmin (`artifact.upload`).
- **Trigger**: `POST .../robot-artifacts/from-template`.
- **Preconditions**: Source template Published.
- **Main Flow**: 1) Copy template + technical contract into a Draft artifact linked via `SourceRobotArtifactTemplateId`.
- **Alternative/Exception Flow**: None material.
- **Related**: `RC-03`.
- **Evidence**: `functional_inventory.md` RC-03.
- **Status**: Supported.

#### FR-091 — Publish, Bulk-Publish, Retire, and Discard Robot Artifacts
- **Description**: The system shall publish one or many Draft robot artifacts atomically only when each has a compatible Published technical contract and verified object-storage checksum/size, retire a Published artifact, or hard-delete an unreferenced Draft.
- **Actor**: OrgAdmin (`artifact.upload`).
- **Trigger**: `PATCH .../{id}/publish`; `PATCH .../publish` (bulk, up to 100 ids); `PATCH .../{id}/retire`; `DELETE .../{id}`.
- **Preconditions**: Compatible Published technical contract assigned; checksum/size verified against object storage.
- **Main Flow**: 1) Validate each artifact. 2) Publish/retire/delete without breaking published program manifests.
- **Alternative/Exception Flow**: Reject publish if the contract is missing/incompatible or the checksum verification fails.
- **Related**: `RC-04`.
- **Evidence**: `functional_inventory.md` RC-04.
- **Status**: Supported.

#### FR-092 — Create Artifact Review (Presigned) URL
- **Description**: The system shall issue a short-lived presigned download URL for reviewing an artifact's Lua bytes without exposing a persistent link.
- **Actor**: OrgAdmin (`artifact.upload`).
- **Trigger**: `POST .../robot-artifacts/{id}/review-url`.
- **Preconditions**: Artifact exists with a stored object.
- **Main Flow**: 1) Request a time-limited URL from MinIO via `IArtifactObjectStorage`. 2) Return URL, expiry, checksum, size.
- **Alternative/Exception Flow**: None material.
- **Related**: `RC-05`.
- **Evidence**: `functional_inventory.md` RC-05.
- **Status**: Supported.

#### FR-093 — Global RobotArtifactTemplate Lifecycle
- **Description**: The system shall let a SystemAdmin upload, review, publish, retire, and discard globally reusable robot artifact templates, distinct from organization artifacts.
- **Actor**: SystemAdmin.
- **Trigger**: `GET/POST/PATCH/DELETE .../robot-artifact-templates[/{id}][/publish|retire]`.
- **Preconditions**: SystemAdmin-only.
- **Main Flow**: 1) Upload/review/publish/retire/discard per lifecycle.
- **Alternative/Exception Flow**: None material.
- **Related**: `RC-06`.
- **Evidence**: `functional_inventory.md` RC-06.
- **Status**: Supported.

#### FR-094 — Robot Artifact Technical Contract Authoring
- **Description**: The system shall let authors declare, validate, publish, and retire versioned technical contracts (effects, quantity mode, ordering constraints) for a Lua artifact, at global or organization scope.
- **Actor**: SystemAdmin / OrgAdmin.
- **Trigger**: `GET/POST/PUT/DELETE .../robot-artifact-technical-contracts[/{id}]`; `POST .../validation-preview`; `PATCH .../publish|retire`.
- **Preconditions**: Contract compatible with the declared runtime target/machine model.
- **Main Flow**: 1) Author contract with declared effects/ordering constraints. 2) Validate. 3) Publish/retire.
- **Alternative/Exception Flow**: None material.
- **Related**: `RC-07`.
- **Evidence**: `functional_inventory.md` RC-07.
- **Status**: Supported.

#### FR-095 — Import Fairino `.icebot.json` Sidecars
- **Description**: The system shall convert reviewed Fairino sidecar files (schema v1/v2) into organization Draft technical contracts, replacing an existing Draft of the same code/version.
- **Actor**: OrgAdmin (`artifact.upload`).
- **Trigger**: `POST .../organizations/{orgId}/robot-artifact-technical-contracts/import-sidecars`.
- **Preconditions**: 1–50 sidecar items, schema v1 or v2.
- **Main Flow**: 1) Parse each sidecar. 2) Create/replace Draft technical contract per item.
- **Alternative/Exception Flow**: Per-item result reports success/failure independently.
- **Related**: `RC-08`.
- **Evidence**: `functional_inventory.md` RC-08.
- **Status**: Supported.

#### FR-096 — Assign Technical Contract to Template/Artifact
- **Description**: The system shall bind a Published, target-compatible technical contract to a Draft artifact or global template before publication.
- **Actor**: SystemAdmin / OrgAdmin.
- **Trigger**: `PUT .../robot-artifact-templates/{id}/technical-contract`; `PUT .../robot-artifacts/{id}/technical-contract`.
- **Preconditions**: Contract Published and target/model-compatible.
- **Main Flow**: 1) Validate compatibility. 2) Assign contract reference.
- **Alternative/Exception Flow**: Reject incompatible or non-Published contracts.
- **Related**: `RC-09`.
- **Evidence**: `functional_inventory.md` RC-09.
- **Status**: Supported.

#### FR-097 — Robot Program CRUD and RunOrder Authoring
- **Description**: The system shall let authors create an ordered RobotProgram, atomically replace its artifact membership with explicit unique RunOrder while Draft, and publish an immutable ordered manifest.
- **Actor**: OrgAdmin (`program.read`/`program.manage`).
- **Trigger**: `GET/POST .../robot-programs`; `PUT .../{id}`; `PUT .../{id}/artifacts`; `PATCH .../publish|retire`; `DELETE .../{id}`.
- **Preconditions**: Draft state for artifact-membership replacement; unique `RunOrder` per artifact.
- **Main Flow**: 1) Create/update program. 2) Replace ordered artifact list. 3) Publish immutable manifest via `RobotProgramManifestBuilder`.
- **Alternative/Exception Flow**: None material.
- **Related**: `RC-10`.
- **Evidence**: `functional_inventory.md` RC-10.
- **Status**: Supported.

#### FR-098 — Robot Authoring Bundle Upload, Validation, Materialization, Discard
- **Description**: The system shall accept one bounded Fairino-Studio export ZIP, verify its structure/checksum, create a durable idempotent import session, allow revalidation, and materialize Draft technical contracts/artifacts/one ordered Draft RobotProgram from a validated import — or allow discarding an import that has not reached Materialized.
- **Actor**: OrgAdmin (`artifact.upload` [+ `program.manage` for materialize]).
- **Trigger**: `POST .../robot-authoring-imports` (+ `Idempotency-Key`); `POST .../{importId}/validate`; `POST .../{importId}/materialize`; `POST .../{importId}/discard`.
- **Preconditions**: Bundle contains `export-manifest.json` + `artifacts/` + `contracts/`; checksum verified.
- **Main Flow**: 1) Upload and stage bundle. 2) Validate structure/checksum/RunOrder/existing-identity conflicts. 3) Materialize into Draft resources in one serialized metadata transaction. 4) Discard if abandoned before Materialized, best-effort deleting staged bundle.
- **Alternative/Exception Flow**: Validation returns `CanMaterialize=false` with errors/warnings rather than blocking silently.
- **Related**: `RC-11`, `RC-12`, `RC-13`, `RC-14`.
- **Evidence**: `functional_inventory.md` RC-11, RC-12, RC-13, RC-14.
- **Status**: Supported.

#### FR-099 — Preview/Confirm Semantic Composition and Publish Import Resources
- **Description**: The system shall resolve recipe ingredient/option requirements against imported artifact technical effects, propose an ordered artifact composition with a deterministic checksum, atomically apply it to the Draft program on confirmation, and then resumably publish each materialized contract, artifact, and the program.
- **Actor**: OrgAdmin (`artifact.upload` + `program.manage`).
- **Trigger**: `POST .../{importId}/preview-composition`; `POST .../{importId}/confirm-composition`; `POST .../{importId}/publish-resources`.
- **Preconditions**: Confirm requires the `previewChecksum` from the preview step.
- **Main Flow**: 1) Preview proposed order/blockers. 2) Confirm applies composition to the Draft program. 3) Publish resources stopping at the exact resource error if one occurs.
- **Alternative/Exception Flow**: Stale preview checksum on confirm is rejected.
- **Related**: `RC-15`, `RC-16`.
- **Evidence**: `functional_inventory.md` RC-15, RC-16.
- **Status**: Supported.

#### FR-100 — Create Configuration Release Draft from Import; Get Import/Workspace
- **Description**: The system shall derive a Draft ConfigurationRelease and single execution route/binding automatically from a published authoring import and recipe/option selection, and shall provide a single convergence read model reporting import status, blockers, and allowed next actions.
- **Actor**: OrgAdmin (`release.publish` / `program.read`).
- **Trigger**: `POST .../{importId}/create-release-draft`; `GET .../{importId}[/workspace]`.
- **Preconditions**: Import published/materialized as applicable.
- **Main Flow**: 1) Derive release/route/binding. 2) Return workspace read model.
- **Alternative/Exception Flow**: None material.
- **Related**: `RC-17`, `RC-18`.
- **Evidence**: `functional_inventory.md` RC-17, RC-18.
- **Status**: Supported.

#### FR-101 — Robot Artifact Orphan Object Cleanup
- **Description**: The system shall periodically delete unreferenced robot-artifact/authoring-import objects older than a grace period, holding a distributed advisory lock to avoid duplicate runs.
- **Actor**: System job.
- **Trigger**: `RobotArtifactOrphanCleanupJob` (interval-based).
- **Preconditions**: Object unreferenced and past grace period.
- **Main Flow**: 1) Compare referenced storage keys vs. object storage listing. 2) Delete orphans under advisory lock.
- **Alternative/Exception Flow**: None material.
- **Related**: `RC-19`.
- **Evidence**: `functional_inventory.md` RC-19.
- **Status**: Supported.

`[Inferred]` — `RC-20` ("Apply"→"Materialize" import terminology) is a naming/documentation note in `functional_inventory.md`, not a distinct functional capability: it records that the public API surface for FR-098's materialize step uses "materialize" terminology while the persisted enum value is internally still named `Applied`. It is called out here explicitly so it is not mistaken for an uncovered gap. Evidence: `functional_inventory.md` RC-20.

### 4.11 Production Configuration

#### FR-102 — Create Configuration Release Draft and Author Execution Routes
- **Description**: The system shall create a Draft ConfigurationRelease with the next sequential organization release number, and atomically replace its execution routes and ordered robot-program bindings after validating product/recipe/program references.
- **Actor**: OrgAdmin (`release.publish`).
- **Trigger**: `POST .../organizations/{orgId}/configuration-releases`; `PUT .../{releaseId}/routes`.
- **Preconditions**: Release in Draft for route replacement.
- **Main Flow**: 1) Allocate release number. 2) Replace routes/bindings after reference validation.
- **Alternative/Exception Flow**: None material.
- **Related**: `PC-01`, `PC-02`.
- **Evidence**: `functional_inventory.md` PC-01, PC-02.
- **Status**: Supported.

#### FR-103 — Publish, Retire, Discard Configuration Release; Review Releases
- **Description**: The system shall publish an immutable, deployment-profile-neutral release manifest only when route/binding/inventory-readiness validation passes, allow retiring/discarding per lifecycle rules, and let authorized users review releases and authoring options.
- **Actor**: OrgAdmin (`release.publish`/`release.read`).
- **Trigger**: `PATCH .../{releaseId}/publish|retire`; `DELETE .../{releaseId}`; `GET .../configuration-releases[/{releaseId}][/authoring-options]`.
- **Preconditions**: Inventory readiness policy passes (`ProductionInventoryReadinessGuard`).
- **Main Flow**: 1) Validate routes/bindings/readiness. 2) Publish immutable manifest. 3) Retire/discard per lifecycle.
- **Alternative/Exception Flow**: Publish blocked (409) if inventory readiness fails.
- **Related**: `PC-03`, `PC-04`.
- **Evidence**: `functional_inventory.md` PC-03, PC-04.
- **Status**: Supported.

#### FR-104 — Preview Configuration Deployment
- **Description**: The system shall preview deployment eligibility (readiness, capability, inventory, capacity) and return a deterministic `deploymentChecksum` that the deploy request must echo.
- **Actor**: OrgAdmin (`release.deploy`).
- **Trigger**: `POST .../kiosks/{kioskId}/configuration-deployments/preview`.
- **Preconditions**: Published configuration release.
- **Main Flow**: 1) Evaluate per-endpoint eligibility/blockers. 2) Return checksum, artifact totals, validation report.
- **Alternative/Exception Flow**: None material.
- **Related**: `PC-05`.
- **Evidence**: `functional_inventory.md` PC-05.
- **Status**: Supported.

#### FR-105 — Deploy Full Edge Configuration
- **Description**: The system shall build/reuse the deterministic Full Edge ZIP from the published manifest and create a durable `DeployConfiguration` edge command, blocking on inventory readiness and a stale/missing preview checksum.
- **Actor**: OrgAdmin (`release.deploy`).
- **Trigger**: `POST .../kiosks/{kioskId}/configuration-deployments/full-edge` (+ `Idempotency-Key`).
- **Preconditions**: Valid, matching `deploymentPreviewChecksum`; inventory readiness passes; `acknowledgeRemainingRisk` supplied if applicable.
- **Main Flow**: 1) Validate checksum/readiness. 2) Build/reuse ZIP bundle. 3) Create durable edge command.
- **Alternative/Exception Flow**: Reject if checksum stale/missing or readiness fails.
- **Related**: `PC-06`.
- **Evidence**: `functional_inventory.md` PC-06.
- **Status**: Supported.

#### FR-106 — Deploy Low-Cost Artifact Set
- **Description**: The system shall create a capacity-limited artifact-set deployment for a low-cost controller from explicit route/program selections, enforcing controller artifact-count/storage capacity and inventory readiness.
- **Actor**: OrgAdmin (`release.deploy`).
- **Trigger**: `POST .../kiosks/{kioskId}/configuration-deployments/low-cost` (+ `Idempotency-Key`).
- **Preconditions**: Selections fit within controller capacity; matching preview checksum.
- **Main Flow**: 1) Validate capacity/readiness. 2) Create `ControllerArtifactSetDeployment`/items.
- **Alternative/Exception Flow**: Reject if capacity exceeded.
- **Related**: `PC-07`.
- **Evidence**: `functional_inventory.md` PC-07.
- **Status**: Supported.

#### FR-107 — Rollback Configuration Deployment
- **Description**: The system shall let an operator select a previously Active deployment as an immutable rollback target and create a new deployment/command without mutating deployment/artifact history.
- **Actor**: OrgAdmin (`release.rollback`).
- **Trigger**: `POST .../configuration-deployments/{deploymentId}/rollback` (+ `Idempotency-Key`).
- **Preconditions**: Target deployment was previously Active.
- **Main Flow**: 1) Dispatch to the same Full Edge/Low-Cost deploy handler with `IsRollback=true`.
- **Alternative/Exception Flow**: None material.
- **Related**: `PC-08`.
- **Evidence**: `functional_inventory.md` PC-08.
- **Status**: Supported.

#### FR-108 — Monitor Deployments and Inspect Deployed Artifacts
- **Description**: The system shall expose a unified, tenant-scoped read history across Full Edge and Low-cost deployment profiles including failure provenance.
- **Actor**: OrgAdmin (`deployment.read`).
- **Trigger**: `GET .../configuration-deployments`; `GET .../kiosks/{kioskId}/configuration-deployments[/{id}][/artifacts]`.
- **Preconditions**: None beyond read policy.
- **Main Flow**: 1) Return filtered deployment history/artifact snapshot.
- **Alternative/Exception Flow**: None material.
- **Related**: `PC-09`.
- **Evidence**: `functional_inventory.md` PC-09.
- **Status**: Supported.

#### FR-109 — Configuration Inventory Readiness Gate (Query)
- **Description**: The system shall let a user query kiosk inventory readiness for a release's routes, matching the policy enforced at publish/deploy time.
- **Actor**: OrgAdmin / Manager (`inventory.view`).
- **Trigger**: `GET .../kiosks/{kioskId}/configuration-releases/{releaseId}/inventory-readiness`.
- **Preconditions**: Release exists.
- **Main Flow**: 1) Evaluate readiness via the same `ProductionInventoryReadinessGuard`/`InventoryReadinessPolicyOptions` used at publish/deploy time (FR-055, FR-103, FR-105/106).
- **Alternative/Exception Flow**: None material.
- **Related**: `PC-10`.
- **Evidence**: `functional_inventory.md` PC-10.
- **Status**: Supported.

#### FR-110 — Deployment Timeout Reconciliation and Failure Notification
- **Description**: The system shall automatically fail deployments whose edge command expired, whose accepted command received no installation report, or whose Installed state never reached Active within configured timeouts, and shall notify operators when a deployment transitions to Failed.
- **Actor**: System job.
- **Trigger**: `DeploymentTimeoutReconciliationJob`; `DeploymentFailureNotificationJob` (both periodic).
- **Preconditions**: Deployment exceeds a configured timeout, or newly transitions Failed.
- **Main Flow**: 1) Mark deployments Failed with reason codes (`CommandExpired`, `ExecutionReportTimeout`, `ActivationReportTimeout`). 2) Dispatch failure notification.
- **Alternative/Exception Flow**: None material.
- **Related**: `PC-11`, `PC-12`.
- **Evidence**: `functional_inventory.md` PC-11, PC-12.
- **Status**: Supported (PC-12 confirmed via wiring/registration, not full line-by-line read).

### 4.12 Production Packages

#### FR-111 — Production Package CRUD and Version/Definition Authoring
- **Description**: The system shall let a SystemAdmin create, rename, and retire a platform-level ProductionPackage, and define/publish/retire an immutable package manifest version (global products, published artifact templates with technical contracts, program blueprints/slots, route blueprints) after deterministic validation.
- **Actor**: SystemAdmin.
- **Trigger**: `GET/POST/PUT/PATCH .../production-packages[/{id}][/retire]`; `POST .../{packageId}/versions`; `GET/PUT .../versions/{versionId}/definition`; `PATCH .../publish|retire`.
- **Preconditions**: Definition passes `ProductionPackageDefinitionValidator` before publish.
- **Main Flow**: 1) CRUD package. 2) Define version manifest. 3) Publish after validation.
- **Alternative/Exception Flow**: None material.
- **Related**: `PP-01`, `PP-02`.
- **Evidence**: `functional_inventory.md` PP-01, PP-02.
- **Status**: Supported.

#### FR-112 — Organization Package Catalog and Installation Preview
- **Description**: The system shall show an organization only Published production package versions available for installation, and preview a package installation's selected-product resolution and manifest checksum without materializing resources.
- **Actor**: OrgAdmin / Manager (`package.read`).
- **Trigger**: `GET .../organizations/{orgId}/production-packages/catalog`; `POST .../production-package-installations/preview`.
- **Preconditions**: Package version Published.
- **Main Flow**: 1) List catalog. 2) Preview resolved product keys, program/route codes, warnings, and manifest checksum.
- **Alternative/Exception Flow**: None material.
- **Related**: `PP-03`, `PP-04`.
- **Evidence**: `functional_inventory.md` PP-03, PP-04.
- **Status**: Supported.

#### FR-113 — Install Production Package
- **Description**: The system shall materialize (or reuse) organization Products/Recipes/RobotArtifacts/RobotPrograms and a Draft ConfigurationRelease from an immutable published package version, idempotently keyed and serialized against concurrent authoring/other installs.
- **Actor**: OrgAdmin / Manager (`package.install`).
- **Trigger**: `POST .../production-package-installations` (+ `Idempotency-Key`).
- **Preconditions**: Package version Published; no conflicting concurrent install/authoring in progress.
- **Main Flow**: 1) Resolve selected products. 2) Materialize or reuse resources. 3) Produce a Draft configuration.
- **Alternative/Exception Flow**: Concurrent conflicting operations are serialized/rejected rather than corrupting state.
- **Related**: `PP-05`.
- **Evidence**: `functional_inventory.md` PP-05.
- **Status**: Supported.

#### FR-114 — Get/List Installations and Workspace; Retry Failed Installation
- **Description**: The system shall provide a single aggregated workspace read model reporting technical/commercial readiness and next required/optional/recovery actions for an installation, and let a Failed installation be retried using its original selected-product snapshot.
- **Actor**: OrgAdmin / Manager (`package.read`/`package.install`).
- **Trigger**: `GET .../production-package-installations[/{id}][/workspace]`; `POST .../{installationId}/retry`.
- **Preconditions**: Retry applies only to Failed installations.
- **Main Flow**: 1) Return workspace read model. 2) Retry without reconstructing the original request.
- **Alternative/Exception Flow**: None material.
- **Related**: `PP-06`, `PP-07`.
- **Evidence**: `functional_inventory.md` PP-06, PP-07.
- **Status**: Supported.

#### FR-115 — Fork Package-Managed Installation
- **Description**: The system shall convert an Installed package-managed configuration into an organization-owned fork, copy-on-write cloning shared Draft-referenced artifacts, so it can be customized outside package lifecycle constraints.
- **Actor**: OrgAdmin (`package.fork`).
- **Trigger**: `POST .../{installationId}/fork`.
- **Preconditions**: Installation is Installed and package-managed.
- **Main Flow**: 1) Clone shared Draft-referenced artifacts. 2) Set `OwnershipMode=OrganizationFork`.
- **Alternative/Exception Flow**: None material.
- **Related**: `PP-08`.
- **Evidence**: `functional_inventory.md` PP-08.
- **Status**: Supported.

#### FR-116 — Repair Installation Materializations
- **Description**: The system shall restore soft-deleted package materialization targets in place for an Installed, package-managed installation, and reject with itemized issues when automatic repair is unsafe.
- **Actor**: OrgAdmin / Manager (`package.install`).
- **Trigger**: `POST .../{installationId}/repair`.
- **Preconditions**: Installation Installed and package-managed.
- **Main Flow**: 1) Identify soft-deleted materialization targets. 2) Restore in place, or reject (409) with issues.
- **Alternative/Exception Flow**: Reject when repair is unsafe (e.g., conflicting concurrent changes).
- **Related**: `PP-09`.
- **Evidence**: `functional_inventory.md` PP-09.
- **Status**: Supported.

#### FR-117 — Preview and Execute Package Upgrade
- **Description**: The system shall preview an upgrade from an Installed package-managed installation to a newer Published package version (returning a deterministic checksum), and materialize a reviewable successor installation for an approved upgrade preview, revalidating the checksum before/after materialization and remaining resumable on retry.
- **Actor**: OrgAdmin / Manager (`package.read`/`package.install`).
- **Trigger**: `POST .../{installationId}/upgrades/preview`; `POST .../{installationId}/upgrades` (+ `Idempotency-Key`).
- **Preconditions**: Target package version Published and newer than source.
- **Main Flow**: 1) Preview added/removed/changed products, menu impact, blockers. 2) Execute, revalidating checksum, producing `ReadyForReview`.
- **Alternative/Exception Flow**: Stale checksum on execute is rejected.
- **Related**: `PP-10`, `PP-11`.
- **Evidence**: `functional_inventory.md` PP-10, PP-11.
- **Status**: Supported.

#### FR-118 — List/Get Package Upgrades, Cutover, Rollback, and Abandon
- **Description**: The system shall expose upgrade history/detail (including endpoint rollback attempt audit trail), cut an upgrade over only when every frozen execution endpoint has an Active deployment on the successor release, roll back a Completed upgrade in two phases (dispatch rollback deployments then restore catalog/menu bindings), and let an operator abandon a ReadyForReview/Failed upgrade.
- **Actor**: OrgAdmin / Manager (`package.read`/`package.install`); OrgAdmin (`release.rollback`).
- **Trigger**: `GET .../{installationId}/upgrades[/{upgradeId}]`; `POST .../{upgradeId}/cutover`; `POST .../{upgradeId}/rollback`; `POST .../{upgradeId}/abandon`.
- **Preconditions**: Cutover requires all frozen endpoints Active on successor; rollback dispatches up to 3 attempts/endpoint; abandon requires ReadyForReview/Failed state and no still-referenced successor resources.
- **Main Flow**: 1) List/detail upgrades. 2) Cutover reassigns canonical product codes, rebinds menu items, supersedes source installation transactionally. 3) Rollback dispatches then restores. 4) Abandon soft-deletes successor roots preserving audit provenance.
- **Alternative/Exception Flow**: Abandon is rejected if successor resources are still referenced.
- **Related**: `PP-12`, `PP-13`, `PP-14`, `PP-15`.
- **Evidence**: `functional_inventory.md` PP-12, PP-13, PP-14, PP-15.
- **Status**: Supported.

#### FR-119 — Stale Upgrade Reconciliation
- **Description**: The system shall automatically fail a stuck Materializing upgrade after a configured timeout without inferring cutover or edge failure.
- **Actor**: System job.
- **Trigger**: `ProductionPackageUpgradeReconciliationJob` (periodic).
- **Preconditions**: Upgrade Materializing with no persisted progress past `MaterializingTimeoutMinutes`.
- **Main Flow**: 1) Detect stuck upgrade. 2) Mark `Failed/UpgradeMaterializationTimedOut`.
- **Alternative/Exception Flow**: None material.
- **Related**: `PP-16`.
- **Evidence**: `functional_inventory.md` PP-16.
- **Status**: Supported (job wiring confirmed; reconciliation service internals not opened).

### 4.13 IoT REST and MQTT Edge Contract

Device event ingestion (`IOT-01`), batch telemetry (`IOT-02`), heartbeat (`IOT-03`), and readiness ingestion (`IOT-04`) are the same capabilities as FR-028, FR-029, FR-027, and FR-030 respectively (§4.3), reachable identically over REST or MQTT (see FR-126); they are not repeated here.

#### FR-120 — Edge Command Pull (Dispatch Delivery)
- **Description**: The system shall let an authenticated, active execution endpoint pull up to N pending commands, enrich artifact payloads with short-lived URLs, and record a delivery attempt.
- **Actor**: Edge runtime.
- **Trigger**: `POST /api/v1/iot/execution-endpoints/{endpointId}/commands/pull`.
- **Preconditions**: Endpoint authenticated and Active.
- **Main Flow**: 1) Fetch up to `MaxCommands` pending commands. 2) Enrich payload (e.g. artifact URL). 3) Record delivery attempt.
- **Alternative/Exception Flow**: None material.
- **Related**: `IOT-05`.
- **Evidence**: `functional_inventory.md` IOT-05.
- **Status**: Supported.

#### FR-121 — Edge Command Acknowledgement
- **Description**: The system shall apply command acknowledgement state transitions, project `ExecuteOrder` acceptance/rejection onto order lifecycle, and enforce a clock-skew acknowledgement window.
- **Actor**: Edge runtime.
- **Trigger**: `POST /api/v1/iot/execution-endpoints/{endpointId}/commands/{commandId}/ack`.
- **Preconditions**: Command previously delivered to this endpoint.
- **Main Flow**: 1) Apply `AckStatus` (Received/Accepted/Rejected/ExecutorBusy/DeliveryFailed). 2) Project onto order status where applicable, publishing `OrderStatusChanged`.
- **Alternative/Exception Flow**: Reject acknowledgements outside the allowed clock-skew window.
- **Related**: `IOT-06`.
- **Evidence**: `functional_inventory.md` IOT-06.
- **Status**: Supported.

#### FR-122 — Execution Report Ingestion (Deployment/Production)
- **Description**: The system shall apply idempotent execution/deployment status reports, validate provenance checksums against the accepted command, and publish order/inventory realtime events on commit.
- **Actor**: Edge runtime.
- **Trigger**: `POST /api/v1/iot/execution-endpoints/{endpointId}/commands/{commandId}/reports` (HTTPS recovery fallback; primary transport is MQTT, see FR-141).
- **Preconditions**: Report's provenance checksum matches the accepted command.
- **Main Flow**: 1) Validate provenance. 2) Apply idempotently by `(SourceEventId, SequenceNumber)`. 3) Publish realtime events.
- **Alternative/Exception Flow**: Return `Duplicate` for already-applied reports rather than reprocessing.
- **Related**: `IOT-07`.
- **Evidence**: `functional_inventory.md` IOT-07.
- **Status**: Supported.

#### FR-123 — Production Event Checkpoint Query
- **Description**: The system shall let an execution endpoint query its own contiguous production-event checkpoint as a reconnect resume cursor.
- **Actor**: Edge runtime.
- **Trigger**: `GET /api/v1/iot/execution-endpoints/{endpointId}/production-sync/checkpoint?sourceExecutorId=`.
- **Preconditions**: Endpoint authenticated.
- **Main Flow**: 1) Return `LastContiguousSequenceNumber`/`LastContiguousEventId`/`UpdatedAt`.
- **Alternative/Exception Flow**: None material.
- **Related**: `IOT-08`.
- **Evidence**: `functional_inventory.md` IOT-08.
- **Status**: Supported.

#### FR-124 — Production Event Batch Ingestion and Edge State-Summary Sync
- **Description**: The system shall durably ingest sequenced production events per `(originNodeId, eventId)` idempotency, advancing the checkpoint only over contiguous sequences, and separately apply a monotonic per-`(sourceExecutorId, summaryKind)` advisory state summary for reconnect visibility without creating durable production events.
- **Actor**: Edge runtime.
- **Trigger**: `POST .../production-sync/events`; `POST .../production-sync/state-summaries`.
- **Preconditions**: None beyond authentication.
- **Main Flow**: 1) Ingest events, advancing checkpoint contiguously. 2) Apply state-summary rows keyed by `(sourceExecutorId, summaryKind)`, higher `StateRevision` only.
- **Alternative/Exception Flow**: Per-item accepted/duplicate/rejected/stale result, HTTP 200/207.
- **Related**: `IOT-09`.
- **Evidence**: `functional_inventory.md` IOT-09.
- **Status**: Supported.

#### FR-125 — MQTT Command-Available Wake-Up Publish
- **Description**: The system shall best-effort publish a wake-up notification after committing a durable edge command, retrying transient failures without blocking or reverting the command.
- **Actor**: System (Cloud, after command commit).
- **Trigger**: Commit of a new `EdgeCommand`.
- **Preconditions**: Command durably committed.
- **Main Flow**: 1) Publish to `icebot/execution-endpoints/{endpointId}/commands/available` (QoS1, not retained).
- **Alternative/Exception Flow**: Publish failure is logged only; it does not roll back the already-committed command.
- **Related**: `MQTT-01`.
- **Evidence**: `functional_inventory.md` MQTT-01.
- **Status**: Supported.

#### FR-126 — MQTT Edge Uplink Consumption
- **Description**: The system shall consume typed edge uplink messages over MQTT (heartbeat/telemetry/readiness/execution-report/production-events/state-summaries) and dispatch each to the same Application command/query handler used by the equivalent HTTPS endpoint, publishing a correlated result.
- **Actor**: Edge runtime (via MQTT broker).
- **Trigger**: Subscription to `$share/{group}/icebot/execution-endpoints/+/uplink/{messageType}` for all 6 message types.
- **Preconditions**: Message not retained and within payload-size guard.
- **Main Flow**: 1) Parse topic/message type. 2) Dispatch to shared Application handler (identical to FR-027–FR-030, FR-122, FR-124). 3) Publish `EdgeUplinkResult` to the `.../uplink/results` topic.
- **Alternative/Exception Flow**: See FR-127 for rejection conditions.
- **Related**: `MQTT-02`.
- **Evidence**: `functional_inventory.md` MQTT-02.
- **Status**: Supported.

#### FR-127 — MQTT Topic Parsing, Retained-Message Rejection, and Payload-Size Guard
- **Description**: The system shall validate and parse uplink topics into endpoint id and message type, rejecting malformed or `results`-suffixed topics, and reject retained messages or oversized payloads before deserialization.
- **Actor**: System (internal to MQTT-02).
- **Trigger**: Every inbound uplink message.
- **Preconditions**: None.
- **Main Flow**: 1) Parse `(endpointId, messageType)`. 2) Reject retained (`Retain=true`) or oversized payloads with `EdgeUplinkResult{Succeeded=false}`.
- **Alternative/Exception Flow**: None material.
- **Related**: `MQTT-03`.
- **Evidence**: `functional_inventory.md` MQTT-03.
- **Status**: Supported.

### 4.14 Realtime and Cross-Cutting Reads

#### FR-128 — SignalR Channel Join (Orders, Operations, Dashboard)
- **Description**: The system shall let an authorized client join a per-order, per-kiosk, or scoped dashboard SignalR group after verifying scoped access.
- **Actor**: Management/Customer/Staff UI client.
- **Trigger**: `JoinOrder`/`LeaveOrder` on `/hubs/orders`; `JoinKiosk`/`LeaveKiosk` on `/hubs/operations`; `JoinDashboard` on `/hubs/management-dashboard`.
- **Preconditions**: Caller has scoped access to the target order/kiosk/dashboard scope.
- **Main Flow**: 1) Verify access. 2) Add/remove connection to/from the group.
- **Alternative/Exception Flow**: Throw `HubException` if unauthorized.
- **Related**: `SIG-01`, `SIG-02`, `SIG-03`.
- **Evidence**: `functional_inventory.md` SIG-01, SIG-02, SIG-03.
- **Status**: Supported.

Note: the realtime push events themselves (`SIG-04` order/payment, `SIG-05` kiosk/operations, `SIG-06` dashboard invalidation) are already specified per-domain at FR-067 (orders), FR-078 (payments), FR-087 (operations), and FR-130 (dashboard) — not repeated here to avoid duplication.

#### FR-129 — GraphQL Server Wiring
- **Description**: The system shall serve a HotChocolate GraphQL endpoint restricted to authorized query resolvers that delegate to Application query handlers.
- **Actor**: System.
- **Trigger**: `/graphql` endpoint mount at startup.
- **Preconditions**: None.
- **Main Flow**: 1) Register query types and authorization. 2) Map endpoint in the request pipeline.
- **Alternative/Exception Flow**: None material.
- **Related**: `GQL-01`.
- **Evidence**: `functional_inventory.md` GQL-01.
- **Status**: Supported.

### 4.15 Sync

#### FR-130 — Automatic Order Execution Dispatch and Timeout Reconciliation
- **Description**: The system shall automatically dispatch a paid, ready-for-fulfillment order to exactly one ready/safe/capable execution endpoint as a durable `EdgeCommand`, and periodically detect commands/executions with no timely report or heartbeat, updating observation status without asserting a physical execution outcome.
- **Actor**: System job.
- **Trigger**: `OrderExecutionDispatchReconciliationJob`; `OrderExecutionTimeoutReconciliationJob` (both periodic).
- **Preconditions**: Order `ReadyForFulfillment` (dispatch); command/execution past a timeout threshold (reconciliation).
- **Main Flow**: 1) Dispatch creates `EdgeCommand(ExecuteOrder)` and publishes MQTT wake-up (FR-125). 2) Reconciliation transitions observation status (`Stale`/`Delayed`, `Unreachable`/`PendingRecovery`/`SupportRequired`) and pushes `OrderExecutionObservationChanged`.
- **Alternative/Exception Flow**: Reconciliation never asserts a physical outcome — only observation status.
- **Related**: `SYNC-01`, `SYNC-02`.
- **Evidence**: `functional_inventory.md` SYNC-01, SYNC-02.
- **Status**: Supported.

#### FR-131 — Execution Metrics Collection
- **Description**: The system shall periodically publish counts of stale/unreachable in-flight production executions as operational metrics.
- **Actor**: System job.
- **Trigger**: `ExecutionMetricsCollectionJob` (30-second timer).
- **Preconditions**: None.
- **Main Flow**: 1) Compute counts. 2) Set `IceBotEdgeMetrics` observed values.
- **Alternative/Exception Flow**: None material.
- **Related**: `SYNC-03`.
- **Evidence**: `functional_inventory.md` SYNC-03.
- **Status**: Supported.

#### FR-132 — Sync Dead-Letter Listing, Retry, Resolve, and Ignore
- **Description**: The system shall let authorized operators list and inspect failed sync events routed to the dead-letter queue (including prior retry attempts), manually retry a dead-lettered execution-report event exactly once at a time by replaying it through the same ingestion handler, and mark a dead letter Resolved or Ignored with an audit note/reason.
- **Actor**: Management UI client (operator).
- **Trigger**: `GET /api/v1/management/sync-dead-letters[/{id}]`; `POST .../{id}/retry|resolve|ignore`.
- **Preconditions**: Retry is scoped to `ExecutionReport.*` event types only.
- **Main Flow**: 1) List/inspect. 2) Retry replays via `IngestExecutionReportCommandHandler` (FR-122); success resolves the dead letter. 3) Resolve/ignore mark terminal status with audit note.
- **Alternative/Exception Flow**: Retry returns 422 for non-`ExecutionReport.*` event types — production-event/state-summary dead letters have no automated replay path and must be resolved/ignored manually instead.
- **Related**: `SYNC-04`, `SYNC-05`, `SYNC-06`, `SYNC-07`.
- **Evidence**: `functional_inventory.md` SYNC-04, SYNC-05, SYNC-06, SYNC-07.
- **Status**: Supported (SYNC-04, SYNC-06, SYNC-07); Partial (SYNC-05, retry scope limitation).

### 4.16 Dashboard

#### FR-133 — Management Dashboard Metrics Aggregation and Realtime Invalidation
- **Description**: The system shall compute a scope-filtered snapshot of organization, store, kiosk, order, inventory, and device-event counts for the management dashboard, and notify subscribed dashboard scopes to refetch whenever a state change affecting those metrics commits.
- **Actor**: Management UI client (query) / System (invalidation push).
- **Trigger**: GraphQL `dashboard`; commit of any tracked mutation (order/payment/kiosk/alert/maintenance/inventory).
- **Preconditions**: Caller's RBAC scope (org/store/kiosk ids) determines the returned snapshot.
- **Main Flow**: 1) Aggregate counts scoped to caller. 2) Emit `DashboardInvalidatedEvent` alongside the triggering domain event, pushed via SignalR (FR-128) to `dashboard:system`/`dashboard:organization:{id}`/`dashboard:store:{id}`.
- **Alternative/Exception Flow**: No REST equivalent exists for the dashboard query — GraphQL only.
- **Related**: `DASH-01`, `DASH-02`.
- **Evidence**: `functional_inventory.md` DASH-01, DASH-02.
- **Status**: Supported.

---

## 5. Non-Functional Requirements

#### NFR-001 — Idempotent State-Changing Operations
Write endpoints that can be safely retried (order placement, payment session creation, refund request, franchise onboarding start, configuration deployment/rollback, production package install/upgrade) shall accept an `Idempotency-Key` and return the original result on retry rather than duplicating side effects. Evidence: `functional_inventory.md` ORD-01, PAY-01, PAY-11, TEN-16, PC-06–PC-08, PP-05, PP-11. Status: Supported.

#### NFR-002 — Edge/Cloud Offline Tolerance
The system shall tolerate temporary Edge/Cloud disconnection by queuing durable commands (`EdgeCommand`), retrying delivery, and dead-lettering unresolvable sync events rather than requiring continuous connectivity. Evidence: `repo_truth_map.md` §2, §8; `functional_inventory.md` Sync section. Status: Supported.

#### NFR-003 — Automated Reconciliation of Stuck Workflows
Background jobs shall periodically scan for and act on missed order dispatch, stale kiosk connectivity, stuck configuration deployments/package upgrades, and stalled payment sessions — each job's exact action is the specific transition it performs (e.g., mark connectivity `Unreachable`, fail a deployment with a reason code, schedule a payment retry), not a general guarantee of recovery. `[Inferred]` Several of these paths deliberately terminate in a manual-intervention or support-required state rather than resolving automatically (e.g. `PAY-16`'s intervention notification, `SYNC-02`'s `SupportRequired` observation status) — the word "recover" should be read as "detect and attempt the coded remediation," not "guarantee resolution without an operator." Evidence: `functional_inventory.md` SYNC-01, SYNC-02, DEV-22, PC-11, PP-16, PAY-07, PAY-16. Status: Supported (job existence and the specific transition each performs); Inferred (that these transitions constitute "recovery" in every case).

#### NFR-004 — Restrictive Default Referential Integrity
Foreign keys shall default to `Restrict` delete behavior to prevent accidental cascading data loss. Several configuration classes additionally write an explicit `Cascade` setting for specific parent-owns-child relationships (e.g., technical contract → declared effects, authoring import → items, production package parent → child), but `database_inventory.md` §9 item 6 explicitly leaves open whether `IceBotDbContext.ConfigureEntityConventions`'s later global loop overwrites those explicit `Cascade` settings back to `Restrict` — this was not settled by static reading alone. Evidence: `database_inventory.md` §3, §9 item 6. Status: Supported (the global `Restrict` default). `[Unclear]` whether the explicit `Cascade` exceptions actually take effect at runtime — see §8.



#### NFR-005 — Consistent Monetary and Quantity Precision
All `decimal` fields (money, quantities) shall use `precision(18,4)` by a global EF Core convention, avoiding per-entity precision drift. Evidence: `database_inventory.md` §7. Status: Supported.

#### NFR-006 — Auditable, Append-Only Evidence Trails
Order and order-item status transitions, and production-incident status transitions, shall be recorded in dedicated append-only history tables (`OrderStatusHistory`, `OrderItemStatusHistory`, `ProductionIncidentHistory`), and order/order-item records shall carry immutable order-time snapshots (product/recipe/price) so historical evidence is not affected by later catalog changes. `[Inferred]` Alerts and Maintenance Tickets do not have an equivalent dedicated per-aggregate history table in the evidence — their lifecycle is tracked via status/timestamp fields on the aggregate itself, with `OperationLog` providing a separate, general-purpose evidence trail rather than a one-to-one audit table per aggregate; this is a narrower claim than "every named aggregate has an append-only history table." Evidence: `database_inventory.md` §2, §5. Status: Supported (Orders/OrderItem/ProductionIncident history tables; order-time snapshots); Inferred (that Alerts/Maintenance Tickets/Operations are covered by the same uniform pattern). One noted inconsistency: `OrderItemStatusHistory`/`ProductionIncidentHistory` lack the audit columns that `OrderStatusHistory` has — see §8.

#### NFR-007 — Scoped Role-Based Access Control
Management endpoints decorated with an authorization policy shall enforce a JWT-carried role plus matching organization/store/kiosk scope (from the same `AccountRole`) before allowing access, rejecting cross-scope composition. Evidence: `functional_inventory.md` IDN-28. Status: Supported for the specific endpoints cited in §4 (see FR-016). `[Open Question]` Whether every management endpoint, GraphQL resolver, and SignalR hub method enforces this without exception was not established by an exhaustive audit.

#### NFR-008 — Endpoint-Level Transport Authentication for Edge Traffic
IoT/Edge REST and MQTT traffic shall authenticate per execution-endpoint profile — mutual TLS with certificate-fingerprint pinning for Full Edge, or ECDSA P-256 signed requests with nonce deduplication for Low-Cost Controllers — with credentials provisioned/rotated/revoked explicitly and MQTT broker passwords never persisted (returned once at generation time only). Evidence: `repo_truth_map.md` §3, §8; `functional_inventory.md` DEV-10–DEV-15, MQTT-04. Status: Supported.

#### NFR-009 — Account-Enumeration-Resistant Password Recovery
The forgot-password flow shall always return a generic success response regardless of whether the account exists or is eligible, to prevent account enumeration. Evidence: `functional_inventory.md` IDN-06. Status: Supported.

#### NFR-010 — Brute-Force Login Protection
The system shall lock an account for 15 minutes after 5 cumulative failed local-login attempts. Evidence: `functional_inventory.md` IDN-01. Status: Supported.

#### NFR-011 — No Persisted Raw Secrets
Refresh tokens, password-reset tokens, and account invitations shall be stored only as hashes; MQTT broker passwords shall never be persisted in plaintext. Evidence: `database_inventory.md` §2 Identity; `functional_inventory.md` DEV-15. Status: Supported.

#### NFR-012 — Verified Payment Webhook Ingestion
Payment provider (PayOS) webhook payloads shall be signature-verified before being applied to any payment/order state. Evidence: `functional_inventory.md` PAY-03. Status: Supported.

#### NFR-013 — Bounded Runtime-Menu Cache Freshness
The kiosk runtime-menu projection shall be cacheable for a short, bounded interval (15 seconds) with ETag/`If-None-Match` support, so kiosks can poll cheaply without recomputation on every request. Evidence: `functional_inventory.md` SC-08. Status: Supported.

#### NFR-014 — Bounded-Batch Data Retention
Scheduled data-retention deletions (heartbeats, device events, operation logs, processed sync inbox rows, expired identity credentials, notification deliveries) shall run in bounded batches (`BatchSize=1000`, `MaxBatchesPerRun=20`) rather than a single unbounded delete, to limit lock/transaction duration. Evidence: `database_inventory.md` §7. Status: Supported.

#### NFR-015 — Modular Monolith Boundary Discipline
The system shall preserve bounded-context separation (no direct cross-context domain dependency outside documented, intentional references). Evidence: `repo_truth_map.md` §2, §4. Status: Supported (the boundary structure itself). `[Inferred rationale]` That this boundary discipline "allows independent evolution of business areas without requiring a distributed microservice topology" is the architectural rationale documented in `ARCHITECTURE.md`/`repo_truth_map.md`, not an outcome independently verified by observing actual independent-evolution events (e.g. a context being extracted or changed without touching others).

#### NFR-016 — Horizontally Shareable MQTT Uplink Consumption
MQTT uplink message consumption shall use shared-subscription topic groups (`$share/{group}/...`) so a message is delivered to only one member of the consumer group at the broker level. Evidence: `functional_inventory.md` MQTT-02. Status: Supported (the shared-subscription topic configuration). `[Inferred]` Broker-level load-sharing reduces duplicate *delivery* across consumer instances; it does not by itself prove "no duplicate processing" end-to-end — that additionally depends on the per-message idempotency/deduplication each handler performs (see FR-027–FR-030, FR-122, FR-124), which is a separate, already-cited mechanism.

#### NFR-017 — Indexed High-Volume/Time-Series Tables
High-volume, time-ordered tables (`KioskHeartbeats`, `DeviceEvents`, `OperationLogs`, `SyncEventInbox`, `SyncDeadLetters`) shall carry dedicated time-based indexes to support efficient range scans as data grows. Evidence: `database_inventory.md` §4. Status: Supported, with one known gap — `EdgeCommandDeliveryAttempts` lacks its documented `SentAt`-bearing time index (see §8).

#### NFR-018 — Layered Compile-Time Dependency Discipline
The codebase shall preserve the `WebAPI → Infrastructure → Application → Domain` dependency direction, with `Domain` free of outward dependencies, to keep business rules testable and independent of infrastructure concerns. Evidence: `repo_truth_map.md` §2. Status: Supported.

#### NFR-019 — Consistent Global Persistence Conventions
EF Core global conventions (string length defaults, JSON column mapping by `*Json` naming convention, GUID vs. identity key generation strategy per base entity type) shall apply uniformly to reduce per-entity configuration drift and reviewer surprise. Evidence: `database_inventory.md` §5, §7. Status: Supported.

#### NFR-020 — Separated Operational and Diagnostic Visibility
For the endpoints cited below, curated operational read APIs (no raw payload) are distinct from diagnostics-scoped reads (raw payload, retries, provenance), gated by a dedicated `operations.diagnostics`-class policy. Evidence: `functional_inventory.md` OPS-17, OPS-18, PAY-06, ORD-11. Status: Supported for these cited endpoints. `[Open Question]` Whether this curated/diagnostics separation is applied consistently and completely across *every* raw-payload or provenance-bearing surface in the system (not just the ones directly cited) was not established by an exhaustive audit.

#### NFR-021 — Periodic Operational Metrics Publication
The system shall periodically publish counts of stale/unreachable in-flight executions as operational metrics for external monitoring. Evidence: `functional_inventory.md` SYNC-03. Status: Supported.

#### NFR-022 — External Storage for Large Binary Artifacts
Robot artifact binaries (`.lua` files) shall be stored in an S3-compatible object store (MinIO), not the relational database. Evidence: `database_inventory.md` §7. Status: Supported (the storage split itself). `[Inferred rationale]` "Keeping the primary database lean" and "allowing independent scaling/backup of binary content" are the architectural rationale for this split, not independently measured/observed benefits (e.g. no backup-procedure or scaling-test evidence was found).

#### NFR-023 — Structural Tenant-Consistency Enforcement
For the specific relationships enumerated in `database_inventory.md` §3 (e.g. `DeviceEvent → Device` via `(DeviceId, KioskId)`, `EdgeCommand → KioskExecutionEndpoint` via `(TargetExecutionEndpointId, KioskId)`, `KioskConfigurationDeployment → ConfigurationRelease` via `(ConfigurationReleaseId, OrganizationId)`), composite foreign keys make cross-tenant row persistence structurally invalid for *those* relationships. This is an enumerated set of examples, not a claim that every cross-context or cross-tenant reference in the schema is covered by an equivalent composite-FK guard — relationships not listed in `database_inventory.md` §3 are not asserted to have this protection. Reads are not filtered by a blanket EF Core tenant query filter in any case (a design choice, not a gap, per `database_inventory.md` §6); tenant scoping for reads relies on application-layer handlers. Evidence: `database_inventory.md` §3, §6. Status: Supported for the enumerated relationships; `[Unclear]` whether an equivalent guarantee holds for cross-tenant references not covered by an enumerated composite FK.

#### NFR-024 — Distributed Job Coordination via Advisory Locks
Singleton/distributed background jobs (e.g., robot-artifact orphan cleanup) shall use PostgreSQL advisory locks to prevent duplicate concurrent runs across multiple service instances. Evidence: `database_inventory.md` §7. Status: Supported.

#### NFR-025 — No Native Database Partitioning (Current Limitation)
The system does not currently implement PostgreSQL native table partitioning for any high-volume table, despite `docs/data/DATA_MODELING_RULES.md` documenting a partition-key plan; current scaling relies on indexing (NFR-017) and bounded retention batching (NFR-014) alone. Evidence: `database_inventory.md` §4, §7. Status: Unclear whether/when partitioning is planned — flagged as `[Open Question]` (see §8).

---

## 6. Data Requirements

Full entity-level detail lives in `deliverables/00_repo_evidence/database_inventory.md`; this section summarizes it for SRS purposes.

### 6.1 Persistence Platform
PostgreSQL 17 via Npgsql/EF Core (`IceBotDbContext`), database `IceBotDB`, per current configuration (`[Assumption]` current deployment default, not necessarily a binding version requirement). `IceBotDbContext.cs` (`src/Infrastructure/Data/IceBotDbContext.cs:109-214`) exposes **98** `DbSet<T>` properties — verified by direct count against the source file, correcting `database_inventory.md`'s own stated "~130" (see §8). At least 99 tables have been created cumulatively across 5 migrations (`InitialCreate`, `CatchUpProductionPackageAndExecutionWorkflows`, `CompleteLocalOperationalWorkflows`, `CompleteLocalOperationalChanges`, `AddProductionIncidents`) — this is a sum of `CreateTable` calls across migration history, not an independently re-verified current-schema table count from the model snapshot; treat it as a lower bound. Evidence: `database_inventory.md` §1, §7; `src/Infrastructure/Data/IceBotDbContext.cs:109-214`.

### 6.2 Entity Groups (by bounded context)
Per `repo_truth_map.md` §4 and `database_inventory.md` §1, Production Configuration, Production Execution, and Production Packages are three distinct bounded-context ownership concepts (configuration-time routing/binding; Cloud-side execution/audit projections; franchise packaging), grouped together below only for brevity of listing — they remain separate contexts, not one merged context.

Tenants (Organization, Store, Kiosk, KioskOperationalStateTransition, FranchiseOnboarding); Identity (Account, AccountRole, AccountNotificationDevice, AccountInvitation, PasswordResetRequest, RefreshToken, Role, AccountStores join); Catalog (ProductCategory, Product, ProductVariant, OptionGroup, ProductOption, ProductOptionIngredientRequirement, Recipe, RecipeItem, Ingredient); Inventory (IngredientDispenserState, InventoryTopologyChangeRecord, InventoryTopologyRebindRecord, StockMovement); SalesCatalog (Menu, MenuItem, MenuItemProductOption); Orders (Order, OrderItem, OrderItemOption, OrderItemOptionIngredientRequirement, OrderStatusHistory, OrderItemStatusHistory, ProductionIncident, ProductionIncidentHistory); Payments (PaymentMethod, PaymentTransaction, PaymentCallback, Refund); Devices (DeviceType, DeviceModel, Device, DeviceEvent, KioskHeartbeat, KioskConnectivityProjection, KioskExecutionEndpoint, ExecutionEndpointCredentialBinding, ExecutionEndpointMqttCredential, ExecutionEndpointReadinessProjection, ExecutionEndpointCapabilityProjection, ExecutionEndpointRequestNonce, ExecutionEndpointSupportedRobotTarget); RobotConfiguration (RobotProgram, RobotProgramArtifact, RobotArtifact, RobotArtifactTemplate, RobotArtifactTechnicalContract, RobotArtifactDeclaredEffect, RobotArtifactOrderingConstraint, RobotAuthoringImport, RobotAuthoringImportItem); **Production Configuration** (ConfigurationRelease, ExecutionRoute, ExecutionRouteRobotBinding, KioskConfigurationDeployment, ControllerArtifactSetDeployment(+Item)); **Production Execution** (OrderExecutionRecord, ProductionExecutionRecord); **Production Packages** (the full ProductionPackage/Version/Definition/Installation/Materialization/Composition and Upgrade family); Operations (Alert, MaintenanceTicket, OperationLog, NotificationDelivery); Sync (SyncEventInbox, ProductionEventCheckpoint, EdgeStateSummary, SyncDeadLetter, SyncDeadLetterRetryAttempt, EdgeCommand, EdgeCommandDeliveryAttempt). Evidence: `database_inventory.md` §1.

### 6.3 Key Relationship Patterns
- **Global delete-behavior convention**: every FK defaults to `Restrict`, with a small explicit set of `Cascade` parent-owns-child pairs (see NFR-004).
- **Composite tenant-consistency FKs**: e.g. `DeviceEvent → Device` via `(DeviceId, KioskId)`, `EdgeCommand → KioskExecutionEndpoint` via `(TargetExecutionEndpointId, KioskId)`, `KioskConfigurationDeployment → ConfigurationRelease` via `(ConfigurationReleaseId, OrganizationId)` — enforce that cross-tenant rows cannot be persisted (see NFR-023).
- **True 1:1 relationships**: `KioskExecutionEndpoint ↔ ExecutionEndpointMqttCredential`, `↔ ExecutionEndpointReadinessProjection`; `Kiosk ↔ KioskConnectivityProjection`.
- **Self-referencing lineage FKs**: `Product.TemplateProductId`, `Recipe.TemplateRecipeId`, `RobotArtifactTechnicalContract.SourceContractId`, `RefreshToken.ReplacedByTokenId`.
- **Explicit many:many join**: `Account ↔ Store` via `AccountStores`, composite PK.
- Evidence: `database_inventory.md` §3.

### 6.4 Constraints and Indexes
- Soft-delete-aware uniqueness (unique only among non-deleted rows) for reusable business codes (`Organization.Code`, `Store.(OrganizationId,Code)`, `Account.UserName/Email`, `Product.(scope…,Code)`, `Device.(KioskId,Code)`/`SerialNumber`, etc.).
- Immutable evidence/retry keys (`PaymentTransaction.TransactionNumber`, `Order.OrderNumber`, `Refund.RefundNumber`) carry a unique index that is **not** filtered by soft-delete, i.e. they are unique across retained rows including soft-deleted ones — physical deletion or a future migration change could still free the value, so "unique forever" would overstate what the index itself guarantees.
- Business-invariant partial unique indexes, each scoped exactly to its own filter predicate (do not generalize the "active"/"default" condition across aggregates — each entity's predicate is distinct): at most one default `ProductOption` per group (filtered `IsDefault = TRUE AND DeletedAt IS NULL`); at most one default, non-retired `Recipe` per variant (filtered `IsDefault = TRUE AND Status <> 4 AND DeletedAt IS NULL`); at most one `Primary`-settlement `PaymentTransaction` per order (filtered `SettlementDisposition = 1`); at most one Pending/Installed `KioskConfigurationDeployment` per kiosk (filtered `Status IN (1,2)`); at most one active, non-terminal `ProductionPackageUpgrade` per installation (filtered `DeletedAt IS NULL AND Status IN (0,1,2,3)`); at most one active `IngredientDispenserState` container binding per device slot (filtered `IsActive = TRUE AND DeletedAt IS NULL`).
- Check constraints: `KioskExecutionEndpoint` profile/identity consistency; `ProductionPackageInstallation` kiosk-requires-store.
- Evidence: `database_inventory.md` §4.

### 6.5 JSON Field Roles
Any `string` property ending in `Json` is mapped to PostgreSQL `jsonb` by a blanket global convention (this mapping mechanism is directly observed in code). `[Inferred]` The four-role taxonomy below — (1) **source-of-truth configuration** (mutable pre-publish, versioned — e.g. `Kiosk.SettingsJson`, `RobotProgram.ProgramManifestJson`); (2) **immutable order/execution-time snapshot** (e.g. `OrderItem.RecipeSnapshotJson`, `PaymentTransaction.RawRequestJson/RawResponseJson`); (3) **append-only external evidence/debug payload** (e.g. `DeviceEvent.PayloadJson`, `SyncEventInbox.PayloadJson`); (4) **metadata / non-critical extension** (e.g. `Organization.MetadataJson`, `Product.MetadataJson`) — is an interpretive categorization cross-checked against `docs/data/JSON_FIELD_RULES.md` and field-naming conventions, not a role tag stated verbatim on each field in code. Evidence: `database_inventory.md` §5.

### 6.6 Multi-Tenancy Data Model
Tenant root is `Organization → Store → Kiosk`, both `Store.OrganizationId` and `Kiosk.OrganizationId`/`Kiosk.StoreId` non-nullable. The shared `TenantScopeType` enum's resolution order `Device > Kiosk > Store > Organization > Global` is not uniformly valid for every entity that uses it: `RobotProgram` explicitly rejects `ScopeType.Global` at creation (`ValidateScope()`) despite the enum defining that value, so each entity's actually-allowed scope subset must be checked individually rather than assuming the full five-value range applies everywhere — see BR-02 and §8. Entities implementing the full override hierarchy (`Product`, `Recipe`, `Menu`, `RobotProgram`) carry a nullable Org/Store/Kiosk[/Device] tuple plus a `ScopeType` and (for Product/Recipe) a `Template*Id` lineage field. Some entities carry only a required `OrganizationId` (no override hierarchy — e.g. `ConfigurationRelease`, `RobotArtifact`). Others derive tenant ownership only by joining through a scoped owner with no duplicated `OrganizationId` column (e.g. `Alert`, `OperationLog`, `SyncEventInbox`). There is no blanket EF Core tenant query filter; scoping is enforced in application-layer handlers plus the composite-FK consistency pairs in §6.3 (which cover only the specific relationships enumerated there — see NFR-023). Evidence: `database_inventory.md` §6.

### 6.7 Physical Database Notes
PostgreSQL 17, Npgsql provider with `EnableRetryOnFailure`. Soft delete is a global query filter except for 12 principal types with required non-deleted dependents (`Account`, `Organization`, `Store`, `Kiosk`, `Device`, `Product`, `Ingredient`, `IngredientDispenserState`, `Order`, `PaymentTransaction`, `ConfigurationRelease`, `KioskExecutionEndpoint`), for which the codebase provides an explicit `WhereNotDeleted()` extension method that calling code is expected to use. `[Inferred]` The existence of this exception list and helper method is directly observed in code; whether every query against these 12 types actually calls `WhereNotDeleted()` where needed (i.e. full compliance with the convention) was not verified — this is a developer-responsibility convention, not an enforced/audited guarantee. Data retention defaults: `HeartbeatDays=30`, `DeviceEventDays=90`, `OperationLogDays=90`, `ProcessedSyncInboxDays=180`, `ExpiredIdentityCredentialDays=30`, `NotificationDeliveryDays=90`, deleted in bounded batches. Robot artifact binaries live in MinIO, not PostgreSQL. No native table partitioning exists yet. Evidence: `database_inventory.md` §7.

---

## 7. Business Rules

The following cross-cutting rules recur across multiple functional requirements in §4 and are called out once here rather than repeated per FR.

- **BR-01 — Role assignment hierarchy**: A caller may only assign a role to another account if the caller's own role outranks or equals the target role in the hierarchy SystemAdmin > OrgAdmin > Manager, and the requested scope must be within the caller's own allowed scope. Evidence: `functional_inventory.md` IDN-23, IDN-24.
- **BR-02 — Tenant scope resolution order**: Where an entity supports scope override, resolution follows Device > Kiosk > Store > Organization > Global (most specific wins). `[Inferred]` This order is not uniformly valid for every scoped entity — e.g. `RobotProgram` rejects `Global` at creation despite the shared enum defining it — so each entity's legal scope subset should be confirmed individually rather than assumed from the enum alone (see §6.6, §8). Evidence: `database_inventory.md` §6.
- **BR-03 — Payment/execution decoupling**: Payment confirmation and robot execution are explicitly decoupled in time; a reconciliation worker repairs missed dispatch rather than requiring execution to happen synchronously with payment. Evidence: `repo_truth_map.md` §5 item 4.
- **BR-04 — MQTT command delivery is notification-only; uplink evidence has a dual path**: For Cloud→Edge command delivery, MQTT (`MQTT-01`) only publishes a best-effort wake-up notification — it never carries the command payload itself; Edge must always pull the actual command and send its acknowledgement over REST (`IOT-05`, `IOT-06`), there is no MQTT equivalent for command pull/ack. For Edge→Cloud evidence (heartbeat, telemetry, readiness, execution reports, production-sync events/state-summaries), both a REST endpoint and an MQTT uplink handler exist and dispatch to the same Application handler, so either transport reaches the same durable Cloud-database record. Evidence: `repo_truth_map.md` §8; `functional_inventory.md` MQTT-01, MQTT-02, IOT-05, IOT-06.
- **BR-05 — Soft-delete exceptions for principal types**: `Account`, `Organization`, `Store`, `Kiosk`, `Device`, `Product`, `Ingredient`, `IngredientDispenserState`, `Order`, `PaymentTransaction`, `ConfigurationRelease`, and `KioskExecutionEndpoint` are excluded from the automatic soft-delete query filter because they have required, non-soft-deleted evidence dependents; the codebase provides a `WhereNotDeleted()` extension for callers to use explicitly. `[Inferred]` This is a convention that creates a developer responsibility, not an enforced or audited guarantee — whether every query against these 12 types actually applies the filter where needed was not verified. Evidence: `database_inventory.md` §7.
- **BR-06 — Activation preflight for sellable items**: A Menu or MenuItem cannot be set Active unless its referenced product/variant/recipe/options pass a full preflight (currency match, active recipe for machine-produced variants, statically satisfiable option groups). Evidence: `functional_inventory.md` SC-02, SC-05.
- **BR-07 — Recipe lifecycle immutability**: Recipes follow a strict Draft→Published→Active→Retired lifecycle; ingredient items can only be replaced while Draft; recipes are retired, never deleted. Evidence: `functional_inventory.md` CAT-15–CAT-17.
- **BR-08 — Robot artifact publish gating**: A Draft robot artifact can only be published when it has a compatible Published technical contract and its object-storage checksum/size has been verified. Evidence: `functional_inventory.md` RC-04.
- **BR-09 — Configuration release publish gating**: A Configuration Release can only be published after route/binding validation and a passing inventory-readiness check (the same policy used at deployment preview/deploy time). Evidence: `functional_inventory.md` PC-03, PC-05, PC-10; INV-14.
- **BR-10 — Production package version immutability**: Once published, a `ProductionPackageVersion`'s manifest is immutable; installations/upgrades reference it by exact version rather than a mutable pointer. Evidence: `functional_inventory.md` PP-02, PP-05, PP-11.
- **BR-11 — Refund/incident mandatory reasons**: Refund-required flagging, refund rejection, maintenance-ticket cancellation, and several other state transitions require a non-empty audit reason. Evidence: `functional_inventory.md` ORD-05, PAY-13, OPS-15.
- **BR-12 — One active constraint per resource slot**: At most one default option per option group, one default non-retired recipe per variant, one Primary-settlement payment transaction per order, one Pending/Installed configuration deployment per kiosk, one active (non-terminal) upgrade per installation, and one active container binding per device slot — each enforced by its own partial unique index with its own distinct filter predicate (see §6.4; "active"/"default" is not the same predicate across these six cases), not just application logic. Evidence: `database_inventory.md` §4.
- **BR-13 — Idempotency-key deduplication**: Where an `Idempotency-Key` is accepted, repeating the same key with the same logical request must return the original result rather than creating a duplicate resource or side effect. Evidence: `functional_inventory.md` (see NFR-001 evidence list).
- **BR-14 — Kiosk operational-state guard during execution**: A kiosk cannot be transitioned to `Maintenance`/`Cleaning`/`Restocking` while an execution is Accepted/Running; several inventory operations (rebind, device retire/replace) are similarly blocked during an active execution. Evidence: `functional_inventory.md` TEN-15, DEV-08, INV-05.
- **BR-15 — Inspection-before-resolution**: A production incident's resolution cannot be selected until an inspection outcome has been recorded. Evidence: `functional_inventory.md` ORD-22, ORD-23.

---

## 8. Assumptions and Open Questions

### 8.1 Regarding this document's own inputs
- `[Open Question]` `deliverables/00_repo_evidence/evidence_review_final.md` was requested as a source for this SRS but does not exist in the repository at the time of writing. This document was produced from `repo_truth_map.md`, `functional_inventory.md`, `database_inventory.md`, and `project_introduction.md` only. If `evidence_review_final.md` is added later, this SRS should be revisited against it.

### 8.2 Carried from `repo_truth_map.md` §10
- `[Open Question]` Exact request/response DTO shapes per endpoint were not inspected; Swagger/OpenAPI or controller source would be authoritative if a future deliverable needs this.
- `[Open Question]` The full permission matrix in `docs/api/AUTHORIZATION_RULES.md` was only partially read (lines 1–154); remaining permission codes beyond `program.manage` are not enumerated in the evidence base.
- `[Open Question]` Database physical details beyond what `database_inventory.md` covers were not independently re-inspected for this SRS.
- `[Open Question]` `docs/flows/CHECKOUT_EXECUTION_FLOW.md` was read only through roughly line 120 in the original evidence pass; later sections (failure/refund detail beyond what's summarized) were not fully reviewed.
- `[Open Question]` Whether GraphQL exposes any mutations (vs. read-only, as currently documented) should be reconfirmed against `src/WebAPI/GraphQL/` if precise schema detail is needed.

### 8.3 Carried from `functional_inventory.md` "Notes, Ambiguities, and Open Questions"
- `[Inferred]` IDN-15b (temporary-password onboarding without invitation): the code path exists, but its surrounding lifecycle (forced password change, restricted first-login access) is stated in `docs/api/IDENTITY_ONBOARDING_RULES.md` as not part of the current contract — classified Partial (see FR-009).
- `[Inferred]` Several rows (`UpdateDeviceTypeCommandHandler`, `SetDeviceTypeStatusCommandHandler`, `UpdateDeviceModelCommandHandler`, some read query handlers, OptionGroup/ProductOption delete handlers, RC-19/PP-16/PC-12 internals) were confirmed via controller/job wiring and doc description rather than a full line-by-line read — carried into this SRS at the same confidence level as `functional_inventory.md` states.
- `[Open Question]` SYNC-05 (sync dead-letter retry) supports only `ExecutionReport.*` event types; there is no automated replay path yet for production-event/state-summary dead letters (FR-132).
- `[Open Question]` No REST or GraphQL surface was found for a plain "Dashboard" controller outside the GraphQL `dashboard` query (FR-133) — if a REST dashboard endpoint exists elsewhere it was not located.
- `[Open Question]` This SRS (like `functional_inventory.md`) does not map requirements to `tests/IceBot.UnitTests`/`tests/IceBot.IntegrationTests` coverage; a future requirements-traceability-matrix deliverable could use the FR-xxx / inventory-ID columns as its key.

### 8.4 Carried from `database_inventory.md` §9 (Discrepancies)
- `[Open Question]` `RobotProgram` has no `TemplateProgramId` field despite `docs/architecture/MULTI_TENANCY_RULES.md` describing one — either it lives in an unread partial-class file or the doc is stale.
- `[Open Question]` `ConfigurationRelease`'s actual unique index (`OrganizationId, ReleaseNumber`) differs from the doc's suggested broader composite; the additional scope fields live one level down on `ExecutionRoute`/`Recipe`/`RobotProgram` instead.
- `[Open Question]` `EdgeCommandDeliveryAttempts` is missing its documented `SentAt`-bearing time index (see NFR-017).
- `[Open Question]` `ProductionEventCheckpoints`/`EdgeStateSummaries` are structurally bounded upsert tables, not append-only logs, despite being grouped with high-volume tables in `docs/data/DATA_MODELING_RULES.md` — worth a reviewer's confirmation that the grouping is intentional.
- `[Open Question]` `ProductOption.TemplateProductOptionId` has no configured FK, unlike the equivalent `Product`/`Recipe` lineage fields — not confirmed whether intentional (soft reference) or an oversight.
- `[Open Question]` Whether the global `DeleteBehavior.Restrict` convention silently overrides explicitly-configured `Cascade` relationships was not settled by static code reading alone (see NFR-004).
- `[Open Question]` `OrderStatusHistory` (full `BusinessEntity`, audited) vs. `OrderItemStatusHistory`/`ProductionIncidentHistory` (bare `GuidEntity`, unaudited) is a real inconsistency between structurally-parallel tables (see NFR-006).
- `[Open Question]` `Device.MetadataJson`/`DeviceModel.MetadataJson` have no schema-version field, unlike `DeviceModel.CapabilitiesJson` — likely intentional per the metadata-field convention, but asymmetric within the same entity.
- `[Open Question]` `ExecutionRoute.RequiredCapabilitiesJson` has no dedicated schema-version column, possibly because its version is embedded in the JSON body itself rather than a sibling column.
- `[Open Question]` Two different configuration keys are used for the DB connection string depending on runtime vs. design-time code path (`CONNECTIONSTRING` vs. `ConnectionStrings:IceBot_DB`) — a real operational divergence worth awareness when debugging environment-specific connection issues.
- `[Open Question]` `ExecutionEndpointCredentialBinding.PublicKeyPem` has no `HasMaxLength` override, inheriting the global 500-character default — unusually short for a PEM-encoded public key; not confirmed whether intentional.

### 8.5 Carried from `project_introduction.md` §12
- `[Assumption]` The overall business motivation (why this product, target market sizing, competitive context) is not present in the evidence files, since those were derived from code/architecture docs rather than a business plan.
- `[Open Question]` Whether the two `Partial`-status features (IDN-15b, SYNC-05) are planned for completion within this project's remaining timeline, or accepted as permanent limitations, should be confirmed with the team/supervisor.
- `[Assumption]` No frontend/tablet/mobile client implementation exists in this repository; all UI-facing claims in this SRS (§3.1) are inferred from the API contract, not from observed frontend code.

### 8.6 Carried from team review (`deliverables/05_team_review/codex_review_project_intro_srs.md`)
- `[Open Question]` The requested path `deliverables/00_repo_evidence/evidence_review_final.md` still does not exist; the review used a differently-located file, `deliverables/05_review_checklists/evidence_review_final.md`. This path discrepancy should be corrected in the document set or explicitly explained in the final report.
- `[Open Question]` `functional_inventory.md`'s own Summary table states 265 rows; a direct count of `ID`-prefixed rows yields 260 (Operations is short 4 rows against its stated 26; Payments is short 1 row against its stated 17). This SRS uses 260 throughout and does not correct `functional_inventory.md` itself (out of scope per the rule against modifying `00_repo_evidence/`) — whether 260 or 265 is the intended authoritative count remains open.
- `[Open Question]` `database_inventory.md` states "~130 `DbSet<T>` properties"; a direct count against `src/Infrastructure/Data/IceBotDbContext.cs:109-214` finds 98. This SRS uses the verified 98 figure (§6.1) and does not correct `database_inventory.md` itself.
- `[Open Question]` This SRS consolidates 260 inventory rows into 133 FRs by narrative grouping (each FR's `Related`/`Evidence` fields now individually list every consolidated ID, per this revision). A formal, separately-maintained inventory-ID-to-FR traceability matrix (one row per `functional_inventory.md` ID, with its FR number and confidence/status) does not yet exist and would be a more rigorous artifact than in-line grouping — flagged as a follow-up deliverable, not produced here.
- `[Open Question]` No test-execution or `tests/IceBot.UnitTests`/`tests/IceBot.IntegrationTests` coverage evidence backs any `Supported` status in this document (see the Status legend at the top of this file); "Supported" should be read as "statically code-evidenced," and no FR/NFR should be read as implying a runtime-verified guarantee until test or execution evidence is linked.
- `[Open Question]` Whether scoped RBAC is enforced on every management endpoint, GraphQL resolver, and SignalR hub method (FR-016, NFR-007) was not established by an exhaustive authorization-coverage audit.
- `[Open Question]` Whether explicit `Cascade` delete-behavior configurations are actually preserved after `IceBotDbContext.ConfigureEntityConventions`'s global `Restrict` loop runs (NFR-004) was not settled by static reading alone.
- `[Open Question]` The following platform/operational concerns were raised by team review as materially relevant to a complete SRS but were not found addressed in the evidence base, and are not claimed as implemented or absent here: (a) identity/reference-data bootstrap seeding beyond the one `PaymentMethodCatalogHostedService` note already in `functional_inventory.md`'s Notes section; (b) robot-artifact object-storage startup validation (distinct from FR-101's orphan-cleanup job); (c) API versioning/deprecation policy and a structured error/problem-response envelope; (d) rate limiting, CORS policy, and request/body size limits on public, IoT, and artifact-upload interfaces; (e) backup/restore procedures and disaster-recovery targets (RPO/RTO); (f) whether deleted/soft-deleted records can be viewed or restored by any actor, and the audit-visibility rules for deleted data. Each should be either evidenced from the codebase or explicitly scoped out in the final report, rather than left silently unaddressed.
- `[Open Question]` Which background jobs (reconciliation, cleanup, retention, notification-delivery) are mandatory for correctness versus optional per deployment profile, and which are actually enabled in a given environment, was not established in the evidence base.

---

*End of baseline document. This file is intended to be reviewed and iterated on by the team before being adapted into the formal school/thesis SRS report structure.*

