# API Error Contract

## Search Keywords

`ApiResult`, `API error`, `status code`, `503`, `dependency unavailable`,
`validationErrors`, `businessError`, `systemError`, `X-Correlation-ID`,
`retry`, `mutation state`

## Purpose

This document defines the public error semantics for REST endpoints. It keeps
WebApp, Tablet, and external integrations from inferring behavior from localized
messages, provider exceptions, or controller-specific response shapes.

## Envelope

Controller-facing handlers return `ApiResult<T>` and controllers preserve its
HTTP status code. The envelope may contain `succeeded`, `statusCode`, `message`,
`data`, `details`, `validationErrors`, `businessError`, and `systemError`.

- The HTTP status and `statusCode` must match.
- Consumers branch on the HTTP status and a documented `businessError` code.
  They must not parse `message`.
- `validationErrors` identifies request fields when request-shape validation
  fails. It is not a substitute for a business conflict code.
- `businessError` is an optional stable string code, not display text. It is
  required when the documented client behavior differs within the same HTTP
  status, including idempotency conflicts, stale state, sales admission,
  payment lifecycle, and retryable dependencies.
- `details` is consumable only when this document or an owning flow explicitly
  documents its keys for the accompanying `businessError` code.
- `validationErrors` is for deterministic request-shape correction. It may be
  returned without `businessError`.
- `systemError` is a reserved envelope field and is always `null` in public
  responses. Exception messages, exception types, stacks, provider payloads,
  secrets, credentials, connection strings, and tokens remain in protected
  logs only.
- Every response passes the `X-Correlation-ID` header. Support uses it to find
  protected diagnostics; APIs must not duplicate diagnostic values in `details`.

## Stable Error Code Rules

Codes use uppercase dotted context ownership:

```text
<CONTEXT>.<CONDITION>
```

Examples: `ORDER.IDEMPOTENCY_CONFLICT`,
`SALES.KIOSK_CONNECTIVITY_UNAVAILABLE`, and `PAYMENT.WINDOW_EXPIRED`.
The owning bounded context defines each public error as a typed definition with
one code, HTTP status, and safe default message. Code must never be inferred by
parsing a message or exception text. Adding or changing a public code is an API
contract change.

## Status Semantics

| Status | Meaning | Consumer action |
| --- | --- | --- |
| `400` | Request shape or validation is invalid. | Correct input; do not retry unchanged. |
| `401` | Authentication is absent, expired, or invalid. | Reauthenticate. |
| `403` | Actor lacks permission or effective scope. | Do not retry; request access if appropriate. |
| `404` | Resource is absent or intentionally not visible in scope. | Refresh the owning view; do not infer cross-tenant existence. |
| `409` | Current lifecycle, revision, duplicate, or idempotency state conflicts. | Refresh authoritative state before a new action. |
| `423` | Account or resource is temporarily locked. | Wait for the documented unlock condition. |
| `429` | Rate limit exceeded. | Retry only after the advertised delay. |
| `503` | An enabled feature cannot complete because its external dependency is unavailable or safely rejected the operation. | Preserve input, clear terminal mutation state, and allow an explicit retry. |
| `500` | Unexpected backend defect. | Show a generic failure and record correlation information for support. |

`503` is not a generic replacement for validation, authorization, or an
authoritative business decision. It is only for a temporary dependency failure
at the owning feature boundary. A provider's authoritative rejection uses
`502`; unexpected exceptions use a sanitized `500` response.

## Commerce Error Codes

The following table owns the current runtime-menu, checkout, and payment-session
contract. Unlisted simple scoped `404` results have no code.

| Code | HTTP | Client action | Allowed details |
| --- | --- | --- | --- |
| `ORDER.IDEMPOTENCY_KEY_INVALID` | 400 | Generate a valid key before retrying. | None |
| `ORDER.IDEMPOTENCY_CONFLICT` | 409 | Do not reuse the key for different order content. | None |
| `ORDER.CLIENT_ORDER_ID_CONFLICT` | 409 | Generate a new client order identity. | None |
| `ORDER.CLIENT_TOTAL_MISMATCH` | 409 | Refresh cart total before retrying. | `clientTotalAmount`, `calculatedTotalAmount` |
| `ORDER.CURRENCY_MISMATCH` | 400 | Correct the cart. | None |
| `ORDER.OPTION_SELECTION_INVALID` | 409 | Refresh menu options and correct the selection. | None |
| `ORDER.PACKAGED_OPTION_UNSUPPORTED` | 409 | Correct the selected option. | None |
| `PAYMENT.IDEMPOTENCY_KEY_INVALID` | 400 | Generate a valid key before retrying. | None |
| `PAYMENT.IDEMPOTENCY_CONFLICT` | 409 | Do not reuse the key for different payment content. | None |
| `PAYMENT.WINDOW_EXPIRED` | 409 | Stop payment and refresh the order. | None |
| `PAYMENT.PREVIOUS_SESSION_FAILED` | 409 | Retry with a new key. | None |
| `PAYMENT.SESSION_CREATION_IN_PROGRESS` | 409 | Wait, then retry with the same key. | None |
| `PAYMENT.ORDER_ALREADY_PAID` | 409 | Refresh payment/order state. | None |
| `PAYMENT.ORDER_NOT_PAYABLE` | 409 | Stop payment and refresh the order. | None |
| `PAYMENT.AMOUNT_CHANGED` | 409 | Refresh the authoritative amount. | `expectedAmount`, `orderAmount`, `expectedCurrency`, `orderCurrency` |
| `PAYMENT.METHOD_NOT_CONFIGURED` | 503 | Stop and retry after operator configuration. | None |
| `PAYMENT.METHOD_INACTIVE` | 503 | Stop and retry after operator activation. | None |
| `PAYMENT.PROVIDER_OUTCOME_UNKNOWN` | 503 | Wait for reconciliation, then retry with the same key. | None |
| `PAYMENT.PROVIDER_UNAVAILABLE` | 503 | Retry with the same key after backoff. | None |
| `PAYMENT.PROVIDER_REJECTED` | 502 | Stop automatic retry and show a safe failure. | None |
| `PAYMENT.RECONCILIATION_NOT_ELIGIBLE` | 409 | Refresh the payment session before requesting reconciliation. | None |
| `PAYMENT.WEBHOOK_PAYLOAD_INVALID` | 400 | Provider must correct the payload; no local payment mutation occurred. | None |
| `PAYMENT.WEBHOOK_VERIFICATION_FAILED` | 400 | Provider signature verification failed; no local payment mutation occurred. | None |
| `PAYMENT.WEBHOOK_CONFIGURATION_UNAVAILABLE` | 503 | Provider should retry after the operator restores webhook verification configuration. | None |

`SALES.*` codes come exclusively from `SalesAdmissionBlockerCode`. They use the
same stable code and safe definition message in runtime-menu
`admission.blockers[].code` / `admission.blockers[].message` and failed order or
payment responses. The public blocker precedence is:

```text
OrganizationInactive, StoreInactive, KioskInactive, KioskOperationalHold,
StoreSalesPaused, StoreClosed, KioskConnectivityUnavailable,
CustomerSessionOccupied, MenuItemPaused, CatalogUnavailable,
ProductionRouteUnavailable, InventoryMissing, InventoryInactive,
InventoryDeviceUnavailable, InventoryCalibrationMissing, InventoryExpired,
InventoryEvidenceStale, InventoryUnitMismatch,
InventoryQuantityUnavailable, InventoryInsufficient
```

The earliest applicable item above is the public primary blocker, independent
of evaluator collection order. Payment preserves that upstream `SALES.*` cause
when sales admission owns the decision.

| Code | HTTP when blocking checkout/payment | Client action |
| --- | --- | --- |
| `SALES.ORGANIZATION_INACTIVE` | 409 | Stop and contact the organization operator. |
| `SALES.STORE_INACTIVE` | 409 | Stop and contact the store operator. |
| `SALES.STORE_SALES_PAUSED` | 409 | Wait for store sales to resume. |
| `SALES.STORE_CLOSED` | 409 | Wait for the store to open. |
| `SALES.KIOSK_INACTIVE` | 409 | Stop and contact the kiosk operator. |
| `SALES.KIOSK_OPERATIONAL_HOLD` | 409 | Wait for kiosk recovery. |
| `SALES.KIOSK_CONNECTIVITY_UNAVAILABLE` | 409 | Wait, refresh the runtime menu, then retry. |
| `SALES.CUSTOMER_SESSION_OCCUPIED` | 409 | Wait for the current customer session to finish. |
| `SALES.MENU_ITEM_PAUSED` | 409 | Refresh menu and choose another item. |
| `SALES.CATALOG_UNAVAILABLE` | 409 | Refresh menu and choose another item. |
| `SALES.PRODUCTION_ROUTE_UNAVAILABLE` | 409 | Refresh menu and choose another item. |
| `SALES.INVENTORY_MISSING` | 409 | Refresh menu; operator must configure inventory. |
| `SALES.INVENTORY_INACTIVE` | 409 | Refresh menu; operator must activate inventory. |
| `SALES.INVENTORY_DEVICE_UNAVAILABLE` | 409 | Wait for operational recovery, then refresh. |
| `SALES.INVENTORY_CALIBRATION_MISSING` | 409 | Stop; operator must complete calibration. |
| `SALES.INVENTORY_EXPIRED` | 409 | Refresh menu; operator must replace expired inventory. |
| `SALES.INVENTORY_EVIDENCE_STALE` | 409 | Wait for fresh inventory evidence, then refresh. |
| `SALES.INVENTORY_UNIT_MISMATCH` | 409 | Stop; operator must correct inventory configuration. |
| `SALES.INVENTORY_QUANTITY_UNAVAILABLE` | 409 | Wait for fresh inventory evidence, then refresh. |
| `SALES.INVENTORY_INSUFFICIENT` | 409 | Refresh menu; operator must refill inventory. |

Identity scope errors are also stable public codes:
`IDENTITY.ORGANIZATION_SUSPENDED`, `IDENTITY.ORGANIZATION_INACTIVE`, and
`IDENTITY.ORGANIZATION_ACCESS_UNAVAILABLE`. They return `403`; the client must
stop and request an active organization scope.

## Client Follow-Up

WebApp and Flutter keep `businessError` as a nullable string. They must map the
documented commerce codes to refresh, wait, retry-with-same-key,
retry-with-new-key, or stop states. They must retain a safe generic fallback for
an unknown or absent code and must not branch on `message` text. This Backend
slice does not change client code or the JSON type of `businessError`.

## Provider Failure Rules

- Provider timeout, transport, malformed success response, and transient
  provider response are classified as either outcome-unknown or unavailable by
  the owning payment boundary. Authoritative provider rejections are distinct.
- The failure must not make unrelated API routes, authentication, catalog reads,
  or readiness fail.
- Public responses use a safe message and documented application code. Detailed
  provider diagnostics stay in protected logs or diagnostics.
- A command that may be retried must preserve its idempotency behavior. Do not
  issue a second provider side effect merely because the caller received `503`.
- A client mutation enters a terminal error state for `400`, `403`, `404`,
  `409`, `423`, and `503`; it must not remain disabled indefinitely after the
  response completes.

## Payment Webhook Rules

- An empty request body is request validation and returns `400` with
  `validationErrors.rawPayload`.
- A non-empty malformed payload returns
  `PAYMENT.WEBHOOK_PAYLOAD_INVALID`; an invalid or missing signature returns
  `PAYMENT.WEBHOOK_VERIFICATION_FAILED`. Neither path reads or mutates payment
  state.
- Missing backend checksum configuration returns
  `PAYMENT.WEBHOOK_CONFIGURATION_UNAVAILABLE` with `503` so the provider can
  retry. Unexpected failures return a sanitized `500` for the same reason.
- Once provider verification succeeds, duplicate event identity, a callback
  previously recorded as ignored, an unmatched local transaction, or a
  settlement validation conflict is acknowledged with `2xx` after durable
  handling where a local transaction exists. This avoids retry storms. These
  conditions are bounded operational diagnostics and are not public business
  errors; raw payloads and signatures are never logged.

## Related Docs

- [API Surface Rules](API_SURFACE_RULES.md)
- [Idempotency And Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
- [Dependency Rules](../architecture/DEPENDENCY_RULES.md)
- [Startup And Bootstrap Rules](../operations/STARTUP_AND_BOOTSTRAP_RULES.md)
