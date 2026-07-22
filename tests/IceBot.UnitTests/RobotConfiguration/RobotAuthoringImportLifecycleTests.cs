using Domain.Common;
using Domain.RobotConfiguration.AuthoringImports;

namespace IceBot.UnitTests.RobotConfiguration;

public sealed class RobotAuthoringImportLifecycleTests
{
    [Fact]
    public void ReleaseLinkRequiresPublishedImportResources()
    {
        var importSession = CreateImport();

        var exception = Assert.Throws<DomainRuleException>(() =>
            importSession.LinkConfigurationRelease(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid()));

        Assert.Contains("published resources", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SameReleaseLinkIsIdempotentButDifferentReleaseIsRejected()
    {
        var importSession = CreatePublishedImport();
        var releaseId = Guid.NewGuid();
        var firstLinkedAt = DateTimeOffset.UtcNow;

        importSession.LinkConfigurationRelease(releaseId, firstLinkedAt, Guid.NewGuid());
        importSession.LinkConfigurationRelease(releaseId, firstLinkedAt.AddMinutes(1), Guid.NewGuid());

        Assert.Equal(releaseId, importSession.LinkedConfigurationReleaseId);
        Assert.Equal(firstLinkedAt, importSession.ReleaseLinkedAt);
        Assert.Throws<DomainRuleException>(() =>
            importSession.LinkConfigurationRelease(Guid.NewGuid(), firstLinkedAt.AddMinutes(2), Guid.NewGuid()));
    }

    private static RobotAuthoringImport CreatePublishedImport()
    {
        var importSession = CreateImport();
        var now = DateTimeOffset.UtcNow;
        importSession.MarkValidated("{}", now, Guid.NewGuid());
        importSession.MarkApplied(Guid.NewGuid(), now, Guid.NewGuid());
        importSession.MarkPublished(now, Guid.NewGuid());
        return importSession;
    }

    private static RobotAuthoringImport CreateImport() => RobotAuthoringImport.Create(
        Guid.NewGuid(),
        null,
        null,
        null,
        Guid.NewGuid(),
        new string('a', 64),
        Guid.NewGuid().ToString("N"),
        1,
        "MAKE_ICE_CREAM",
        "Make ice cream",
        "FAIRINO_LUA_V1",
        "FR5",
        "robot-authoring-imports/staged.zip",
        Guid.NewGuid());
}
