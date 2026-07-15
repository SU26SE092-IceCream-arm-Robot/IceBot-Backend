# Tablet and Cloud Contract

This document owns the API and state contracts between the tablet, Cloud checkout/payment services, and Cloud customer status projection.

## Search Keywords

`tablet`, `runtime menu`, `sales catalog`, `place order`, `payment session`, `payment callback`, `customer status`, `QR payment`, `order status`

## Tablet To Local Edge

### Get Runtime Menu Projection

```http
GET /api/v1/local/runtime-products?kioskId={kioskId}
```

Purpose: return the menu that can currently be sold from this kiosk.

Response:

```json
{
  "snapshotId": "uuid",
  "kioskId": "uuid",
  "generatedAt": "2026-05-21T10:00:00Z",
  "expiresAt": "2026-05-21T10:00:15Z",
  "runtimeStateTimestamp": "2026-05-21T09:59:59Z",
  "machineAvailable": true,
  "products": [
    {
      "productId": "uuid",
      "productVariantId": "uuid",
      "menuItemId": "uuid",
      "productCode": "VANILLA_CUP",
      "productVariantCode": "M",
      "displayName": "Vanilla Cup",
      "sizeCode": "M",
      "price": 25000,
      "currency": "VND",
      "available": true,
      "unavailableReason": null,
      "recipeId": "uuid",
      "recipeVersion": 3,
      "estimatedIngredientLevels": [
        {
          "ingredientId": "uuid",
          "ingredientCode": "VANILLA_MIX",
          "levelStatus": "Medium"
        }
      ]
    }
  ]
}
```

Projection inputs:

- Menu item snapshot.
- Product variant snapshot.
- Product snapshot.
- Recipe snapshot.
- `IngredientDispenserState`.
- Device state.
- Robot availability.
- Availability policy.

This response is a quote for UX, not a reservation.

## Tablet To Cloud

### Get Kiosk Sales Catalog Snapshot

```http
GET /api/v1/kiosks/{kioskId}/runtime-menu
```

Purpose: return the Cloud Sales Catalog snapshot that is currently sellable for a kiosk.

This endpoint is useful when the tablet needs a Cloud-backed menu snapshot, but it is not a replacement for the Local Edge runtime projection. It does not include live machine availability, ingredient sufficiency, robot status, or local queue state. Read-model boundaries and data exclusions for this endpoint are documented in [API Surface Rules](../api/API_SURFACE_RULES.md#read-model-api-boundaries).

Response:

```json
{
  "snapshotId": "uuid",
  "kioskId": "uuid",
  "generatedAt": "2026-05-21T10:00:00Z",
  "expiresAt": "2026-05-21T10:00:15Z",
  "availabilitySource": "CloudSalesCatalog",
  "containsMachineRuntimeState": false,
  "items": [
    {
      "menuId": "uuid",
      "menuItemId": "uuid",
      "productId": "uuid",
      "productVariantId": "uuid",
      "recipeId": "uuid",
      "menuItemCode": "VANILLA_CUP_M",
      "productCode": "VANILLA_CUP",
      "productVariantCode": "M",
      "displayName": "Vanilla Cup",
      "sizeCode": "M",
      "price": 25000,
      "discountAmount": 0,
      "finalPrice": 25000,
      "currency": "VND",
      "preparationTimeSeconds": 90,
      "imageUrl": null,
      "recipeVersion": 3
    }
  ]
}
```

Rules:

- Use this endpoint only for Cloud Sales Catalog truth.
- Cloud online sales require `KioskStatus.Active`, active parent tenant scope, and connectivity `Online` or `Degraded`.
- Lifecycle and connectivity are separate contracts; `Unreachable` is not a `KioskStatus` value.
- Offline-created orders may be synchronized later only if they were created under a valid offline sales session issued while the kiosk was active and offline sales was enabled.
- For final runtime availability before checkout, the tablet should still prefer the Local Edge runtime projection when the edge service is available.
- `snapshotId` identifies the runtime-menu response for client cache/debug purposes. Order creation does not accept it as authority; Cloud always reloads the selected menu items and recalculates prices.

### Create Order

```http
POST /api/v1/orders
```

Headers:

```text
Idempotency-Key: create-order:{clientOrderId}
X-Correlation-Id: {correlationId}
```

Request:

```json
{
  "kioskId": "uuid",
  "clientOrderId": "tablet-order-uuid",
  "items": [
    {
      "clientLineId": "uuid",
      "menuItemId": "uuid",
      "quantity": 1,
      "selectedOptions": [
        {
          "productOptionId": "uuid"
        }
      ]
    }
  ],
  "clientTotalAmount": 25000
}
```

`selectedOptions` contains IDs returned by that runtime menu item. Each option may appear at most once in V1. Cloud validates required groups, single/multiple cardinality, availability, menu membership, currency, and price delta. Cloud stores immutable option snapshots and sends typed selected-option snapshots to Edge; arbitrary client JSON is never forwarded.

Response:

```json
{
  "orderId": "uuid",
  "orderAccessToken": "bearer-capability",
  "orderNumber": "ORD-20260521-0001",
  "customerStatus": "WaitingForPayment",
  "customerStatusMessage": "Waiting for payment. Please scan the QR code.",
  "canRetryPayment": true,
  "requiresStaffSupport": false,
  "totalAmount": 25000,
  "currency": "VND"
}
```

Cloud creates:

- `Order`
- `OrderItem`

Cloud must calculate price from backend Sales Catalog `MenuItem.Price`. Tablet totals are used only for comparison and conflict detection.

### Create Payment Session

```http
POST /api/v1/orders/{orderId}/payment-sessions
```

Headers:

```text
Idempotency-Key: payment-session:{orderId}
Order-Access-Token: {orderAccessToken}
X-Correlation-Id: {correlationId}
```

Request:

```json
{
  "paymentMethodCode": "payos",
  "expectedAmount": 25000,
  "expectedCurrency": "VND"
}
```

Response:

```json
{
  "orderId": "uuid",
  "paymentTransactionId": "uuid",
  "checkoutUrl": "https://provider-checkout-url",
  "qrCodePayload": "provider-qr-payload",
  "expiresAt": "2026-05-21T10:05:00Z"
}
```

Cloud creates:

- `PaymentTransaction`
- provider payment session

Do not create `RobotJob` at this stage.

## Provider To Cloud

### Payment Callback

Provider callback is provider-specific and should be handled by the Payments context.

Cloud must:

- Verify signature/provider authenticity.
- Deduplicate provider event by provider event id.
- Update `PaymentTransactionStatus`.
- Set `OrderStatus = ReadyForFulfillment` only after verified payment.
- Commit payment/order state before notifying Tablet or Edge.
- Emit a durable domain/application event after commit, such as `PaymentSucceeded` or `OrderReadyForFulfillment`.

Cloud must not:

- Block the provider webhook response while waiting for Edge acceptance.
- Let Tablet notification depend on Edge dispatch success.
- Create robot runtime state in the payment webhook transaction.

After commit, independent flows run:

```text
Paid order committed
  -> Tablet status notification
  -> ExecuteOrder dispatch attempt 1
  -> reconciliation of a missing initial command
```

The dispatch handler selects exactly one active execution endpoint whose observed active release or low-cost artifact set covers every machine-produced order line. It resolves each line to a release route and ordered robot-program bindings before creating the durable command. Zero matching endpoints defers dispatch; multiple matching endpoints are rejected as ambiguous rather than selected implicitly.

The command identity is `(OrderId, DispatchAttemptNo)`. Repeating the same attempt returns the existing command. The reconciliation worker creates only missing attempt `1`; it does not invent a new attempt after Edge rejection. Command expiry and the active-command admission limit are configured independently from delivery retries. Payment remains paid when dispatch fails because the provider-confirmed payment transaction has already committed.

## Cloud To Tablet Status

Tablet needs fast feedback after the customer pays. Cloud supports this through polling `GET /api/v1/orders/{orderId}` or `GET /api/v1/orders/{orderId}/payment-status` every 2-3 seconds. Both requests send `Order-Access-Token` received from order creation; payment-session creation and customer cancellation use the same header.

Raw order/payment state-machine enums are not serialized by the customer polling contracts. The tablet client consumes the following projected fields on `OrderResult` and `PaymentStatusResult`:

- `CustomerStatus` (string code)
- `CustomerStatusMessage` (client-facing fallback message; frontend may localize by `CustomerStatus`)
- `CanRetryPayment` (boolean indicator)
- `RequiresStaffSupport` (boolean indicator)

Tablet screen mapping based on projections (v1):

| CustomerStatus | CanRetryPayment | RequiresStaffSupport | CustomerStatusMessage | Tablet screen / action |
| --- | --- | --- | --- | --- |
| `WaitingForPayment` | true | false | Waiting for payment. Please scan the QR code. | QR payment screen |
| `PaymentCancelled` | true | false | Payment was cancelled. You can try paying again. | QR payment screen + retry |
| `PaymentExpired` | true | false | Payment session expired. Please retry. | QR payment screen + retry |
| `PaymentFailed` | true | false | Payment failed. You can try paying again. | QR payment screen + retry |
| `Preparing` | false | false | Payment successful. Preparing your order. | Payment successful, preparing order |
| `Ready` | false | false | Your order is ready. Please pick it up! | Ready / pick up |
| `Completed` | false | false | Order completed. Thank you! | Completed |
| `Cancelled` | false | false | Order cancelled. | Order cancelled / aborted |
| `RefundRequired` | false | true | Order cancelled after payment. Please contact staff... / Order execution failed... | Staff support / manual refund required |


## Related Docs

- [IoT Contract](IOT_CONTRACT.md)
- [Checkout Execution Flow](../flows/CHECKOUT_EXECUTION_FLOW.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
