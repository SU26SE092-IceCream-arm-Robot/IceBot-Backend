using Application.RobotConfiguration.Storage.Abstractions;
using Domain.Sync.Ingestion;
using Domain.Devices.ExecutionEndpoints;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Application.EdgeIntegration;
using Application.EdgeIntegration.Dispatch;
using Application.EdgeIntegration.Reports;
using Application.EdgeIntegration.CommandDelivery.Commands;
using Application.EdgeIntegration.Dispatch.Commands;
using Application.EdgeIntegration.Reports.Commands;
using Application.EdgeIntegration.Timeouts.Commands;
using Application.EdgeIntegration.CommandDelivery.Services;
using Application.EdgeIntegration.Dispatch.Services;
using Application.EdgeIntegration.Reports.Services;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Domain.Common.Enums;
using Domain.Devices.Catalog;
using Domain.Devices.Telemetry;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.Enums;
using Domain.Sync.Entities;
using Domain.Sync.Enums;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.EdgeIntegration.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IceBot.IntegrationTests.EdgeIntegration;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class EdgeControllerContractIntegrationTests
{
    private readonly IntegrationTestFixture _fixture;

    public EdgeControllerContractIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [IntegrationFact]
    public async Task DeploymentContract_PullDownloadVerifyAcceptInstallActivate_IsIdempotentBySourceEventId()
    {
        var luaBytes = Encoding.UTF8.GetBytes("print('icebot-contract')");
        var graph = await SeedDeploymentAsync(luaBytes);

        var pulled = await PullAsync(graph);
        var pulledCommand = Assert.Single(pulled.Data!.Commands);
        var artifact = JsonNode.Parse(pulledCommand.PayloadJson)!["Artifacts"]![0]!;
        var downloadUrl = artifact["DownloadUrl"]!.GetValue<string>();

        using var httpClient = new HttpClient();
        var download = await httpClient.GetAsync(downloadUrl);
        var downloadedBytes = await download.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal(luaBytes.Length, artifact["SizeBytes"]!.GetValue<long>());
        Assert.Equal(luaBytes, downloadedBytes);
        Assert.Equal(graph.ArtifactChecksum, Sha256(downloadedBytes));
        Assert.Equal(graph.ArtifactChecksum, artifact["Checksum"]!.GetValue<string>());

        var accepted = await AcknowledgeAsync(graph, "Accepted");
        Assert.True(accepted.Succeeded);
        Assert.Equal(nameof(EdgeCommandStatus.Accepted), accepted.Data!.Status);

        var installedEventId = Guid.NewGuid();
        var installed = await ReportAsync(graph, installedEventId, 1, "Installed");
        Assert.True(installed.Succeeded);
        Assert.True(installed.Data!.Applied);
        Assert.False(installed.Data.Duplicate);

        var activeEventId = Guid.NewGuid();
        var activeAt = DateTimeOffset.UtcNow;
        var active = await ReportAsync(graph, activeEventId, 2, "Active", edgeCreatedAt: activeAt);
        var duplicate = await ReportAsync(graph, activeEventId, 2, "Active", edgeCreatedAt: activeAt);

        Assert.True(active.Succeeded);
        Assert.True(active.Data!.Applied);
        Assert.True(duplicate.Succeeded);
        Assert.True(duplicate.Data!.Duplicate);
        Assert.False(duplicate.Data.Applied);

        await using var assertionContext = _fixture.CreateDbContext();
        var deployment = await assertionContext.KioskConfigurationDeployments.SingleAsync(x => x.Id == graph.DeploymentId);
        Assert.Equal(KioskConfigurationDeploymentStatus.Active, deployment.Status);
        Assert.Equal(activeEventId, deployment.LastEdgeDeploymentEventId);
        Assert.Equal(2, await assertionContext.SyncEventInbox.CountAsync(x => x.AggregateId == graph.CommandId));
    }

    [IntegrationFact]
    public async Task DeploymentContract_RejectedAck_RecordsReasonAndPreventsExecutionReport()
    {
        var graph = await SeedDeploymentAsync(Encoding.UTF8.GetBytes("print('reject')"));
        await PullAsync(graph);

        var rejected = await AcknowledgeAsync(graph, "Rejected", "ArtifactUnsupported", "Runtime target is unavailable.");
        var report = await ReportAsync(graph, Guid.NewGuid(), 1, "Installed");

        Assert.True(rejected.Succeeded);
        Assert.Equal(nameof(EdgeCommandStatus.Rejected), rejected.Data!.Status);
        Assert.Equal("ArtifactUnsupported", rejected.Data.RejectionCode);
        Assert.False(report.Succeeded);
        Assert.Equal(404, report.StatusCode);
    }

    [IntegrationFact]
    public async Task DeploymentContract_FailedReport_IsPersistedAndDuplicateDoesNotTransitionAgain()
    {
        var graph = await SeedDeploymentAsync(Encoding.UTF8.GetBytes("print('failure')"));
        await PullAsync(graph);
        await AcknowledgeAsync(graph, "Accepted");

        var sourceEventId = Guid.NewGuid();
        var failedAt = DateTimeOffset.UtcNow;
        var failed = await ReportAsync(graph, sourceEventId, 1, "Failed", "ChecksumMismatch",
            "Downloaded bytes did not match the manifest.", failedAt);
        var duplicate = await ReportAsync(graph, sourceEventId, 1, "Failed", "ChecksumMismatch",
            "Downloaded bytes did not match the manifest.", failedAt);

        Assert.True(failed.Succeeded);
        Assert.True(failed.Data!.Applied);
        Assert.True(duplicate.Succeeded);
        Assert.True(duplicate.Data!.Duplicate);

        await using var assertionContext = _fixture.CreateDbContext();
        var deployment = await assertionContext.KioskConfigurationDeployments.SingleAsync(x => x.Id == graph.DeploymentId);
        Assert.Equal(KioskConfigurationDeploymentStatus.Failed, deployment.Status);
        Assert.Equal("ChecksumMismatch", deployment.FailureCode);
        Assert.Equal(1, await assertionContext.SyncEventInbox.CountAsync(x => x.EventId == sourceEventId));
    }

    private async Task<Application.Shared.Wrappers.ApiResult<Application.EdgeIntegration.CommandDelivery.Results.EdgeCommandPullResult>> PullAsync(
        DeploymentGraph graph)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var handler = new PullEdgeCommandsCommandHandler(
            new EdgeCommandStore(dbContext),
            new ArtifactCommandPayloadEnricher(_fixture.CreateObjectStorage()));
        return await handler.HandleAsync(new PullEdgeCommandsCommand
        {
            KioskId = graph.KioskId,
            EndpointId = graph.EndpointId,
            MaxCommands = 1
        });
    }

    private async Task<Application.Shared.Wrappers.ApiResult<Application.EdgeIntegration.CommandDelivery.Results.EdgeCommandAckResult>> AcknowledgeAsync(
        DeploymentGraph graph,
        string status,
        string? rejectionCode = null,
        string? rejectionMessage = null)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var handler = new AcknowledgeEdgeCommandCommandHandler(
            new EdgeCommandStore(dbContext),
            new NoOpRealtimeNotificationPublisher());
        return await handler.HandleAsync(new AcknowledgeEdgeCommandCommand
        {
            KioskId = graph.KioskId,
            EndpointId = graph.EndpointId,
            CommandId = graph.CommandId,
            AckStatus = status,
            AcknowledgedAt = DateTimeOffset.UtcNow,
            RejectionCode = rejectionCode,
            RejectionMessage = rejectionMessage
        });
    }

    private async Task<Application.Shared.Wrappers.ApiResult<Application.EdgeIntegration.Reports.Results.ExecutionReportIngestResult>> ReportAsync(
        DeploymentGraph graph,
        Guid sourceEventId,
        long sequenceNumber,
        string status,
        string? errorCode = null,
        string? errorMessage = null,
        DateTimeOffset? edgeCreatedAt = null)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var reportStore = new ExecutionReportStore(dbContext);
        var handler = new IngestExecutionReportCommandHandler(
            reportStore,
            new NoOpRealtimeNotificationPublisher(),
            Options.Create(new ExecutionReportIngestionOptions()));
        return await handler.HandleAsync(new IngestExecutionReportCommand
        {
            KioskId = graph.KioskId,
            EndpointId = graph.EndpointId,
            CommandId = graph.CommandId,
            SourceEventId = sourceEventId,
            SequenceNumber = sequenceNumber,
            EdgeCreatedAt = edgeCreatedAt ?? DateTimeOffset.UtcNow,
            ReportType = "Deployment",
            Status = status,
            DeploymentId = graph.DeploymentId,
            SourceConfigurationReleaseId = graph.ReleaseId,
            ReleaseChecksum = graph.ReleaseChecksum,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        });
    }

    private async Task<DeploymentGraph> SeedDeploymentAsync(byte[] luaBytes)
    {
        var storage = _fixture.CreateObjectStorage();
        var checksum = Sha256(luaBytes);
        var storageKey = $"robot-artifacts/contract/{Guid.NewGuid():N}/{checksum}.lua";
        await using (var content = new MemoryStream(luaBytes))
        {
            await storage.WriteImmutableAsync(
                new ArtifactObjectWriteRequest(storageKey, "text/x-lua", luaBytes.Length, checksum),
                content);
        }

        await using var dbContext = _fixture.CreateDbContext();
        var organization = new Organization
        {
            Code = $"ORG-{Guid.NewGuid():N}",
            Name = "Contract organization",
            Status = EntityStatus.Active
        };
        var store = new Store
        {
            OrganizationId = organization.Id,
            Code = $"STORE-{Guid.NewGuid():N}",
            Name = "Contract store",
            Status = EntityStatus.Active
        };
        var kiosk = new Kiosk
        {
            OrganizationId = organization.Id,
            StoreId = store.Id,
            Code = $"KIOSK-{Guid.NewGuid():N}",
            Name = "Contract kiosk",
            Status = KioskStatus.Active
        };
        var edgeRuntimeId = Guid.NewGuid();
        var endpoint = KioskExecutionEndpoint.CreateProvisioning(
            kiosk.Id,
            $"EDGE-{Guid.NewGuid():N}",
            KioskExecutionProfile.FullEdge,
            ExecutionEndpointAuthenticationMode.MutualTls);
        var release = ConfigurationRelease.CreateDraft(organization.Id, 1);
        var releaseChecksum = Sha256(Guid.NewGuid().ToByteArray());
        SetProperty(release, nameof(ConfigurationRelease.ReleaseChecksum), releaseChecksum);

        dbContext.AddRange(organization, store, kiosk, endpoint, release);
        await dbContext.SaveChangesAsync();

        var credential = endpoint.ProvisionCredential($"cert-{Guid.NewGuid():N}", DateTimeOffset.UtcNow);
        endpoint.Activate(edgeRuntimeId, DateTimeOffset.UtcNow);
        dbContext.ExecutionEndpointCredentialBindings.Add(credential);
        await dbContext.SaveChangesAsync();

        var deployment = KioskConfigurationDeployment.CreatePending(
            kiosk.Id,
            organization.Id,
            endpoint.Id,
            edgeRuntimeId,
            release.Id,
            releaseChecksum,
            1,
            Guid.NewGuid().ToString("N"),
            DateTimeOffset.UtcNow);
        var payload = new JsonObject
        {
            ["DeploymentId"] = deployment.Id,
            ["Artifacts"] = new JsonArray
            {
                new JsonObject
                {
                    ["FileName"] = "make_ice_cream.lua",
                    ["StorageKey"] = storageKey,
                    ["Checksum"] = checksum,
                    ["SizeBytes"] = luaBytes.Length
                }
            }
        }.ToJsonString();
        var command = EdgeCommand.Create(
            EdgeCommandType.DeployConfiguration,
            kiosk.Id,
            endpoint.Id,
            payload,
            DateTimeOffset.UtcNow,
            commandExpiryAt: DateTimeOffset.UtcNow.AddMinutes(10),
            deploymentId: deployment.Id,
            deploymentKind: DeploymentCommandTargetKind.FullEdgeConfiguration);

        dbContext.AddRange(deployment, command);
        await dbContext.SaveChangesAsync();

        return new DeploymentGraph(
            kiosk.Id, endpoint.Id, deployment.Id, command.Id, release.Id, releaseChecksum, checksum);
    }

    private static string Sha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static void SetProperty<T>(object target, string propertyName, T value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Property {propertyName} was not found.");
        property.SetValue(target, value);
    }

    private sealed record DeploymentGraph(
        Guid KioskId,
        Guid EndpointId,
        Guid DeploymentId,
        Guid CommandId,
        Guid ReleaseId,
        string ReleaseChecksum,
        string ArtifactChecksum);
}
