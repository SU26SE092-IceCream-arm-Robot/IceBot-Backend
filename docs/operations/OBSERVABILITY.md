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
| `icebot.mqtt.credentials.reconciliation.outcomes` | Counter | Durable MQTT credential reconciliation outcomes | `outcome` |
| `icebot.mqtt.credentials.operation.timeouts` | Counter | Provisioning or rotation operations that exceeded the five-minute lease | `operation` |
| `icebot.mqtt.credentials.revocation.retry.attempts` | Counter | Automatic stale revocation retries | `outcome` |
| `icebot.mqtt.credentials.stale.candidates` | Observable gauge | Stale credential operations selected by the latest scan | none |
| `icebot.payos.request.failures` | Counter | Final PayOS timeout, open-circuit, or transient failures | `provider`, `operation`, `failure.kind` |
| `icebot.payment_session.reconciliation.outcomes` | Counter | Recovery outcomes for incomplete provider-session creation responses | `outcome` |
| `icebot.payment_session.interventions` | Counter | Sessions requiring operator investigation after terminal reconciliation outcomes | `intervention` |
| `icebot.payment_session.reconciliation.pending_age` | Histogram (seconds) | Age of an incomplete session when a reconciliation attempt starts | none |
| `icebot.production_package.upgrade.preview` | Counter | Upgrade preview outcomes | `outcome`, `has_blockers` |
| `icebot.production_package.upgrade.materialization` | Counter | Successor materialization outcomes | `outcome` |
| `icebot.production_package.upgrade.cutover` | Counter | Cutover outcomes | `outcome` |
| `icebot.production_package.upgrade.rollback_attempt` | Counter | Created rollback deployment attempts | `profile`, `attempt_no` |
| `icebot.production_package.upgrade.rollback` | Counter | Aggregate rollback outcomes | `outcome` |
| `icebot.production_package.upgrade.rollback_pending_age` | Histogram (seconds) | Age of an upgrade waiting for rollback activation | none |
| `icebot.notification_delivery.outcomes` | Counter | Durable push delivery outcomes | `status`, `notification.type` |
| `icebot.notification_delivery.processing_lag` | Histogram (seconds) | Time from scheduled attempt to worker claim | none |
| `icebot.notification_delivery.due_batch_size` | Histogram | Number of due deliveries selected per scan | none |
| `icebot.notification.push.timed_out` | Counter | Firebase push attempts stopped by the per-delivery operation timeout; retry remains owned by the durable delivery worker | none |
| `icebot.automation.runs` | Counter | Outcome of operational reconciliation and retention runs | `automation.job`, `outcome` |
| `icebot.automation.candidate.failures` | Counter | Candidate-level failures isolated so later candidates or stages can continue | `automation.job` |
| `icebot.automation.run.duration` | Histogram (seconds) | Duration of each operational automation run or reconciliation stage | `automation.job`, `outcome` |
| `icebot.automation.last_success.unix_time` | Observable gauge | Unix timestamp of the last fully successful automation run | `automation.job` |
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
- Production-package upgrade tags are bounded outcomes only. Endpoint and upgrade identities remain in the typed detail/audit read model.
- Notification metrics use bounded status/type tags only. Delivery, account, kiosk, and tenant identifiers remain in diagnostics reads and logs.
- PayOS `failure.kind` is bounded to `timeout`, `circuit_open`, and `transient`; HTTP payment-creation `POST` requests are not retried.
- Payment-session reconciliation uses the persisted provider order code. `AwaitingWebhook` means provider lookup reported paid while Cloud is still waiting for the signed webhook; it must be investigated through order-scoped payment diagnostics rather than treated as fulfillment success.
- Alert on any sustained increase of `icebot.payment_session.interventions`, especially `AwaitingWebhook`, `IdentityMismatch`, and `AmountMismatch`. Use the tenant-scoped intervention queue to identify affected orders; metric tags intentionally contain no payment or order IDs.
- MQTT disabled is an explicit outcome, not a publish failure. Alert only on `outcome=failed` when MQTT is expected to be enabled.
- MQTT credential metrics use only bounded operation/outcome tags. Endpoint identity remains in structured logs and management alerts.
- Alert when an enabled automation job has no recent `last_success` update, when `partial_failure` is sustained, or when candidate failures grow. Inspect logs by job and candidate ID; IDs deliberately remain out of metric tags.

Suggested initial alerts:

- sustained increase of MQTT `failed` outcomes;
- any sustained non-zero `icebot.mqtt.credentials.stale.candidates` gauge;
- growth of `icebot.mqtt.credentials.operation.timeouts` or failed MQTT credential revocation retries;
- any `icebot.payment_session.interventions{intervention="IdentityMismatch"}` or
  `{intervention="AmountMismatch"}` occurrence;
- sustained non-zero
  `icebot.payment_session.interventions{intervention="AwaitingWebhook"}` or
  `{intervention="RetryExhausted"}`;
- payment-session reconciliation pending-age p95 above the configured stale and
  retry budget;
- p95 pull or ACK latency above the command expiry budget;
- p95 report lag above the report reconciliation threshold;
- non-zero Unreachable gauge for a sustained interval;
- growing Stale gauge combined with low heartbeat freshness.
- no successful operational-automation run within two expected job intervals; and
- sustained `icebot.notification.push.timed_out` growth while notification delivery remains enabled.

Exact thresholds are deployment-specific and should be tuned from observed baselines rather than hardcoded in application code.

## 5. Production Guidance

For production environments:
1. **Logs**: Continue using Serilog. You can add a Serilog sink to export directly to Seq, Loki, or Elasticsearch.
2. **Logs via OTLP**: If using an OTLP collector, enable `Observability:Serilog:OtlpSinkEnabled=true`.
3. **Traces/Metrics**: Enable the OTLP exporter (`Observability:OpenTelemetry:OtlpExporterEnabled=true`) and point `OtlpEndpoint` to an OpenTelemetry Collector or APM ingest endpoint (e.g., Jaeger, Prometheus, Datadog).
4. **Debug Body Logging**: Keep `Observability:DebugBodyLogging:Enabled = false` unless actively diagnosing a live production payload issue.

## Related Docs

- [Deployment Configuration](DEPLOYMENT_CONFIG.md)
- [MQTT Operations](MQTT_OPERATIONS.md)
- [Alert Lifecycle Flow](../flows/ALERT_LIFECYCLE_FLOW.md)
- [Restart And Power Recovery](RESTART_AND_POWER_RECOVERY.md)
