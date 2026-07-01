# MQTT Operations

## Ownership

MQTT is a best-effort command-available wake-up channel. `EdgeCommand` in PostgreSQL and the authenticated command-pull API remain authoritative.

```text
Cloud commits EdgeCommand
-> Cloud publishes CommandAvailable
-> Edge receives endpoint-scoped wake-up
-> Edge calls command pull
-> Edge validates, accepts, and executes the durable command
```

The wake-up contains no executable payload. Duplicate and missing MQTT messages are expected and harmless because Edge deduplicates pulled commands and also polls periodically.

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
dotnet run --project .\src\WebAPI\WebAPI.csproj
```

Local ACL boundary:

- `icebot-backend` may publish `icebot/execution-endpoints/+/commands/available`.
- An Edge MQTT username must equal its `executionEndpointId` UUID.
- That endpoint may subscribe only to `icebot/execution-endpoints/{executionEndpointId}/commands/available`.
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

## Production

Production must use:

- a broker endpoint reachable only from approved backend and Edge networks;
- TLS (`EdgeCommandMqtt__UseTls=true`) with a trusted broker certificate;
- a unique backend client id per backend instance;
- backend publish credentials stored in the deployment secret manager;
- one revocable MQTT identity per execution endpoint;
- endpoint-scoped subscribe ACLs and a backend-only publish ACL;
- broker connection, authentication failure, publish failure, and client-session metrics;
- credential rotation coordinated with execution-endpoint provisioning.

HTTPS and MQTT credentials are separate. Never reuse the mTLS certificate or
signed-request credential as the MQTT password. Dynsec administrator and
backend publisher credentials come from deployment secrets, never appsettings
or Git.

Management lifecycle:

```text
POST   /api/v1/management/execution-endpoints/{id}/mqtt-credential   provision
PATCH  /api/v1/management/execution-endpoints/{id}/mqtt-credential   rotate
DELETE /api/v1/management/execution-endpoints/{id}/mqtt-credential   revoke
```

Broker mutation and database audit cannot share a distributed transaction.
The handler serializes each endpoint operation and uses idempotent broker
upsert. A failed or interrupted operation is retried through the same endpoint;
durable command polling remains available while MQTT credentials are repaired.
An endpoint with a non-revoked MQTT credential cannot be retired; revoke the
broker client first. Disabling an endpoint blocks command pull/dispatch but does
not rotate or delete its MQTT identity, so reactivation can preserve the same
subscriber setup.

For multiple backend replicas, each replica needs a unique MQTT client id. Retained wake-ups remain disabled; durable command recovery comes from command pull, not broker retention.

Backend publish outcomes are exported through `icebot.mqtt.wakeup.publish.attempts`. Broker-side connection/session/authentication metrics remain owned by Mosquitto/EMQX and should be scraped or exported separately; application metrics cannot observe subscriber disconnects that never reach the backend.
