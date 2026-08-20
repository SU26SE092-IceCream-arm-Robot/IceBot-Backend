# Robot Lua Deployment And Activation Flow

This document owns release authoring, execution endpoint provisioning, deployment, artifact download, verification, activation, rollback, and the artifact-specific deployment failure rules.

## Search Keywords

`configuration release`, `execution route`, `deployment preview`, `Full Edge deployment`, `low-cost deployment`, `artifact download`, `activation`, `rollback`, `deployment checksum`

## Step And API Lookup

| Step | Actor | API / operation | Effect |
| --- | --- | --- | --- |
| 12P. Preview deployment | Management UI | `POST /api/v1/management/kiosks/{kioskId}/configuration-deployments/preview` | Resolves active endpoint candidates, readiness/capabilities, inventory policy, immutable artifact totals, low-cost capacity, installation modes, validation acknowledgement, and a deterministic preview checksum. Full Edge always previews the complete release; only Low-cost accepts route/program selections. It creates no deployment or object-storage bundle. More than one eligible endpoint requires explicit selection. The deploy request must echo the selected candidate's checksum as `deploymentPreviewChecksum`; the command rebuilds the preview and rejects stale or blocked input before creating deployment state. |
| 12. Create release draft | Management UI | `POST /api/v1/management/organizations/{organizationId}/configuration-releases` | Creates a Draft release and allocates the next organization release number. Policy: `release.publish`. |
| 12A. Author routes | Management UI | `PUT /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}/routes` | Atomically replaces Draft execution routes and ordered robot-program bindings after validating product, recipe, organization, and published-program references. The request uses typed `requiredCapabilities[]` and must include `expectedRevision` from release detail. A stale revision returns `409 Conflict`; reload the complete Draft before retrying. |
| 12B. Review releases | Management UI | `GET /api/v1/management/organizations/{organizationId}/configuration-releases`, `GET /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}`, and the organization authoring-options lookup | Returns tenant-scoped release summaries/details and authoring lookup data. Policy: `release.read`. |
| 13. Publish release | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}/publish` | Validates routes, bindings, and immutable program/artifact snapshots, then publishes a deployment-profile-neutral content manifest. It does not build a Full Edge ZIP or certify Lua compatibility. Policy: `release.publish`. |
| 13R. Retire release | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}/retire` | Stops normal new deployments. Active history and validated rollback remain available; Pending/Installed deployments must finish first. |
| 13X. Discard release draft | Management UI | `DELETE /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}` | Hard-deletes only a Draft release and its route/binding children when no deployment references exist. |
| 13A. Create endpoint | Management UI | `POST /api/v1/management/kiosks/{kioskId}/execution-endpoints` | Creates a Full Edge or Low-cost endpoint in `Provisioning`; Full Edge requires mutual TLS authentication mode. Policy: `execution-endpoints.manage`. |
| 13B. Report device inventory | Authenticated Edge | `PUT /api/v1/iot/execution-endpoints/{endpointId}/reported-devices` | Replaces the endpoint's declared device/runtime snapshot. It is separate from readiness, operational evidence only, and not proof that uploaded Lua behaves correctly. |
| 13C. Provision endpoint | Management UI / provisioning operator | `POST /api/v1/management/kiosks/{kioskId}/execution-endpoints/{endpointId}/provision` | Full Edge pins a client-certificate SHA-256 fingerprint; low-cost stores an ECDSA P-256 public key/fingerprint. The private key never enters Cloud. The operation also assigns profile identity and activates the endpoint. Policy: `execution-endpoints.provision`. |
| 13D. Operate endpoint | Management UI | `PATCH .../disable`, `PATCH .../reactivate`, `PATCH .../credential`, `PATCH .../retire` | Lifecycle actions use `execution-endpoints.manage`; credential rotation uses `execution-endpoints.credentials.manage`. Neither changes release or artifact history. |
| 14A. Deploy Full Edge | Management UI | `POST /api/v1/management/kiosks/{kioskId}/configuration-deployments/full-edge` | Requires an operator `reason` (3-500 characters), materializes or reuses the deterministic Full Edge ZIP from the published content manifest, then creates `KioskConfigurationDeployment` and its durable `DeployConfiguration` command. The request, actor, matching authorization scope, release/checksum, kiosk, endpoint, and initial state are written to the kiosk operation log in the same persistence transaction. Policy: `release.deploy`. |
| 14B. Deploy low-cost set | Management UI | `POST /api/v1/management/kiosks/{kioskId}/configuration-deployments/low-cost` | Requires an operator `reason` (3-500 characters), creates a capacity-limited artifact-set deployment and durable command for a low-cost controller, and writes the same request audit evidence to the kiosk operation log. Policy: `release.deploy`. |
| 14C. Monitor deployments | Management UI | `GET /api/v1/management/configuration-deployments`, `GET /api/v1/management/kiosks/{kioskId}/configuration-deployments`, and `GET /api/v1/management/kiosks/{kioskId}/configuration-deployments/{deploymentId}` | Reads one unified, tenant-scoped history across Full Edge and Low-cost profiles with `Pending`, `Installed`, `Active`, or `Failed` state and failure provenance. The global list is a read-only management index; detail reads are kiosk-owned. Policy: `deployment.read`; idempotency keys are not exposed by these read endpoints. |
| 14C1. Inspect deployed artifacts | Management UI / diagnostics | `GET /api/v1/management/kiosks/{kioskId}/configuration-deployments/{deploymentId}/artifacts` | Returns the immutable artifact/run-order materialization for that deployment. Full Edge reads published program manifests; Low-cost reads the stored controller artifact-set snapshot. |
| 14D. Roll back | Management UI | `POST /api/v1/management/kiosks/{kioskId}/configuration-deployments/{deploymentId}/rollback` | Selects a previously Active deployment for that kiosk as the immutable rollback target and creates a new deployment plus command. The body requires `reason` (3-500 characters) and `expectedActiveDeploymentId`, which must still match the endpoint's observed active deployment. A stale history view returns `409`; reload before retrying. The rollback request is audited in the kiosk operation log. Policy: `release.rollback`. |
| 15. Pull command | Execution endpoint | `POST /api/v1/iot/execution-endpoints/{endpointId}/commands/pull` | Authenticates the endpoint. Full Edge receives short-lived URLs for both the complete bundle and individual artifacts; Low-cost receives URLs only for its selected artifact set. URLs are generated at pull time and are not durable command state. |
| 16. Download files | Execution endpoint | Direct HTTPS GET to presigned object-storage URL | Full Edge uses individual files for incremental/cache-aware updates or the ZIP for cold install/full recovery. Low-cost downloads selected `.lua` files. No backend file-proxy route is used. |
| 17. Verify files | Execution endpoint | Local operation | Full Edge verifies bundle SHA-256/size, safely extracts it, then verifies every manifest artifact. Low-cost verifies each selected artifact directly. |
| 18. Acknowledge command | Execution endpoint | `POST /api/v1/iot/execution-endpoints/{endpointId}/commands/{commandId}/ack` | Reports transport/dispatch state: `Received`, `Accepted`, `Rejected`, `ExecutorBusy`, or `DeliveryFailed`. `ExecutorBusy` is temporary and permits redelivery. It does not report installation completion. |
| 19. Report deployment | Execution endpoint | `POST /api/v1/iot/execution-endpoints/{endpointId}/commands/{commandId}/reports` | Reports `Installed`, then `Active`, or reports `Failed`. Installed/Active must echo the command's release id/checksum; Low-cost must also echo active-set version/checksum. Cloud rejects mismatched command, deployment, profile, or provenance before changing observed state. Direct `Pending -> Active` is invalid. |

For Full Edge, these IoT endpoints require a dedicated HTTPS mTLS transport to Kestrel. Public browser HTTPS may terminate at a reverse proxy, but an Edge mTLS route must be direct TLS or TCP TLS passthrough so the client certificate reaches Kestrel. This transport setup is an environment prerequisite; it is not configured through the Management UI.

## Download And Activation Contract

- The object-storage bucket is private.
- Full Edge distribution is hybrid: compare local artifact checksums first, download only missing/changed files when practical, and use the bundle for cold install, cache loss, full recovery, or full rollback.
- A published release stores only its profile-neutral content manifest and checksum. The Full Edge ZIP is a deterministic derived transport object created when a Full Edge deployment is requested; Low-cost publication and deployment do not depend on that ZIP.
- Durable command payloads store bundle/artifact identity and checksums; they do not store presigned URLs.
- An authenticated command pull generates fresh `DownloadUrl` and `DownloadUrlExpiresAt` fields.
- `RobotArtifacts:ObjectStorage:DownloadEndpoint` must be reachable from the Edge/controller network. A Docker-only hostname is not sufficient for a remote endpoint.
- URL expiry, download failure, length mismatch, or checksum mismatch must fail deployment.
- Declared compatibility and behavior certification are separate. Cloud never certifies Lua behavior. When Edge has reported a device inventory, a release whose declared runtime/model pair is absent is blocked before command creation. When Edge has not reported an inventory, MVP deployment remains allowed with `RuntimeProfileUnknown`; this warning must not be presented as a successful compatibility check.
- Endpoint identity, lifecycle, persistence, and command delivery remain blocking operational routing gates regardless of runtime-profile evidence.
- Full Edge must reject unsafe archive paths, unexpected entries, decompression limits, bundle checksum mismatch, or any per-artifact checksum/size mismatch. It must not activate a partial or unverified release.
- Pulling an unacknowledged command again may issue fresh URLs; it does not create new artifact identity.
- A durable deployment payload that cannot be parsed is terminally marked `DeliveryFailed` with `InvalidDurablePayload`; it is not returned to the executor and does not block later commands in the same pull.

## Full Edge And Low-Cost Differences

| Concern | Full Edge | Low-cost Controller |
| --- | --- | --- |
| Deployment unit | Complete immutable `ConfigurationRelease` | Explicit capacity-limited active artifact set derived from a release |
| Local storage | Full artifact/configuration cache according to Edge profile | Small active artifact set that survives reboot |
| Manifest | Full release manifest | Selected route/program/artifact items |
| Transport | Hybrid: immutable ZIP plus individual artifact URLs | Individual selected `.lua` files |
| Download verification | Bundle plus every extracted artifact | Every selected artifact |
| Activation report | `KioskConfigurationDeployment` | `ControllerArtifactSetDeployment` |

Both profiles use the same immutable `RobotArtifact` bytes and checksum identity.

## Failure And Retry Rules

- Re-uploading identical normalized artifact code plus checksum in one organization returns the existing artifact as idempotent success.
- Artifact, program, and release retire commands are idempotent. Retirement preserves immutable bytes, manifests, deployment history, and rollback provenance.
- Low-cost capacity limits are backend configuration and are not request fields.
- Deployment and rollback requests require `Idempotency-Key` and an operator reason. Retry with the same endpoint, key, and payload returns the previously created deployment; the key cannot be reused for a different deployment payload. A rollback additionally carries the active deployment observed by the client and is rejected when that observation is stale.
- Repeating an execution report with the same `SourceEventId` returns the existing result without applying the transition twice.
- Published parent history may retain retired children. Retirement is blocked only by mutable Draft parent references, or by Pending/Installed deployments for a release.
- Artifact name, runtime target, machine model, and description do not redefine an existing identity on retry; backend returns the stored metadata. Use a different artifact code when the same bytes intentionally represent a distinct artifact identity.
- Command pull may return a delivered but unacknowledged deployment again.
- Command acknowledgement is transport state only; deployment completion belongs to execution reports.
- An expired unaccepted deployment command changes its Pending deployment to `Failed/CommandExpired`.
- Command expiry applies only before executor acceptance. After acceptance, a separate report timeout applies.
- A deployment still Pending after its accepted-report timeout becomes `Failed/ExecutionReportTimeout`.
- A deployment still Installed after its activation timeout becomes `Failed/ActivationReportTimeout`.
- A report arriving after `ExecutionReportTimeout` cannot revive that failed deployment attempt. Operators must inspect endpoint state and request a new deployment or rollback, preserving attempt history.
- A failed deployment keeps the previously active configuration/artifact set. It must not delete a known-good active set merely because a new deployment failed.
- Rollback never mutates the selected deployment, release, artifact set, program manifest, or artifact bytes. It creates a new Pending deployment and a new `DeployConfiguration` command.
- A Retired release may be redeployed only through rollback to a deployment that was previously Active; normal deployment still requires Published status.
- Rollback is rejected when the target is not Active, is already the endpoint's observed active deployment, no longer matches the endpoint profile, or another Pending/Installed deployment blocks the endpoint.

## Related Docs

- [Robot Lua Artifact Flow](ROBOT_LUA_ARTIFACT_FLOW.md)
- [Edge Command Contract](../iot/EDGE_COMMAND_CONTRACT.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
- [Deployment Configuration](../operations/DEPLOYMENT_CONFIG.md)
