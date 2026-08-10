using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.RobotConfiguration.AuthoringImports;
using Application.RobotConfiguration.AuthoringImports.Composition;
using Application.RobotConfiguration.Programs.Abstractions;
using Application.RobotConfiguration.Programs.Commands;
using Application.Shared.Ownership;
using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using Domain.RobotConfiguration.ArtifactContracts;
using Domain.RobotConfiguration.Artifacts;
using Domain.RobotConfiguration.AuthoringImports;
using Domain.RobotConfiguration.Programs;
using Domain.Tenants.Enums;
using IceBot.UnitTests.TestSupport;
using NSubstitute;
using Application.Shared.Concurrency;

namespace IceBot.UnitTests.RobotConfiguration;

public sealed class RobotAuthoringCompositionTests
{
    [Fact]
    public async Task Preview_ResolvesOptionIngredientDeclaredAsConditionalIngredientEffect()
    {
        var organizationId = Guid.NewGuid();
        var program = RobotProgram.CreateDraft("ADD_OREO", "Add Oreo",
            TenantScopeType.Organization, organizationId);
        var artifact = RobotArtifact.CreateDraft(organizationId, "ADD_OREO", "Add Oreo",
            "robot-artifacts/add-oreo.lua", "add-oreo.lua", new string('a', 64), "FAIRINO_LUA_V1", "FR5",
            100, DateTimeOffset.UtcNow);
        var contract = RobotArtifactTechnicalContract.CreateDraft("ADD_OREO", 1, "FAIRINO_LUA_V1", "FR5",
            organizationId, schemaVersion: 2);
        contract.ReplaceDefinition(
            [new RobotArtifactEffectDefinition("ADD_OREO", RobotArtifactEffectKind.Ingredient,
                "OREO_CRUMB", "OREO", RobotArtifactQuantityMode.FixedInArtifact, 10, "gram", "TOPPING_STATION")],
            [new RobotArtifactOrderingConstraintDefinition(RobotArtifactOrderingConstraintType.Phase, "OPTION", 1)]);

        var importSession = RobotAuthoringImport.Create(organizationId, null, null, null, Guid.NewGuid(),
            new string('b', 64), "import-key", 1, "ADD_OREO", "Add Oreo", "FAIRINO_LUA_V1",
            "FR5", "robot-authoring-imports/import.zip", Guid.NewGuid());
        importSession.AddItem("ADD_OREO", "add-oreo.lua", "add-oreo.icebot.json", 1,
            artifact.Checksum, new string('c', 64));
        Assert.Single(importSession.Items).MarkResolved(artifact.Id, contract.Id, true);
        program.AddArtifact(artifact.Id, 1);
        importSession.MarkValidated("{}", DateTimeOffset.UtcNow, Guid.NewGuid());
        importSession.MarkApplied(program.Id, DateTimeOffset.UtcNow, Guid.NewGuid());

        var ingredient = new Ingredient { Id = Guid.NewGuid(), Code = "OREO_CRUMB", Name = "Oreo crumb" };
        var product = new Product { Id = Guid.NewGuid(), OrganizationId = organizationId, Code = "ICE_CREAM", Name = "Ice cream" };
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(), ProductId = product.Id, Product = product, Code = "STANDARD", Name = "Standard",
            FulfillmentType = FulfillmentType.MachineProduced
        };
        product.ProductVariants.Add(variant);
        var optionGroup = new OptionGroup { Id = 1, ProductId = product.Id, Product = product, Code = "TOPPINGS", Name = "Toppings" };
        var option = new ProductOption
        {
            Id = Guid.NewGuid(), OptionGroupId = optionGroup.Id, OptionGroup = optionGroup,
            Code = "OREO", Name = "Oreo", ExecutionImpact = ProductOptionExecutionImpact.ProductionAffecting
        };
        option.IngredientRequirements.Add(new ProductOptionIngredientRequirement
        {
            Id = Guid.NewGuid(), ProductOptionId = option.Id, ProductOption = option,
            IngredientId = ingredient.Id, Ingredient = ingredient, Quantity = 10, Unit = "gram",
            RequiredWorkcellCapabilityCode = "TOPPING_STATION"
        });
        optionGroup.ProductOptions.Add(option);
        product.OptionGroups.Add(optionGroup);
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, ProductVariantId = variant.Id,
            ProductVariant = variant, Code = "STANDARD", Name = "Standard", Status = RecipeStatus.Published
        };

        var importStore = Substitute.For<IRobotAuthoringImportStore>();
        importStore.GetAsync(organizationId, importSession.Id, false, Arg.Any<CancellationToken>()).Returns(importSession);
        var compositionStore = Substitute.For<IRobotAuthoringCompositionStore>();
        compositionStore.GetRecipeAsync(organizationId, recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);
        compositionStore.GetProgramAsync(organizationId, program.Id, Arg.Any<CancellationToken>()).Returns(program);
        compositionStore.GetArtifactsAsync(organizationId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([artifact]);
        compositionStore.GetContractsAsync(organizationId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([contract]);
        var replacementHandler = new ReplaceRobotProgramArtifactsCommandHandler(
            Substitute.For<IRobotProgramStore>(), Substitute.For<IRobotArtifactStore>(),
            Substitute.For<ITechnicalResourceMutationPolicy>(), InlineTechnicalResourceMutationCoordinator.Instance);
        var handlers = new RobotAuthoringCompositionHandlers(importStore, compositionStore, replacementHandler);

        var result = await handlers.PreviewAsync(new PreviewRobotAuthoringCompositionQuery(
            TestData.SystemAdmin(), organizationId, importSession.Id, recipe.Id, ["OREO"]), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.True(result.Data!.CanConfirm);
        Assert.Equal("Resolved", Assert.Single(result.Data.Requirements).Status);
        Assert.Equal("OREO", Assert.Single(result.Data.ProposedArtifacts).RequiredOptionCode);
    }

    [Theory]
    [InlineData(2, true)]
    [InlineData(1, true)]
    public async Task Preview_TechnicalDeclarationNeverBlocksOperatorConfirmation(int schemaVersion, bool canConfirm)
    {
        var organizationId = Guid.NewGuid();
        var program = RobotProgram.CreateDraft("MAKE_ICE_CREAM", "Make ice cream",
            TenantScopeType.Organization, organizationId);
        var artifact = RobotArtifact.CreateDraft(organizationId, "DISPENSE", "Dispense",
            "robot-artifacts/dispense.lua", "dispense.lua", new string('a', 64), "FAIRINO_LUA_V1", "FR5",
            100, DateTimeOffset.UtcNow);
        var contract = RobotArtifactTechnicalContract.CreateDraft("DISPENSE", 1, "FAIRINO_LUA_V1", "FR5",
            organizationId, schemaVersion: schemaVersion);
        contract.ReplaceDefinition(
            [new RobotArtifactEffectDefinition("DISPENSE_BASE", RobotArtifactEffectKind.Ingredient,
                "ICE_CREAM_BASE", null, RobotArtifactQuantityMode.FixedInArtifact, 100, "gram", "DISPENSER")],
            [new RobotArtifactOrderingConstraintDefinition(RobotArtifactOrderingConstraintType.Phase, "BASE", 1)]);

        var importSession = RobotAuthoringImport.Create(organizationId, null, null, null, Guid.NewGuid(),
            new string('b', 64), "import-key", 1, "MAKE_ICE_CREAM", "Make ice cream", "FAIRINO_LUA_V1",
            "FR5", "robot-authoring-imports/import.zip", Guid.NewGuid());
        importSession.AddItem("DISPENSE", "dispense.lua", "dispense.icebot.json", 1,
            artifact.Checksum, new string('c', 64));
        Assert.Single(importSession.Items).MarkResolved(artifact.Id, contract.Id, true);
        program.AddArtifact(artifact.Id, 1);
        importSession.MarkValidated("{}", DateTimeOffset.UtcNow, Guid.NewGuid());
        importSession.MarkApplied(program.Id, DateTimeOffset.UtcNow, Guid.NewGuid());

        var ingredient = new Ingredient { Id = Guid.NewGuid(), Code = "ICE_CREAM_BASE", Name = "Base" };
        var product = new Product { Id = Guid.NewGuid(), OrganizationId = organizationId, Code = "ICE_CREAM", Name = "Ice cream" };
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(), ProductId = product.Id, Product = product, Code = "STANDARD", Name = "Standard",
            FulfillmentType = FulfillmentType.MachineProduced
        };
        product.ProductVariants.Add(variant);
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, ProductVariantId = variant.Id,
            ProductVariant = variant, Code = "STANDARD", Name = "Standard", Status = RecipeStatus.Published
        };
        recipe.RecipeItems.Add(new RecipeItem
        {
            Id = Guid.NewGuid(), RecipeId = recipe.Id, IngredientId = ingredient.Id, Ingredient = ingredient,
            Quantity = 100, Unit = "gram", StepOrder = 1
        });

        var importStore = Substitute.For<IRobotAuthoringImportStore>();
        importStore.GetAsync(organizationId, importSession.Id, false, Arg.Any<CancellationToken>())
            .Returns(importSession);
        var compositionStore = Substitute.For<IRobotAuthoringCompositionStore>();
        compositionStore.GetRecipeAsync(organizationId, recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);
        compositionStore.GetProgramAsync(organizationId, program.Id, Arg.Any<CancellationToken>()).Returns(program);
        compositionStore.GetArtifactsAsync(organizationId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([artifact]);
        compositionStore.GetContractsAsync(organizationId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([contract]);
        var replacementHandler = new ReplaceRobotProgramArtifactsCommandHandler(
            Substitute.For<IRobotProgramStore>(), Substitute.For<IRobotArtifactStore>(),
            Substitute.For<ITechnicalResourceMutationPolicy>(),
            InlineTechnicalResourceMutationCoordinator.Instance);
        var handlers = new RobotAuthoringCompositionHandlers(importStore, compositionStore, replacementHandler);

        var result = await handlers.PreviewAsync(new PreviewRobotAuthoringCompositionQuery(
            TestData.SystemAdmin(), organizationId, importSession.Id, recipe.Id, []), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(canConfirm, result.Data!.CanConfirm);
        Assert.Empty(result.Data.Blockers);
        Assert.Equal("DISPENSE", Assert.Single(result.Data.ProposedArtifacts).ArtifactCode);
        if (schemaVersion >= 2)
            Assert.Equal("DISPENSER", Assert.Single(result.Data.SuggestedCapabilityCodes));
        else
            Assert.Contains(result.Data.Warnings, warning => warning.Code == "BLACK_BOX_ARTIFACT_INCLUDED");
    }

    [Fact]
    public async Task Preview_UsesCurrentDraftProgramOrderAsTheManualTieBreaker()
    {
        var organizationId = Guid.NewGuid();
        var artifactA = RobotArtifact.CreateDraft(organizationId, "FIRST", "First",
            "robot-artifacts/first.lua", "first.lua", new string('a', 64), "FAIRINO_LUA_V1", "FR5",
            100, DateTimeOffset.UtcNow);
        var artifactB = RobotArtifact.CreateDraft(organizationId, "SECOND", "Second",
            "robot-artifacts/second.lua", "second.lua", new string('b', 64), "FAIRINO_LUA_V1", "FR5",
            100, DateTimeOffset.UtcNow);
        var contractA = RobotArtifactTechnicalContract.CreateDraft("FIRST", 1, "FAIRINO_LUA_V1", "FR5",
            organizationId, schemaVersion: 2);
        var contractB = RobotArtifactTechnicalContract.CreateDraft("SECOND", 1, "FAIRINO_LUA_V1", "FR5",
            organizationId, schemaVersion: 2);
        var program = RobotProgram.CreateDraft("MAKE_ICE_CREAM", "Make ice cream",
            TenantScopeType.Organization, organizationId);
        program.AddArtifact(artifactB.Id, 1);
        program.AddArtifact(artifactA.Id, 2);

        var importSession = RobotAuthoringImport.Create(organizationId, null, null, null, Guid.NewGuid(),
            new string('c', 64), "import-key", 1, "MAKE_ICE_CREAM", "Make ice cream", "FAIRINO_LUA_V1",
            "FR5", "robot-authoring-imports/import.zip", Guid.NewGuid());
        importSession.AddItem("FIRST", "first.lua", "first.icebot.json", 1, artifactA.Checksum, new string('d', 64));
        importSession.AddItem("SECOND", "second.lua", "second.icebot.json", 2, artifactB.Checksum, new string('e', 64));
        importSession.Items.Single(item => item.ArtifactCode == "FIRST").MarkResolved(artifactA.Id, contractA.Id, true);
        importSession.Items.Single(item => item.ArtifactCode == "SECOND").MarkResolved(artifactB.Id, contractB.Id, true);
        importSession.MarkValidated("{}", DateTimeOffset.UtcNow, Guid.NewGuid());
        importSession.MarkApplied(program.Id, DateTimeOffset.UtcNow, Guid.NewGuid());

        var product = new Product { Id = Guid.NewGuid(), OrganizationId = organizationId, Code = "ICE_CREAM", Name = "Ice cream" };
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(), ProductId = product.Id, Product = product, Code = "STANDARD", Name = "Standard",
            FulfillmentType = FulfillmentType.MachineProduced
        };
        product.ProductVariants.Add(variant);
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, ProductVariantId = variant.Id,
            ProductVariant = variant, Code = "STANDARD", Name = "Standard", Status = RecipeStatus.Published
        };

        var importStore = Substitute.For<IRobotAuthoringImportStore>();
        importStore.GetAsync(organizationId, importSession.Id, false, Arg.Any<CancellationToken>()).Returns(importSession);
        var compositionStore = Substitute.For<IRobotAuthoringCompositionStore>();
        compositionStore.GetRecipeAsync(organizationId, recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);
        compositionStore.GetProgramAsync(organizationId, program.Id, Arg.Any<CancellationToken>()).Returns(program);
        compositionStore.GetArtifactsAsync(organizationId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([artifactA, artifactB]);
        compositionStore.GetContractsAsync(organizationId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([contractA, contractB]);
        var replacementHandler = new ReplaceRobotProgramArtifactsCommandHandler(
            Substitute.For<IRobotProgramStore>(), Substitute.For<IRobotArtifactStore>(),
            Substitute.For<ITechnicalResourceMutationPolicy>(), InlineTechnicalResourceMutationCoordinator.Instance);
        var handlers = new RobotAuthoringCompositionHandlers(importStore, compositionStore, replacementHandler);

        var result = await handlers.PreviewAsync(new PreviewRobotAuthoringCompositionQuery(
            TestData.SystemAdmin(), organizationId, importSession.Id, recipe.Id, []), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.True(result.Data!.CanConfirm);
        Assert.Equal(["SECOND", "FIRST"], result.Data.ProposedArtifacts.Select(item => item.ArtifactCode));
    }
}
