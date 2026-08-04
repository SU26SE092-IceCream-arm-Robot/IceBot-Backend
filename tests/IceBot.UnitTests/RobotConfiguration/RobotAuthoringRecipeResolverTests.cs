using Application.RobotConfiguration.AuthoringImports;
using Application.RobotConfiguration.AuthoringImports.Composition;
using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using Domain.RobotConfiguration.ArtifactContracts;
using Domain.RobotConfiguration.AuthoringImports;
using Domain.Tenants.Enums;
using NSubstitute;

namespace IceBot.UnitTests.RobotConfiguration;

public sealed class RobotAuthoringRecipeResolverTests
{
    [Fact]
    public async Task ResolveAsync_ExactSingleRecipe_ReturnsSingleMatch()
    {
        var organizationId = Guid.NewGuid();
        var import = AppliedImport(organizationId, out var contract);
        var recipe = Recipe(organizationId, "VANILLA", "Vanilla", [
            ("FRESH-MILK", 13.333333m),
            ("COMPRITAL-SOFT-PREMIUM", 21.333333m),
            ("PURIFIED-WATER", 46.666667m)]);
        var resolver = Resolver(import, [contract], [recipe]);

        var result = await resolver.ResolveAsync(organizationId, import.Id, CancellationToken.None);

        Assert.Equal("SingleMatch", result.Status);
        Assert.Equal(recipe.Id, Assert.Single(result.Candidates).RecipeId);
    }

    [Fact]
    public async Task ResolveAsync_MultipleExactRecipes_RequiresExplicitSelection()
    {
        var organizationId = Guid.NewGuid();
        var import = AppliedImport(organizationId, out var contract);
        var recipes = new[]
        {
            Recipe(organizationId, "VANILLA-A", "Vanilla A", [("FRESH-MILK", 13.333333m), ("COMPRITAL-SOFT-PREMIUM", 21.333333m), ("PURIFIED-WATER", 46.666667m)]),
            Recipe(organizationId, "VANILLA-B", "Vanilla B", [("FRESH-MILK", 13.333333m), ("COMPRITAL-SOFT-PREMIUM", 21.333333m), ("PURIFIED-WATER", 46.666667m)])
        };
        var resolver = Resolver(import, [contract], recipes);

        var result = await resolver.ResolveAsync(organizationId, import.Id, CancellationToken.None);

        Assert.Equal("MultipleMatches", result.Status);
        Assert.Equal(2, result.Candidates.Count);
    }

    [Fact]
    public async Task ResolveAsync_NoExactRecipe_LeavesDraftResourcesWithoutBinding()
    {
        var organizationId = Guid.NewGuid();
        var import = AppliedImport(organizationId, out var contract);
        var resolver = Resolver(import, [contract], [
            Recipe(organizationId, "DIFFERENT", "Different", [("FRESH-MILK", 100m)])]);

        var result = await resolver.ResolveAsync(organizationId, import.Id, CancellationToken.None);

        Assert.Equal("NoMatch", result.Status);
        Assert.Empty(result.Candidates);
        Assert.Null(import.ComposedRecipeId);
        Assert.Null(import.LinkedConfigurationReleaseId);
    }

    private static RobotAuthoringRecipeResolver Resolver(
        RobotAuthoringImport import,
        IReadOnlyCollection<RobotArtifactTechnicalContract> contracts,
        IReadOnlyCollection<Recipe> recipes)
    {
        var imports = Substitute.For<IRobotAuthoringImportStore>();
        imports.GetAsync(import.OrganizationId, import.Id, false, Arg.Any<CancellationToken>()).Returns(import);
        var composition = Substitute.For<IRobotAuthoringCompositionStore>();
        composition.GetContractsAsync(import.OrganizationId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(contracts.ToArray());
        composition.ListEligibleRecipesAsync(import.OrganizationId, Arg.Any<CancellationToken>()).Returns(recipes.ToArray());
        return new RobotAuthoringRecipeResolver(imports, composition);
    }

    private static RobotAuthoringImport AppliedImport(Guid organizationId, out RobotArtifactTechnicalContract contract)
    {
        contract = RobotArtifactTechnicalContract.CreateDraft("VANILLA_BASE", 1, "FAIRINO_LUA_V1", "FR5",
            organizationId, schemaVersion: 2);
        contract.ReplaceDefinition(
        [
            Effect("FRESH-MILK", 13.333333m),
            Effect("COMPRITAL-SOFT-PREMIUM", 21.333333m),
            Effect("PURIFIED-WATER", 46.666667m)
        ], [new RobotArtifactOrderingConstraintDefinition(RobotArtifactOrderingConstraintType.Phase, "BASE", 1)]);

        var import = RobotAuthoringImport.Create(organizationId, null, null, null, Guid.NewGuid(),
            new string('a', 64), "import-key", 1, "VANILLA", "Vanilla", "FAIRINO_LUA_V1", "FR5",
            "robot-authoring-imports/import.zip", Guid.NewGuid());
        import.AddItem("VANILLA_BASE", "vanilla.lua", "vanilla.icebot.json", 1,
            new string('b', 64), new string('c', 64));
        import.Items.Single().MarkResolved(Guid.NewGuid(), contract.Id, true);
        import.MarkValidated("{}", DateTimeOffset.UtcNow, Guid.NewGuid());
        import.MarkApplied(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid());
        return import;
    }

    private static RobotArtifactEffectDefinition Effect(string ingredientCode, decimal quantity) => new(
        $"DISPENSE_{ingredientCode}", RobotArtifactEffectKind.Ingredient, ingredientCode, null,
        RobotArtifactQuantityMode.FixedInArtifact, quantity, "gram", null);

    private static Recipe Recipe(Guid organizationId, string code, string name,
        IReadOnlyCollection<(string IngredientCode, decimal Quantity)> items)
    {
        var product = new Product { Id = Guid.NewGuid(), OrganizationId = organizationId, Code = $"PRODUCT-{code}", Name = name };
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(), ProductId = product.Id, Product = product, Code = "STANDARD", Name = "Standard",
            FulfillmentType = FulfillmentType.MachineProduced
        };
        product.ProductVariants.Add(variant);
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, ProductVariantId = variant.Id,
            ProductVariant = variant, Code = code, Name = name, Status = RecipeStatus.Active
        };
        foreach (var item in items)
        {
            var ingredient = new Ingredient { Id = Guid.NewGuid(), Code = item.IngredientCode, Name = item.IngredientCode };
            recipe.RecipeItems.Add(new RecipeItem
            {
                Id = Guid.NewGuid(), RecipeId = recipe.Id, IngredientId = ingredient.Id, Ingredient = ingredient,
                Quantity = item.Quantity, Unit = "gram", StepOrder = recipe.RecipeItems.Count + 1
            });
        }
        return recipe;
    }
}
