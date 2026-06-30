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
docker compose --profile mqtt up -d mqtt-init mosquitto
```

Provision one MQTT subscriber after its execution endpoint exists:

```powershell
.\mqtt\provision-endpoint.ps1 `
  -ExecutionEndpointId "00000000-0000-0000-0000-000000000000" `
  -Password "local-edge-secret"
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

EMQX may replace Mosquitto locally when its dashboard or richer policy tooling is useful, but it must preserve the same topic and endpoint identity boundary.

## Edge Subscriber

After connecting, Edge subscribes with QoS 1 to exactly:

```text
icebot/execution-endpoints/{executionEndpointId}/commands/available
```

For every notification, including duplicates, Edge calls:

```http
POST /api/v1/iot/kiosks/{kioskId}/commands/pull
X-Execution-Endpoint-Id: {executionEndpointId}
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

Do not reuse the HTTPS execution credential as an MQTT password unless the provisioning design explicitly manages both protocols as one rotatable credential bundle. Never put broker credentials or private keys in appsettings or Git.

For multiple backend replicas, each replica needs a unique MQTT client id. Retained wake-ups remain disabled; durable command recovery comes from command pull, not broker retention.
