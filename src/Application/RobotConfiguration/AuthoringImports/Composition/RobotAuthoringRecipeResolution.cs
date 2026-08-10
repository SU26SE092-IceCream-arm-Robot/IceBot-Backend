using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using Domain.RobotConfiguration.ArtifactContracts;
using Domain.RobotConfiguration.AuthoringImports;

namespace Application.RobotConfiguration.AuthoringImports.Composition;

public sealed record RobotAuthoringRecipeCandidate(
    Guid RecipeId,
    string RecipeCode,
    string RecipeName,
    string ProductCode,
    string ProductName,
    string ProductVariantCode,
    string ProductVariantName);

public sealed record RobotAuthoringRecipeResolution(
    string Status,
    string Message,
    IReadOnlyCollection<RobotAuthoringRecipeCandidate> Candidates)
{
    public static RobotAuthoringRecipeResolution NotReady() => new(
        "NotReady",
        "Recipe matching begins after the import has materialized Draft technical resources.",
        []);
}

public sealed class RobotAuthoringRecipeResolver(
    IRobotAuthoringImportStore importStore,
    IRobotAuthoringCompositionStore store)
{
    public async Task<RobotAuthoringRecipeResolution> ResolveAsync(
        Guid organizationId,
        Guid importId,
        CancellationToken cancellationToken)
    {
        var importSession = await importStore.GetAsync(organizationId, importId, false, cancellationToken);
        if (importSession is null)
            return RobotAuthoringRecipeResolution.NotReady();
        if (importSession.Status != RobotAuthoringImportStatus.Applied)
            return RobotAuthoringRecipeResolution.NotReady();

        var contractIds = importSession.Items
            .Where(item => item.TechnicalContractId.HasValue)
            .Select(item => item.TechnicalContractId!.Value)
            .Distinct()
            .ToArray();
        var contracts = await store.GetContractsAsync(importSession.OrganizationId, contractIds, cancellationToken);
        var effects = contracts.Where(contract => contract.SchemaVersion >= 2)
            .SelectMany(contract => contract.Effects).ToArray();
        if (effects.Any(effect => !string.IsNullOrWhiteSpace(effect.OptionCode)))
            return new("OptionSelectionRequired",
                "Operator-declared metadata mentions production options. Select the intended Recipe and options explicitly.", []);

        var artifactRequirements = effects
            .Where(effect => effect.EffectKind == RobotArtifactEffectKind.Ingredient)
            .Select(effect => Requirement.From(effect.IngredientCode, effect.QuantityMode, effect.FixedQuantity, effect.Unit))
            .ToArray();
        if (artifactRequirements.Length == 0 || artifactRequirements.Any(requirement => requirement is null))
            return new("NoMatch",
                "No Recipe suggestion can be matched from operator-declared metadata. Select the intended Recipe explicitly.", []);

        var required = artifactRequirements.Cast<Requirement>().OrderBy(requirement => requirement.Code)
            .ThenBy(requirement => requirement.Quantity).ThenBy(requirement => requirement.Unit).ToArray();
        var candidates = (await store.ListEligibleRecipesAsync(importSession.OrganizationId, cancellationToken))
            .Where(recipe => Matches(recipe, required))
            .Select(recipe => new RobotAuthoringRecipeCandidate(recipe.Id, recipe.Code, recipe.Name,
                recipe.ProductVariant.Product.Code, recipe.ProductVariant.Product.Name,
                recipe.ProductVariant.Code, recipe.ProductVariant.Name))
            .OrderBy(candidate => candidate.ProductName, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.ProductVariantName, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.RecipeName, StringComparer.Ordinal)
            .ToArray();

        return candidates.Length switch
        {
            0 => new("NoMatch", "No Recipe matches the operator-declared metadata. Select the intended Recipe explicitly.", []),
            1 => new("SingleMatch", "One Recipe matches the operator-declared metadata. Operator confirmation is still required.", candidates),
            _ => new("MultipleMatches", "Multiple Recipes match the operator-declared metadata. Choose the intended Recipe explicitly.", candidates)
        };
    }

    private static bool Matches(Recipe recipe, IReadOnlyCollection<Requirement> required)
    {
        if (recipe.Status is not RecipeStatus.Published and not RecipeStatus.Active ||
            recipe.ProductVariant.FulfillmentType != FulfillmentType.MachineProduced)
            return false;

        var items = recipe.RecipeItems.Where(item => item.DeletedAt is null && !item.IsOptional)
            .Select(item => new Requirement(Normalize(item.Ingredient.Code), item.Quantity, NormalizeUnit(item.Unit)))
            .OrderBy(item => item.Code).ThenBy(item => item.Quantity).ThenBy(item => item.Unit).ToArray();
        return items.SequenceEqual(required);
    }

    private sealed record Requirement(string Code, decimal Quantity, string Unit)
    {
        public static Requirement? From(string? ingredientCode, RobotArtifactQuantityMode quantityMode,
            decimal? fixedQuantity, string? unit) =>
            quantityMode == RobotArtifactQuantityMode.FixedInArtifact && !string.IsNullOrWhiteSpace(ingredientCode) &&
            fixedQuantity is > 0 && !string.IsNullOrWhiteSpace(unit)
                ? new Requirement(Normalize(ingredientCode), fixedQuantity.Value, NormalizeUnit(unit))
                : null;
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
    private static string NormalizeUnit(string value) => value.Trim().ToLowerInvariant();
}
