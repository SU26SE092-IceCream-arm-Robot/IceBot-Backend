using Application.ProductionPackages;
using Domain.Common;
using Domain.ProductionPackages;
using Domain.RobotConfiguration.ArtifactContracts;

namespace IceBot.UnitTests.ProductionPackages;

public sealed class ProductionPackageDefinitionValidatorTests
{
    [Fact]
    public void ValidateEffects_DoesNotAllowOneOptionArtifactToSatisfyAnotherOption()
    {
        var contract = RobotArtifactTechnicalContract.CreateDraft(
            "TOPPING_A", 1, "FAIRINO_LUA_V1", "FR5");
        contract.ReplaceDefinition(
        [
            new RobotArtifactEffectDefinition("ADD_TOPPING_A", RobotArtifactEffectKind.Ingredient,
                "NUTS", "TOPPING_A", RobotArtifactQuantityMode.FixedInArtifact, 10, "g", "TOPPING_STATION")
        ], []);
        var definition = ProductionPackageArtifactDefinition.Create(
            "TOPPING", Guid.NewGuid(), new string('a', 64), contract.Id, new string('b', 64));
        var blueprint = ProductionPackageProgramBlueprint.Create("PROGRAM", "FAIRINO_LUA_V1", "FR5",
            [("TOPPING", "TOPPING", "ADD_TOPPING_A", "OPTION", true, false, 1)]);

        var exception = Assert.Throws<DomainRuleException>(() =>
            ProductionPackageDefinitionValidator.ValidateEffects(
                blueprint.Slots.ToArray(),
                new Dictionary<string, ProductionPackageArtifactDefinition>(StringComparer.Ordinal)
                    { [definition.SourceKey] = definition },
                new Dictionary<Guid, RobotArtifactTechnicalContract> { [contract.Id] = contract },
                [new IngredientRequirement("NUTS", 10, "g", "TOPPING_B")]));

        Assert.Contains("does not match", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateSingleCapability_RejectsMultipleCapabilitiesInV1()
    {
        const string json = """
            {"schemaVersion":1,"requires":[{"code":"ROBOT_ARM"},{"code":"TOPPING_STATION"}]}
            """;

        var exception = Assert.Throws<DomainRuleException>(() =>
            ProductionPackageDefinitionValidator.ValidateSingleCapability(json));

        Assert.Contains("exactly one", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateOptionExecutionBoundary_RejectsCommercialOptionWithMachineEffect()
    {
        var exception = Assert.Throws<DomainRuleException>(() =>
            ProductionPackageDefinitionValidator.ValidateOptionExecutionBoundary(
                [new ProductionOptionExecutionInput("TOPPING",
                    Domain.Catalog.Enums.ProductOptionExecutionImpact.CommercialOnly, false)],
                new HashSet<string>(["TOPPING"], StringComparer.OrdinalIgnoreCase), "ROUTE"));

        Assert.Contains("commercial-only", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateOptionExecutionBoundary_RejectsProductionOptionWithoutProductionInput()
    {
        var exception = Assert.Throws<DomainRuleException>(() =>
            ProductionPackageDefinitionValidator.ValidateOptionExecutionBoundary(
                [new ProductionOptionExecutionInput("TOPPING",
                    Domain.Catalog.Enums.ProductOptionExecutionImpact.ProductionAffecting, false)],
                new HashSet<string>(StringComparer.OrdinalIgnoreCase), "ROUTE"));

        Assert.Contains("no deterministic production input", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveOptionExecutionImpacts_UsesArtifactEffectForLegacyV1Snapshot()
    {
        var optionId = Guid.NewGuid();
        var product = ProductionPackageProductDefinition.Create("PRODUCT", Guid.NewGuid(), $$"""
            {
              "SchemaVersion": 1,
              "Product": {
                "Id": "{{Guid.NewGuid()}}",
                "Code": "PRODUCT",
                "Name": "Product",
                "OptionGroups": [{
                  "Id": 1,
                  "Code": "TOPPING",
                  "Name": "Topping",
                  "Options": [{
                    "Id": "{{optionId}}",
                    "Code": "OREO",
                    "Name": "Oreo",
                    "IngredientRequirements": []
                  }]
                }]
              }
            }
            """);
        var contract = RobotArtifactTechnicalContract.CreateDraft("OREO", 1, "FAIRINO_LUA_V1", "FR5");
        contract.ReplaceDefinition(
        [
            new RobotArtifactEffectDefinition("ADD_OREO", RobotArtifactEffectKind.Option, null, "OREO",
                RobotArtifactQuantityMode.None, null, null, "TOPPING_STATION")
        ], []);
        var artifact = ProductionPackageArtifactDefinition.Create(
            "OREO", Guid.NewGuid(), new string('a', 64), contract.Id, new string('b', 64));
        var program = ProductionPackageProgramBlueprint.Create("PROGRAM", "FAIRINO_LUA_V1", "FR5",
            [("OREO", "OREO", "ADD_OREO", "OPTION", false, false, 1)]);
        var route = ProductionPackageRouteBlueprint.Create(
            "ROUTE", "PRODUCT", "VARIANT", "RECIPE", ["OREO"], "PROGRAM",
            """{"schemaVersion":1,"requires":[{"code":"TOPPING_STATION"}]}""", 1);
        var version = ProductionPackageVersion.CreateDraft(Guid.NewGuid(), 1);
        version.ReplaceDefinition([product], [artifact], [program], [route]);

        var impacts = ProductionPackageDefinitionValidator.ResolveOptionExecutionImpacts(version, [contract]);

        Assert.Equal(Domain.Catalog.Enums.ProductOptionExecutionImpact.ProductionAffecting, impacts[optionId]);
    }

    [Fact]
    public void ReplaceDefinition_RejectsDuplicateSourceProductIdentity()
    {
        var sourceProductId = Guid.NewGuid();
        var first = ProductionPackageProductDefinition.Create(
            "FIRST", sourceProductId, LegacyProductJson("FIRST", Guid.NewGuid(), "OPTION"));
        var second = ProductionPackageProductDefinition.Create(
            "SECOND", sourceProductId, LegacyProductJson("SECOND", Guid.NewGuid(), "OPTION"));
        var artifact = ProductionPackageArtifactDefinition.Create(
            "ACTION", Guid.NewGuid(), new string('a', 64), Guid.NewGuid(), new string('b', 64));
        var program = ProductionPackageProgramBlueprint.Create("PROGRAM", "FAIRINO_LUA_V1", "FR5",
            [("ACTION", "ACTION", "ACTION", "BASE", true, false, 1)]);
        var route = ProductionPackageRouteBlueprint.Create(
            "ROUTE", "FIRST", "VARIANT", "RECIPE", [], "PROGRAM",
            """{"schemaVersion":1,"requires":[{"code":"ROBOT_ARM"}]}""", 1);
        var version = ProductionPackageVersion.CreateDraft(Guid.NewGuid(), 1);

        var exception = Assert.Throws<DomainRuleException>(() =>
            version.ReplaceDefinition([first, second], [artifact], [program], [route]));

        Assert.Contains("product sources", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveOptionExecutionImpacts_ScopesLegacyEffectsToOwningProductRoutes()
    {
        var firstOptionId = Guid.NewGuid();
        var secondOptionId = Guid.NewGuid();
        var firstProduct = ProductionPackageProductDefinition.Create(
            "FIRST", Guid.NewGuid(), LegacyProductJson("FIRST", firstOptionId, "EXTRA"));
        var secondProduct = ProductionPackageProductDefinition.Create(
            "SECOND", Guid.NewGuid(), LegacyProductJson("SECOND", secondOptionId, "EXTRA"));

        var optionContract = RobotArtifactTechnicalContract.CreateDraft("OPTION", 1, "FAIRINO_LUA_V1", "FR5");
        optionContract.ReplaceDefinition(
        [
            new RobotArtifactEffectDefinition("ADD_EXTRA", RobotArtifactEffectKind.Option, null, "EXTRA",
                RobotArtifactQuantityMode.None, null, null, "ROBOT_ARM")
        ], []);
        var baseContract = RobotArtifactTechnicalContract.CreateDraft("BASE", 1, "FAIRINO_LUA_V1", "FR5");
        baseContract.ReplaceDefinition(
        [
            new RobotArtifactEffectDefinition("MAKE_BASE", RobotArtifactEffectKind.Motion, null, null,
                RobotArtifactQuantityMode.None, null, null, "ROBOT_ARM")
        ], []);
        var optionArtifact = ProductionPackageArtifactDefinition.Create(
            "OPTION", Guid.NewGuid(), new string('a', 64), optionContract.Id, new string('b', 64));
        var baseArtifact = ProductionPackageArtifactDefinition.Create(
            "BASE", Guid.NewGuid(), new string('c', 64), baseContract.Id, new string('d', 64));
        var optionProgram = ProductionPackageProgramBlueprint.Create("OPTION_PROGRAM", "FAIRINO_LUA_V1", "FR5",
            [("OPTION", "OPTION", "ADD_EXTRA", "OPTION", false, false, 1)]);
        var baseProgram = ProductionPackageProgramBlueprint.Create("BASE_PROGRAM", "FAIRINO_LUA_V1", "FR5",
            [("BASE", "BASE", "MAKE_BASE", "BASE", true, false, 1)]);
        var firstRoute = ProductionPackageRouteBlueprint.Create(
            "FIRST_ROUTE", "FIRST", "VARIANT", "RECIPE", ["EXTRA"], "OPTION_PROGRAM",
            """{"schemaVersion":1,"requires":[{"code":"ROBOT_ARM"}]}""", 1);
        var secondRoute = ProductionPackageRouteBlueprint.Create(
            "SECOND_ROUTE", "SECOND", "VARIANT", "RECIPE", [], "BASE_PROGRAM",
            """{"schemaVersion":1,"requires":[{"code":"ROBOT_ARM"}]}""", 2);
        var version = ProductionPackageVersion.CreateDraft(Guid.NewGuid(), 1);
        version.ReplaceDefinition([firstProduct, secondProduct], [optionArtifact, baseArtifact],
            [optionProgram, baseProgram], [firstRoute, secondRoute]);

        var impacts = ProductionPackageDefinitionValidator.ResolveOptionExecutionImpacts(
            version, [optionContract, baseContract]);

        Assert.Equal(Domain.Catalog.Enums.ProductOptionExecutionImpact.ProductionAffecting, impacts[firstOptionId]);
        Assert.Equal(Domain.Catalog.Enums.ProductOptionExecutionImpact.CommercialOnly, impacts[secondOptionId]);
    }

    [Fact]
    public void Validate_AllowsSameRecipeCodeInDifferentVariants()
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
        var contract = PublishedContract("BASE", "MAKE_BASE", RobotArtifactEffectKind.Motion, null);
        var artifact = ProductionPackageArtifactDefinition.Create(
            "BASE", Guid.NewGuid(), new string('a', 64), contract.Id, contract.ContractChecksum!);
        var program = ProductionPackageProgramBlueprint.Create("PROGRAM", "FAIRINO_LUA_V1", "FR5",
            [("BASE", "BASE", "MAKE_BASE", "BASE", true, false, 1)]);
        var small = ProductionPackageRouteBlueprint.Create("SMALL_ROUTE", "PRODUCT", "SMALL", "DEFAULT", [],
            "PROGRAM", """{"schemaVersion":1,"requires":[{"code":"ROBOT_ARM"}]}""", 1);
        var large = ProductionPackageRouteBlueprint.Create("LARGE_ROUTE", "PRODUCT", "LARGE", "DEFAULT", [],
            "PROGRAM", """{"schemaVersion":1,"requires":[{"code":"ROBOT_ARM"}]}""", 2);
        var version = ProductionPackageVersion.CreateDraft(Guid.NewGuid(), 1);
        version.ReplaceDefinition([product], [artifact], [program], [small, large]);

        ProductionPackageDefinitionValidator.Validate(version, [contract]);
    }

    [Fact]
    public void Validate_AppliesProductionOptionOnlyToRoutesThatSupportIt()
    {
        var optionId = Guid.NewGuid();
        var product = ProductionPackageProductDefinition.Create("PRODUCT", Guid.NewGuid(), $$"""
            {
              "SchemaVersion": 2,
              "Product": {
                "Id": "{{Guid.NewGuid()}}", "Code": "PRODUCT", "Name": "Product",
                "Variants": [{ "Id": "{{Guid.NewGuid()}}", "Code": "STANDARD", "Name": "Standard",
                  "Recipes": [{ "Id": "{{Guid.NewGuid()}}", "Code": "DEFAULT", "Name": "Default", "Version": 1, "Items": [] }] }],
                "OptionGroups": [{ "Id": 1, "Code": "OPTIONS", "Name": "Options",
                  "Options": [{ "Id": "{{optionId}}", "Code": "EXTRA", "Name": "Extra",
                    "ExecutionImpact": 1, "IngredientRequirements": [] }] }]
              }
            }
            """);
        var baseContract = PublishedContract("BASE", "MAKE_BASE", RobotArtifactEffectKind.Motion, null);
        var optionContract = PublishedContract("EXTRA", "ADD_EXTRA", RobotArtifactEffectKind.Option, "EXTRA");
        var baseArtifact = ProductionPackageArtifactDefinition.Create(
            "BASE", Guid.NewGuid(), new string('a', 64), baseContract.Id, baseContract.ContractChecksum!);
        var optionArtifact = ProductionPackageArtifactDefinition.Create(
            "EXTRA", Guid.NewGuid(), new string('b', 64), optionContract.Id, optionContract.ContractChecksum!);
        var baseProgram = ProductionPackageProgramBlueprint.Create("BASE_PROGRAM", "FAIRINO_LUA_V1", "FR5",
            [("BASE", "BASE", "MAKE_BASE", "BASE", true, false, 1)]);
        var optionProgram = ProductionPackageProgramBlueprint.Create("OPTION_PROGRAM", "FAIRINO_LUA_V1", "FR5",
            [("EXTRA", "EXTRA", "ADD_EXTRA", "OPTION", false, false, 1)]);
        var baseRoute = ProductionPackageRouteBlueprint.Create("BASE_ROUTE", "PRODUCT", "STANDARD", "DEFAULT", [],
            "BASE_PROGRAM", """{"schemaVersion":1,"requires":[{"code":"ROBOT_ARM"}]}""", 1);
        var optionRoute = ProductionPackageRouteBlueprint.Create("OPTION_ROUTE", "PRODUCT", "STANDARD", "DEFAULT", ["EXTRA"],
            "OPTION_PROGRAM", """{"schemaVersion":1,"requires":[{"code":"ROBOT_ARM"}]}""", 2);
        var version = ProductionPackageVersion.CreateDraft(Guid.NewGuid(), 1);
        version.ReplaceDefinition([product], [baseArtifact, optionArtifact], [baseProgram, optionProgram],
            [baseRoute, optionRoute]);

        ProductionPackageDefinitionValidator.Validate(version, [baseContract, optionContract]);
    }

    private static RobotArtifactTechnicalContract PublishedContract(string code, string effectCode,
        RobotArtifactEffectKind effectKind, string? optionCode)
    {
        var contract = RobotArtifactTechnicalContract.CreateDraft(code, 1, "FAIRINO_LUA_V1", "FR5");
        contract.ReplaceDefinition(
        [
            new RobotArtifactEffectDefinition(effectCode, effectKind, null, optionCode,
                RobotArtifactQuantityMode.None, null, null, "ROBOT_ARM")
        ], []);
        contract.Publish(DateTimeOffset.UtcNow, null);
        return contract;
    }

    private static string LegacyProductJson(string productCode, Guid optionId, string optionCode) => $$"""
        {
          "SchemaVersion": 1,
          "Product": {
            "Id": "{{Guid.NewGuid()}}",
            "Code": "{{productCode}}",
            "Name": "{{productCode}}",
            "OptionGroups": [{
              "Id": 1,
              "Code": "OPTIONS",
              "Name": "Options",
              "Options": [{
                "Id": "{{optionId}}",
                "Code": "{{optionCode}}",
                "Name": "{{optionCode}}",
                "IngredientRequirements": []
              }]
            }]
          }
        }
        """;

}
