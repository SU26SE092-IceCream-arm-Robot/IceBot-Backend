# Deployment Configuration

This document lists the minimum backend configuration that must be provided outside source control before running a deployed environment.

## Search Keywords

`deployment`, `backend config`, `environment variables`, `appsettings`, `JWT`, `database connection`, `Firebase`, `SMTP`, `PayOS`, `PORT`, `health`, `info`

## Configuration Source

The WebAPI loads configuration in this order:

```text
appsettings.json
appsettings.{Environment}.json
environment variables
```

Use environment variables or deployment secrets for real credentials. Do not rely on sample values in `appsettings.json` outside local development.

## Docker Compose Boundary

Backend docker compose, when added, should contain only backend app runtime dependencies such as PostgreSQL, Redis, and backend-owned infrastructure. Do not require `IceBot-Tools` to run the backend.

Tooling infrastructure such as Qdrant, RAG services, local model caches, and agent automation belongs in the `IceBot-Tools` compose lifecycle. If backend and tools need to communicate locally, use environment variables such as `RAG_API_URL` or an explicitly shared Docker network.

## Required Runtime Settings

| Area | Configuration key |
| --- | --- |
| Database | `ConnectionStrings__IceBot_DB` |
| JWT secret | `Authentication__Jwt__Secret` |
| JWT issuer | `Authentication__Jwt__Issuer` |
| JWT audience | `Authentication__Jwt__Audience` |
| Email host | `Email__Host` |
| Email port | `Email__Port` |
| Email sender | `Email__From` |
| Email display name | `Email__DisplayName` |
| Email username | `Email__UserName` |
| Email password | `Email__Password` |
| Email TLS toggle | `Email__EnableSsl` |
| Password reset frontend URL | `Email__PasswordResetBaseUrl` |
| Invitation frontend URL | `Email__InvitationBaseUrl` |
| Firebase enabled flag | `Firebase__Enabled` |
| Firebase credentials path | `Firebase__CredentialsPath` |
| PayOS client id | `PayOS__ClientId` |
| PayOS API key | `PayOS__ApiKey` |
| PayOS checksum key | `PayOS__ChecksumKey` |
| PayOS base URL | `PayOS__BaseUrl` |
| PayOS return URL | `PayOS__ReturnUrl` |
| PayOS cancel URL | `PayOS__CancelUrl` |
| Browser frontend origins | `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, ... |
| Expose stack traces | `ErrorHandling__ExposeStackTrace` |
| Enable Serilog OTLP log sink | `Observability__Serilog__OtlpSinkEnabled` |
| Enable OpenTelemetry OTLP export | `Observability__OpenTelemetry__OtlpExporterEnabled` |
| OpenTelemetry OTLP endpoint | `Observability__OpenTelemetry__OtlpEndpoint` |
| Enable debug body logging | `Observability__DebugBodyLogging__Enabled` |
| Diagnostics API key | `Diagnostics__ApiKey` |
| Diagnostics realtime external ping toggle | `Diagnostics__EnableExternalPing` |
| Diagnostics realtime external ping timeout seconds | `Diagnostics__ExternalPingTimeoutSeconds` |
| Public port, if hosting platform injects it | `PORT` |

## Operational Endpoints

Use these for deployment checks:

```text
GET /health
GET /health/ready
GET /management/diagnostics/health
GET /info
```

`/info` may include build metadata if these values are provided:

```text
BUILD_COMMIT
BUILD_TIME
```

For CI/CD diagnostics:

```http
GET /management/diagnostics/health
X-Diagnostics-Key: <Diagnostics__ApiKey>
```

This endpoint returns safe checks for database connectivity, migration status, and required config presence. It does not return secret values.

Realtime SMTP, Firebase, and PayOS checks are disabled by default. Enable them only for CI/CD or controlled diagnostics:

```text
Diagnostics__EnableExternalPing=true
Diagnostics__ExternalPingTimeoutSeconds=5
```

When enabled, diagnostics performs provider reachability checks without sending email or creating payment sessions. `/health/ready` still checks database readiness only.

## Notes

- CORS allows any origin only in Development when no origin is configured. Deployed environments must set `Cors__AllowedOrigins__0` and additional indexed values as needed.
- Firebase can be disabled with `Firebase__Enabled=false`, but Google/Firebase login paths will then return service-unavailable behavior.
- SMTP failures must not make account onboarding unrecoverable; admins can resend invitations.
- PayOS webhook/payment behavior depends on correct public return/cancel URLs and checksum key.
- Set `ErrorHandling__ExposeStackTrace=false` and `Observability__DebugBodyLogging__Enabled=false` in deployed environments.
- For production observability, set `Observability__OpenTelemetry__OtlpExporterEnabled=true` for traces/metrics and `Observability__Serilog__OtlpSinkEnabled=true` for structured logs, then configure the OTLP endpoint to point to your collector.
- Set `Diagnostics__ApiKey` outside Development before using `/management/diagnostics/health`.
- Keep `Diagnostics__EnableExternalPing=false` unless the deployment check intentionally needs live SMTP/Firebase/PayOS reachability.
