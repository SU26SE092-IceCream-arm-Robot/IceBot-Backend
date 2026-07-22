# Robot Lua Artifact Flow

This is the entry point for the current backend flow for Fairino `.lua` artifacts. It indexes the authoring and deployment contracts without duplicating their detailed API and operational rules.

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
  -> Full Edge chooses bundle or changed .lua files; Low-cost downloads selected .lua files
  -> endpoint verifies byte length and SHA-256 checksum
  -> endpoint installs and activates configuration
  -> endpoint reports deployment result to Cloud
```

## Flow Documents

- [Robot Lua Authoring And Import Flow](ROBOT_LUA_AUTHORING_AND_IMPORT_FLOW.md): Fairino export bundle, sidecars, templates, artifact lifecycle, and ordered `RobotProgram` authoring.
- [Robot Lua Deployment And Activation Flow](ROBOT_LUA_DEPLOYMENT_AND_ACTIVATION_FLOW.md): release authoring, endpoint provisioning, preview, deployment, download, activation, rollback, and retry rules.

## Documentation Ownership

- This file owns the shared boundary and the end-to-end index.
- The authoring and import document owns authoring-side APIs and artifact/program lifecycle rules.
- The deployment and activation document owns release-to-endpoint deployment rules and artifact-specific activation behavior.
- [API Surface Rules](../api/API_SURFACE_RULES.md) owns route placement, request/response boundaries, paging, and transport-facing behavior.
- [IoT Contract](../iot/IOT_CONTRACT.md) owns Edge/controller authentication, command pull/ack, report envelopes, and retry semantics.
- [Deployment Configuration](../operations/DEPLOYMENT_CONFIG.md) owns environment variables, secrets, storage endpoints, and timeout settings.
- [Robot Artifact Operational Smoke Test](../operations/ROBOT_ARTIFACT_OPERATIONAL_SMOKE.md) owns runnable migration, MinIO, and integration verification commands.

Other documents should summarize only the rules they own and link to the smallest owning flow instead of maintaining a second copy of this lifecycle.

## Related Docs

- [System Flows](SYSTEM_FLOWS.md)
- [Back-Office Setup Flow](BACK_OFFICE_SETUP_FLOW.md)
- [Robot Artifact Operational Smoke Test](../operations/ROBOT_ARTIFACT_OPERATIONAL_SMOKE.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Authorization Rules](../api/AUTHORIZATION_RULES.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
- [Deployment Configuration](../operations/DEPLOYMENT_CONFIG.md)
- [Boundary Contexts](../architecture/BOUNDARY_CONTEXTS.md)
