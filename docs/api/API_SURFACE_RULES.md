# API Surface Rules

This document defines the backend API surface categories for IceBot. It is an ownership map, not a full endpoint contract. Detailed request/response contracts belong in Swagger, feature docs, or integration docs such as [IoT Contract](../iot/IOT_CONTRACT.md).

Controller attributes, generated OpenAPI, and the GraphQL schema are the exact
executable endpoint inventory. Documentation route lists are curated
ownership/usage indexes. Specialized flow documents may repeat a route only to
explain behavior owned by that flow.

## Search Keywords

`API surface`, `route prefix`, `tablet API`, `customer API`, `management API`, `current account`, `me API`, `authentication`, `auth`, `login`, `external login`, `Firebase Google login`, `refresh token`, `forgot password`, `reset password`, `change password`, `invitation`, `accept invitation`, `account onboarding`, `payment webhook`, `IoT API`, `edge API`, `health`, `info`

## Purpose

Use separate API surfaces for separate client workflows.

Do not reuse an endpoint only because it can return similar data. Tablet/customer, internal management, current account, provider webhook, and IoT/edge APIs have different security, stability, payload, and ownership needs.

Application services and stores may still reuse lower-level query/persistence logic.

## Surface Categories

| Surface | Route pattern | Primary clients | Auth direction |
| --- | --- | --- | --- |
| Tablet/customer runtime | `/api/v1/runtime/...` | Provisioned Flutter self-order tablet | Short-lived `ClientDeviceBearer` JWT; order-specific calls also require `Order-Access-Token` |
| Client-device session | `/api/v1/client-device-sessions` | Provisioned Flutter self-order tablet | Per-installation credential exchange over HTTPS; rate limited and not an account session |
| Internal management | `/api/v1/management/...` | Back-office UI for SystemAdmin, Manager, Staff, Technician, OrgAdmin depending on policy | JWT + scoped RBAC policy |
| Current account | `/api/v1/me...` | Logged-in internal user managing own profile/security | JWT |
| Authentication | `/api/v1/authentication...` | Internal login/password recovery clients | Mixed public/login and token flows |
| Payment provider webhook | `/api/v1/payments/.../webhook` | Payment provider callbacks | Provider signature verification |
| IoT/edge | `/api/v1/iot/...` | Local edge backend/kiosk runtime | Full Edge mTLS certificate pinning or low-cost ECDSA P-256 signed request over TLS |
| Operations health/info | `/health...`, `/info` | Load balancer, deployment monitor, developer tooling | Public operational probe |

## API Lookup

| Area | Main routes | Read when asking about |
| --- | --- | --- |
| Authentication and password recovery | `/api/v1/authentication/*` | login, external login, Firebase Google login, refresh token, forgot password, reset password, accept invitation |
| Public landing content and service registration | `/api/v1/content-pages/{slug}`, `/api/v1/service-registrations` | public legal/content pages and idempotent service-interest registration; registration does not create a tenant directly |
| Service-registration review | `/api/v1/management/service-registrations/*` | SystemAdmin-only review, rejection, atomic tenant/OrgAdmin provisioning, and retry after a provisioning failure |
| Content page management | `/api/v1/management/content-pages/*` | SystemAdmin-only draft and immutable publication of platform public long-form pages |
| Current account | `/api/v1/me`, `/api/v1/me/profile`, `/api/v1/me/password`, `/api/v1/me/access`, `/api/v1/me/sessions`, `/api/v1/me/notification-devices` | own profile, edit profile, change password, inspect/revoke active refresh sessions and current token access, and manage the caller's FCM registrations |
| Account management | `/api/v1/management/organizations/{organizationId}/accounts/*` | create internal account, invitation link generation, assign/update roles, effective access, disable account, set password |
| Organization management | `/api/v1/management/organizations/*` | create/update organizations, list/view lifecycle evidence, and SystemAdmin suspend/resume or deactivate/reactivate organization service |
| Platform organization sales reporting | `/api/v1/management/organizations/sales-summaries` | SystemAdmin-only, paged organization/currency aggregates for platform administration; no customer, order, provider-transaction, or exact-time detail |
| Store management | `/api/v1/management/stores/*`, `/api/v1/management/organizations/*/stores` | create/update/activate/disable stores, list and view stores |
| Kiosk management | `/api/v1/management/kiosks/*`, `/api/v1/management/stores/*/kiosks` | create/update/set status of kiosks, list and view kiosks |
| Kiosk menu-item operational availability | `/api/v1/management/kiosks/{kioskId}/menu-item-availability`, `/api/v1/management/kiosks/{kioskId}/menu-items/{menuItemId}/availability` | list effective kiosk menu items, then pause or resume one item's sale without changing shared menu authoring data |
| Device management | global index: `/api/v1/management/devices`; kiosk-owned operations: `/api/v1/management/kiosks/{kioskId}/devices/*` | create/update/set management status/retire devices, list and view devices |
| Device catalog | `/api/v1/management/device-types/*`, `/api/v1/management/device-models/*` | lookup device type/model IDs; SystemAdmin authors the global hardware catalog |
| Execution endpoint management | global index: `/api/v1/management/execution-endpoints`; kiosk-owned operations: `/api/v1/management/kiosks/{kioskId}/execution-endpoints/*` | create, provision, inspect, configure compatibility, disable/reactivate, rotate credentials, and retire Full Edge or low-cost execution endpoints |
| Account role assignment lookup | `/api/v1/management/accounts/assignable-role-options`, `/api/v1/management/role-scope-options`, GraphQL `tenantTree` | choose roles the current account-management actor may assign, then select valid organization/store/kiosk scope options |
| Global product templates | `/api/v1/management/product-templates/*` | SystemAdmin-only platform template authoring |
| Organization product and menu management | `/api/v1/management/organizations/{organizationId}/products/*`, `/api/v1/management/organizations/{organizationId}/menus/*` | tenant-scoped catalog/menu/pricing operations |
| Robot configuration management | `/api/v1/management/organizations/{organizationId}/robot-authoring-imports/*`, `/api/v1/management/organizations/{organizationId}/robot-artifacts`, `/api/v1/management/organizations/{organizationId}/robot-programs/*`, `/api/v1/management/organizations/{organizationId}/production-program-bindings/*`, `/api/v1/management/organizations/{organizationId}/configuration-releases/*`, `/api/v1/management/kiosks/{kioskId}/configuration-deployments/*` | upload a manifest bundle or raw Lua ZIP and automatically materialize Draft robot resources; review/publish program resources, explicitly bind Recipe to Program, then author/review a release and request/read/rollback deployment |
| Global robot artifact templates | `/api/v1/management/robot-artifact-templates/*`, `/api/v1/management/robot-artifact-template-contracts/*`, `/api/v1/management/organizations/{organizationId}/robot-artifacts/from-template` | manage reusable global Lua templates and their platform-owned technical contracts, then clone a Published template into an organization-owned Draft artifact |
| Back-office order operations | GraphQL `orders`, `order`, `orderStatusHistory`, `orderExecutionAttempts`; REST `/api/v1/management/orders/*`, `/api/v1/management/refunds/*` | scoped order reads in GraphQL; cancellation, redispatch, refund-required, Staff-confirmed cash settlement, manual refund commands, and restricted execution diagnostics in REST |
| Daily payment reconciliation | `/api/v1/management/payment-reconciliation/*` | read-only daily local-versus-provider evidence summary and discrepancy queue for OrgAdmin/Manager; not provider payout or bank-settlement reconciliation |
| Inventory management | `/api/v1/management/inventory/*`, `/api/v1/management/kiosks/{kioskId}/inventory/workspace`, `/api/v1/management/kiosks/{kioskId}/inventory/*`, `/api/v1/management/kiosks/{kioskId}/configuration-releases/{releaseId}/inventory-readiness` | workspace returns current inventory records, active refill tasks, inventory-level counts, and allowed actions. `InStock` is inventory evidence only, not a sellability decision; separate routes own topology, release readiness, stock movement history, and refill/estimate mutations |
| Operations telemetry | `/api/v1/management/kiosks/{kioskId}/operations/workspace`, `/api/v1/management/kiosks/{kioskId}/heartbeats`, `/api/v1/management/kiosks/{kioskId}/device-events`, `/api/v1/management/kiosks/{kioskId}/operation-logs` | workspace returns current safe operational context. `soleReadyEndpoint` is emitted only when exactly one endpoint is Ready; it is not a dispatch-routing decision. Paged routes retain kiosk connectivity history, device warnings/errors, and Edge local operation logs |
| Sync dead-letter operations | `/api/v1/management/sync-dead-letters` | SystemAdmin inspection, typed retry, retry audit, resolve, and ignore |
| Maintenance support | `/api/v1/management/maintenance-tickets/*` | manual operations/support tickets for kiosk/device/order/event issues; ticket-scoped assignee lookup returns only eligible active maintenance operators |
| Client-device management | `/api/v1/management/kiosks/{kioskId}/client-devices`, `/api/v1/management/client-devices/{clientDeviceId}/...` | provision, revoke, rotate, rebind, and replace a managed self-order tablet under scoped RBAC |
| Tablet checkout | `/api/v1/runtime/menu`, `/api/v1/runtime/orders...` | authenticated runtime menu, order, payment session, payment status, and cancellation |
| Edge integration | `/api/v1/iot/...` | command pull, command ack, execution reports, event replay, heartbeat, configuration sync |
| Operations probes | `/health`, `/health/ready`, `/info` | liveness, readiness, build/service info |

## Management Route Ownership

Management routes should expose the resource owner in the path when the owner is required for validation, scope checks, or safe mutation.

- Use global `/management/{resource}` routes for platform-wide catalogs or cross-scope management indexes. Examples: organizations, stores, kiosks, devices, accounts, roles, device types/models, product categories, ingredients, product templates, robot artifact templates, alerts, refunds, maintenance tickets, and sync dead letters. These routes must still apply scoped filtering for non-SystemAdmin users.
- Use `/management/organizations/{organizationId}/...` for tenant-owned authoring and configuration such as products, menus, robot artifacts, robot programs, configuration releases, product options, and recipes.
- Use `/management/stores/{storeId}/...` when the resource is created or listed under a store owner, such as kiosks.
- Use `/management/kiosks/{kioskId}/...` for physical kiosk ownership: devices, execution endpoints, telemetry reads, inventory topology, dispenser topology actions, release deployment, deployment reads, rollback, and inventory readiness.
- Use `/management/orders/{orderId}/...` for order-owned workflow commands and audit reads such as refunds, redispatch, status history, execution attempts, cancellation, and refund-required transitions.
- `POST /management/orders/{orderId}/cash-payments/{paymentTransactionId}/confirm` confirms physical cash already received by Staff. It requires `cash-payments.confirm`, checks the caller's actual order scope inside the command handler, and is idempotent after the same payment is settled. It is not a provider webhook or a general payment-method mutation.

Create and mutation routes should prefer the parent owner path. Global list/search routes are acceptable when they are management indexes and the handler/store applies explicit scoped filtering. Do not choose a global route only because the child id is globally unique; if the parent owner changes validation meaning or reduces operator mistakes, put the parent in the route.

## Management Route Naming

- Name collection routes after the owned resource. Use `POST` on the collection when one request creates one or more resources of that collection; transport cardinality does not belong in route names. For example, multipart upload uses `POST .../robot-artifacts`, not `/bulk`.
- Put a lifecycle action after the item or collection it changes: `/{id}/publish`, `/{id}/retire`, or `/publish` for an atomic selected set. Do not alternate between `publish-bulk` and `bulk-publish`.
- Name public workflow commands after their observable result. Use `resume` and `publish-resources`; avoid generic actions such as `apply` or `process`.
- Distinguish platform templates from tenant-owned resources in the noun itself when both are exposed. Platform contracts use `/robot-artifact-template-contracts`; tenant contracts use `/organizations/{organizationId}/robot-artifact-technical-contracts`.
- A global route without an owner is allowed for a read-only cross-scope index, such as `/configuration-deployments`. Item mutation and detail routes still include the physical or tenant owner.
- Do not retain legacy aliases before first production deployment. Controller attributes, generated OpenAPI, flow docs, and frontend operation catalogs must change together.

### Organization Operational Lifecycle

Organization service lifecycle is platform-owned and has three operational states:

```text
Active -> Suspended -> Active       temporary platform hold and resume
Active -> Inactive                  service end/offboarding
Suspended -> Inactive               end service while held
Inactive -> Active                  explicit reactivation after readiness confirmation
```

Use explicit SystemAdmin commands; do not use ambiguous `disable`/`activate`
aliases for Organizations:

```text
POST /api/v1/management/organizations/{organizationId}/suspend
POST /api/v1/management/organizations/{organizationId}/resume
POST /api/v1/management/organizations/{organizationId}/deactivate
POST /api/v1/management/organizations/{organizationId}/reactivate
GET  /api/v1/management/organizations/{organizationId}/status-history
```

Every transition requires `reason`, `expectedRevision`, and an optional
idempotency key. Suspension additionally requires a structured `reasonCode`.
Reactivation requires `readinessConfirmed: true`; it is not a resume alias.
Suspended and inactive Organization role scopes are removed before management
authorization is evaluated, including for access tokens issued before the state
transition. SystemAdmin's global scope remains available for inspection and
recovery. Edge evidence, provider webhooks, and reconciliation ingress use
their own authentication paths and remain outside this tenant-account gate.

## Tablet / Customer APIs

Tablet/customer APIs model the checkout and order-status workflow. They must not use `/management/...`.

Current examples:

```text
POST /api/v1/client-device-sessions
GET /api/v1/runtime/menu
POST /api/v1/runtime/orders
GET /api/v1/runtime/orders/{orderId}
POST /api/v1/runtime/orders/{orderId}/payment-sessions
GET /api/v1/runtime/orders/{orderId}/payment-status
POST /api/v1/runtime/orders/{orderId}/cancel
```

Rules:

- Keep payloads small and UX-oriented.
- Do not expose internal management fields or back-office-only metadata.
- Use idempotency for retried checkout/payment commands.
- `Idempotency-Key` is required for order placement, payment-session creation, and refund requests. The backend scopes it to the kiosk, order, or payment transaction; clients must not reuse one key for a different request body.
- Every runtime call requires the distinct `ClientDeviceBearer` scheme. The server resolves Kiosk, Store, Organization, credential/session versions, and device lifecycle from the current database row; the request cannot select those values.
- `POST /runtime/orders` returns an `orderAccessToken` bearer capability. Order status, payment-session creation, payment-status polling, and customer cancellation require that token in the `Order-Access-Token` header. The token is scoped to both one order and its source ClientDevice and expires after 24 hours.
- Online sales require `KioskStatus.Active`, `KioskOperationalState.Operational`, active parent tenant scope, and a current kiosk connectivity projection of `Online` or `Degraded`.
- `KioskStatus` is lifecycle state. `KioskOperationalState` controls whether an otherwise active kiosk accepts new work. Connectivity is a separate observed projection and never changes either state automatically.
- `PATCH /api/v1/management/stores/{storeId}/kiosks/{kioskId}/operational-state` requires `kiosks.operations.manage`, a typed state, and an audit reason. `Maintenance`, `Cleaning`, and `Restocking` are rejected while an execution is running; `EmergencyStopRequested` remains available to hold new work and request immediate safety intervention.
- `EmergencyStopRequested` is Cloud intent, not evidence that the robot physically stopped. Only a typed Edge safety projection may report `ExecutionSafetyState.EmergencyStopped`; V1 does not send a hardware stop command.
- Pausing a kiosk holds paid queued work. It does not cancel/refund orders or assert that an accepted/running execution failed. `ExecuteOrder` commands are not created or delivered until the kiosk returns to `Operational`; deployment and recovery commands remain deliverable.
- Offline-created order sync is not part of the current API. It requires a separate offline-session authority, snapshot, payment, quota, expiry, replay, and reconciliation contract before an ingest endpoint is added.
- Cloud sales catalog snapshots do not replace Local Edge runtime truth for inventory/device/robot availability.

## Internal Management APIs

The complete internal management REST and GraphQL route catalog is owned by [Management API Surface](MANAGEMENT_API_SURFACE.md). This file retains only cross-cutting API categories, ownership rules, and client-facing contracts.

## Current Account APIs

Use `/me` only for the authenticated user's own account/profile/security surface.

Current examples:

```text
GET /api/v1/me
GET /api/v1/me/access
PUT /api/v1/me/profile
PUT /api/v1/me/password
GET /api/v1/me/notification-devices
PUT /api/v1/me/notification-devices/{installationId}
DELETE /api/v1/me/notification-devices/{installationId}
```

Rules:

- Do not use `/me` for business resources such as orders, kiosks, reports, or maintenance tickets.
- Password recovery is not `/me` because the user may be logged out.
- `/me/access` reports the caller's current token roles, permission codes, effective scoped ids, and `permissionScopes`. `permissionCodes` answers whether the token carries a capability; `permissionScopes` identifies the exact role-assignment scopes that grant each capability. Clients must not infer either value from role names. This is token evidence, not a fresh database authorization recalculation.
- Each `permissionScopes` item contains `permissionCode`, `scopeRequired`, `isGlobal`, and scope tuples containing `organizationId`, `storeId`, and `kioskId`. Keep tuples intact when matching a selected resource; do not combine ids from separate tuples. An organization-only tuple applies within that organization and its descendants, a store tuple applies within that store and its kiosks, and a kiosk tuple applies only to that kiosk. `isGlobal` is true for System Admin access and permissions whose catalog definition does not require tenant scope.
- Route guards and navigation may use this evidence for UX, but every management API must still enforce authorization and resource tenancy independently.
- Notification-device routes are self-service FCM registration only. They never accept `AccountId`, expose a push token/hash, or grant trusted-session behavior. Registration is serialized by both account installation and token identity. Reassigning or invalidating a registration removes the stored raw provider token while retaining its hash as audit correlation. Delivery selects registrations only while their owning account is Active.

## Authentication And Password Recovery APIs

Search keywords: `authentication`, `auth`, `local login`, `username password login`, `Firebase Google login`, `external login`, `refresh token`, `revoke refresh token`, `forgot password`, `reset password`, `change password`, `accept invitation`, `invitation link`, `current account password`, `management accounts`.

Management owns the allowed authentication methods for an internal account. Google login resolves and validates the verified provider email against the configured `GoogleEmail`, then binds `GoogleSubjectId` on first successful login. It must not fall back to `Account.Email` or overwrite the configured Google email from token claims.

Current examples:

```text
POST /api/v1/authentication/login
POST /api/v1/authentication/external-login
POST /api/v1/authentication/refresh-token
POST /api/v1/authentication/revoke-refresh-token
POST /api/v1/authentication/forgot-password
POST /api/v1/authentication/reset-password
POST /api/v1/authentication/accept-invitation
```

Rules:

- Login and forgot/reset password endpoints can be public.
- Account management is organization-owned under `/management/organizations/{organizationId}/accounts`. The route organization is the tenant authority; every submitted account role must carry that same `OrganizationId`.
- Change password for a logged-in user stays under `/me/password`.
- Refresh rotation rechecks persisted `AccountStatus` inside the token transaction. A non-Active account has its remaining refresh sessions revoked and receives no replacement token.
- Account onboarding and invitation lifecycle rules live in [Identity Onboarding Rules](IDENTITY_ONBOARDING_RULES.md).

## Provider Webhook APIs

Provider webhook routes are provider-specific integration endpoints.

Rules:

- Verify provider signature/authenticity.
- Deduplicate provider events.
- Do not put webhooks under management or tablet surfaces.

## IoT / Edge APIs

IoT/edge APIs are for local edge backend and kiosk runtime integration.

Current direction:

```text
POST /api/v1/iot/execution-endpoints/{endpointId}/commands/pull
POST /api/v1/iot/execution-endpoints/{endpointId}/commands/{commandId}/ack
POST /api/v1/iot/execution-endpoints/{endpointId}/commands/{commandId}/reports
POST /api/v1/iot/execution-endpoints/{endpointId}/device-events
POST /api/v1/iot/execution-endpoints/{endpointId}/telemetry-events
POST /api/v1/iot/execution-endpoints/{endpointId}/production-sync/events
GET /api/v1/iot/execution-endpoints/{endpointId}/production-sync/checkpoint
POST /api/v1/iot/execution-endpoints/{endpointId}/production-sync/state-summaries
POST /api/v1/iot/execution-endpoints/{endpointId}/heartbeat
POST /api/v1/iot/execution-endpoints/{endpointId}/readiness
```

Rules:

- Do not use internal account JWT as the long-term kiosk runtime credential.
- IoT routes no longer accept plaintext `X-Execution-Credential`. Full Edge endpoints authenticate with a directly presented client certificate pinned by SHA-256 fingerprint. Low-cost endpoints authenticate each raw HTTP request with ECDSA NIST P-256, timestamp, and a database-deduplicated nonce over TLS.
- Execution endpoint reads are tenant-scoped and never return credential material. Full Edge provisioning accepts `ClientCertificateSha256Fingerprint`; low-cost provisioning accepts `EcdsaPublicKeyPem`. Provisioning assigns exactly one profile identity: `FullEdgeRuntimeId` or `ControllerId`; it does not require operator-declared robot compatibility.
- Cryptographic transport verification belongs to WebAPI. Application handlers retain endpoint/kiosk/status/credential-binding checks but do not receive HTTP certificates, signatures, or plaintext credentials.
- Heartbeat ingest derives trust from the authenticated execution endpoint, validates `originNodeId` against its bound profile identity, and deduplicates by `(kioskId, originNodeId, heartbeatSequence)`. Unique stale sequences remain history-only; only the newest `Online`/`Degraded` sequence advances `Kiosk.LastOnlineAt` using Cloud receive time or changes current connectivity.
- `KioskStatus` owns management lifecycle (`Provisioning`, `Active`, `Disabled`, `Retired`). `KioskOperationalState` independently owns sales/dispatch admission (`Operational`, `PausedByOperator`, `Maintenance`, `Cleaning`, `Restocking`, `EmergencyStopRequested`, `OutOfService`). `KioskConnectivityProjection` separately owns observed connectivity (`Unknown`, `Online`, `Degraded`, `Unreachable`). Heartbeats and timeout reconciliation never mutate lifecycle or operational state.
- Heartbeat ingestion and timeout reconciliation serialize by kiosk. `KioskStatusChanged` carries lifecycle fields for management transitions or connectivity fields for observed transitions, and is never emitted for duplicate heartbeat delivery or an unchanged projection.
- Readiness ingest is a typed complete snapshot per execution endpoint. `stateRevision` is monotonic and persistent across ordinary reboot for the same executor identity; a newer revision replaces readiness/activity/safety and the complete capability set. It does not mutate `KioskStatus`.
- Machine-produced menu/order readiness requires at least one Active endpoint whose latest projection is Ready and Safe, was received by Cloud within `EdgeTelemetryIngestion__ReadinessTimeoutSeconds`, and whose available capability set covers every route binding. Deployment preview and production-package workspace use the same freshness rule. Dispatch additionally requires the selected endpoint to be Idle.
- Execution route `RequiredCapabilitiesJson` is optional internal persistence and runtime-manifest data. For a `ProductionProgramBinding`, backend proposes codes from optional published declarations and snapshots them when the operator creates the binding. Cloud knows declarations, not what Lua actually requires. When every artifact declares the built-in `FAIRINO_LUA_V1` / `FR5` packaging profile and no code is declared, Backend adds `ROBOT_ARM` with `TargetProfileDefault` evidence. This is a packaging-profile default, not proof of Lua behavior, installed hardware, or runtime availability. Other missing declarations contribute no invented requirement. The management editor does not offer an arbitrary capability picker or author `minVersion`; existing internal/package contracts with required `minVersion` remain fail-closed when endpoint evidence cannot compare it.
- Device-event ingest accepts one `Warning`, `Error`, or `Critical` evidence record, verifies device/kiosk ownership, deduplicates globally by `eventId`, and publishes `DeviceEventCreated` only after a new row commits. Raw payload remains excluded from management reads.
- Newly accepted current `Error` or `Critical` device events also create one Open Alert atomically and publish `AlertChanged` after commit. Events older than `EdgeTelemetryIngestion__AlertAutomationMaxEventAgeMinutes` remain audit history but do not trigger alert/push automation. Warning events do not auto-create alerts.
- `PUT /api/v1/iot/execution-endpoints/{endpointId}/reported-devices` accepts an authenticated complete hardware snapshot with a monotonic `snapshotRevision`, source-device keys, optional mapped Device ids, and declared runtime/model metadata. It is separate from readiness: elapsed time alone does not invalidate unchanged hardware inventory. This metadata is operational evidence, not operator configuration or proof of Lua behavior. It does not block endpoint provisioning. A known mismatch with a release's declared runtime/model blocks deployment; absence of a report produces a warning and remains allowed for the FR5 MVP.
- Endpoint activation, credential rotation, disable/reactivate, and retirement are management operations. They do not install artifacts; release deployment remains a separate command flow.
- MQTT subscriber credentials have a separate endpoint-scoped lifecycle: `POST/PATCH/DELETE /management/kiosks/{kioskId}/execution-endpoints/{id}/mqtt-credential`. Provision and rotation return a generated password once; normal reads expose only username, status, and credential version. HTTPS transport credentials are never reused for MQTT.
- `POST /api/v1/iot/execution-endpoints/{endpointId}/commands/{commandId}/reports` is the current V1 execution/deployment report ingest endpoint. It records an immutable `SyncEventInbox` envelope; the same source event id with a different command or payload returns conflict.
- Order-level reports update `OrderExecutionRecord` and must agree with available job evidence before a final summary is accepted. Job-level reports require immutable `sourceProductionJobId`, `orderItemId`, `productionUnitNo`, and `productionUnitQuantity`; they update the matching `ProductionExecutionRecord`, derive effective unit counts, and advance the item/order lifecycle when evidence is decisive. A source job cannot be rebound, and unit ranges cannot overlap within one command. Stock evidence is job-scoped and commits with the unit projection. The ingestion coordinator keeps one database transaction while deployment, order/job projection, and stock persistence use separate aggregate ports.
- `POST /api/v1/iot/execution-endpoints/{endpointId}/telemetry-events` replays only typed Heartbeat, DeviceEvent, and LocalLog items with item-level atomicity and per-item results.
- `POST /api/v1/iot/execution-endpoints/{endpointId}/production-sync/events` owns durable ProductionEvent replay and contiguous sequence acknowledgement. Production history does not replace typed command reports or stock movements.
- Production events are ordered by monotonic `(originNodeId, sequenceNumber)`. Cloud may store a later event across a gap, but acknowledges only `ProductionEventCheckpoint.LastContiguousSequenceNumber`.
- Latest-state summaries use a separate `(sourceExecutorId, summaryKind, stateRevision)` channel. Cloud applies only newer revisions; summaries never advance the production-history checkpoint or prove that historical events were received.
- Successful telemetry items receive processed `SyncEventInbox` receipts after their typed destination commits. A ProductionEvent is itself stored in `SyncEventInbox` with its sequence and advances its checkpoint in the same transaction.
- Keep IoT DTOs separate from EF entities.
- After an `EdgeCommand` commits, MQTT publishes a best-effort endpoint-scoped `CommandAvailable` wake-up for `ExecuteOrder` and `DeployConfiguration`. MQTT is notification only; Edge pulls command details through the API and periodic polling remains authoritative.

## Operations Health APIs

Health APIs are operational probes, not business APIs.

Current examples:

```text
GET /health
GET /health/ready
GET /management/diagnostics/health
GET /info
```

Rules:

- `/health` is a lightweight public liveness probe and does not check database or provider connectivity.
- `/health/ready` is a public/internal-safe readiness probe. In V1 it checks PostgreSQL database connectivity only. SMTP, Firebase, and PayOS network connectivity do not block readiness in V1.
- Database failures return a generic `"Database unavailable"` reason. Raw connection strings, credentials, or exception details must not be exposed.
- `/management/diagnostics/health` is a CI/CD and dev/ops diagnostics probe. It checks PostgreSQL, migration status, and safe config presence for JWT, SMTP, Firebase, and PayOS.
- Realtime SMTP, Firebase, and PayOS pings are opt-in through `Diagnostics:EnableExternalPing=true`. They must not block `/health/ready`.
- Diagnostics responses must not expose secret values, connection strings, raw provider exceptions, SMTP passwords, PayOS checksum keys, or Firebase credentials.
- `Diagnostics:ApiKey` controls diagnostics access. In non-development environments, configure it and send `X-Diagnostics-Key`.
- `/info` exposes non-sensitive service/build metadata.
- Do not require user JWT for health probes.

## Read Model API Boundaries

To ensure stability, performance, and security, read-model endpoints are strictly scoped to their intended UI or integration workflows. They must not be expanded to aggregate cross-cutting operational or reporting details.

### 1. Tenant Navigation & Scope Selection Boundaries
* **Endpoints:** 
  - GraphQL `tenantTree`
  - `GET /api/v1/management/role-scope-options`
* **Purpose:** Administrative layout navigation and validation of scopes when creating/assigning user roles.
* **Includes:** Hierarchy structural identifiers (Organization -> Store -> Kiosk) and scope codes.
* **EXCLUDES:** Revenue metrics, active alerts, device health, inventory levels, or machine runtime logs.
* **Ownership:** Excluded metrics belong to dashboard or reporting-specific APIs.

### 2. Kiosk Sales Menu Boundaries
* **Endpoint:** `GET /api/v1/runtime/menu`
* **Purpose:** Rendering customer-facing catalog pricing and availability on the order tablet.
* **Includes:** Product name, variant codes, prices, discount figures, images, and recipe versions.
* **EXCLUDES:** Recipe preparation details (coordinates, Fairino robot points), manufacturing cost margin data, and live dispenser levels.
* **Ownership:** Deep robot configuration lives in IoT sync profiles, while cost metrics belong to product inventory reporting.

### 3. Customer Order Tracking Boundaries
* **Endpoints:**
  - `GET /api/v1/runtime/orders/{orderId}`
  - `GET /api/v1/runtime/orders/{orderId}/payment-status`
* **Purpose:** Real-time customer receipt and preparation status tracking.
* **Includes:** Quantity, billing totals, payment confirmation, preparation state, and tablet-friendly status projections (`CustomerStatus`, `CustomerStatusMessage`, `CanRetryPayment`, `RequiresStaffSupport`).
* **EXCLUDES:** Internal order-item, order-payment, payment-transaction, and order state-machine enums; raw payment provider callback bodies; device error codes; robot joint telemetry.
* **Ownership:** System error analytics are scoped to maintenance/operations portals, not client order details.

## Franchise Onboarding Workflow

Franchise onboarding is an organization-owned command workflow, not a global
setup endpoint. It creates/checkpoints Store and Kiosk provisioning and may
install a Production Package, but deliberately stops at `ReadyForActivation`.
Activation, publication, and deployment remain explicit existing commands.

```http
POST /api/v1/management/organizations/{organizationId}/franchise-onboardings
GET  /api/v1/management/organizations/{organizationId}/franchise-onboardings
GET  /api/v1/management/organizations/{organizationId}/franchise-onboardings/{onboardingId}
POST /api/v1/management/organizations/{organizationId}/franchise-onboardings/{onboardingId}/resume
POST /api/v1/management/organizations/{organizationId}/franchise-onboardings/{onboardingId}/cancel
```

Start requires `Idempotency-Key`. Reusing a key with the same payload resumes or
returns the same workflow; reusing it with a different payload returns conflict.
Checkpoints allow retry after a process interruption without recreating completed
resources. Cancel applies only to Pending/Failed workflows and does not delete
resources already provisioned. Running or ReadyForActivation workflows reject it.

The collection read supports `status`, `pageNumber`, and `pageSize`. It is
always scoped by the organization in the route.

Notification-delivery diagnostics are organization-owned operations reads.
They expose delivery state and bounded failure details, never the raw FCM
payload:

```http
GET /api/v1/management/organizations/{organizationId}/notification-deliveries
GET /api/v1/management/organizations/{organizationId}/notification-deliveries/{deliveryId}
POST /api/v1/management/organizations/{organizationId}/notification-deliveries/{deliveryId}/requeue
```

The execution-attempt and operation-log diagnostics GET endpoints require
`operations.diagnostics` and enforce the caller's effective organization/store/kiosk
scope in the database query. Payment diagnostics use the separate
`payments.diagnostics.view` policy and never return raw provider payloads.

The requeue command requires `notifications.manage`, a 3-500 character reason,
and a `PermanentFailure` delivery. It preserves `DeliveryKey`, resets the retry
budget, and appends a `NotificationDeliveryRequeued` operation-log record. It
does not replay or mutate the source Alert, deployment, ticket, or Order.

## API Result And Error Handling

Controller-facing Application handlers return `ApiResult<T>` or `PagedResult<T>`, and controllers should preserve the wrapper status code:

```csharp
return StatusCode(result.StatusCode, result);
```

Rules:

- `ApiResult<T>.StatusCode` must match the HTTP response status code.
- `InternalResult<T>` is not an API response contract and must not be returned directly by controllers.
- `AppException` subclasses must preserve their intended HTTP status through `GlobalExceptionMiddleware`.
- Middleware must not collapse `NotFoundException`, `ForbiddenException`, or `ConflictException` into `400 Bad Request`.
- Provider/system failures may include `SystemError` for diagnostics, but public responses must not expose secrets or sensitive config.

Recommended status use:

| Case | Status |
| --- | --- |
| Read/update success | `200 OK` |
| Created success | `201 Created` |
| Validation failure | `400 Bad Request` |
| Unauthorized | `401 Unauthorized` |
| Forbidden/scoped denied | `403 Forbidden` |
| Resource not found | `404 Not Found` |
| Business conflict/duplicate | `409 Conflict` |
| Provider/system failure | `500 Internal Server Error` unless a more specific application status is intentionally returned |

## Validation Strategy

Current v1 validation convention:

- Do not introduce FluentValidation yet.
- **Request DTO / DataAnnotations (Format & Syntax):** Use DataAnnotations for simple request DTO shape validation, such as required fields, string length, numeric range, and basic format.
- **Enum Inputs:** Send enum values as strings. JSON request bodies do not accept integer enum values.
- **Application Validators / Rule Helpers (Cross-Field / Request-Level):** Use static `RequestValidator` / rule helper classes for cross-field or request-level rules that do not need database access.
- **Handlers & Stores (Business constraints & Database-dependent):** Use handlers and stores for database-dependent validation, such as uniqueness, parent existence, active parent checks, and tenant-scope ownership.
- **Domain Methods (Invariants):** Use domain methods for entity invariants and state transitions.
- **Failure Returns & Exceptions:**
  - Handlers should return `ApiResult<T>.Fail(..., 400)` (or `409 Conflict` / `404 Not Found` as appropriate) for business rule / database-dependent validation failures, rather than throwing exceptions, to preserve clean control flow.
  - `ValidationException` is strictly reserved for automatic request DTO binding and DataAnnotations validation failures caught at the controller level before the handler is invoked.
  - Domain entities throw `DomainRuleException` if invariants are violated during processing.
- **Controller Cleanup:** Gradually remove repeated controller `EnsureValidModel()` helpers by relying on `[ApiController]` plus centralized `InvalidModelStateResponseFactory`.
- **Response Shape:** Keep the current validation response shape unless a separate API contract decision changes it.

Do not move business validation into controllers. Controllers should validate transport/request shape and then call Application handlers.

## GraphQL Management Reads

GraphQL is exposed at `/graphql` as an internal read/query surface for frontend UI aggregation.

- **Scope:** Read/query only. No mutations are implemented in this phase.
- **REST Surface:** REST remains the existing contract for commands, tablet actions, payment integrations, webhooks, and IoT edge communication.
- **Implementation:** GraphQL resolvers are thin adapters that delegate execution directly to Application CQRS query handlers. No database queries are performed directly inside the resolvers.
- **Code Organization:** Keep GraphQL feature/domain-first, not GraphQL-artifact-first. Although `/graphql` is hosted from WebAPI and frontend may see one large query surface, backend code should still be organized around the owning Application/domain features such as Tenants, Orders, Devices, Inventory, and Dashboard. GraphQL root/query classes are transport composition only, similar to controllers.
- **Wiring:** Register GraphQL query extensions in `src/WebAPI/GraphQL/GraphQLEndpointExtensions.cs`; do not add feature-specific GraphQL registrations directly to `Program.cs`.
- **Authorization:** Reuses JWT authentication and tenant-scoped RBAC rules. Endpoints require authentication via the standard `[Authorize]` attribute.

## SignalR Realtime Surface

SignalR is used for push-based real-time UI notifications. It operates as a delta and invalidation stream alongside REST/GraphQL.

SignalR is not the robot runtime bus. Cloud-to-Edge and Edge-to-Cloud runtime integration should use the IoT/MQTT/sync boundary documented in [System Overview Flow](../flows/SYSTEM_OVERVIEW_FLOW.md#integration-transport-boundaries) and [IoT Contract](../iot/IOT_CONTRACT.md).

### Routes and Hubs

| Hub | Route | Scope | Events |
| --- | --- | --- | --- |
| `OrderHub` | `/hubs/orders` | Order-specific updates | `OrderStatusChanged`, `OrderItemFulfillmentChanged`, `PaymentStatusChanged`, `OrderExecutionObservationChanged` |
| `OperationsHub` | `/hubs/operations` | Kiosk & telemetry status | `OrderItemFulfillmentChanged`, `KioskStatusChanged`, `KioskOperationalStateChanged`, `ExecutionReadinessChanged`, `DeviceEventCreated`, `AlertChanged`, `MaintenanceTicketChanged`, `InventoryChanged` |
| `ManagementDashboardHub` | `/hubs/management-dashboard` | Scoped dashboards (System, Org, Store) | `DashboardInvalidated` |

### Subscription Groups

Clients must join relevant groups to receive scoped events:
- `order:{orderId}`
- `kiosk:{kioskId}`
- `dashboard:system` (SystemAdmin only)
- `dashboard:organization:{organizationId}` (OrgAdmin/Manager/Staff/Technician with appropriate scope)
- `dashboard:store:{storeId}` (OrgAdmin/Manager/Staff/Technician with appropriate scope)

### Client Behavior Rules

1. **Initial Snapshot:** Call REST or GraphQL API on page load.
2. **Real-time Delta:** Apply updates immediately when a SignalR event is received.
3. **Re-sync / Fallback:** Re-fetch full REST/GraphQL payload on connection loss, reconnect, refresh, or version gap.

## Related Docs

- [Management API Surface](MANAGEMENT_API_SURFACE.md)
- [SignalR Realtime Contract](SIGNALR_REALTIME_CONTRACT.md)
- [SignalR Smoke Test Workflow](../operations/SIGNALR_SMOKE_TEST.md)
- [Authorization Rules](AUTHORIZATION_RULES.md)
- [Identity Onboarding Rules](IDENTITY_ONBOARDING_RULES.md)
- [Naming Rules](../process/NAMING_RULES.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
- [System Flows](../flows/SYSTEM_FLOWS.md)
- [Checkout Execution Flow](../flows/CHECKOUT_EXECUTION_FLOW.md)
- [Management Read Flow](../flows/MANAGEMENT_READ_FLOW.md)
- [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
Suspension and deactivation commit the Organization state and immutable
transition evidence before a hosted reconciliation job revokes refresh sessions
for accounts assigned to that Organization. The revocation job is retryable;
current HTTP scope enforcement does not wait for it.
