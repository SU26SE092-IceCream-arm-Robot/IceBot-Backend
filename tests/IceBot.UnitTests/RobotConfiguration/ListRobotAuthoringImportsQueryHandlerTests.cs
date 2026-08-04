using Application.Identity.Tokens.Claims;
using Application.RobotConfiguration.AuthoringImports;
using Application.RobotConfiguration.AuthoringImports.Queries;
using Domain.RobotConfiguration.AuthoringImports;
using NSubstitute;

namespace IceBot.UnitTests.RobotConfiguration;

public sealed class ListRobotAuthoringImportsQueryHandlerTests
{
    [Fact]
    public async Task RejectsUnknownStatusBeforeQueryingStore()
    {
        var organizationId = Guid.NewGuid();
        var store = Substitute.For<IRobotAuthoringImportStore>();
        var handler = new ListRobotAuthoringImportsQueryHandler(store);

        var result = await handler.HandleAsync(new ListRobotAuthoringImportsQuery(
            OrganizationAdmin(organizationId), organizationId, "Applied", null, null, null, null));

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        await store.DidNotReceive().CountImportsAsync(Arg.Any<RobotAuthoringImportListCriteria>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectsCrossOrganizationQueryBeforeQueryingStore()
    {
        var organizationId = Guid.NewGuid();
        var store = Substitute.For<IRobotAuthoringImportStore>();
        var handler = new ListRobotAuthoringImportsQueryHandler(store);

        var result = await handler.HandleAsync(new ListRobotAuthoringImportsQuery(
            OrganizationAdmin(Guid.NewGuid()), organizationId, null, null, null, null, null));

        Assert.False(result.Succeeded);
        Assert.Equal(403, result.StatusCode);
        await store.DidNotReceive().CountImportsAsync(Arg.Any<RobotAuthoringImportListCriteria>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsSafeMaterializedSummaryUsingPublicLifecycleStatus()
    {
        var organizationId = Guid.NewGuid();
        var importId = Guid.NewGuid();
        var createdFrom = DateTimeOffset.UtcNow.AddDays(-7);
        var createdTo = DateTimeOffset.UtcNow;
        var store = Substitute.For<IRobotAuthoringImportStore>();
        store.CountImportsAsync(Arg.Any<RobotAuthoringImportListCriteria>(), Arg.Any<CancellationToken>()).Returns(1);
        store.ListImportsAsync(Arg.Any<RobotAuthoringImportListCriteria>(), Arg.Any<CancellationToken>()).Returns(
        [
            new RobotAuthoringImportListRow(
                importId,
                organizationId,
                null,
                null,
                null,
                RobotAuthoringImportStatus.Applied,
                "MAKE_ICE_CREAM",
                "Make ice cream",
                "FAIRINO_LUA_V1",
                "FR5",
                "{\"canMaterialize\":true,\"errors\":[],\"warnings\":[],\"existingArtifactCount\":0,\"newArtifactCount\":1,\"existingContractCount\":0,\"newContractCount\":1}",
                2,
                Guid.NewGuid(),
                null,
                null,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                null,
                null,
                null,
                "Org Admin")
        ]);
        var handler = new ListRobotAuthoringImportsQueryHandler(store);

        var result = await handler.HandleAsync(new ListRobotAuthoringImportsQuery(
            OrganizationAdmin(organizationId), organizationId, "materialized", null, null, null, "ice", 0, 500,
            createdFrom, createdTo));

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Pagination.Page);
        Assert.Equal(100, result.Pagination.PageSize);
        var item = Assert.Single(result.Data!);
        Assert.Equal(importId, item.Id);
        Assert.Equal("Materialized", item.Status);
        Assert.Equal(2, item.ItemCount);
        Assert.Equal("Org Admin", item.CreatedByDisplayName);
        Assert.Contains("PreviewSemanticComposition", item.NextActions);
        Assert.DoesNotContain("PublishImportResources", item.NextActions);
        Assert.NotNull(item.Validation);
        Assert.True(item.Validation!.CanMaterialize);
        await store.Received(1).CountImportsAsync(
            Arg.Is<RobotAuthoringImportListCriteria>(criteria =>
                criteria.Status == RobotAuthoringImportPublicStatus.Materialized &&
                criteria.Search == "ice" &&
                criteria.CreatedFrom == createdFrom &&
                criteria.CreatedTo == createdTo),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectsInvertedCreatedRangeBeforeQueryingStore()
    {
        var organizationId = Guid.NewGuid();
        var store = Substitute.For<IRobotAuthoringImportStore>();
        var handler = new ListRobotAuthoringImportsQueryHandler(store);

        var result = await handler.HandleAsync(new ListRobotAuthoringImportsQuery(
            OrganizationAdmin(organizationId),
            organizationId,
            null,
            null,
            null,
            null,
            null,
            CreatedFrom: DateTimeOffset.UtcNow,
            CreatedTo: DateTimeOffset.UtcNow.AddMinutes(-1)));

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        await store.DidNotReceive().CountImportsAsync(Arg.Any<RobotAuthoringImportListCriteria>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void MalformedValidationJsonProducesBlockedSummaryInsteadOfBreakingInboxRow()
    {
        var row = new RobotAuthoringImportListRow(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            null,
            RobotAuthoringImportStatus.Validated,
            "MAKE_ICE_CREAM",
            "Make ice cream",
            "FAIRINO_LUA_V1",
            "FR5",
            "{not-json}",
            1,
            null,
            null,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            null);

        var result = RobotAuthoringImportListItemResult.From(row);

        Assert.NotNull(result.Validation);
        Assert.False(result.Validation!.CanMaterialize);
        Assert.Equal(1, result.Validation.ErrorCount);
        Assert.Contains("ResolveArtifactRevisionConflict", result.NextActions);
    }

    private static CurrentUserContext OrganizationAdmin(Guid organizationId) => new()
    {
        AccountId = Guid.NewGuid(),
        RoleScopes = [new UserRoleScope("OrgAdmin", organizationId, null, null)]
    };
}
