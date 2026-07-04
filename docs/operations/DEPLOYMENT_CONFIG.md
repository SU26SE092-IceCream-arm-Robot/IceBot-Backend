# Deployment Configuration

This document classifies backend deployment configuration by priority and source. Not every setting must be repeated as an environment variable: safe, environment-independent defaults may remain in `appsettings.json`.

## Search Keywords

`deployment`, `backend config`, `environment variables`, `appsettings`, `JWT`, `database connection`, `Firebase`, `SMTP`, `PayOS`, `MinIO`, `S3`, `robot artifact storage`, `PORT`, `health`, `info`

## Configuration Source

The WebAPI loads configuration in this order:

```text
appsettings.json
appsettings.{Environment}.json
environment variables
```

Use environment variables or a deployment secret store for credentials and environment-specific addresses. Values marked **Use appsettings default** below do not need to be repeated unless the deployment intentionally changes them.

## Docker Compose Boundary

Backend docker compose, when added, should contain only backend app runtime dependencies such as PostgreSQL, Redis, and backend-owned infrastructure. Do not require `IceBot-Tools` to run the backend.

Tooling infrastructure such as Qdrant, RAG services, local model caches, and agent automation belongs in the `IceBot-Tools` compose lifecycle. If backend and tools need to communicate locally, use environment variables such as `RAG_API_URL` or an explicitly shared Docker network.

## Configuration Priority

| Priority | Meaning |
| --- | --- |
| **P0 Core** | Required for every deployed backend. Deployment must provide or explicitly verify it. |
| **P0 Feature** | Required when the named feature is enabled or used in that environment. |
| **P1** | Production-sensitive. A default exists, but the operator must review it for the target environment. |
| **P2** | Operational tuning or optional integration. Use the `appsettings` default until there is a reason to override it. |

`Secret/env required` means the checked-in value is a placeholder, local credential, or environment-specific address and must not be used as a deployed value. `Use appsettings default` means omission from environment variables is intentional and supported.

## Core And Feature Credentials

| Area | Configuration key | Priority | Deploy action |
| --- | --- | --- | --- |
| Database connection | `ConnectionStrings__IceBot_DB` | **P0 Core** | **Secret/env required.** Never deploy the checked-in local connection string. |
| JWT signing secret | `Authentication__Jwt__Secret` | **P0 Core** | **Secret/env required.** Use a strong environment-specific secret. |
| JWT issuer | `Authentication__Jwt__Issuer` | **P1** | Use appsettings default only if `IceBotApp` is the intended issuer. |
| JWT audience | `Authentication__Jwt__Audience` | **P1** | Use appsettings default only if `IceBotUsers` is the intended audience. |
| Browser frontend origins | `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, ... | **P0 Core** | **Env required for browser deployments.** Production does not use the Development allow-any fallback. |
| Public hosting port | `PORT` | **P0 Feature** | Provide only when the hosting platform injects a public port; otherwise use normal ASP.NET hosting configuration. |
| Diagnostics API key | `Diagnostics__ApiKey` | **P0 Feature** | **Secret/env required** before exposing management diagnostics outside Development. |

## Initial SystemAdmin Bootstrap

These values are required only while an environment has no active `SystemAdmin` role assignment. They are not normal long-lived application settings.

| Area | Configuration key | Direct environment alias | Priority | Deploy action |
| --- | --- | --- | --- | --- |
| Admin username | `BootstrapAdmin__UserName` | `BOOTSTRAP_ADMIN_USERNAME` | **P0 Core, first deployment** | Environment-specific value required for initial bootstrap. |
| Admin email | `BootstrapAdmin__Email` | `BOOTSTRAP_ADMIN_EMAIL` | **P0 Core, first deployment** | Environment-specific value required for initial bootstrap. |
| Admin password | `BootstrapAdmin__Password` | `BOOTSTRAP_ADMIN_PASSWORD` | **P0 Core, first deployment** | **Secret store/env required. Never put this value in appsettings or source control.** |
| Admin display name | `BootstrapAdmin__FullName` | `BOOTSTRAP_ADMIN_FULLNAME` | **P2** | Optional; defaults to `Bootstrap System Admin`. |

Bootstrap lifecycle:

1. Supply username, email, and a strong generated password through the deployment secret store.
2. Start the backend and confirm that the account has the active `SystemAdmin` role.
3. Log in and rotate the password through the normal account flow.
4. Remove all bootstrap admin values from deployment configuration.

The hosted bootstrap exits without reading these values when an active `SystemAdmin` already exists. Keeping them after bootstrap is still discouraged: if all active SystemAdmin assignments are later removed, a restart could match/create the configured account, reset its password, and grant `SystemAdmin` again.

## Email And Identity Providers

| Area | Configuration key | Priority | Deploy action |
| --- | --- | --- | --- |
| Email host | `Email__Host` | **P0 Feature** | Required when invitation/password-reset email delivery is enabled. |
| Email username | `Email__UserName` | **P0 Feature** | **Secret/env required** when SMTP authentication is used. |
| Email password | `Email__Password` | **P0 Feature** | **Secret/env required** when SMTP authentication is used. |
| Email sender | `Email__From` | **P0 Feature** | Required for email delivery; do not use the sample address. |
| Password reset frontend URL | `Email__PasswordResetBaseUrl` | **P0 Feature** | Required before password-reset links are issued. |
| Invitation frontend URL | `Email__InvitationBaseUrl` | **P0 Feature** | Required before invitation links are issued. |
| Email port | `Email__Port` | **P1** | Review for the SMTP provider; appsettings defaults to `587`. |
| Email TLS mode | `Email__EnableSsl` | **P1** | Review with the selected SMTP port/provider. |
| SMTP operation timeout | `Email__OperationTimeoutSeconds` | **P2** | Defaults to `30` seconds for connect/authenticate/send/disconnect. Email delivery is not retried without an outbox delivery identity. |
| Email display name | `Email__DisplayName` | **P2** | Use appsettings default or override branding. |
| Firebase enabled flag | `Firebase__Enabled` | **P1** | Explicitly set `false` when Google/Firebase login is not deployed. |
| Firebase credentials path | `Firebase__CredentialsPath` | **P0 Feature** | **Secret-mounted path required** when Firebase is enabled outside an environment that supplies application-default credentials. |
| Firebase auth resilience | `Firebase__Resilience__OperationTimeoutSeconds`, `__RetryCount`, `__RetryDelayMilliseconds`, `__CircuitBreakerFailureRatio`, `__CircuitBreakerMinimumThroughput`, `__CircuitBreakerSamplingDurationSeconds`, `__CircuitBreakerBreakDurationSeconds` | **P2** | Defaults are suitable initially. Only transport and explicit Firebase service failures retry; invalid/expired/revoked tokens never retry. Settings are startup-validated. |

## Payment Provider

| Area | Configuration key | Priority | Deploy action |
| --- | --- | --- | --- |
| PayOS client id | `PayOS__ClientId` | **P0 Feature** | **Secret/env required** when PayOS payment is enabled. |
| PayOS API key | `PayOS__ApiKey` | **P0 Feature** | **Secret/env required** when PayOS payment is enabled. |
| PayOS checksum key | `PayOS__ChecksumKey` | **P0 Feature** | **Secret/env required** for request/webhook integrity. |
| PayOS return URL | `PayOS__ReturnUrl` | **P0 Feature** | Environment-specific public URL required for checkout. |
| PayOS cancel URL | `PayOS__CancelUrl` | **P0 Feature** | Environment-specific public URL required for checkout cancellation. |
| PayOS base URL | `PayOS__BaseUrl` | **P2** | Use the appsettings provider URL unless PayOS changes the endpoint or a test stub is used. |
| PayOS resilience | `PayOS__Resilience__AttemptTimeoutSeconds`, `__TotalTimeoutSeconds`, `__CircuitBreakerFailureRatio`, `__CircuitBreakerMinimumThroughput`, `__CircuitBreakerSamplingDurationSeconds`, `__CircuitBreakerBreakDurationSeconds` | **P2** | Dependency-specific timeout and circuit settings. Payment-creation `POST` retry remains disabled. Settings are startup-validated. |

## Robot Artifact Storage

| Area | Configuration key | Priority | Deploy action |
| --- | --- | --- | --- |
| Object-storage endpoint | `RobotArtifacts__ObjectStorage__Endpoint` | **P0 Feature** | Environment-specific internal S3/MinIO endpoint required for artifact upload. |
| Edge-reachable download endpoint | `RobotArtifacts__ObjectStorage__DownloadEndpoint` | **P0 Feature** | Environment-specific endpoint required; it must be reachable by Edge/controllers. |
| Object-storage access key | `RobotArtifacts__ObjectStorage__AccessKey` | **P0 Feature** | **Secret/env required.** Do not deploy `minioadmin`. |
| Object-storage secret key | `RobotArtifacts__ObjectStorage__SecretKey` | **P0 Feature** | **Secret/env required.** Do not deploy `minioadmin`. |
| Object-storage bucket | `RobotArtifacts__ObjectStorage__BucketName` | **P1** | Appsettings default is acceptable only when the deployment uses the same private bucket name. |
| Auto-create bucket | `RobotArtifacts__ObjectStorage__AutoCreateBucket` | **P0 Feature** | Keep `false` in production and provision the bucket through infrastructure. Development and the local backend compose may set `true`. |
| Read-only storage resilience | `RobotArtifacts__ObjectStorage__ReadRetryCount`, `__ReadRetryDelayMilliseconds` | **P2** | Defaults to two retries with 200 ms base delay for stat, bucket check, and presigned URL only. Upload streams are not retried. |
| Storage TLS toggle | `RobotArtifacts__ObjectStorage__UseSsl` | **P1** | Review for the internal storage endpoint; production usually requires TLS. |
| Download TLS toggle | `RobotArtifacts__ObjectStorage__DownloadUseSsl` | **P1** | Review for the Edge-facing endpoint; production should use TLS. |
| Presigned URL lifetime | `RobotArtifacts__ObjectStorage__DownloadUrlExpirySeconds` | **P2** | Use appsettings default `900` seconds unless deployment latency requires tuning. |
| Enable orphan cleanup | `RobotArtifacts__ObjectStorage__OrphanCleanupEnabled` | **P1** | Keep the appsettings default `true` unless cleanup is owned externally. |
| Orphan grace period | `RobotArtifacts__ObjectStorage__OrphanGracePeriodHours` | **P2** | Use appsettings default `24`. |
| Orphan cleanup interval | `RobotArtifacts__ObjectStorage__OrphanCleanupIntervalHours` | **P2** | Use appsettings default `24`. |
| Cleanup delete limit | `RobotArtifacts__ObjectStorage__OrphanCleanupMaxDeletesPerRun` | **P2** | Use appsettings default `100`. |

Object storage is validated before background jobs start. Connection failure, invalid credentials, or a missing bucket while `AutoCreateBucket=false` stops application startup. This prevents the API from reporting healthy while artifact upload and deployment are unavailable.

## Runtime Safety And Capacity

| Area | Configuration key | Priority | Deploy action |
| --- | --- | --- | --- |
| Enable deployment reconciliation | `DeploymentTimeoutReconciliation__Enabled` | **P1** | Keep the appsettings default `true` for active Edge/controller deployments. |
| Reconciliation interval | `DeploymentTimeoutReconciliation__IntervalSeconds` | **P2** | Use appsettings default `60`. |
| Commands per reconciliation run | `DeploymentTimeoutReconciliation__MaxCommandsPerRun` | **P2** | Use appsettings default `100`. |
| Accepted-report timeout | `DeploymentTimeoutReconciliation__AcceptedReportTimeoutMinutes` | **P1** | Review against expected download/install duration; default is `30`. |
| Installed-activation timeout | `DeploymentTimeoutReconciliation__InstalledActivationTimeoutMinutes` | **P1** | Review against expected activation duration; default is `30`. |
| Signed-request clock skew | `ExecutionEndpointSecurity__SignedRequestMaxClockSkewSeconds` | **P1** | Review device clock quality; default is `300`. |
| Signed-request nonce retention | `ExecutionEndpointSecurity__NonceRetentionSeconds` | **P1** | Must remain longer than the accepted replay window; default is `900`. |
| IoT request body limit | `ExecutionEndpointSecurity__MaxRequestBodyBytes` | **P1** | Review against command/report payload size; default is 1 MiB. |
| Low-cost artifact count | `LowCostControllerCapacity__MaxArtifactCount` | **P1** | Configure from the supported controller profile; default is `50`. |
| Low-cost artifact bytes | `LowCostControllerCapacity__MaxArtifactStorageBytes` | **P1** | Configure from controller storage capacity; default is 50 MiB. |
| Enable order execution dispatch | `OrderExecutionDispatch__Enabled` | **P1** | Keep enabled when paid machine-produced orders must be dispatched to Edge. |
| Execute-order command expiry | `OrderExecutionDispatch__CommandExpiryMinutes` | **P1** | Review against kiosk queue and customer-wait policy; default is `30`. |
| Active commands per endpoint | `OrderExecutionDispatch__MaxActiveCommandsPerEndpoint` | **P1** | Set from real Edge capacity; default is `20`. |
| Dispatch reconciliation interval | `OrderExecutionDispatch__ReconciliationIntervalSeconds` | **P2** | Use appsettings default `10` unless database load requires tuning. |
| Dispatch reconciliation batch size | `OrderExecutionDispatch__ReconciliationBatchSize` | **P2** | Use appsettings default `50` unless recovery volume requires tuning. |
| Execution-timeout reconciliation interval | `OrderExecutionDispatch__TimeoutReconciliationIntervalSeconds` | **P2** | Use appsettings default `30`. |
| Execution-timeout batch size | `OrderExecutionDispatch__TimeoutReconciliationBatchSize` | **P2** | Use appsettings default `100`. |
| Accepted report timeout | `OrderExecutionDispatch__AcceptedReportTimeoutMinutes` | **P1** | Maximum time after ACK before missing order-summary evidence becomes stale; default is `5`. |
| Running report timeout | `OrderExecutionDispatch__RunningReportTimeoutMinutes` | **P1** | Maximum silence while running before observation becomes stale; default is `30`. |
| Heartbeat unreachable threshold | `OrderExecutionDispatch__HeartbeatUnreachableMinutes` | **P1** | Missing, Offline, or older heartbeat changes stale observation to unreachable; default is `2`. |
| Unreachable support escalation | `OrderExecutionDispatch__UnreachableSupportEscalationMinutes` | **P1** | Prolonged silence escalates customer projection from `PendingRecovery` to `SupportRequired` without changing the business order to Failed; default is `15`. |
| Maximum order dispatch attempts | `OrderExecutionDispatch__MaxDispatchAttempts` | **P1** | Hard ceiling across initial dispatch and operator redispatch; default is `3`. |
| Execution-report future clock skew | `ExecutionReportIngestion__MaxFutureClockSkewSeconds` | **P1** | Maximum accepted future offset for report and stock-evidence timestamps; default is `300` seconds. |
| Edge telemetry future clock skew | `EdgeTelemetryIngestion__MaxFutureClockSkewSeconds` | **P1** | Maximum accepted future offset for heartbeat and future device-event timestamps; default is `300` seconds. |
| Kiosk heartbeat timeout | `EdgeTelemetryIngestion__HeartbeatTimeoutSeconds` | **P1** | Maximum Cloud receive-time silence before an Active kiosk becomes Offline/Unreachable; default is `90` seconds. |
| Connectivity reconciliation interval | `EdgeTelemetryIngestion__ConnectivityReconciliationIntervalSeconds` | **P1** | Background scan interval for heartbeat timeout transitions; default is `15` seconds. |
| Connectivity reconciliation batch size | `EdgeTelemetryIngestion__ConnectivityReconciliationBatchSize` | **P2** | Maximum Active kiosk candidates checked per scan; appsettings default is `100`. |
| Edge batch event count | `EdgeTelemetryIngestion__MaxBatchEventCount` | **P1** | Maximum items accepted by one telemetry replay or production-history replay request; default is `100`. Request body size remains bounded separately by execution-endpoint security. |
| Enable retention purge | `DataRetention__Enabled` | **P1** | Default `true`; disable only for controlled maintenance/debugging. |
| Retention schedule | `DataRetention__IntervalHours` | **P2** | Default `24` hours. The job runs once at startup, then on this interval. |
| Raw telemetry retention | `DataRetention__HeartbeatDays`, `DataRetention__DeviceEventDays`, `DataRetention__OperationLogDays` | **P1** | Defaults: heartbeat `30`, device events `90`, operation logs `90` days. Ticket-referenced device events are protected. |
| Processed inbox retention | `DataRetention__ProcessedSyncInboxDays` | **P1** | Default `180` days. Applies only to Processed/Ignored rows without a dead-letter reference. |
| Retention work limits | `DataRetention__BatchSize`, `DataRetention__MaxBatchesPerRun` | **P1** | Defaults `1000` rows per SQL delete and `20` batches per entity per run. Tune against database load. |
| Enable MQTT command wake-up | `EdgeCommandMqtt__Enabled` | **P1** | Default `false`; enable only when a broker and endpoint subscriptions are configured. Polling remains authoritative. |
| MQTT broker host/port | `EdgeCommandMqtt__Host`, `EdgeCommandMqtt__Port` | **P0 Feature** | Required when MQTT wake-up is enabled. Defaults are `localhost:1883` for local development only. |
| MQTT TLS | `EdgeCommandMqtt__UseTls` | **P0 Secret/Security** | Enable for production broker connections. Certificate trust uses the host OS trust store. |
| MQTT credentials | `EdgeCommandMqtt__Username`, `EdgeCommandMqtt__Password` | **P0 Secret** | Supply through deployment secrets when broker authentication is enabled; do not commit values. |
| MQTT client/topic | `EdgeCommandMqtt__ClientId`, `EdgeCommandMqtt__TopicPrefix` | **P1** | Client id must be unique per backend instance; topic prefix defaults to `icebot`. |
| MQTT publish resilience | `EdgeCommandMqtt__ConnectTimeoutSeconds`, `EdgeCommandMqtt__PublishTimeoutSeconds`, `EdgeCommandMqtt__PublishRetryCount`, `EdgeCommandMqtt__PublishRetryDelayMilliseconds` | **P2** | Defaults to 5-second connect timeout, 6-second attempt timeout, one retry, and 250 ms base delay. Keep retries low because periodic pull is authoritative. |
| MQTT credential provisioning | `MqttCredentialProvisioning__Enabled`, `__Provider`, `__Host`, `__Port`, `__UseTls` | **P0 Feature/Security** | Enables execution-endpoint subscriber provisioning through Mosquitto Dynamic Security. Disabled by default. Production requires TLS. |
| MQTT dynsec administrator | `MqttCredentialProvisioning__AdminUsername`, `__AdminPassword` | **P0 Secret** | Broker-control identity used only for client credential lifecycle. Supply from secret manager; never reuse backend publisher or endpoint credentials. |
| MQTT endpoint subscriber role | `MqttCredentialProvisioning__SubscriberRole` | **P1** | Must exist on the broker and restrict `%u` to `icebot/execution-endpoints/%u/commands/available`. Local bootstrap creates `icebot-endpoint-subscriber`. |
| MQTT credential resilience | `MqttCredentialProvisioning__TimeoutSeconds`, `__RetryCount`, `__RetryDelayMilliseconds` | **P2** | Defaults to 10-second command timeout, one retry, and 500 ms base delay. Retry covers transport failure only; broker business errors are returned without retry. |

Broker startup, endpoint-scoped ACL provisioning, Edge subscription behavior, and production TLS rules are defined in [MQTT Operations](MQTT_OPERATIONS.md).

## Observability And Diagnostics

| Area | Configuration key | Priority | Deploy action |
| --- | --- | --- | --- |
| Expose stack traces | `ErrorHandling__ExposeStackTrace` | **P1** | Use the safe appsettings default `false`. |
| Serilog OTLP sink | `Observability__Serilog__OtlpSinkEnabled` | **P2** | Default `false`; enable when an OTLP log collector is deployed. |
| OpenTelemetry OTLP export | `Observability__OpenTelemetry__OtlpExporterEnabled` | **P2** | Default `false`; enable when an OTLP collector is deployed. |
| OpenTelemetry endpoint | `Observability__OpenTelemetry__OtlpEndpoint` | **P0 Feature** | Required when either OTLP exporter is enabled; environment-specific. |
| Debug body logging | `Observability__DebugBodyLogging__Enabled` | **P1** | Keep the safe appsettings default `false`; enable only for controlled debugging. |
| Diagnostics external ping | `Diagnostics__EnableExternalPing` | **P2** | Default `false`; enable only for controlled provider diagnostics. |
| External ping timeout | `Diagnostics__ExternalPingTimeoutSeconds` | **P2** | Use appsettings default `5` unless provider latency requires tuning. |

## Operational Endpoints

Use these for deployment checks:

```text
GET /health
GET /health/ready
GET /management/diagnostics/health
GET /info
```

For the PostgreSQL migration, MinIO, endpoint seed, and artifact-to-active-deployment verification workflow, use [Robot Artifact Operational Smoke Test](ROBOT_ARTIFACT_OPERATIONAL_SMOKE.md).

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
- Robot artifact uploads store Lua files in S3-compatible object storage. Use MinIO for local/dev and S3-compatible cloud object storage in production. PostgreSQL stores metadata only.
- Set `ErrorHandling__ExposeStackTrace=false` and `Observability__DebugBodyLogging__Enabled=false` in deployed environments.
- For production observability, set `Observability__OpenTelemetry__OtlpExporterEnabled=true` for traces/metrics and `Observability__Serilog__OtlpSinkEnabled=true` for structured logs, then configure the OTLP endpoint to point to your collector.
- Set `Diagnostics__ApiKey` outside Development before using `/management/diagnostics/health`.
- Keep `Diagnostics__EnableExternalPing=false` unless the deployment check intentionally needs live SMTP/Firebase/PayOS reachability.
- IoT runtime endpoints require HTTPS. Full Edge client certificates are accepted at the TLS handshake and authenticated by the provisioned SHA-256 fingerprint in WebAPI; do not terminate mTLS at an untrusted proxy.
- After applying the execution transport-security migration, rotate any pre-existing low-cost credential binding that has no ECDSA public key and any Full Edge binding whose reference is not the normalized certificate SHA-256 fingerprint.
