using System.Text.Json;
using Application.ProductionConfiguration.Routes.Support;
using Domain.Common;
using Domain.ProductionPackages;
using Domain.RobotConfiguration.ArtifactContracts;

namespace Application.ProductionPackages;

public static class ProductionPackageDefinitionValidator
{
    public static void Validate(ProductionPackageVersion version,
        IReadOnlyCollection<RobotArtifactTechnicalContract> contracts)
    {
        var contractsById = contracts.ToDictionary(x => x.Id);
        var artifacts = version.Artifacts.ToDictionary(x => x.SourceKey, StringComparer.Ordinal);
        var programs = version.Programs.ToDictionary(x => x.BlueprintCode, StringComparer.Ordinal);
        var products = version.Products.ToDictionary(x => x.SourceKey, StringComparer.Ordinal);
        var optionImpacts = ResolveOptionExecutionImpacts(version, contracts);

        foreach (var artifact in artifacts.Values)
        {
            if (!contractsById.TryGetValue(artifact.TechnicalContractId, out var contract) ||
                contract.Status != RobotArtifactContractStatus.Published ||
                contract.ContractChecksum != artifact.TechnicalContractChecksum)
                throw new DomainRuleException($"Package artifact {artifact.SourceKey} has invalid technical-contract provenance.");
        }

        foreach (var route in version.Routes)
        {
            if (!products.TryGetValue(route.ProductSourceKey, out var productDefinition) ||
                !programs.TryGetValue(route.ProgramBlueprintCode, out var blueprint))
                throw new DomainRuleException($"Package route {route.RouteCode} references an unknown product or program blueprint.");

            var productDocument = ProductionPackageProductSnapshotCodec.Deserialize(productDefinition.ProductSnapshotJson);
            var product = productDocument.Product;
            ValidateUniqueOptionCodes(product, route.RouteCode);
            var variants = product.Variants.Where(x => string.Equals(
                x.Code, route.ProductVariantSourceKey, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (variants.Length != 1)
                throw new DomainRuleException($"Package route {route.RouteCode} must resolve exactly one ProductVariant source key.");
            var recipes = variants[0].Recipes
                .Where(x => string.Equals(x.Code, route.RecipeSourceKey, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (recipes.Length != 1)
                throw new DomainRuleException($"Package route {route.RouteCode} must resolve exactly one Recipe source key.");

            var orderedSlots = OrderSlots(blueprint, artifacts, contractsById);
            var allOptions = product.OptionGroups.SelectMany(group => group.Options).ToArray();
            var optionByCode = allOptions.ToDictionary(option => option.Code, StringComparer.OrdinalIgnoreCase);
            var supportedOptionCodes = ResolveSupportedOptionCodes(
                route, productDefinition.ProductSnapshotJson, optionImpacts);
            if (supportedOptionCodes.Any(code => !optionByCode.ContainsKey(code)))
                throw new DomainRuleException($"Package route {route.RouteCode} references an unknown supported option code.");
            if (supportedOptionCodes.Any(code => optionImpacts[optionByCode[code].Id] !=
                    Domain.Catalog.Enums.ProductOptionExecutionImpact.ProductionAffecting))
                throw new DomainRuleException($"Package route {route.RouteCode} may support only production-affecting options.");
            var requirements = recipes[0].Items.Select(x => new IngredientRequirement(
                    x.IngredientCode, x.Quantity, x.Unit, null))
                .Concat(allOptions
                    .Where(option => supportedOptionCodes.Contains(option.Code))
                    .SelectMany(option =>
                    option.IngredientRequirements.Select(x => new IngredientRequirement(
                        x.IngredientCode, x.Quantity, x.Unit, option.Code.Trim().ToUpperInvariant()))))
                .ToArray();
            var optionEffects = orderedSlots
                .SelectMany(slot => contractsById[artifacts[slot.ArtifactSourceKey].TechnicalContractId].Effects)
                .Where(effect => !string.IsNullOrWhiteSpace(effect.OptionCode))
                .Select(effect => effect.OptionCode!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (optionEffects.Any(code => !supportedOptionCodes.Contains(code)))
                throw new DomainRuleException($"Package route {route.RouteCode} has an artifact effect for an unsupported option.");
            ValidateOptionExecutionBoundary(allOptions
                .Where(option => optionImpacts[option.Id] == Domain.Catalog.Enums.ProductOptionExecutionImpact.CommercialOnly ||
                                 supportedOptionCodes.Contains(option.Code)).Select(option =>
                new ProductionOptionExecutionInput(option.Code, optionImpacts[option.Id],
                    option.IngredientRequirements.Count > 0)).ToArray(), optionEffects, route.RouteCode);
            ValidateEffects(orderedSlots, artifacts, contractsById, requirements);
            ValidateCapabilities(route.RequiredCapabilitiesJson);
        }
    }

    public static IReadOnlyList<ProductionPackageProgramSlot> OrderSlots(
        ProductionPackageProgramBlueprint blueprint,
        IReadOnlyDictionary<string, ProductionPackageArtifactDefinition> artifacts,
        IReadOnlyDictionary<Guid, RobotArtifactTechnicalContract> contracts)
    {
        var slots = blueprint.Slots.ToDictionary(x => x.RequiredEffectCode, StringComparer.Ordinal);
        var edges = slots.Keys.ToDictionary(x => x, _ => new HashSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);
        var indegree = slots.Keys.ToDictionary(x => x, _ => 0, StringComparer.Ordinal);
        foreach (var slot in slots.Values)
        {
            if (!artifacts.TryGetValue(slot.ArtifactSourceKey, out var definition) ||
                !contracts.TryGetValue(definition.TechnicalContractId, out var contract) ||
                contract.RuntimeTargetCode != blueprint.RuntimeTargetCode ||
                contract.MachineModelCode != blueprint.MachineModelCode ||
                contract.Effects.All(x => x.EffectCode != slot.RequiredEffectCode))
                throw new DomainRuleException($"Artifact contract does not provide effect {slot.RequiredEffectCode} for the blueprint target.");

            foreach (var constraint in contract.OrderingConstraints
                         .Where(x => x.ConstraintType != RobotArtifactOrderingConstraintType.Phase))
            {
                if (!slots.ContainsKey(constraint.Value)) continue;
                var from = constraint.ConstraintType == RobotArtifactOrderingConstraintType.BeforeEffect
                    ? slot.RequiredEffectCode : constraint.Value;
                var to = constraint.ConstraintType == RobotArtifactOrderingConstraintType.BeforeEffect
                    ? constraint.Value : slot.RequiredEffectCode;
                if (edges[from].Add(to)) indegree[to]++;
            }
        }

        var result = new List<ProductionPackageProgramSlot>();
        while (result.Count < slots.Count)
        {
            var next = slots.Values
                .Where(x => indegree[x.RequiredEffectCode] == 0 && result.All(y => y.RequiredEffectCode != x.RequiredEffectCode))
                .OrderBy(x => PhaseRank(x.Phase)).ThenBy(x => x.SortHint)
                .ThenBy(x => x.RequiredEffectCode, StringComparer.Ordinal).FirstOrDefault()
                ?? throw new DomainRuleException("Artifact ordering constraints contain a cycle.");
            result.Add(next);
            foreach (var target in edges[next.RequiredEffectCode]) indegree[target]--;
        }
        return result;
    }

    public static void ValidateEffects(IReadOnlyCollection<ProductionPackageProgramSlot> slots,
        IReadOnlyDictionary<string, ProductionPackageArtifactDefinition> definitions,
        IReadOnlyDictionary<Guid, RobotArtifactTechnicalContract> contracts,
        IReadOnlyCollection<IngredientRequirement> requirements)
    {
        var effects = slots.SelectMany(slot => contracts[definitions[slot.ArtifactSourceKey].TechnicalContractId]
            .Effects.Where(x => x.EffectKind == RobotArtifactEffectKind.Ingredient)).ToArray();

        foreach (var effect in effects.Where(x => x.QuantityMode == RobotArtifactQuantityMode.FixedInArtifact))
        {
            if (!requirements.Any(x => Matches(x, effect)))
                throw new DomainRuleException($"Fixed artifact effect {effect.EffectCode} does not match its Recipe or option requirement.");
        }

        foreach (var requirement in requirements)
        {
            if (!effects.Any(effect => effect.QuantityMode == RobotArtifactQuantityMode.FixedInArtifact && Matches(requirement, effect)))
                throw new DomainRuleException($"Recipe or option ingredient {requirement.IngredientCode} has no matching fixed artifact effect.");
        }
    }

    public static IReadOnlyCollection<string> ValidateCapabilities(string json)
    {
        using var document = JsonDocument.Parse(json);
        var codes = document.RootElement.TryGetProperty("requires", out var requires) && requires.ValueKind == JsonValueKind.Array
            ? requires.EnumerateArray().Where(x => x.TryGetProperty("code", out _))
                .Select(x => x.GetProperty("code").GetString()?.Trim().ToUpperInvariant())
                .Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().Distinct(StringComparer.Ordinal).ToArray()
            : [];
        if (codes.Length == 0)
            throw new DomainRuleException("Production package routes require at least one capability code.");
        var error = ExecutionRouteRequiredCapabilitiesContract.Validate(json, codes);
        if (error is not null) throw new DomainRuleException(error);
        return codes;
    }

    private static bool Matches(IngredientRequirement requirement, RobotArtifactDeclaredEffect effect) =>
        string.Equals(requirement.IngredientCode, effect.IngredientCode, StringComparison.OrdinalIgnoreCase) &&
        requirement.Quantity == effect.FixedQuantity &&
        string.Equals(requirement.Unit, effect.Unit, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(requirement.OptionCode, effect.OptionCode, StringComparison.OrdinalIgnoreCase);

    private static void ValidateUniqueOptionCodes(ProductionPackageProductSnapshot product, string routeCode)
    {
        var duplicates = product.OptionGroups.SelectMany(x => x.Options).GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1).Select(x => x.Key).ToArray();
        if (duplicates.Length > 0)
            throw new DomainRuleException($"Package route {routeCode} requires option codes to be unique across its Product.");
    }

    public static void ValidateOptionExecutionBoundary(
        IReadOnlyCollection<ProductionOptionExecutionInput> options,
        IReadOnlySet<string> optionEffectCodes,
        string routeCode)
    {
        foreach (var option in options)
        {
            if (option.ExecutionImpact == Domain.Catalog.Enums.ProductOptionExecutionImpact.CommercialOnly)
            {
                if (option.HasIngredientRequirements || optionEffectCodes.Contains(option.Code))
                    throw new DomainRuleException(
                        $"Commercial-only option {option.Code} on package route {routeCode} cannot have production effects.");
                continue;
            }

            if (!option.HasIngredientRequirements && !optionEffectCodes.Contains(option.Code))
                throw new DomainRuleException(
                    $"Production-affecting option {option.Code} on package route {routeCode} has no deterministic production input.");
        }
    }

    public static IReadOnlyDictionary<Guid, Domain.Catalog.Enums.ProductOptionExecutionImpact>
        ResolveOptionExecutionImpacts(ProductionPackageVersion version,
            IReadOnlyCollection<RobotArtifactTechnicalContract> contracts)
    {
        var contractById = contracts.ToDictionary(contract => contract.Id);
        var artifactByKey = version.Artifacts.ToDictionary(artifact => artifact.SourceKey, StringComparer.Ordinal);
        var programByCode = version.Programs.ToDictionary(program => program.BlueprintCode, StringComparer.Ordinal);
        var result = new Dictionary<Guid, Domain.Catalog.Enums.ProductOptionExecutionImpact>();

        foreach (var definition in version.Products)
        {
            var programCodes = version.Routes.Where(route => route.ProductSourceKey == definition.SourceKey)
                .Select(route => route.ProgramBlueprintCode).ToHashSet(StringComparer.Ordinal);
            var optionEffectCodes = programCodes.Where(programByCode.ContainsKey)
                .SelectMany(code => programByCode[code].Slots)
                .Where(slot => artifactByKey.ContainsKey(slot.ArtifactSourceKey))
                .SelectMany(slot => contractById.TryGetValue(
                        artifactByKey[slot.ArtifactSourceKey].TechnicalContractId, out var contract)
                    ? contract.Effects
                    : [])
                .Where(effect => !string.IsNullOrWhiteSpace(effect.OptionCode))
                .Select(effect => effect.OptionCode!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var product = ProductionPackageProductSnapshotCodec.Deserialize(definition.ProductSnapshotJson).Product;
            foreach (var option in product.OptionGroups.SelectMany(group => group.Options))
            {
                if (!result.TryAdd(option.Id, option.ExecutionImpact ??
                        (option.IngredientRequirements.Count > 0 || optionEffectCodes.Contains(option.Code)
                            ? Domain.Catalog.Enums.ProductOptionExecutionImpact.ProductionAffecting
                            : Domain.Catalog.Enums.ProductOptionExecutionImpact.CommercialOnly)))
                    throw new DomainRuleException("Package Product snapshots contain duplicate option identities.");
            }
        }

        return result;
    }

    public static IReadOnlySet<string> ResolveSupportedOptionCodes(ProductionPackageRouteBlueprint route,
        string productSnapshotJson,
        IReadOnlyDictionary<Guid, Domain.Catalog.Enums.ProductOptionExecutionImpact> optionImpacts)
    {
        var document = ProductionPackageProductSnapshotCodec.Deserialize(productSnapshotJson);
        var optionByCode = document.Product.OptionGroups.SelectMany(group => group.Options)
            .ToDictionary(option => option.Code, StringComparer.OrdinalIgnoreCase);
        var requested = route.GetSupportedOptionCodes().ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (document.SchemaVersion == 1)
            requested.RemoveWhere(code => optionByCode.TryGetValue(code, out var option) &&
                optionImpacts[option.Id] != Domain.Catalog.Enums.ProductOptionExecutionImpact.ProductionAffecting);
        return requested;
    }

    private static int PhaseRank(string phase) => phase.Trim().ToUpperInvariant() switch
    {
        "PREPARE" => 0,
        "BASE" => 100,
        "OPTION" => 200,
        "FINISH" => 300,
        "DELIVER" => 400,
        "CLEANUP" => 500,
        _ => 250
    };
}

public sealed record IngredientRequirement(string IngredientCode, decimal Quantity, string Unit, string? OptionCode);
public sealed record ProductionOptionExecutionInput(string Code,
    Domain.Catalog.Enums.ProductOptionExecutionImpact ExecutionImpact,
    bool HasIngredientRequirements);
