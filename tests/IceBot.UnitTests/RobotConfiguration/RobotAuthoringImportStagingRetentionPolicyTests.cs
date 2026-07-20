using Application.RobotConfiguration.AuthoringImports.Rules;
using Domain.RobotConfiguration.AuthoringImports;

namespace IceBot.UnitTests.RobotConfiguration;

public sealed class RobotAuthoringImportStagingRetentionPolicyTests
{
    private static readonly DateTimeOffset Threshold = new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(RobotAuthoringImportStatus.Uploaded)]
    [InlineData(RobotAuthoringImportStatus.Validated)]
    [InlineData(RobotAuthoringImportStatus.Failed)]
    public void ActiveRetryableImport_IsRetainedRegardlessOfAge(RobotAuthoringImportStatus status)
    {
        Assert.True(RobotAuthoringImportStagingRetentionPolicy.ShouldRetain(
            status, null, Threshold));
    }

    [Fact]
    public void AppliedImport_IsRetainedOnlyInsideRetentionWindow()
    {
        Assert.True(RobotAuthoringImportStagingRetentionPolicy.ShouldRetain(
            RobotAuthoringImportStatus.Applied, Threshold, Threshold));
        Assert.False(RobotAuthoringImportStagingRetentionPolicy.ShouldRetain(
            RobotAuthoringImportStatus.Applied, Threshold.AddTicks(-1), Threshold));
    }

    [Fact]
    public void DiscardedImport_IsNeverRetainedByLifecycleReference()
    {
        Assert.False(RobotAuthoringImportStagingRetentionPolicy.ShouldRetain(
            RobotAuthoringImportStatus.Discarded, Threshold.AddDays(1), Threshold));
    }
}
