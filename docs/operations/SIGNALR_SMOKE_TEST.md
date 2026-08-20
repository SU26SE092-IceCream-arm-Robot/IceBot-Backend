# SignalR Manual Smoke Test Workflow

## Search Keywords

`SignalR smoke test`, `OrderHub`, `OperationsHub`, `ManagementDashboardHub`, `JWT`, `JoinOrder`, `JoinKiosk`, `JoinDashboard`, `ExecutionReadinessChanged`

This guide provides steps to manually verify that the SignalR realtime surface is working correctly.

## 1. Start the Backend

Start the API backend with the local development configuration.

```powershell
# From the IceBot-Backend directory
dotnet run --project src/WebAPI/WebAPI.csproj --launch-profile "WebAPI"
```

## 2. Authenticate and Get JWT

Use a tool like Postman, curl, or a local frontend to authenticate as a user that has both ordering and operations permissions (or perform separately for a customer and an admin).

```bash
# Example authentication request (pseudo-code)
curl -X POST https://localhost:7001/api/v1/authentication/login \
  -H "Content-Type: application/json" \
  -d '{"emailOrUsername":"admin@example.com", "password":"password"}'
```

Extract the `token` from the response.

## 3. Test OrderHub

1. Connect to `https://localhost:7001/hubs/orders` using a SignalR client (like Postman's WebSocket/SignalR feature or a simple HTML/JS page).
2. Configure the connection to send the JWT as a Bearer token.
3. Call the `JoinOrder` method:
   - Target: `JoinOrder`
   - Arguments: `["00000000-0000-0000-0000-000000000000"]` (Replace with a valid Order ID)
4. Trigger an API that changes the order/payment state, such as placing an order, cancelling a pending order, or processing a payment webhook.
   - Expected: You should receive an `OrderStatusChanged` or `PaymentStatusChanged` event in your SignalR client.

## 4. Test OperationsHub

1. Connect to `https://localhost:7001/hubs/operations` with the JWT.
2. Call the `JoinKiosk` method:
   - Target: `JoinKiosk`
   - Arguments: `["00000000-0000-0000-0000-000000000000"]` (Replace with a valid Kiosk ID)
3. Trigger a kiosk lifecycle or connectivity change:
   - Example lifecycle: call `PATCH /api/v1/management/kiosks/{kioskId}/status` with `Disabled`, then restore it to `Active` after the check.
   - Example connectivity: ingest a newer authenticated heartbeat or run timeout reconciliation so status changes between `Online`, `Degraded`, and `Unreachable`.
   - Expected: You should receive a `KioskStatusChanged` event in your SignalR client.
   - Call `PATCH /api/v1/management/stores/{storeId}/kiosks/{kioskId}/operational-state` with a new state and reason.
   - Expected: You should receive a distinct `KioskOperationalStateChanged` event.
4. Trigger a maintenance ticket update:
   - Example: Call `POST /api/v1/management/maintenance-tickets` to create a ticket for that kiosk.
   - Expected: You should receive a `MaintenanceTicketChanged` event.
5. Complete an inventory refill task:
   - Example: Call `POST /api/v1/management/kiosks/{kioskId}/inventory/refill-tasks/{taskId}/complete`.
   - Expected: You should receive an `InventoryChanged` event.

## 5. Test ManagementDashboardHub

1. Connect to `https://localhost:7001/hubs/management-dashboard` with the JWT.
2. Call `JoinDashboard` with one of these argument sets:
   - System dashboard: `["system", null, null]`
   - Organization dashboard: `["organization", "organization-guid", null]`
   - Store dashboard: `["store", null, "store-guid"]`
3. Trigger an order, payment, kiosk, maintenance, or inventory change.
   - Expected: You should receive a `DashboardInvalidated` event.

## 6. Expected Event Names

- **OrderHub**: `OrderStatusChanged`, `OrderItemFulfillmentChanged`, `PaymentStatusChanged`
- **OperationsHub**: `OrderItemFulfillmentChanged` is sent to `kiosk:{kioskId}` for fulfillment-workspace refresh.
- **OperationsHub**: `KioskStatusChanged`, `KioskOperationalStateChanged`, `ExecutionReadinessChanged`, `DeviceEventCreated`, `AlertChanged`, `MaintenanceTicketChanged`, `InventoryChanged`
- **ManagementDashboardHub**: `DashboardInvalidated`

`DeviceEventCreated` is emitted for a newly committed device event. `AlertChanged` is also emitted when Error/Critical telemetry creates an actionable alert and when that alert is acknowledged or resolved.

## Related Docs

- [SignalR Realtime Contract](../api/SIGNALR_REALTIME_CONTRACT.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Observability](OBSERVABILITY.md)
