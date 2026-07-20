using Domain.Common.Enums;
using Domain.Tenants.Entities;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.Tenants.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IceBot.IntegrationTests.Tenants;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class FranchiseOnboardingPersistenceIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task ConcurrentInsert_WithSameOrganizationAndIdempotencyKey_PersistsOneWorkflow()
    {
        var organizationId = Guid.NewGuid();
        await using (var seed = fixture.CreateDbContext())
        {
            seed.Organizations.Add(new Organization
            {
                Id = organizationId,
                Code = $"ONBOARD-{Guid.NewGuid():N}",
                Name = "Onboarding integration organization",
                Status = EntityStatus.Active
            });
            await seed.SaveChangesAsync();
        }

        var key = $"onboarding-{Guid.NewGuid():N}";
        var actorId = Guid.NewGuid();
        await using var firstContext = fixture.CreateDbContext();
        await using var secondContext = fixture.CreateDbContext();
        var firstStore = new FranchiseOnboardingStore(firstContext);
        var secondStore = new FranchiseOnboardingStore(secondContext);

        var inserted = await Task.WhenAll(
            firstStore.TryInsertAsync(Create(organizationId, actorId, key)),
            secondStore.TryInsertAsync(Create(organizationId, actorId, key)));

        Assert.Single(inserted, value => value);
        await using var assertion = fixture.CreateDbContext();
        Assert.Equal(1, await assertion.FranchiseOnboardings.CountAsync(x =>
            x.OrganizationId == organizationId && x.IdempotencyKey == key));
    }

    [IntegrationFact]
    public async Task List_IsOrganizationScopedAndStatusFiltered()
    {
        var firstOrganizationId = Guid.NewGuid();
        var secondOrganizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        await using (var seed = fixture.CreateDbContext())
        {
            seed.Organizations.AddRange(
                new Organization { Id = firstOrganizationId, Code = $"ONBOARD-{Guid.NewGuid():N}", Name = "First", Status = EntityStatus.Active },
                new Organization { Id = secondOrganizationId, Code = $"ONBOARD-{Guid.NewGuid():N}", Name = "Second", Status = EntityStatus.Active });
            seed.FranchiseOnboardings.AddRange(
                Create(firstOrganizationId, actorId, $"first-{Guid.NewGuid():N}"),
                Create(secondOrganizationId, actorId, $"second-{Guid.NewGuid():N}"));
            await seed.SaveChangesAsync();
        }

        await using var context = fixture.CreateDbContext();
        var store = new FranchiseOnboardingStore(context);

        var rows = await store.ListAsync(
            firstOrganizationId, Domain.Tenants.Enums.FranchiseOnboardingStatus.Pending, 1, 20);
        var count = await store.CountAsync(
            firstOrganizationId, Domain.Tenants.Enums.FranchiseOnboardingStatus.Pending);

        Assert.Single(rows);
        Assert.Equal(firstOrganizationId, rows[0].OrganizationId);
        Assert.Equal(1, count);
    }

    private static FranchiseOnboarding Create(Guid organizationId, Guid actorId, string key) =>
        FranchiseOnboarding.Start(
            organizationId,
            key,
            new string('A', 64),
            "{}",
            actorId,
            DateTimeOffset.UtcNow);
}
