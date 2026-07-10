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
| Current account | `/api/v1/me`, `/api/v1/me/profile`, `/api/v1/me/password`, `/api/v1/me/access`, `/api/v1/me/notification-devices` | own profile, edit profile, change password, inspect current token access, and manage the caller's FCM registrations |
| Account management | `/api/v1/management/accounts/*` | create internal account, invitation link generation, assign/update roles, effective access, disable account, set password |
| Organization management | `/api/v1/management/organizations/*` | create/update/activate/disable organizations, list and view organizations |
| Store management | `/api/v1/management/stores/*`, `/api/v1/management/organizations/*/stores` | create/update/activate/disable stores, list and view stores |
| Kiosk management | `/api/v1/management/kiosks/*`, `/api/v1/management/stores/*/kiosks` | create/update/set status of kiosks, list and view kiosks |
| Device management | global index: `/api/v1/management/devices`; kiosk-owned operations: `/api/v1/management/kiosks/{kioskId}/devices/*` | create/update/set management status/retire devices, list and view devices |
| Device catalog | `/api/v1/management/device-types/*`, `/api/v1/management/device-models/*` | lookup device type/model IDs; SystemAdmin authors the global hardware catalog |
| Execution endpoint management | global index: `/api/v1/management/execution-endpoints`; kiosk-owned operations: `/api/v1/management/kiosks/{kioskId}/execution-endpoints/*` | create, provision, inspect, configure compatibility, disable/reactivate, rotate credentials, and retire Full Edge or low-cost execution endpoints |
| Tenant scope lookup | GraphQL `tenantTree`, `/api/v1/management/role-scope-options` | select valid organization/store/kiosk scopes for RBAC and management navigation |
| Global product templates | `/api/v1/management/product-templates/*` | SystemAdmin-only platform template authoring |
| Organization product and menu management | `/api/v1/management/organizations/{organizationId}/products/*`, `/api/v1/management/organizations/{organizationId}/menus/*` | tenant-scoped catalog/menu/pricing operations |
| Robot configuration management | `/api/v1/management/organizations/{organizationId}/robot-artifacts`, `/api/v1/management/organizations/{organizationId}/robot-programs/*`, `/api/v1/management/organizations/{organizationId}/configuration-releases/*`, `/api/v1/management/kiosks/{kioskId}/configuration-deployments/*` | upload immutable robot Lua artifacts, publish robot programs, publish immutable configuration releases, and request/read/rollback Full Edge or low-cost controller deployment |
| Global robot artifact templates | `/api/v1/management/robot-artifact-templates/*`, `/api/v1/management/organizations/{organizationId}/robot-artifacts/from-template` | manage reusable global Lua templates and clone a Published template into an organization-owned Draft artifact |
| Back-office order operations | GraphQL `orders`, `order`, `orderStatusHistory`, `orderExecutionAttempts`; REST `/api/v1/management/orders/*`, `/api/v1/management/execution-attempts/*`, `/api/v1/management/refunds/*` | scoped order reads in GraphQL; cancellation, redispatch, refund-required and manual refund commands in REST |
| Inventory management | `/api/v1/management/inventory/*`, `/api/v1/management/kiosks/{kioskId}/inventory/*`, `/api/v1/management/kiosks/{kioskId}/configuration-releases/{releaseId}/inventory-readiness` | dispenser topology, release readiness, state, stock movement history, refill, estimate adjustment |
| Operations telemetry | `/api/v1/management/kiosks/{kioskId}/heartbeats`, `/api/v1/management/kiosks/{kioskId}/device-events`, `/api/v1/management/kiosks/{kioskId}/operation-logs` | kiosk connectivity history, device warnings/errors, and Edge local operation logs |
| Sync dead-letter operations | `/api/v1/management/sync-dead-letters` | SystemAdmin inspection, typed retry, retry audit, resolve, and ignore |
| Maintenance support | `/api/v1/management/maintenance-tickets/*` | manual operations/support tickets for kiosk/device/order/event issues |
| Tablet checkout | `/api/v1/kiosks/...`, `/api/v1/orders...` | runtime menu, place order, payment session, payment status |
| Edge integration | `/api/v1/iot/...` | command pull, command ack, execution reports, event replay, heartbeat, configuration sync |
| Operations probes | `/health`, `/health/ready`, `/info` | liveness, readiness, build/service info |

## Management Route Ownership

Management routes should expose the resource owner in the path when the owner is required for validation, scope checks, or safe mutation.

- Use global `/management/{resource}` routes for platform-wide catalogs or cross-scope management indexes. Examples: organizations, stores, kiosks, devices, accounts, roles, device types/models, product categories, ingredients, product templates, robot artifact templates, alerts, refunds, maintenance tickets, and sync dead letters. These routes must still apply scoped filtering for non-SystemAdmin users.
- Use `/management/organizations/{organizationId}/...` for tenant-owned authoring and configuration such as products, menus, robot artifacts, robot programs, configuration releases, product options, and recipes.
- Use `/management/stores/{storeId}/...` when the resource is created or listed under a store owner, such as kiosks.
- Use `/management/kiosks/{kioskId}/...` for physical kiosk ownership: devices, execution endpoints, telemetry reads, inventory topology, dispenser topology actions, release deployment, deployment reads, rollback, and inventory readiness.
- Use `/management/orders/{orderId}/...` for order-owned workflow commands and audit reads such as refunds, redispatch, status history, execution attempts, cancellation, and refund-required transitions.

Create and mutation routes should prefer the parent owner path. Global list/search routes are acceptable when they are management indexes and the handler/store applies explicit scoped filtering. Do not choose a global route only because the child id is globally unique; if the parent owner changes validation meaning or reduces operator mistakes, put the parent in the route.

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
GET /api/v1/management/product-templates
POST/PUT/PATCH/DELETE /api/v1/management/product-templates/{productId}/option-groups/*
GET /api/v1/management/organizations/{organizationId}/products
POST /api/v1/management/organizations/{organizationId}/products/from-template
POST /api/v1/management/organizations/{organizationId}/products/{productId}/option-groups
PUT /api/v1/management/organizations/{organizationId}/products/{productId}/option-groups/{optionGroupId}
PATCH /api/v1/management/organizations/{organizationId}/products/{productId}/option-groups/{optionGroupId}/status
DELETE /api/v1/management/organizations/{organizationId}/products/{productId}/option-groups/{optionGroupId}
POST /api/v1/management/organizations/{organizationId}/products/{productId}/option-groups/{optionGroupId}/options
PUT /api/v1/management/organizations/{organizationId}/products/{productId}/option-groups/{optionGroupId}/options/{productOptionId}
PATCH /api/v1/management/organizations/{organizationId}/products/{productId}/option-groups/{optionGroupId}/options/{productOptionId}/availability
DELETE /api/v1/management/organizations/{organizationId}/products/{productId}/option-groups/{optionGroupId}/options/{productOptionId}
GET /api/v1/management/organizations/{organizationId}/menus
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
GET /api/v1/management/kiosks/{kioskId}/devices/{deviceId}
POST /api/v1/management/kiosks/{kioskId}/devices
PUT /api/v1/management/kiosks/{kioskId}/devices/{deviceId}
PATCH /api/v1/management/kiosks/{kioskId}/devices/{deviceId}/status
DELETE /api/v1/management/kiosks/{kioskId}/devices/{deviceId}
PATCH /api/v1/management/kiosks/{kioskId}/execution-endpoints/{endpointId}/credential
POST /api/v1/management/kiosks/{kioskId}/execution-endpoints/{endpointId}/mqtt-credential
PATCH /api/v1/management/kiosks/{kioskId}/execution-endpoints/{endpointId}/mqtt-credential
DELETE /api/v1/management/kiosks/{kioskId}/execution-endpoints/{endpointId}/mqtt-credential
GET /api/v1/management/execution-endpoints
GET /api/v1/management/kiosks/{kioskId}/execution-endpoints/{endpointId}
POST /api/v1/management/kiosks/{kioskId}/execution-endpoints
PUT /api/v1/management/kiosks/{kioskId}/execution-endpoints/{endpointId}/supported-robot-targets
POST /api/v1/management/kiosks/{kioskId}/execution-endpoints/{endpointId}/provision
PATCH /api/v1/management/kiosks/{kioskId}/execution-endpoints/{endpointId}/disable
PATCH /api/v1/management/kiosks/{kioskId}/execution-endpoints/{endpointId}/reactivate
PATCH /api/v1/management/kiosks/{kioskId}/execution-endpoints/{endpointId}/retire
GET /api/v1/management/roles
GET /api/v1/management/role-scope-options
GET /api/v1/management/permission-matrix
GET /api/v1/management/ingredients
GET /api/v1/management/ingredients/{ingredientId}
POST /api/v1/management/ingredients
PUT /api/v1/management/ingredients/{ingredientId}
PATCH /api/v1/management/ingredients/{ingredientId}/status
DELETE /api/v1/management/ingredients/{ingredientId}
GET /api/v1/management/organizations/{organizationId}/products/{productId}/variants/{variantId}/recipes
GET /api/v1/management/organizations/{organizationId}/products/{productId}/variants/{variantId}/recipes/{recipeId}
POST /api/v1/management/organizations/{organizationId}/products/{productId}/variants/{variantId}/recipes
PUT /api/v1/management/organizations/{organizationId}/products/{productId}/variants/{variantId}/recipes/{recipeId}
PUT /api/v1/management/organizations/{organizationId}/products/{productId}/variants/{variantId}/recipes/{recipeId}/items
PATCH /api/v1/management/organizations/{organizationId}/products/{productId}/variants/{variantId}/recipes/{recipeId}/status
POST /api/v1/management/organizations/{organizationId}/products/{productId}/variants/{variantId}/recipes/{recipeId}/versions
GET /api/v1/management/product-templates/{productId}/variants/{variantId}/recipes
GET /api/v1/management/product-templates/{productId}/variants/{variantId}/recipes/{recipeId}
POST /api/v1/management/product-templates/{productId}/variants/{variantId}/recipes
PUT /api/v1/management/product-templates/{productId}/variants/{variantId}/recipes/{recipeId}
PUT /api/v1/management/product-templates/{productId}/variants/{variantId}/recipes/{recipeId}/items
PATCH /api/v1/management/product-templates/{productId}/variants/{variantId}/recipes/{recipeId}/status
POST /api/v1/management/product-templates/{productId}/variants/{variantId}/recipes/{recipeId}/versions
PATCH /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/publish
PATCH /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/retire
DELETE /api/v1/management/robot-artifact-templates/{templateId}
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
GET /api/v1/management/organizations/{organizationId}/configuration-releases/authoring-options
GET /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}
POST /api/v1/management/organizations/{organizationId}/configuration-releases
PUT /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}/routes
GET /api/v1/management/configuration-deployments
GET /api/v1/management/kiosks/{kioskId}/configuration-deployments
GET /api/v1/management/kiosks/{kioskId}/configuration-deployments/{deploymentId}
POST /api/v1/management/kiosks/{kioskId}/configuration-deployments/{deploymentId}/rollback
GraphQL orders
GraphQL order
GraphQL orderStatusHistory
GraphQL orderExecutionAttempts
POST /api/v1/management/orders/{orderId}/execution-attempts
GET /api/v1/management/execution-attempts/{sourceCommandId}
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
POST /api/v1/management/kiosks/{kioskId}/inventory/dispenser-states
PUT /api/v1/management/kiosks/{kioskId}/inventory/dispenser-states/{dispenserStateId}
PATCH /api/v1/management/kiosks/{kioskId}/inventory/dispenser-states/{dispenserStateId}/status
DELETE /api/v1/management/kiosks/{kioskId}/inventory/dispenser-states/{dispenserStateId}
POST /api/v1/management/kiosks/{kioskId}/inventory/dispenser-states/{id}/refill
POST /api/v1/management/kiosks/{kioskId}/inventory/dispenser-states/{id}/adjust-estimate
GET /api/v1/management/kiosks/{kioskId}/heartbeats
GET /api/v1/management/kiosks/{kioskId}/operation-logs
GET /api/v1/management/kiosks/{kioskId}/operation-logs/{operationLogId}
GET /api/v1/management/kiosks/{kioskId}/operation-logs/{operationLogId}/diagnostics
GET /api/v1/management/kiosks/{kioskId}/device-events
GET /api/v1/management/alerts
GET /api/v1/management/alerts/{alertId}
PATCH /api/v1/management/alerts/{alertId}/acknowledge
PATCH /api/v1/management/alerts/{alertId}/resolve
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

- Device and execution-endpoint item operations are kiosk-owned routes: `/api/v1/management/kiosks/{kioskId}/devices/{deviceId}/...` and `/api/v1/management/kiosks/{kioskId}/execution-endpoints/{endpointId}/...`. Handlers must reject mismatched route kiosk and item ownership with `404`.
- `DELETE /api/v1/management/kiosks/{kioskId}/devices/{deviceId}` is a soft retire operation. It sets `DeviceStatus.Retired` and soft-deletes the row; it does not physically delete the device record.
- Device retirement is atomic with Inventory topology retirement and is blocked while the kiosk has an Accepted or Running execution. Active dispenser states are retired with the supplied `reason` query value or the system reason `DEVICE_RETIRED`; estimates remain historical and are not silently discarded.
- `POST /api/v1/management/kiosks/{kioskId}/devices/{deviceId}/replace` requires both `devices.manage` and `inventory.configure` and accepts an already-provisioned replacement Device in the same kiosk. It preserves every active container/ingredient/configuration mapping, transfers positive estimates with balanced stock movements, writes rebind audit records, then retires the source Device in one transaction.
- `PATCH /api/v1/management/kiosks/{kioskId}/devices/{deviceId}/status` must not set `Retired`; use the retire endpoint instead.
- `Device.Status` is a management/operations state for configured hardware. Runtime connectivity and error evidence still come from heartbeat and device-event telemetry.
- Device types and models are a global technical catalog, not tenant-owned records. Authenticated device-management users may read the catalog; only `SystemAdmin` may author it.
- Device catalog routes are `GET/POST /management/device-types`, `GET/PUT /management/device-types/{id}`, `PATCH /management/device-types/{id}/status`, `GET/POST /management/device-types/{id}/models`, and `GET/PUT/DELETE /management/device-models/{id}`.
- Device type codes and device model codes are immutable after creation. A model code is unique within its type. Model delete is a soft retire operation so installed devices retain historical identity.
- New or updated devices may reference only an active DeviceType and a non-retired DeviceModel belonging to that type. Deactivation/retirement prevents future assignments but does not rewrite existing devices.
- Device model capabilities use a typed string list at the API boundary. JSON and capability schema version remain persistence details and are not supplied by FE.
- A capability required by active dispenser topology cannot be removed from its DeviceModel. A DeviceModel cannot be retired while assigned to a non-retired Device.

Rules:

- Use `/management/...`, not `/admin/...`.
- Access is controlled by authorization policies, not the route prefix.
- Tenant authorization must match role and resource scope on the same `UserRoleScope`; combining a privileged role from one scope with an unrelated scope from another assignment is forbidden.
- It is valid for multiple roles to share the same management endpoint when policy allows it.
- Management APIs can expose configuration/admin fields that tablet APIs should not expose.
- Organization update uses scoped authorization: `SystemAdmin` can update platform-managed fields; `OrgAdmin` can update only basic profile/contact fields for assigned organization scope.
- Product and menu ownership comes from the organization route, never from a body-supplied `OrganizationId`. Generic updates cannot move `OrganizationId`, `ScopeType`, `StoreId`, `KioskId`, or template lineage. Global product templates are managed separately by `SystemAdmin`; `POST .../products/from-template` copies template metadata, variants, options, and the latest Published/Active recipe definitions into a new organization-owned Draft configuration while recording template lineage.
- GraphQL `tenantTree` is a scope/navigation read model, not a dashboard overview. Do not add revenue, alert, inventory, or runtime metrics to it.
- Back-office order operations are manual support workflows. Paid orders should be marked `RefundRequired`; they are not cancelled directly.
- Order status history is a back-office audit read model. It exposes order status transitions and a small actor snapshot (`changedByAccountId`, `changedByName`, `changedByEmail`), not full account objects, raw payment callback bodies, or robot telemetry.
- Execution-attempt reads use durable `ExecuteOrder` commands as the list authority, so pending or rejected attempts remain visible before an execution projection exists. Detail combines the optional order-summary projection with job/unit `ProductionExecutionRecord` rows, ordered delivery-attempt history, timeout provenance, redispatch actor/reason, and previous/next dispatch references. It excludes command payload JSON, raw sync events, and stock payloads. Both routes use `orders.view` and enforce scope through the owning Order.
- The per-order execution-attempt list is paging-only and has no status, endpoint, or time filters. Dispatch attempts are bounded by `OrderExecutionDispatch__MaxDispatchAttempts` (default `3`).
- Accepted commands create a provisional order-execution projection with sequence `0`. Management reads may show it before the first Edge order-summary report. Timeout reconciliation changes only observation/customer projection to `Stale/Delayed`, `Unreachable/PendingRecovery`, or prolonged `Unreachable/SupportRequired`; it must not infer `OrderStatus.Failed` from silence. Customer order/payment polling reads the latest dispatch attempt projection.
- `POST /management/orders/{orderId}/execution-attempts` is the explicit operator redispatch command. Backend allocates `latest DispatchAttemptNo + 1` under the order advisory lock; clients do not choose attempt numbers. It requires `orders.manage`, an authenticated account, and a reason of at most 500 characters.
- Redispatch is allowed only when the latest execute-order command is `DeliveryFailed`, or `Rejected` while the Order is `ExecutionRejected` (rejection before physical output). `RefundRequired`, `Failed`, active attempts, and possible physical-output cases are not redispatched automatically.
- `OrderExecutionDispatch__MaxDispatchAttempts` limits attempts. The new command stores `CreatedByAccountId`; `OrderStatusHistory` stores actor, attempt number, and reason. Repeating the request by the same operator while that new attempt is active returns the existing attempt rather than allocating another.
- Refund APIs in v1 track manual staff-handled compensation only. Supported methods are `FullMoneyRefund` and `Voucher`; both are full-order compensation flows, not partial refunds or line-item refunds.
- Full money refund sets `PaymentStatus = Refunded` only when staff confirms the money was actually refunded. Voucher compensation does not reverse payment status.
- Rejecting or cancelling a refund keeps `OrderStatus = RefundRequired`; staff may create another refund/compensation record later.
- `POST /api/v1/management/orders/{orderId}/refunds` should use `Idempotency-Key` for safe manual retries.
- Payment-session creation selects `paymentMethodCode` and submits the amount/currency currently displayed by the client. Backend remains authoritative from the stored Order and returns `409` without creating a provider session when the values differ.
- Full-money refund completion requires staff to explicitly submit `moneyWasRefunded`; omission must not be interpreted as a successful money reversal.
- Menu and menu-item creation always starts in `Draft`; lifecycle changes use the dedicated status commands. Menu-item currency is inherited from its parent menu, and product-variant currency is inherited from its parent product.
- Changing a menu currency updates all current menu-item currencies in the same unit of work. Historical orders keep their sale-time snapshots.
- Normal management contracts do not expose generic `MetadataJson` fields for organizations, products, variants, menus, or menu items. Add typed request/read-model fields when a concrete UI use case exists.
- Product and variant creation always starts unavailable; availability changes use the dedicated commands.
- ProductCategory is a global flat reference catalog in V1. `product-categories.read` provides the flat lookup for selecting `CategoryId` during product authoring. `product-categories.manage` creates, updates metadata, activates/deactivates, and deletes only unreferenced categories. Parent/child category hierarchy is not exposed in the API contract.
- Product options are authored as `Product -> OptionGroup -> ProductOption` and inherit Product tenant scope and currency. Group status and option availability use dedicated endpoints; metadata updates cannot change lifecycle state. Product cloning creates new groups/options. A MenuItem exposes only its configured subset through `productOptionIds`. Runtime menu returns typed active groups and available options. Checkout submits unique `selectedOptions[].productOptionId` values; backend validates group cardinality, availability, menu membership, and price deltas before storing immutable `OrderItemOption` snapshots. Raw option JSON from clients is not accepted or forwarded to Edge.
- Deleting a ProductOption or OptionGroup is rejected while any MenuItem membership still references it. Setting an option unavailable keeps authoring membership but removes it from runtime-menu output; if an active required group no longer has enough available choices, the MenuItem is not sellable. Catalog edits never rewrite placed-order option snapshots.
- Cloning a Product creates new OptionGroup and ProductOption identities. Cloned options retain `TemplateProductOptionId` lineage, start unavailable, and can be selected only by MenuItems whose Product is that clone.
- Ingredients are a global reference catalog in V1. `ingredients.read` provides paged lookup with optional active-status filtering. `ingredients.manage` creates, updates, and changes active status. Inactive ingredients cannot be added to Draft recipes. Delete is allowed only while no RecipeItem, dispenser state, or stock movement references the ingredient.
- Recipes are authored under their owning ProductVariant. Organization/store/kiosk scope is inherited from Product and is never accepted from the request body. Recipe code is immutable within a version family; backend allocates the next version number for each variant/code.
- Recipe metadata and ingredient membership can be changed only while status is `Draft`. `PUT .../items` atomically replaces ingredient requirements. `RecipeItem.DisplayOrder` is declaration order, not robot execution order.
- Recipe lifecycle is `Draft -> Published -> Active -> Retired`. Publishing requires at least one non-optional ingredient. Published/Active recipe content is immutable; historical Order recipe snapshots are never rewritten.
- `POST .../recipes/{recipeId}/versions` copies a non-Draft recipe and its ingredient requirements into the next backend-allocated version as Draft. The new version is not default automatically. Version allocation is serialized per ProductVariant; concurrent default changes return `409` and the database enforces one non-retired default recipe per ProductVariant.
- Product-template cloning copies the latest Published/Active recipe version for each variant/code into the organization product as a new Draft recipe. It creates new recipe/item identities and retains `TemplateRecipeId` lineage.
- Organization-owned Product, Menu, and cloned Product create contracts do not accept `ScopeType`. Backend derives it from the most-specific supplied scope id: Kiosk, Store, then Organization.
- Organization-owned RobotProgram create contracts also do not accept `ScopeType`; RobotProgram additionally supports Device scope, so backend derives its scope from Device, Kiosk, Store, then Organization.
- Execution endpoint authentication mode is derived from the selected profile: `FullEdge -> MutualTls`, `LowCostController -> SignedCommandTls`.
- Normal device and kiosk management contracts do not expose raw `MetadataJson` or `SettingsJson`. Store opening hours use a typed per-day schedule while persistence continues to serialize schema-versioned JSON internally.
- Store opening hours are an online-sale gate for both Cloud runtime-menu reads and order placement. An empty schedule means unrestricted hours; a configured schedule treats omitted/closed days as closed and evaluates `[OpensAt, ClosesAt)` in `Store.TimeZone`. `OpensAt > ClosesAt` is an overnight interval: it stays open through midnight until the following day's close time. Closed Stores return `409`.
- Configuration-release route authoring accepts `RecipeId` and derives `ProductVariantId` from the recipe before storing both route identities.
- Setting an internal-account password changes credential material only. Enabling local login remains a separate account-policy update.
- Authentication responses contain tokens, minimal identity, role scopes, and enabled login methods. Full profile fields belong to `/me`.
- Kiosk order creation derives `OrderChannel = Tablet` from the endpoint contract. Anonymous clients cannot choose an analytics/audit channel value.
- Deployment command identifiers are internal transport coordination data. Management responses expose deployment identity and status, not `EdgeCommandId`.
- Inventory owns Cloud dispenser topology in V1. Create binds an immutable `Kiosk + Device + Ingredient + ContainerCode` identity; update changes only capacity, unit, and the typed level-to-quantity profile. Unit cannot change after an estimate or stock history exists. Ingredient/device rebinding requires retiring the old state and creating a new one.
- Dispenser topology is authored directly through Inventory management APIs, not materialized by Configuration Release. `inventory.configure` excludes Staff and owns create/update/status/delete; `inventory.manage` continues to own refill and estimate adjustment.
- `GET /api/v1/management/kiosks/{kioskId}/inventory/topology` returns the kiosk Device -> containers -> Ingredient configuration, including devices with no configured containers. `CanHostDispenser` distinguishes valid unconfigured dispenser hardware from unrelated devices.
- The topology read model retains referenced retired devices and reports `DeviceInactive`, `DeviceUnavailable`, `ContainerInactive`, and `IngredientInactive` warnings instead of silently hiding stale topology references.
- Creating, updating, reactivating, or rebinding a dispenser requires a DeviceModel with `IngredientDispenser`. Devices without a model or with unrelated capabilities cannot own dispenser topology. Categorical level mapping does not require a sensor capability.
- Dispenser topology item operations are kiosk-owned routes: `/api/v1/management/kiosks/{kioskId}/inventory/dispenser-states/{id}/...`. Handlers must reject mismatched `{kioskId}` and dispenser state ownership with `404`.
- `POST /api/v1/management/kiosks/{kioskId}/inventory/dispenser-states/{id}/rebind` never mutates topology identity in place. It retires the source, creates a replacement, records an immutable rebind audit row, and commits estimate movements atomically. Rebind is rejected while the kiosk has an Accepted or Running execution.
- A positive source estimate requires explicit `Discard` or `Transfer`. Transfer is allowed only for the same Ingredient and Unit and records balanced transfer-out/transfer-in movements. Otherwise FE must choose Discard; no estimate is copied or erased silently.
- `GET /api/v1/management/kiosks/{kioskId}/inventory/dispenser-states/{id}/rebind-history` exposes the source/replacement identities, actor, reason, estimate disposition, and quantities without returning raw audit payload JSON.
- `GET /api/v1/management/kiosks/{kioskId}/inventory/dispenser-states/{id}/history` is the paged operational timeline for refill, adjustment, consumption, topology lifecycle, and rebind events. It returns account or execution-endpoint actor identity, reason, quantity delta, exact before/after balance when recorded, and topology before/after state.
- New StockMovement rows persist nullable `BalanceBefore` in addition to `BalanceAfter`; historical rows created before this contract may legitimately return a null before-value.
- Topology configuration update and status requests require an operator reason. Automatic lifecycle changes use explicit system reason codes rather than an empty audit reason.
- Level-to-quantity mapping supports Edge-reported `Low`, `Medium`, and `Full` only. A non-empty mapping must define all three levels exactly once and quantities must increase strictly in that order. Numeric sensor calibration is not supported in this phase.
- A dispenser state with stock movement history cannot be deleted and must be retired. Retired states reject sensor updates, refill, estimate adjustment, and execution stock evidence. Reactivation requires a non-retired device and active ingredient.
- Inventory estimates still do not decide runtime menu sellability or robot execution availability in V1.
- Inventory readiness compares each applicable release Recipe's required ingredients with the target kiosk topology. It returns `Ready`, `MissingIngredient`, `ContainerInactive`, `DeviceUnavailable`, or `CalibrationMissing` per route/ingredient. `CalibrationMissing` means the categorical `Low/Medium/Full` quantity mapping is absent; it does not imply raw sensor calibration support.
- `GET /api/v1/management/kiosks/{kioskId}/configuration-releases/{releaseId}/inventory-readiness` is the operational read model. It is computed from current topology and Recipe requirements; it is not persisted into ConfigurationRelease.
- Release publication and deployment only consume readiness. They never create, reactivate, rebind, or otherwise repair Inventory topology.
- `ProductionInventoryReadiness.PublishPolicy` defaults to `Warn`: publishing succeeds and returns not-ready kiosk details because a reusable release may be published before every kiosk is provisioned. `DeployPolicy` defaults to `Block`: target deployment returns `409` before creating a deployment or EdgeCommand when readiness is not `Ready`.
- Readiness is an operational setup/deployment gate only. Runtime-menu visibility, order creation, inventory reservation, and execution sellability remain unchanged in this phase.
- Operations telemetry APIs expose curated heartbeat/event fields only. Do not return raw `PayloadJson` by default.
- Normal telemetry reads also exclude source node ids, heartbeat sequence numbers, and correlation/causation ids. Those ingestion identities remain available to machine contracts.
- Operation-log list/detail reads are kiosk-owned and filter by `deviceId`, `orderId`, `severity`, `from`, and `to`. Their normal response excludes raw payload and sync identities. `GET .../operation-logs/{operationLogId}/diagnostics` exposes only raw payload under `operations.diagnostics`; it is limited to scoped `SystemAdmin` and `Technician` users.
- `DeviceEvent` is immutable log/evidence, not mutable alert state. Newly accepted Error/Critical telemetry creates a separate Open Alert in the same transaction; Warning remains evidence only.
- Alert management uses `/api/v1/management/alerts`: scoped list/get plus acknowledge/resolve. V1 has no general manual create endpoint; alert creation belongs to authenticated telemetry ingestion.
- Maintenance ticket V1 is a manual operations/support workflow. Tickets are kiosk-scoped work items with optional evidence links to device, order, or device event. V1 does not include auto-generated tickets, alert engine, chat, reopen, or GraphQL maintenance aggregate.
- Execution endpoint credential rotation is a maintenance operation. It revokes the current credential binding and activates the new credential reference in one database save. Rotation preserves the endpoint's prior Active or Disabled state; Provisioning and Retired endpoints cannot rotate. Hot credential overlap is not part of V1.
- Robot artifact bulk upload is the only public upload contract. It accepts one to 50 multipart `.lua` files, stores file bytes in S3-compatible object storage, and stores immutable metadata in `RobotArtifact`.
- Bulk robot artifact upload accepts up to 50 files plus a JSON manifest that supplies per-file metadata. Uploaded artifacts remain unassigned Draft inventory and do not change any robot-program sequence. Request-shape errors reject the whole request; upload failures use item-level atomicity and return per-item results without rolling back successful items.
- Artifact upload retry identity is organization + normalized artifact code + SHA-256. An exact retry returns the existing artifact id and metadata as success; bulk results expose `uploadedCount`, `existingCount`, and per-item `wasExisting`.
- Artifact review URLs are organization-scoped, short-lived, and generated only after confirming the private object exists. They are not durable download contracts and must not be stored by clients.
- Draft discard hard-deletes metadata only when no `RobotProgramArtifact` references exist. Metadata deletion commits before best-effort object deletion; the orphan cleanup job removes residual objects after its grace period.
- Bulk artifact publish atomically transitions 1-100 unique selected Draft artifacts from one organization. Published items are idempotent no-ops; duplicate ids, missing, cross-organization, Disabled, or Retired selections reject the complete publish request.
- Robot artifact publish makes an uploaded artifact available for programs. Robot program and configuration release publication create immutable definitions from their authored children.
- `RobotArtifactTemplate` is a global authoring source, not a runtime artifact. Only a Published template may be cloned; cloning creates a separate organization-owned Draft `RobotArtifact`, copies immutable bytes to an organization object key, and records `SourceRobotArtifactTemplateId` for lineage. Programs, releases, deployments, and execution endpoints never reference templates directly. An unreferenced Draft template may be hard-discarded; Published and Retired templates remain history.
- Retire lifecycle commands are idempotent and do not hard-delete artifact bytes, manifests, or deployment history. Artifact retirement is blocked by Draft programs, program retirement by Draft releases, and release retirement by Pending/Installed deployments. Published history can retain retired references for audit and rollback.
- Robot programs are created as organization-owned drafts. Store, kiosk, and device scope may narrow that ownership, but all scope ids must belong to the same tenant hierarchy. Global robot-program creation is not exposed because `RobotArtifact` is organization-owned.
- `PUT /management/organizations/{organizationId}/robot-programs/{programId}/artifacts` replaces the complete ordered membership while the program is Draft. `RunOrder` is explicit API data; backend must not derive execution order from an exported filename prefix.
- Every assigned artifact must belong to the program organization. Artifact parameters must be valid JSON. Publishing still requires all assigned artifacts to be Published.
- Artifact and program list endpoints are paged and tenant-scoped. Program lists return summaries without manifest JSON or artifact collections. Program detail includes ordered artifact metadata so management clients can edit/reorder a draft without issuing one request per artifact.
- `PUT /management/organizations/{organizationId}/robot-programs/{programId}` edits draft code, name, and description only. Program scope is immutable after creation; changing ownership scope requires a new draft program.
- `RobotProgramArtifact` is aggregate membership, not an independent management resource. Clients replace the ordered collection through the program endpoint instead of creating or deleting membership rows individually.
- Configuration releases are created as organization-owned drafts with backend-assigned release numbers. Route authoring replaces the complete Draft route/binding collection; `ExecutionRoute` and `ExecutionRouteRobotBinding` are aggregate children, not independent CRUD resources.
- Draft robot programs and Draft configuration releases can be hard-discarded through their `DELETE` endpoints. Published or referenced records are preserved; retirement remains the lifecycle operation for published history.
- `GET /management/organizations/{organizationId}/configuration-releases/authoring-options` is a tenant-scoped UI lookup read model owned by configuration-release authoring. It returns eligible machine-produced ProductVariant options, Published/Active Recipes, and organization-owned Published RobotPrograms with scope and display metadata. `productVariantId`, `search`, and `limit` are optional; `limit` applies independently to each result group. The command handler still revalidates every selected id when routes are submitted.
- Release route authoring requires Published/Active recipes to belong to their product variants and bindings to reference Published robot programs owned by the release organization. Kiosk/device compatibility remains a deployment-time validation.
- Release lists return summaries without manifest JSON or route/binding collections. Release detail remains the review surface for the complete authored graph.
- `GET /management/configuration-deployments` is a read-only global management index for scoped deployment search. Deployment detail and rollback are kiosk-owned routes under `/management/kiosks/{kioskId}/configuration-deployments/...` because deployment affects a physical kiosk execution endpoint.
- Configuration deployment reads unify Full Edge and Low-cost histories behind tenant-scoped, paged management surfaces. Filters include organization, store, kiosk, release, profile, and status for the global index; kiosk-owned reads bind `kioskId` from the route. Profile-specific provenance remains nullable rather than being discarded.
- Configuration rollback selects a previously Active deployment, then creates a new profile-matching deployment and durable command. It does not mutate or reactivate the historical deployment row. Retired releases are eligible only through this validated rollback path.
- The complete Fairino export-to-deployment sequence is owned by [Robot Lua Artifact Flow](../flows/ROBOT_LUA_ARTIFACT_FLOW.md); keep this document focused on route and client boundaries.
- Publish commands do not deploy to an execution endpoint.
- Full Edge deployment requests create a durable deployment command for the immutable release. Edge validates downloaded content and reports installation and activation separately.
- Low-cost deployment requests select unique route/program pairs. Backend includes every ordered artifact in each selected published program and enforces the configured controller capacity.
- Normal Full Edge and low-cost deployments require a Published configuration release. A Retired release may be deployed only through the validated rollback endpoint; callers cannot use the normal deployment routes to bypass retirement.
- Full Edge deploy, Low-cost deploy, and rollback requests require `Idempotency-Key`. The key is durable and unique per execution endpoint. An exact retry returns the existing deployment and command; reusing a key for a different release or Low-cost selection returns `409`.
- Robot program, configuration release, and deployment read routes use `program.read`, `release.read`, and `deployment.read`. Authoring/publishing/deployment commands retain their narrower mutation policies.
- When a deployment command expires before acceptance, its still-Pending deployment becomes `Failed/CommandExpired`.
- An accepted deployment that receives no installation report before its deadline becomes `Failed/ExecutionReportTimeout`.
- Artifact bytes are not exposed through a public REST download endpoint. After execution-endpoint authentication, command pull enriches deployment artifact descriptors with short-lived object-storage read URLs. These URLs are not durable API identifiers and must not be stored as release state.

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
- `/me/access` reports the caller's current token roles and effective scoped ids. It is not a fresh database authorization recalculation.
- Notification-device routes are self-service FCM registration only. They never accept `AccountId`, expose a push token/hash, or grant trusted-session behavior.

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
GET /api/v1/iot/execution-endpoints/{endpointId}/configuration
```

Rules:

- Do not use internal account JWT as the long-term kiosk runtime credential.
- IoT routes no longer accept plaintext `X-Execution-Credential`. Full Edge endpoints authenticate with a directly presented client certificate pinned by SHA-256 fingerprint. Low-cost endpoints authenticate each raw HTTP request with ECDSA NIST P-256, timestamp, and a database-deduplicated nonce over TLS.
- Execution endpoint reads are tenant-scoped and never return credential material. Full Edge provisioning accepts `ClientCertificateSha256Fingerprint`; low-cost provisioning accepts `EcdsaPublicKeyPem`. Both require at least one supported robot target and assign exactly one profile identity: `FullEdgeRuntimeId` or `ControllerId`.
- Cryptographic transport verification belongs to WebAPI. Application handlers retain endpoint/kiosk/status/credential-binding checks but do not receive HTTP certificates, signatures, or plaintext credentials.
- Heartbeat ingest derives trust from the authenticated execution endpoint, validates `originNodeId` against its bound profile identity, and deduplicates by `(kioskId, originNodeId, heartbeatSequence)`. Unique stale sequences remain history-only; only the newest `Online`/`Degraded` sequence advances `Kiosk.LastOnlineAt` using Cloud receive time or changes current connectivity.
- The connectivity state machine owns `Active <-> Offline`: reachable heartbeat evidence recovers an Offline kiosk when its parent scope is active; an Offline heartbeat or heartbeat timeout moves an Active kiosk Offline. It does not override Provisioning, Maintenance, Disabled, or Retired. Manual management requests cannot set Offline or recover Offline to Active.
- Heartbeat ingestion and timeout reconciliation serialize by kiosk. `KioskStatusChanged` is emitted only for a committed transition, never for duplicate heartbeat delivery or an unchanged state.
- Readiness ingest is a typed complete snapshot per execution endpoint. `stateRevision` is monotonic per executor; a newer revision replaces readiness/activity/safety and the complete capability set. It does not mutate `KioskStatus`.
- Machine-produced menu/order readiness requires at least one Active endpoint whose latest projection is Ready and Safe and whose available capability set covers every route binding. Dispatch additionally requires the selected endpoint to be Idle.
- Execution route `RequiredCapabilitiesJson` is optional. When supplied, it must use the V1 bounded schema with `schemaVersion = 1` and `requires[]` capability objects. Requirement codes must be declared by that route's robot bindings; unknown JSON fields are rejected. Cloud V1 enforcement remains `RequiredWorkcellCapabilityCode` against endpoint readiness capabilities.
- Device-event ingest accepts one `Warning`, `Error`, or `Critical` evidence record, verifies device/kiosk ownership, deduplicates globally by `eventId`, and publishes `DeviceEventCreated` only after a new row commits. Raw payload remains excluded from management reads.
- Newly accepted `Error` or `Critical` device events also create one Open Alert atomically and publish `AlertChanged` after commit. Warning events do not auto-create alerts.
- Supported robot targets are a complete replacement contract and may change only while the endpoint is `Provisioning` or `Disabled`. A device-specific target must reference a device attached to the same kiosk.
- Endpoint activation, credential rotation, disable/reactivate, and retirement are management operations. They do not install artifacts; release deployment remains a separate command flow.
- MQTT subscriber credentials have a separate endpoint-scoped lifecycle: `POST/PATCH/DELETE /management/kiosks/{kioskId}/execution-endpoints/{id}/mqtt-credential`. Provision and rotation return a generated password once; normal reads expose only username, status, and credential version. HTTPS transport credentials are never reused for MQTT.
- `POST /api/v1/iot/execution-endpoints/{endpointId}/commands/{commandId}/reports` is the current V1 execution/deployment report ingest endpoint. It records an immutable `SyncEventInbox` envelope; the same source event id with a different command or payload returns conflict.
- Order-level reports update only `OrderExecutionRecord`; job-level reports require `sourceProductionJobId` and update only the matching `ProductionExecutionRecord`. Stock evidence is job-scoped. The ingestion coordinator keeps one database transaction while deployment, order/job projection, and stock persistence use separate aggregate ports.
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
* **Includes:** Quantity, billing totals, payment confirmation, preparation state, and tablet-friendly status projections (`CustomerStatus`, `CustomerStatusMessage`, `CanRetryPayment`, `RequiresStaffSupport`).
* **EXCLUDES:** Internal order-item, order-payment, payment-transaction, and order state-machine enums; raw payment provider callback bodies; device error codes; robot joint telemetry.
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
| `OrderHub` | `/hubs/orders` | Order-specific updates | `OrderStatusChanged`, `PaymentStatusChanged`, `OrderExecutionObservationChanged` |
| `OperationsHub` | `/hubs/operations` | Kiosk & telemetry status | `KioskStatusChanged`, `ExecutionReadinessChanged`, `DeviceEventCreated`, `AlertChanged`, `MaintenanceTicketChanged`, `InventoryChanged` |
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
