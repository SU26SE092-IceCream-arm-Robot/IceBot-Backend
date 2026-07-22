using Domain.Common;
using Domain.ProductionPackages;

namespace IceBot.UnitTests.ProductionPackages;

public sealed class ProductionPackageInstallationLifecycleTests
{
    [Fact]
    public void ValidLifecycle_ReachesInstalledAndRejectsFurtherMutation()
    {
        var installation = Start();
        installation.MarkMaterializing();
        installation.Complete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(ProductionPackageInstallationStatus.Installed, installation.Status);
        Assert.Throws<DomainRuleException>(() => installation.MarkMaterializing());
        Assert.Throws<DomainRuleException>(() =>
            installation.Fail("LATE_FAILURE", "Late failure.", DateTimeOffset.UtcNow));
        Assert.Throws<DomainRuleException>(() => installation.Restart(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void FailedLifecycle_CanRestartWithoutFailureResidue()
    {
        var installation = Start();
        installation.MarkMaterializing();
        installation.Fail("TRANSIENT", "Transient failure.", DateTimeOffset.UtcNow);

        installation.Restart(DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.Equal(ProductionPackageInstallationStatus.Pending, installation.Status);
        Assert.Null(installation.FailureCode);
        Assert.Null(installation.FailureMessage);
        Assert.Null(installation.CompletedAt);
    }

    [Fact]
    public void PendingInstallation_CannotCompleteBeforeMaterialization()
    {
        var installation = Start();

        Assert.Throws<DomainRuleException>(() =>
            installation.Complete(Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    private static ProductionPackageInstallation Start() =>
        ProductionPackageInstallation.Start(
            Guid.NewGuid(), null, null, Guid.NewGuid(), new string('a', 64), new string('b', 64),
            $"lifecycle-{Guid.NewGuid():N}", ["ICE_CREAM"], DateTimeOffset.UtcNow);
}
