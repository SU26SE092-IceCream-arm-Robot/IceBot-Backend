using Application.Identity.Tokens.Claims;
using Application.ProductionPackages.Workspace;
using Domain.Common;
using Domain.ProductionPackages;
using NSubstitute;

namespace IceBot.UnitTests.ProductionPackages;

public sealed class ProductionPackageWorkspaceTests
{
    [Fact]
    public void Installation_PreservesCanonicalProductSelectionForRetry()
    {
        var installation = ProductionPackageInstallation.Start(
            Guid.NewGuid(), null, null, Guid.NewGuid(), new string('a', 64), new string('b', 64),
            "retry-key", ["drink", "ICE_CREAM", "drink"], DateTimeOffset.UtcNow);

        Assert.Equal(["DRINK", "ICE_CREAM"], installation.GetSelectedProductSourceKeys());
    }

    [Fact]
    public void InstalledInstallation_CannotBeDowngradedToFailed()
    {
        var installation = ProductionPackageInstallation.Start(
            Guid.NewGuid(), null, null, Guid.NewGuid(), new string('a', 64), new string('b', 64),
            "terminal-state", ["ICE_CREAM"], DateTimeOffset.UtcNow);
        installation.MarkMaterializing();
        installation.Complete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var exception = Assert.Throws<DomainRuleException>(() =>
            installation.Fail("LATE_FAILURE", "A concurrent request failed late.", DateTimeOffset.UtcNow));

        Assert.Contains("pending or materializing", exception.Message);
        Assert.Equal(ProductionPackageInstallationStatus.Installed, installation.Status);
    }

    [Fact]
    public async Task Workspace_UsesInstallationStoreScopeForManagerAuthorization()
    {
        var organizationId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var installationId = Guid.NewGuid();
        var store = Substitute.For<IProductionPackageWorkspaceStore>();
        store.GetScopeAsync(organizationId, installationId, Arg.Any<CancellationToken>())
            .Returns(new ProductionPackageWorkspaceScope(organizationId, storeId, null));
        store.GetAsync(organizationId, installationId, Arg.Any<CancellationToken>())
            .Returns(Workspace(installationId, organizationId, storeId));
        var user = new CurrentUserContext
        {
            RoleScopes = [new UserRoleScope("Manager", organizationId, storeId, null)]
        };

        var result = await new ProductionPackageWorkspaceService(store)
            .GetAsync(user, organizationId, installationId, CancellationToken.None);

        Assert.True(result.Succeeded);
        await store.Received(1).GetAsync(organizationId, installationId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void RequiredOptionGroup_ReturnsOneDeficitActionWithCandidates()
    {
        var options = new[]
        {
            new WorkspaceOptionAvailabilityInput(Guid.NewGuid(), Guid.NewGuid(), 1, "TOPPING", true, true, 1, false),
            new WorkspaceOptionAvailabilityInput(Guid.NewGuid(), Guid.NewGuid(), 1, "TOPPING", true, true, 1, false),
            new WorkspaceOptionAvailabilityInput(Guid.NewGuid(), Guid.NewGuid(), 1, "TOPPING", true, true, 1, false)
        };

        var action = Assert.Single(ProductionPackageWorkspaceRules.BuildRequiredOptionGroupActions(options));

        Assert.Equal("RestoreRequiredOptionGroupAvailability", action.Code);
        Assert.NotNull(action.Context);
        Assert.Equal(1, action.Context.OptionGroupId);
        Assert.NotEqual(Guid.Empty, action.Context.ProductId);
        Assert.Equal(1, action.RequiredCount);
        Assert.Equal(3, action.CandidateResourceIds!.Count);
    }

    private static ProductionPackageWorkspaceResult Workspace(Guid installationId, Guid organizationId,
        Guid storeId) => new(installationId, organizationId, storeId, null, "Installed", "PackageManaged",
        Guid.NewGuid(), "PACKAGE", "Package", Guid.NewGuid(), 1, [], [], [], [], [], [], [], null,
        new WorkspaceTechnicalReadinessResult(false, false, false, null, []),
        new WorkspaceCommercialReadinessResult(false, []), [], [], []);
}
