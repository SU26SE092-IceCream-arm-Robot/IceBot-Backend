# Management API Surface

This document owns the curated internal management REST and GraphQL route catalog. Controller attributes, generated OpenAPI, and the GraphQL schema remain the exact executable inventory. Cross-cutting route ownership, validation, response, and transport rules remain in [API Surface Rules](API_SURFACE_RULES.md).

## Search Keywords

`management API`, `internal management routes`, `back-office API`, `organization routes`, `kiosk routes`, `catalog management`, `production package API`, `deployment API`, `inventory management`, `GraphQL management reads`

## Route Catalog

Management APIs are for internal operations, not only the `Manager` role.

Current examples:

### Catalog And Sales Catalog Routes

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
PUT /api/v1/management/organizations/{organizationId}/products/{productId}/option-groups/{optionGroupId}/options/{productOptionId}/ingredient-requirements
GET /api/v1/management/organizations/{organizationId}/menus
```

### Identity, Payments, And Tenant Routes

```text
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
PATCH /api/v1/management/organizations/{organizationId}/stores/{storeId}/sales-pause
PATCH /api/v1/management/organizations/{organizationId}/stores/{storeId}/sales-resume
GET /api/v1/management/kiosks
GET /api/v1/management/kiosks/{kioskId}
POST /api/v1/management/stores/{storeId}/kiosks
PUT /api/v1/management/kiosks/{kioskId}
PATCH /api/v1/management/kiosks/{kioskId}/status
```

### Device And Execution Endpoint Routes

```text
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
```

### Ingredient And Recipe Routes

```text
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
```

### Robot And Production Configuration Routes

```text
GET/POST /api/v1/management/robot-artifact-template-contracts
GET/PUT/DELETE /api/v1/management/robot-artifact-template-contracts/{contractId}
POST /api/v1/management/robot-artifact-template-contracts/{contractId}/validation-preview
PATCH /api/v1/management/robot-artifact-template-contracts/{contractId}/publish
PATCH /api/v1/management/robot-artifact-template-contracts/{contractId}/retire
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
POST /api/v1/management/kiosks/{kioskId}/configuration-deployments/preview
POST /api/v1/management/organizations/{organizationId}/robot-programs
PUT /api/v1/management/organizations/{organizationId}/robot-programs/{programId}/artifacts
POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports
GET /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}
GET /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/workspace
POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/validate
POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/materialize
POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/preview-composition
POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/confirm-composition
POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/publish-resources
POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/create-release-draft
POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/discard
POST /api/v1/management/organizations/{organizationId}/robot-artifacts
PATCH /api/v1/management/organizations/{organizationId}/robot-artifacts/publish
GET /api/v1/management/organizations/{organizationId}/robot-artifacts
GET /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}
GET /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/usage
POST /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/review-url
DELETE /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}
GET /api/v1/management/kiosks/{kioskId}/configuration-deployments/{deploymentId}/artifacts
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
```

### Orders, Payments, Inventory, And Operations Routes

```text
GraphQL orders
GraphQL order
GraphQL orderStatusHistory
GraphQL orderExecutionAttempts
GraphQL fulfillmentQueue
GraphQL orderItemStatusHistory
POST /api/v1/management/orders/{orderId}/execution-attempts
GET /api/v1/management/production-incidents
GET /api/v1/management/orders/{orderId}/production-incidents/{incidentId}
POST /api/v1/management/orders/{orderId}/items/{orderItemId}/production-incidents
PATCH /api/v1/management/orders/{orderId}/production-incidents/{incidentId}/inspection
POST /api/v1/management/orders/{orderId}/production-incidents/{incidentId}/resolution
PATCH /api/v1/management/orders/{orderId}/production-incidents/{incidentId}/complete
POST /api/v1/management/orders/{orderId}/items/{orderItemId}/manual-fulfillment-events
POST /api/v1/management/orders/{orderId}/items/{orderItemId}/fulfill
POST /api/v1/management/orders/{orderId}/items/{orderItemId}/fail
GET /api/v1/management/orders/{orderId}/execution-attempts/{sourceCommandId}/diagnostics
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

## Route Boundary Summaries

These summaries describe client-visible scope, authorization, and request/response behavior. Detailed lifecycle and domain invariants remain owned by the linked flow and architecture documents.

### Maintenance Assignment

Maintenance assignment accepts only an active `Technician`, `Manager`, or
`OrgAdmin` whose single role-scope assignment matches the ticket kiosk, store,
or organization. Cross-tenant role and scope composition is rejected. Push-token
registration is not an assignment prerequisite.

### Device Catalog And Lifecycle

- Device and execution-endpoint item operations are kiosk-owned routes: `/api/v1/management/kiosks/{kioskId}/devices/{deviceId}/...` and `/api/v1/management/kiosks/{kioskId}/execution-endpoints/{endpointId}/...`. Handlers must reject mismatched route kiosk and item ownership with `404`.
- `DELETE /api/v1/management/kiosks/{kioskId}/devices/{deviceId}` is a soft retire operation. It sets `DeviceStatus.Retired` and soft-deletes the row; it does not physically delete the device record.
- Device retirement is atomic with Inventory topology retirement and is blocked while the kiosk has an Accepted or Running execution. Active dispenser states are retired with the supplied `reason` query value or the system reason `DEVICE_RETIRED`; estimates remain historical and are not silently discarded.
- `POST /api/v1/management/kiosks/{kioskId}/devices/{deviceId}/replace` requires both `devices.manage` and `inventory.configure` and accepts an already-provisioned replacement Device in the same kiosk. It preserves every active container/ingredient/configuration mapping, transfers positive estimates with balanced stock movements, writes rebind audit records, then retires the source Device in one transaction.
- `PATCH /api/v1/management/kiosks/{kioskId}/devices/{deviceId}/status` must not set `Retired`; use the retire endpoint instead.
- Device lifecycle is `Provisioning -> Online|Offline|Maintenance|Error|Disabled`; operational states may move between each other or to `Disabled`; `Disabled -> Provisioning` is the explicit re-enable path; `Retired` is terminal and is reached only through device retirement.
- A provider-confirmed payment received after local payment expiry or customer cancellation remains authoritative. A pending order becomes execution-ready; an already-cancelled order becomes `RefundRequired` for staff handling and is never dispatched automatically.
- If more than one provider session for the same Order is confirmed paid, every transaction retains provider truth as `Paid`, but exactly one transaction is the primary settlement. Later paid occurrences become `DuplicateRefundRequired`, appear in the payment-intervention queue, and move the Order to `RefundRequired` without changing settlement `PaidAmount` or `PaidAt`; fulfillment is not dispatched for the duplicate event.
- `Device.Status` is a management/operations state for configured hardware. Runtime connectivity and error evidence still come from heartbeat and device-event telemetry.
- Device types and models are a global technical catalog, not tenant-owned records. Authenticated device-management users may read the catalog; only `SystemAdmin` may author it.
- Device catalog routes are `GET/POST /management/device-types`, `GET/PUT /management/device-types/{id}`, `PATCH /management/device-types/{id}/status`, `GET/POST /management/device-types/{id}/models`, and `GET/PUT/DELETE /management/device-models/{id}`.
- Device type codes and device model codes are immutable after creation. A model code is unique within its type. Model delete is a soft retire operation so installed devices retain historical identity.
- New or updated devices may reference only an active DeviceType and a non-retired DeviceModel belonging to that type. Deactivation/retirement prevents future assignments but does not rewrite existing devices.
- Device model capabilities use a typed string list at the API boundary. JSON and capability schema version remain persistence details and are not supplied by FE.
- A capability required by active dispenser topology cannot be removed from its DeviceModel. A DeviceModel cannot be retired while assigned to a non-retired Device.

### Cross-Cutting Management Rules

- Use `/management/...`, not `/admin/...`.
- Access is controlled by authorization policies, not the route prefix.
- Tenant authorization must match role and resource scope on the same `UserRoleScope`; combining a privileged role from one scope with an unrelated scope from another assignment is forbidden.
- It is valid for multiple roles to share the same management endpoint when policy allows it.
- Management APIs can expose configuration/admin fields that tablet APIs should not expose.
- Organization update uses scoped authorization: `SystemAdmin` can update platform-managed fields; `OrgAdmin` can update only basic profile/contact fields for assigned organization scope.
- Product and menu ownership comes from the organization route, never from a body-supplied `OrganizationId`. Generic updates cannot move `OrganizationId`, `ScopeType`, `StoreId`, `KioskId`, or template lineage. Global product templates are managed separately by `SystemAdmin`; `POST .../products/from-template` copies template metadata, variants, options, and the latest Published/Active recipe definitions into a new organization-owned Draft configuration while recording template lineage.
- GraphQL `tenantTree` is a scope/navigation read model, not a dashboard overview. Do not add revenue, alert, inventory, or runtime metrics to it.

### Orders, Fulfillment, And Payments

- Back-office order operations are manual support workflows. Paid orders should be marked `RefundRequired`; they are not cancelled directly.
- Manual lines use `manual-fulfillment-events` for the strict `Pending -> Accepted -> Preparing -> Completed` lifecycle, or `Failed` from a non-terminal state. The request requires a client-generated `fulfillmentEventId`; reusing it with a different payload returns `409`.
- Packaged lines use idempotent `fulfill` and `fail` commands with a client-generated `fulfillmentEventId`. Staff moves them directly from `Pending` to `Completed` when handing the ready-made item to the customer, or from `Pending` to `Failed` with a required reason when fulfillment is impossible. They never enter `Accepted` or `Preparing`.
- Machine-produced lines reject both management flows and advance only from authenticated Edge production reports. Order status is aggregated from all immutable line fulfillment types; completing one production job cannot complete a mixed order while other lines remain incomplete.
- One failed item moves a paid order to `FulfillmentIssue` and requires staff review; it does not automatically refund the whole order and does not prevent remaining non-terminal items from being fulfilled.
- A paid machine order that cannot create its initial Edge command remains retryable only until `OrderExecutionDispatch__InitialDispatchSupportEscalationMinutes`. After that SLA it becomes `FulfillmentIssue`, records order history, and publishes `SupportRequired`; this does not claim that physical execution failed.
- Manual and packaged fulfillment remain line-atomic. Machine-produced lines retain unit/range outcomes; their business line completes only when every expected unit's effective outcome is `Completed`. A failed unit moves the paid order to `FulfillmentIssue` without erasing successful unit or stock evidence.
- `fulfillmentQueue` returns tenant-scoped manual and packaged work; `orderItemStatusHistory` returns the item-level audit trail. Both are GraphQL management reads protected by `orders.view`.
- Packaged variants may expose only `CommercialOnly` options. Physical packaged choices belong in separate product variants. Manual variants may use production-affecting options as staff instructions; machine-produced variants require active-route support for each production-affecting option.
- `FulfillmentType` is management/backend context and is not returned in the customer order-item response.
- Order status history is a back-office audit read model. It exposes order status transitions and a small actor snapshot (`changedByAccountId`, `changedByName`, `changedByEmail`), not full account objects, raw payment callback bodies, or robot telemetry.
- Execution-attempt reads use durable `ExecuteOrder` commands as the list authority, so pending or rejected attempts remain visible before an execution projection exists. Detail combines the optional order-summary projection with job/unit `ProductionExecutionRecord` rows, completed/failed/in-progress/unreported unit counts, ordered delivery-attempt history, timeout provenance, redispatch actor/reason, and previous/next dispatch references. It excludes command payload JSON, raw sync events, and stock payloads. Both routes use `orders.view` and enforce scope through the owning Order.
- The per-order execution-attempt list is paging-only and has no status, endpoint, or time filters. Dispatch attempts are bounded by `OrderExecutionDispatch__MaxDispatchAttempts` (default `3`).
- Accepted commands create a provisional order-execution projection with sequence `0`. Management reads may show it before the first Edge order-summary report. Timeout reconciliation changes only observation/customer projection to `Stale/Delayed`, `Unreachable/PendingRecovery`, or prolonged `Unreachable/SupportRequired`; it must not infer `OrderStatus.Failed` from silence. Customer order/payment polling reads the latest dispatch attempt projection.
- `POST /management/orders/{orderId}/execution-attempts` is the explicit operator redispatch command. Backend allocates `latest DispatchAttemptNo + 1` under the order advisory lock; clients do not choose attempt numbers. It requires `orders.manage`, an authenticated account, and a reason of at most 500 characters.
- GraphQL `orderExecutionAttempts` exposes the normal operational summary. Full command provenance, delivery attempts, executor sequence data, and production evidence are restricted to `GET /management/orders/{orderId}/execution-attempts/{sourceCommandId}/diagnostics` with `operations.diagnostics`; the attempt must belong to the route order.
- `POST /management/orders/{orderId}/items/{orderItemId}/production-remakes` creates an idempotent remake command for an exact failed unit range. `remakeRequestId` is client-generated. The normal endpoint permits it only for a paid `FulfillmentIssue`, complete terminal source evidence, and units whose latest outcome is `Failed` with `physicalOutputMayHaveOccurred=false`. It never replays the whole order.
- Failed or manual-intervention production job evidence creates one Orders-owned production incident in the same ingestion transaction. Unknown or possible physical output remains `AwaitingInspection`; confirmed no-output evidence records `NotProduced` without claiming that output exists.
- `GET /management/production-incidents` is the tenant-scoped operations work queue. Incident detail and mutations are order-owned routes. Manual defect reporting must reference an existing execution job and exact production-unit range; it cannot invent production provenance.
- Inspection is required before selecting a resolution. Supported V1 resolutions are deliver existing output, discard, exact-unit remake, full-order refund, full-order voucher, technical review, or no action. A defective-output remake is allowed only through the matching incident whose inspection is `Defective` and whose selected resolution is `RequestRemake`; this exception does not weaken the normal remake endpoint.
- Resolution selection is idempotent by `resolutionRequestId` plus a stored fingerprint of the normalized resolution payload. Reusing an id with changed resolution, payment target, voucher data, reason, or acknowledgement returns `409`. Remake stores the resulting Edge command id; refund/voucher stores the Payments-owned refund id. Completing an incident is an explicit staff audit action and does not rewrite immutable execution or stock-consumption evidence.
- Refund/voucher incident resolution requires explicit `acknowledgeFullOrderCompensation=true` because V1 has no partial-refund contract. It additionally requires the existing `refunds.manage` scope enforced by the Payments handler. Production incidents never trigger automatic refunds.
- Redispatch is allowed only when the latest execute-order command is `DeliveryFailed`, or `Rejected` while the Order is `ExecutionRejected` (rejection before physical output). `RefundRequired`, `Failed`, active attempts, and possible physical-output cases are not redispatched automatically.
- `OrderExecutionDispatch__MaxDispatchAttempts` limits attempts. The new command stores `CreatedByAccountId`; `OrderStatusHistory` stores actor, attempt number, and reason. Repeating the request by the same operator while that new attempt is active returns the existing attempt rather than allocating another.
- Refund APIs in v1 track manual staff-handled compensation only. Supported methods are `FullMoneyRefund` and `Voucher`. Normal refund-required orders use full-order compensation; duplicate-payment intervention refunds the selected duplicate occurrence while preserving the primary settlement.
- Full money refund of the primary settlement sets `PaymentStatus = Refunded` only when staff confirms the money was actually refunded. Resolving a duplicate occurrence keeps the Order payment `Paid`, marks only that duplicate transaction resolved/refunded, and restores the pre-intervention Order status after all duplicate occurrences are resolved. Voucher compensation does not reverse payment status.
- Rejecting or cancelling a refund keeps `OrderStatus = RefundRequired`; staff may create another refund/compensation record later.
- `POST /api/v1/management/orders/{orderId}/refunds` should use `Idempotency-Key` for safe manual retries. `paymentTransactionId` is optional when there is one unambiguous refund target; it is required when multiple duplicate payments await resolution. The selected transaction must be paid and belong to the route Order.
- Payment-session creation selects `paymentMethodCode` and submits the amount/currency currently displayed by the client. Backend remains authoritative from the stored Order and returns `409` without creating a provider session when the values differ.
- Full-money refund completion requires staff to explicitly submit `moneyWasRefunded`; omission must not be interpreted as a successful money reversal.
- `GET /api/v1/management/orders/{orderId}/payment-diagnostics` is an order-owned diagnostics read protected by `operations.diagnostics`. It exposes provider identity, reconciliation attempts, bounded failure details, and stored provider request/response evidence; normal tablet and order-management responses do not expose those fields.
- Payment-session creation persists a deterministic provider order code before the provider `POST`. Recovery queries that identity instead of repeating the create request. A provider lookup may restore checkout instructions, but only a verified provider webhook may commit `Paid` and trigger fulfillment.
- Provider webhook idempotency is exact: reusing `ProviderEventId` requires the same provider payment identity and raw verified payload. A different identity or payload returns `409`. Verified callbacks rejected by business validation are retained as ignored evidence and do not mutate payment or Order state.
- `GET /api/v1/management/payment-session-interventions` is a tenant-filtered `payments.manage` work queue. It returns bounded payment/order identity, issue code, retry state, and eligibility; it excludes raw provider payloads. `DUPLICATE_PAYMENT_REFUND_REQUIRED` entries are manual refund/compensation work and are not provider-reconciliation candidates.
- `POST /api/v1/management/orders/{orderId}/payment-transactions/{paymentTransactionId}/reconcile` performs one provider lookup for an eligible incomplete session. Eligibility is shared with the intervention queue: a pending provider session has missing checkout instructions or has reached its local expiry, even when an old URL/QR payload remains stored. The command requires `payments.manage`, a reason, exact Order ownership, and writes request/result operation-log audit records. It never repeats the provider create `POST` and never treats lookup-only `PAID` as fulfillment authority.
- Entering payment-session manual intervention enqueues a durable `payment_intervention` push for scoped Staff/Manager recipients, with organization OrgAdmin fallback. Ordinary scheduled retries, restored sessions, explicit cancellation/expiry, and known missing provider sessions do not notify. Repeating the same payment transaction and intervention code is idempotent per recipient.

### Catalog And Sales Catalog

- Menu and menu-item creation always starts in `Draft`; lifecycle changes use the dedicated status commands. Menu-item currency is inherited from its parent menu, and product-variant currency is inherited from its parent product.
- Menu currency can change only while the menu has no items. MenuItem currency is inherited at creation, and historical orders keep their sale-time snapshots.
- A Product or ProductVariant referenced by a non-deleted MenuItem cannot be deleted, and referenced Product currency cannot change, until those references are archived or replaced. MenuItem activation performs static Product, Variant, Recipe, ingredient, currency, and option-satisfiability validation before entering `Active`.
- `UpdateMenuItemRequest.ClearRecipe = true` explicitly removes an optional Recipe binding; it cannot be combined with `RecipeId`. Omission preserves the current binding.
- Runtime-menu responses expose a deterministic content `Revision` as `ETag`. `SnapshotId` remains a per-request identity; clients may use `If-None-Match` and receive `304` while sellable content is unchanged.
- Normal management contracts do not expose generic `MetadataJson` fields for organizations, products, variants, menus, or menu items. Add typed request/read-model fields when a concrete UI use case exists.
- Product and variant creation always starts unavailable; availability changes use the dedicated commands.
- ProductCategory is a global flat reference catalog in V1. `product-categories.read` provides the flat lookup for selecting `CategoryId` during product authoring. `product-categories.manage` creates, updates metadata, activates/deactivates, and deletes only unreferenced categories. The domain and database model do not contain parent/child hierarchy.
- Product options are authored as `Product -> OptionGroup -> ProductOption` and inherit Product tenant scope and currency. Group status and option availability use dedicated endpoints; metadata updates cannot change lifecycle state. Product cloning creates new groups/options. A MenuItem exposes only its configured subset through `productOptionIds`. Runtime menu returns typed active groups and selectable options. Checkout submits unique `selectedOptions[].productOptionId` values; backend validates every active group definition, including required groups with no configured MenuItem membership, plus cardinality, option/ingredient lifecycle, menu membership, and price deltas before storing immutable `OrderItemOption` snapshots. Raw option JSON from clients is not accepted or forwarded to Edge.
- Deleting a ProductOption or OptionGroup is rejected while any MenuItem membership still references it. Setting an option or one of its required ingredients inactive keeps authoring membership but removes the option from runtime-menu output; if an active required group no longer has enough selectable choices, the MenuItem is not sellable. A MenuItem whose attached Recipe references an inactive Ingredient is also not sellable. Catalog edits never rewrite placed-order recipe or option snapshots.
- Cloning a Product creates new OptionGroup and ProductOption identities. Cloned options retain `TemplateProductOptionId` lineage, start unavailable, and can be selected only by MenuItems whose Product is that clone.
- Ingredients are a global reference catalog in V1. `ingredients.read` provides paged lookup with optional active-status filtering. `ingredients.manage` creates, updates, and changes active status. Inactive ingredients cannot be added to Draft recipes. Delete is allowed only while no RecipeItem, dispenser state, or stock movement references the ingredient.
- Recipes are authored under their owning ProductVariant. Organization/store/kiosk scope is inherited from Product and is never accepted from the request body. Recipe code is immutable within a version family; backend allocates the next version number for each variant/code.
- Recipe metadata and ingredient membership can be changed only while status is `Draft`. `PUT .../items` atomically replaces ingredient requirements. `RecipeItem.DisplayOrder` is declaration order, not robot execution order.
- Product options declare required typed `executionImpact`: `CommercialOnly` changes price/customer choice without changing machine execution; `ProductionAffecting` participates in ingredient/artifact composition. Create/update requests must send the field explicitly. Commercial-only options cannot have ingredient execution requirements. `PUT .../ingredient-requirements` accepts non-empty requirements only for production-affecting options. Every requirement uses the catalog ingredient unit and declares its required workcell capability. Each selected production-affecting option snapshots those requirements into the order; dispatch requires an active, online kiosk dispenser and an available matching capability on the chosen endpoint. Estimated quantity remains outside this gate.
- New order recipe snapshots use schema version `2` and include immutable base-recipe ingredient declarations. Existing version `1` snapshots remain historical records.
- Recipe lifecycle is `Draft -> Published -> Active -> Retired`. Publishing requires at least one non-optional ingredient. Published/Active recipe content is immutable; historical Order recipe snapshots are never rewritten.
- `POST .../recipes/{recipeId}/versions` copies a non-Draft recipe and its ingredient requirements into the next backend-allocated version as Draft. The new version is not default automatically. Version allocation is serialized per ProductVariant; concurrent default changes return `409` and the database enforces one non-retired default recipe per ProductVariant.
- Product-template cloning copies the latest Published/Active recipe version for each variant/code into the organization product as a new Draft recipe. It creates new recipe/item identities and retains `TemplateRecipeId` lineage.

### Request And Response Boundaries

- Organization-owned Product, Menu, and cloned Product create contracts do not accept `ScopeType`. Backend derives it from the most-specific supplied scope id: Kiosk, Store, then Organization.
- Organization-owned RobotProgram create contracts also do not accept `ScopeType`; RobotProgram additionally supports Device scope, so backend derives its scope from Device, Kiosk, Store, then Organization.
- Execution endpoint authentication mode is derived from the selected profile: `FullEdge -> MutualTls`, `LowCostController -> SignedCommandTls`.
- Normal device and kiosk management contracts do not expose raw `MetadataJson` or `SettingsJson`. Store opening hours use a typed per-day schedule while persistence continues to serialize schema-versioned JSON internally.
- Store opening hours and the explicit sales-pause lifecycle are online-sale admission gates for both Cloud runtime-menu reads and order placement. An empty schedule means unrestricted hours; a configured schedule treats omitted/closed days as closed and evaluates `[OpensAt, ClosesAt)` in `Store.TimeZone`. `OpensAt > ClosesAt` is an overnight interval: it stays open through midnight until the following day's close time. Closed or manually paused Stores return `409` for new sales admission.
- Sales pause is distinct from disabling a Store. `PATCH /management/organizations/{organizationId}/stores/{storeId}/sales-pause` requires a reason and accepts an optional future `resumeAt`; `PATCH .../sales-resume` resumes immediately. A timed pause stops blocking automatically at `resumeAt`. Neither scheduled close nor sales pause cancels paid, queued, accepted, or running fulfillment.
- Order placement snapshots `paymentDeadlineAt` from `Payments:OrderPaymentWindow:DurationMinutes`. A payment session may be created after the Store closes or pauses only for an already placed Order whose payment deadline is still open. New sessions are rejected after that deadline, provider expiry is capped by it, and customer projections no longer offer payment retry. A later verified provider `Paid` webhook remains financial authority and is not discarded because the local deadline passed.
- Configuration-release route authoring accepts `RecipeId` and derives `ProductVariantId` from the recipe before storing both route identities.
- Setting an internal-account password changes credential material only. Enabling local login remains a separate account-policy update.
- Authentication responses contain tokens, minimal identity, role scopes, and enabled login methods. Full profile fields belong to `/me`.
- Kiosk order creation derives `OrderChannel = Tablet` from the endpoint contract. Anonymous clients cannot choose an analytics/audit channel value.
- Deployment command identifiers are internal transport coordination data. Management responses expose deployment identity and status, not `EdgeCommandId`.

### Inventory

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
- Production stock evidence always applies `QuantityConsumed` to the current Cloud estimate and records one matching `CONSUME` movement. `BalanceAfter` is optional. When Cloud already has an estimate, a supplied value must equal `BalanceBefore - QuantityConsumed`; a mismatch rejects the entire execution report. When the estimate is unknown, a valid supplied value establishes the post-consumption estimate; if it is omitted, the estimate and both movement balances remain unknown.
- `StockMovement.SourceEventId` is an immutable evidence identity. A retry with the same dispenser, consumed quantity, order, executor, estimate flag, and supplied post-balance is a no-op; reusing it with different evidence rejects the report.
- `InventoryChanged.EstimatedQuantity` is nullable and preserves an unknown estimate; realtime notifications must not project unknown quantity as zero.
- Refill, estimate adjustment, topology update/status/delete/rebind, device replacement, and execution consumption serialize on the same dispenser-state mutation identity. A mutation must acquire that transaction-scoped lock before loading the mutable state; multi-dispenser reports acquire locks in deterministic dispenser-id order.
- Topology configuration update and status requests require an operator reason. Automatic lifecycle changes use explicit system reason codes rather than an empty audit reason.
- Level-to-quantity mapping supports Edge-reported `Low`, `Medium`, and `Full` only. A non-empty mapping must define all three levels exactly once and quantities must increase strictly in that order. Numeric sensor calibration is not supported in this phase.
- A dispenser state with stock movement history cannot be deleted and must be retired. Retired states reject sensor updates, refill, estimate adjustment, and execution stock evidence. Reactivation requires a non-retired device and active ingredient.
- Inventory estimates still do not decide runtime menu sellability or robot execution availability in V1.
- Reaching a zero estimate updates inventory state and history but does not change V1 topology readiness. Stock thresholds, reservation, and sellability require a separate inventory-availability policy.
- Inventory readiness compares each applicable release Recipe's required ingredients with the target kiosk topology. It returns `Ready`, `MissingIngredient`, `ContainerInactive`, `DeviceUnavailable`, or `CalibrationMissing` per route/ingredient. `CalibrationMissing` means the categorical `Low/Medium/Full` quantity mapping is absent; it does not imply raw sensor calibration support.
- `GET /api/v1/management/kiosks/{kioskId}/configuration-releases/{releaseId}/inventory-readiness` is the operational read model. It is computed from current topology and Recipe requirements; it is not persisted into ConfigurationRelease.
- Release publication and deployment only consume readiness. They never create, reactivate, rebind, or otherwise repair Inventory topology.
- `ProductionInventoryReadiness.PublishPolicy` defaults to `Warn`: publishing succeeds and returns not-ready kiosk details because a reusable release may be published before every kiosk is provisioned. `DeployPolicy` defaults to `Block`: target deployment returns `409` before creating a deployment or EdgeCommand when readiness is not `Ready`.
- Readiness is an operational setup/deployment gate only. Runtime-menu visibility, order creation, inventory reservation, and execution sellability remain unchanged in this phase.

### Operations

- Operations telemetry APIs expose curated heartbeat/event fields only. Do not return raw `PayloadJson` by default.
- Normal telemetry reads also exclude source node ids, heartbeat sequence numbers, and correlation/causation ids. Those ingestion identities remain available to machine contracts.
- Operation-log list/detail reads are kiosk-owned and filter by `deviceId`, `orderId`, `severity`, `from`, and `to`. Their normal response excludes raw payload and sync identities. `GET .../operation-logs/{operationLogId}/diagnostics` exposes only raw payload under `operations.diagnostics`; it is limited to scoped `SystemAdmin` and `Technician` users.
- `DeviceEvent` is immutable log/evidence, not mutable alert state. Newly accepted Error/Critical telemetry creates a separate Open Alert in the same transaction; Warning remains evidence only.
- Alert management uses `/api/v1/management/alerts`: scoped list/get plus acknowledge/resolve. V1 has no general manual create endpoint; alert creation belongs to authenticated telemetry ingestion.
- Maintenance tickets are kiosk-scoped work items with optional evidence links to device, order, device event, or alert. `OperationalImpact` is `None`, `BlocksNewOrders`, or `RequestsEmergencyStop`; starting an impacted ticket atomically moves the kiosk to `Maintenance` or `EmergencyStopRequested`. Resolving or closing a ticket never reopens sales automatically. Configured inventory-empty alert automation may create one linked ticket; chat, reopen, ticket SLA/escalation, and a GraphQL maintenance aggregate remain outside the current contract.

### Robot Configuration, Releases, And Production Packages

- Execution endpoint credential rotation is a maintenance operation. It revokes the current credential binding and activates the new credential reference in one database save. Rotation preserves the endpoint's prior Active or Disabled state; Provisioning and Retired endpoints cannot rotate. Hot credential overlap is not part of V1.
- Robot artifact bulk upload is the only public upload contract. It accepts one to 50 multipart `.lua` files, stores file bytes in S3-compatible object storage, and stores immutable metadata in `RobotArtifact`.
- Bulk robot artifact upload accepts up to 50 files plus a JSON manifest that supplies per-file metadata. Uploaded artifacts remain unassigned Draft inventory and do not change any robot-program sequence. Request-shape errors reject the whole request; upload failures use item-level atomicity and return per-item results without rolling back successful items.
- Robot authoring import is the simplified custom-authoring surface above advanced artifact/program CRUD. Upload accepts one Fairino ZIP plus target scope and requires `Idempotency-Key`; normal FE never sends artifact IDs, contract IDs, storage keys, checksums, or membership IDs. Validation is read-only for authoring resources. Materialization requires both `artifact.upload` and `program.manage`, creates Draft resources only, preserves manifest `RunOrder`, and is serialized by import identity. `POST .../{importId}/publish-resources` is a separate explicit, resumable confirmation that publishes contracts, assigns them, publishes artifacts, then publishes the program. Release attachment and deployment remain separate operations.
- The robot-authoring workspace is a read model, not a write owner. It combines current import progress, existing package ownership, release status, deployment candidates, blockers, and next actions. Package ownership is informational: the workspace does not automatically require a fork. A separate explicit customization workflow decides whether a package-managed resource must be forked before mutation.
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
- Release route authoring also requires an explicit `supportedOptionCodes` collection. Codes must identify unique production-affecting options of the route Product. The immutable route, production-definition checksum, release manifest, runtime-menu filtering, order validation, and dispatch all enforce the same policy.
- Release lists return summaries without manifest JSON or route/binding collections. Release detail remains the review surface for the complete authored graph.
- `GET /management/configuration-deployments` is a read-only global management index for scoped deployment search. Deployment detail and rollback are kiosk-owned routes under `/management/kiosks/{kioskId}/configuration-deployments/...` because deployment affects a physical kiosk execution endpoint.
- Configuration deployment reads unify Full Edge and Low-cost histories behind tenant-scoped, paged management surfaces. Filters include organization, store, kiosk, release, profile, and status for the global index; kiosk-owned reads bind `kioskId` from the route. Profile-specific provenance remains nullable rather than being discarded.
- Deployment preview is kiosk-owned and read-only. Full Edge preview always includes the complete release and rejects route/program selections because the Full Edge command installs the whole release. Low-cost preview may accept route/program selections; otherwise it derives the only binding for each route and reports `ProgramSelectionRequired` for ambiguous routes. It evaluates active endpoint identity, readiness, safety/activity, reported capabilities, robot target compatibility, inventory policy, low-cost capacity, immutable artifact totals, installation modes, risk acknowledgement, and a deterministic preview checksum. It never creates a deployment, Edge command, presigned URL, or Full Edge bundle.
- Configuration rollback selects a previously Active deployment, then creates a new profile-matching deployment and durable command. It does not mutate or reactivate the historical deployment row. Retired releases are eligible only through this validated rollback path.
- The complete Fairino export-to-deployment sequence is owned by [Robot Lua Artifact Flow](../flows/ROBOT_LUA_ARTIFACT_FLOW.md); keep this document focused on route and client boundaries.
- Production package installation is the default simplified franchise contract. It materializes organization-owned Catalog, artifact, program, route, and Draft release resources without accepting technical IDs or ordering fields from normal FE. Existing artifact/program/release authoring endpoints remain an advanced technical surface. See [Production Package Installation Flow](../flows/PRODUCTION_PACKAGE_INSTALLATION_FLOW.md).
- Option-specific robot artifacts use `RequiredOptionCode` in program/release/Edge manifests. The Edge runtime skips the artifact when the order line does not select that option; this is file selection, not parameter injection into Lua.
- Production package V1 requires option codes to be unique within each packaged Product and exactly one required capability code per route. Package replace, publish, preview, and install share the same deterministic validation contract.
- Package management includes package update/retirement and a version-definition read. The definition read returns the complete replaceable Products, artifact sources, program slots, and routes so authoring clients do not reconstruct a PUT payload from metadata.
- Organization installation history is available from `GET /management/organizations/{organizationId}/production-package-installations` with status, store, kiosk, and paging filters.
- Each package route identifies `productSourceKey + productVariantSourceKey + recipeSourceKey`, so recipe codes need to be unique only inside one variant. Recipe materialization source keys include the Product Variant code to preserve that scope. `supportedOptionCodes` explicitly lists production-affecting options supported by that route; an empty list means none. Commercial-only options are never listed. Route programs cannot declare option effects outside this policy.
- `GET /management/organizations/{organizationId}/production-package-installations/{installationId}/workspace` is the package-oriented aggregate read model for one FE workspace. It separates `technicalReadiness` from `commercialReadiness` and returns `requiredActions`, `optionalActions`, and `recoveryActions`. Publish, availability, menu assignment, release, and deployment writes continue through their existing command endpoints.
- Workspace actions are typed guidance only. Their structured context carries required parent resource IDs and, for deployment, the compatible endpoint/profile and low-cost route/program selections. Menu context separates assigned variant IDs from currently sellable variant IDs, so an existing inactive assignment produces deterministic activation/review guidance rather than duplicate assignment. Writes still use the owning command APIs. Installation-specific writes are `retry`, which reuses the persisted selection and original idempotency identity; `fork`, which changes technical ownership and copy-on-writes RobotArtifacts still shared with another package-managed installation when referencing programs remain Draft; and `repair`, which restores soft-deleted materialization targets under their original identities. Fork never rewrites a Published program manifest; later customization of published resources uses a new Draft program/release.
- Definition-changing package-managed technical recovery first requires `POST /management/organizations/{organizationId}/production-package-installations/{installationId}/fork`. Those recovery actions remain blocked with `PackageForkRequired` until ownership changes to `OrganizationFork`. In-place soft-delete repair preserves package ownership and does not require a fork; commercial availability and menu operations also do not require a fork.
- Package installation reuses an exact RobotArtifact only when an Installed or Superseded package-managed installation already owns it and object size/checksum validation succeeds. An organization-authored artifact with the same natural identity returns `409`; package installation never converts that artifact into package ownership implicitly.
- Package-managed Product and ProductVariant technical identity and child structure are immutable until fork. Product/Variant code and technical classification, adding Variants/Recipes/OptionGroups/Options, and changing OptionGroup selection requirements are definition changes. Commercial names, descriptions, prices, availability, display order, images, and menu placement remain mutable.
- `POST /management/organizations/{organizationId}/production-package-installations/{installationId}/repair` restores verified soft-deleted targets for an Installed package-managed installation. It never creates a replacement installation or changes materialization target identities. Workspace and repair derive the same expected materialization set from the immutable package version and persisted product selection; Configuration Release evidence must target the installation's exact `DraftConfigurationReleaseId`. The store validates all targets before writing and restores them atomically. Physical deletion, tenant/scope mismatch, unsupported identity, missing materialization evidence, and restore constraint conflicts return `409`; `details.issues` identifies each affected resource for operator/support handling. Repeating repair after success is a successful no-op.
- Production package version upgrade remains nested under its source installation: `.../{installationId}/upgrades/...`. Preview, paged history, and detail use `package.read`; execute, cutover, and abandon use `package.install`; rollback uses `release.rollback`. Execute requires `Idempotency-Key` plus the exact preview checksum. Abandon accepts only `ReadyForReview` or `Failed`, requires an operator reason, preserves source/audit evidence, and is idempotent. Rollback requires an operator reason; detail exposes typed menu evidence, frozen endpoints, current rollback observation, and rollback-attempt audit. FE never submits successor database IDs, staging codes, menu rollback snapshots, endpoint deployment IDs, or field-ownership choices. Backend derives and persists those as typed evidence. Publication and deployment remain separate existing APIs. Cutover requires package-managed source/successor ownership and an exact Active deployment row for every frozen endpoint; the row must match tenant, kiosk, endpoint, profile, and successor release. Fork is blocked while either installation participates in a `Materializing`, `ReadyForReview`, or `RollbackPending` upgrade. Forking the successor after completed cutover invalidates package rollback rather than allowing rollback to overwrite organization-owned changes.
- Workspace blockers carry typed readiness impact (`Technical`, `Commercial`, or `Both`); the two readiness projections are evaluated independently rather than partitioning blocker codes. Required option-group availability is grouped by stable `OptionGroupId`.
- A required option group below `MinSelections` produces one `RestoreRequiredOptionGroupAvailability` action with `requiredCount` and candidate option IDs. Individual `EnableOption` actions remain optional choices.
- Technical workspace readiness means the release is published, compatible with an active endpoint, inventory topology satisfies base Recipe and required-option-group requirements, and the release is Active on the kiosk. `latestDeploymentStatus` is not treated as informational-only readiness evidence.
- Robot artifact technical contracts are typed, versioned publication records. Metadata JSON and Lua file names are not behavior authority. Normal artifact/template responses expose whether a contract reference is assigned, not whether the referenced contract is currently publishable; publish and clone commands perform authoritative status, checksum, scope, and compatibility validation. Parameterized quantity remains unavailable until the Edge runtime contract consumes it.
- Technical-contract lists are paged and accept `status` and `search`. Organization-scoped lists follow normal tenant ownership. For the global catalog, `SystemAdmin` can inspect all lifecycle states while `OrgAdmin` can read only Published contracts; direct global Draft/Retired reads return not found.
- Deployment validation preview returns a checksum and residual-risk warnings. Deploy requests must echo the current checksum and organization acknowledgement; acknowledgement cannot override objective compatibility, integrity, effect, quantity, ordering, capability, or topology failures.
- Publish commands do not deploy to an execution endpoint.
- Full Edge deployment requests create a durable deployment command for the immutable release. Edge validates downloaded content and reports installation and activation separately.
- Low-cost deployment requests select unique route/program pairs. Backend includes every ordered artifact in each selected published program and enforces the configured controller capacity.
- Normal Full Edge and low-cost deployments require a Published configuration release. A Retired release may be deployed only through the validated rollback endpoint; callers cannot use the normal deployment routes to bypass retirement.
- Full Edge deploy, Low-cost deploy, and rollback requests require `Idempotency-Key`. The key is durable and unique per execution endpoint. An exact retry returns the existing deployment and command; reusing a key for a different release or Low-cost selection returns `409`.
- Robot program, configuration release, and deployment read routes use `program.read`, `release.read`, and `deployment.read`. Authoring/publishing/deployment commands retain their narrower mutation policies.
- When a deployment command expires before acceptance, its still-Pending deployment becomes `Failed/CommandExpired`.
- An accepted deployment that receives no installation report before its deadline becomes `Failed/ExecutionReportTimeout`.
- Artifact bytes are not exposed through a public REST download endpoint. After execution-endpoint authentication, command pull enriches deployment artifact descriptors with short-lived object-storage read URLs. These URLs are not durable API identifiers and must not be stored as release state.

## Related Docs

- [API Surface Rules](API_SURFACE_RULES.md)
- [Authorization Rules](AUTHORIZATION_RULES.md)
- [Multi-Tenancy Rules](../architecture/MULTI_TENANCY_RULES.md)
- [System Flows](../flows/SYSTEM_FLOWS.md)
