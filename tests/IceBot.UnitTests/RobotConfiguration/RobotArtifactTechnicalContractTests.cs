using Domain.Common;
using Domain.RobotConfiguration.ArtifactContracts;

namespace IceBot.UnitTests.RobotConfiguration;

public sealed class RobotArtifactTechnicalContractTests
{
    [Fact]
    public void ReplaceDefinition_RejectsUndefinedEnumValues()
    {
        var contract = RobotArtifactTechnicalContract.CreateDraft(
            "INVALID_ENUMS", 1, "FAIRINO_LUA_V1", "FR5");

        Assert.Throws<DomainRuleException>(() => contract.ReplaceDefinition(
        [
            new RobotArtifactEffectDefinition(
                "INVALID", (RobotArtifactEffectKind)999, null, null,
                RobotArtifactQuantityMode.None, null, null, null)
        ], []));

        Assert.Throws<DomainRuleException>(() => contract.ReplaceDefinition(
        [
            new RobotArtifactEffectDefinition(
                "INVALID", RobotArtifactEffectKind.System, null, null,
                (RobotArtifactQuantityMode)999, null, null, null)
        ], []));

        Assert.Throws<DomainRuleException>(() => contract.ReplaceDefinition(
        [
            new RobotArtifactEffectDefinition(
                "VALID", RobotArtifactEffectKind.System, null, null,
                RobotArtifactQuantityMode.None, null, null, null)
        ],
        [
            new RobotArtifactOrderingConstraintDefinition(
                (RobotArtifactOrderingConstraintType)999, "BASE", 1)
        ]));
    }

    [Fact]
    public void ReplaceDefinition_RejectsOptionCodeOnMotionEffect()
    {
        var contract = RobotArtifactTechnicalContract.CreateDraft(
            "MOVE_HOME", 1, "FAIRINO_LUA_V1", "FR5");

        var exception = Assert.Throws<DomainRuleException>(() => contract.ReplaceDefinition(
        [
            new RobotArtifactEffectDefinition(
                "MOVE_HOME", RobotArtifactEffectKind.Motion, null, "EXTRA_TOPPING",
                RobotArtifactQuantityMode.None, null, null, null)
        ], []));

        Assert.Contains("cannot declare ingredient or option", exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReplaceDefinition_AllowsOptionEffectToIdentifyConsumedIngredient()
    {
        var contract = RobotArtifactTechnicalContract.CreateDraft(
            "ADD_OREO", 1, "FAIRINO_LUA_V1", "FR5");

        contract.ReplaceDefinition(
        [
            new RobotArtifactEffectDefinition(
                "ADD_OREO", RobotArtifactEffectKind.Option, "OREO_CRUMB", "OREO",
                RobotArtifactQuantityMode.FixedInArtifact, 10, "g", "TOPPING_STATION")
        ], []);

        var effect = Assert.Single(contract.Effects);
        Assert.Equal("OREO_CRUMB", effect.IngredientCode);
        Assert.Equal("OREO", effect.OptionCode);
    }

    [Fact]
    public void Publish_StoresParameterizedQuantityAsOperatorDeclaration()
    {
        var contract = RobotArtifactTechnicalContract.CreateDraft(
            "DISPENSE_BASE", 1, "FAIRINO_LUA_V1", "FR5");
        contract.ReplaceDefinition(
        [
            new RobotArtifactEffectDefinition(
                "DISPENSE_BASE", RobotArtifactEffectKind.Ingredient, "ICE_CREAM_BASE", null,
                RobotArtifactQuantityMode.Parameterized, null, "g", "DISPENSER")
        ], []);

        contract.Publish(DateTimeOffset.UtcNow, null, parameterizedRuntimeSupported: false);

        Assert.Equal(RobotArtifactContractStatus.Published, contract.Status);
    }
}
