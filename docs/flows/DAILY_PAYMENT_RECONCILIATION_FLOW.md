# Daily Payment Reconciliation

## Purpose

This read-only workflow compares IceBot's local payment facts with a later
PayOS payment-request lookup. It helps an OrgAdmin or scoped Manager identify
payment records needing intervention. It is not provider payout or bank
settlement reconciliation.

```text
Signed PayOS webhook
  -> applies the authoritative local payment state

Payment observation worker
  -> queries known provider payment requests later
  -> appends normalized, sanitized evidence

Reconciliation API
  -> compares local primary settlements against latest fresh evidence
  -> returns daily totals and discrepancies
```

An observation never changes an order, payment transaction, refund, or robot
command. A signed callback remains the only provider event that can apply a
payment to order fulfillment.

## Access

`payments.reconciliation.view` is assigned to:

- `OrgAdmin` within its organization;
- `Manager` only within its assigned organization/store/kiosk scope.

The endpoints do not grant SystemAdmin tenant transaction detail. Platform-wide
aggregate sales reporting remains a separate privacy-bounded capability.

## API

```http
GET /api/v1/management/payment-reconciliation/daily
  ?date=2026-08-21
  &organizationId={optional}
  &storeId={optional}
  &kioskId={optional}
  &provider=PayOS

GET /api/v1/management/payment-reconciliation/discrepancies
  ?date=2026-08-21
  &organizationId={optional}
  &storeId={optional}
  &kioskId={optional}
  &provider=PayOS
  &pageNumber=1
  &pageSize=50
```

Both endpoints use `Payments:DailyReconciliation:TimeZoneId` to convert the
requested local day into a half-open UTC interval. The current implementation
uses the configured platform time zone; organization-specific time-zone history
is not part of this version.

Daily totals keep the channels separate:

- `ExpectedProviderCollectedAmount`: locally applied primary provider payments;
- `CashCollectedAmount`: locally confirmed primary cash payments;
- `ProcessedMoneyRefundAmount`: processed `FullMoneyRefund` rows only;
- `ExpectedNetCollectedAmount`: provider collection plus cash minus money refunds;
- `ProviderConfirmedCollectedAmount`: only returned when every required primary
  provider transaction has fresh successful lookup evidence.

Voucher compensation is not a monetary refund and does not reduce collection
totals. Duplicate provider payments do not inflate expected collection and are
returned as a discrepancy until the existing refund workflow resolves them.

## Status And Limits

`IncompleteEvidence` has precedence over `Mismatch`. A provider timeout,
missing lookup result, or stale observation is evidence that is incomplete, not
a zero-value provider result. `Mismatch` requires fresh evidence that conflicts
with local state or amount.

The first implementation can detect known payment-request inconsistencies. It
cannot discover provider-only transactions, confirm provider refund completion,
or prove money has settled to a bank account. Those capabilities require a
provider statement or payout ingestion contract.

## Search Keywords

`payment reconciliation`, `PaymentProviderObservation`, `payments.reconciliation.view`,
`PayOS lookup`, `daily collection`, `money refund`, `duplicate payment`.

## Related Docs

- [Payment API surface](../api/MANAGEMENT_API_SURFACE.md)
- [Checkout and execution flow](CHECKOUT_EXECUTION_FLOW.md)
- [Payment reconciliation implementation plan](../../.project-memory/DAILY_PAYMENT_RECONCILIATION_IMPLEMENTATION_PLAN.md)
