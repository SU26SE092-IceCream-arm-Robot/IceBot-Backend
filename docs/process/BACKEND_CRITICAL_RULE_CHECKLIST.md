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

- New online order/payment session is allowed only when lifecycle is `KioskStatus.Active` and connectivity is `Online` or `Degraded`.
- `Maintenance`, `Disabled`, `Retired`, `Unknown` connectivity, and `Unreachable` connectivity do not allow new Cloud online sales.
- Offline sale support requires a future offline session/capability; it is not inferred from lifecycle or connectivity fields.

## Order Item Fulfillment

Verify:

- Manual items alone accept strict `Pending -> Accepted -> Preparing -> Completed` transitions and require a reason when failed.
- Packaged items use only the packaged commands: idempotent `Pending -> Completed` through `fulfill`, or `Pending -> Failed` through `fail` with a required reason. They never enter `Accepted` or `Preparing`.
- Machine-produced items reject management fulfillment commands and advance only from authenticated execution reports.
- A mixed order completes only after every item is completed.
- A failed paid item creates `FulfillmentIssue`; remaining items may still progress and whole-order refund is never inferred automatically.
- Manual and packaged fulfillment writes require `fulfillmentEventId`; same id and same payload is idempotent, while payload mismatch is rejected.
- V1 manual/packaged transitions apply to the entire order line quantity; partial line fulfillment is unsupported.
- Packaged options are `CommercialOnly`; machine-produced production-affecting options require route support.
- Customer order responses do not expose backend-only `FulfillmentType`.

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
