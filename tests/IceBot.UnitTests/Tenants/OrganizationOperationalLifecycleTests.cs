using Application.Identity.Tokens.Claims;
using Application.Tenants.Abstractions;
using Application.Tenants.Organizations.Commands;
using Application.Tenants.Organizations.Requests;
using Domain.Common;
using Domain.Common.Enums;
using Domain.Tenants.Entities;
using NSubstitute;

namespace IceBot.UnitTests.Tenants;

public sealed class OrganizationOperationalLifecycleTests
{
    [Fact]
    public void ActiveOrganization_CanSuspendThenResume()
    {
        var organization = CreateOrganization();
        var actorId = Guid.NewGuid();

        var suspension = organization.Suspend(actorId, "TermsViolation", "Policy review", 0, "suspend-1", DateTimeOffset.UtcNow);

        Assert.Equal(EntityStatus.Suspended, organization.Status);
        Assert.Equal(1, organization.StatusRevision);
        Assert.Equal(EntityStatus.Active, suspension.FromStatus);
        Assert.Equal(EntityStatus.Suspended, suspension.ToStatus);

        organization.Resume(actorId, "Review completed", 1, "resume-1", DateTimeOffset.UtcNow);

        Assert.Equal(EntityStatus.Active, organization.Status);
        Assert.Equal(2, organization.StatusRevision);
    }

    [Fact]
    public void InactiveOrganization_RequiresReadinessConfirmationToReactivate()
    {
        var organization = CreateOrganization();
        var actorId = Guid.NewGuid();
        organization.Deactivate(actorId, "Service ended", 0, "deactivate-1", DateTimeOffset.UtcNow);

        Assert.Throws<DomainRuleException>(() => organization.Reactivate(
            actorId, "Attempt reopen", 1, "reactivate-1", DateTimeOffset.UtcNow, readinessConfirmed: false));

        organization.Reactivate(actorId, "Operational review passed", 1, "reactivate-2", DateTimeOffset.UtcNow, readinessConfirmed: true);

        Assert.Equal(EntityStatus.Active, organization.Status);
        Assert.NotNull(organization.ReactivatedAt);
    }

    [Fact]
    public async Task ResumeInactiveOrganization_IsRejectedByLifecycleHandler()
    {
        var organization = CreateOrganization();
        var actorId = Guid.NewGuid();
        organization.Deactivate(actorId, "Service ended", 0, null, DateTimeOffset.UtcNow);
        var store = CreateStore(organization);
        var handler = new OrganizationLifecycleTransitionCommandHandler(store);

        var result = await handler.HandleAsync(new OrganizationLifecycleTransitionCommand
        {
            UserContext = new CurrentUserContext { AccountId = actorId, IsSystemAdmin = true },
            OrganizationId = organization.Id,
            Action = OrganizationLifecycleAction.Resume,
            Request = new OrganizationLifecycleTransitionRequest
            {
                Reason = "Incorrect operation",
                ExpectedRevision = organization.StatusRevision
            }
        });

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(EntityStatus.Inactive, organization.Status);
        await store.DidNotReceive().AddStatusTransitionAsync(Arg.Any<OrganizationStatusTransition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Suspend_PersistsTransitionWithActorAndReason()
    {
        var organization = CreateOrganization();
        var actorId = Guid.NewGuid();
        var store = CreateStore(organization);
        var handler = new OrganizationLifecycleTransitionCommandHandler(store);

        var result = await handler.HandleAsync(new OrganizationLifecycleTransitionCommand
        {
            UserContext = new CurrentUserContext { AccountId = actorId, IsSystemAdmin = true },
            OrganizationId = organization.Id,
            Action = OrganizationLifecycleAction.Suspend,
            Request = new OrganizationLifecycleTransitionRequest
            {
                ReasonCode = "TermsViolation",
                Reason = "Repeated policy breach",
                ExpectedRevision = 0,
                IdempotencyKey = "suspend-request-1"
            }
        });

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(EntityStatus.Suspended, organization.Status);
        await store.Received(1).AddStatusTransitionAsync(
            Arg.Is<OrganizationStatusTransition>(transition =>
                transition.FromStatus == EntityStatus.Active &&
                transition.ToStatus == EntityStatus.Suspended &&
                transition.ChangedByAccountId == actorId &&
                transition.ReasonCode == "TermsViolation"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RepeatedLifecycleRequestWithSameIdempotencyKey_ReturnsRecordedTransition()
    {
        var organization = CreateOrganization();
        var transition = new OrganizationStatusTransition
        {
            OrganizationId = organization.Id,
            FromStatus = EntityStatus.Active,
            ToStatus = EntityStatus.Suspended,
            ReasonCode = "TermsViolation",
            Reason = "Repeated policy breach",
            OrganizationStatusRevision = 1
        };
        var store = CreateStore(organization);
        store.GetStatusTransitionByIdempotencyKeyAsync(organization.Id, "suspend-request-1", Arg.Any<CancellationToken>())
            .Returns(transition);
        var handler = new OrganizationLifecycleTransitionCommandHandler(store);

        var result = await handler.HandleAsync(new OrganizationLifecycleTransitionCommand
        {
            UserContext = new CurrentUserContext { AccountId = Guid.NewGuid(), IsSystemAdmin = true },
            OrganizationId = organization.Id,
            Action = OrganizationLifecycleAction.Suspend,
            Request = new OrganizationLifecycleTransitionRequest
            {
                ReasonCode = "TermsViolation",
                Reason = "Repeated policy breach",
                ExpectedRevision = 0,
                IdempotencyKey = "suspend-request-1"
            }
        });

        Assert.True(result.Succeeded, result.Message);
        await store.DidNotReceive().AddStatusTransitionAsync(Arg.Any<OrganizationStatusTransition>(), Arg.Any<CancellationToken>());
        await store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static Organization CreateOrganization() => new()
    {
        Id = Guid.NewGuid(),
        Code = "ORG-TEST",
        Name = "Organization Test",
        Status = EntityStatus.Active
    };

    private static IOrganizationStore CreateStore(Organization organization)
    {
        var store = Substitute.For<IOrganizationStore>();
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<Task<Application.Shared.Wrappers.ApiResult<Application.Tenants.Organizations.Results.OrganizationResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<Task<Application.Shared.Wrappers.ApiResult<Application.Tenants.Organizations.Results.OrganizationResult>>>>(0)());
        store.GetByIdAsync(organization.Id, false, Arg.Any<CancellationToken>()).Returns(organization);
        store.GetByIdAsync(organization.Id, true, Arg.Any<CancellationToken>()).Returns(organization);
        store.GetStatusTransitionByIdempotencyKeyAsync(organization.Id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((OrganizationStatusTransition?)null);
        return store;
    }
}
