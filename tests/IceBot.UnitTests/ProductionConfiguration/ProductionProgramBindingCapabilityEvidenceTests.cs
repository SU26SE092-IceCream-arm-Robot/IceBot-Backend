using Domain.ProductionConfiguration.Entities;

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
}
