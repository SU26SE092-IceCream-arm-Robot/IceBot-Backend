# Edge Command Contract

This document owns Cloud-to-Edge command delivery, endpoint provisioning/authentication, command acknowledgement, execution reports, and configuration distribution.

## Search Keywords

`EdgeCommand`, `command pull`, `command ack`, `execution report`, `DeployConfiguration`, `ExecuteOrder`, `MQTT wake-up`, `execution endpoint`, `mTLS`, `signed command`, `configuration sync`

## Cloud To Edge Notification

### MQTT Command-Available Wake-Up

After a durable `ExecuteOrder` or `DeployConfiguration` command commits, Cloud makes a best-effort MQTT publish. The wake-up lowers latency only; periodic authenticated command pull remains the delivery authority and recovers broker outages or missed messages.

Topic:

```text
icebot/execution-endpoints/{executionEndpointId}/commands/available
```

Payload:

```json
{
  "type": "CommandAvailable",
  "commandId": "uuid",
  "commandType": "ExecuteOrder",
  "targetExecutionEndpointId": "uuid",
  "notifiedAt": "2026-05-21T10:00:00Z",
  "version": 1
}
```

Rules:

- MQTT payload is a wake-up signal only.
- Edge must call command pull after receiving this notification.
- The message uses QoS 1 and is not retained. Duplicate delivery is expected.
- MQTT publish failure does not roll back or fail the already committed command.
- Duplicate MQTT messages are expected and must be harmless.
- MQTT notification is best-effort; periodic Edge pull is the delivery authority.
- Broker ACLs bind each MQTT subscriber to its own execution-endpoint topic. Edge calls command pull after every wake-up and also after reconnect/on its polling interval. Operational setup is defined in [MQTT Operations](../operations/MQTT_OPERATIONS.md).
- MQTT subscriber identity is provisioned separately from HTTPS execution authentication. Username and client id equal `executionEndpointId`; the generated password is returned once, held by the broker/Edge secret stores, and never persisted in the application database. Rotation immediately invalidates the old password; revoke disables and disconnects the broker client.

## Edge To Cloud

### Pull Commands

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/commands/pull
```

Full Edge sends its provisioned client certificate during the TLS handshake. Low-cost controllers add the signed-request headers defined under **Execution Endpoint Transport Authentication** below.

Request:

```json
{
  "maxCommands": 10,
  "edgeTime": "2026-05-21T10:00:02Z"
}
```

Response:

```json
{
  "serverTime": "2026-05-21T10:00:03Z",
  "commands": [
    {
      "commandId": "uuid",
      "commandType": "DeployConfiguration",
      "kioskId": "uuid",
      "targetExecutionEndpointId": "uuid",
      "orderId": null,
      "dispatchAttemptNo": null,
      "issuedAt": "2026-05-21T10:00:00Z",
      "expiresAt": "2026-05-21T10:10:00Z",
      "payloadJson": "{... canonical command payload ...}"
    }
  ]
}
```

Rules:

- Edge must deduplicate by `commandId`.
- New `ExecuteOrder` payloads include `SchemaVersion = 4`, `executionIntent`, a `productionUnitStartNo` on each line, and the immutable `restartPolicy` of every selected robot program. V1 emits and accepts only `ManualOnly`; schema-3 payloads without the field are interpreted as `ManualOnly`. Edge must reject unsupported schema versions or restart policies. Cloud may read release provenance from older payloads without treating unsupported payloads as fully executable contracts. Each selected option can include immutable ingredient requirements (`ingredientId`, code/name snapshots, quantity per option, unit, required workcell capability); Edge must use this order snapshot rather than live catalog data.
- An ordered artifact may include `RequiredOptionCode`. Edge executes that artifact only when the same order line contains a selected option with the matching normalized code; otherwise Edge skips it without changing the remaining `RunOrder`. This is conditional file selection, not a Lua runtime parameter.
- Deployment commands include typed Cloud correlation fields for deployment ownership. `PayloadJson` is execution data, not the authoritative link used by timeout reconciliation.
- Pull first materializes any short-lived artifact download URLs. Only after payload enrichment succeeds does it mark returned commands as `Delivered` and record a delivery attempt.
- Retrying command pull can return delivered but unacknowledged commands.
- Runtime execution state is reported through the event/report ingest boundary, not command ack.
- If a deployment command expires before acceptance, Cloud marks the command `Rejected` with `CommandExpired` and marks the linked Pending deployment `Failed`.
- Once accepted, command expiry no longer applies. If no `Installed`, `Active`, or `Failed` report moves the deployment out of Pending within the configured accepted-report timeout, Cloud marks that attempt `Failed/ExecutionReportTimeout` without changing the endpoint's previously active release/artifact set.
- Late reports do not revive a timed-out attempt. Reconciliation requires a new deployment/rollback attempt so Cloud and endpoint history remain explicit.
- A Full Edge deployment materializes or reuses a deterministic bundle from the published profile-neutral release manifest. Its `DeployConfiguration` payload contains both that immutable bundle descriptor and individual artifact descriptors. During authenticated pull, both receive short-lived URLs so Edge may choose cache-aware incremental download or the complete bundle. Low-cost publication and payloads do not require a Full Edge bundle and contain only their selected artifact descriptors.
- The Full Edge ZIP contains `release-content-manifest.json` plus `artifacts/{RobotArtifactId}.lua`. The manifest includes routes, ordered program bindings, parameters, compatibility, entry names, sizes, and artifact checksums needed for installation.
- Rollback uses the same `DeployConfiguration` command contract and includes `RollbackTargetDeploymentId` as provenance. Edge installs it as a new deployment attempt; it does not locally mutate the historical deployment.
- `Installed` and `Active` deployment reports must match the accepted command's typed `DeploymentId` and deployment kind. Full Edge reports must echo `SourceConfigurationReleaseId` and `ReleaseChecksum`; Low-cost reports must additionally echo `ActiveSetVersion` and `ActiveSetChecksum`. Mismatches are rejected without changing deployment or endpoint observed state. `Failed` may omit installed-state provenance because no activation is asserted.
- Presigned download URLs are transport data only. They are not persisted in `EdgeCommand.PayloadJson`, release manifests, or artifact metadata.
- The object-storage bucket remains private. Edge must download before URL expiry and must not treat the URL as an artifact identity.
- `DownloadUrl` must use an endpoint reachable from the execution endpoint. A Docker-internal MinIO hostname is not a valid external Edge download endpoint unless both runtimes share that network.
- For incremental download, Edge verifies each artifact byte length and SHA-256 checksum. For bundle download, Edge first verifies bundle size/checksum, safely extracts expected entries only, then verifies every extracted artifact against `release-content-manifest.json`.
- A failed download, expired URL, size mismatch, or checksum mismatch must fail the deployment attempt. Edge may pull the unacknowledged command again to obtain fresh download URLs; it must not activate partial or unverified files.
- Fairino-Studio currently exports multiple `.lua` files: normally one file per editor step, while a paired loop is exported as one file. Each exported file is stored as one `RobotArtifact`; `RobotProgramArtifact.RunOrder` defines their runtime sequence.
- Filename prefixes such as `01_` are human-facing export hints, not execution authority. Edge executes the ordered program manifest delivered by Cloud.

### Execution Endpoint Provisioning Boundary

Before command pull, Cloud management must create and activate a `KioskExecutionEndpoint`:

1. Create the endpoint in `Provisioning` for one kiosk and one execution profile.
2. Replace its supported robot targets using runtime-target code, machine-model code, and optional same-kiosk device binding.
3. Provision profile-specific authentication material and a profile identity.
4. Activate the endpoint; only an Active endpoint with an Active credential may authenticate command pull or report execution state.

Full Edge uses `FullEdgeRuntimeId` and requires `MutualTls`; provisioning accepts the client certificate SHA-256 fingerprint. Low-cost uses `ControllerId` and requires `SignedCommandTls`; provisioning accepts an ECDSA NIST P-256 public key PEM. The backend stores only the canonical public key and its SHA-256 fingerprint, never the controller private key. Disabling or retiring an endpoint blocks runtime authentication without deleting deployment or execution history.

Full Edge provisioning body:

```json
{
  "profileIdentity": "<full-edge-runtime-id>",
  "clientCertificateSha256Fingerprint": "<64 lowercase hex characters>"
}
```

Low-cost provisioning body:

```json
{
  "profileIdentity": "<controller-id>",
  "ecdsaPublicKeyPem": "-----BEGIN PUBLIC KEY-----\n...\n-----END PUBLIC KEY-----"
}
```

### Execution Endpoint Transport Authentication

All IoT routes require HTTPS. The authenticated `KioskExecutionEndpoint` is
identified by `{endpointId}` in the route; Cloud derives `KioskId` from that
endpoint instead of trusting a second route or header identity.

Persistence uses the same boundary: readiness, supported-target, telemetry,
alert, deployment, and execution projection rows cannot pair an endpoint or
device with a different kiosk. Deployments also require the selected release
and kiosk to belong to the same organization. Production execution reports
must reference the endpoint targeted by their source command. These are
database constraints in addition to transport authentication and handler
authorization checks.

**Full Edge:** Kestrel requests a client certificate and the application compares its SHA-256 fingerprint with the active credential binding using constant-time comparison. Certificate-chain trust is not used as endpoint identity; the provisioned fingerprint is the pinning boundary.

**Low-cost controller:** every request includes:

```text
X-Execution-Timestamp: <Unix seconds>
X-Execution-Nonce: <new UUID per request>
X-Execution-Signature: <Base64 ECDSA P-256 signature>
```

The signature uses SHA-256 and IEEE P1363 fixed-field format (64-byte `r || s`). It signs this UTF-8 canonical string, with one LF between fields and no trailing LF:

```text
UPPERCASE_HTTP_METHOD
REQUEST_PATH
RAW_QUERY_STRING_OR_EMPTY
endpoint-id-in-D-format
unix-timestamp
nonce-in-D-format
lowercase-sha256-of-raw-request-body
```

The request path includes the API version and route exactly as sent. The query string includes its leading `?` when present. The body hash is computed from raw bytes before JSON model binding.

Cloud rejects signatures outside `ExecutionEndpointSecurity:SignedRequestMaxClockSkewSeconds`. After successful signature verification, it atomically stores `(EndpointId, Nonce)`; reuse is rejected even across backend instances. Expired nonce rows are removed by data retention. Clients retry with a new timestamp, nonce, and signature.

### Command Ack

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/commands/{commandId}/ack
```

Request:

```json
{
  "ackStatus": "Accepted",
  "acknowledgedAt": "2026-05-21T10:00:05Z",
  "localStatePersisted": true,
  "rejectionCode": null,
  "rejectionMessage": null,
  "physicalOutputMayHaveOccurred": null
}
```

Allowed `ackStatus` values:

- `Received`
- `Accepted`
- `Rejected`
- `ExecutorBusy`
- `DeliveryFailed`

Command ack owns delivery and executor-admission state. For `ExecuteOrder`, that admission decision also projects to the order lifecycle as defined below. `Running`, `Completed`, `Failed`, and
`RequiresManualIntervention` belong to the execution event/report ingest
boundary, not this endpoint.

`acknowledgedAt` is executor evidence, not ordering or expiry authority. It must not be farther in the future than `ExecutionReportIngestion__MaxFutureClockSkewSeconds`, and must not predate command creation or recorded delivery beyond that same skew allowance. Cloud applies the acknowledgement, command expiry, provisional execution projection, status history, and realtime update at Cloud receive time.

For `ExecuteOrder` commands:

- `Accepted` may move `Order` from `ReadyForFulfillment` to `Accepted` when aggregate line state allows it.
- `Accepted` requires `localStatePersisted=true`. Edge sends it only after one local transaction durably records the command, production/deployment jobs, immutable provenance, and the ACK outbox entry. Persisting after ACK is invalid because a crash would leave Cloud with an accepted command that Edge cannot recover.
- If that local transaction cannot commit, Edge responds `Rejected` with `LocalPersistenceUnavailable`, `LocalDatabaseCorrupt`, `InsufficientLocalStorage`, or `EventBacklogLimitExceeded`. It must not start physical execution.
- `ExecutorBusy` is temporary: the command returns to `PendingDelivery` and the order remains `ReadyForFulfillment`.
- `Rejected` with `physicalOutputMayHaveOccurred` absent or `false` moves the order to `ExecutionRejected`.
- `Rejected` with `physicalOutputMayHaveOccurred=true` moves the paid order to `RefundRequired` because staff support or compensation may be required.
- Order status changes and their `OrderStatusHistory` row commit together with the command acknowledgement.
- Accepting an `ExecuteOrder` command creates a provisional `OrderExecutionRecord` with sequence `0`. This is Cloud correlation state, not a fabricated Edge event; the first order-summary report starts at sequence `1` and replaces it with executor evidence.

Execution timeout reconciliation:

- Before ACK, an expired `ExecuteOrder` command becomes `Rejected/CommandExpired`; a still-`ReadyForFulfillment` order becomes `ExecutionRejected` with status history.
- An Accepted command with no order-summary report beyond the configured deadline becomes `Stale/Delayed` when the executor heartbeat is still current.
- An Accepted or Running execution with no current heartbeat becomes `Unreachable/PendingRecovery`.
- Prolonged unreachable observation becomes `Unreachable/SupportRequired` for customer/support handling without asserting physical failure.
- Reconciliation changes `OrderExecutionRecord.ObservationStatus` and `CustomerExecutionStatus`; it does not claim that the physical job failed and does not change an Accepted/Preparing Order to `Failed`.
- Customer order/payment polling reads the latest dispatch attempt projection. SignalR publishes `OrderExecutionObservationChanged` to the order group for the same projection.
- A later valid executor report restores observation to `Fresh` through normal sequence validation.
- Missing-report deadlines and support escalation use the last Cloud receive time. `LastExecutorReportedAt` remains diagnostics evidence and cannot make a freshly received report immediately stale when an executor clock moves backward.

Redispatch is an explicit management operation, not an Edge-side automatic retry. Backend permits a new attempt only after transport `DeliveryFailed` or a rejection proven to be before physical output. It allocates the next attempt number under the order lock, enforces the configured maximum, and records operator/reason audit. `ExecutorBusy` redelivers the same command; possible physical output, `RefundRequired`, production failure, and manual intervention remain support/compensation flows.

### Execution Reports

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/commands/{commandId}/reports
```

Request:

```json
{
  "sourceEventId": "uuid",
  "sequenceNumber": 12001,
  "edgeCreatedAt": "2026-05-21T10:01:58Z",
  "executorReportedAt": "2026-05-21T10:01:59Z",
  "reportType": "ProductionExecution",
  "status": "Running",
  "deploymentId": null,
  "sourceProductionJobId": "uuid",
  "sourceConfigurationReleaseId": "uuid",
  "releaseChecksum": "sha256-hex",
  "physicalOutputMayHaveOccurred": true,
  "errorCode": null,
  "errorMessage": null,
  "payloadJson": null,
  "stockMovements": [
    {
      "sourceEventId": "uuid",
      "ingredientDispenserStateId": "uuid",
      "quantityConsumed": 12.5,
      "balanceAfter": 87.5,
      "occurredAt": "2026-05-21T10:01:57Z",
      "isEstimated": false
    }
  ]
}
```

Response:

```json
{
  "succeeded": true,
  "statusCode": 200,
  "message": "Execution report applied successfully.",
  "data": {
    "commandId": "uuid",
    "sourceEventId": "uuid",
    "reportType": "Deployment",
    "status": "Active",
    "applied": true,
    "duplicate": false
  }
}
```

Allowed `reportType` values in V1:

| Report type | Meaning | Target |
| --- | --- |
| `Deployment` | Full Edge configuration deployment or low-cost active artifact-set deployment result | `KioskConfigurationDeployment`, `ControllerArtifactSetDeployment`, `KioskExecutionEndpoint` active snapshot |
| `ProductionExecution` | Execute-order production progress/result | `ProductionExecutionRecord`, `OrderExecutionRecord` when order provenance is present |

Deployment `status` values:

- `Installed`
- `Active`
- `Failed`

Production execution `status` values:

- `Accepted`
- `Running`
- `Completed`
- `Failed`
- `RequiresManualIntervention`

Rules:

- Command ack is dispatch-only. Execution reports are the current V1 boundary for deployment and production status after a command has been accepted.
- The endpoint deduplicates by `(sourceExecutorId, sourceEventId)` using `SyncEventInbox`. A retry is a duplicate only when command identity and the complete normalized report payload match; reusing the event id for another command or payload returns `409 Conflict`.
- Production reports must repeat the `SourceConfigurationReleaseId` and `ReleaseChecksum` from the accepted execute-order command. Low-cost reports must also repeat the command's `ActiveSetVersion` and `ActiveSetChecksum`; Full Edge reports omit both. Cloud compares this provenance against the immutable command payload before creating execution or stock projections.
- `edgeCreatedAt`, optional `executorReportedAt`, and stock-evidence `occurredAt` cannot be farther in the future than `ExecutionReportIngestion__MaxFutureClockSkewSeconds` relative to Cloud receipt time.
- `sequenceNumber` is executor-local ordering evidence for projection updates.
- A final replay may transition `Accepted` directly to `Completed`, `Failed`, or `RequiresManualIntervention`; `Running` may have been lost while the controller was disconnected.
- `physicalOutputMayHaveOccurred` must be set when reporting failed production execution. It drives customer/support projection: failure before output can be handled differently from failure after possible physical output.
- Deployment report `Active` updates the observed active configuration/artifact-set snapshot on `KioskExecutionEndpoint`.
- Job/unit evidence is the Cloud authority for per-unit production outcomes. Non-overlapping ranges update `ProductionExecutionRecord`, optional stock evidence, effective completed/failed/in-progress counts, and the affected machine line in one transaction. A line completes only after all expected units are covered by effective `Completed` evidence; one failed unit moves a paid order to `FulfillmentIssue` while successful unit and inventory evidence remain intact.
- A report with `sourceProductionJobId=null` remains the Edge-computed order observation and updates `OrderExecutionRecord`. When any job evidence exists, a final summary requires complete coverage and must agree with the Cloud-derived aggregate; contradictory final summaries are rejected. A non-final summary may arrive behind newer unit evidence and cannot rewind the business lifecycle.
- A remake is a new `ExecuteOrder` command with `executionIntent=Remake`, `remakeOfSourceCommandId`, and an exact `productionUnitStartNo`/quantity range. Cloud creates it only for failed units with confirmed no physical output. Evidence from the later remake attempt supersedes the failed outcome for those units; all earlier execution and stock evidence remains immutable.
- If Edge or a controller restarts during an active production job, Edge reports that exact job as `RequiresManualIntervention` with `errorCode` equal to `RuntimeRestarted`, `ControllerRestarted`, or `PowerInterrupted`. The report includes the exact unit range and `physicalOutputMayHaveOccurred`; unknown is represented by `null`. Cloud never automatically replays an accepted command, resumes Lua, or restarts the artifact list. See [Restart And Power Recovery](../operations/RESTART_AND_POWER_RECOVERY.md).
- If local persistence becomes unavailable after acceptance, Edge stops admitting new work and reports each affected active production job as `RequiresManualIntervention/LocalPersistenceLost`. It preserves exact unit identity and reports physical output as `true`, `false`, or `null`; inability to persist evidence must never be translated to `false`.
- Changed lines publish `OrderItemFulfillmentChanged`; aggregate order transitions publish `OrderStatusChanged`; applied order-summary observations publish `OrderExecutionObservationChanged` through SignalR after commit.
- `stockMovements` is typed append-only consumption evidence and is accepted only on a report with `sourceProductionJobId`. Every item must identify the same `OrderItemId` as that job report. Each item has its own globally unique `sourceEventId`; duplicates are serialized by evidence identity and ignored even when two different reports arrive concurrently. The dispenser must belong to the reporting kiosk. Evidence freshness is independent from lifecycle projection freshness, so a sequence-stale job observation can still append previously unseen physical evidence.
- A supplied `balanceAfter` updates the observed dispenser estimate. Without it, Cloud records evidence without guessing a new balance. Inventory evidence does not gate runtime-menu sellability or order creation in V1.
- Applied stock evidence publishes `InventoryChanged` after commit. Do not encode authoritative stock adjustments only inside `payloadJson`.

### Configuration Sync

There is no current `GET /api/v1/iot/execution-endpoints/{endpointId}/configuration` endpoint.

Current production configuration distribution uses the durable command flow:

```text
Published ConfigurationRelease
-> DeployConfiguration EdgeCommand
-> authenticated command pull
-> short-lived artifact download URLs
-> local size/checksum verification
-> Installed report
-> Active report
```

Cloud ships immutable release/program manifests and ordered `RobotArtifact` descriptors. `RobotProgramArtifact.RunOrder` defines artifact execution order, and optional `RequiredOptionCode` controls whether an option-specific file participates for an order line. Cloud does not ship `RobotProgramStep`, motion commands, Blockly trees, teaching points, or realtime robot steps.

A future catalog/menu snapshot endpoint is a separate contract. It must not reintroduce the removed step-first robot configuration model or duplicate the deployment command path.

## Related Docs

- [IoT Contract](IOT_CONTRACT.md)
- [MQTT Operations](../operations/MQTT_OPERATIONS.md)
- [Robot Lua Artifact Flow](../flows/ROBOT_LUA_ARTIFACT_FLOW.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
