# Operations Support Flow

This document describes operational visibility and manual support flows after kiosks are running.

## Search Keywords

`operations support`, `operations telemetry`, `heartbeat`, `device event`, `stock movement`, `execution event`, `management dashboard`, `order status history`, `manual refund`, `maintenance support`, `staff support`

## Flow

```text
Kiosk / Edge
  -> heartbeat
  -> device events
  -> stock movements
  -> execution events
  -> Cloud read models
  -> management dashboard / support screens
```

Back-office support example:

```text
Order issue
  -> inspect order overview
  -> inspect order status history
  -> inspect payment status
  -> inspect kiosk heartbeat/events
  -> mark refund required or create refund record when needed
```

## Rules

- Heartbeats and device events are operational evidence.
- DeviceEvent is not a long-term actionable alert state. Add Alert entity/API later if needed.
- Inventory V1 is reporting/operations only and does not control runtime sellability.
- Maintenance Ticket V1 is a manual support workflow for kiosk/device/order/event issues, not an auto-alert engine.
- Manual refund/compensation is tracked in the backend, but actual money movement can be staff-handled outside provider integration in V1.
- Operations telemetry APIs expose curated heartbeat/event fields only. Do not return raw `PayloadJson` by default.

## Related Docs

- [System Flows](SYSTEM_FLOWS.md)
- [Maintenance Ticket Flow](MAINTENANCE_TICKET_FLOW.md)
- [Failure Flows](FAILURE_FLOWS.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Authorization Rules](../api/AUTHORIZATION_RULES.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
