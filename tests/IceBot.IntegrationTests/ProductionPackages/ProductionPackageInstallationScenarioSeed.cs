using System.Security.Cryptography;
using Application.RobotConfiguration.Storage.Abstractions;
using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using Domain.Common.Enums;
using Domain.Devices.Catalog;
using Domain.Devices.Connectivity;
using Domain.Devices.ExecutionEndpoints;
using Domain.Devices.ExecutionEndpoints.Projections;
using Domain.Devices.Telemetry;
using Domain.Inventory.Entities;
using Domain.Inventory.Enums;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.ProductionPackages;
using Domain.ProductionExecution.Enums;
using Domain.RobotConfiguration.ArtifactContracts;
using Domain.RobotConfiguration.ArtifactTemplates;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using IceBot.IntegrationTests.Infrastructure;

namespace IceBot.IntegrationTests.ProductionPackages;

internal sealed record ProductionPackageInstallationScenario(
    Guid OrganizationId,
    Guid StoreId,
    Guid KioskId,
    Guid ExecutionEndpointId,
    Guid PackageId,
    Guid PackageVersionId,
    string ProductSourceKey,
    string TemplateStorageKey);

internal static class ProductionPackageInstallationScenarioSeed
{
    public static readonly byte[] ArtifactBytes = "return true"u8.ToArray();
    public static readonly string ArtifactChecksum =
        Convert.ToHexString(SHA256.HashData(ArtifactBytes)).ToLowerInvariant();

    public static async Task<ProductionPackageInstallationScenario> SeedAsync(
        IntegrationTestFixture fixture,
        IArtifactObjectStorage storage,
        Guid actorId)
    {
        await storage.EnsureReadyAsync();
        var templateStorageKey = $"robot-artifact-templates/{Guid.NewGuid():N}/base.lua";

        ProductionPackageInstallationScenario scenario;
        await using (var dbContext = fixture.CreateDbContext())
        {
            var organization = new Organization
            {
                Code = $"ORG-{Guid.NewGuid():N}",
                Name = "Full package installation organization",
                Status = EntityStatus.Active
            };
            var account = new Account
            {
                Id = actorId,
                UserName = $"package-{Guid.NewGuid():N}",
                Email = $"package-{Guid.NewGuid():N}@example.test",
                Status = AccountStatus.Active
            };
            var sourceProduct = new Product
            {
                Code = $"SOURCE-{Guid.NewGuid():N}",
                Name = "Source ice cream",
                ProductType = "IceCream",
                BasePrice = 30_000,
                Currency = "VND",
                IsAvailable = true
            };
            var store = new Store
            {
                OrganizationId = organization.Id,
                Code = $"STORE-{Guid.NewGuid():N}",
                Name = "Package installation store",
                Status = EntityStatus.Active
            };
            var kiosk = new Kiosk
            {
                OrganizationId = organization.Id,
                StoreId = store.Id,
                Code = $"KIOSK-{Guid.NewGuid():N}",
                Name = "Package installation kiosk",
                Status = KioskStatus.Active
            };
            var ingredient = new Ingredient
            {
                Code = $"CREAM-{Guid.NewGuid():N}",
                Name = "Ice cream base",
                Unit = "gram",
                IsActive = true
            };
            var sourceVariant = new ProductVariant
            {
                ProductId = sourceProduct.Id,
                Code = "STANDARD",
                Name = "Standard",
                FulfillmentType = FulfillmentType.MachineProduced,
                BasePrice = 30_000,
                Currency = "VND",
                IsAvailable = true
            };
            var sourceRecipe = new Recipe
            {
                ProductVariantId = sourceVariant.Id,
                Code = "DEFAULT",
                Name = "Default",
                Version = 1,
                IsDefault = true,
                YieldQuantity = 1,
                Unit = "serving"
            };
            var sourceRecipeItem = new RecipeItem
            {
                RecipeId = sourceRecipe.Id,
                IngredientId = ingredient.Id,
                Ingredient = ingredient,
                Quantity = 100,
                Unit = "gram",
                StepOrder = 1,
                IsOptional = false
            };
            sourceRecipe.RecipeItems.Add(sourceRecipeItem);
            sourceVariant.Recipes.Add(sourceRecipe);
            sourceProduct.ProductVariants.Add(sourceVariant);

            var contract = PublishedContract(actorId, ingredient.Code);
            var template = PublishedTemplate(contract, templateStorageKey);
            var package = ProductionPackage.Create($"PACKAGE-{Guid.NewGuid():N}", "Full installation package");
            var version = PublishedVersion(
                package.Id,
                sourceProduct.Id,
                sourceVariant.Id,
                sourceRecipe.Id,
                sourceRecipeItem.Id,
                ingredient,
                template,
                contract,
                actorId);

            var deviceType = new DeviceType
            {
                Code = $"DISPENSER-{Guid.NewGuid():N}",
                Name = "Package installation dispenser"
            };
            dbContext.DeviceTypes.Add(deviceType);
            await dbContext.SaveChangesAsync();
            var device = Device.CreateProvisioning(
                deviceType.Id,
                null,
                kiosk.Id,
                $"DEVICE-{Guid.NewGuid():N}",
                "Package installation dispenser",
                null,
                null,
                null,
                null);
            device.SetStatus(DeviceStatus.Online);
            var dispenserState = new IngredientDispenserState
            {
                DeviceId = device.Id,
                KioskId = kiosk.Id,
                IngredientId = ingredient.Id,
                ContainerCode = $"CONTAINER-{Guid.NewGuid():N}",
                CurrentLevelStatus = IngredientLevelStatus.Full,
                EstimatedQuantity = 1000,
                CapacityQuantity = 1000,
                Unit = "gram",
                LevelToQuantityProfileJson =
                    """[{"Level":1,"EstimatedQuantity":100},{"Level":2,"EstimatedQuantity":500},{"Level":3,"EstimatedQuantity":1000}]""",
                LastMeasuredAt = DateTimeOffset.UtcNow
            };
            var endpoint = KioskExecutionEndpoint.CreateProvisioning(
                kiosk.Id,
                $"EDGE-{Guid.NewGuid():N}",
                KioskExecutionProfile.FullEdge,
                ExecutionEndpointAuthenticationMode.MutualTls);
            endpoint.ReplaceSupportedRobotTargets([("FAIRINO_LUA_V1", "FR5", null)]);

            dbContext.AddRange(
                organization,
                account,
                store,
                kiosk,
                ingredient,
                sourceProduct,
                contract,
                template,
                package,
                version,
                device,
                dispenserState,
                endpoint);
            await dbContext.SaveChangesAsync();
            var credential = endpoint.ProvisionCredential($"cert-{Guid.NewGuid():N}", DateTimeOffset.UtcNow);
            endpoint.Activate(actorId, DateTimeOffset.UtcNow);
            dbContext.ExecutionEndpointCredentialBindings.Add(credential);
            var readiness = ExecutionEndpointReadinessProjection.Create(
                kiosk.Id,
                endpoint.Id,
                endpoint.FullEdgeRuntimeId!.Value,
                1,
                ExecutionReadinessState.Ready,
                ExecutionActivityState.Idle,
                ExecutionSafetyState.Safe,
                null,
                PhysicalOutputState.No,
                null,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow);
            dbContext.ExecutionEndpointReadinessProjections.Add(readiness);
            dbContext.ExecutionEndpointCapabilityProjections.Add(new ExecutionEndpointCapabilityProjection
            {
                ExecutionEndpointReadinessProjectionId = readiness.Id,
                CapabilityCode = "ROBOT_ARM",
                IsAvailable = true
            });
            await dbContext.SaveChangesAsync();
            scenario = new ProductionPackageInstallationScenario(
                organization.Id,
                store.Id,
                kiosk.Id,
                endpoint.Id,
                package.Id,
                version.Id,
                "ICE_CREAM",
                templateStorageKey);
        }

        await using var source = new MemoryStream(ArtifactBytes, writable: false);
        await storage.WriteImmutableAsync(
            new ArtifactObjectWriteRequest(
                templateStorageKey,
                "text/x-lua",
                ArtifactBytes.LongLength,
                ArtifactChecksum),
            source);
        return scenario;
    }

    private static RobotArtifactTechnicalContract PublishedContract(Guid actorId, string ingredientCode)
    {
        var contract = RobotArtifactTechnicalContract.CreateDraft(
            $"BASE-{Guid.NewGuid():N}", 1, "FAIRINO_LUA_V1", "FR5");
        contract.ReplaceDefinition(
            [new RobotArtifactEffectDefinition(
                "DISPENSE_CREAM",
                RobotArtifactEffectKind.Ingredient,
                ingredientCode,
                null,
                RobotArtifactQuantityMode.FixedInArtifact,
                100,
                "gram",
                "ROBOT_ARM")],
            []);
        contract.Publish(DateTimeOffset.UtcNow, actorId, parameterizedRuntimeSupported: false);
        return contract;
    }

    private static RobotArtifactTemplate PublishedTemplate(
        RobotArtifactTechnicalContract contract,
        string storageKey)
    {
        var template = RobotArtifactTemplate.CreateDraft(
            $"BASE-{Guid.NewGuid():N}",
            "Base",
            storageKey,
            "base.lua",
            ArtifactChecksum,
            "FAIRINO_LUA_V1",
            "FR5",
            ArtifactBytes.LongLength,
            DateTimeOffset.UtcNow,
            technicalContractId: contract.Id,
            technicalContractChecksum: contract.ContractChecksum);
        template.Publish();
        return template;
    }

    private static ProductionPackageVersion PublishedVersion(
        Guid packageId,
        Guid sourceProductId,
        Guid sourceVariantId,
        Guid sourceRecipeId,
        Guid sourceRecipeItemId,
        Ingredient ingredient,
        RobotArtifactTemplate template,
        RobotArtifactTechnicalContract contract,
        Guid actorId)
    {
        var product = ProductionPackageProductDefinition.Create("ICE_CREAM", sourceProductId, $$"""
            {
              "SchemaVersion": 2,
              "Product": {
                "Id": "{{sourceProductId}}",
                "Code": "ICE_CREAM",
                "Name": "Ice cream",
                "ProductType": "IceCream",
                "Currency": "VND",
                "Variants": [{
                  "Id": "{{sourceVariantId}}",
                  "Code": "STANDARD",
                  "Name": "Standard",
                  "FulfillmentType": 3,
                  "Recipes": [{
                    "Id": "{{sourceRecipeId}}",
                    "Code": "DEFAULT",
                    "Name": "Default",
                    "Version": 1,
                    "IsDefault": true,
                    "YieldQuantity": 1,
                    "Unit": "serving",
                    "InstructionsSchemaVersion": 1,
                    "Items": [{
                      "Id": "{{sourceRecipeItemId}}",
                      "IngredientId": "{{ingredient.Id}}",
                      "IngredientCode": "{{ingredient.Code}}",
                      "Quantity": 100,
                      "Unit": "gram",
                      "StepOrder": 1,
                      "IsOptional": false
                    }]
                  }]
                }],
                "OptionGroups": []
              }
            }
            """);
        var artifact = ProductionPackageArtifactDefinition.Create(
            "BASE", template.Id, template.Checksum, contract.Id, contract.ContractChecksum!);
        var program = ProductionPackageProgramBlueprint.Create(
            "STANDARD",
            "FAIRINO_LUA_V1",
            "FR5",
            [("BASE", "BASE", "DISPENSE_CREAM", "BASE", true, false, 1)]);
        var route = ProductionPackageRouteBlueprint.Create(
            "STANDARD",
            "ICE_CREAM",
            "STANDARD",
            "DEFAULT",
            [],
            "STANDARD",
            """{"schemaVersion":1,"requires":[{"code":"ROBOT_ARM"}]}""",
            1);
        var version = ProductionPackageVersion.CreateDraft(packageId, 1);
        version.ReplaceDefinition([product], [artifact], [program], [route]);
        version.Publish(DateTimeOffset.UtcNow, actorId);
        return version;
    }
}
