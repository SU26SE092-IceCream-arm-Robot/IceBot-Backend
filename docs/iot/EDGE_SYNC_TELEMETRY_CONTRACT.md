# Edge Sync and Telemetry Contract

This document owns Edge-to-Cloud telemetry, inventory sensor observations, production-history replay, state-summary recovery, heartbeat, and readiness/capability projection contracts.

## Search Keywords

`device event`, `telemetry replay`, `inventory observation`, `production sync`, `checkpoint`, `state summary`, `heartbeat`, `readiness`, `capability projection`, `SyncEventInbox`, `ExecutionReadinessChanged`

## Transport

Typed MQTT uplink is the primary realtime transport for heartbeat, telemetry
replay, inventory observations, readiness, production events, and state summaries:

```text
icebot/execution-endpoints/{endpointId}/uplink/{messageType}
```

The JSON shown in the HTTPS sections below is the `payload` inside the common
MQTT envelope. HTTPS endpoints remain supported for bootstrap, broker outage,
manual diagnostics, and recovery. Both transports invoke the same handlers and
therefore share tenancy validation, idempotency identities, transaction
boundaries, and result semantics. Edge must not allocate a new event ID,
sequence, or revision merely because it changes transport.

The common envelope, application result topic, retry rules, and ACL boundary are
defined in [MQTT Operations](../operations/MQTT_OPERATIONS.md).

### Device Warning/Error Evidence

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/device-events
```

This single-event endpoint accepts authenticated `Warning`, `Error`, or `Critical` evidence for a device attached to the reporting kiosk. `originNodeId` must match the execution endpoint profile identity. `eventId` is globally unique and acts as the idempotency key; a retry returns the existing event and does not publish SignalR again. `occurredAt` uses the Edge telemetry future-skew limit. Optional structured payload is limited to 16384 characters, stored as evidence, and excluded from the normal management read API. After commit, Cloud publishes `DeviceEventCreated` to the kiosk operations group. A newly accepted `Error` or `Critical` event creates an Open Alert in the same transaction and publishes `AlertChanged`; Warning remains evidence only.

### Inventory Sensor Observations

Edge publishes dispenser-level observations through MQTT message type
`inventory-observations`. The payload is authenticated as the endpoint's bound
executor identity and is not a management REST write surface.

```json
{
  "sourceExecutorId": "uuid-bound-to-execution-endpoint",
  "observations": [
    {
      "sourceEventId": "uuid",
      "ingredientDispenserStateId": "uuid",
      "deviceId": "uuid",
      "observationSequence": 311,
      "observedLevelStatus": "Low",
      "observedAt": "2026-07-31T10:00:00Z",
      "sensorPayload": { "sensor": "level-switch", "raw": "LOW" }
    }
  ]
}
```

Rules:

- The batch has 1 to 100 observations. `sourceEventId` is unique within the batch and `(sourceExecutorId, sourceEventId)` is the Cloud idempotency identity.
- `observationSequence` is positive and must be persistent per source executor and dispenser state. A reused source event with different dispenser, device, sequence, or level is a conflict.
- The endpoint must be Active with an active credential, and its bound profile identity must equal `sourceExecutorId`.
- Cloud verifies that the dispenser state is active, belongs to the endpoint kiosk, and is bound to the supplied device. Edge cannot report another kiosk's inventory by changing IDs.
- V1 supports only `Low`, `Medium`, and `Full`. `Unknown` is not an observed physical level.
- `observedAt` is evidence and can be at most five minutes ahead of Cloud receive time. Cloud receive time is timeout authority.
- An observation at or below the latest applied sequence, or no newer than the latest applied sensor observation, is stored as `OutOfOrder` audit evidence and does not overwrite the inventory projection. Manual refill and adjustment timestamps do not make sensor evidence stale.
- If the dispenser has a configured Low/Medium/Full calibration profile, Cloud derives `EstimatedQuantity` from that profile. Without calibration, the level changes but quantity remains unknown.
- Raw `sensorPayload` is bounded diagnostic evidence and is not exposed in normal inventory responses. The dispenser history exposes the observation level, disposition, time, derived estimate, and endpoint reference.
- This channel describes physical inventory evidence. It does not prove a custom Lua program consumed the recipe quantity and does not create a stock movement. Sensor evidence is optional for `ManualEstimate` and `SensorAssisted`; only an explicitly configured `SensorRequired` dispenser uses fresh calibrated sensor evidence as a sellability gate.

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

For the same persistent `sourceExecutorId`, production sequence numbers, heartbeat sequence numbers, readiness revisions, and state-summary revisions must survive ordinary process and device restart. Resetting a counter causes new records to be treated as stale or conflicting. A genuinely reprovisioned runtime receives a new executor identity and starts new streams; V1 does not infer a reboot epoch from wall-clock timestamps.

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

- A current heartbeat updates `KioskConnectivityProjection` to `Online`, `Degraded`, or `Unreachable` and never mutates `KioskStatus` lifecycle.
- A stale lower-sequence heartbeat is retained for history and returned with `stale=true`; it never rewinds connectivity or `LastOnlineAt`.
- The reconciliation job transitions a previously observed connectivity projection to `Unreachable` after `EdgeTelemetryIngestion__HeartbeatTimeoutSeconds` without an accepted heartbeat.
- Lifecycle management and connectivity observation are independent; an `Active` kiosk may be `Unreachable`, and a `Maintenance` kiosk may still report `Online`.
- Heartbeat ingest and timeout reconciliation use the same per-kiosk serialized boundary and recheck current state inside it.
- `KioskStatusChanged` is published only after a committed lifecycle or connectivity transition. Duplicate heartbeats and unchanged projections do not publish an event.

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
  "localPersistenceHealth": {
    "storageWritable": true,
    "freeSpaceBytes": 10737418240,
    "minimumRequiredFreeSpaceBytes": 1073741824,
    "localDatabaseHealth": "Healthy",
    "pendingEventCount": 0,
    "maximumPendingEventCount": 10000
  },
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

### Reported Device Inventory

```http
PUT /api/v1/iot/execution-endpoints/{endpointId}/reported-devices
```

Hardware inventory is a separate full snapshot, sent at startup, reconnect, or
hardware change. It does not share readiness freshness: an unchanged device
inventory remains valid until a newer snapshot replaces it, while readiness is
short-lived operational evidence.

```json
{
  "sourceExecutorId": "uuid",
  "snapshotRevision": 12,
  "observedAt": "2026-07-01T12:00:00Z",
  "devices": [
    {
      "sourceDeviceKey": "arm-left",
      "deviceId": null,
      "runtimeTargetCode": "FAIRINO_LUA_V1",
      "machineModelCode": "FR5"
    }
  ]
}
```

The authenticated Edge declares its observed device/runtime inventory. This is
not operator configuration and does not certify Lua behavior. Cloud uses a
known declared mismatch with RobotProgram runtime/model metadata to block
deployment. No snapshot is `RuntimeProfileUnknown` and remains a warning for
the FR5 MVP. Endpoint identity, lifecycle, readiness, command persistence, and
delivery remain independent blocking routing gates.

`localPersistenceHealth` is required. `Healthy` database state, writable storage,
free space at or above the reported minimum, and event backlog at or below the
reported maximum are mandatory for command admission. Cloud derives the effective
projection defensively: if any check fails, it persists `NotReady` with one of
`LocalStorageNotWritable`, `InsufficientLocalStorage`, `LocalDatabaseUnhealthy`, or
`EventBacklogLimitExceeded`, even if Edge requested `Ready`. Negative values, a
non-positive threshold, or an unknown database-health value reject the snapshot.
Disk usage and pending-sync values in heartbeat remain operational history; they do
not replace this admission snapshot.

`KioskStatus` remains lifecycle/connectivity. Readiness controls machine
sellability and admission: online menu/order validation requires Ready + Safe
and every declared route capability available; command dispatch also requires
Idle. Busy is temporary executor occupancy, not kiosk Offline. SignalR emits
`ExecutionReadinessChanged` only after a newer projection commits.

Readiness is current-state evidence only for `EdgeTelemetryIngestion__ReadinessTimeoutSeconds` after Cloud receives it. Runtime menu, checkout, deployment preview, and production-package workspace ignore an older projection even when its last reported value was Ready/Safe. Executor wall-clock time is not used for this TTL.

Historical device events remain ingestible and deduplicated for audit. An Error/Critical event older than `EdgeTelemetryIngestion__AlertAutomationMaxEventAgeMinutes` at Cloud receive time does not create/correlate an operational Alert or send a critical push; replay must not masquerade as a new incident.


## Related Docs

- [IoT Contract](IOT_CONTRACT.md)
- [Observability](../operations/OBSERVABILITY.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
