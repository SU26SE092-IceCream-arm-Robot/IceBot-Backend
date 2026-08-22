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
| Public order token key ring | `PublicOrderAccess__KeyRingDirectory` | **P0 Core** | **Required in Production.** Persistent shared filesystem path for Data Protection keys; mount the same protected directory into every API instance. Tokens survive restarts but remain invalid after the 24-hour lifetime. |
| Browser frontend origins | `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, ... | **P0 Core** | **Env required for browser deployments.** Production does not use the Development allow-any fallback. |
| Require kiosk connectivity before sales | `KioskSalesAdmission__RequireConnectivity` | **P0 Safety** | Default `true`. The current production demo sets `false` temporarily so a kiosk can show the runtime menu and begin checkout before Edge heartbeat integration is verified. Restore `true` before unattended operation; this flag does not bypass dispatch/readiness checks. |
| Trusted reverse-proxy networks | `ReverseProxy__TrustedNetworks__0`, `ReverseProxy__TrustedNetworks__1`, ... | **P0 Core behind TLS-terminating proxy** | **Required in Production.** CIDRs whose `X-Forwarded-For` and `X-Forwarded-Proto` headers WebAPI may trust. The current K3s default is `10.42.0.0/16`; override it when the observed Traefik-to-WebAPI source network differs. Do not clear trust lists to accept arbitrary client headers. |
| Dedicated Edge mTLS listener | `ExecutionEndpointTransport__MutualTlsListener__Required` | **P0 Feature** | Set `true` only after configuring the named Kestrel HTTPS endpoint and its server certificate. Startup then rejects a missing/non-HTTPS endpoint or certificate. This listener must receive the Edge TLS handshake directly, or through TCP TLS passthrough; an HTTP reverse proxy that terminates TLS cannot forward the client certificate to WebAPI. |
| Dedicated Edge mTLS endpoint name | `ExecutionEndpointTransport__MutualTlsListener__EndpointName` | **P0 Feature** | Defaults to `EdgeMtls`. Configure `Kestrel__Endpoints__EdgeMtls__Url=https://+:8443` and either endpoint/default Kestrel certificate settings through the deployment secret store. Do not put PFX passwords or certificate files in source control. |
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
| Firebase push operation timeout | `Firebase__PushDelivery__OperationTimeoutSeconds` | **P1** | Default `30` seconds. Bounds one FCM send attempt. Do not add sender-level retry: durable `NotificationDelivery` owns retry and delivery identity. Keep this lower than `NotificationDelivery__ProcessingTimeoutSeconds`. |

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
| Enable payment-session reconciliation | `Payments__SessionReconciliation__Enabled` | **P1** | Keep the appsettings default `true` when PayOS checkout is enabled. The worker repairs responses lost after provider session creation without repeating the create `POST`. |
| Payment-session reconciliation timing | `Payments__SessionReconciliation__IntervalSeconds`, `__StaleAfterSeconds`, `__RetryDelaySeconds` | **P2** | Start with the appsettings defaults. Increase the interval or delay only when provider rate limits require it. Settings are startup-validated. |
| Payment-session reconciliation batch | `Payments__SessionReconciliation__BatchSize` | **P2** | Maximum pending transactions queried per scan; appsettings default is `50`. |
| Order payment window | `Payments__OrderPaymentWindow__DurationMinutes` | **P1** | Server-authoritative time allowed to start payment after Order placement, including across Store closing or a manual sales pause. Default `15`; startup validation allows `1-120`. PayOS session expiry is capped by this deadline. |

## Runtime Menu Redis Cache

The runtime-menu cache is optional. It accelerates the anonymous kiosk menu projection but does not replace live kiosk/store admission or transactional checkout validation. Keep it disabled until the environment has an isolated Redis service.

| Area | Configuration key | Priority | Deploy action |
| --- | --- | --- | --- |
| Enable runtime-menu cache | `RuntimeMenuCache__Enabled` | **P1** | Set `true` only after Redis connectivity is verified. `false` uses the database projection without changing the public API. |
| Redis connection string | `RuntimeMenuCache__RedisConnectionString` | **P0 Feature** | **Secret/env required** when enabled. Use an environment-specific Redis endpoint, TLS policy, and credentials. |
| Redis namespace | `RuntimeMenuCache__InstanceName` | **P1** | Required when enabled. Use a distinct prefix per deployment, for example `icebot:production:`. Do not share a namespace across environments. |
| Distributed projection TTL | `RuntimeMenuCache__DistributedExpirationSeconds` | **P2** | Default `10`; allowed `1-60`. Bounds stale catalog projection data in Redis. |
| Process-local projection TTL | `RuntimeMenuCache__LocalExpirationSeconds` | **P2** | Default `1`; allowed `1` through the distributed TTL. This short L1 cache reduces stampedes but is not actively invalidated across API replicas. |
| Uncached response TTL | `RuntimeMenuCache__UncachedSnapshotExpirationSeconds` | **P2** | Default `15`; applies when cache is disabled or Redis is unavailable. |

Redis loss must not make kiosk ordering unavailable: the backend logs the cache failure, records a metric, and resolves the projection from PostgreSQL. Do not set a long TTL to compensate for slow catalog queries; profile and fix the query path instead.

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
| Authoring import staging retention | `RobotArtifacts__ObjectStorage__AuthoringImportRetentionHours` | **P2** | Use appsettings default `168` hours. The window applies to Applied import staging; Uploaded, Validated, and Failed imports retain staging while retry actions remain available. Discarded imports are eligible for cleanup after the orphan grace period. Import metadata/provenance remains in PostgreSQL after staging ZIP removal. |

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
| Release publish inventory policy | `ProductionInventoryReadiness__PublishPolicy` | **P1** | `Warn` by default. Use `Block` only when every applicable kiosk must be provisioned before release publication. |
| Release deploy inventory policy | `ProductionInventoryReadiness__DeployPolicy` | **P1** | `Block` by default. `Warn` permits deployment while returning detailed readiness warnings. |
| Upgrade reconciliation enabled | `ProductionPackageUpgrade__Reconciliation__Enabled` | **P1** | Keep enabled so crashed or abandoned materialization work cannot lock a source installation indefinitely. |
| Upgrade materialization timeout | `ProductionPackageUpgrade__Reconciliation__MaterializingTimeoutMinutes` | **P1** | Maximum interval without persisted progress before a Materializing upgrade is marked Failed; default is `15`. |
| Upgrade reconciliation schedule | `ProductionPackageUpgrade__Reconciliation__IntervalSeconds`, `__BatchSize` | **P2** | Defaults are `60` seconds and `100` candidates. Settings are startup-validated. |
| Enable order execution dispatch | `OrderExecutionDispatch__Enabled` | **P1** | Keep enabled when paid machine-produced orders must be dispatched to Edge. |
| Execute-order command expiry | `OrderExecutionDispatch__CommandExpiryMinutes` | **P1** | Review against kiosk queue and customer-wait policy; default is `30`. |
| Active commands per endpoint | `OrderExecutionDispatch__MaxActiveCommandsPerEndpoint` | **P1** | Technical backstop for command delivery; keep `1` for the current one-customer-per-kiosk workflow. It is not a customer-queue capacity setting. |
| Dispatch reconciliation interval | `OrderExecutionDispatch__ReconciliationIntervalSeconds` | **P2** | Use appsettings default `10` unless database load requires tuning. |
| Dispatch reconciliation batch size | `OrderExecutionDispatch__ReconciliationBatchSize` | **P2** | Use appsettings default `50` unless recovery volume requires tuning. |
| Initial dispatch support escalation | `OrderExecutionDispatch__InitialDispatchSupportEscalationMinutes` | **P1** | Paid machine orders with no initial command become `FulfillmentIssue` after this duration; default is `15` minutes. |
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
| Execution readiness timeout | `EdgeTelemetryIngestion__ReadinessTimeoutSeconds` | **P1** | Maximum Cloud receive-time age for a Ready/Safe projection used by menu, checkout, deployment preview, and workspace; default is `120` seconds. |
| Alert automation event age | `EdgeTelemetryIngestion__AlertAutomationMaxEventAgeMinutes` | **P1** | Maximum age at Cloud receive time for replayed Error/Critical device evidence to create/correlate an Alert or push; default is `60` minutes. Older events remain audit history. |
| Connectivity reconciliation interval | `EdgeTelemetryIngestion__ConnectivityReconciliationIntervalSeconds` | **P1** | Background scan interval for heartbeat timeout transitions; default is `15` seconds. |
| Connectivity reconciliation batch size | `EdgeTelemetryIngestion__ConnectivityReconciliationBatchSize` | **P2** | Maximum Active kiosk candidates checked per scan; appsettings default is `100`. |
| Edge batch event count | `EdgeTelemetryIngestion__MaxBatchEventCount` | **P1** | Maximum items accepted by one telemetry replay or production-history replay request; default is `100`. Request body size remains bounded separately by execution-endpoint security. |
| Enable retention purge | `DataRetention__Enabled` | **P1** | Default `true`; disable only for controlled maintenance/debugging. |
| Retention schedule | `DataRetention__IntervalHours` | **P2** | Default `24` hours. The job runs once at startup, then on this interval. |
| Raw telemetry retention | `DataRetention__HeartbeatDays`, `DataRetention__DeviceEventDays`, `DataRetention__OperationLogDays` | **P1** | Defaults: heartbeat `30`, device events `90`, operation logs `90` days. Ticket-referenced device events are protected. |
| Processed inbox retention | `DataRetention__ProcessedSyncInboxDays` | **P1** | Default `180` days. Applies only to Processed/Ignored rows without a dead-letter reference. |
| Expired identity credential retention | `DataRetention__ExpiredIdentityCredentialDays` | **P1** | Default `30` days after expiry for refresh tokens, password-reset requests, and account invitations. Active credentials are never purged. |
| Notification delivery retention | `DataRetention__NotificationDeliveryDays` | **P1** | Default `90` days. Ordinary Delivered/PermanentFailure outbox rows are purged; pending/retryable rows and durable idempotency evidence for `deployment_failed`, `fulfillment_overdue`, and `payment_intervention` are retained. |
| Retention work limits | `DataRetention__BatchSize`, `DataRetention__MaxBatchesPerRun` | **P1** | Defaults `1000` rows per SQL delete and `20` batches per entity per run. Tune against database load. A failed retention category is logged and retried on the next scheduled run; other categories continue. |
| Inventory alert automation | `InventoryAlertAutomation__Enabled`, `__IntervalSeconds`, `__BatchSize`, `__MaxBatchesPerRun` | **P1** | Enabled by default. Reconciles Low/Empty kiosk ingredient balances with actionable alerts and refill tasks using bounded rotating scan windows. A failed balance candidate is logged without blocking later candidates. |
| Durable push delivery | `NotificationDelivery__Enabled`, `__IntervalSeconds`, `__BatchSize` | **P1** | Enabled by default. Disable only when Firebase delivery is intentionally unavailable; pending rows remain durable. |
| Push retry policy | `NotificationDelivery__ProcessingTimeoutSeconds`, `__BaseRetryDelaySeconds` | **P1** | Defaults `120` and `30` seconds. Processing rows older than the timeout are reclaimed; transient failures use exponential delay. |
| Fulfillment overdue reminders | `FulfillmentReminder__Enabled`, `__IntervalSeconds`, `__BatchSize` | **P1** | Enabled by default. Scans paid Manual/Packaged items with configured preparation time and enqueues one durable reminder per eligible recipient. It never changes order state. |
| Deployment failure notifications | `DeploymentFailureNotification__Enabled`, `__IntervalSeconds`, `__BatchSize` | **P1** | Enabled by default. Reconciles committed failed Full Edge/Low-cost deployments into one durable notification per eligible recipient. |
| Enable MQTT command wake-up | `EdgeCommandMqtt__Enabled` | **P1** | Default `false`; enable only when a broker and endpoint subscriptions are configured. Polling remains authoritative. |
| MQTT broker host/port | `EdgeCommandMqtt__Host`, `EdgeCommandMqtt__Port` | **P0 Feature** | Required when MQTT wake-up is enabled. Defaults are `localhost:1883` for local development only. |
| MQTT TLS | `EdgeCommandMqtt__UseTls` | **P0 Secret/Security** | Enable for production broker connections. Certificate trust uses the host OS trust store. |
| MQTT credentials | `EdgeCommandMqtt__Username`, `EdgeCommandMqtt__Password` | **P0 Secret** | Supply through deployment secrets when broker authentication is enabled; do not commit values. |
| MQTT client/topic | `EdgeCommandMqtt__ClientId`, `EdgeCommandMqtt__TopicPrefix` | **P1** | Client id must be unique per backend instance; topic prefix defaults to `icebot`. |
| MQTT publish resilience | `EdgeCommandMqtt__ConnectTimeoutSeconds`, `EdgeCommandMqtt__PublishTimeoutSeconds`, `EdgeCommandMqtt__PublishRetryCount`, `EdgeCommandMqtt__PublishRetryDelayMilliseconds` | **P2** | Defaults to 5-second connect timeout, 6-second attempt timeout, one retry, and 250 ms base delay. Keep retries low because periodic pull is authoritative. |
| Enable MQTT Edge uplink | `EdgeUplinkMqtt__Enabled` | **P1** | Default `false`; enables the shared Cloud consumer for typed Edge telemetry/readiness/execution evidence. HTTPS remains the recovery fallback. |
| MQTT uplink connection | `EdgeUplinkMqtt__Host`, `__Port`, `__UseTls`, `__Username`, `__Password`, `__ClientId` | **P0 Secret/Security** | Use a backend broker identity that can subscribe only to the shared uplink filter and publish endpoint result topics. Client id must be unique per backend replica. |
| MQTT uplink routing | `EdgeUplinkMqtt__TopicPrefix`, `__ConsumerGroup` | **P1** | Defaults to `icebot` and `icebot-cloud-uplink`. All replicas must use the same consumer group so one message is processed by one consumer. |
| MQTT uplink limits | `EdgeUplinkMqtt__ConnectTimeoutSeconds`, `__PublishTimeoutSeconds`, `__ReconnectDelaySeconds`, `__MaxPayloadBytes`, `__MaxConcurrentMessages` | **P1** | Defaults to 5 seconds, 6 seconds, 5 seconds, 256 KiB, and 16 concurrent messages. Keep payload bounded; use object storage for files. |
| MQTT credential provisioning | `MqttCredentialProvisioning__Enabled`, `__Provider`, `__Host`, `__Port`, `__UseTls` | **P0 Feature/Security** | Enables execution-endpoint subscriber provisioning through Mosquitto Dynamic Security. Disabled by default. Production requires TLS. |
| MQTT dynsec administrator | `MqttCredentialProvisioning__AdminUsername`, `__AdminPassword` | **P0 Secret** | Broker-control identity used only for client credential lifecycle. Supply from secret manager; never reuse backend publisher or endpoint credentials. |
| MQTT endpoint subscriber role | `MqttCredentialProvisioning__SubscriberRole` | **P1** | Existing configuration key retained for compatibility; the role is now bidirectional. Restrict `%u` to subscribe its `commands/available` and `uplink/results` topics and publish only allowed typed messages below its own `uplink/` prefix. |
| MQTT credential resilience | `MqttCredentialProvisioning__TimeoutSeconds`, `__RetryCount`, `__RetryDelayMilliseconds`, `__ReconciliationIntervalSeconds`, `__ReconciliationBatchSize` | **P2** | Defaults to 10-second command timeout, one transport retry, 500 ms base delay, a 60-second reconciliation scan, and 100 candidates per batch. Stale provision/rotation requires operator retry; stale revocation is retried automatically. |

Broker startup, endpoint-scoped ACL provisioning, Edge subscription behavior, and production TLS rules are defined in [MQTT Operations](MQTT_OPERATIONS.md).

## Observability And Diagnostics

| Area | Configuration key | Priority | Deploy action |
| --- | --- | --- | --- |
| Expose stack traces | `ErrorHandling__ExposeStackTrace` | **P1** | Use the safe appsettings default `false`. |
| Serilog OTLP sink | `Observability__Serilog__OtlpSinkEnabled` | **P2** | Default `false`; enable when an OTLP log collector is deployed. |
| OTel metric export | `Observability__OpenTelemetry__Metrics__ExporterEnabled` | **P2** | Default `false`; enable only when the Collector metric receiver is deployed. |
| OTel trace export | `Observability__OpenTelemetry__Tracing__ExporterEnabled` | **P2** | Default `false`; enable only when the Collector trace receiver is deployed. |
| OTel signal endpoint/protocol | `Observability__OpenTelemetry__Metrics__OtlpEndpoint`, `Observability__OpenTelemetry__Metrics__OtlpProtocol`, `Observability__OpenTelemetry__Tracing__OtlpEndpoint`, `Observability__OpenTelemetry__Tracing__OtlpProtocol` | **P0 Feature** | Configure each enabled signal explicitly and point it to a private Collector receiver. |
| OTel resource identity | `Observability__DeploymentEnvironment`, `Observability__InstanceId` | **P2** | Identify environment and replica. `InstanceId` must be deployment/process identity, never tenant data. |
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
- For production observability, enable metric and trace exporters independently, point them to the private Collector receiver, and optionally enable the Serilog OTLP sink. See [Prometheus And Grafana Handoff](PROMETHEUS_GRAFANA_HANDOFF.md); the Collector, Prometheus, Grafana, retention, and alerts remain DevOps-owned.
- Set `Diagnostics__ApiKey` outside Development before using `/management/diagnostics/health`.
- Keep `Diagnostics__EnableExternalPing=false` unless the deployment check intentionally needs live SMTP/Firebase/PayOS reachability.
- IoT runtime endpoints require HTTPS. Full Edge client certificates are accepted at the TLS handshake and authenticated by the provisioned SHA-256 fingerprint in WebAPI; do not terminate mTLS at an untrusted proxy.
- After applying the execution transport-security migration, rotate any pre-existing low-cost credential binding that has no ECDSA public key and any Full Edge binding whose reference is not the normalized certificate SHA-256 fingerprint.

## Related Docs

- [Observability](OBSERVABILITY.md)
- [MQTT Operations](MQTT_OPERATIONS.md)
- [Robot Artifact Operational Smoke Test](ROBOT_ARTIFACT_OPERATIONAL_SMOKE.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
