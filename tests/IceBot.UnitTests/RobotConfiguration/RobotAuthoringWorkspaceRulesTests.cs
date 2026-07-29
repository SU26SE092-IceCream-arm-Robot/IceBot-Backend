using Application.ProductionConfiguration.Deployments.Services;
using Application.RobotConfiguration.AuthoringImports;
using Application.RobotConfiguration.AuthoringImports.Workspace;
using Domain.ProductionConfiguration.Enums;

namespace IceBot.UnitTests.RobotConfiguration;

public sealed class RobotAuthoringWorkspaceRulesTests
{
    [Fact]
    public void PublishedLinkedRelease_DoesNotKeepDraftPublicationActions()
    {
        var import = ImportResult(
            linkedReleaseId: Guid.NewGuid(),
            kioskId: null,
            nextActions: ["ReviewConfigurationReleaseDraft", "PublishConfigurationRelease"]);

        var actions = RobotAuthoringWorkspaceHandler.BuildActions(
            import,
            ConfigurationReleaseStatus.Published,
            preview: null,
            blockers: [new("ImportHasNoKioskScope", "Select a kiosk.")]);

        Assert.DoesNotContain(actions, action => action.Code == "ReviewConfigurationReleaseDraft");
        Assert.DoesNotContain(actions, action => action.Code == "PublishConfigurationRelease");
        Assert.Contains(actions, action => action.Code == "SelectDeploymentKiosk" && !action.IsBlocked);
    }

    [Fact]
    public void FailedPreview_ProducesBlockedResolutionAction()
    {
        var import = ImportResult(Guid.NewGuid(), Guid.NewGuid(), []);

        var actions = RobotAuthoringWorkspaceHandler.BuildActions(
            import,
            ConfigurationReleaseStatus.Published,
            preview: null,
            blockers: [new("DeploymentPreviewUnavailable", "Access denied.", 403)]);

        Assert.Contains(actions, action =>
            action.Code == "ResolveDeploymentBlockers" &&
            action.IsBlocked &&
            action.BlockerCode == "DeploymentPreviewUnavailable");
    }

    private static RobotAuthoringImportResult ImportResult(
        Guid? linkedReleaseId,
        Guid? kioskId,
        IReadOnlyCollection<string> nextActions) => new(
        Guid.NewGuid(), Guid.NewGuid(), null, kioskId, null, Guid.NewGuid(), new string('a', 64), 1,
        "Materialized", "MAKE_PRODUCT", "Make product", "FAIRINO_LUA_V1", "FR5", null,
        Guid.NewGuid(), linkedReleaseId, Guid.NewGuid(), [], null, [], nextActions,
        DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
        null, null);
}
