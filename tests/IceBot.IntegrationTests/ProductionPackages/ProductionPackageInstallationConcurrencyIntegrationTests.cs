using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Common.Enums;
using Domain.ProductionPackages;
using Domain.Tenants.Entities;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.ProductionPackages;
using Microsoft.EntityFrameworkCore;

namespace IceBot.IntegrationTests.ProductionPackages;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class ProductionPackageInstallationConcurrencyIntegrationTests
{
    private readonly IntegrationTestFixture _fixture;

    public ProductionPackageInstallationConcurrencyIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [IntegrationFact]
    public async Task ConcurrentHttpInstalls_WithSameIdempotencyKey_CreateOneMaterializedGraph()
    {
        var actorId = Guid.NewGuid();
        var storage = _fixture.CreateObjectStorage(autoCreateBucket: true);
        var scenario = await ProductionPackageInstallationScenarioSeed.SeedAsync(
            _fixture, storage, actorId);
        await using var factory = new PackageApiWebApplicationFactory(
            _fixture, storage, actorId);
        using var client = factory.CreateAuthenticatedClient();
        var idempotencyKey = $"concurrent-install-{Guid.NewGuid():N}";

        var responses = await Task.WhenAll(
            SendInstallAsync(client, scenario, idempotencyKey),
            SendInstallAsync(client, scenario, idempotencyKey));
        try
        {
            Assert.Equal(
                [HttpStatusCode.OK, HttpStatusCode.Created],
                responses.Select(response => response.StatusCode).Order().ToArray());
            var installationIds = new List<Guid>();
            foreach (var response in responses)
            {
                using var document = await ReadJsonAsync(response);
                Assert.True(document.RootElement.GetProperty("succeeded").GetBoolean(),
                    document.RootElement.ToString());
                installationIds.Add(document.RootElement.GetProperty("data").GetProperty("id").GetGuid());
            }
            Assert.Single(installationIds.Distinct());

            var installationId = installationIds[0];
            await using var assertionContext = _fixture.CreateDbContext();
            Assert.Equal(1, await assertionContext.ProductionPackageInstallations.CountAsync(
                item => item.OrganizationId == scenario.OrganizationId &&
                    item.IdempotencyKey == idempotencyKey));
            var installation = await assertionContext.ProductionPackageInstallations.AsNoTracking()
                .SingleAsync(item => item.Id == installationId);
            Assert.Equal(ProductionPackageInstallationStatus.Installed, installation.Status);
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
        finally
        {
            foreach (var response in responses)
                response.Dispose();
        }
    }

    [IntegrationFact]
    public async Task ConcurrentFailedRetries_OnlyOneTransitionsInstallationToPending()
    {
        Guid organizationId;
        Guid installationId;
        await using (var seedContext = _fixture.CreateDbContext())
        {
            var organization = new Organization
            {
                Code = $"ORG-{Guid.NewGuid():N}",
                Name = "Package retry organization",
                Status = EntityStatus.Active
            };
            var package = ProductionPackage.Create($"PACKAGE-{Guid.NewGuid():N}", "Retry package");
            var version = ProductionPackageVersion.CreateDraft(package.Id, 1);
            var installation = ProductionPackageInstallation.Start(
                organization.Id,
                null,
                null,
                version.Id,
                new string('a', 64),
                new string('b', 64),
                $"retry-{Guid.NewGuid():N}",
                ["PRODUCT"],
                DateTimeOffset.UtcNow.AddMinutes(-1));
            installation.Fail("SEED_FAILURE", "Seeded retryable failure.", DateTimeOffset.UtcNow);

            seedContext.AddRange(organization, package, version, installation);
            await seedContext.SaveChangesAsync();
            organizationId = organization.Id;
            installationId = installation.Id;
        }

        await using var firstContext = _fixture.CreateDbContext();
        await using var secondContext = _fixture.CreateDbContext();
        var firstStore = new ProductionPackageInstallationStore(firstContext);
        var secondStore = new ProductionPackageInstallationStore(secondContext);
        var now = DateTimeOffset.UtcNow;

        var outcomes = await Task.WhenAll(
            firstStore.TryRestartFailedAsync(organizationId, installationId, now, CancellationToken.None),
            secondStore.TryRestartFailedAsync(organizationId, installationId, now, CancellationToken.None));

        Assert.Single(outcomes, outcome => outcome);
        await using var assertionContext = _fixture.CreateDbContext();
        var persisted = await assertionContext.ProductionPackageInstallations.AsNoTracking()
            .SingleAsync(x => x.Id == installationId);
        Assert.Equal(ProductionPackageInstallationStatus.Pending, persisted.Status);
        Assert.Null(persisted.FailureCode);
        Assert.Null(persisted.FailureMessage);
        Assert.Null(persisted.CompletedAt);
    }

    private static async Task<HttpResponseMessage> SendInstallAsync(
        HttpClient client,
        ProductionPackageInstallationScenario scenario,
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
                scenario.KioskId,
                ProductSourceKeys = new[] { scenario.ProductSourceKey }
            })
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await client.SendAsync(request);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }
}
