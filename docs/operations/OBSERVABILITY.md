# Observability

## Search Keywords

`observability`, `Serilog`, `OpenTelemetry`, `OTLP`, `Aspire Dashboard`, `trace`, `metric`, `structured log`, `debug body logging`

This document outlines the observability strategy for IceBot Backend. It uses a combination of **Serilog** for structured logging and **OpenTelemetry** for traces, metrics, and correlation.

## 1. Architecture Strategy

The observability boundary is separated into roles to avoid duplicating log noise and to keep systems focused:

- **Serilog**: The structured logging pipeline. Responsible for application logs, console output, and file-based logs (or forwarding to Seq/Loki).
- **OpenTelemetry (OTel)**: Handles traces, metrics, and correlation.
- **Aspire Dashboard**: The local developer tool for visualizing traces and metrics.
- **Debug Body Logging**: A temporary, config-gated "microscope" for debugging raw HTTP payloads.

> [!WARNING]
> Do not treat OpenTelemetry as a Serilog replacement. Logs remain owned by Serilog. When logs need to flow to OTLP/Aspire/collector, use the Serilog OpenTelemetry sink, not `OpenTelemetryLoggerProvider`.

## 2. Local Aspire Dashboard

To visualize OpenTelemetry traces, metrics, and optional Serilog OTLP logs locally, run the Aspire Dashboard from the sibling `IceBot-Tools` tooling compose:

```powershell
cd ..\IceBot-Tools
.\scripts\start_aspire_dashboard.ps1
```

- **UI endpoint**: `http://localhost:18888`
- **OTLP endpoint**: `http://localhost:18889`
- The dashboard container is owned by `IceBot-Tools` because it is tooling/dev observability, not a backend runtime dependency.
- The backend must still run without cloning or starting `IceBot-Tools`.

> [!NOTE]
> Aspire Dashboard is for local development **only**. Do not expose it to the public network.

## 3. Configuration

Observability settings are managed in `appsettings.json` under the `Observability` block:

```json
"Observability": {
  "ServiceName": "IceBot.WebAPI",
  "Serilog": {
    "OtlpSinkEnabled": false
  },
  "OpenTelemetry": {
    "Enabled": true,
    "OtlpExporterEnabled": false,
    "OtlpEndpoint": "http://localhost:18889",
    "OtlpProtocol": "grpc"
  },
  "DebugBodyLogging": {
    "Enabled": false,
    "LogRequestBody": true,
    "LogResponseBody": false,
    "MaxBodyLength": 1000
  }
}
```

### OTLP Exporter

- Set `OpenTelemetry:OtlpExporterEnabled: true` to export traces and metrics to the Aspire Dashboard or a production OTLP collector.
- Set `Serilog:OtlpSinkEnabled: true` to export structured logs through the Serilog OpenTelemetry sink to the same OTLP endpoint.
- `OtlpEndpoint` defines the destination.
- In `appsettings.Development.json`, both OTLP exporters default to `false` so the app doesn't depend on Aspire being available.

Serilog OTLP logging is separate from OpenTelemetry traces/metrics:

```text
ILogger<T>
  -> Serilog
      -> console/file
      -> optional Serilog OTLP sink

OpenTelemetry SDK
  -> traces/metrics
      -> optional OTLP exporter
```

Do not add `OpenTelemetryLoggerProvider` unless this decision is explicitly revisited. It would create a second logging provider path and blur the boundary that Serilog owns application logs.

### Debug Body Logging

Dashboard and OTel act as your "radar" (showing what failed and where). Debug body logging is your "microscope" (showing the exact payload).

- Body logging is **disabled by default**.
- It is expensive and risks exposing sensitive data.
- When `Enabled: true`, it safely truncates payloads over `MaxBodyLength` and masks sensitive fields (like passwords/tokens).
- Authentication, password, and webhook endpoints are explicitly ignored by the middleware for safety.

## 4. Edge Runtime Metrics

The `IceBot.EdgeIntegration` meter is registered with the existing OpenTelemetry metrics pipeline. These metrics describe machine-integration latency and failure; ASP.NET instrumentation continues to own ordinary HTTP duration/error metrics.

The `IceBot.Payments.PayOS` meter adds provider-specific failure classification on top of ordinary HTTP client metrics. Payment identifiers are intentionally excluded from metric tags.

| Metric | Type | Meaning | Bounded tags |
| --- | --- | --- | --- |
| `icebot.mqtt.wakeup.publish.attempts` | Counter | MQTT wake-up outcomes, including disabled/succeeded/failed | `outcome`, `command.type` |
| `icebot.payos.request.failures` | Counter | Final PayOS timeout, open-circuit, or transient failures | `provider`, `operation`, `failure.kind` |
| `icebot.edge.command.pull.latency` | Histogram (seconds) | Durable command creation until it is returned by command pull | `command.type` |
| `icebot.edge.command.ack.latency` | Histogram (seconds) | Command delivery until Cloud receives the first state-changing ACK | `command.type`, `ack.status` |
| `icebot.edge.execution.report.lag` | Histogram (seconds) | Executor-reported timestamp until Cloud receives a new report | `report.type` |
| `icebot.edge.execution.observation.transitions` | Counter | Transitions to stale/unreachable customer observations | `observation.status`, `customer.status` |
| `icebot.edge.execution.stale.age` | Histogram (seconds) | Age of the last executor report at stale/unreachable transition | `observation.status` |
| `icebot.edge.execution.observed` | Observable gauge | Current active execution projections that are Stale or Unreachable | `observation.status` |

Rules:

- Metrics are recorded only after their owning database commit succeeds.
- Duplicate ACKs and duplicate execution reports do not add latency/transition measurements.
- The stale/unreachable gauge is refreshed from PostgreSQL every 30 seconds; it is not an in-memory lifecycle counter.
- IDs such as command, order, kiosk, endpoint, or device must never be metric tags. Use traces/logs for entity-level investigation.
- PayOS `failure.kind` is bounded to `timeout`, `circuit_open`, and `transient`; HTTP payment-creation `POST` requests are not retried.
- MQTT disabled is an explicit outcome, not a publish failure. Alert only on `outcome=failed` when MQTT is expected to be enabled.

Suggested initial alerts:

- sustained increase of MQTT `failed` outcomes;
- p95 pull or ACK latency above the command expiry budget;
- p95 report lag above the report reconciliation threshold;
- non-zero Unreachable gauge for a sustained interval;
- growing Stale gauge combined with low heartbeat freshness.

Exact thresholds are deployment-specific and should be tuned from observed baselines rather than hardcoded in application code.

## 5. Production Guidance

For production environments:
1. **Logs**: Continue using Serilog. You can add a Serilog sink to export directly to Seq, Loki, or Elasticsearch.
2. **Logs via OTLP**: If using an OTLP collector, enable `Observability:Serilog:OtlpSinkEnabled=true`.
3. **Traces/Metrics**: Enable the OTLP exporter (`Observability:OpenTelemetry:OtlpExporterEnabled=true`) and point `OtlpEndpoint` to an OpenTelemetry Collector or APM ingest endpoint (e.g., Jaeger, Prometheus, Datadog).
4. **Debug Body Logging**: Keep `Observability:DebugBodyLogging:Enabled = false` unless actively diagnosing a live production payload issue.
