using Domain.Devices.ExecutionEndpoints;
using System.Reflection;
using Domain.Common.Enums;
using Domain.Devices.Catalog;
using Domain.Devices.Telemetry;
using Domain.ProductionConfiguration.Entities;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.ProductionConfiguration.Persistence.Deployments;
using Infrastructure.ProductionConfiguration.Persistence.Releases;
using Microsoft.EntityFrameworkCore;

namespace IceBot.IntegrationTests.ProductionConfiguration;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class AllocationConcurrencyIntegrationTests
{
    private readonly IntegrationTestFixture _fixture;

    public AllocationConcurrencyIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [IntegrationFact]
    public async Task ConcurrentReleaseAllocation_AssignsDistinctSequentialNumbers()
    {
        var organizationId = await SeedOrganizationAsync();

        var releases = await Task.WhenAll(
            CreateNextReleaseAsync(organizationId),
            CreateNextReleaseAsync(organizationId));

        Assert.Equal([1L, 2L], releases.Select(release => release.ReleaseNumber).Order().ToArray());
        await using var assertionContext = _fixture.CreateDbContext();
        Assert.Equal(2, await assertionContext.ConfigurationReleases.CountAsync(
            release => release.OrganizationId == organizationId));
    }

    [IntegrationFact]
    public async Task ConcurrentDeploymentAllocation_AssignsDistinctSequentialAttempts()
    {
        var graph = await SeedFullEdgeGraphAsync();

        var attempts = await Task.WhenAll(
            CreateFailedDeploymentAttemptAsync(graph),
            CreateFailedDeploymentAttemptAsync(graph));

        Assert.Equal([1, 2], attempts.Order().ToArray());
        await using var assertionContext = _fixture.CreateDbContext();
        Assert.Equal(2, await assertionContext.KioskConfigurationDeployments.CountAsync(
            deployment => deployment.KioskId == graph.KioskId &&
                deployment.ConfigurationReleaseId == graph.ReleaseId));
    }

    private async Task<ConfigurationRelease> CreateNextReleaseAsync(Guid organizationId)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var store = new ConfigurationReleaseStore(dbContext);
        return await store.CreateNextReleaseAsync(
            organizationId,
            number => ConfigurationRelease.CreateDraft(organizationId, number));
    }

    private async Task<int> CreateFailedDeploymentAttemptAsync(FullEdgeGraph graph)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var store = new ConfigurationDeploymentStore(dbContext);
        return await store.ExecuteDeploymentCreationAsync(
            graph.KioskId,
            async cancellationToken =>
            {
                var attempt = await store.GetNextFullEdgeDeploymentAttemptNoAsync(
                    graph.KioskId,
                    graph.ReleaseId,
                    cancellationToken);
                var deployment = KioskConfigurationDeployment.CreatePending(
                    graph.KioskId,
                    graph.OrganizationId,
                    graph.EndpointId,
                    graph.EdgeRuntimeId,
                    graph.ReleaseId,
                    graph.ReleaseChecksum,
                    attempt,
                    Guid.NewGuid().ToString("N"),
                    DateTimeOffset.UtcNow,
                    null,
                    "validation-checksum",
                    "UnprovenPhysicalBehavior",
                    "[]");
                deployment.MarkCommandExpired(DateTimeOffset.UtcNow);
                await store.AddFullEdgeDeploymentAsync(deployment, cancellationToken);
                await store.SaveChangesAsync(cancellationToken);
                return attempt;
            });
    }

    private async Task<Guid> SeedOrganizationAsync()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var organization = NewOrganization();
        dbContext.Organizations.Add(organization);
        await dbContext.SaveChangesAsync();
        return organization.Id;
    }

    private async Task<FullEdgeGraph> SeedFullEdgeGraphAsync()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var organization = NewOrganization();
        var store = new Store
        {
            OrganizationId = organization.Id,
            Code = $"STORE-{Guid.NewGuid():N}",
            Name = "Integration store",
            Status = EntityStatus.Active
        };
        var kiosk = new Kiosk
        {
            OrganizationId = organization.Id,
            StoreId = store.Id,
            Code = $"KIOSK-{Guid.NewGuid():N}",
            Name = "Integration kiosk",
            Status = KioskStatus.Active
        };
        var edgeRuntimeId = Guid.NewGuid();
        var endpoint = KioskExecutionEndpoint.CreateProvisioning(
            kiosk.Id,
            $"EDGE-{Guid.NewGuid():N}",
            KioskExecutionProfile.FullEdge,
            ExecutionEndpointAuthenticationMode.MutualTls);
        var release = ConfigurationRelease.CreateDraft(organization.Id, 1);
        var checksum = new string('f', 64);
        SetProperty(release, nameof(ConfigurationRelease.ReleaseChecksum), checksum);

        dbContext.Organizations.Add(organization);
        dbContext.Stores.Add(store);
        dbContext.Kiosks.Add(kiosk);
        dbContext.KioskExecutionEndpoints.Add(endpoint);
        dbContext.ConfigurationReleases.Add(release);
        await dbContext.SaveChangesAsync();

        var credential = endpoint.ProvisionCredential($"cert-{Guid.NewGuid():N}", DateTimeOffset.UtcNow);
        endpoint.Activate(edgeRuntimeId, DateTimeOffset.UtcNow);
        dbContext.ExecutionEndpointCredentialBindings.Add(credential);
        await dbContext.SaveChangesAsync();
        return new FullEdgeGraph(organization.Id, kiosk.Id, endpoint.Id, edgeRuntimeId, release.Id, checksum);
    }

    private static Organization NewOrganization() => new()
    {
        Code = $"ORG-{Guid.NewGuid():N}",
        Name = "Integration organization",
        Status = EntityStatus.Active
    };

    private static void SetProperty<T>(object target, string propertyName, T value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Property {propertyName} was not found.");
        property.SetValue(target, value);
    }

    private sealed record FullEdgeGraph(
        Guid OrganizationId,
        Guid KioskId,
        Guid EndpointId,
        Guid EdgeRuntimeId,
        Guid ReleaseId,
        string ReleaseChecksum);
}
