# API Surface Rules

This document defines the backend API surface categories for IceBot. It is an ownership map, not a full endpoint contract. Detailed request/response contracts belong in Swagger, feature docs, or integration docs such as [IoT Contract](../iot/IOT_CONTRACT.md).

## Search Keywords

`API surface`, `route prefix`, `tablet API`, `customer API`, `management API`, `current account`, `me API`, `authentication`, `auth`, `login`, `external login`, `Firebase Google login`, `refresh token`, `forgot password`, `reset password`, `change password`, `invitation`, `accept invitation`, `account onboarding`, `payment webhook`, `IoT API`, `edge API`, `health`, `info`

## Purpose

Use separate API surfaces for separate client workflows.

Do not reuse an endpoint only because it can return similar data. Tablet/customer, internal management, current account, provider webhook, and IoT/edge APIs have different security, stability, payload, and ownership needs.

Application services and stores may still reuse lower-level query/persistence logic.

## Surface Categories

| Surface | Route pattern | Primary clients | Auth direction |
| --- | --- | --- | --- |
| Tablet/customer | `/api/v1/kiosks/...`, `/api/v1/orders...` | Flutter tablet/customer checkout flow | Public v1 endpoints with idempotency and validation |
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
| Current account | `/api/v1/me`, `/api/v1/me/profile`, `/api/v1/me/password`, `/api/v1/me/access` | own profile, edit profile, change password, inspect current token access while logged in |
| Account management | `/api/v1/management/accounts/*` | create internal account, invitation link generation, assign/update roles, effective access, disable account, set password |
| Organization management | `/api/v1/management/organizations/*` | create/update/activate/disable organizations, list and view organizations |
| Store management | `/api/v1/management/stores/*`, `/api/v1/management/organizations/*/stores` | create/update/activate/disable stores, list and view stores |
| Kiosk management | `/api/v1/management/kiosks/*`, `/api/v1/management/stores/*/kiosks` | create/update/set status of kiosks, list and view kiosks |
| Device management | `/api/v1/management/devices/*`, `/api/v1/management/kiosks/*/devices` | create/update/set management status/retire devices, list and view devices |
| Execution endpoint management | `/api/v1/management/execution-endpoints/*`, `/api/v1/management/kiosks/{kioskId}/execution-endpoints` | create, provision, inspect, configure compatibility, disable/reactivate, rotate credentials, and retire Full Edge or low-cost execution endpoints |
| Tenant scope lookup | GraphQL `tenantTree`, `/api/v1/management/role-scope-options` | select valid organization/store/kiosk scopes for RBAC and management navigation |
| Product and menu management | `/api/v1/management/products`, `/api/v1/management/menus` | back-office catalog/menu/pricing operations |
| Robot configuration management | `/api/v1/management/organizations/{organizationId}/robot-artifacts`, `/api/v1/management/organizations/{organizationId}/robot-programs/*`, `/api/v1/management/organizations/{organizationId}/configuration-releases/*`, `/api/v1/management/kiosks/{kioskId}/configuration-deployments/{profile}` | upload immutable robot Lua artifacts, publish robot programs, publish immutable configuration releases, and request Full Edge or low-cost controller deployment |
| Back-office order operations | `/api/v1/management/orders`, `/api/v1/management/refunds` | internal order search, unpaid cancellation, refund-required marking, manual refund tracking |
| Inventory management | `/api/v1/management/inventory/*` | dispenser states, stock movement history, refill, estimate adjustment |
| Operations telemetry | `/api/v1/management/kiosks/{kioskId}/heartbeats`, `/api/v1/management/kiosks/{kioskId}/events` | kiosk connectivity history and device warnings/errors |
| Maintenance support | `/api/v1/management/maintenance-tickets/*` | manual operations/support tickets for kiosk/device/order/event issues |
| Tablet checkout | `/api/v1/kiosks/...`, `/api/v1/orders...` | runtime menu, place order, payment session, payment status |
| Edge integration | `/api/v1/iot/...` | command pull, command ack, execution reports, future event batch sync, heartbeat, configuration sync |
| Operations probes | `/health`, `/health/ready`, `/info` | liveness, readiness, build/service info |

## Tablet / Customer APIs

Tablet/customer APIs model the checkout and order-status workflow. They must not use `/management/...`.

Current examples:

```text
GET /api/v1/kiosks/{kioskId}/runtime-menu
POST /api/v1/orders
GET /api/v1/orders/{orderId}
POST /api/v1/orders/{orderId}/payment-sessions
GET /api/v1/orders/{orderId}/payment-status
POST /api/v1/orders/{orderId}/cancel
```

Rules:

- Keep payloads small and UX-oriented.
- Do not expose internal management fields or back-office-only metadata.
- Use idempotency for retried checkout/payment commands.
- Online sales require `KioskStatus.Active` and active parent tenant scope.
- `KioskStatus.Offline` does not allow new online sales through Cloud APIs.
- Offline-created orders may be synchronized later only when they were created under a valid offline sales session issued while the kiosk was active and offline sales was enabled.
- Cloud sales catalog snapshots do not replace Local Edge runtime truth for inventory/device/robot availability.

## Internal Management APIs

Management APIs are for internal operations, not only the `Manager` role.

Current examples:

```text
GET /api/v1/management/products
GET /api/v1/management/menus
GET /api/v1/management/accounts
GET /api/v1/management/accounts/{accountId}/effective-access
PUT /api/v1/management/accounts/{accountId}/roles
GET /api/v1/management/payment-methods
GET /api/v1/management/organizations
GET /api/v1/management/organizations/{organizationId}
POST /api/v1/management/organizations
PUT /api/v1/management/organizations/{organizationId}
PATCH /api/v1/management/organizations/{organizationId}/disable
PATCH /api/v1/management/organizations/{organizationId}/activate
GET /api/v1/management/stores
GET /api/v1/management/stores/{storeId}
POST /api/v1/management/organizations/{organizationId}/stores
PUT /api/v1/management/stores/{storeId}
PATCH /api/v1/management/stores/{storeId}/disable
PATCH /api/v1/management/stores/{storeId}/activate
GET /api/v1/management/kiosks
GET /api/v1/management/kiosks/{kioskId}
POST /api/v1/management/stores/{storeId}/kiosks
PUT /api/v1/management/kiosks/{kioskId}
PATCH /api/v1/management/kiosks/{kioskId}/status
GET /api/v1/management/devices
GET /api/v1/management/devices/{deviceId}
POST /api/v1/management/kiosks/{kioskId}/devices
PUT /api/v1/management/devices/{deviceId}
PATCH /api/v1/management/devices/{deviceId}/status
DELETE /api/v1/management/devices/{deviceId}
PATCH /api/v1/management/execution-endpoints/{endpointId}/credential
GET /api/v1/management/execution-endpoints
GET /api/v1/management/execution-endpoints/{endpointId}
POST /api/v1/management/kiosks/{kioskId}/execution-endpoints
PUT /api/v1/management/execution-endpoints/{endpointId}/supported-robot-targets
POST /api/v1/management/execution-endpoints/{endpointId}/provision
PATCH /api/v1/management/execution-endpoints/{endpointId}/disable
PATCH /api/v1/management/execution-endpoints/{endpointId}/reactivate
PATCH /api/v1/management/execution-endpoints/{endpointId}/retire
GET /api/v1/management/roles
GET /api/v1/management/role-scope-options
GET /api/v1/management/permission-matrix
PATCH /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/publish
PATCH /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/retire
PATCH /api/v1/management/organizations/{organizationId}/robot-programs/{programId}/retire
PATCH /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}/retire
PATCH /api/v1/management/organizations/{organizationId}/robot-programs/{programId}/publish
DELETE /api/v1/management/organizations/{organizationId}/robot-programs/{programId}
PATCH /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}/publish
DELETE /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}
POST /api/v1/management/kiosks/{kioskId}/configuration-deployments/full-edge
POST /api/v1/management/kiosks/{kioskId}/configuration-deployments/low-cost
POST /api/v1/management/organizations/{organizationId}/robot-programs
PUT /api/v1/management/organizations/{organizationId}/robot-programs/{programId}/artifacts
POST /api/v1/management/organizations/{organizationId}/robot-artifacts/bulk
PATCH /api/v1/management/organizations/{organizationId}/robot-artifacts/publish-bulk
GET /api/v1/management/organizations/{organizationId}/robot-artifacts
GET /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}
POST /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/review-url
DELETE /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}
GET /api/v1/management/organizations/{organizationId}/robot-programs
GET /api/v1/management/organizations/{organizationId}/robot-programs/{programId}
PUT /api/v1/management/organizations/{organizationId}/robot-programs/{programId}
GET /api/v1/management/organizations/{organizationId}/configuration-releases
GET /api/v1/management/organizations/{organizationId}/configuration-release-authoring-options
GET /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}
POST /api/v1/management/organizations/{organizationId}/configuration-releases
PUT /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}/routes
GET /api/v1/management/configuration-deployments
GET /api/v1/management/configuration-deployments/{deploymentId}
POST /api/v1/management/configuration-deployments/{deploymentId}/rollback
GET /api/v1/management/orders
GET /api/v1/management/orders/{orderId}
GET /api/v1/management/orders/{orderId}/status-history
PATCH /api/v1/management/orders/{orderId}/cancel
PATCH /api/v1/management/orders/{orderId}/refund-required
GET /api/v1/management/refunds
GET /api/v1/management/refunds/{refundId}
POST /api/v1/management/orders/{orderId}/refunds
PATCH /api/v1/management/refunds/{refundId}/mark-processed
PATCH /api/v1/management/refunds/{refundId}/reject
PATCH /api/v1/management/refunds/{refundId}/cancel
GET /api/v1/management/inventory/dispenser-states
GET /api/v1/management/inventory/stock-movements
POST /api/v1/management/inventory/dispenser-states/{id}/refill
POST /api/v1/management/inventory/dispenser-states/{id}/adjust-estimate
GET /api/v1/management/kiosks/{kioskId}/heartbeats
GET /api/v1/management/kiosks/{kioskId}/events
GET /api/v1/management/maintenance-tickets
GET /api/v1/management/maintenance-tickets/{ticketId}
POST /api/v1/management/maintenance-tickets
PUT /api/v1/management/maintenance-tickets/{ticketId}
PATCH /api/v1/management/maintenance-tickets/{ticketId}/assign
PATCH /api/v1/management/maintenance-tickets/{ticketId}/start
PATCH /api/v1/management/maintenance-tickets/{ticketId}/resolve
PATCH /api/v1/management/maintenance-tickets/{ticketId}/close
PATCH /api/v1/management/maintenance-tickets/{ticketId}/cancel
```

Device management rules:

- `DELETE /api/v1/management/devices/{deviceId}` is a soft retire operation. It sets `DeviceStatus.Retired` and soft-deletes the row; it does not physically delete the device record.
- `PATCH /api/v1/management/devices/{deviceId}/status` must not set `Retired`; use the retire endpoint instead.
- `Device.Status` is a management/operations state for configured hardware. Runtime connectivity and error evidence still come from heartbeat and device-event telemetry.

Rules:

- Use `/management/...`, not `/admin/...`.
- Access is controlled by authorization policies, not the route prefix.
- Tenant authorization must match role and resource scope on the same `UserRoleScope`; combining a privileged role from one scope with an unrelated scope from another assignment is forbidden.
- It is valid for multiple roles to share the same management endpoint when policy allows it.
- Management APIs can expose configuration/admin fields that tablet APIs should not expose.
- Organization update uses scoped authorization: `SystemAdmin` can update platform-managed fields; `OrgAdmin` can update only basic profile/contact fields for assigned organization scope.
- GraphQL `tenantTree` is a scope/navigation read model, not a dashboard overview. Do not add revenue, alert, inventory, or runtime metrics to it.
- Back-office order operations are manual support workflows. Paid orders should be marked `RefundRequired`; they are not cancelled directly.
- Order status history is a back-office audit read model. It exposes order status transitions and a small actor snapshot (`changedByAccountId`, `changedByName`, `changedByEmail`), not full account objects, raw payment callback bodies, or robot telemetry.
- Refund APIs in v1 track manual staff-handled compensation only. Supported methods are `FullMoneyRefund` and `Voucher`; both are full-order compensation flows, not partial refunds or line-item refunds.
- Full money refund sets `PaymentStatus = Refunded` only when staff confirms the money was actually refunded. Voucher compensation does not reverse payment status.
- Rejecting or cancelling a refund keeps `OrderStatus = RefundRequired`; staff may create another refund/compensation record later.
- `POST /api/v1/management/orders/{orderId}/refunds` should use `Idempotency-Key` for safe manual retries.
- Inventory management in v1 is reporting/operations only. It does not decide runtime menu sellability or robot execution availability.
- Operations telemetry APIs expose curated heartbeat/event fields only. Do not return raw `PayloadJson` by default.
- `DeviceEvent` is log/evidence, not actionable alert state. Long-term alert UI should use a separate Alert API/entity if needed.
- Maintenance ticket V1 is a manual operations/support workflow. Tickets are kiosk-scoped work items with optional evidence links to device, order, or device event. V1 does not include auto-generated tickets, alert engine, chat, reopen, or GraphQL maintenance aggregate.
- Execution endpoint credential rotation is a maintenance operation. It revokes the current credential binding and activates the new credential reference in one database save. Rotation preserves the endpoint's prior Active or Disabled state; Provisioning and Retired endpoints cannot rotate. Hot credential overlap is not part of V1.
- Robot artifact bulk upload is the only public upload contract. It accepts one to 50 multipart `.lua` files, stores file bytes in S3-compatible object storage, and stores immutable metadata in `RobotArtifact`.
- Bulk robot artifact upload accepts up to 50 files plus a JSON manifest that supplies per-file metadata. Uploaded artifacts remain unassigned Draft inventory and do not change any robot-program sequence. Request-shape errors reject the whole request; upload failures use item-level atomicity and return per-item results without rolling back successful items.
- Artifact upload retry identity is organization + normalized artifact code + SHA-256. An exact retry returns the existing artifact id and metadata as success; bulk results expose `uploadedCount`, `existingCount`, and per-item `wasExisting`.
- Artifact review URLs are organization-scoped, short-lived, and generated only after confirming the private object exists. They are not durable download contracts and must not be stored by clients.
- Draft discard hard-deletes metadata only when no `RobotProgramArtifact` references exist. Metadata deletion commits before best-effort object deletion; the orphan cleanup job removes residual objects after its grace period.
- Bulk artifact publish atomically transitions 1-100 unique selected Draft artifacts from one organization. Published items are idempotent no-ops; duplicate ids, missing, cross-organization, Disabled, or Retired selections reject the complete publish request.
- Robot artifact publish makes an uploaded artifact available for `RobotProgram` manifests. Robot program publish calculates `ProgramManifestJson` and `ProgramManifestChecksum` from ordered `RobotProgramArtifact` membership. Configuration release publish calculates immutable release manifest/checksum from execution routes and published robot program bindings.
- Retire lifecycle commands are idempotent and do not hard-delete artifact bytes, manifests, or deployment history. Artifact retirement is blocked by Draft programs, program retirement by Draft releases, and release retirement by Pending/Installed deployments. Published history can retain retired references for audit and rollback.
- Robot programs are created as organization-owned drafts. Store, kiosk, and device scope may narrow that ownership, but all scope ids must belong to the same tenant hierarchy. Global robot-program creation is not exposed because `RobotArtifact` is organization-owned.
- `PUT /management/organizations/{organizationId}/robot-programs/{programId}/artifacts` replaces the complete ordered membership while the program is Draft. `RunOrder` is explicit API data; backend must not derive execution order from an exported filename prefix.
- Every assigned artifact must belong to the program organization. Artifact parameters must be valid JSON. Publishing still requires all assigned artifacts to be Published.
- Artifact and program list endpoints are paged and tenant-scoped. Program lists return summaries without manifest JSON or artifact collections. Program detail includes ordered artifact metadata so management clients can edit/reorder a draft without issuing one request per artifact.
- `PUT /management/organizations/{organizationId}/robot-programs/{programId}` edits draft code, name, and description only. Program scope is immutable after creation; changing ownership scope requires a new draft program.
- `RobotProgramArtifact` is aggregate membership, not an independent management resource. Clients replace the ordered collection through the program endpoint instead of creating or deleting membership rows individually.
- Configuration releases are created as organization-owned drafts with backend-assigned release numbers. Route authoring replaces the complete Draft route/binding collection; `ExecutionRoute` and `ExecutionRouteRobotBinding` are aggregate children, not independent CRUD resources.
- Draft robot programs and Draft configuration releases can be hard-discarded through their `DELETE` endpoints. Published or referenced records are preserved; retirement remains the lifecycle operation for published history.
- `GET /management/organizations/{organizationId}/configuration-release-authoring-options` is a tenant-scoped UI lookup read model. It returns organization/global machine-produced ProductVariant options, Published/Active Recipes, and Published RobotPrograms with scope and display metadata. `productVariantId`, `search`, and `limit` are optional; `limit` applies independently to each result group. The command handler still revalidates every selected id when routes are submitted.
- Release route authoring requires Published/Active recipes to belong to their product variants and bindings to reference Published robot programs from the release organization or global scope. Kiosk/device compatibility remains a deployment-time validation.
- Release lists return summaries without manifest JSON or route/binding collections. Release detail remains the review surface for the complete authored graph.
- Configuration deployment reads unify Full Edge and Low-cost histories behind one tenant-scoped, paged management surface. Filters include organization, store, kiosk, release, profile, and status. Profile-specific provenance remains nullable rather than being discarded.
- Configuration rollback selects a previously Active deployment, then creates a new profile-matching deployment and durable command. It does not mutate or reactivate the historical deployment row. Retired releases are eligible only through this validated rollback path.
- The complete Fairino export-to-deployment sequence is owned by [Robot Lua Artifact Flow](../flows/ROBOT_LUA_ARTIFACT_FLOW.md); keep this document focused on route and client boundaries.
- Publish commands do not deploy to an execution endpoint.
- Full Edge deployment requests create a `KioskConfigurationDeployment` and a durable `DeployConfiguration` `EdgeCommand` in one database save. The command payload references the immutable release manifest/checksum and artifact descriptors; the edge runtime still validates checksum and reports install/activation later.
- Low-cost controller active-set deployment requests create a `ControllerArtifactSetDeployment` and a durable `DeployConfiguration` `EdgeCommand` in one database transaction. The request selects route/program/artifact/run-order items; backend-owned `LowCostControllerCapacity` configuration supplies the V1 capacity ceiling, so clients cannot claim larger hardware capacity.
- Deployment creation is serialized per kiosk/controller with a PostgreSQL transaction-scoped advisory lock. The pending-state check, attempt/version allocation, deployment insert, and durable command insert execute inside that boundary.
- Full Edge deploy, Low-cost deploy, and rollback requests require `Idempotency-Key`. The key is durable and unique per execution endpoint. An exact retry returns the existing deployment and command; reusing a key for a different release or Low-cost selection returns `409`.
- Deployment create/rollback responses may return their `IdempotencyKey` for immediate retry correlation. Deployment list/detail read models must not expose stored idempotency keys.
- Robot program, configuration release, and deployment read routes use `program.read`, `release.read`, and `deployment.read`. Authoring/publishing/deployment commands retain their narrower mutation policies.
- Deploy commands carry typed deployment correlation (`DeploymentId` and `DeploymentKind`). When a command expires before acceptance, background reconciliation marks both the command and its still-Pending deployment terminal with `CommandExpired`; management reads must not show that deployment as Pending indefinitely.
- Accepted deployment commands use a separate report deadline. A still-Pending deployment beyond that deadline becomes `Failed/ExecutionReportTimeout` through a conditional database update, preventing concurrent report ingestion or multiple backend jobs from blindly overwriting a newer state.
- Artifact bytes are not exposed through a public REST download endpoint. After execution-endpoint authentication, command pull enriches deployment artifact descriptors with short-lived object-storage read URLs. These URLs are not durable API identifiers and must not be stored as release state.

## Current Account APIs

Use `/me` only for the authenticated user's own account/profile/security surface.

Current examples:

```text
GET /api/v1/me
GET /api/v1/me/access
PUT /api/v1/me/profile
PUT /api/v1/me/password
```

Rules:

- Do not use `/me` for business resources such as orders, kiosks, reports, or maintenance tickets.
- Password recovery is not `/me` because the user may be logged out.
- `/me/access` reports the caller's current token roles and effective scoped ids. It is not a fresh database authorization recalculation.

## Authentication And Password Recovery APIs

Search keywords: `authentication`, `auth`, `local login`, `username password login`, `Firebase Google login`, `external login`, `refresh token`, `revoke refresh token`, `forgot password`, `reset password`, `change password`, `accept invitation`, `invitation link`, `current account password`, `management accounts`.

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
- Account management remains under `/management/accounts`.
- Change password for a logged-in user stays under `/me/password`.
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
POST /api/v1/iot/kiosks/{kioskId}/commands/pull
POST /api/v1/iot/kiosks/{kioskId}/commands/{commandId}/ack
POST /api/v1/iot/kiosks/{kioskId}/execution-reports
POST /api/v1/iot/kiosks/{kioskId}/events
POST /api/v1/iot/kiosks/{kioskId}/heartbeat
GET /api/v1/iot/kiosks/{kioskId}/configuration
```

Rules:

- Do not use internal account JWT as the long-term kiosk runtime credential.
- IoT routes no longer accept plaintext `X-Execution-Credential`. Full Edge endpoints authenticate with a directly presented client certificate pinned by SHA-256 fingerprint. Low-cost endpoints authenticate each raw HTTP request with ECDSA NIST P-256, timestamp, and a database-deduplicated nonce over TLS.
- Execution endpoint reads are tenant-scoped and never return credential material. Full Edge provisioning accepts `ClientCertificateSha256Fingerprint`; low-cost provisioning accepts `EcdsaPublicKeyPem`. Both require at least one supported robot target and assign exactly one profile identity: `FullEdgeRuntimeId` or `ControllerId`.
- Cryptographic transport verification belongs to WebAPI. Application handlers retain endpoint/kiosk/status/credential-binding checks but do not receive HTTP certificates, signatures, or plaintext credentials.
- Supported robot targets are a complete replacement contract and may change only while the endpoint is `Provisioning` or `Disabled`. A device-specific target must reference a device attached to the same kiosk.
- Endpoint activation, credential rotation, disable/reactivate, and retirement are management operations. They do not install artifacts; release deployment remains a separate command flow.
- `POST /api/v1/iot/kiosks/{kioskId}/execution-reports` is the current V1 execution/deployment report ingest endpoint. It records a `SyncEventInbox` receipt for deduplication and applies the report to deployment state or Cloud execution projections.
- `POST /api/v1/iot/kiosks/{kioskId}/events` remains the future broader batch event/sync surface and should not be used as the current command execution status endpoint.
- Keep IoT DTOs separate from EF entities.
- MQTT is notification only; Edge pulls command details through the API.

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
* **Ownership:** Excluded metrics must be served by future dashboard or reporting-specific APIs.

### 2. Kiosk Sales Menu Boundaries
* **Endpoint:** `GET /api/v1/kiosks/{kioskId}/runtime-menu`
* **Purpose:** Rendering customer-facing catalog pricing and availability on the order tablet.
* **Includes:** Product name, variant codes, prices, discount figures, images, and recipe versions.
* **EXCLUDES:** Recipe preparation details (coordinates, Fairino robot points), manufacturing cost margin data, and live dispenser levels.
* **Ownership:** Deep robot configuration lives in IoT sync profiles, while cost metrics belong to product inventory reporting.

### 3. Customer Order Tracking Boundaries
* **Endpoints:**
  - `GET /api/v1/orders/{orderId}`
  - `GET /api/v1/orders/{orderId}/payment-status`
* **Purpose:** Real-time customer receipt and preparation status tracking.
* **Includes:** Quantity, item status, billing totals, payment confirmation, preparation state, and tablet-friendly status projections (CustomerStatus, CustomerStatusMessage, CanRetryPayment, RequiresStaffSupport).
* **EXCLUDES:** Raw payment provider callback bodies, device error codes, robot joint telemetry.
* **Ownership:** System error analytics are scoped to maintenance/operations portals, not client order details.

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

## Read Model API Boundaries

To ensure stability, performance, and security, read-model endpoints are strictly scoped to their intended UI or integration workflows. They must not be expanded to aggregate cross-cutting operational or reporting details.

### 1. Tenant Navigation & Scope Selection Boundaries
* **Endpoints:** 
  - GraphQL `tenantTree`
  - `GET /api/v1/management/role-scope-options`
* **Purpose:** Administrative layout navigation and validation of scopes when creating/assigning user roles.
* **Includes:** Hierarchy structural identifiers (Organization -> Store -> Kiosk) and scope codes.
* **EXCLUDES:** Revenue metrics, active alerts, device health, inventory levels, or machine runtime logs.
* **Ownership:** Excluded metrics must be served by future dashboard or reporting-specific APIs.

### 2. Kiosk Sales Menu Boundaries
* **Endpoint:** `GET /api/v1/kiosks/{kioskId}/runtime-menu`
* **Purpose:** Rendering customer-facing catalog pricing and availability on the order tablet.
* **Includes:** Product name, variant codes, prices, discount figures, images, and recipe versions.
* **EXCLUDES:** Recipe preparation details (coordinates, Fairino robot points), manufacturing cost margin data, and live dispenser levels.
* **Ownership:** Deep robot configuration lives in IoT sync profiles, while cost metrics belong to product inventory reporting.

### 3. Customer Order Tracking Boundaries
* **Endpoints:**
  - `GET /api/v1/orders/{orderId}`
  - `GET /api/v1/orders/{orderId}/payment-status`
* **Purpose:** Real-time customer receipt and preparation status tracking.
* **Includes:** Quantity, item status, billing totals, payment confirmation, preparation state, and tablet-friendly status projections (CustomerStatus, CustomerStatusMessage, CanRetryPayment, RequiresStaffSupport).
* **EXCLUDES:** Raw payment provider callback bodies, device error codes, robot joint telemetry.
* **Ownership:** System error analytics are scoped to maintenance/operations portals, not client order details.

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
| `OrderHub` | `/hubs/orders` | Order-specific updates | `OrderStatusChanged`, `PaymentStatusChanged` |
| `OperationsHub` | `/hubs/operations` | Kiosk & telemetry status | `KioskStatusChanged`, `DeviceEventCreated`, `MaintenanceTicketChanged`, `InventoryChanged` |
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
