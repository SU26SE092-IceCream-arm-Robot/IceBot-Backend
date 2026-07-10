# Edge Sync and Telemetry Contract

This document owns Edge-to-Cloud telemetry, production-history replay, state-summary recovery, heartbeat, and readiness/capability projection contracts.

## Search Keywords

`device event`, `telemetry replay`, `production sync`, `checkpoint`, `state summary`, `heartbeat`, `readiness`, `capability projection`, `SyncEventInbox`, `ExecutionReadinessChanged`

### Device Warning/Error Evidence

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/device-events
```

This single-event endpoint accepts authenticated `Warning`, `Error`, or `Critical` evidence for a device attached to the reporting kiosk. `originNodeId` must match the execution endpoint profile identity. `eventId` is globally unique and acts as the idempotency key; a retry returns the existing event and does not publish SignalR again. `occurredAt` uses the Edge telemetry future-skew limit. Optional structured payload is limited to 16384 characters, stored as evidence, and excluded from the normal management read API. After commit, Cloud publishes `DeviceEventCreated` to the kiosk operations group. A newly accepted `Error` or `Critical` event creates an Open Alert in the same transaction and publishes `AlertChanged`; Warning remains evidence only.

### Telemetry Replay

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/telemetry-events
```

`/telemetry-events` is the authenticated replay surface for heartbeat evidence,
device warning/error evidence, and local operation logs. It does not accept
production history.

Request shape:

```json
{
  "originNodeId": "uuid-bound-to-execution-endpoint",
  "events": [
    {
      "eventId": "uuid-envelope-id",
      "eventType": "Heartbeat",
      "heartbeat": {
        "heartbeatSequence": 41,
        "reportedAt": "2026-07-01T10:00:00Z",
        "status": "Online",
        "pendingSyncEventCount": 2
      }
    },
    {
      "eventId": "uuid-device-event-id",
      "eventType": "DeviceEvent",
      "deviceEvent": {
        "deviceId": "uuid",
        "eventType": "MotorOverheat",
        "severity": "Error",
        "message": "Motor exceeded temperature threshold.",
        "occurredAt": "2026-07-01T09:59:30Z",
        "payload": { "temperatureC": 85 }
      }
    },
    {
      "eventId": "uuid-local-log-source-id",
      "eventType": "LocalLog",
      "localLog": {
        "deviceId": "uuid",
        "action": "RuntimeRestarted",
        "category": "EdgeRuntime",
        "severity": "Info",
        "message": "Runtime restarted after local power interruption.",
        "occurredAt": "2026-07-01T09:58:00Z"
      }
    }
  ]
}
```

Rules:

- The batch contains 1 to `EdgeTelemetryIngestion__MaxBatchEventCount` items; default maximum is 100.
- `eventId` values must be non-empty and unique inside one request.
- Exactly one typed payload must match each `eventType`.
- Each item is atomic and independent. Valid items commit even when another item is rejected.
- A fully accepted/duplicate batch returns `200`; a valid envelope with one or more rejected/failed items returns `207 Multi-Status` with per-item status.
- Per-item statuses are `Accepted`, `Duplicate`, `Rejected`, or `Failed`.
- A successful telemetry item records a processed `SyncEventInbox` receipt. Destination tables remain the data source: `KioskHeartbeat`, `DeviceEvent`, or `OperationLog`.
- Heartbeats retain `(kioskId, originNodeId, heartbeatSequence)` destination idempotency. Device events and local logs use the envelope `eventId` as their source identity.
- If processing commits but receipt recording is interrupted, retry reaches destination dedup and then records the missing receipt.
- Retrying an existing processed `eventId` returns `Duplicate` without replaying side effects or SignalR notifications.
- Batch device events retain the same Alert rule: newly accepted Error/Critical evidence creates one Open Alert; Warning does not.
- Local operation logs may reference a device or order only when it belongs to the reporting kiosk. Raw payload remains bounded to 16384 characters.

### Production History Replay

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/production-sync/events
```

This endpoint accepts only durable production-history items. Each item carries
`eventId`, `sequenceNumber`, `eventType`, `schemaVersion`, `edgeCreatedAt`, and
optional order/command/job correlation plus payload. It returns item-level
results and the contiguous acknowledged sequence.

Production-history rules:

- `originNodeId` is the persistent executor identity bound to the authenticated endpoint: `FullEdgeRuntimeId` or `ControllerId`.
- `(originNodeId, eventId)` is the production-event idempotency identity. `sequenceNumber` is positive, monotonic, persistent across ordinary restart, and unique per origin.
- A ProductionEvent is stored directly in `SyncEventInbox` with its sequence. It does not create a second generic receipt.
- Cloud accepts an event received beyond a gap but advances `ProductionEventCheckpoint` only over committed contiguous sequences. The item result returns `acknowledgedSequenceNumber`; Edge retains and retries everything above it.
- Reusing an event id with another sequence, type, correlation identity, schema version, or payload returns conflict for that item. Reusing a sequence for another event id also returns conflict.
- An event at or below the acknowledged checkpoint is an idempotent duplicate even after its old detailed receipt has passed retention.

Checkpoint query:

```http
GET /api/v1/iot/execution-endpoints/{endpointId}/production-sync/checkpoint?sourceExecutorId={id}
```

The authenticated endpoint may query only its bound executor identity. A new stream returns sequence `0`. This endpoint is the reconnect resume cursor; timestamps are not ordering authority.

### Latest-State Summary Channel

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/production-sync/state-summaries
```

```json
{
  "sourceExecutorId": "uuid-bound-to-execution-endpoint",
  "summaries": [
    {
      "summaryKind": "CurrentExecution",
      "stateRevision": 18,
      "summarySchemaVersion": 1,
      "edgeCreatedAt": "2026-07-01T10:00:00Z",
      "payload": { "status": "Running", "sourceCommandId": "uuid" }
    }
  ]
}
```

`stateRevision` is positive and monotonic per `(sourceExecutorId, summaryKind)`. A newer revision replaces the current summary, an exact same revision is `Duplicate`, an older revision is `Stale`, and the same revision with different content is rejected as conflict. Summary ingestion is item-level and may return `207 Multi-Status`.

The summary channel is advisory current state used to recover visibility quickly after reconnect. It is not durable event history, does not create production events, and never advances `ProductionEventCheckpoint`.

### Heartbeat

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/heartbeat
```

Request:

```json
{
  "originNodeId": "uuid-bound-to-execution-endpoint",
  "heartbeatSequence": 123,
  "reportedAt": "2026-05-21T10:00:00Z",
  "status": "Online",
  "appVersion": "1.0.0",
  "firmwareVersion": "farino-x.y",
  "networkStatus": "Online",
  "robotStatus": "Ready",
  "cpuUsagePercent": 10,
  "memoryUsagePercent": 20,
  "diskUsagePercent": 30,
  "pendingSyncEventCount": 0
}
```

The request uses the same HTTPS execution-endpoint authentication as command pull and execution reports. `originNodeId` must equal the endpoint's bound `FullEdgeRuntimeId` or `ControllerId`. `(kioskId, originNodeId, heartbeatSequence)` is the idempotency key; retry returns the existing heartbeat. `reportedAt` cannot exceed `EdgeTelemetryIngestion__MaxFutureClockSkewSeconds` into the future. Cloud stores unique out-of-order heartbeat evidence, but only a heartbeat whose sequence is newer than the latest stored sequence may change current connectivity. `Kiosk.LastOnlineAt` advances only for a newer `Online` or `Degraded` heartbeat and uses Cloud receive time, never the Edge clock.

Connectivity state machine:

- A current `Offline` heartbeat transitions only `KioskStatus.Active -> KioskStatus.Offline`.
- A current `Online` or `Degraded` heartbeat transitions only `KioskStatus.Offline -> KioskStatus.Active`, and only while the parent organization and store remain active.
- A stale lower-sequence heartbeat is retained for history and returned with `stale=true`; it never rewinds `KioskStatus` or `LastOnlineAt`.
- The reconciliation job transitions `Active -> Offline` with connectivity `Unreachable` after `EdgeTelemetryIngestion__HeartbeatTimeoutSeconds` without an accepted heartbeat.
- Connectivity automation never changes `Provisioning`, `Maintenance`, `Disabled`, or `Retired` kiosks.
- Manual management updates cannot set `Offline` or recover `Offline -> Active`; those transitions belong to accepted heartbeat evidence and timeout reconciliation.
- Heartbeat ingest and timeout reconciliation use the same per-kiosk serialized boundary and recheck current state inside it.
- `KioskStatusChanged` is published only after a committed status transition. Duplicate heartbeats and unchanged states do not publish an event.

### Execution Readiness And Capability Projection

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/readiness
```

The authenticated execution endpoint publishes a complete observed snapshot:

```json
{
  "sourceExecutorId": "uuid",
  "stateRevision": 42,
  "executorReportedAt": "2026-07-01T12:00:00Z",
  "readiness": "Ready",
  "activity": "Idle",
  "safety": "Safe",
  "currentCommandId": null,
  "physicalOutputState": "No",
  "faultCode": null,
  "capabilities": [
    { "capabilityCode": "ICE_CREAM", "workcellCode": "CELL-A", "isAvailable": true }
  ]
}
```

`stateRevision` is positive and monotonic per source executor. Older revisions
are ignored, exact retries are duplicates, and reuse of one revision with
different content returns conflict. `capabilities` is a complete replacement,
not a patch. Cloud stores typed readiness and capability rows; it does not infer
availability from heartbeat strings or generic summary payloads.

`KioskStatus` remains lifecycle/connectivity. Readiness controls machine
sellability and admission: online menu/order validation requires Ready + Safe
and every declared route capability available; command dispatch also requires
Idle. Busy is temporary executor occupancy, not kiosk Offline. SignalR emits
`ExecutionReadinessChanged` only after a newer projection commits.


## Related Docs

- [IoT Contract](IOT_CONTRACT.md)
- [Observability](../operations/OBSERVABILITY.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
