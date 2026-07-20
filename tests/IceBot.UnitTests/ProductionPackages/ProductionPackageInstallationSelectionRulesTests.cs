using Application.ProductionPackages.Installation;
using Domain.Common;
using Domain.ProductionPackages;

namespace IceBot.UnitTests.ProductionPackages;

public sealed class ProductionPackageInstallationSelectionRulesTests
{
    [Fact]
    public void Resolve_ReturnsOnlyDependenciesReachableFromSelectedProducts()
    {
        var version = VersionWithIndependentProducts();

        var result = ProductionPackageInstallationSelectionRules.Resolve(
            version, new HashSet<string>(StringComparer.Ordinal) { "ICE_CREAM" });

        Assert.Equal("ICE_ROUTE", Assert.Single(result.Routes).RouteCode);
        Assert.Equal("ICE_PROGRAM", Assert.Single(result.Programs).BlueprintCode);
        Assert.Equal("ICE_ARTIFACT", Assert.Single(result.Artifacts).SourceKey);
    }

    [Fact]
    public void Resolve_IncludesArtifactSharedBySelectedProgramsOnlyOnce()
    {
        var version = VersionWithSharedArtifact();

        var result = ProductionPackageInstallationSelectionRules.Resolve(
            version, new HashSet<string>(StringComparer.Ordinal) { "ICE_CREAM", "DRINK" });

        Assert.Equal(2, result.Routes.Count);
        Assert.Equal(2, result.Programs.Count);
        Assert.Equal("SHARED_HOME", Assert.Single(result.Artifacts).SourceKey);
    }

    [Fact]
    public void Resolve_RejectsSelectedProductWithoutExecutionRoute()
    {
        var artifact = Artifact("ICE_ARTIFACT");
        var version = ProductionPackageVersion.CreateDraft(Guid.NewGuid(), 1);
        version.ReplaceDefinition(
            [Product("ICE_CREAM"), Product("UNROUTED")],
            [artifact],
            [Program("ICE_PROGRAM", artifact.SourceKey)],
            [Route("ICE_ROUTE", "ICE_CREAM", "ICE_PROGRAM")]);

        var exception = Assert.Throws<DomainRuleException>(() =>
            ProductionPackageInstallationSelectionRules.Resolve(
                version, new HashSet<string>(StringComparer.Ordinal) { "UNROUTED" }));

        Assert.Contains("UNROUTED", exception.Message);
    }

    private static ProductionPackageVersion VersionWithIndependentProducts()
    {
        var iceArtifact = Artifact("ICE_ARTIFACT");
        var drinkArtifact = Artifact("DRINK_ARTIFACT");
        var version = ProductionPackageVersion.CreateDraft(Guid.NewGuid(), 1);
        version.ReplaceDefinition(
            [Product("ICE_CREAM"), Product("DRINK")],
            [iceArtifact, drinkArtifact],
            [Program("ICE_PROGRAM", "ICE_ARTIFACT"), Program("DRINK_PROGRAM", "DRINK_ARTIFACT")],
            [Route("ICE_ROUTE", "ICE_CREAM", "ICE_PROGRAM"), Route("DRINK_ROUTE", "DRINK", "DRINK_PROGRAM")]);
        return version;
    }

    private static ProductionPackageVersion VersionWithSharedArtifact()
    {
        var version = ProductionPackageVersion.CreateDraft(Guid.NewGuid(), 1);
        version.ReplaceDefinition(
            [Product("ICE_CREAM"), Product("DRINK")],
            [Artifact("SHARED_HOME")],
            [Program("ICE_PROGRAM", "SHARED_HOME"), Program("DRINK_PROGRAM", "SHARED_HOME")],
            [Route("ICE_ROUTE", "ICE_CREAM", "ICE_PROGRAM"), Route("DRINK_ROUTE", "DRINK", "DRINK_PROGRAM")]);
        return version;
    }

    private static ProductionPackageProductDefinition Product(string code) =>
        ProductionPackageProductDefinition.Create(code, Guid.NewGuid(), "{}");

    private static ProductionPackageArtifactDefinition Artifact(string code) =>
        ProductionPackageArtifactDefinition.Create(
            code, Guid.NewGuid(), new string('a', 64), Guid.NewGuid(), new string('b', 64));

    private static ProductionPackageProgramBlueprint Program(string code, string artifactCode) =>
        ProductionPackageProgramBlueprint.Create(
            code, "FAIRINO_LUA_V1", "FR5",
            [("MAIN", artifactCode, $"{code}_EFFECT", "PRODUCTION", true, false, 1)]);

    private static ProductionPackageRouteBlueprint Route(string code, string productCode, string programCode) =>
        ProductionPackageRouteBlueprint.Create(
            code, productCode, "STANDARD", "DEFAULT", [], programCode,
            """{"schemaVersion":1,"requires":[{"code":"ROBOT_ARM"}]}""", 1);
}
