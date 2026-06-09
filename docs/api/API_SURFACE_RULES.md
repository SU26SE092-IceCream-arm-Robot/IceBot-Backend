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
| IoT/edge | `/api/v1/iot/...` | Local edge backend/kiosk runtime | Kiosk/device credential, future mTLS/signing |
| Operations health/info | `/health...`, `/info` | Load balancer, deployment monitor, developer tooling | Public operational probe |

## API Lookup

| Area | Main routes | Read when asking about |
| --- | --- | --- |
| Authentication and password recovery | `/api/v1/authentication/*` | login, external login, Firebase Google login, refresh token, forgot password, reset password, accept invitation |
| Current account | `/api/v1/me`, `/api/v1/me/profile`, `/api/v1/me/password` | own profile, edit profile, change password while logged in |
| Account management | `/api/v1/management/accounts/*` | create internal account, invitation link generation, assign roles, disable account, set password |
| Organization management | `/api/v1/management/organizations/*` | create/update/activate/disable organizations, list and view organizations |
| Store management | `/api/v1/management/stores/*`, `/api/v1/management/organizations/*/stores` | create/update/activate/disable stores, list and view stores |
| Kiosk management | `/api/v1/management/kiosks/*`, `/api/v1/management/stores/*/kiosks` | create/update/set status of kiosks, list and view kiosks |
| Tenant scope lookup | `/api/v1/management/tenant-tree` | select valid organization/store/kiosk scopes for RBAC and management navigation |
| Product and menu management | `/api/v1/management/products`, `/api/v1/management/menus` | back-office catalog/menu/pricing operations |
| Tablet checkout | `/api/v1/kiosks/...`, `/api/v1/orders...` | runtime menu, place order, payment session, payment status |
| Edge integration | `/api/v1/iot/...` | command pull, command ack, events, heartbeat, configuration sync |
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
GET /api/v1/management/tenant-tree
GET /api/v1/management/roles
GET /api/v1/management/role-scope-options
GET /api/v1/management/permission-matrix
```

Rules:

- Use `/management/...`, not `/admin/...`.
- Access is controlled by authorization policies, not the route prefix.
- It is valid for multiple roles to share the same management endpoint when policy allows it.
- Management APIs can expose configuration/admin fields that tablet APIs should not expose.
- Organization update uses scoped authorization: `SystemAdmin` can update platform-managed fields; `OrgAdmin` can update only basic profile/contact fields for assigned organization scope.
- `tenant-tree` is a scope/navigation read model, not a dashboard overview. Do not add revenue, alert, inventory, or runtime metrics to it.

## Current Account APIs

Use `/me` only for the authenticated user's own account/profile/security surface.

Current examples:

```text
GET /api/v1/me
PUT /api/v1/me/profile
PUT /api/v1/me/password
```

Rules:

- Do not use `/me` for business resources such as orders, kiosks, reports, or maintenance tickets.
- Password recovery is not `/me` because the user may be logged out.

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
POST /api/v1/iot/kiosks/{kioskId}/events
POST /api/v1/iot/kiosks/{kioskId}/heartbeat
GET /api/v1/iot/kiosks/{kioskId}/configuration
```

Rules:

- Do not use internal account JWT as the long-term kiosk runtime credential.
- Keep IoT DTOs separate from EF entities.
- MQTT is notification only; Edge pulls command details through the API.

## Operations Health APIs

Health APIs are operational probes, not business APIs.

Current examples:

```text
GET /health
GET /health/ready
GET /info
```

Rules:

- `/health` is a lightweight liveness probe.
- `/health/ready` may check dependencies such as the database.
- `/info` exposes non-sensitive service/build metadata.
- Do not require user JWT for health probes.
- Do not expose secrets, connection strings, stack traces, or sensitive dependency details.

## Read Model API Boundaries

To ensure stability, performance, and security, read-model endpoints are strictly scoped to their intended UI or integration workflows. They must not be expanded to aggregate cross-cutting operational or reporting details.

### 1. Tenant Navigation & Scope Selection Boundaries
* **Endpoints:** 
  - `GET /api/v1/management/tenant-tree`
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

## Related Docs

- [Authorization Rules](AUTHORIZATION_RULES.md)
- [Identity Onboarding Rules](IDENTITY_ONBOARDING_RULES.md)
- [Naming Rules](../process/NAMING_RULES.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
- [System Flows](../flows/SYSTEM_FLOWS.md)
- [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
