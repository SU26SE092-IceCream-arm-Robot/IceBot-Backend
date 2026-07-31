using Application.ProductionConfiguration.Releases.Abstractions;
using Application.ProductionConfiguration.Routes.Abstractions;
using Application.ProductionConfiguration.Routes.Commands;
using Application.ProductionConfiguration.Routes.Contracts;
using Application.ProductionConfiguration.Routes.Support;
using Application.ProductionConfiguration.Releases.Support;
using Application.Shared.Ownership;
using Application.Shared.Concurrency;
using Domain.ProductionConfiguration.Entities;
using IceBot.UnitTests.TestSupport;
using NSubstitute;

namespace IceBot.UnitTests.ProductionConfiguration;

public sealed class ConfigurationReleaseRouteAuthoringTests
{
    [Fact]
    public void RevisionToken_ChangesWhenTheMutableRouteGraphChanges()
    {
        var release = ConfigurationRelease.CreateDraft(Guid.NewGuid(), 1);
        var before = ConfigurationReleaseRevisionToken.Create(release);

        release.AddRoute(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ICE_CREAM",
            0,
            ExecutionRouteCapabilityRequirementContractCodec.ToStorageJson(
                [new ExecutionRouteCapabilityRequirementContract("ROBOT_ARM", true)]),
            []);

        var after = ConfigurationReleaseRevisionToken.Create(release);

        Assert.NotEqual(before, after);
        Assert.Equal(64, after.Length);
    }

    [Fact]
    public void TypedCapabilityRequirements_AreCanonicalizedWithoutVersionMetadata()
    {
        var json = ExecutionRouteCapabilityRequirementContractCodec.ToStorageJson(
        [
            new ExecutionRouteCapabilityRequirementContract(" robot_arm ", true)
        ]);

        Assert.NotNull(json);
        Assert.Contains("\"code\":\"ROBOT_ARM\"", json);
        Assert.DoesNotContain("minVersion", json);
        Assert.Null(ExecutionRouteRequiredCapabilitiesContract.Validate(json, ["ROBOT_ARM"]));
    }

    [Fact]
    public async Task ReplaceRoutes_RejectsStaleRevisionBeforeLoadingRouteResources()
    {
        var release = ConfigurationRelease.CreateDraft(Guid.NewGuid(), 1);
        var releaseStore = Substitute.For<IConfigurationReleaseStore>();
        var routeStore = Substitute.For<IConfigurationRouteStore>();
        var ownership = Substitute.For<ITechnicalResourceMutationPolicy>();
        releaseStore.GetReleaseForEditAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);
        var handler = new ReplaceConfigurationReleaseRoutesCommandHandler(
            releaseStore,
            routeStore,
            ownership,
            InlineTechnicalResourceMutationCoordinator.Instance);

        var result = await handler.HandleAsync(new ReplaceConfigurationReleaseRoutesCommand
        {
            UserContext = TestData.SystemAdmin(),
            OrganizationId = release.OrganizationId,
            ReleaseId = release.Id,
            ExpectedRevision = new string('0', 64),
            Routes =
            [
                new ConfigurationReleaseRouteInput(
                    Guid.NewGuid(),
                    "DEFAULT",
                    0,
                    [new ExecutionRouteCapabilityRequirementContract("ROBOT_ARM", true)],
                    [],
                    [new ConfigurationReleaseRobotBindingInput(Guid.NewGuid(), 1, "ROBOT_ARM")])
            ]
        });

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        await routeStore.DidNotReceive().ListRecipesByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
    }
}
