# Robot Lua Artifact Flow

This document owns the current backend flow for exporting Fairino `.lua` files, registering immutable robot artifacts, composing an ordered robot program, publishing configuration, and delivering files to an execution endpoint.

## Search Keywords

`Fairino Studio`, `.lua`, `Lua export`, `RobotArtifact`, `RobotProgram`, `RobotProgramArtifact`, `RunOrder`, `artifact.upload`, `release.publish`, `release.deploy`, `configuration deployment`, `presigned download URL`, `artifact checksum`, `Full Edge`, `Low-cost Controller`

## Ownership Boundary

```text
Fairino-Studio
  owns project editing and .lua generation

Cloud backend
  owns immutable artifact metadata, object storage references,
  ordered RobotProgram manifests, configuration releases, and deployment commands

Object storage
  owns .lua file bytes

Execution endpoint
  downloads, verifies, installs, activates, and executes deployed artifacts
```

The `.fairobot` project file remains a design-time Fairino-Studio file. It is not a `RobotArtifact` and is not uploaded through the runtime artifact API.

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
| 3. Find existing artifacts | Management UI | `GET /api/v1/management/organizations/{organizationId}/robot-artifacts` | Returns a tenant-scoped, paged artifact list with optional `search` and `status`. |
| 4. Upload files | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-artifacts/bulk` | Uploads up to 50 files with metadata. Each successful item creates one unassigned Draft `RobotArtifact`; no program sequence changes. Policy: `artifact.upload`. |
| 5. Inspect artifact | Management UI | `GET /api/v1/management/robot-artifacts/{artifactId}` | Returns artifact metadata, storage key, filename, checksum, compatibility codes, size, and status. It does not return a download URL. |
| 6. Publish artifact | Management UI | `PATCH /api/v1/management/robot-artifacts/{artifactId}/publish` | Changes Draft artifact to Published so it may be included in a published program manifest. Policy: `artifact.upload`. |
| 7. Create program | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-programs` | Creates an organization-owned Draft `RobotProgram`, optionally narrowed to Store, Kiosk, or Device scope. Policy: `release.publish`. |
| 8. Set execution order | Management UI | `PUT /api/v1/management/robot-programs/{programId}/artifacts` | Atomically replaces the complete `RobotProgramArtifact` membership and explicit `RunOrder` values while the program is Draft. |
| 9. Edit program metadata | Management UI | `PUT /api/v1/management/robot-programs/{programId}` | Updates Draft code, name, and description. Scope remains immutable. |
| 10. Review programs | Management UI | `GET /api/v1/management/robot-programs` and `GET /api/v1/management/robot-programs/{programId}` | Returns tenant-scoped program data and ordered artifact metadata for review/reordering. |
| 11. Publish program | Management UI | `PATCH /api/v1/management/robot-programs/{programId}/publish` | Validates published artifacts and creates immutable `ProgramManifestJson` plus `ProgramManifestChecksum`. |
| 12. Create release draft | Management UI | `POST /api/v1/management/organizations/{organizationId}/configuration-releases` | Creates a Draft release and allocates the next organization release number. Policy: `release.publish`. |
| 12A. Author routes | Management UI | `PUT /api/v1/management/configuration-releases/{releaseId}/routes` | Atomically replaces Draft execution routes and ordered robot-program bindings after validating product, recipe, organization, and published-program references. |
| 12B. Review releases | Management UI | `GET /api/v1/management/configuration-releases` and `GET /api/v1/management/configuration-releases/{releaseId}` | Returns tenant-scoped release summaries/details with route and binding metadata. |
| 13. Publish release | Management UI | `PATCH /api/v1/management/configuration-releases/{releaseId}/publish` | Validates execution routes/program bindings and creates immutable release manifest/checksum. Policy: `release.publish`. |
| 14A. Deploy Full Edge | Management UI | `POST /api/v1/management/kiosks/{kioskId}/configuration-deployments` | Creates `KioskConfigurationDeployment` and durable `DeployConfiguration` command. Policy: `release.deploy`. |
| 14B. Deploy low-cost set | Management UI | `POST /api/v1/management/kiosks/{kioskId}/controller-artifact-set-deployments` | Creates a capacity-limited artifact-set deployment and durable command for a low-cost controller. Policy: `release.deploy`. |
| 15. Pull command | Execution endpoint | `POST /api/v1/iot/kiosks/{kioskId}/commands/pull` | Authenticates endpoint credential, returns deployment manifest, and enriches artifact descriptors with short-lived `DownloadUrl` values. |
| 16. Download files | Execution endpoint | Direct HTTPS GET to presigned object-storage URL | Downloads private `.lua` bytes without exposing a public backend download route. |
| 17. Verify files | Execution endpoint | Local operation | Verifies `ContentLengthBytes` and SHA-256 `ArtifactChecksum` before install or activation. |
| 18. Acknowledge command | Execution endpoint | `POST /api/v1/iot/kiosks/{kioskId}/commands/{commandId}/ack` | Reports dispatch acceptance/rejection only. It does not report installation completion. |
| 19. Report deployment | Execution endpoint | `POST /api/v1/iot/kiosks/{kioskId}/execution-reports` | Reports `Installed`, `Active`, or `Failed`; updates deployment state and the endpoint's observed active configuration snapshot. |

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

- 1 to 50 files.
- File count equals manifest item count.
- Every uploaded basename has exactly one manifest item.
- File names are unique within the request.
- Every file uses the `.lua` extension and required metadata is present.

Execution uses item-level atomicity:

- Successful items remain committed even if another item fails.
- Failed items do not roll back successful uploads.
- All success returns HTTP `201`.
- Mixed success/failure returns HTTP `207` with an item result for every file.
- All failure returns HTTP `400` with the complete item result collection.
- Each successful item returns `RobotArtifactId` and artifact metadata. The client chooses membership and `RunOrder` later through the program-membership `PUT`.

If the HTTP request is interrupted after some items commit, the client should list artifacts and reconcile by filename/code/checksum before retrying. A blind retry may correctly return conflicts for items already stored.

### Staging Behavior

- Upload does not add an artifact to any `RobotProgram`.
- Upload does not append an artifact to the end of an existing sequence.
- Upload does not publish an artifact or program.
- Management UI should show newly uploaded files as unassigned relative to the selected program.
- The user explicitly drags/inserts an artifact into the ordered list, or chooses an explicit append action.
- Only the subsequent `PUT /management/robot-programs/{programId}/artifacts` assigns membership and `RunOrder`.
- Unassigned organization artifacts do not block program publication because they are outside that program aggregate.

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

- Re-uploading identical artifact code plus checksum in one organization returns conflict instead of creating duplicate metadata.
- Object bytes are written before metadata is committed. Automatic cleanup of an object orphaned by a later database failure is not implemented yet.
- Command pull may return a delivered but unacknowledged deployment again.
- Command acknowledgement is transport state only; deployment completion belongs to execution reports.
- A failed deployment keeps the previously active configuration/artifact set. It must not delete a known-good active set merely because a new deployment failed.
- Management deployment list/detail and rollback routes are not exposed yet.

## Current Missing Surface

The following pieces remain after release authoring:

1. Management deployment list/detail APIs.
2. Explicit rollback command APIs.
3. Runtime integration test using real MinIO plus an Edge/controller client.

These are missing API/integration surfaces, not responsibilities of Fairino-Studio.

## Related Docs

- [System Flows](SYSTEM_FLOWS.md)
- [Back-Office Setup Flow](BACK_OFFICE_SETUP_FLOW.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Authorization Rules](../api/AUTHORIZATION_RULES.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
- [Deployment Configuration](../operations/DEPLOYMENT_CONFIG.md)
- [Boundary Contexts](../architecture/BOUNDARY_CONTEXTS.md)
