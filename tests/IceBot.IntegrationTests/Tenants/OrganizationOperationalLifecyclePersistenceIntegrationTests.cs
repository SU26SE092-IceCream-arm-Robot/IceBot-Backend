using Application.Identity.Tokens.Claims;
using Application.Tenants.Organizations.Commands;
using Application.Tenants.Organizations.Requests;
using Domain.Common.Enums;
using Domain.Tenants.Entities;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.Tenants.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IceBot.IntegrationTests.Tenants;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class OrganizationOperationalLifecyclePersistenceIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task ConcurrentSameIdempotencyKey_PersistsOneSuspensionTransition()
    {
        var organizationId = await SeedOrganizationAsync();
        var actorId = Guid.NewGuid();
        var request = new OrganizationLifecycleTransitionRequest
        {
            ReasonCode = "AdministrativeHold",
            Reason = "Integration-test suspension",
            ExpectedRevision = 0,
            IdempotencyKey = $"suspend-{Guid.NewGuid():N}"
        };

        await using var firstContext = fixture.CreateDbContext();
        await using var secondContext = fixture.CreateDbContext();
        var first = CreateHandler(firstContext);
        var second = CreateHandler(secondContext);

        var results = await Task.WhenAll(
            first.HandleAsync(Command(organizationId, actorId, request)),
            second.HandleAsync(Command(organizationId, actorId, request)));

        Assert.All(results, result => Assert.True(result.Succeeded, result.Message));

        await using var assertion = fixture.CreateDbContext();
        var organization = await assertion.Organizations.SingleAsync(item => item.Id == organizationId);
        Assert.Equal(EntityStatus.Suspended, organization.Status);
        Assert.Equal(1, organization.StatusRevision);
        Assert.Equal(1, await assertion.OrganizationStatusTransitions.CountAsync(item =>
            item.OrganizationId == organizationId && item.RequestIdempotencyKey == request.IdempotencyKey));
    }

    [IntegrationFact]
    public async Task StaleExpectedRevision_CannotAppendSecondLifecycleTransition()
    {
        var organizationId = await SeedOrganizationAsync();
        var actorId = Guid.NewGuid();

        await using (var firstContext = fixture.CreateDbContext())
        {
            var first = await CreateHandler(firstContext).HandleAsync(Command(
                organizationId,
                actorId,
                new OrganizationLifecycleTransitionRequest
                {
                    ReasonCode = "AdministrativeHold",
                    Reason = "First decision",
                    ExpectedRevision = 0,
                    IdempotencyKey = $"first-{Guid.NewGuid():N}"
                }));
            Assert.True(first.Succeeded, first.Message);
        }

        await using (var staleContext = fixture.CreateDbContext())
        {
            var stale = await CreateHandler(staleContext).HandleAsync(Command(
                organizationId,
                actorId,
                new OrganizationLifecycleTransitionRequest
                {
                    Reason = "Stale deactivation",
                    ExpectedRevision = 0,
                    IdempotencyKey = $"stale-{Guid.NewGuid():N}"
                },
                OrganizationLifecycleAction.Deactivate));

            Assert.False(stale.Succeeded);
            Assert.Equal(409, stale.StatusCode);
        }

        await using var assertion = fixture.CreateDbContext();
        Assert.Equal(1, await assertion.OrganizationStatusTransitions.CountAsync(item => item.OrganizationId == organizationId));
    }

    private async Task<Guid> SeedOrganizationAsync()
    {
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Code = $"LIFECYCLE-{Guid.NewGuid():N}",
            Name = "Organization lifecycle integration test",
            Status = EntityStatus.Active
        };

        await using var context = fixture.CreateDbContext();
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();
        return organization.Id;
    }

    private static OrganizationLifecycleTransitionCommandHandler CreateHandler(global::Infrastructure.Data.IceBotDbContext context) =>
        new(new OrganizationStore(context));

    private static OrganizationLifecycleTransitionCommand Command(
        Guid organizationId,
        Guid actorId,
        OrganizationLifecycleTransitionRequest request,
        OrganizationLifecycleAction action = OrganizationLifecycleAction.Suspend) =>
        new()
        {
            OrganizationId = organizationId,
            Action = action,
            Request = request,
            UserContext = new CurrentUserContext { AccountId = actorId, IsSystemAdmin = true }
        };
}
