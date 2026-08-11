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
-> backend validates structure and materializes Draft resources automatically
-> GET workspace
-> POST publish resources
-> open Bind Configuration
-> create an operator-declared Recipe-to-Program binding
-> open release authoring
-> create/review/publish a release from existing bindings
-> select an execution endpoint and request deployment
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
ownership context, composition blockers, and currently allowed authoring
actions. The client must use returned actions/blockers as guidance but
must still call the typed command route for the action; an action code is not a
generic mutation endpoint and does not grant permission.

| Workspace action | Typed command to call | When it is the normal next step |
| --- | --- | --- |
| `ValidateImport` / `MaterializeImport` | `POST .../{importId}/resume` | Recovery only: one operator action resumes structural validation and Draft-resource materialization after an interrupted or blocked import. The normal upload already performs both operations automatically. |
| `PublishImportResources` | `POST .../{importId}/publish-resources` | Draft artifact bytes and RobotProgram order were reviewed. Recipe binding is not required for technical-resource publication. |
| `PreviewSemanticComposition` / `ConfirmSemanticComposition` | `POST .../{importId}/preview-composition` and `POST .../{importId}/confirm-composition` | Advanced import-local orchestration only. The normal WebApp uses the separate Production Program Binding API after publication. These operations compare declarations and never certify Lua behavior. |
| Release and deployment actions | Kiosk configuration workspace | Published resources are selected deliberately when authoring/reviewing a release and then deploying it to an endpoint. They are outside the import workspace. |

`robot-artifacts`, `robot-artifact-technical-contracts`, and `robot-programs`
are advanced technical-authoring resources. Use them to inspect, repair, clone,
or deliberately build a graph without a normal import. They are not the normal
bundle-import sequence.

The shared product/UI journey is [Robot Authoring Workspace Journey](../../../IceBot-Product/product/journeys/ROBOT_AUTHORING_WORKSPACE.md).

The management result uses `Materialized` and `ResourcesPublished` as public
status values. It exposes `materializedRobotProgramId`, `materializedAt`, and
validation `canMaterialize`. The persistence aggregate retains its internal
`Applied` naming; that storage detail is not part of the API contract.

Lua is a black box to Cloud in every authoring path. A sidecar or published
technical contract is an optional operator declaration, not a certificate that
describes Lua behavior. Contract validation proves only that the referenced
declaration exists, is immutable/published, belongs to the correct scope, and
has not been corrupted. It does not prove that the declaration describes Lua.
Recipe expectation is not a runtime consumption fact; Edge/device/sensor
evidence owns reconciliation of actual execution and consumption.

## Step And API Lookup

| Step | Actor | API / operation | Effect |
| --- | --- | --- | --- |
| 1. Edit project | Fairino-Studio user | No backend API | Saves Blockly/editor state in a local `.fairobot` project. |
| 2. Export Lua | Fairino-Studio user | No backend API | The normal export produces one `*-export.zip` containing `export-manifest.json`, ordered `.lua` files under `artifacts/`, and matching `.icebot.json` declaration wrappers under `contracts/`. A wrapper may contain empty `effects` and `orderingConstraints`; Fairino does not invent production meaning. Explicit metadata remains an operator declaration. |
| 2I. Find/resume import | Management UI | `GET /api/v1/management/organizations/{organizationId}/robot-authoring-imports` | Reads the durable, paged organization import inbox after reload or hand-off. Filters only narrow the authorized organization result. Select a returned `importId`, then read its workspace; the inbox never returns staged ZIP bytes, storage keys, raw Lua, or item checksums. |
| 2A. Import authoring bundle | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports` | Uploads one bounded ZIP with `Idempotency-Key`, validates archive/file identity, checksums, explicit `RunOrder`, checks durable resource conflicts, and automatically materializes organization-scoped Draft artifacts and one ordered Draft RobotProgram when no blocker exists. Runtime target/model and declared effects are diagnostic declarations, not compatibility proof. |
| 2AL. Import raw Lua into Draft program | Advanced API; not exposed by the normal WebApp | `POST /api/v1/management/organizations/{organizationId}/robot-programs/{programId}/raw-lua-artifacts` | Compatibility API for deliberate technical repair. It is not a second normal authoring lifecycle. The normal WebApp accepts only the Fairino Production-aware bundle. |
| 2B. Resume interrupted import | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/resume` | Idempotently resumes structural validation and Draft-resource materialization through one recovery action. It is shown only when an import did not reach `Materialized`; validation blockers remain visible for correction. |
| 2B1. Validate authoring bundle | Advanced diagnostics/recovery | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/validate` | Rechecks staged bytes, manifest identity/order, artifact revision conflicts, and program identity. Declaration conflicts, missing production declarations, and declared ordering differences are warnings. They do not prove or block Lua behavior. The normal WebApp does not expose this as a separate step. |
| 2C. Materialize authoring bundle | Advanced diagnostics/recovery | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/materialize` | Materializes immutable Draft artifacts and one ordered Draft RobotProgram. A usable non-empty declaration may also create/reuse a Draft technical contract; absent, conflicting, or empty declarations do not block artifact/program creation. The normal WebApp reaches this automatically through upload or resume. |
| 2C1. Adjust materialized Draft order | Management UI | `PUT /api/v1/management/organizations/{organizationId}/robot-programs/{programId}/artifacts` | The operator owns the full contiguous order. Stale edits return `409`; declaration phase/before/after metadata may warn but never silently reorders or blocks the manifest. |
| 2S. Preview import-local composition | Advanced automation | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/preview-composition` | Builds an integrity-protected advisory preview for automation that deliberately uses the import-local release-draft API. It is not the normal WebApp binding screen. |
| 2SC. Confirm import-local composition | Advanced automation | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/confirm-composition` | Records the selected Recipe/options for the advanced import-local release-draft API. It does not certify Lua behavior or replace the normal Production Program Binding lifecycle. |
| 2P. Publish technical resources | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/publish-resources` | Publishes integrity-checked artifact bytes and the ordered RobotProgram independently of Recipe binding. Optional declarations are published/assigned when present and valid; artifacts without declarations remain publishable black boxes. The normal UI performs Recipe-to-Program binding afterward in the separate Bind Configuration lifecycle. |
| 2R. Create release draft from import | Advanced management automation | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/create-release-draft` | Retained typed orchestration API for an explicit automated workflow. It requires both import publication and the same confirmed Recipe/option composition, then derives the Published program, route code, capability JSON, priority, and binding order from the published artifact contracts. The caller cannot supply a capability code. The normal authoring UI does not call it: release creation, endpoint selection, and deployment belong to the Kiosk configuration workspace. |
| 2W. Read authoring workspace | Management UI | `GET /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/workspace` | Aggregates import progress and optional Recipe suggestions matched only from operator-declared metadata. Zero, one, or multiple matches always require explicit Recipe selection/confirmation; lack of a suggestion does not block manual binding. |
| 2D. Read/discard import | Management UI | `GET .../robot-authoring-imports/{importId}` and `POST .../{importId}/discard` | Returns import status and lifecycle actions. Only imports that have not reached `Materialized` may be discarded; staged ZIP deletion is best effort. Cleanup retains staging bytes while an import remains `Uploaded`, `Validated`, or `Failed` so its advertised retry actions remain executable. `Materialized`, `ResourcesPublished`, or `Discarded` staging bytes may be removed by retention cleanup. |
| 2T. Manage global templates | SystemAdmin | `POST /api/v1/management/robot-artifact-templates`, then `PATCH /api/v1/management/robot-artifact-templates/{templateId}/publish` | Uploads reusable Lua templates as Draft and publishes reviewed templates. Templates may be listed, inspected, reviewed through a short-lived URL, and retired, but never execute directly. An incorrect unreferenced Draft may be discarded with `DELETE /api/v1/management/robot-artifact-templates/{templateId}`. Platform-owned technical contracts use the distinct `/api/v1/management/robot-artifact-template-contracts` collection. |
| 3. Find existing artifacts | Management UI | `GET /api/v1/management/organizations/{organizationId}/robot-artifacts` | Returns a tenant-scoped, paged artifact list with optional `search` and `status`. |
| 3T. Find global templates | Management UI | `GET /api/v1/management/robot-artifact-templates` | Returns reusable global templates. `SystemAdmin` manages them; `OrgAdmin` may inspect them. |
| 3C. Clone template | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-artifacts/from-template` | Copies one Published template into a separate organization-owned Draft artifact. An optional referenced declaration must remain Published, scope-correct, and checksum-consistent; templates without declarations are also cloneable. It does not publish or assign program membership. |
| 4. Upload files | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-artifacts` | Uploads up to 50 files with metadata. Each successful item creates one unassigned Draft `RobotArtifact`; no program sequence changes. Policy: `artifact.upload`. |
| 4A. Import technical sidecars | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-artifact-technical-contracts/import-sidecars` | Converts 1-50 reviewed Fairino V1 or V2 `.icebot.json` sidecars into organization-owned Draft technical contracts with item-level results. The persisted technical contract retains the supplied schema version. Re-importing the same code/version replaces that Draft only when schema version and runtime target remain unchanged; Published or Retired versions require a new version. Contracts are not published automatically. |
| 4B. Author technical declaration | Management UI | `GET`, `PUT`, validation-preview, publish, retire, and Draft-discard routes under the organization technical-contract resource | Reviews and publishes optional operator-declared metadata, then may assign it to a Draft artifact. The declaration is not behavior provenance or certification. |
| 5. Inspect artifact | Management UI | `GET /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}` | Returns artifact metadata only when both artifact and organization match the caller's scope. It does not return a download URL. |
| 5A. Review Lua bytes | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/review-url` | Returns an `artifact.upload`-authorized short-lived presigned URL plus checksum/size metadata. The URL is ephemeral and must not be persisted as artifact identity. |
| 5B. Discard staging artifact | Management UI | `DELETE /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}` | Hard-deletes only an unreferenced Draft artifact. Metadata commits first; object deletion is best-effort and orphan cleanup handles any remaining object. |
| 6. Publish reviewed artifacts | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/robot-artifacts/publish` | Atomically publishes 1-100 unique selected Draft artifacts after object-storage size/SHA-256 verification. A technical declaration is optional; when referenced it must be Published, scope-correct, and checksum-consistent. |
| 6A. Publish one artifact | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/publish` | Single-artifact alternative with explicit organization ownership. |
| 6B. Retire artifact | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/retire` | Stops new authoring use without deleting Lua bytes or breaking published manifests/rollback. Draft program references must be removed first. |
| 7. Create program | Management UI | `POST /api/v1/management/organizations/{organizationId}/robot-programs` | Creates an organization-owned Draft `RobotProgram`, optionally narrowed to Store, Kiosk, or Device scope. Policy: `program.manage`. |
| 8. Set execution order | Management UI | `PUT /api/v1/management/organizations/{organizationId}/robot-programs/{programId}/artifacts` | Atomically replaces the complete `RobotProgramArtifact` membership and contiguous explicit `RunOrder` values while the program is Draft. An editor should send `expectedLastModifiedAt` from the read model; a stale value returns `409` instead of overwriting another editor's reorder. |
| 9. Edit program metadata | Management UI | `PUT /api/v1/management/organizations/{organizationId}/robot-programs/{programId}` | Updates Draft code, name, and description. Scope remains immutable. |
| 10. Review programs | Management UI | `GET /api/v1/management/organizations/{organizationId}/robot-programs` and `GET /api/v1/management/organizations/{organizationId}/robot-programs/{programId}` | Returns tenant-scoped program data and ordered artifact metadata for review/reordering. Policy: `program.read`. |
| 11. Publish program | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/robot-programs/{programId}/publish` | Validates referenced artifacts and publishes the immutable program definition. |
| 11A. Retire program | Management UI | `PATCH /api/v1/management/organizations/{organizationId}/robot-programs/{programId}/retire` | Stops new release authoring while preserving published release and rollback history. Draft release references must be removed first. |
| 11D. Discard program draft | Management UI | `DELETE /api/v1/management/organizations/{organizationId}/robot-programs/{programId}` | Hard-deletes only a Draft program and its ordered membership. Published or release-referenced programs are preserved. |
| 11B. Load release options | Management UI | `GET /api/v1/management/organizations/{organizationId}/configuration-releases/authoring-options` | Returns eligible organization-owned machine-produced ProductVariant, Published/Active Recipe with production-affecting option candidates, and Published RobotProgram options with declared workcell capability codes. `workcellCapabilities` indexes each code to the published programs that declare it. The response has no capability-version candidate because endpoint readiness does not report comparable versions. Optional `productVariantId`, `search`, and per-group `limit` reduce selector payloads. Global templates are excluded by default; only a SystemAdmin may explicitly request `includeGlobalTemplates=true`. Route submission sends `RecipeId`; backend derives and stores its `ProductVariantId`. |

`POST /api/v1/management/organizations/{organizationId}/production-program-bindings` accepts the Recipe, Published RobotProgram, and supported production-option codes. Creating the binding is the operator confirmation boundary. Backend snapshots capability proposals from optional published declarations into `requiredCapabilityCodes`; these are `DeclaredRequiredCapabilityCodes`, not inferred Lua requirements. Missing declarations contribute no invented capability.
| 11C. Automated release linkage | Advanced management automation | `POST /api/v1/management/organizations/{organizationId}/robot-authoring-imports/{importId}/create-release-draft` | Optional automation API after import publication, not the normal custom-authoring UI path. The normal path selects and reviews the published program and Recipe in the Kiosk configuration workspace. |

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
