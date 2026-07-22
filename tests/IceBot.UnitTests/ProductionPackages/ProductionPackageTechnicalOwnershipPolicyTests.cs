using Application.ProductionPackages.Ownership;
using Domain.ProductionPackages;
using Application.Shared.Ownership;
using NSubstitute;

namespace IceBot.UnitTests.ProductionPackages;

public sealed class ProductionPackageTechnicalOwnershipPolicyTests
{
    [Theory]
    [InlineData(TechnicalResourceKind.Product, ProductionPackageResourceKind.Product)]
    [InlineData(TechnicalResourceKind.ProductVariant, ProductionPackageResourceKind.ProductVariant)]
    [InlineData(TechnicalResourceKind.Recipe, ProductionPackageResourceKind.Recipe)]
    [InlineData(TechnicalResourceKind.ProductOption, ProductionPackageResourceKind.ProductOption)]
    [InlineData(TechnicalResourceKind.RobotArtifact, ProductionPackageResourceKind.RobotArtifact)]
    [InlineData(TechnicalResourceKind.RobotProgram, ProductionPackageResourceKind.RobotProgram)]
    [InlineData(TechnicalResourceKind.ConfigurationRelease, ProductionPackageResourceKind.ConfigurationRelease)]
    public async Task DefinitionMutation_IsRejected_ForPackageManagedResource(
        TechnicalResourceKind resourceKind,
        ProductionPackageResourceKind packageResourceKind)
    {
        var resourceId = Guid.NewGuid();
        var store = Substitute.For<IProductionPackageTechnicalOwnershipStore>();
        store.IsPackageManagedAsync(packageResourceKind, resourceId,
                Arg.Any<CancellationToken>())
            .Returns(true);

        var error = await new ProductionPackageTechnicalOwnershipPolicy(store)
            .ValidateDefinitionMutationAsync(resourceKind, resourceId);

        Assert.Equal(
            "Package-managed technical configuration must be forked before its definition can be changed.",
            error);
    }

    [Fact]
    public async Task DefinitionMutation_IsAllowed_ForOrganizationFork()
    {
        var resourceId = Guid.NewGuid();
        var store = Substitute.For<IProductionPackageTechnicalOwnershipStore>();
        store.IsPackageManagedAsync(ProductionPackageResourceKind.ConfigurationRelease, resourceId,
                Arg.Any<CancellationToken>())
            .Returns(false);

        var error = await new ProductionPackageTechnicalOwnershipPolicy(store)
            .ValidateDefinitionMutationAsync(TechnicalResourceKind.ConfigurationRelease, resourceId);

        Assert.Null(error);
    }
}
