using System.Text.Json;
using Application.Tenants;
using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using Domain.Common;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionPackages;
using Domain.RobotConfiguration.ArtifactContracts;
using Domain.RobotConfiguration.ArtifactTemplates;
using Domain.RobotConfiguration.Artifacts;
using Domain.RobotConfiguration.Programs;
using Domain.RobotConfiguration.Programs.Manifests;

namespace Application.ProductionPackages.Installation;

internal static class ProductionPackageInstallationMaterializer
{
    internal static IReadOnlyCollection<MaterializedProduct> MaterializeProducts(
        InstallProductionPackageCommand command, ProductionPackageInstallation installation,
        IReadOnlyCollection<ProductionPackageProductDefinition> definitions,
        IReadOnlyDictionary<Guid, ProductOptionExecutionImpact> optionImpacts)
    {
        var now = DateTimeOffset.UtcNow;
        var scopeType = TenantScopeResolver.Resolve(command.StoreId, command.KioskId);
        var result = new List<MaterializedProduct>();
        foreach (var definition in definitions)
        {
            var source = ProductionPackageProductSnapshotCodec.Deserialize(definition.ProductSnapshotJson).Product;
            var product = new Product
            {
                OrganizationId = command.OrganizationId,
                StoreId = command.StoreId,
                KioskId = command.KioskId,
                TemplateProductId = source.Id,
                CategoryId = source.CategoryId,
                Code = ApplyIdentitySuffix(source.Code, command.MaterializationIdentitySuffix),
                Name = source.Name,
                DisplayName = source.DisplayName,
                Description = source.Description,
                ProductType = source.ProductType,
                BasePrice = source.BasePrice,
                Currency = source.Currency,
                IsAvailable = false,
                PreparationTimeSeconds = source.PreparationTimeSeconds,
                ImageAssetId = source.ImageAssetId,
                ImageAltText = source.ImageAltText,
                ScopeType = scopeType,
                CreatedAt = now,
                CreatedByAccountId = command.UserContext.AccountId
            };
            var variants = new Dictionary<Guid, ProductVariant>();
            var recipesByCode = new Dictionary<string, Recipe>(StringComparer.Ordinal);
            var recipeRequirements = new Dictionary<string, IReadOnlyCollection<IngredientQuantityRequirement>>(StringComparer.Ordinal);
            var optionRequirementsByCode = new Dictionary<string, IReadOnlyCollection<IngredientQuantityRequirement>>(
                StringComparer.Ordinal);
            foreach (var variantSource in source.Variants)
            {
                var variant = new ProductVariant
                {
                    ProductId = product.Id,
                    Code = variantSource.Code,
                    Name = variantSource.Name,
                    DisplayName = variantSource.DisplayName,
                    Description = variantSource.Description,
                    VariantType = variantSource.VariantType,
                    FulfillmentType = variantSource.FulfillmentType,
                    SizeCode = variantSource.SizeCode,
                    BasePrice = variantSource.BasePrice,
                    Currency = source.Currency,
                    IsAvailable = false,
                    DisplayOrder = variantSource.DisplayOrder,
                    PreparationTimeSeconds = variantSource.PreparationTimeSeconds,
                    ImageAssetId = variantSource.ImageAssetId,
                    ImageAltText = variantSource.ImageAltText,
                    CreatedAt = now,
                    CreatedByAccountId = command.UserContext.AccountId
                };
                product.ProductVariants.Add(variant);
                variants[variantSource.Id] = variant;
                foreach (var recipeSource in variantSource.Recipes)
                {
                    var currentRecipeRequirements = new List<IngredientQuantityRequirement>();
                    var recipe = new Recipe
                    {
                        OrganizationId = command.OrganizationId,
                        StoreId = command.StoreId,
                        KioskId = command.KioskId,
                        ProductVariantId = variant.Id,
                        TemplateRecipeId = recipeSource.Id,
                        Code = recipeSource.Code,
                        Name = recipeSource.Name,
                        Version = 1,
                        Status = RecipeStatus.Draft,
                        IsDefault = recipeSource.IsDefault,
                        YieldQuantity = recipeSource.YieldQuantity,
                        Unit = recipeSource.Unit,
                        EstimatedDurationSeconds = recipeSource.EstimatedDurationSeconds,
                        EffectiveFrom = recipeSource.EffectiveFrom,
                        EffectiveTo = recipeSource.EffectiveTo,
                        InstructionsSchemaVersion = recipeSource.InstructionsSchemaVersion,
                        InstructionsJson = recipeSource.InstructionsJson,
                        ScopeType = scopeType,
                        CreatedAt = now,
                        CreatedByAccountId = command.UserContext.AccountId
                    };
                    foreach (var item in recipeSource.Items)
                    {
                        recipe.RecipeItems.Add(new RecipeItem
                        {
                            RecipeId = recipe.Id,
                            IngredientId = item.IngredientId,
                            Quantity = item.Quantity,
                            Unit = item.Unit,
                            StepOrder = item.StepOrder,
                            IsOptional = item.IsOptional,
                            Notes = item.Notes,
                            CreatedAt = now,
                            CreatedByAccountId = command.UserContext.AccountId
                        });
                        currentRecipeRequirements.Add(new IngredientQuantityRequirement(item.IngredientCode, item.Quantity, item.Unit, null));
                    }
                    variant.Recipes.Add(recipe);
                    var recipeKey = RecipeLookupKey(variant.Code, recipe.Code);
                    if (!recipesByCode.TryAdd(recipeKey, recipe))
                        throw new DomainRuleException("Package Product snapshot contains duplicate Recipe codes within one variant.");
                    recipeRequirements.Add(recipeKey, currentRecipeRequirements);
                    installation.AddMaterialization(ProductionPackageResourceKind.Recipe,
                        $"{definition.SourceKey}:VARIANT:{variantSource.Code}:RECIPE:{recipeSource.Code}",
                        recipe.Id.ToString("D"));
                }
                installation.AddMaterialization(ProductionPackageResourceKind.ProductVariant,
                    $"{definition.SourceKey}:VARIANT:{variantSource.Code}", variant.Id.ToString("D"));
            }

            foreach (var groupSource in source.OptionGroups)
            {
                var group = new OptionGroup
                {
                    ProductId = product.Id,
                    Code = groupSource.Code,
                    Name = groupSource.Name,
                    Description = groupSource.Description,
                    SelectionType = groupSource.SelectionType,
                    MinSelections = groupSource.MinSelections,
                    MaxSelections = groupSource.MaxSelections,
                    IsRequired = groupSource.IsRequired,
                    IsActive = groupSource.IsActive,
                    DisplayOrder = groupSource.DisplayOrder,
                    CreatedAt = now,
                    CreatedByAccountId = command.UserContext.AccountId
                };
                foreach (var optionSource in groupSource.Options)
                {
                    var currentOptionRequirements = new List<IngredientQuantityRequirement>();
                    var option = new ProductOption
                    {
                        OptionGroupId = group.Id,
                        TemplateProductOptionId = optionSource.Id,
                        Code = optionSource.Code,
                        Name = optionSource.Name,
                        Description = optionSource.Description,
                        PriceDelta = optionSource.PriceDelta,
                        ExecutionImpact = optionImpacts[optionSource.Id],
                        IsDefault = optionSource.IsDefault,
                        IsAvailable = false,
                        DisplayOrder = optionSource.DisplayOrder,
                        CreatedAt = now,
                        CreatedByAccountId = command.UserContext.AccountId
                    };
                    foreach (var requirement in optionSource.IngredientRequirements)
                    {
                        option.IngredientRequirements.Add(new ProductOptionIngredientRequirement
                        {
                            IngredientId = requirement.IngredientId,
                            Quantity = requirement.Quantity,
                            Unit = requirement.Unit,
                            RequiredWorkcellCapabilityCode = requirement.RequiredWorkcellCapabilityCode,
                            CreatedAt = now,
                            CreatedByAccountId = command.UserContext.AccountId
                        });
                        currentOptionRequirements.Add(new IngredientQuantityRequirement(
                            requirement.IngredientCode, requirement.Quantity, requirement.Unit,
                            optionSource.Code.Trim().ToUpperInvariant()));
                    }
                    optionRequirementsByCode.Add(optionSource.Code.Trim().ToUpperInvariant(), currentOptionRequirements);
                    group.ProductOptions.Add(option);
                    installation.AddMaterialization(ProductionPackageResourceKind.ProductOption,
                        $"{definition.SourceKey}:OPTION:{optionSource.Code}", option.Id.ToString("D"));
                }
                product.OptionGroups.Add(group);
            }
            installation.AddMaterialization(ProductionPackageResourceKind.Product, definition.SourceKey, product.Id.ToString("D"));
            result.Add(new MaterializedProduct(definition.SourceKey, product, recipesByCode,
                recipeRequirements, optionRequirementsByCode));
        }
        return result;
    }

    internal static MaterializedArtifacts MaterializeArtifacts(
        ProductionPackageInstallation installation,
        IReadOnlyCollection<ProductionPackageArtifactDefinition> artifactDefinitions,
        IReadOnlyCollection<RobotArtifactTemplate> templates,
        IReadOnlyCollection<RobotArtifactTechnicalContract> contracts,
        IReadOnlyCollection<RobotArtifact> existingArtifacts,
        IReadOnlySet<Guid> packageManagedArtifactIds,
        PreparedPackageArtifacts preparedArtifacts)
    {
        var templatesById = templates.ToDictionary(x => x.Id);
        var contractsById = contracts.ToDictionary(x => x.Id);
        var result = new Dictionary<string, RobotArtifact>(StringComparer.Ordinal);
        var created = new List<RobotArtifact>();
        foreach (var definition in artifactDefinitions)
        {
            if (!templatesById.TryGetValue(definition.RobotArtifactTemplateId, out var template) ||
                template.Status != RobotArtifactStatus.Published || template.Checksum != definition.ArtifactChecksum ||
                !contractsById.TryGetValue(definition.TechnicalContractId, out var contract) ||
                contract.Status != RobotArtifactContractStatus.Published || contract.ContractChecksum != definition.TechnicalContractChecksum)
                throw new DomainRuleException("Package artifact source no longer matches its immutable definition.");

            var existing = existingArtifacts.SingleOrDefault(artifact =>
                artifact.ArtifactCode == definition.SourceKey && artifact.Checksum == template.Checksum);
            if (existing is not null)
            {
                ProductionPackageArtifactPreparation.ValidateReusableArtifact(
                    definition, template, contract, existing, packageManagedArtifactIds);
                result.Add(definition.SourceKey, existing);
                installation.AddMaterialization(ProductionPackageResourceKind.RobotArtifact,
                    definition.SourceKey, existing.Id.ToString("D"), existing.Checksum);
                continue;
            }

            if (!preparedArtifacts.BySourceKey.TryGetValue(definition.SourceKey, out var artifact) ||
                existingArtifacts.Any(candidate => candidate.Id == artifact.Id))
                throw new DomainRuleException("Prepared package artifact no longer matches the locked installation state.");
            result.Add(definition.SourceKey, artifact);
            created.Add(artifact);
            if (artifact.Status == RobotArtifactStatus.Draft)
            {
                artifact.Publish();
            }
            installation.AddMaterialization(ProductionPackageResourceKind.RobotArtifact,
                definition.SourceKey, artifact.Id.ToString("D"), artifact.Checksum);
        }
        return new MaterializedArtifacts(result, created);
    }

    internal static ComposedPrograms ComposePrograms(
        InstallProductionPackageCommand command, ProductionPackageInstallation installation,
        ProductionPackageVersion version, IReadOnlyCollection<MaterializedProduct> products,
        IReadOnlyDictionary<string, RobotArtifact> artifacts,
        IReadOnlyCollection<RobotArtifactTechnicalContract> contracts,
        IReadOnlyDictionary<Guid, ProductOptionExecutionImpact> optionImpacts)
    {
        var now = DateTimeOffset.UtcNow;
        var contractById = contracts.ToDictionary(x => x.Id);
        var artifactDefinitions = version.Artifacts.ToDictionary(x => x.SourceKey, StringComparer.Ordinal);
        var programs = new Dictionary<string, RobotProgram>(StringComparer.Ordinal);
        var bindings = new Dictionary<string, ProductionProgramBinding>(StringComparer.Ordinal);
        var compositions = new List<ProductionComposition>();
        var selectedProductKeys = products.Select(x => x.SourceKey).ToHashSet(StringComparer.Ordinal);
        foreach (var route in version.Routes.Where(x => selectedProductKeys.Contains(x.ProductSourceKey))
                     .OrderBy(x => x.Priority).ThenBy(x => x.RouteCode))
        {
            var blueprint = version.Programs.Single(x => x.BlueprintCode == route.ProgramBlueprintCode);
            var product = products.Single(x => x.SourceKey == route.ProductSourceKey);
            var recipeKey = RecipeLookupKey(route.ProductVariantSourceKey, route.RecipeSourceKey);
            if (!product.RecipesByCode.TryGetValue(recipeKey, out var recipe) ||
                !product.RecipeRequirementsByCode.TryGetValue(recipeKey, out var recipeRequirements))
                throw new DomainRuleException($"Package route {route.RouteCode} references an unknown Recipe source key.");

            var orderedSlots = ProductionPackageDefinitionValidator.OrderSlots(blueprint, artifactDefinitions, contractById);
            var productDefinition = version.Products.Single(definition => definition.SourceKey == route.ProductSourceKey);
            var supportedOptionCodes = ProductionPackageDefinitionValidator.ResolveSupportedOptionCodes(
                route, productDefinition.ProductSnapshotJson, optionImpacts);
            var optionRequirements = supportedOptionCodes
                .SelectMany(code => product.OptionRequirementsByCode.TryGetValue(code, out var requirements)
                    ? requirements
                    : throw new DomainRuleException($"Package route {route.RouteCode} references an unknown supported option code."));
            ProductionPackageDefinitionValidator.ValidateEffects(orderedSlots, artifactDefinitions, contractById,
                recipeRequirements.Concat(optionRequirements)
                    .Select(x => new IngredientRequirement(x.IngredientCode, x.Quantity, x.Unit, x.OptionCode)).ToArray());
            var program = RobotProgram.CreateDraft(PackageProgramCode(version.Version, route.RouteCode,
                    command.MaterializationIdentitySuffix),
                $"{blueprint.BlueprintCode} / {route.RouteCode}", TenantScopeResolver.Resolve(command.StoreId, command.KioskId),
                command.OrganizationId, command.StoreId, command.KioskId,
                description: $"Generated from package version {version.Version}, route {route.RouteCode}.");
            program.CreatedByAccountId = command.UserContext.AccountId;
            var order = 1;
            foreach (var slot in orderedSlots)
                program.AddArtifact(artifacts[slot.ArtifactSourceKey].Id, order++,
                    requiredOptionCode: ResolveRequiredOptionCode(
                        contractById[artifactDefinitions[slot.ArtifactSourceKey].TechnicalContractId]));
            program.Publish(now, orderedSlots.Select(slot =>
            {
                var artifact = artifacts[slot.ArtifactSourceKey];
                return new RobotArtifactManifestSnapshot(
                    artifact.Id, artifact.ArtifactCode, artifact.ArtifactName, artifact.FileName,
                    artifact.Status, artifact.Checksum, artifact.StorageKey, artifact.RuntimeTargetCode,
                    artifact.MachineModelCode, artifact.ContentLengthBytes, artifact.TechnicalContractId,
                    artifact.TechnicalContractChecksum, artifact.RuntimeProfileSource);
            }).ToArray());
            programs.Add(route.RouteCode, program);
            installation.AddMaterialization(ProductionPackageResourceKind.RobotProgram,
                route.RouteCode, program.Id.ToString("D"));

            var input = JsonSerializer.Serialize(new
            {
                version.Id,
                version.ManifestChecksum,
                route.RouteCode,
                ProductVariantId = recipe.ProductVariantId,
                RecipeId = recipe.Id,
                blueprint.RuntimeTargetCode,
                blueprint.MachineModelCode,
                SupportedOptionCodes = supportedOptionCodes.Order(StringComparer.Ordinal),
                Slots = orderedSlots.Select(x => new
                {
                    x.SlotCode,
                    x.RequiredEffectCode,
                    ArtifactId = artifacts[x.ArtifactSourceKey].Id,
                    artifacts[x.ArtifactSourceKey].Checksum
                })
            });
            var report = JsonSerializer.Serialize(new
            {
                IsValid = true,
                RequiresUserAcknowledgement = true,
                Warnings = new[] { "Physical behavior has not been proven on the target kiosk." },
                OrderedEffects = orderedSlots.Select(x => x.RequiredEffectCode)
            });
            var composition = ProductionComposition.Create(installation.Id, command.OrganizationId,
                recipe.ProductVariantId, recipe.Id, null, blueprint.RuntimeTargetCode, blueprint.MachineModelCode,
                input, true, report);
            composition.Apply(program.Id);
            compositions.Add(composition);

            var capabilityCodes = ProductionPackageDefinitionValidator.ValidateCapabilities(
                route.RequiredCapabilitiesJson);
            bindings.Add(route.RouteCode, ProductionProgramBinding.Create(
                command.OrganizationId,
                recipe.ProductVariantId,
                recipe.Id,
                recipe.Version,
                program.Id,
                program.ProgramManifestChecksum!,
                capabilityCodes,
                ProductionProgramBindingCapabilityEvidenceStatus.Declared,
                ProductionProgramBindingAssurance.OperatorDeclared,
                supportedOptionCodes,
                command.UserContext.AccountId));
        }
        return new ComposedPrograms(programs, bindings, compositions);
    }

    internal static ConfigurationRelease CreateRelease(InstallProductionPackageCommand command,
        ProductionPackageInstallation installation, ProductionPackageVersion version, long releaseNumber,
        IReadOnlyCollection<MaterializedProduct> products, IReadOnlyDictionary<string, RobotProgram> programs,
        IReadOnlyDictionary<string, ProductionProgramBinding> bindings,
        IReadOnlyDictionary<Guid, ProductOptionExecutionImpact> optionImpacts)
    {
        var release = ConfigurationRelease.CreateDraft(command.OrganizationId, releaseNumber);
        release.CreatedByAccountId = command.UserContext.AccountId;
        var selectedProductKeys = products.Select(x => x.SourceKey).ToHashSet(StringComparer.Ordinal);
        release.ReplaceRoutes(version.Routes.Where(x => selectedProductKeys.Contains(x.ProductSourceKey))
            .OrderBy(x => x.Priority).ThenBy(x => x.RouteCode).Select(routeDefinition =>
        {
            var product = products.Single(x => x.SourceKey == routeDefinition.ProductSourceKey);
            if (!product.RecipesByCode.TryGetValue(
                    RecipeLookupKey(routeDefinition.ProductVariantSourceKey, routeDefinition.RecipeSourceKey), out var recipe))
                throw new DomainRuleException("Package route Recipe source key was not materialized.");
            var binding = bindings[routeDefinition.RouteCode];
            var capabilityCodes = binding.GetRequiredCapabilityCodes();
            IReadOnlyCollection<(Guid ProductionProgramBindingId, string ProductionProgramBindingChecksum,
                Guid RobotProgramId, int BindingOrder, IReadOnlyCollection<string> CapabilityCodes)> routeBindings =
                [(binding.Id, binding.BindingChecksum, programs[routeDefinition.RouteCode].Id, 1, capabilityCodes)];
            var productDefinition = version.Products.Single(x => x.SourceKey == routeDefinition.ProductSourceKey);
            var supportedOptionCodes = ProductionPackageDefinitionValidator.ResolveSupportedOptionCodes(
                routeDefinition, productDefinition.ProductSnapshotJson, optionImpacts);
            return (recipe.ProductVariantId, recipe.Id, routeDefinition.RouteCode, routeDefinition.Priority,
                (string?)routeDefinition.RequiredCapabilitiesJson,
                (IReadOnlyCollection<string>)supportedOptionCodes.Order(StringComparer.Ordinal).ToArray(), routeBindings);
        }));
        return release;
    }

    internal static string? ResolveRequiredOptionCode(RobotArtifactTechnicalContract contract)
    {
        var optionCodes = contract.Effects.Where(x => !string.IsNullOrWhiteSpace(x.OptionCode))
            .Select(x => x.OptionCode!).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (optionCodes.Length > 1)
            throw new DomainRuleException("One artifact contract cannot be conditional on multiple product options in V1.");
        if (optionCodes.Length == 1 && contract.Effects.Any(x => string.IsNullOrWhiteSpace(x.OptionCode)))
            throw new DomainRuleException("An option-conditional artifact cannot also declare unconditional effects.");
        return optionCodes.SingleOrDefault()?.Trim().ToUpperInvariant();
    }

    internal sealed record MaterializedProduct(string SourceKey, Product Product,
        IReadOnlyDictionary<string, Recipe> RecipesByCode,
        IReadOnlyDictionary<string, IReadOnlyCollection<IngredientQuantityRequirement>> RecipeRequirementsByCode,
        IReadOnlyDictionary<string, IReadOnlyCollection<IngredientQuantityRequirement>> OptionRequirementsByCode);
    internal sealed record IngredientQuantityRequirement(string IngredientCode, decimal Quantity, string Unit,
        string? OptionCode);
    internal sealed record MaterializedArtifacts(
        IReadOnlyDictionary<string, RobotArtifact> All,
        IReadOnlyCollection<RobotArtifact> Created);
    internal sealed record ComposedPrograms(IReadOnlyDictionary<string, RobotProgram> Programs,
        IReadOnlyDictionary<string, ProductionProgramBinding> Bindings,
        IReadOnlyCollection<ProductionComposition> Compositions);

    internal static string RecipeLookupKey(string variantCode, string recipeCode) =>
        $"{variantCode.Trim().ToUpperInvariant()}::{recipeCode.Trim().ToUpperInvariant()}";

    internal static string PackageProgramCode(int packageVersion, string routeCode, string? identitySuffix = null) =>
        ApplyIdentitySuffix($"PKG_{packageVersion}_{routeCode}", identitySuffix);

    internal static string ApplyIdentitySuffix(string code, string? identitySuffix)
        => ProductionPackageMaterializationCode.WithSuffix(code, identitySuffix);

    internal static string ForkArtifactCode(string sourceCode, Guid installationId) =>
        $"{sourceCode.Trim().ToUpperInvariant()}_FORK_{installationId:N}";

    internal static string ForkProgramCode(string sourceCode, Guid installationId) =>
        $"{sourceCode.Trim().ToUpperInvariant()}_FORK_{installationId:N}";
}
