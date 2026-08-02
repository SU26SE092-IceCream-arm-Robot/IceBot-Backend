# Robot Lua Authoring And Import Flow

This document owns Fairino export, authoring-bundle import, template/artifact lifecycle, technical contracts, and ordered `RobotProgram` authoring.

## Search Keywords

`Fairino Studio`, `.lua`, `Lua export`, `RobotArtifact`, `RobotArtifactTemplate`, `RobotArtifactTechnicalContract`, `RobotProgram`, `RobotProgramArtifact`, `RunOrder`, `artifact.upload`, `program.manage`, `authoring import`, `sidecar`

The shared source of truth, boundary, and full lifecycle index are in [Robot Lua Artifact Flow](ROBOT_LUA_ARTIFACT_FLOW.md). Release, deployment, download, and activation rules are in [Robot Lua Deployment And Activation Flow](ROBOT_LUA_DEPLOYMENT_AND_ACTIVATION_FLOW.md).

## Primary FE Integration Journey

The normal FE path is one guided authoring workspace. It must not construct a
RobotProgram by calling artifact, technical-contract, and program CRUD routes
one by one after a normal Fairino export.

```text
GET import inbox
-> choose or resume import
-> GET workspace
POST import bundle
-> GET workspace
-> POST validate
-> GET workspace
-> POST materialize (create Draft resources)
-> GET workspace
-> POST preview composition
-> POST confirm composition
-> POST publish resources
-> POST create release draft
-> GET workspace
-> use normal release publication/deployment workflow
```

On initial page load or after losing a local `importId`, read
`GET /api/v1/management/organizations/{organizationId}/robot-authoring-imports`
first. It is the durable organization-scoped inbox; use its selected `importId`
to open the workspace. It accepts `status`, `storeId`, `kioskId`, `deviceId`,
`search`, `createdFrom`, `createdTo`, `pageNumber`, and `pageSize`. `status` uses the public lifecycle
names `Uploaded`, `Validated`, `Materialized`, `ResourcesPublished`, `Failed`,
or `Discarded`. The inbox returns safe operational summaries only, not staged
bundle/object-storage data or item checksums.

After every mutation, read `GET .../robot-authoring-imports/{importId}/workspace`.
It is the convergence read model for import status, validation, package
ownership context, release status, deployment preview, blockers, and currently
allowed actions. The client must use returned actions/blockers as guidance but
must still call the typed command route for the action; an action code is not a
generic mutation endpoint and does not grant permission.

| Workspace action | Typed command to call | When it is the normal next step |
| --- | --- | --- |
| `ValidateImport` | `POST .../{importId}/validate` | A bundle was staged. |
| `MaterializeImport` | `POST .../{importId}/materialize` | Validation succeeds. This materializes Draft contracts, artifacts, and program. |
| `PreviewSemanticComposition` | `POST .../{importId}/preview-composition` | Draft resources need a Recipe/production-option compatibility check. |
| `ConfirmSemanticComposition` | `POST .../{importId}/confirm-composition` | The exact composition preview is accepted. |
| `PublishImportResources` | `POST .../{importId}/publish-resources` | Composition has been confirmed and technical resources were reviewed. |
| `CreateConfigurationReleaseDraft` | `POST .../{importId}/create-release-draft` | Imported resources are published. |
| `PublishConfigurationRelease` | Configuration release publish route | The linked release is Draft and has passed release checks. |
| `ConfirmDeployment` | Configuration deployment route | The published release has an eligible endpoint. |

`robot-artifacts`, `robot-artifact-technical-contracts`, and `robot-programs`
are advanced technical-authoring resources. Use them to inspect, repair, clone,
or deliberately build a graph without a normal import. They are not the normal
bundle-import sequence.

The shared product/UI journey is [Robot Authoring Workspace Journey](../../../IceBot-Product/product/journeys/ROBOT_AUTHORING_WORKSPACE.md).

The management result uses `Materialized` and `ResourcesPublished` as public
status values. It exposes `materializedRobotProgramId`, `materializedAt`, and
validation `canMaterialize`. The persistence aggregate retains its internal
`Applied` naming; that storage detail is not part of the API contract.

## Step And API Lookup

| Step | Actor | API / operation | Effect |
| --- | --- | --- | --- |
| 1. Edit project | Fairino-Studio user | No backend API | Saves Blockly/editor state in a local `.fairobot` project. |
| 2. Export Lua | Fairino-Studio user | No backend API | The normal `Export LUA` action produces only one `*-export.zip` containing `export-manifest.json`, ordered `.lua` files under `artifacts/`, and matching `.icebot.json` sidecars under `contracts/`. A separate advanced menu command exports individual Lua/sidecar files for debugging. A normal editor step becomes one file; a paired loop becomes one file whose sidecar merges the semantics of both loop steps and requires one shared execution phase. |
| 2I. Find/resume import | Management UI | `GET /api/v1/management/organizations/{organizationId}/robot-authoring-imports` | Reads the durable, paged organization import inbox after reload or hand-off. Filters only narrow the authorized organization result. Select a returned `importId`, then read its workspace; the inbox never returns staged ZIP bytes, storage keys, raw Lua, or item checksums. |
| 2A. Stage authoring bundle | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports` | Uploads one bounded ZIP with `Idempotency-Key`, validates archive structure and target consistency, and creates a durable organization-scoped import session. It does not create or publish runtime resources. |
| 2B. Validate authoring bundle | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/validate` | Rechecks the staged checksum, sidecars, explicit `RunOrder`, existing artifact revisions, technical-contract identities, and program identity. Ambiguous revisions, Retired resources, and a Published artifact bound to another contract block materialization. Sidecar V1 is strictly opaque and may declare only `System`/`Motion` effects without ingredient, option, quantity, or capability semantics. Typed production semantics require V2. `System`/`Motion` effects cannot carry ingredient, option, or quantity fields. `Ingredient` requires `ingredientCode` and may carry `optionCode` when that ingredient consumption is conditional on an option. `Option` requires `optionCode` and may carry `ingredientCode` when the option consumes an ingredient. Composition accepts either typed representation for an option ingredient but still requires an exact option and ingredient match. Errors block materialization; valid V1 artifacts remain warnings during composition. |
| 2C. Materialize authoring bundle | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/materialize` | Requires both `artifact.upload` and `program.manage`. Materializes Draft technical contracts, immutable Draft artifacts, and one ordered Draft RobotProgram in one serialized metadata transaction. It never publishes or deploys. A newly created Draft contract is retained on the import item and is assigned to its artifact only after the contract is explicitly published. |
| 2S. Preview semantic composition | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/preview-composition` | Resolves required Recipe ingredients and selected production-affecting options against imported V2 technical effects. Returns proposed artifact order, conditional option membership, capability suggestions, typed blockers/warnings, and a checksum. It does not write data. V1 opaque artifacts may remain in the proposal with warnings but cannot satisfy typed ingredient/option requirements. |
| 2SC. Confirm semantic composition | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/confirm-composition` | Rebuilds the preview and requires the exact `PreviewChecksum`. With no blockers, atomically replaces membership/order on the imported Draft RobotProgram. It never publishes resources or creates a release. |
| 2P. Confirm import publication | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/publish-resources` | Explicitly publishes/reuses each contract, assigns it to the Draft artifact, verifies and publishes each artifact, then publishes the ordered program. The operation is resumable and stops with the exact resource error; it never creates a release or deployment. |
| 2R. Create release draft from import | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/create-release-draft` | After import publication, selects a Published/Active `Recipe` and supported production option codes. Backend derives the Published program, route code, capability JSON, priority, and binding order, then atomically creates one organization-owned Draft release and route. If artifact contracts declare exactly one capability, it is selected automatically; zero/multiple capabilities require explicit selection. Retry with the same selection returns the same release; a different selection returns conflict. It never publishes or deploys the release. |
| 2W. Read authoring workspace | Management UI | `GET /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/workspace` | Aggregates import progress, package-installation ownership targets, release status, compatible deployment endpoint previews, blockers, and next actions. Package ownership is informational. The workspace does not automatically require or propose a fork; a separate explicit customization workflow decides whether package-managed technical resources must be forked before mutation. |
| 2D. Read/discard import | Management UI | `GET .../robot-authoring-imports/{importId}` and `POST .../{importId}/discard` | Returns import status and lifecycle actions. Only imports that have not reached `Materialized` may be discarded; staged ZIP deletion is best effort. Cleanup retains staging bytes while an import remains `Uploaded`, `Validated`, or `Failed` so its advertised retry actions remain executable. `Materialized`, `ResourcesPublished`, or `Discarded` staging bytes may be removed by retention cleanup. |
| 2T. Manage global templates | SystemAdmin | `POST /api/v1/management/robot-artifact-templates`, then `PATCH /api/v1/management/robot-artifact-templates/{templateId}/publish` | Uploads reusable Lua templates as Draft and publishes reviewed templates. Templates may be listed, inspected, reviewed through a short-lived URL, and retired, but never execute directly. An incorrect unreferenced Draft may be discarded with `DELETE /api/v1/management/robot-artifact-templates/{templateId}`. Platform-owned technical contracts use the distinct `/api/v1/management/robot-artifact-template-contracts` collection. |
| 3. Find existing artifacts | Management UI | `GET /api/v1/management/organizations/{organizationId}/robot-artifacts` | Returns a tenant-scoped, paged artifact list with optional `search` and `status`. |
| 3T. Find global templates | Management UI | `GET /api/v1/management/robot-artifact-templates` | Returns reusable global templates. `SystemAdmin` manages them; `OrgAdmin` may inspect them. |
| 3C. Clone template | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-artifacts/from-template` | Copies one Published template with a still-Published, checksum-consistent technical contract into a separate organization-owned Draft artifact. Compatibility metadata and lineage are inherited. It does not publish or assign program membership. |
| 4. Upload files | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-artifacts` | Uploads up to 50 files with metadata. Each successful item creates one unassigned Draft `RobotArtifact`; no program sequence changes. Policy: `artifact.upload`. |
| 4A. Import technical sidecars | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-artifact-technical-contracts/import-sidecars` | Converts 1-50 reviewed Fairino V1 or V2 `.icebot.json` sidecars into organization-owned Draft technical contracts with item-level results. The persisted technical contract retains the supplied schema version. Re-importing the same code/version replaces that Draft only when schema version and runtime target remain unchanged; Published or Retired versions require a new version. Contracts are not published automatically. |
| 4B. Author technical contract | Management UI | `GET`, `PUT`, validation-preview, publish, retire, and Draft-discard routes under the organization technical-contract resource | Reviews and publishes the typed behavior provenance, then assigns its id to the Draft artifact. |
| 5. Inspect artifact | Management UI | `GET /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}` | Returns artifact metadata only when both artifact and organization match the caller's scope. It does not return a download URL. |
| 5A. Review Lua bytes | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/review-url` | Returns an `artifact.upload`-authorized short-lived presigned URL plus checksum/size metadata. The URL is ephemeral and must not be persisted as artifact identity. |
| 5B. Discard staging artifact | Management UI | `DELETE /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}` | Hard-deletes only an unreferenced Draft artifact. Metadata commits first; object deletion is best-effort and orphan cleanup handles any remaining object. |
| 6. Publish reviewed artifacts | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/robot-artifacts/publish` | Atomically publishes 1-100 unique selected Draft artifacts after staging review. Every Draft requires a compatible Published technical contract and object-storage size/SHA-256 verification. Already Published selections are idempotent success; other states reject the request. Policy: `artifact.upload`. |
| 6A. Publish one artifact | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/publish` | Single-artifact alternative with explicit organization ownership. |
| 6B. Retire artifact | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/retire` | Stops new authoring use without deleting Lua bytes or breaking published manifests/rollback. Draft program references must be removed first. |
| 7. Create program | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-programs` | Creates an organization-owned Draft `RobotProgram`, optionally narrowed to Store, Kiosk, or Device scope. Policy: `program.manage`. |
| 8. Set execution order | Management UI | `PUT /api/v1/management/organizations/{organizationId}/robot-programs/{programId}/artifacts` | Atomically replaces the complete `RobotProgramArtifact` membership and explicit `RunOrder` values while the program is Draft. |
| 9. Edit program metadata | Management UI | `PUT /api/v1/management/organizations/{organizationId}/robot-programs/{programId}` | Updates Draft code, name, and description. Scope remains immutable. |
| 10. Review programs | Management UI | `GET /api/v1/management/organizations/{organizationId}/robot-programs` and `GET /api/v1/management/organizations/{organizationId}/robot-programs/{programId}` | Returns tenant-scoped program data and ordered artifact metadata for review/reordering. Policy: `program.read`. |
| 11. Publish program | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/robot-programs/{programId}/publish` | Validates referenced artifacts and publishes the immutable program definition. |
| 11A. Retire program | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/robot-programs/{programId}/retire` | Stops new release authoring while preserving published release and rollback history. Draft release references must be removed first. |
| 11D. Discard program draft | Management UI | `DELETE /api/v1/management/organizations/{organizationId}/robot-programs/{programId}` | Hard-deletes only a Draft program and its ordered membership. Published or release-referenced programs are preserved. |
| 11B. Load release options | Management UI | `GET /api/v1/management/organizations/{organizationId}/configuration-releases/authoring-options` | Returns eligible machine-produced ProductVariant, Published/Active Recipe with production-affecting option candidates, and Published RobotProgram options with declared workcell capability codes. `workcellCapabilities` indexes each code to the published programs that declare it. The response has no capability-version candidate because endpoint readiness does not report comparable versions. Optional `productVariantId`, `search`, and per-group `limit` reduce selector payloads. Route submission sends `RecipeId`; backend derives and stores its `ProductVariantId`. |
| 11C. Automated release linkage | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/create-release-draft` | Normal custom-authoring path after import publication. The client does not submit program ID, ProductVariant ID, route code, raw capability JSON, priority, or binding order. Sidecar V1 does not declare a reliable workcell capability, so capability code remains an explicit selection until semantic sidecar composition exists. |

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
