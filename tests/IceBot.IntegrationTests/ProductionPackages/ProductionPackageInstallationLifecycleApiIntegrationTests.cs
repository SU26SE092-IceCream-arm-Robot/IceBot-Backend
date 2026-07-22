using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.RobotConfiguration.Storage.Abstractions;
using Domain.ProductionPackages;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using IceBot.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IceBot.IntegrationTests.ProductionPackages;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class ProductionPackageInstallationLifecycleApiIntegrationTests(
    IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task FailedInstallation_CanRetryAfterMissingSourceObjectIsRestored()
    {
        var actorId = Guid.NewGuid();
        var storage = fixture.CreateObjectStorage(autoCreateBucket: true);
        var scenario = await ProductionPackageInstallationScenarioSeed.SeedAsync(fixture, storage, actorId);
        await storage.DeleteIfExistsAsync(scenario.TemplateStorageKey, CancellationToken.None);
        await using var factory = new PackageApiWebApplicationFactory(fixture, storage, actorId);
        using var client = factory.CreateAuthenticatedClient();
        var idempotencyKey = $"missing-source-{Guid.NewGuid():N}";

        using (var failedResponse = await SendInstallAsync(
                   client, scenario, scenario.KioskId, idempotencyKey))
        {
            var responseBody = await failedResponse.Content.ReadAsStringAsync();
            Assert.True(failedResponse.StatusCode == HttpStatusCode.Conflict, responseBody);
            using var failure = await ReadJsonAsync(failedResponse);
            Assert.False(failure.RootElement.GetProperty("succeeded").GetBoolean());
        }

        Guid installationId;
        await using (var failedContext = fixture.CreateDbContext())
        {
            var installation = await failedContext.ProductionPackageInstallations.AsNoTracking()
                .SingleAsync(item => item.OrganizationId == scenario.OrganizationId &&
                    item.IdempotencyKey == idempotencyKey);
            installationId = installation.Id;
            Assert.Equal(ProductionPackageInstallationStatus.Failed, installation.Status);
            Assert.Empty(await failedContext.ProductionPackageMaterializations
                .Where(item => item.InstallationId == installationId)
                .ToArrayAsync());
        }

        await using (var source = new MemoryStream(
                         ProductionPackageInstallationScenarioSeed.ArtifactBytes, writable: false))
        {
            await storage.WriteImmutableAsync(
                new ArtifactObjectWriteRequest(
                    scenario.TemplateStorageKey,
                    "text/x-lua",
                    ProductionPackageInstallationScenarioSeed.ArtifactBytes.LongLength,
                    ProductionPackageInstallationScenarioSeed.ArtifactChecksum),
                source,
                CancellationToken.None);
        }

        using (var retryResponse = await client.PostAsync(
                   $"{InstallationPath(scenario.OrganizationId, installationId)}/retry", null))
        {
            var responseBody = await retryResponse.Content.ReadAsStringAsync();
            Assert.True(retryResponse.StatusCode == HttpStatusCode.Created, responseBody);
            using var retry = await ReadJsonAsync(retryResponse);
            var data = retry.RootElement.GetProperty("data");
            Assert.Equal(installationId, data.GetProperty("id").GetGuid());
            Assert.Equal("Installed", data.GetProperty("status").GetString());
        }

        await using var assertionContext = fixture.CreateDbContext();
        Assert.Equal(1, await assertionContext.ProductionPackageInstallations.CountAsync(
            item => item.OrganizationId == scenario.OrganizationId && item.IdempotencyKey == idempotencyKey));
        Assert.Equal(1, await assertionContext.ProductionPackageMaterializations.CountAsync(
            item => item.InstallationId == installationId &&
                item.ResourceKind == ProductionPackageResourceKind.RobotArtifact));
        Assert.Equal(1, await assertionContext.ProductionPackageMaterializations.CountAsync(
            item => item.InstallationId == installationId &&
                item.ResourceKind == ProductionPackageResourceKind.RobotProgram));
        Assert.Equal(1, await assertionContext.ProductionPackageMaterializations.CountAsync(
            item => item.InstallationId == installationId &&
                item.ResourceKind == ProductionPackageResourceKind.ConfigurationRelease));
    }

    [IntegrationFact]
    public async Task Fork_SharedArtifact_UsesCopyOnWriteWithoutChangingOtherInstallation()
    {
        var actorId = Guid.NewGuid();
        var storage = fixture.CreateObjectStorage(autoCreateBucket: true);
        var scenario = await ProductionPackageInstallationScenarioSeed.SeedAsync(fixture, storage, actorId);
        Guid secondKioskId;
        await using (var seedContext = fixture.CreateDbContext())
        {
            var secondKiosk = new Kiosk
            {
                OrganizationId = scenario.OrganizationId,
                StoreId = scenario.StoreId,
                Code = $"KIOSK-{Guid.NewGuid():N}",
                Name = "Shared package second kiosk",
                Status = KioskStatus.Active
            };
            seedContext.Kiosks.Add(secondKiosk);
            await seedContext.SaveChangesAsync();
            secondKioskId = secondKiosk.Id;
        }

        await using var factory = new PackageApiWebApplicationFactory(fixture, storage, actorId);
        using var client = factory.CreateAuthenticatedClient();
        var firstInstallationId = await InstallAsync(client, scenario, scenario.KioskId);
        var secondInstallationId = await InstallAsync(client, scenario, secondKioskId);

        Guid sharedArtifactId;
        Guid secondProgramId;
        await using (var beforeForkContext = fixture.CreateDbContext())
        {
            var firstArtifactId = await MaterializedTargetIdAsync(
                beforeForkContext, firstInstallationId, ProductionPackageResourceKind.RobotArtifact);
            sharedArtifactId = await MaterializedTargetIdAsync(
                beforeForkContext, secondInstallationId, ProductionPackageResourceKind.RobotArtifact);
            Assert.Equal(sharedArtifactId, firstArtifactId);
            secondProgramId = await MaterializedTargetIdAsync(
                beforeForkContext, secondInstallationId, ProductionPackageResourceKind.RobotProgram);
        }

        using (var response = await client.PostAsync(
                   $"{InstallationPath(scenario.OrganizationId, firstInstallationId)}/fork", null))
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK, responseBody);
        }

        await using var assertionContext = fixture.CreateDbContext();
        var forkedArtifactId = await MaterializedTargetIdAsync(
            assertionContext, firstInstallationId, ProductionPackageResourceKind.RobotArtifact);
        var unchangedArtifactId = await MaterializedTargetIdAsync(
            assertionContext, secondInstallationId, ProductionPackageResourceKind.RobotArtifact);
        var forkedProgramId = await MaterializedTargetIdAsync(
            assertionContext, firstInstallationId, ProductionPackageResourceKind.RobotProgram);
        Assert.NotEqual(sharedArtifactId, forkedArtifactId);
        Assert.Equal(sharedArtifactId, unchangedArtifactId);
        Assert.Equal(forkedArtifactId, await assertionContext.RobotProgramArtifacts
            .Where(item => item.RobotProgramId == forkedProgramId)
            .Select(item => item.RobotArtifactId)
            .SingleAsync());
        Assert.Equal(sharedArtifactId, await assertionContext.RobotProgramArtifacts
            .Where(item => item.RobotProgramId == secondProgramId)
            .Select(item => item.RobotArtifactId)
            .SingleAsync());

        var forkedArtifact = await assertionContext.RobotArtifacts.AsNoTracking()
            .SingleAsync(item => item.Id == forkedArtifactId);
        var copiedBytes = await storage.ReadBytesAsync(
            forkedArtifact.StorageKey, forkedArtifact.ContentLengthBytes, CancellationToken.None);
        Assert.Equal(ProductionPackageInstallationScenarioSeed.ArtifactBytes, copiedBytes);
    }

    [IntegrationFact]
    public async Task InstalledPackage_CanBeForkedOnceThroughHttp()
    {
        var actorId = Guid.NewGuid();
        var storage = fixture.CreateObjectStorage(autoCreateBucket: true);
        var scenario = await ProductionPackageInstallationScenarioSeed.SeedAsync(fixture, storage, actorId);
        await using var factory = new PackageApiWebApplicationFactory(fixture, storage, actorId);
        using var client = factory.CreateAuthenticatedClient();
        var installationId = await InstallAsync(client, scenario, scenario.KioskId);
        var path = InstallationPath(scenario.OrganizationId, installationId);

        using (var response = await client.PostAsync($"{path}/fork", null))
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK, responseBody);
            using var result = await ReadJsonAsync(response);
            var data = result.RootElement.GetProperty("data");
            Assert.True(result.RootElement.GetProperty("succeeded").GetBoolean());
            Assert.Equal("OrganizationFork", data.GetProperty("ownershipMode").GetString());
        }

        using (var repeatedResponse = await client.PostAsync($"{path}/fork", null))
        {
            Assert.Equal(HttpStatusCode.Conflict, repeatedResponse.StatusCode);
            using var result = await ReadJsonAsync(repeatedResponse);
            Assert.False(result.RootElement.GetProperty("succeeded").GetBoolean());
        }

        await using var assertionContext = fixture.CreateDbContext();
        var installation = await assertionContext.ProductionPackageInstallations.AsNoTracking()
            .Include(item => item.Materializations)
            .SingleAsync(item => item.Id == installationId);
        Assert.Equal(ProductionPackageOwnershipMode.OrganizationFork, installation.OwnershipMode);
        var programId = Guid.Parse(Assert.Single(installation.Materializations,
            item => item.ResourceKind == ProductionPackageResourceKind.RobotProgram).TargetKey);
        Assert.Equal(1, await assertionContext.RobotProgramArtifacts.CountAsync(
            item => item.RobotProgramId == programId));
    }

    [IntegrationFact]
    public async Task Repair_RestoresSoftDeletedArtifactInPlace_AndThenBecomesNoOp()
    {
        var actorId = Guid.NewGuid();
        var storage = fixture.CreateObjectStorage(autoCreateBucket: true);
        var scenario = await ProductionPackageInstallationScenarioSeed.SeedAsync(fixture, storage, actorId);
        await using var factory = new PackageApiWebApplicationFactory(fixture, storage, actorId);
        using var client = factory.CreateAuthenticatedClient();
        var installationId = await InstallAsync(client, scenario, scenario.KioskId);

        Guid artifactId;
        await using (var mutationContext = fixture.CreateDbContext())
        {
            artifactId = Guid.Parse(await mutationContext.ProductionPackageMaterializations.AsNoTracking()
                .Where(item => item.InstallationId == installationId &&
                    item.ResourceKind == ProductionPackageResourceKind.RobotArtifact)
                .Select(item => item.TargetKey)
                .SingleAsync());
            var artifact = await mutationContext.RobotArtifacts
                .SingleAsync(item => item.Id == artifactId);
            artifact.DeletedAt = DateTimeOffset.UtcNow;
            artifact.DeletedByAccountId = actorId;
            await mutationContext.SaveChangesAsync();
        }

        var path = $"{InstallationPath(scenario.OrganizationId, installationId)}/repair";
        using (var response = await client.PostAsync(path, null))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var result = await ReadJsonAsync(response);
            var restored = result.RootElement.GetProperty("data").GetProperty("restoredResources");
            var restoredArtifact = Assert.Single(restored.EnumerateArray());
            Assert.Equal("RobotArtifact", restoredArtifact.GetProperty("resourceKind").GetString());
            Assert.Equal(artifactId.ToString("D"), restoredArtifact.GetProperty("targetKey").GetString());
        }

        await using (var assertionContext = fixture.CreateDbContext())
        {
            var artifact = await assertionContext.RobotArtifacts.IgnoreQueryFilters().AsNoTracking()
                .SingleAsync(item => item.Id == artifactId);
            Assert.Null(artifact.DeletedAt);
            Assert.Null(artifact.DeletedByAccountId);
            Assert.Equal(actorId, artifact.UpdatedByAccountId);
        }

        using (var repeatedResponse = await client.PostAsync(path, null))
        {
            Assert.Equal(HttpStatusCode.OK, repeatedResponse.StatusCode);
            using var result = await ReadJsonAsync(repeatedResponse);
            Assert.Empty(result.RootElement.GetProperty("data").GetProperty("restoredResources")
                .EnumerateArray());
        }
    }

    private static async Task<Guid> InstallAsync(
        HttpClient client,
        ProductionPackageInstallationScenario scenario,
        Guid kioskId)
    {
        using var response = await SendInstallAsync(
            client, scenario, kioskId, $"lifecycle-{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var result = await ReadJsonAsync(response);
        return result.RootElement.GetProperty("data").GetProperty("id").GetGuid();
    }

    private static async Task<HttpResponseMessage> SendInstallAsync(
        HttpClient client,
        ProductionPackageInstallationScenario scenario,
        Guid kioskId,
        string idempotencyKey)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/management/organizations/{scenario.OrganizationId:D}/production-package-installations")
        {
            Content = JsonContent.Create(new
            {
                scenario.PackageId,
                PackageVersionId = scenario.PackageVersionId,
                scenario.StoreId,
                KioskId = kioskId,
                ProductSourceKeys = new[] { scenario.ProductSourceKey }
            })
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await client.SendAsync(request);
    }

    private static async Task<Guid> MaterializedTargetIdAsync(
        global::Infrastructure.Data.IceBotDbContext dbContext,
        Guid installationId,
        ProductionPackageResourceKind resourceKind)
    {
        var targetKey = await dbContext.ProductionPackageMaterializations.AsNoTracking()
            .Where(item => item.InstallationId == installationId && item.ResourceKind == resourceKind)
            .Select(item => item.TargetKey)
            .SingleAsync();
        return Guid.Parse(targetKey);
    }

    private static string InstallationPath(Guid organizationId, Guid installationId) =>
        $"/api/v1/management/organizations/{organizationId:D}/production-package-installations/{installationId:D}";

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }
}
