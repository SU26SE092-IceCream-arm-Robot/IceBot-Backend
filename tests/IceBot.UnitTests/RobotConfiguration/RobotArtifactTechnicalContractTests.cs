using Domain.Common;
using Domain.RobotConfiguration.ArtifactContracts;

namespace IceBot.UnitTests.RobotConfiguration;

public sealed class RobotArtifactTechnicalContractTests
{
    [Fact]
    public void Publish_RejectsParameterizedQuantityWhenRuntimeDoesNotSupportIt()
    {
        var contract = RobotArtifactTechnicalContract.CreateDraft(
            "DISPENSE_BASE", 1, "FAIRINO_LUA_V1", "FR5");
        contract.ReplaceDefinition(
        [
            new RobotArtifactEffectDefinition(
                "DISPENSE_BASE", RobotArtifactEffectKind.Ingredient, "ICE_CREAM_BASE", null,
                RobotArtifactQuantityMode.Parameterized, null, "g", "DISPENSER")
        ], []);

        var exception = Assert.Throws<DomainRuleException>(() =>
            contract.Publish(DateTimeOffset.UtcNow, null, parameterizedRuntimeSupported: false));

        Assert.Contains("not supported", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
