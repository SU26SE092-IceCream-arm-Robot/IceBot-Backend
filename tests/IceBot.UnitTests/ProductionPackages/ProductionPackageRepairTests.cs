using Application.Identity.Tokens.Claims;
using Application.ProductionPackages;
using Application.ProductionPackages.Installation;
using Application.RobotConfiguration.Storage.Abstractions;
using Application.RobotConfiguration.Storage.Services;
using Domain.ProductionPackages;
using Microsoft.Extensions.Logging.Abstractions;
using Application.Shared.Concurrency;
using NSubstitute;

namespace IceBot.UnitTests.ProductionPackages;

public sealed class ProductionPackageRepairTests
{
    [Fact]
    public void Expectations_QualifyRecipeByVariantAndBindExactRelease()
    {
        var product = ProductionPackageProductDefinition.Create("PRODUCT", Guid.NewGuid(), $$"""
            {
              "SchemaVersion": 2,
              "Product": {
                "Id": "{{Guid.NewGuid()}}", "Code": "PRODUCT", "Name": "Product",
                "Variants": [
                  { "Id": "{{Guid.NewGuid()}}", "Code": "SMALL", "Name": "Small",
                    "Recipes": [{ "Id": "{{Guid.NewGuid()}}", "Code": "DEFAULT", "Name": "Default", "Version": 1, "Items": [] }] },
                  { "Id": "{{Guid.NewGuid()}}", "Code": "LARGE", "Name": "Large",
                    "Recipes": [{ "Id": "{{Guid.NewGuid()}}", "Code": "DEFAULT", "Name": "Default", "Version": 1, "Items": [] }] }
                ],
                "OptionGroups": []
              }
            }
            """);
        var artifact = ProductionPackageArtifactDefinition.Create(
            "BASE", Guid.NewGuid(), new string('a', 64), Guid.NewGuid(), new string('b', 64));
        var program = ProductionPackageProgramBlueprint.Create("PROGRAM", "FAIRINO_LUA_V1", "FR5",
            [("BASE", "BASE", "MAKE_BASE", "BASE", true, false, 1)]);
        var routes = new[]
        {
            ProductionPackageRouteBlueprint.Create("SMALL_ROUTE", "PRODUCT", "SMALL", "DEFAULT", [],
                "PROGRAM", """{"schemaVersion":1,"requires":[{"code":"ROBOT_ARM"}]}""", 1),
            ProductionPackageRouteBlueprint.Create("LARGE_ROUTE", "PRODUCT", "LARGE", "DEFAULT", [],
                "PROGRAM", """{"schemaVersion":1,"requires":[{"code":"ROBOT_ARM"}]}""", 2)
        };
        var version = ProductionPackageVersion.CreateDraft(Guid.NewGuid(), 1);
        version.ReplaceDefinition([product], [artifact], [program], routes);
        var releaseId = Guid.NewGuid();
        var installation = ProductionPackageInstallation.Start(
            Guid.NewGuid(), null, null, version.Id, new string('c', 64), new string('d', 64),
            "install-key", ["PRODUCT"], DateTimeOffset.UtcNow);
        installation.MarkMaterializing();
        installation.Complete(releaseId, DateTimeOffset.UtcNow);
        typeof(ProductionPackageInstallation).GetProperty(nameof(ProductionPackageInstallation.PackageVersion))!
            .SetValue(installation, version);

        var expectations = ProductionPackageMaterializationExpectationBuilder.Build(installation);

        Assert.Contains(expectations, x => x.SourceKey == "PRODUCT:VARIANT:SMALL:RECIPE:DEFAULT");
        Assert.Contains(expectations, x => x.SourceKey == "PRODUCT:VARIANT:LARGE:RECIPE:DEFAULT");
        var release = Assert.Single(expectations,
            x => x.ResourceKind == ProductionPackageResourceKind.ConfigurationRelease);
        Assert.Equal(releaseId, release.ExpectedTargetId);
    }

    [Fact]
    public void Expectations_ForPartialInstallationUseOnlySelectedDependencyClosure()
    {
        var selectedProduct = ProductDefinition("ICE_CREAM");
        var unselectedProduct = ProductDefinition("DRINK");
        var selectedArtifact = ProductionPackageArtifactDefinition.Create(
            "ICE_ARTIFACT", Guid.NewGuid(), new string('a', 64), Guid.NewGuid(), new string('b', 64));
        var unselectedArtifact = ProductionPackageArtifactDefinition.Create(
            "DRINK_ARTIFACT", Guid.NewGuid(), new string('c', 64), Guid.NewGuid(), new string('d', 64));
        var selectedProgram = ProductionPackageProgramBlueprint.Create(
            "ICE_PROGRAM", "FAIRINO_LUA_V1", "FR5",
            [("MAIN", "ICE_ARTIFACT", "MAKE_ICE", "PRODUCTION", true, false, 1)]);
        var unselectedProgram = ProductionPackageProgramBlueprint.Create(
            "DRINK_PROGRAM", "FAIRINO_LUA_V1", "FR5",
            [("MAIN", "DRINK_ARTIFACT", "MAKE_DRINK", "PRODUCTION", true, false, 1)]);
        var version = ProductionPackageVersion.CreateDraft(Guid.NewGuid(), 1);
        version.ReplaceDefinition(
            [selectedProduct, unselectedProduct],
            [selectedArtifact, unselectedArtifact],
            [selectedProgram, unselectedProgram],
            [
                Route("ICE_ROUTE", "ICE_CREAM", "ICE_PROGRAM"),
                Route("DRINK_ROUTE", "DRINK", "DRINK_PROGRAM")
            ]);
        var installation = ProductionPackageInstallation.Start(
            Guid.NewGuid(), null, null, version.Id, new string('e', 64), new string('f', 64),
            "partial-install", ["ICE_CREAM"], DateTimeOffset.UtcNow);
        installation.MarkMaterializing();
        installation.Complete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        typeof(ProductionPackageInstallation).GetProperty(nameof(ProductionPackageInstallation.PackageVersion))!
            .SetValue(installation, version);

        var expectations = ProductionPackageMaterializationExpectationBuilder.Build(installation);

        Assert.Contains(expectations, item =>
            item.ResourceKind == ProductionPackageResourceKind.RobotArtifact &&
            item.SourceKey == "ICE_ARTIFACT");
        Assert.Contains(expectations, item =>
            item.ResourceKind == ProductionPackageResourceKind.RobotProgram &&
            item.SourceKey == "ICE_ROUTE");
        Assert.DoesNotContain(expectations, item =>
            item.SourceKey is "DRINK_ARTIFACT" or "DRINK_ROUTE");
    }

    [Fact]
    public async Task Repair_RestoresSoftDeletedTargetsWithoutCreatingAnotherInstallation()
    {
        var organizationId = Guid.NewGuid();
        var installation = Installed(organizationId);
        var restored = new ProductionPackageMaterializationRepairItem(
            "RobotArtifact", "PREPARE", Guid.NewGuid().ToString("D"));
        var store = Substitute.For<IProductionPackageInstallationStore>();
        store.GetForEditAsync(organizationId, installation.Id, Arg.Any<CancellationToken>())
            .Returns(installation);
        store.RestoreSoftDeletedMaterializationsAsync(
                organizationId, installation.Id, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ProductionPackageMaterializationRepairResult([restored], []));

        var result = await Service(store).RepairAsync(
            OrgAdmin(organizationId), organizationId, installation.Id, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(installation.Id, result.Data!.InstallationId);
        Assert.Equal(restored, Assert.Single(result.Data.RestoredResources));
        await store.Received(1).RestoreSoftDeletedMaterializationsAsync(
            organizationId, installation.Id, Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await store.DidNotReceive().InsertOrGetAsync(
            Arg.Any<ProductionPackageInstallation>(), Arg.Any<CancellationToken>());
    }

    private static ProductionPackageProductDefinition ProductDefinition(string code) =>
        ProductionPackageProductDefinition.Create(code, Guid.NewGuid(), $$"""
            {
              "SchemaVersion": 2,
              "Product": {
                "Id": "{{Guid.NewGuid()}}", "Code": "{{code}}", "Name": "{{code}}",
                "Variants": [], "OptionGroups": []
              }
            }
            """);

    private static ProductionPackageRouteBlueprint Route(
        string routeCode, string productCode, string programCode) =>
        ProductionPackageRouteBlueprint.Create(
            routeCode, productCode, "STANDARD", "DEFAULT", [], programCode,
            """{"schemaVersion":1,"requires":[{"code":"ROBOT_ARM"}]}""", 1);

    [Fact]
    public async Task Repair_ReturnsConflictWhenTargetWasPhysicallyDeleted()
    {
        var organizationId = Guid.NewGuid();
        var installation = Installed(organizationId);
        var store = Substitute.For<IProductionPackageInstallationStore>();
        store.GetForEditAsync(organizationId, installation.Id, Arg.Any<CancellationToken>())
            .Returns(installation);
        store.RestoreSoftDeletedMaterializationsAsync(
                organizationId, installation.Id, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ProductionPackageMaterializationRepairResult([], [
                new ProductionPackageMaterializationRepairIssue(
                    "RobotArtifact", "PREPARE", Guid.NewGuid().ToString("D"), "TargetPhysicallyMissing")
            ]));

        var result = await Service(store).RepairAsync(
            OrgAdmin(organizationId), organizationId, installation.Id, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("TargetPhysicallyMissing", result.Message, StringComparison.Ordinal);
        var issues = Assert.IsAssignableFrom<IReadOnlyCollection<ProductionPackageMaterializationRepairIssue>>(
            result.Details!["issues"]);
        Assert.Equal("PREPARE", Assert.Single(issues).SourceKey);
    }

    [Fact]
    public async Task Repair_IsSuccessfulNoOpAfterTargetsAreAlreadyActive()
    {
        var organizationId = Guid.NewGuid();
        var installation = Installed(organizationId);
        var store = Substitute.For<IProductionPackageInstallationStore>();
        store.GetForEditAsync(organizationId, installation.Id, Arg.Any<CancellationToken>())
            .Returns(installation);
        store.RestoreSoftDeletedMaterializationsAsync(
                organizationId, installation.Id, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ProductionPackageMaterializationRepairResult([], []));

        var result = await Service(store).RepairAsync(
            OrgAdmin(organizationId), organizationId, installation.Id, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Data!.RestoredResources);
    }

    private static ProductionPackageInstallationService Service(IProductionPackageInstallationStore store)
    {
        var storage = Substitute.For<IArtifactObjectStorage>();
        var contracts = Substitute.For<Application.RobotConfiguration.ArtifactContracts.IRobotArtifactTechnicalContractStore>();
        return new ProductionPackageInstallationService(
            Substitute.For<IProductionPackageStore>(), store, storage,
            new ArtifactUploadContentService(storage, NullLogger<ArtifactUploadContentService>.Instance),
            new ArtifactPublicationValidator(contracts, storage),
            InlineTechnicalResourceMutationCoordinator.Instance);
    }

    private static ProductionPackageInstallation Installed(Guid organizationId)
    {
        var installation = ProductionPackageInstallation.Start(
            organizationId, null, null, Guid.NewGuid(), new string('a', 64), new string('b', 64),
            "install-key", ["ICE_CREAM"], DateTimeOffset.UtcNow);
        installation.MarkMaterializing();
        installation.AddMaterialization(ProductionPackageResourceKind.Product, "ICE_CREAM",
            Guid.NewGuid().ToString("D"));
        installation.Complete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        return installation;
    }

    private static CurrentUserContext OrgAdmin(Guid organizationId) => new()
    {
        AccountId = Guid.NewGuid(),
        RoleScopes = [new UserRoleScope("OrgAdmin", organizationId, null, null)]
    };
}
