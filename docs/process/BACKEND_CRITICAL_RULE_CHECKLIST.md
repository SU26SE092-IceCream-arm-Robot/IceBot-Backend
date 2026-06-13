# Backend Critical Rule Checklist

Use this checklist before backend handoff or deployment smoke testing when automated tests do not yet cover the rule.

## Search Keywords

`critical rule checklist`, `manual backend verification`, `maintenance lifecycle`, `refund status`, `kiosk active sales`, `enum string`, `accounts read manage`, `payment webhook`

## Maintenance Ticket Lifecycle

Verify:

- `Open -> Assigned` succeeds.
- `Open -> InProgress` succeeds.
- `Assigned -> InProgress` succeeds.
- `InProgress -> Resolved` succeeds.
- `Resolved -> Closed` succeeds.
- `Open/Assigned/InProgress -> Cancelled` succeeds with a reason.
- `Resolved -> Cancelled` fails.
- `Closed -> Resolved` fails.
- `InProgress -> Assigned` fails.
- `Cancelled -> any other status` fails.

## Refund Status Behavior

Verify:

- Marking a paid order refund-required sets order support state without provider refund integration.
- Requesting a refund uses `Idempotency-Key`.
- Processing `FullMoneyRefund` sets payment status to `Refunded` only when staff confirms actual money refund.
- Processing voucher compensation does not set payment status to `Refunded`.
- Rejecting or cancelling a refund keeps the order in `RefundRequired`.

## Kiosk Online Sales

Verify:

- New online order/payment session is allowed only when `KioskStatus.Active`.
- `Offline`, `Maintenance`, `Suspended`, and `Inactive` do not allow new Cloud online sales.
- Offline sale support is not controlled by `KioskStatus.Offline`; it requires a future offline session/capability.

## Enum Inputs

Verify:

- JSON request body enum values are accepted as strings.
- JSON request body enum integer values are rejected.
- Query string enum filters return `400` for invalid values.

## Account Authorization

Verify:

- `accounts.read` allows `SystemAdmin`, `OrgAdmin`, and `Manager`.
- `accounts.manage` allows `SystemAdmin` only.
- Scoped account reads are filtered for non-`SystemAdmin`.
- Account mutations remain denied for `OrgAdmin` and `Manager`.

## Payment Webhook

Verify:

- Invalid PayOS signature returns a failure response and does not mark payment paid.
- Duplicate webhook callback is treated as already processed.
- Late cancelled/expired callback does not override an already paid order.
- Missing PayOS config returns service-unavailable behavior instead of leaking secrets.
