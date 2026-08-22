# Robot Artifact Operational Smoke Test

## Search Keywords

`robot artifact smoke test`, `Lua upload`, `RobotProgram`, `configuration release`, `deployment`, `execution endpoint`, `MinIO`, `artifact checksum`

Use this workflow to validate PostgreSQL migrations, MinIO object storage, execution-endpoint compatibility, and the complete artifact authoring/deployment path.

The end-to-end business flow and API lookup remain owned by [Robot Lua Artifact Flow](../flows/ROBOT_LUA_ARTIFACT_FLOW.md). This document owns only operational setup and executable verification.

## Local Runtime Dependencies

Backend compose owns PostgreSQL and MinIO for local backend operation:

```powershell
docker compose up -d postgres minio
```

Local defaults are:

```text
MinIO API: http://localhost:9000
MinIO console: http://localhost:9001
Bucket: icebot-robot-artifacts
Access key: minioadmin
Secret key: minioadmin
```

Override `MINIO_ROOT_USER`, `MINIO_ROOT_PASSWORD`, `MINIO_BUCKET_NAME`, and `MINIO_DOWNLOAD_ENDPOINT` outside source control when defaults are unsuitable. `MINIO_DOWNLOAD_ENDPOINT` must be reachable by the actual Edge/controller, not only by the backend container.

Development configuration and the local backend compose set `AutoCreateBucket=true`, so backend startup creates the private bucket when it is absent. Production defaults to `false`: infrastructure must provision the bucket before the API starts. PostgreSQL stores artifact metadata only.

## Apply Migrations To A Test Database

Set a test connection string, then apply migrations explicitly:

```powershell
$env:ConnectionStrings__IceBot_DB = "Host=localhost;Port=5432;Database=IceBotDB;Username=postgres;Password=p@ssw0rd12345"
dotnet ef database update --project src\Infrastructure --startup-project src\WebAPI --context IceBotDbContext
```

Do not point this command at a shared or production database unless that deployment is intentional.

## Automated Operational Smoke

The smoke test uses isolated PostgreSQL and MinIO Testcontainers. It applies all migrations, creates the bucket on upload, and seeds an active Full Edge endpoint with the supported robot target `FAIRINO_LUA_V1 / FR5`.

```powershell
dotnet test tests\IceBot.IntegrationTests\IceBot.IntegrationTests.csproj --filter FullyQualifiedName~RobotArtifactOperationalSmokeTests
```

The validated flow is:

```text
bulk upload .lua
-> publish RobotArtifact
-> create RobotProgram
-> assign artifact RunOrder
-> publish RobotProgram
-> create ConfigurationRelease
-> author execution route
-> publish release
-> deploy to Full Edge endpoint
-> pull and accept command
-> report Installed
-> report Active
-> create and pay a machine-produced order
-> resolve endpoint/release/route/program
-> create ExecuteOrder dispatch attempt 1
-> repeat attempt 1 and receive the same command
-> acknowledge Accepted and project Order Accepted
-> report Running with typed stock evidence and project Order Preparing
-> report Completed and project Order Completed
-> verify Rejected, ExecutorBusy, and Failed order outcomes
```

Success requires the deployment and endpoint projection to reference the active release, and the paid order to produce exactly one idempotent `ExecuteOrder` command for the selected endpoint.

## Contract Coverage

The broader Edge/controller integration suite additionally verifies presigned download, byte length, SHA-256 checksum, Accepted/Rejected acknowledgement, Installed/Active/Failed reports, and duplicate `SourceEventId` handling:

```powershell
dotnet test tests\IceBot.IntegrationTests\IceBot.IntegrationTests.csproj
```

## Related Docs

- [Robot Lua Artifact Flow](../flows/ROBOT_LUA_ARTIFACT_FLOW.md)
- [Robot Lua Deployment And Activation Flow](../flows/ROBOT_LUA_DEPLOYMENT_AND_ACTIVATION_FLOW.md)
- [Deployment Configuration](DEPLOYMENT_CONFIG.md)
- [MQTT Operations](MQTT_OPERATIONS.md)
