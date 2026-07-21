# Robot Lua Authoring And Import Flow

This document owns Fairino export, authoring-bundle import, template/artifact lifecycle, technical contracts, and ordered `RobotProgram` authoring.

## Search Keywords

`Fairino Studio`, `.lua`, `Lua export`, `RobotArtifact`, `RobotArtifactTemplate`, `RobotArtifactTechnicalContract`, `RobotProgram`, `RobotProgramArtifact`, `RunOrder`, `artifact.upload`, `program.manage`, `authoring import`, `sidecar`

The shared source of truth, boundary, and full lifecycle index are in [Robot Lua Artifact Flow](ROBOT_LUA_ARTIFACT_FLOW.md). Release, deployment, download, and activation rules are in [Robot Lua Deployment And Activation Flow](ROBOT_LUA_DEPLOYMENT_AND_ACTIVATION_FLOW.md).

## Step And API Lookup

| Step | Actor | API / operation | Effect |
| --- | --- | --- | --- |
| 1. Edit project | Fairino-Studio user | No backend API | Saves Blockly/editor state in a local `.fairobot` project. |
| 2. Export Lua | Fairino-Studio user | No backend API | The normal `Export LUA` action produces only one `*-export.zip` containing `export-manifest.json`, ordered `.lua` files under `artifacts/`, and matching `.icebot.json` sidecars under `contracts/`. A separate advanced menu command exports individual Lua/sidecar files for debugging. A normal editor step becomes one file; a paired loop becomes one file whose sidecar merges the semantics of both loop steps and requires one shared execution phase. |
| 2A. Stage authoring bundle | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports` | Uploads one bounded ZIP with `Idempotency-Key`, validates archive structure and target consistency, and creates a durable organization-scoped import session. It does not create or publish runtime resources. |
| 2B. Validate authoring bundle | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/validate` | Rechecks the staged checksum, sidecars, explicit `RunOrder`, existing artifact revisions, technical-contract identities, and program identity. Ambiguous revisions, Retired resources, and a Published artifact bound to another contract block apply. Sidecar V1 is strictly opaque and may declare only `System`/`Motion` effects without ingredient, option, quantity, or capability semantics. Typed production semantics require V2. `System`/`Motion` effects cannot carry ingredient, option, or quantity fields. `Ingredient` requires `ingredientCode` and may carry `optionCode` when that ingredient consumption is conditional on an option. `Option` requires `optionCode` and may carry `ingredientCode` when the option consumes an ingredient. Composition accepts either typed representation for an option ingredient but still requires an exact option and ingredient match. Errors block apply; valid V1 artifacts remain warnings during composition. |
| 2C. Apply authoring bundle | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/apply` | Requires both `artifact.upload` and `program.manage`. Materializes Draft technical contracts, immutable Draft artifacts, and one ordered Draft RobotProgram in one serialized metadata transaction. It never publishes or deploys. A newly created Draft contract is retained on the import item and is assigned to its artifact only after the contract is explicitly published. |
| 2S. Preview semantic composition | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/composition-preview` | Resolves required Recipe ingredients and selected production-affecting options against imported V2 technical effects. Returns proposed artifact order, conditional option membership, capability suggestions, typed blockers/warnings, and a checksum. It does not write data. V1 opaque artifacts may remain in the proposal with warnings but cannot satisfy typed ingredient/option requirements. |
| 2SC. Confirm semantic composition | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/composition-confirm` | Rebuilds the preview and requires the exact `PreviewChecksum`. With no blockers, atomically replaces membership/order on the imported Draft RobotProgram. It never publishes resources or creates a release. |
| 2P. Confirm import publication | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/publish` | Explicitly publishes/reuses each contract, assigns it to the Draft artifact, verifies and publishes each artifact, then publishes the ordered program. The operation is resumable and stops with the exact resource error; it never creates a release or deployment. |
| 2R. Create release draft from import | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/release-draft` | After import publication, selects a Published/Active `Recipe` and supported production option codes. Backend derives the Published program, route code, capability JSON, priority, and binding order, then atomically creates one organization-owned Draft release and route. If artifact contracts declare exactly one capability, it is selected automatically; zero/multiple capabilities require explicit selection. Retry with the same selection returns the same release; a different selection returns conflict. It never publishes or deploys the release. |
| 2W. Read authoring workspace | Management UI | `GET /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/workspace` | Aggregates import progress, package-installation ownership targets, release status, compatible deployment endpoint previews, blockers, and next actions. Package ownership is informational. The workspace does not automatically require or propose a fork; a separate explicit customization workflow decides whether package-managed technical resources must be forked before mutation. |
| 2D. Read/discard import | Management UI | `GET .../robot-authoring-imports/{importId}` and `POST .../{importId}/discard` | Returns import status and lifecycle actions. Only non-Applied imports may be discarded; staged ZIP deletion is best effort. Cleanup retains staging bytes while an import remains Uploaded, Validated, or Failed so its advertised retry actions remain executable. Applied or Discarded staging bytes may be removed by retention cleanup. |
| 2T. Manage global templates | SystemAdmin | `POST /api/v1/management/robot-artifact-templates/bulk`, then `PATCH /api/v1/management/robot-artifact-templates/{templateId}/publish` | Uploads reusable Lua templates as Draft and publishes reviewed templates. Templates may be listed, inspected, reviewed through a short-lived URL, and retired, but never execute directly. An incorrect unreferenced Draft may be discarded with `DELETE /api/v1/management/robot-artifact-templates/{templateId}`. |
| 3. Find existing artifacts | Management UI | `GET /api/v1/management/organizations/{organizationId}/robot-artifacts` | Returns a tenant-scoped, paged artifact list with optional `search` and `status`. |
| 3T. Find global templates | Management UI | `GET /api/v1/management/robot-artifact-templates` | Returns reusable global templates. `SystemAdmin` manages them; `OrgAdmin` may inspect them. |
| 3C. Clone template | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-artifacts/from-template` | Copies one Published template with a still-Published, checksum-consistent technical contract into a separate organization-owned Draft artifact. Compatibility metadata and lineage are inherited. It does not publish or assign program membership. |
| 4. Upload files | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-artifacts/bulk` | Uploads up to 50 files with metadata. Each successful item creates one unassigned Draft `RobotArtifact`; no program sequence changes. Policy: `artifact.upload`. |
| 4A. Import technical sidecars | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-artifact-technical-contracts/import-sidecars` | Converts 1-50 reviewed Fairino V1 or V2 `.icebot.json` sidecars into organization-owned Draft technical contracts with item-level results. The persisted technical contract retains the supplied schema version. Re-importing the same code/version replaces that Draft only when schema version and runtime target remain unchanged; Published or Retired versions require a new version. Contracts are not published automatically. |
| 4B. Author technical contract | Management UI | `GET`, `PUT`, validation-preview, publish, retire, and Draft-discard routes under the organization technical-contract resource | Reviews and publishes the typed behavior provenance, then assigns its id to the Draft artifact. |
| 5. Inspect artifact | Management UI | `GET /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}` | Returns artifact metadata only when both artifact and organization match the caller's scope. It does not return a download URL. |
| 5A. Review Lua bytes | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/review-url` | Returns an `artifact.upload`-authorized short-lived presigned URL plus checksum/size metadata. The URL is ephemeral and must not be persisted as artifact identity. |
| 5B. Discard staging artifact | Management UI | `DELETE /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}` | Hard-deletes only an unreferenced Draft artifact. Metadata commits first; object deletion is best-effort and orphan cleanup handles any remaining object. |
| 6. Publish reviewed artifacts | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/robot-artifacts/publish-bulk` | Atomically publishes 1-100 unique selected Draft artifacts after staging review. Every Draft requires a compatible Published technical contract and object-storage size/SHA-256 verification. Already Published selections are idempotent success; other states reject the request. Policy: `artifact.upload`. |
| 6A. Publish one artifact | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/publish` | Single-artifact alternative with explicit organization ownership. |
| 6B. Retire artifact | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/retire` | Stops new authoring use without deleting Lua bytes or breaking published manifests/rollback. Draft program references must be removed first. |
| 7. Create program | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-programs` | Creates an organization-owned Draft `RobotProgram`, optionally narrowed to Store, Kiosk, or Device scope. Policy: `program.manage`. |
| 8. Set execution order | Management UI | `PUT /api/v1/management/organizations/{organizationId}/robot-programs/{programId}/artifacts` | Atomically replaces the complete `RobotProgramArtifact` membership and explicit `RunOrder` values while the program is Draft. |
| 9. Edit program metadata | Management UI | `PUT /api/v1/management/organizations/{organizationId}/robot-programs/{programId}` | Updates Draft code, name, and description. Scope remains immutable. |
| 10. Review programs | Management UI | `GET /api/v1/management/organizations/{organizationId}/robot-programs` and `GET /api/v1/management/organizations/{organizationId}/robot-programs/{programId}` | Returns tenant-scoped program data and ordered artifact metadata for review/reordering. Policy: `program.read`. |
| 11. Publish program | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/robot-programs/{programId}/publish` | Validates referenced artifacts and publishes the immutable program definition. |
| 11A. Retire program | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/robot-programs/{programId}/retire` | Stops new release authoring while preserving published release and rollback history. Draft release references must be removed first. |
| 11D. Discard program draft | Management UI | `DELETE /api/v1/management/organizations/{organizationId}/robot-programs/{programId}` | Hard-deletes only a Draft program and its ordered membership. Published or release-referenced programs are preserved. |
| 11B. Load release options | Management UI | `GET /api/v1/management/organizations/{organizationId}/configuration-releases/authoring-options` | Returns eligible machine-produced ProductVariant, Published/Active Recipe, and Published RobotProgram options for release authoring. Optional `productVariantId`, `search`, and per-group `limit` reduce selector payloads. Route submission sends `RecipeId`; backend derives and stores its `ProductVariantId`. |
| 11C. Automated release linkage | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/release-draft` | Normal custom-authoring path after import publication. The client does not submit program ID, ProductVariant ID, route code, raw capability JSON, priority, or binding order. Sidecar V1 does not declare a reliable workcell capability, so capability code remains an explicit selection until semantic sidecar composition exists. |

## Fairino Export Mapping

The normal automation input is one ZIP with this fixed archive layout:

```text
export-manifest.json
artifacts/<file>.lua
contracts/<file>.icebot.json
```

`export-manifest.json` owns the explicit positive, unique, contiguous `runOrder`.
The backend does not derive order from filename prefixes. Archive paths are
normalized and bounded; traversal, symbolic links, duplicate normalized paths,
unsafe compression ratios, excessive entry counts, and excessive expanded size
are rejected before an import session is created.

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

Sidecar schema behavior:

- V1 remains valid for existing projects and declares generic `System`/`Motion` effects plus phase order.
- V2 is emitted only when the Fairino step has explicit IceBot semantics. It may declare `IngredientCode`, `OptionCode`, `FixedInArtifact` quantity/unit, workcell capability, phase, and before/after effect constraints.
- Sidecar enum fields use string names; numeric enum values and the currently unsupported `Composite` effect kind are rejected.
- Fairino and Cloud never infer ingredient, option, or physical quantity from a display label or Lua filename.
- `Parameterized` quantity is rejected during bundle validation for the current Fairino runtime.

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
      "parametersJson": null
    },
    {
      "robotArtifactId": "22222222-2222-2222-2222-222222222222",
      "runOrder": 2,
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

## Related Docs

- [Robot Lua Artifact Flow](ROBOT_LUA_ARTIFACT_FLOW.md)
- [Robot Lua Deployment And Activation Flow](ROBOT_LUA_DEPLOYMENT_AND_ACTIVATION_FLOW.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Authorization Rules](../api/AUTHORIZATION_RULES.md)
