# MQTT Operations

## Search Keywords

`MQTT`, `Mosquitto`, `broker`, `execution endpoint`, `MQTT credential`, `topic ACL`, `wake-up notification`, `command pull`, `TLS`

## Ownership

MQTT has two transport roles:

- Cloud-to-Edge command-available wake-up remains best effort. `EdgeCommand` in
  PostgreSQL and authenticated command pull remain authoritative.
- Edge-to-Cloud telemetry, readiness, execution reports, production events, and
  state summaries use typed QoS 1 uplink messages. The owning Application
  handler and committed Cloud state remain authoritative; broker acceptance is
  not business acceptance.

```text
Cloud commits EdgeCommand
-> Cloud publishes CommandAvailable
-> Edge receives endpoint-scoped wake-up
-> Edge calls command pull
-> Edge validates, accepts, and executes the durable command
```

The wake-up contains no executable payload. Duplicate and missing MQTT messages are expected and harmless because Edge deduplicates pulled commands and also polls periodically.

For uplink, Edge keeps every message in its local outbox until Cloud publishes a
matching application result. Missing results are retried with the same message
and evidence identity. HTTPS ingest remains a recovery fallback and invokes the
same handlers.

## Local Broker

Local development uses Mosquitto from the independent `IceBot-Tools` lifecycle:

```powershell
cd ..\IceBot-Tools\docker
$env:MQTT_BACKEND_PASSWORD = "local-backend-secret"
$env:MQTT_DYNSEC_ADMIN_PASSWORD = "local-dynsec-admin-secret"
docker compose --profile mqtt up -d mqtt-init mosquitto
```

Local Mosquitto uses the Dynamic Security plugin. Bootstrap creates a backend
publisher role and a shared endpoint subscriber role whose `%u` username is
restricted to its endpoint topic.

Enable backend credential administration without committing the dynsec admin secret:

```powershell
$env:MqttCredentialProvisioning__Enabled = "true"
$env:MqttCredentialProvisioning__AdminPassword = $env:MQTT_DYNSEC_ADMIN_PASSWORD
$env:MqttCredentialProvisioning__RetryCount = "1"
$env:MqttCredentialProvisioning__RetryDelayMilliseconds = "500"
```

Provision one MQTT subscriber after its execution endpoint is active:

```powershell
.\mqtt\provision-endpoint.ps1 `
  -ExecutionEndpointId "00000000-0000-0000-0000-000000000000" `
  -BearerToken "management-jwt"
```

Enable backend publishing without committing credentials:

```powershell
$env:EdgeCommandMqtt__Enabled = "true"
$env:EdgeCommandMqtt__Host = "localhost"
$env:EdgeCommandMqtt__Port = "1883"
$env:EdgeCommandMqtt__Username = "icebot-backend"
$env:EdgeCommandMqtt__Password = "local-backend-secret"
$env:EdgeCommandMqtt__PublishTimeoutSeconds = "6"
$env:EdgeCommandMqtt__PublishRetryCount = "1"
$env:EdgeCommandMqtt__PublishRetryDelayMilliseconds = "250"
dotnet run --project .\src\WebAPI\WebAPI.csproj
```

Local ACL boundary:

- `icebot-backend` may publish
  `icebot/execution-endpoints/+/commands/available` and
  `icebot/execution-endpoints/+/uplink/results`.
- Cloud uplink consumers use the shared group `icebot-cloud-uplink` and subscribe
  to one exact wildcard topic per allowed message type. They do not subscribe
  to `uplink/results`.
- An Edge MQTT username must equal its `executionEndpointId` UUID.
- That endpoint may subscribe only to its `commands/available` and
  `uplink/results` topics.
- That endpoint may publish only the allowed typed message names below its own
  `icebot/execution-endpoints/{executionEndpointId}/uplink/` prefix. It cannot
  publish `results` or another endpoint's messages.
- No anonymous access is enabled.
- The management API creates or rotates the broker client through Mosquitto
  Dynamic Security. PostgreSQL stores lifecycle metadata only; it never stores
  the MQTT password or broker password hash.
- Provision/rotate returns the generated password once. Rotation invalidates
  the previous password immediately; hot overlap is intentionally not V1.

EMQX may replace Mosquitto locally when its dashboard or richer policy tooling is useful, but it must preserve the same topic and endpoint identity boundary.

## Edge Subscriber

After connecting, Edge subscribes with QoS 1 to exactly:

```text
icebot/execution-endpoints/{executionEndpointId}/commands/available
```

For every notification, including duplicates, Edge calls:

```http
POST /api/v1/iot/execution-endpoints/{endpointId}/commands/pull
```

Edge must also pull on a periodic timer and immediately after reconnect. MQTT receipt does not mark a command Delivered or Accepted; only the command pull/ack contracts do that.

## Edge Uplink

Publish topic:

```text
icebot/execution-endpoints/{executionEndpointId}/uplink/{messageType}
```

Allowed message types:

```text
heartbeat
telemetry-events
readiness
execution-report
production-events
state-summaries
```

Every payload uses this envelope:

```json
{
  "schemaVersion": 1,
  "messageId": "uuid",
  "sentAt": "2026-07-29T10:00:00Z",
  "payload": {}
}
```

Cloud result topic:

```text
icebot/execution-endpoints/{executionEndpointId}/uplink/results
```

Result:

```json
{
  "schemaVersion": 1,
  "messageId": "uuid",
  "endpointId": "uuid",
  "messageType": "readiness",
  "processedAt": "2026-07-29T10:00:01Z",
  "succeeded": true,
  "statusCode": 200,
  "retryable": false,
  "message": "Execution readiness applied.",
  "data": {}
}
```

Rules:

- QoS is 1 and retain is false in both directions.
- Broker PUBACK means only that the broker accepted the message.
- `messageId` correlates transport results; domain idempotency remains the
  event ID, sequence, revision, or command/report identity inside `payload`.
- Edge removes an outbox entry only after a matching successful or
  non-retryable application result.
- `retryable=true`, result timeout, disconnect, or broker failure retries the
  identical envelope and domain evidence identities.
- A `207` batch result is transport success; Edge must inspect per-item results
  and retain only failed/retryable items as defined by the owning contract.
- Message type selects a strict typed payload. Unknown fields, unknown enum
  values, unsupported schema versions, and topic/message mismatches are
  rejected.
- MQTT and HTTPS must use the same persistent source executor identity,
  event IDs, sequence numbers, state revisions, and command IDs.
- MQTT is not used for command pull/ack, checkpoint reads, artifact/file
  transfer, or signed object download.

## Production

Production must use:

- a broker endpoint reachable only from approved backend and Edge networks;
- TLS (`EdgeCommandMqtt__UseTls=true`) with a trusted broker certificate;
- a unique backend client id per backend instance;
- backend publish/subscribe credentials stored in the deployment secret manager;
- one revocable MQTT identity per execution endpoint;
- endpoint-scoped bidirectional ACLs and separate backend publish/subscribe ACLs;
- broker connection, authentication failure, publish failure, and client-session metrics;
- credential rotation coordinated with execution-endpoint provisioning.

HTTPS and MQTT credentials are separate. Never reuse the mTLS certificate or
signed-request credential as the MQTT password. Dynsec administrator and
backend publisher credentials come from deployment secrets, never appsettings
or Git.

Management lifecycle:

```text
POST   /api/v1/management/kiosks/{kioskId}/execution-endpoints/{id}/mqtt-credential   provision
PATCH  /api/v1/management/kiosks/{kioskId}/execution-endpoints/{id}/mqtt-credential   rotate
DELETE /api/v1/management/kiosks/{kioskId}/execution-endpoints/{id}/mqtt-credential   revoke
```

Provision and rotation return the password once together with:

- `subscribeTopic` for command wake-up;
- `uplinkPublishTopicPattern` for the allowed typed uplink message name;
- `uplinkResultTopic` for Cloud application results.

Broker mutation and database audit cannot share a distributed transaction.
The handler commits a durable `PendingProvision`, `PendingRotation`, or
`PendingRevoke` intent before broker I/O, uses idempotent broker mutation, and
finalizes only the credential version that created that intent. Broker failure
is recorded as `Failed` or `RevokeFailed`. A recent pending operation rejects a
concurrent mutation. The reconciliation job processes operations that remain
pending for five minutes: provisioning and rotation become `Failed` because the
one-time secret cannot be recovered after a crash, while revocation is claimed
with a new credential version and retried automatically. A stale `RevokeFailed`
operation is also retried. Provisioning or rotation can then be retried through
the same management endpoint, which replaces any broker credential whose secret
was not returned. Cancellation or a process crash cannot roll back a broker
mutation that already occurred. Durable command polling remains available while
MQTT credentials are repaired.

The job runs only when `MqttCredentialProvisioning:Enabled` is true. Configure
`ReconciliationIntervalSeconds` and `ReconciliationBatchSize` for scan cadence
and bounded work. It reports through the `mqtt_credential_reconciliation`
operational-automation metric and logs endpoint-level outcomes that require an
operator retry.

The dedicated `IceBot.MqttCredentialLifecycle` meter reports stale candidate
count, timeout transitions, reconciliation outcomes, and automatic revocation
retry results. A lease timeout opens or correlates
`MQTT_CREDENTIAL_OPERATION_TIMEOUT`; a failed automatic revocation retry opens
or correlates `MQTT_CREDENTIAL_REVOKE_FAILED`. These Error alerts are visible in
the normal tenant-scoped alert queue and through `AlertChanged`. A successful
manual or automatic repair resolves the active alert during the next scan.
An endpoint with a non-revoked MQTT credential cannot be retired; revoke the
broker client first. Disabling an endpoint blocks command pull/dispatch but does
not rotate or delete its MQTT identity, so reactivation can preserve the same
subscriber setup.

For multiple backend replicas, each replica needs a unique MQTT client id and
the same uplink consumer group. Shared subscriptions distribute each uplink
message to one replica. Retained wake-ups and uplink messages remain disabled;
durable command recovery comes from command pull, while durable uplink recovery
comes from the Edge local outbox and Cloud application result.

Backend wake-up outcomes are exported through
`icebot.mqtt.wakeup.publish.attempts`. Uplink outcomes and processing latency
use `icebot.mqtt.uplink.messages` and
`icebot.mqtt.uplink.processing.latency`. Broker-side
connection/session/authentication metrics remain owned by Mosquitto/EMQX and
should be scraped or exported separately; application metrics cannot observe
disconnects or rejected broker ACL operations that never reach the backend.

## Related Docs

- [Edge Command Contract](../iot/EDGE_COMMAND_CONTRACT.md)
- [Deployment Configuration](DEPLOYMENT_CONFIG.md)
- [Observability](OBSERVABILITY.md)
- [Restart And Power Recovery](RESTART_AND_POWER_RECOVERY.md)
