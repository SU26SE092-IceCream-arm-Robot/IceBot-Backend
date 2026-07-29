using Application.ProductionPackages.Installation;
using Domain.Common;
using Domain.ProductionPackages;

namespace IceBot.UnitTests.ProductionPackages;

public sealed class ProductionPackageInstallationRequestRulesTests
{
    [Fact]
    public void ResolveSelectedProductKeys_NormalizesAndDeduplicatesSelection()
    {
        var version = VersionWithProducts("ICE_CREAM", "DRINK");

        var selected = ProductionPackageInstallationRequestRules.ResolveSelectedProductKeys(
            version, [" ice_cream ", "ICE_CREAM"]);

        Assert.Equal("ICE_CREAM", Assert.Single(selected));
    }

    [Theory]
    [InlineData("")]
    [InlineData("UNKNOWN")]
    public void ResolveSelectedProductKeys_RejectsInvalidSelection(string productKey)
    {
        var version = VersionWithProducts("ICE_CREAM");

        Assert.Throws<DomainRuleException>(() =>
            ProductionPackageInstallationRequestRules.ResolveSelectedProductKeys(version, [productKey]));
    }

    [Fact]
    public void ComputeRequestChecksum_IsStableAcrossSelectionOrder()
    {
        var version = VersionWithProducts("ICE_CREAM", "DRINK");
        var organizationId = Guid.NewGuid();

        var first = ProductionPackageInstallationRequestRules.ComputeRequestChecksum(
            organizationId, null, null, version, ["ICE_CREAM", "DRINK"]);
        var second = ProductionPackageInstallationRequestRules.ComputeRequestChecksum(
            organizationId, null, null, version, ["DRINK", "ICE_CREAM"]);

        Assert.Equal(first, second);
    }

    private static ProductionPackageVersion VersionWithProducts(params string[] productKeys)
    {
        var version = ProductionPackageVersion.CreateDraft(Guid.NewGuid(), 1);
        var artifact = ProductionPackageArtifactDefinition.Create(
            "ARTIFACT", Guid.NewGuid(), new string('a', 64), Guid.NewGuid(), new string('b', 64));
        var program = ProductionPackageProgramBlueprint.Create(
            "PROGRAM", "FAIRINO_LUA_V1", "FR5",
            [("MAIN", "ARTIFACT", "DISPENSE", "PRODUCTION", true, false, 1)]);
        version.ReplaceDefinition(
            productKeys.Select(key => ProductionPackageProductDefinition.Create(key, Guid.NewGuid(), "{}")),
            [artifact],
            [program],
            productKeys.Select((key, index) => ProductionPackageRouteBlueprint.Create(
                $"ROUTE_{index}", key, "STANDARD", "DEFAULT", [], "PROGRAM",
                """{"schemaVersion":1,"requires":[]}""", index)));
        return version;
    }
}
