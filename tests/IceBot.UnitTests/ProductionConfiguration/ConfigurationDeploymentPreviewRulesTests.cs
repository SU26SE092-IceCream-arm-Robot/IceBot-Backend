using Application.ProductionConfiguration.Deployments.Services;
using Domain.Devices.ExecutionEndpoints;
using Domain.ProductionConfiguration.Entities;
using Application.ProductionConfiguration.Routes.Support;

namespace IceBot.UnitTests.ProductionConfiguration;

public sealed class ConfigurationDeploymentPreviewRulesTests
{
    [Fact]
    public void FullEdge_AlwaysSelectsCompleteRelease()
    {
        var firstProgramId = Guid.NewGuid();
        var secondProgramId = Guid.NewGuid();
        var release = ReleaseWithBindings(firstProgramId, secondProgramId);

        var result = ConfigurationDeploymentPreviewRules.ResolveSelections(
            release,
            KioskExecutionProfile.FullEdge,
            [new DeploymentPreviewSelection(release.ExecutionRoutes.Single().Id, firstProgramId)]);

        Assert.Equal(2, result.Selections.Count);
        Assert.Contains(result.Selections, item => item.RobotProgramId == firstProgramId);
        Assert.Contains(result.Selections, item => item.RobotProgramId == secondProgramId);
        Assert.Contains(result.Blockers, item => item.Code == "SelectionsNotApplicable");
    }

    [Fact]
    public void LowCost_AmbiguousRouteRequiresExplicitSelection()
    {
        var release = ReleaseWithBindings(Guid.NewGuid(), Guid.NewGuid());

        var result = ConfigurationDeploymentPreviewRules.ResolveSelections(
            release, KioskExecutionProfile.LowCostController, []);

        Assert.Empty(result.Selections);
        Assert.Contains(result.Blockers, item => item.Code == "ProgramSelectionRequired");
    }

    [Fact]
    public void RequiredCapabilityContract_PreservesMinimumVersionForEnforcement()
    {
        const string json = """
            {"schemaVersion":1,"requires":[{"code":"CUP_DISPENSER","minVersion":"1.2","required":true}]}
            """;

        var requirements = ExecutionRouteRequiredCapabilitiesContract.ParseValidated(json);

        var requirement = Assert.Single(requirements);
        Assert.Equal("CUP_DISPENSER", requirement.Code);
        Assert.Equal("1.2", requirement.MinVersion);
        Assert.True(requirement.Required);
        Assert.True(ExecutionRouteRequiredCapabilitiesContract.HasUnverifiableRequiredVersion(json));
    }

    [Fact]
    public void RequiredCapabilityContract_MalformedLegacyValueFailsClosed()
    {
        Assert.True(ExecutionRouteRequiredCapabilitiesContract.HasUnverifiableRequiredVersion("{not-json"));
    }

    [Fact]
    public void RequiredCapabilityContract_MissingRequiredPropertiesFailsClosed()
    {
        Assert.True(ExecutionRouteRequiredCapabilitiesContract.HasUnverifiableRequiredVersion("{}"));
        Assert.True(ExecutionRouteRequiredCapabilitiesContract.HasUnverifiableRequiredVersion(
            """{"schemaVersion":1}"""));
    }

    private static ConfigurationRelease ReleaseWithBindings(params Guid[] programIds)
    {
        var release = ConfigurationRelease.CreateDraft(Guid.NewGuid(), 1);
        IReadOnlyCollection<(Guid ProductionProgramBindingId, string ProductionProgramBindingChecksum,
            Guid RobotProgramId, int BindingOrder, IReadOnlyCollection<string> CapabilityCodes)> bindings =
            programIds.Select((programId, index) =>
                (Guid.NewGuid(), new string('a', 64), programId, index + 1,
                    (IReadOnlyCollection<string>)[$"CAPABILITY_{index + 1}"])).ToArray();
        release.ReplaceRoutes(
        [
            (
                Guid.NewGuid(),
                Guid.NewGuid(),
                "DEFAULT",
                0,
                (string?)null,
                (IReadOnlyCollection<string>)[],
                bindings
            )
        ]);
        return release;
    }
}
