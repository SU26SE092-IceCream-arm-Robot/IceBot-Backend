using Application.ProductionPackages.Installation;
using Domain.Common;
using Domain.ProductionPackages;
using Domain.SalesCatalog.Enums;

namespace IceBot.UnitTests.ProductionPackages;

public sealed class ProductionPackageUpgradeLifecycleTests
{
    [Fact]
    public void Upgrade_RequiresSuccessorEvidenceBeforeReviewAndSupportsRollbackLifecycle()
    {
        var upgrade = CreateUpgrade();
        var sourceProductId = Guid.NewGuid();
        var targetProductId = Guid.NewGuid();
        upgrade.AddCatalogIdentityChange(ProductionPackageUpgradeCatalogIdentityChange.Create(
            "ICE_CREAM", sourceProductId, targetProductId, "ICE_CREAM", "ICE_CREAM_OLD",
            "ICE_CREAM_UPG", "ICE_CREAM", Checksum('a'), Checksum('b')));

        upgrade.MarkReadyForReview(Guid.NewGuid(), DateTimeOffset.UtcNow);
        upgrade.Complete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        upgrade.BeginRollback(Guid.NewGuid(), DateTimeOffset.UtcNow);
        upgrade.CompleteRollback(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(ProductionPackageUpgradeStatus.RolledBack, upgrade.Status);
        Assert.NotNull(upgrade.TargetInstallationId);
        Assert.NotNull(upgrade.CompletedAt);
        Assert.NotNull(upgrade.RollbackRequestedAt);
        Assert.NotNull(upgrade.RolledBackAt);
    }

    [Fact]
    public void Upgrade_RejectsDuplicateMenuAndEndpointEvidence()
    {
        var upgrade = CreateUpgrade();
        var menuId = Guid.NewGuid();
        var menuItemId = Guid.NewGuid();
        var change = ProductionPackageUpgradeMenuChange.Create(
            ProductionPackageUpgradeMenuChangeKind.Rebind, menuId, menuItemId,
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null,
            MenuItemStatus.Active, MenuItemStatus.Active, Checksum('a'), Checksum('b'), []);
        var endpoint = ProductionPackageUpgradeEndpointTarget.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        upgrade.AddMenuChange(change);
        upgrade.AddEndpointTarget(endpoint);

        Assert.Throws<DomainRuleException>(() => upgrade.AddMenuChange(
            ProductionPackageUpgradeMenuChange.Create(
                ProductionPackageUpgradeMenuChangeKind.DeactivateRemoved, Guid.NewGuid(), menuItemId,
                Guid.NewGuid(), null, Guid.NewGuid(), null, null, null,
                MenuItemStatus.Active, MenuItemStatus.Unavailable, Checksum('c'), Checksum('d'), [])));
        Assert.Throws<DomainRuleException>(() => upgrade.AddEndpointTarget(
            ProductionPackageUpgradeEndpointTarget.Create(endpoint.KioskExecutionEndpointId,
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())));
    }

    [Fact]
    public void Installation_UpgradeSuffixParticipatesInIdempotencyAndSurvivesLifecycle()
    {
        var version = CreateVersion();
        var organizationId = Guid.NewGuid();
        var withoutSuffix = ProductionPackageInstallationRequestRules.ComputeRequestChecksum(
            organizationId, null, null, version, ["ICE_CREAM"]);
        var withSuffix = ProductionPackageInstallationRequestRules.ComputeRequestChecksum(
            organizationId, null, null, version, ["ICE_CREAM"], "UPG_123");
        var installation = ProductionPackageInstallation.Start(organizationId, null, null,
            version.Id, Checksum('a'), withSuffix, "upgrade-install", ["ICE_CREAM"],
            DateTimeOffset.UtcNow, "UPG_123");

        installation.MarkMaterializing();
        installation.Complete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        installation.Supersede();
        installation.RestoreFromSuperseded();

        Assert.NotEqual(withoutSuffix, withSuffix);
        Assert.Equal("UPG_123", installation.MaterializationIdentitySuffix);
        Assert.Equal(ProductionPackageInstallationStatus.Installed, installation.Status);
    }

    [Fact]
    public void MaterializationCode_LongCodesKeepDistinctStableFingerprints()
    {
        var prefix = new string('A', 90);
        var first = ProductionPackageMaterializationCode.WithSuffix($"{prefix}_FIRST", "UPG_123");
        var second = ProductionPackageMaterializationCode.WithSuffix($"{prefix}_SECOND", "UPG_123");

        Assert.Equal(100, first.Length);
        Assert.Equal(100, second.Length);
        Assert.NotEqual(first, second);
        Assert.Equal(first, ProductionPackageMaterializationCode.WithSuffix($"{prefix}_FIRST", "UPG_123"));
    }

    [Fact]
    public void FailedUpgrade_ResumesWithTheSameSuccessorIdentity()
    {
        var upgrade = CreateUpgrade();
        var successorId = Guid.NewGuid();
        upgrade.AttachTargetInstallation(successorId, DateTimeOffset.UtcNow);
        upgrade.Fail("PREPARATION_FAILED", "Preparation failed.", DateTimeOffset.UtcNow);

        upgrade.ResumeMaterialization(DateTimeOffset.UtcNow);
        upgrade.AttachTargetInstallation(successorId, DateTimeOffset.UtcNow);

        Assert.Equal(ProductionPackageUpgradeStatus.Materializing, upgrade.Status);
        Assert.Equal(successorId, upgrade.TargetInstallationId);
        Assert.Null(upgrade.FailureCode);
        Assert.Null(upgrade.FailureMessage);
        Assert.Throws<DomainRuleException>(() =>
            upgrade.AttachTargetInstallation(Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ReadyForReviewUpgrade_CanBeAbandonedWithAuditEvidence()
    {
        var upgrade = CreateUpgrade();
        var actorId = Guid.NewGuid();
        var abandonedAt = DateTimeOffset.UtcNow.AddMinutes(1);
        upgrade.AddCatalogIdentityChange(ProductionPackageUpgradeCatalogIdentityChange.Create(
            "ICE_CREAM", Guid.NewGuid(), Guid.NewGuid(), "ICE_CREAM", "ICE_CREAM_OLD",
            "ICE_CREAM_UPG", "ICE_CREAM", Checksum('a'), Checksum('b')));
        upgrade.MarkReadyForReview(Guid.NewGuid(), abandonedAt.AddSeconds(-1));

        upgrade.Abandon(actorId, "Operator rejected the prepared successor.", abandonedAt);
        upgrade.Abandon(actorId, "Idempotent retry does not replace audit evidence.", abandonedAt.AddSeconds(1));

        Assert.Equal(ProductionPackageUpgradeStatus.Abandoned, upgrade.Status);
        Assert.Equal(actorId, upgrade.AbandonedByAccountId);
        Assert.Equal(abandonedAt, upgrade.AbandonedAt);
        Assert.Equal("Operator rejected the prepared successor.", upgrade.AbandonReason);
        Assert.Equal(abandonedAt, upgrade.LastProgressAt);
    }

    [Fact]
    public void Upgrade_RejectsAbandonWhileMaterializingOrAfterCutover()
    {
        var actorId = Guid.NewGuid();
        var materializing = CreateUpgrade();
        Assert.Throws<DomainRuleException>(() =>
            materializing.Abandon(actorId, "Not allowed while materializing.", DateTimeOffset.UtcNow));

        var completed = CreateUpgrade();
        completed.AddCatalogIdentityChange(ProductionPackageUpgradeCatalogIdentityChange.Create(
            "ICE_CREAM", Guid.NewGuid(), Guid.NewGuid(), "ICE_CREAM", "ICE_CREAM_OLD",
            "ICE_CREAM_UPG", "ICE_CREAM", Checksum('a'), Checksum('b')));
        completed.MarkReadyForReview(Guid.NewGuid(), DateTimeOffset.UtcNow);
        completed.Complete(actorId, DateTimeOffset.UtcNow);

        Assert.Throws<DomainRuleException>(() =>
            completed.Abandon(actorId, "Cutover requires rollback.", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void FailedUpgrade_CanBeAbandonedAndCannotResumeAfterward()
    {
        var upgrade = CreateUpgrade();
        upgrade.Fail("PREPARATION_FAILED", "Preparation failed.", DateTimeOffset.UtcNow);

        upgrade.Abandon(Guid.NewGuid(), "Do not retry this successor.", DateTimeOffset.UtcNow.AddSeconds(1));

        Assert.Equal(ProductionPackageUpgradeStatus.Abandoned, upgrade.Status);
        Assert.Throws<DomainRuleException>(() => upgrade.ResumeMaterialization(DateTimeOffset.UtcNow.AddSeconds(2)));
    }

    [Fact]
    public void EndpointRollbackAttempts_PreserveHistoryAndEnforceLimit()
    {
        var endpoint = ProductionPackageUpgradeEndpointTarget.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var actorId = Guid.NewGuid();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        endpoint.RecordRollbackDeployment(first, actorId, "Initial rollback", DateTimeOffset.UtcNow, 2);
        endpoint.RecordRollbackDeployment(second, actorId, "Retry after timeout", DateTimeOffset.UtcNow, 2);

        Assert.Equal(second, endpoint.RollbackDeploymentId);
        Assert.Collection(endpoint.RollbackAttempts.OrderBy(item => item.AttemptNo),
            attempt =>
            {
                Assert.Equal(1, attempt.AttemptNo);
                Assert.Equal(first, attempt.DeploymentId);
                Assert.Null(attempt.ReplacedDeploymentId);
            },
            attempt =>
            {
                Assert.Equal(2, attempt.AttemptNo);
                Assert.Equal(second, attempt.DeploymentId);
                Assert.Equal(first, attempt.ReplacedDeploymentId);
            });
        Assert.Throws<DomainRuleException>(() => endpoint.RecordRollbackDeployment(
            Guid.NewGuid(), actorId, "Too many", DateTimeOffset.UtcNow, 2));
    }

    private static ProductionPackageUpgrade CreateUpgrade() => ProductionPackageUpgrade.Approve(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Checksum('a'), Checksum('b'), Checksum('c'),
        ["ICE_CREAM"], $"upgrade-{Guid.NewGuid():N}", Guid.NewGuid(), DateTimeOffset.UtcNow);

    private static ProductionPackageVersion CreateVersion()
        => ProductionPackageVersion.CreateDraft(Guid.NewGuid(), 1);

    private static string Checksum(char value) => new(value, 64);
}
