# SignalR Realtime Contract

## Scope
SignalR is used exclusively for Cloud-to-Human UI realtime updates, UI deltas, and dashboard invalidation. It is **not** used for Cloud-to-Edge/Kiosk/Robot runtime integration or machine-to-machine command/event flows (which will be handled by MQTT/edge sync). SignalR does not send robot execution commands or device control commands.

## Hub Routes
All SignalR connections are hosted under the following routes:
- `/hubs/orders`: Realtime updates for customer ordering and checkout flows.
- `/hubs/operations`: Realtime updates for staff operations, kiosk status, maintenance, and inventory.
- `/hubs/management-dashboard`: Realtime invalidation triggers for the management dashboard.

## Authentication
SignalR connections require a valid JWT token. Pass the token as a Bearer token during connection setup. Authorization rules apply per hub and per group join method (e.g. you can only join an order group if you own the order, or a kiosk group if your account has access to the organization/store).

## Join Methods
Clients must explicitly join a group to receive targeted events.

### OrderHub
- `JoinOrder(Guid orderId)`: Listen for updates on a specific order.
- `LeaveOrder(Guid orderId)`: Stop listening to an order.

### OperationsHub
- `JoinKiosk(Guid kioskId)`: Listen for operations updates related to a specific kiosk.
- `LeaveKiosk(Guid kioskId)`: Stop listening to a specific kiosk.

### ManagementDashboardHub
- `JoinDashboard(string scope, Guid? organizationId, Guid? storeId)`: Listen for dashboard invalidation events. `scope` must be `system`, `organization`, or `store`.
- `LeaveDashboard(string scope, Guid? organizationId, Guid? storeId)`: Stop listening to the matching dashboard group.

## Events

### OrderHub Events
- `OrderStatusChanged`: Triggered when an order status changes (e.g., placed, preparing, completed).
- `PaymentStatusChanged`: Triggered when payment succeeds or fails.

### OperationsHub Events
- `MaintenanceTicketChanged`: Triggered when a maintenance ticket is created, updated, assigned, started, resolved, closed, or cancelled.
- `InventoryChanged`: Triggered when a dispenser is refilled or its stock estimate is adjusted.
- `KioskStatusChanged`: Triggered when the operational status of a kiosk changes (e.g., Active, Offline, Maintenance).
- `DeviceEventCreated`: (Reserved for future device ingest path) Triggered when a new telemetry or warning event is synced from a device.

### ManagementDashboardHub Events
- `DashboardInvalidated`: Triggered when any significant state changes that requires the management dashboard to refresh its aggregated data.

## Client Workflow

1. **Load Initial State**: Fetch the initial state using REST/GraphQL APIs.
2. **Connect**: Connect to the appropriate SignalR hub with your JWT.
3. **Join Group**: Call the relevant join method (e.g., `JoinOrder` or `JoinKiosk`).
4. **Apply Events**: Apply event payloads immediately when sufficient information is present.
5. **Refetch**: Refetch from REST/GraphQL on reconnect, refresh, or suspected version gap.

## Reconnect Rule
If the SignalR connection drops, the client should attempt to reconnect. Upon successful reconnection, the client **must** refetch the current state via REST/GraphQL to ensure no events were missed during the downtime. SignalR events are fire-and-forget and do not support durable event history.

## Boundary
SignalR is UI realtime only, not a robot runtime or MQTT bus. Do not use SignalR hubs to send commands to the kiosk hardware or robot.

## Related Docs
- [API Surface Rules](API_SURFACE_RULES.md)
