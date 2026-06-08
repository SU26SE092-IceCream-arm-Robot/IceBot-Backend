# API Surface Rules

This document defines the backend API surface categories for IceBot. It is an ownership map, not a full endpoint contract. Detailed request/response contracts belong in Swagger, feature docs, or integration docs such as [IoT Contract](IOT_CONTRACT.md).

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
- Cloud sales catalog snapshots do not replace Local Edge runtime truth for inventory/device/robot availability.

## Internal Management APIs

Management APIs are for internal operations, not only the `Manager` role.

Current examples:

```text
GET /api/v1/management/products
GET /api/v1/management/menus
GET /api/v1/management/accounts
GET /api/v1/management/payment-methods
```

Rules:

- Use `/management/...`, not `/admin/...`.
- Access is controlled by authorization policies, not the route prefix.
- It is valid for multiple roles to share the same management endpoint when policy allows it.
- Management APIs can expose configuration/admin fields that tablet APIs should not expose.

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

## Related Docs

- [Authorization Rules](AUTHORIZATION_RULES.md)
- [Identity Onboarding Rules](IDENTITY_ONBOARDING_RULES.md)
- [Naming Rules](NAMING_RULES.md)
- [IoT Contract](IOT_CONTRACT.md)
- [System Flows](SYSTEM_FLOWS.md)
- [Idempotency and Retry Rules](IDEMPOTENCY_RETRY_RULES.md)
