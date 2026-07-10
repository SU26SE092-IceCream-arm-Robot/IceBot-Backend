# Alert Lifecycle Flow

## Search Keywords

`alert`, `alert lifecycle`, `acknowledge alert`, `resolve alert`, `dedup`, `occurrence count`, `device event`, `SignalR AlertChanged`

## Purpose

`DeviceEvent` is immutable telemetry evidence. `Alert` is the actionable operational state derived from suitable telemetry. `MaintenanceTicket` remains a separate work-management record and is not created automatically in V1.

## Creation

```text
Edge submits DeviceEvent
-> Backend authenticates endpoint and validates device/kiosk scope
-> Warning: store evidence only
-> Error or Critical: create or correlate an actionable Alert in one transaction
-> Commit
-> Publish DeviceEventCreated and AlertChanged
```

Rules:

- Only newly accepted `Error` and `Critical` device events create or update alerts automatically.
- `Warning` remains searchable evidence and does not create alert noise by default.
- `Alert.SourceType = DeviceEvent` and `Alert.SourceId` points to the persisted `DeviceEvent.Id`.
- Device-event retry uses the existing `eventId` idempotency boundary and creates neither a duplicate event nor a duplicate alert.
- Alert creation fails atomically with device-event ingestion; the system does not commit one without the other.
- Raw `PayloadJson` remains evidence-only and is not copied into the Alert response.

## Correlation And Deduplication

Repeated events are grouped by `KioskId + DeviceId + normalized AlertCode` within the configured rolling correlation window (`EdgeTelemetryIngestion:AlertCorrelationWindowMinutes`, default 15 minutes).

- An `Open` or `Acknowledged` alert inside the window receives another occurrence instead of creating a new row.
- `OccurrenceCount` increments and `LastOccurredAt` advances; `RaisedAt` remains the first occurrence.
- `SourceId`, title, and message describe the latest occurrence. Severity may increase but does not decrease.
- `Resolved` and `Suppressed` alerts are terminal and are never reopened by correlation. A later event creates a new alert.
- Correlation is serialized with a PostgreSQL advisory transaction lock, so concurrent repeated events cannot create parallel alerts for the same key.
- An event outside the rolling window creates a new alert even when an older non-terminal alert exists.

## Lifecycle

```text
Open
-> Acknowledged
-> Resolved

Open
-> Resolved
```

- Acknowledge records `AcknowledgedByAccountId` and `AcknowledgedAt`.
- Resolve requires resolution notes and records `ResolvedAt`.
- Repeating acknowledge or resolve is idempotent and does not publish another transition.
- Resolved and Suppressed are terminal for the exposed V1 lifecycle.
- Lifecycle mutations serialize by `alertId`; SignalR publishes only after commit.

## API

```http
GET   /api/v1/management/alerts
GET   /api/v1/management/alerts/{alertId}
PATCH /api/v1/management/alerts/{alertId}/acknowledge
PATCH /api/v1/management/alerts/{alertId}/resolve
```

List filters: `status`, `severity`, `organizationId`, `storeId`, `kioskId`, `deviceId`, `from`, `to`, `pageNumber`, and `pageSize`. Date filters and default descending order use `LastOccurredAt`, so a correlated active alert returns to the top of the operational queue.

Creation is intentionally part of authenticated device-event ingestion rather than a general management `POST /alerts`. V1 does not allow operators to fabricate telemetry alerts manually.

## Authorization

| Policy | Roles | Behavior |
| --- | --- | --- |
| `alerts.view` | SystemAdmin, OrgAdmin, Manager, Staff, Technician | Read alerts inside assigned tenant scope |
| `alerts.manage` | SystemAdmin, OrgAdmin, Manager, Technician | Acknowledge or resolve alerts inside assigned tenant scope |

## Realtime

`AlertChanged` is sent to `kiosk:{kioskId}` after creation, correlated occurrence, acknowledgement, or resolution commits. It contains the alert identity, scope, device, severity, old/new status, occurrence count, last occurrence timestamp, update timestamp, and version. Clients use REST for initial state/history and SignalR for committed deltas.

## Excluded From V1

- configurable alert rules or thresholds;
- alert assignment, escalation, snooze, or suppression API;
- automatic MaintenanceTicket creation;
- automatic resolution from a later healthy telemetry event.
