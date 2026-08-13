# Prometheus And Grafana Handoff

## Search Keywords

`Prometheus`, `Grafana`, `OpenTelemetry Collector`, `OTLP`, `metrics`, `dashboard`, `alert`, `DevOps`, `monitoring`

## Purpose

This is the Backend-to-DevOps contract for production metrics. It does not provision, configure, or require Collector, Prometheus, Grafana, TLS, storage, or alert channels.

## Topology

```text
IceBot Backend -- OTLP --> OpenTelemetry Collector -- Prometheus exporter --> Prometheus --> Grafana
```

Aspire remains a local debugging tool at `localhost`; it is not part of this topology.

## Backend Contract

- Protocol: OTLP gRPC by default, or HTTP/protobuf when configured.
- Metrics and traces have independent exporter switches and endpoints.
- Resource attributes: `service.name`, `service.version`, `service.instance.id`, `deployment.environment.name`.
- `service.name` defaults to `IceBot.WebAPI`.
- Backend must start and process traffic if the Collector, Prometheus, or Grafana is unavailable.
- Metrics never include organization, kiosk, account, order, payment, command, endpoint, deployment, or artifact identifiers as labels.

Configure only the enabled signal with environment variables such as:

```text
Observability__OpenTelemetry__Metrics__ExporterEnabled=true
Observability__OpenTelemetry__Metrics__OtlpEndpoint=http://collector.internal:4317
Observability__OpenTelemetry__Tracing__ExporterEnabled=true
Observability__OpenTelemetry__Tracing__OtlpEndpoint=http://collector.internal:4317
Observability__DeploymentEnvironment=Production
Observability__InstanceId=<deployment-replica-id>
```

## DevOps Responsibilities

- Run and secure the Collector OTLP receiver.
- Expose a Collector Prometheus scrape endpoint only on the required private network.
- Configure Prometheus scrape interval, retention, disk limits, and target health.
- Configure Grafana data source, authentication, notification channels, dashboard and alert provisioning.
- Monitor host, PostgreSQL, object storage, MQTT broker, and Redis separately with their appropriate exporters.
- Validate Collector, Prometheus, and Grafana outages do not alter Backend request or job behavior.

## Initial Dashboard And Alert Intent

- Platform: request/error rate, latency, process/runtime health, readiness.
- Edge: command pull/ACK, execution report lag, stale or unreachable projections, deployment failures.
- Durable workflows: automation last success, run duration, candidate failures, payment and notification failures.
- Dependencies: PostgreSQL, Redis when enabled, object storage, MQTT broker, VPS disk/CPU/RAM.

Provision alert rules as code. Each alert needs an owner, severity, pending duration, no-data behavior, recovery condition, and runbook. Thresholds must come from staging observations, not Backend constants.

## Related Docs

- [Observability](OBSERVABILITY.md)
- [Deployment Configuration](DEPLOYMENT_CONFIG.md)
