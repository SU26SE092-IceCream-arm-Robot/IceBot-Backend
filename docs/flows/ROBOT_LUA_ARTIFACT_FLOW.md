# Robot Lua Artifact Flow

This document owns the current backend flow for exporting Fairino `.lua` files, registering immutable robot artifacts, composing an ordered robot program, publishing configuration, and delivering files to an execution endpoint.

## Search Keywords

`Fairino Studio`, `.lua`, `Lua export`, `RobotArtifact`, `RobotProgram`, `RobotProgramArtifact`, `RunOrder`, `artifact.read`, `artifact.upload`, `program.read`, `program.manage`, `release.read`, `release.publish`, `deployment.read`, `release.deploy`, `configuration deployment`, `presigned download URL`, `artifact checksum`, `Full Edge`, `Low-cost Controller`

## Ownership Boundary

```text
Fairino-Studio
  owns project editing and .lua generation

Cloud backend
  owns global authoring templates, immutable organization artifact metadata, object storage references,
  ordered RobotProgram manifests, configuration releases, and deployment commands

Object storage
  owns .lua file bytes

Execution endpoint
  downloads, verifies, installs, activates, and executes deployed artifacts
```

The `.fairobot` project file remains a design-time Fairino-Studio file. It is not a `RobotArtifact` and is not uploaded through the runtime artifact API.

`RobotArtifactTemplate` is also design-time input. It is globally managed and cannot be added to a program, release, or deployment. An organization must clone a Published template into its own Draft `RobotArtifact`, review it, and publish it before production use. The clone records `SourceRobotArtifactTemplateId`, but owns separate metadata and object-storage bytes so later template retirement cannot break organization runtime history.

## End-To-End Flow

```text
Fairino-Studio project
  -> export one or more .lua files
  -> upload each file as RobotArtifact
  -> publish each RobotArtifact
  -> create RobotProgram draft
  -> replace ordered RobotProgramArtifact membership
  -> review RobotProgram
  -> publish RobotProgram manifest
  -> author and publish ConfigurationRelease
  -> create and configure a kiosk execution endpoint
  -> provision its credential and profile identity
  -> request deployment for the kiosk execution endpoint
  -> endpoint pulls DeployConfiguration command
  -> endpoint downloads each .lua file through a short-lived URL
  -> endpoint verifies byte length and SHA-256 checksum
  -> endpoint installs and activates configuration
  -> endpoint reports deployment result to Cloud
```

## Step And API Lookup

| Step | Actor | API / operation | Effect |
| --- | --- | --- | --- |
| 1. Edit project | Fairino-Studio user | No backend API | Saves Blockly/editor state in a local `.fairobot` project. |
| 2. Export Lua | Fairino-Studio user | No backend API | Produces multiple `.lua` files. A normal editor step becomes one file; a paired loop becomes one file. |
| 2T. Manage global templates | SystemAdmin | `POST /api/v1/management/robot-artifact-templates/bulk`, then `PATCH /api/v1/management/robot-artifact-templates/{templateId}/publish` | Uploads reusable Lua templates as Draft and publishes reviewed templates. Templates may be listed, inspected, reviewed through a short-lived URL, and retired, but never execute directly. An incorrect unreferenced Draft may be discarded with `DELETE /api/v1/management/robot-artifact-templates/{templateId}`. |
| 3. Find existing artifacts | Management UI | `GET /api/v1/management/organizations/{organizationId}/robot-artifacts` | Returns a tenant-scoped, paged artifact list with optional `search` and `status`. |
| 3T. Find global templates | Management UI | `GET /api/v1/management/robot-artifact-templates` | Returns reusable global templates. `SystemAdmin` manages them; `OrgAdmin` may inspect them. |
| 3C. Clone template | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-artifacts/from-template` | Copies one Published template into a separate organization-owned Draft artifact with compatibility metadata inherited and lineage recorded. It does not publish or assign program membership. |
| 4. Upload files | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-artifacts/bulk` | Uploads up to 50 files with metadata. Each successful item creates one unassigned Draft `RobotArtifact`; no program sequence changes. Policy: `artifact.upload`. |
| 5. Inspect artifact | Management UI | `GET /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}` | Returns artifact metadata only when both artifact and organization match the caller's scope. It does not return a download URL. |
| 5A. Review Lua bytes | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/review-url` | Returns an `artifact.upload`-authorized short-lived presigned URL plus checksum/size metadata. The URL is ephemeral and must not be persisted as artifact identity. |
| 5B. Discard staging artifact | Management UI | `DELETE /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}` | Hard-deletes only an unreferenced Draft artifact. Metadata commits first; object deletion is best-effort and orphan cleanup handles any remaining object. |
| 6. Publish reviewed artifacts | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/robot-artifacts/publish-bulk` | Atomically publishes 1-100 unique selected Draft artifacts after staging review. Already Published selections are idempotent success; other states reject the request. Policy: `artifact.upload`. |
| 6A. Publish one artifact | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/publish` | Single-artifact alternative with explicit organization ownership. |
| 6B. Retire artifact | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/retire` | Stops new authoring use without deleting Lua bytes or breaking published manifests/rollback. Draft program references must be removed first. |
| 7. Create program | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-programs` | Creates an organization-owned Draft `RobotProgram`, optionally narrowed to Store, Kiosk, or Device scope. Policy: `program.manage`. |
| 8. Set execution order | Management UI | `PUT /api/v1/management/organizations/{organizationId}/robot-programs/{programId}/artifacts` | Atomically replaces the complete `RobotProgramArtifact` membership and explicit `RunOrder` values while the program is Draft. |
| 9. Edit program metadata | Management UI | `PUT /api/v1/management/organizations/{organizationId}/robot-programs/{programId}` | Updates Draft code, name, and description. Scope remains immutable. |
| 10. Review programs | Management UI | `GET /api/v1/management/organizations/{organizationId}/robot-programs` and `GET /api/v1/management/organizations/{organizationId}/robot-programs/{programId}` | Returns tenant-scoped program data and ordered artifact metadata for review/reordering. Policy: `program.read`. |
| 11. Publish program | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/robot-programs/{programId}/publish` | Validates published artifacts and creates immutable `ProgramManifestJson` plus `ProgramManifestChecksum`. |
| 11A. Retire program | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/robot-programs/{programId}/retire` | Stops new release authoring while preserving published release and rollback history. Draft release references must be removed first. |
| 11D. Discard program draft | Management UI | `DELETE /api/v1/management/organizations/{organizationId}/robot-programs/{programId}` | Hard-deletes only a Draft program and its ordered membership. Published or release-referenced programs are preserved. |
| 11B. Load release options | Management UI | `GET /api/v1/management/organizations/{organizationId}/configuration-release-authoring-options` | Returns eligible machine-produced ProductVariant, Published/Active Recipe, and Published RobotProgram options for release authoring. Optional `productVariantId`, `search`, and per-group `limit` reduce selector payloads. |
| 12. Create release draft | Management UI | `POST /api/v1/management/organizations/{organizationId}/configuration-releases` | Creates a Draft release and allocates the next organization release number. Policy: `release.publish`. |
| 12A. Author routes | Management UI | `PUT /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}/routes` | Atomically replaces Draft execution routes and ordered robot-program bindings after validating product, recipe, organization, and published-program references. |
| 12B. Review releases | Management UI | `GET /api/v1/management/organizations/{organizationId}/configuration-releases`, `GET /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}`, and the organization authoring-options lookup | Returns tenant-scoped release summaries/details and authoring lookup data. Policy: `release.read`. |
| 13. Publish release | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}/publish` | Validates execution routes/program bindings and creates immutable release manifest/checksum. Policy: `release.publish`. |
| 13R. Retire release | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}/retire` | Stops normal new deployments. Active history and validated rollback remain available; Pending/Installed deployments must finish first. |
| 13X. Discard release draft | Management UI | `DELETE /api/v1/management/organizations/{organizationId}/configuration-releases/{releaseId}` | Hard-deletes only a Draft release and its route/binding children when no deployment references exist. |
| 13A. Create endpoint | Management UI | `POST /api/v1/management/kiosks/{kioskId}/execution-endpoints` | Creates a Full Edge or Low-cost endpoint in `Provisioning`; Full Edge requires mutual TLS authentication mode. Policy: `devices.manage`. |
| 13B. Set robot compatibility | Management UI | `PUT /api/v1/management/execution-endpoints/{endpointId}/supported-robot-targets` | Replaces the complete runtime-target/machine-model/device compatibility set while the endpoint is not Active or Retired. |
| 13C. Provision endpoint | Management UI / provisioning operator | `POST /api/v1/management/execution-endpoints/{endpointId}/provision` | Full Edge pins a client-certificate SHA-256 fingerprint; low-cost stores an ECDSA P-256 public key/fingerprint. The private key never enters Cloud. The operation also assigns profile identity and activates the endpoint. |
| 13D. Operate endpoint | Management UI | `PATCH .../disable`, `PATCH .../reactivate`, `PATCH .../credential`, `PATCH .../retire` | Controls endpoint lifecycle and credential rotation without changing release or artifact history. |
| 14A. Deploy Full Edge | Management UI | `POST /api/v1/management/kiosks/{kioskId}/configuration-deployments/full-edge` | Creates `KioskConfigurationDeployment` and durable `DeployConfiguration` command. Policy: `release.deploy`. |
| 14B. Deploy low-cost set | Management UI | `POST /api/v1/management/kiosks/{kioskId}/configuration-deployments/low-cost` | Creates a capacity-limited artifact-set deployment and durable command for a low-cost controller. Policy: `release.deploy`. |
| 14C. Monitor deployments | Management UI | `GET /api/v1/management/configuration-deployments` and `GET /api/v1/management/configuration-deployments/{deploymentId}` | Reads one unified, tenant-scoped history across Full Edge and Low-cost profiles with `Pending`, `Installed`, `Active`, or `Failed` state and failure provenance. Policy: `deployment.read`; idempotency keys are not exposed by these read endpoints. |
| 14D. Roll back | Management UI | `POST /api/v1/management/configuration-deployments/{deploymentId}/rollback` | Selects a previously Active deployment as the immutable rollback target and creates a new deployment plus command. Policy: `release.rollback`. |
| 15. Pull command | Execution endpoint | `POST /api/v1/iot/kiosks/{kioskId}/commands/pull` | Authenticates Full Edge by mTLS fingerprint or low-cost by signed request + nonce, returns deployment manifest, and enriches artifact descriptors with short-lived `DownloadUrl` values. |
| 16. Download files | Execution endpoint | Direct HTTPS GET to presigned object-storage URL | Downloads private `.lua` bytes without exposing a public backend download route. |
| 17. Verify files | Execution endpoint | Local operation | Verifies `ContentLengthBytes` and SHA-256 `ArtifactChecksum` before install or activation. |
| 18. Acknowledge command | Execution endpoint | `POST /api/v1/iot/kiosks/{kioskId}/commands/{commandId}/ack` | Reports transport/dispatch state: `Received`, `Accepted`, `Rejected`, or `DeliveryFailed`. It does not report installation completion. |
| 19. Report deployment | Execution endpoint | `POST /api/v1/iot/kiosks/{kioskId}/execution-reports` | Reports `Installed`, then `Active`, or reports `Failed`; updates deployment state and the endpoint's observed active configuration snapshot. Direct `Pending -> Active` is invalid. |

## Fairino Export Mapping

Fairino-Studio currently exports files using human-readable ordered names such as:

```text
01_MoveJ_PrepareTray.lua
02_SetDO_OpenValve.lua
03_Loop_DispenseCycle.lua
```

The mapping is:

```text
one exported .lua file
  -> one RobotArtifact

one reusable ordered execution definition
  -> one RobotProgram

one position inside that program
  -> one RobotProgramArtifact with RunOrder
```

Filename prefixes are not execution authority. The management client must send explicit positive, unique `RunOrder` values. Cloud serializes that order into the program manifest, and Edge executes the manifest order.

## Artifact Upload Contract

Global template upload uses the same multipart file/manifest shape and file limits, with `templateCode` and `templateName` replacing organization artifact code/name. Template object keys live under `robot-artifact-templates/`; organization clones receive separate keys under `robot-artifacts/{organizationId}/`.

Bulk upload is the only public upload API and accepts one to 50 files through `multipart/form-data`.

Each manifest item contains:

| Field | Meaning |
| --- | --- |
| `fileName` | Basename matching exactly one uploaded non-empty `.lua` file. |
| `artifactCode` | Stable management code within the organization. |
| `artifactName` | Human-readable name. |
| `runtimeTargetCode` | Runtime compatibility gate, for example a Fairino Lua runtime target. |
| `machineModelCode` | Robot/machine model compatibility gate. |
| `exportedAt` | Optional design-time export timestamp. |
| `description` | Optional management description. |
| `metadataJson` | Optional valid JSON metadata; not a compatibility authority. |

Cloud computes the checksum from uploaded bytes. Clients do not provide or choose the authoritative checksum or storage key.

The multipart request contains:

- `files`: repeated file part, one per exported `.lua` file.
- `manifestJson`: JSON array that matches each file by case-insensitive basename.

Example `manifestJson`:

```json
[
  {
    "fileName": "01_MoveJ_PrepareTray.lua",
    "artifactCode": "PREPARE_TRAY",
    "artifactName": "Prepare tray",
    "runtimeTargetCode": "FAIRINO_LUA_V1",
    "machineModelCode": "FR5",
    "exportedAt": "2026-06-27T10:00:00Z",
    "description": null,
    "metadataJson": null
  },
  {
    "fileName": "02_SetDO_OpenValve.lua",
    "artifactCode": "OPEN_VALVE",
    "artifactName": "Open valve",
    "runtimeTargetCode": "FAIRINO_LUA_V1",
    "machineModelCode": "FR5"
  }
]
```

Request-level rules are validated before any item is written:

- 1 to 50 files, each no larger than 10 MiB.
- File count equals manifest item count.
- Every uploaded basename has exactly one manifest item.
- File names are unique within the request.
- Every file uses the `.lua` extension and required metadata is present.

Execution uses item-level atomicity:

- Successful items remain committed even if another item fails.
- Failed items do not roll back successful uploads.
- All newly created items return HTTP `201`.
- A fully successful request containing one or more existing matches returns HTTP `200`.
- Mixed success/failure returns HTTP `207` with an item result for every file.
- All failure returns HTTP `400` with the complete item result collection.
- Each successful item returns `RobotArtifactId`, artifact metadata, and `wasExisting`. Summary fields distinguish `uploadedCount` from `existingCount`. The client chooses membership and `RunOrder` later through the program-membership `PUT`.

If the HTTP request is interrupted after some items commit, the client may safely retry the same files and metadata identity. Matching organization + normalized artifact code + SHA-256 returns the existing artifact as success instead of creating duplicate metadata.

### Staging Behavior

- Upload does not add an artifact to any `RobotProgram`.
- Upload does not append an artifact to the end of an existing sequence.
- Upload does not publish an artifact or program.
- Draft discard is unavailable after publication and is blocked while any robot program references the artifact.
- Review URLs are short-lived transport data. A discard may invalidate an already-issued review URL before its nominal expiry.
- Management UI should show newly uploaded files as unassigned relative to the selected program.
- The user explicitly drags/inserts an artifact into the ordered list, or chooses an explicit append action.
- Only the subsequent `PUT /management/organizations/{organizationId}/robot-programs/{programId}/artifacts` assigns membership and `RunOrder`.
- Unassigned organization artifacts do not block program publication because they are outside that program aggregate.

### Bulk Publish

Request:

```json
{
  "robotArtifactIds": [
    "11111111-1111-1111-1111-111111111111",
    "22222222-2222-2222-2222-222222222222"
  ]
}
```

Rules:

- All selected artifacts must exist and belong to the selected organization.
- Draft artifacts become Published in one database transaction.
- Already Published artifacts are returned as successful no-op items, making request retry safe.
- Any missing, cross-organization, or Retired artifact rejects the whole request before statuses change.
- Bulk publish does not assign program membership or `RunOrder`.

## Program Ordering Contract

Example membership replacement:

```json
{
  "artifacts": [
    {
      "robotArtifactId": "11111111-1111-1111-1111-111111111111",
      "runOrder": 1,
      "parametersSchemaVersion": 1,
      "parametersJson": null
    },
    {
      "robotArtifactId": "22222222-2222-2222-2222-222222222222",
      "runOrder": 2,
      "parametersSchemaVersion": 1,
      "parametersJson": null
    }
  ]
}
```

Rules:

- Replacement is allowed only while `RobotProgram.Status = Draft`.
- The collection must not be empty.
- `RunOrder` starts at a positive value and must be unique. Contiguous numbering is recommended but not required by V1.
- Reusing the same `RobotArtifact` at different run orders is allowed.
- Every artifact must belong to the program organization.
- Publishing requires every referenced artifact to be Published.
- `RobotProgramArtifact` is aggregate membership, not an independent CRUD resource.
- `RobotProgram` and `RobotArtifact` are separate aggregate roots. Program publication reads artifact state to create an immutable program manifest; Artifact does not own program membership.
- `ConfigurationRelease` owns routes and bindings, but not deployments. Release publication consumes immutable published-program snapshots instead of treating RobotProgram/RobotArtifact as children of the release aggregate.

## Download And Activation Contract

- The object-storage bucket is private.
- Durable command payloads store `StorageKey`, checksum, compatibility metadata, and size; they do not store presigned URLs.
- An authenticated command pull generates fresh `DownloadUrl` and `DownloadUrlExpiresAt` fields.
- `RobotArtifacts:ObjectStorage:DownloadEndpoint` must be reachable from the Edge/controller network. A Docker-only hostname is not sufficient for a remote endpoint.
- URL expiry, download failure, length mismatch, checksum mismatch, or compatibility mismatch must fail deployment.
- Edge must not activate a partial or unverified artifact set.
- Pulling an unacknowledged command again may issue fresh URLs; it does not create new artifact identity.

## Full Edge And Low-Cost Differences

| Concern | Full Edge | Low-cost Controller |
| --- | --- | --- |
| Deployment unit | Complete immutable `ConfigurationRelease` | Explicit capacity-limited active artifact set derived from a release |
| Local storage | Full artifact/configuration cache according to Edge profile | Small active artifact set that survives reboot |
| Manifest | Full release manifest | Selected route/program/artifact items |
| Download verification | Required | Required |
| Activation report | `KioskConfigurationDeployment` | `ControllerArtifactSetDeployment` |

Both profiles use the same immutable `RobotArtifact` bytes and checksum identity.

## Failure And Retry Rules

- Re-uploading identical normalized artifact code plus checksum in one organization returns the existing artifact as idempotent success.
- Concurrent uploads of that same identity are resolved by the database unique constraint. The losing request returns the winner's metadata and removes its own redundant object rather than surfacing a conflict.
- Artifact, program, and release retire commands are idempotent. Retirement preserves immutable bytes, manifests, deployment history, and rollback provenance.
- Full Edge and Low-cost deployment creation is serialized per execution scope. Backend configuration, not the management request, owns the Low-cost artifact-count and storage-byte ceilings.
- Deployment and rollback requests require `Idempotency-Key`. Retry with the same endpoint, key, and payload returns the previously created deployment; the key cannot be reused for a different deployment payload.
- Explicit `CommandExpiryAt` is part of the idempotent request fingerprint. Reusing a key with a different explicit expiry returns `409`; omitting expiry remains distinguishable from supplying one.
- Execution-report ingestion is serialized by `SourceEventId`; concurrent delivery of the same report returns the existing duplicate result instead of racing the inbox unique constraint.
- Published parent history may retain retired children. Retirement is blocked only by mutable Draft parent references, or by Pending/Installed deployments for a release.
- Artifact name, runtime target, machine model, and description do not redefine an existing identity on retry; backend returns the stored metadata. Use a different artifact code when the same bytes intentionally represent a distinct artifact identity.
- Object bytes are written before metadata is committed. A domain failure before DB write triggers immediate best-effort deletion.
- Database exceptions are treated as commit-ambiguous: upload does not immediately delete the object because metadata may already have committed.
- `RobotArtifactOrphanCleanupJob` periodically compares private object-storage keys with committed `RobotArtifact.StorageKey` values. It deletes only unreferenced objects older than the configured grace period and stops at a configured per-run delete limit.
- Orphan cleanup holds a PostgreSQL advisory lock for the scan/delete window, so multiple backend instances do not clean the same bucket concurrently.
- Objects without a reliable last-modified timestamp are skipped rather than deleted.
- Release-number allocation is serialized per organization with a transaction-scoped PostgreSQL advisory lock; other organizations remain independent.
- Command pull may return a delivered but unacknowledged deployment again.
- Command acknowledgement is transport state only; deployment completion belongs to execution reports.
- Each deployment command has a typed `DeploymentId` and `DeploymentKind`; reconciliation never parses `PayloadJson` to discover ownership.
- `DeploymentTimeoutReconciliationJob` rejects expired `PendingDelivery`/`Delivered` deployment commands with `CommandExpired` and changes the linked Pending deployment to Failed. It also completes reconciliation when pull/ack already rejected the command as expired.
- Command expiry applies only before executor acceptance. After acceptance, a separate configurable report timeout starts from `EdgeCommand.RespondedAt`.
- If the linked deployment is still Pending after `AcceptedReportTimeoutMinutes`, conditional reconciliation marks it `Failed/ExecutionReportTimeout`.
- If a deployment remains Installed beyond `InstalledActivationTimeoutMinutes`, conditional reconciliation marks it `Failed/ActivationReportTimeout`. A late Active report cannot revive that failed attempt.
- A report arriving after `ExecutionReportTimeout` cannot revive that failed deployment attempt. Operators must inspect endpoint state and request a new deployment or rollback, preserving attempt history.
- A failed deployment keeps the previously active configuration/artifact set. It must not delete a known-good active set merely because a new deployment failed.
- Rollback never mutates the selected deployment, release, artifact set, program manifest, or artifact bytes. It creates a new Pending deployment and a new `DeployConfiguration` command.
- A Retired release may be redeployed only through rollback to a deployment that was previously Active; normal deployment still requires Published status.
- Rollback is rejected when the target is not Active, is already the endpoint's observed active deployment, no longer matches the endpoint profile, or another Pending/Installed deployment blocks the endpoint.

## Current Missing Surface

The following piece remains after release authoring, deployment reads, rollback, and deployment timeout reconciliation:

1. Runtime integration test using real MinIO plus an Edge/controller client.

These are missing API/integration surfaces, not responsibilities of Fairino-Studio.

## Related Docs

- [System Flows](SYSTEM_FLOWS.md)
- [Back-Office Setup Flow](BACK_OFFICE_SETUP_FLOW.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Authorization Rules](../api/AUTHORIZATION_RULES.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
- [Deployment Configuration](../operations/DEPLOYMENT_CONFIG.md)
- [Boundary Contexts](../architecture/BOUNDARY_CONTEXTS.md)
