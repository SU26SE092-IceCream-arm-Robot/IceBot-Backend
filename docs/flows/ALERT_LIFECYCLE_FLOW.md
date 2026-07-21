# Alert Lifecycle Flow

## Search Keywords

`alert`, `alert lifecycle`, `acknowledge alert`, `resolve alert`, `dedup`, `occurrence count`, `device event`, `SignalR AlertChanged`, `Firebase critical alert`

## Purpose

`DeviceEvent` is immutable telemetry evidence. `Alert` is the actionable operational state derived from suitable telemetry. `MaintenanceTicket` remains a separate work-management record; inventory-empty automation may create a linked ticket when enabled.

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

## Critical Alert Push Notification

Problem: a franchise operator may not have the management dashboard open while
an unattended kiosk has a fault that requires human intervention. SignalR is
appropriate for connected clients but does not recall an absent operator.

Confirmed decision: `CriticalOperationalAlertOpened` is the first Firebase
business notification. It is derived from committed `Alert` state, never from
raw `DeviceEvent` payload. A push is attempted only when a new Alert is created
as Critical or an existing correlated Alert increases from Error to Critical.
Duplicate source events and later Critical occurrences do not send another
push.

Recipient policy:

1. Select distinct active Technician and Manager accounts whose active role
   scope matches the Alert kiosk, store, or organization and which have an
   active notification-device registration.
2. If that set is empty, select active OrgAdmin accounts assigned to the exact
   organization and having an active notification-device registration.
3. Do not broadcast to SystemAdmin, Staff, unrelated tenant scopes, or every
   account that can technically read an Alert.

The payload is bounded to notification type, Alert/Kiosk/Device identifiers,
Alert code, and Critical severity. Raw telemetry payload, customer data, and
the complete device error message are excluded. The visible title is the
bounded Alert title.

The Alert and one durable `NotificationDelivery` per recipient are committed in
the same database transaction. A background worker sends Firebase push after
commit and retries transient failures. Delivery failure never rolls back device
evidence or actionable Alert state. SignalR and management reads remain the live
and authoritative operational surfaces. The outbox provides at-least-once send
attempts; clients deduplicate by `deliveryId`.

Excluded from the critical-alert trigger:

- every Error/Warning DeviceEvent: too noisy and bypasses Alert correlation.
- customer/order progress: the kiosk/tablet already uses authoritative polling
  and SignalR while the customer is present.

Operations owns `NotificationDelivery` and recipient selection; Identity owns
account notification-device registrations and Firebase delivery. Inventory
empty uses a separate trigger and delivery key instead of reusing the critical
device-alert trigger.

The same outbox is shared infrastructure for other independently owned events,
including overdue Manual/Packaged fulfillment. Each event keeps its own trigger,
recipient policy, and idempotent delivery key; it does not become an Alert.

Failed configuration deployments are reconciled from committed Full Edge and
Low-cost deployment state, including executor failures and timeout failures.
They notify scoped Technician/Manager accounts and fall back to OrgAdmin.
Candidates from both execution profiles share one failure-time-ordered batch,
so one profile cannot starve the other. A failure with no currently eligible
recipient remains pending and becomes deliverable if a matching account and
notification device are provisioned later; Cloud does not create a synthetic
recipient or mark it notified without a delivery. Recipient-less failures are
excluded before the bounded batch is selected, so they cannot starve later
deliverable failures. One failed candidate is isolated and does not stop the
remaining items in the batch.
Maintenance assignment accepts only an active Technician, Manager, or OrgAdmin
in the ticket tenant scope. It notifies that assignee when an active notification
device exists. Requeueing a permanently failed delivery repeats delivery only;
it never repeats the source business transition.

Payment-session reconciliation creates a `payment_intervention` delivery only
when the session enters manual intervention: retry exhaustion, a provider
identity or amount mismatch, or provider-paid state still awaiting the signed
webhook after retries are exhausted. Scoped Staff and Manager recipients are
preferred, with organization OrgAdmin fallback. Retryable reconciliation,
restored checkout instructions, explicit cancellation/expiry, and known
provider-session absence do not create this notification. Delivery identity is
the payment transaction, intervention code, and recipient, so repeating the
same reconciliation result is idempotent.

The durable delivery-key evidence for `deployment_failed`,
`fulfillment_overdue`, and `payment_intervention` is retained beyond ordinary
notification-delivery retention. Source reconciliation therefore cannot resend
the same occurrence merely because historical outbox rows were purged.

## Excluded From V1

- configurable alert rules or thresholds;
- alert assignment, escalation, snooze, or suppression API;
- configurable inventory thresholds beyond the current Low/Empty state mapping.

## MQTT Credential Operational Alerts

The MQTT credential reconciliation job derives actionable alerts from committed
credential state. It does not create alerts for a transient broker error that
the original request records and returns immediately.

| Alert code | Trigger | Recovery |
| --- | --- | --- |
| `MQTT_CREDENTIAL_OPERATION_TIMEOUT` | Provisioning or rotation remains pending beyond the five-minute operation lease and is marked `Failed` | Resolve after an operator retry activates the credential |
| `MQTT_CREDENTIAL_REVOKE_FAILED` | Automatic stale revocation retry fails and records `RevokeFailed` | Resolve when a later automatic or operator revocation reaches `Revoked` |

Alerts are correlated by execution endpoint source and alert code under a
PostgreSQL advisory lock. A repeated failed revocation retry increments
`OccurrenceCount`; periodic scans do not create synthetic occurrences. Active
alerts are scanned for recovery even after the credential no longer qualifies
as stale, so successful manual repair also resolves the alert. Creation,
occurrence, and resolution publish `AlertChanged` only after commit. These alerts
use Error severity and do not trigger the Critical Firebase notification policy.

## Inventory Alert Automation

The reconciliation job maps active dispenser state to `INVENTORY_LOW` or
`INVENTORY_EMPTY`. It serializes each dispenser with a PostgreSQL advisory lock,
keeps one active alert for the current threshold, resolves stale/duplicate active
alerts, and publishes SignalR only for committed transitions. When
`InventoryAlertAutomation:CreateMaintenanceTicketForEmpty` is enabled, Empty
creates one linked maintenance ticket. Empty also creates a durable push for the
scoped operational recipients. Healthy recovery resolves the alert; it does not
close the maintenance ticket automatically.
