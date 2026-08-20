using Application.ProductionConfiguration.Bindings;
using Domain.ProductionConfiguration.Entities;
using Domain.RobotConfiguration.Programs.Manifests;

namespace IceBot.UnitTests.ProductionConfiguration;

public sealed class ProductionProgramBindingCapabilityEvidenceTests
{
    [Fact]
    public void Create_StoresCanonicalOperatorDeclaredCapabilitySet()
    {
        var binding = ProductionProgramBinding.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(),
            new string('a', 64), ["topping_station", "cup_dispenser", "TOPPING_STATION"],
            ProductionProgramBindingCapabilityEvidenceStatus.Declared,
            ProductionProgramBindingAssurance.OperatorDeclared, ["oreo"], Guid.NewGuid());

        Assert.Equal(["CUP_DISPENSER", "TOPPING_STATION"], binding.GetRequiredCapabilityCodes());
        Assert.Equal(ProductionProgramBindingCapabilityEvidenceStatus.Declared, binding.CapabilityEvidenceStatus);
        Assert.Equal(ProductionProgramBindingAssurance.OperatorDeclared, binding.Assurance);
        Assert.Equal(64, binding.BindingChecksum.Length);
    }

    [Fact]
    public void Create_AllowsMissingEvidenceWithoutInventingCapability()
    {
        var binding = ProductionProgramBinding.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(),
            new string('b', 64), [], ProductionProgramBindingCapabilityEvidenceStatus.Missing,
            ProductionProgramBindingAssurance.OperatorDeclared, [], Guid.NewGuid());

        Assert.Empty(binding.GetRequiredCapabilityCodes());
        Assert.Equal(ProductionProgramBindingCapabilityEvidenceStatus.Missing, binding.CapabilityEvidenceStatus);
    }

    [Fact]
    public void Resolve_UsesRobotArmDefault_ForAnAllFairinoFr5ProgramWithoutDeclarations()
    {
        var proposal = RobotProgramCapabilityProfileDefaults.Resolve(CreateManifest("FAIRINO_LUA_V1", "FR5"), []);

        Assert.Equal(["ROBOT_ARM"], proposal.RequiredCapabilityCodes);
        Assert.Equal(ProductionProgramBindingCapabilityEvidenceStatus.TargetProfileDefault, proposal.Status);
    }

    [Fact]
    public void Resolve_DoesNotApplyProfileDefault_ToMixedOrUnknownTargets()
    {
        var proposal = RobotProgramCapabilityProfileDefaults.Resolve(CreateManifest("FAIRINO_LUA_V1", "FR3"), []);

        Assert.Empty(proposal.RequiredCapabilityCodes);
        Assert.Equal(ProductionProgramBindingCapabilityEvidenceStatus.Missing, proposal.Status);
    }

    private static RobotProgramManifestDocument CreateManifest(string runtimeTargetCode, string machineModelCode) => new(
        Guid.NewGuid(), "PROGRAM", 1,
        [new RobotProgramManifestItem(Guid.NewGuid(), 1, 1, null,
            new RobotProgramManifestArtifact(Guid.NewGuid(), new string('c', 64), "program.lua", runtimeTargetCode,
                machineModelCode, 1, null, null))]);
}
