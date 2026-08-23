using Application.RobotConfiguration.Storage.Abstractions;
using Domain.Catalog.Entities;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Catalog.Bootstrap;

public sealed record DevelopmentIceBotDemoResetResult(
    Guid OrganizationId,
    int DeletedImportCount,
    int DeletedArtifactCount,
    int DeletedProgramCount,
    int DeletedContractCount,
    int DeletedBindingCount,
    int DeletedReleaseCount,
    int DeletedMenuItemCount,
    int DeletedObjectCount,
    int RetainedObjectCount,
    bool DeletedAutomationFixture);

/// <summary>
/// Destructive local-only reset for the ICEBOT-DEMO authoring/catalog fixture.
/// It preserves the demo tenant boundary but refuses to erase runtime or commercial evidence.
/// </summary>
public sealed class DevelopmentIceBotDemoReset(
    IceBotDbContext dbContext,
    IArtifactObjectStorage objectStorage,
    ILogger<DevelopmentIceBotDemoReset> logger)
{
    public const string OrganizationCode = "ICEBOT-DEMO";
    private const string AutomationFixtureOrganizationCode = "ICEBOT-AUTOMATION-TEST";
    private const string ProductCode = "KEM-TUOI-VANI";

    public async Task<DevelopmentIceBotDemoResetResult> ResetAsync(
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var deletedAutomationFixture = await DeleteAutomationFixtureAsync(cancellationToken);
        var organization = await dbContext.Organizations.IgnoreQueryFilters()
            .SingleOrDefaultAsync(candidate => candidate.Code == OrganizationCode, cancellationToken)
            ?? throw new InvalidOperationException(
                $"{OrganizationCode} is missing. Start the Development backend once to create the demo baseline.");

        var productIds = await dbContext.Products.IgnoreQueryFilters()
            .Where(product => product.OrganizationId == organization.Id && product.Code == ProductCode)
            .Select(product => product.Id)
            .ToArrayAsync(cancellationToken);
        var variantIds = await dbContext.ProductVariants.IgnoreQueryFilters()
            .Where(variant => productIds.Contains(variant.ProductId))
            .Select(variant => variant.Id)
            .ToArrayAsync(cancellationToken);
        var recipeIds = await dbContext.Recipes.IgnoreQueryFilters()
            .Where(recipe => recipe.OrganizationId == organization.Id && variantIds.Contains(recipe.ProductVariantId))
            .Select(recipe => recipe.Id)
            .ToArrayAsync(cancellationToken);
        var programIds = await dbContext.RobotPrograms.IgnoreQueryFilters()
            .Where(program => program.OrganizationId == organization.Id)
            .Select(program => program.Id)
            .ToArrayAsync(cancellationToken);
        var optionGroupIds = await dbContext.OptionGroups.IgnoreQueryFilters()
            .Where(group => productIds.Contains(group.ProductId))
            .Select(group => group.Id)
            .ToArrayAsync(cancellationToken);
        var optionIds = await dbContext.ProductOptions.IgnoreQueryFilters()
            .Where(option => optionGroupIds.Contains(option.OptionGroupId))
            .Select(option => option.Id)
            .ToArrayAsync(cancellationToken);
        var releaseIds = await dbContext.ConfigurationReleases.IgnoreQueryFilters()
            .Where(release => release.OrganizationId == organization.Id)
            .Select(release => release.Id)
            .ToArrayAsync(cancellationToken);
        var menuItemIds = await dbContext.MenuItems.IgnoreQueryFilters()
            .Where(item => variantIds.Contains(item.ProductVariantId) ||
                           (item.RecipeId.HasValue && recipeIds.Contains(item.RecipeId.Value)))
            .Select(item => item.Id)
            .ToArrayAsync(cancellationToken);

        await EnsureNoRuntimeEvidenceAsync(organization.Id, releaseIds, menuItemIds, cancellationToken);

        var importKeys = await dbContext.RobotAuthoringImports.IgnoreQueryFilters()
            .Where(importSession => importSession.OrganizationId == organization.Id)
            .Select(importSession => importSession.StagingStorageKey)
            .ToArrayAsync(cancellationToken);
        var importIds = await dbContext.RobotAuthoringImports.IgnoreQueryFilters()
            .Where(importSession => importSession.OrganizationId == organization.Id)
            .Select(importSession => importSession.Id)
            .ToArrayAsync(cancellationToken);
        var artifactKeys = await dbContext.RobotArtifacts.IgnoreQueryFilters()
            .Where(artifact => artifact.OrganizationId == organization.Id)
            .Select(artifact => artifact.StorageKey)
            .ToArrayAsync(cancellationToken);
        var contractIds = await dbContext.RobotArtifactTechnicalContracts.IgnoreQueryFilters()
            .Where(contract => contract.OrganizationId == organization.Id)
            .Select(contract => contract.Id)
            .ToArrayAsync(cancellationToken);
        var importCount = await dbContext.RobotAuthoringImports.IgnoreQueryFilters()
            .CountAsync(importSession => importSession.OrganizationId == organization.Id, cancellationToken);
        var artifactCount = await dbContext.RobotArtifacts.IgnoreQueryFilters()
            .CountAsync(artifact => artifact.OrganizationId == organization.Id, cancellationToken);
        var programCount = await dbContext.RobotPrograms.IgnoreQueryFilters()
            .CountAsync(program => program.OrganizationId == organization.Id, cancellationToken);
        var contractCount = await dbContext.RobotArtifactTechnicalContracts.IgnoreQueryFilters()
            .CountAsync(contract => contract.OrganizationId == organization.Id, cancellationToken);
        var bindingCount = await dbContext.ProductionProgramBindings.IgnoreQueryFilters()
            .CountAsync(binding => binding.OrganizationId == organization.Id, cancellationToken);

        await using (var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            // ExecuteDelete bypasses EF's tracked cascade behavior; remove the RESTRICT child rows explicitly.
            await dbContext.RobotAuthoringImportItems.IgnoreQueryFilters()
                .Where(item => importIds.Contains(item.RobotAuthoringImportId))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.RobotAuthoringImports.IgnoreQueryFilters()
                .Where(importSession => importSession.OrganizationId == organization.Id)
                .ExecuteDeleteAsync(cancellationToken);

            await dbContext.MenuItemProductOptions.IgnoreQueryFilters()
                .Where(option => menuItemIds.Contains(option.MenuItemId))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.MenuItems.IgnoreQueryFilters()
                .Where(item => menuItemIds.Contains(item.Id))
                .ExecuteDeleteAsync(cancellationToken);

            await dbContext.ExecutionRouteRobotBindings.IgnoreQueryFilters()
                .Where(binding => releaseIds.Contains(binding.ExecutionRoute.ConfigurationReleaseId))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.ExecutionRoutes.IgnoreQueryFilters()
                .Where(route => releaseIds.Contains(route.ConfigurationReleaseId))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.ProductionProgramBindings.IgnoreQueryFilters()
                .Where(binding => binding.OrganizationId == organization.Id)
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.ConfigurationReleases.IgnoreQueryFilters()
                .Where(release => releaseIds.Contains(release.Id))
                .ExecuteDeleteAsync(cancellationToken);

            await dbContext.ProductionCompositions.IgnoreQueryFilters()
                .Where(composition => composition.OrganizationId == organization.Id ||
                                      variantIds.Contains(composition.ProductVariantId) ||
                                      recipeIds.Contains(composition.RecipeId) ||
                                      (composition.GeneratedRobotProgramId.HasValue &&
                                       programIds.Contains(composition.GeneratedRobotProgramId.Value)))
                .ExecuteDeleteAsync(cancellationToken);

            await dbContext.RobotProgramArtifacts.IgnoreQueryFilters()
                .Where(item => programIds.Contains(item.RobotProgramId))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.RobotPrograms.IgnoreQueryFilters()
                .Where(program => program.OrganizationId == organization.Id)
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.RobotArtifacts.IgnoreQueryFilters()
                .Where(artifact => artifact.OrganizationId == organization.Id)
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.RobotArtifactDeclaredEffects.IgnoreQueryFilters()
                .Where(effect => contractIds.Contains(effect.TechnicalContractId))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.RobotArtifactOrderingConstraints.IgnoreQueryFilters()
                .Where(constraint => contractIds.Contains(constraint.TechnicalContractId))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.RobotArtifactTechnicalContracts.IgnoreQueryFilters()
                .Where(contract => contract.OrganizationId == organization.Id)
                .ExecuteDeleteAsync(cancellationToken);

            await dbContext.ProductOptionIngredientRequirements.IgnoreQueryFilters()
                .Where(requirement => optionIds.Contains(requirement.ProductOptionId))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.ProductOptions.IgnoreQueryFilters()
                .Where(option => optionIds.Contains(option.Id))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.OptionGroups.IgnoreQueryFilters()
                .Where(group => optionGroupIds.Contains(group.Id))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.RecipeItems.IgnoreQueryFilters()
                .Where(item => recipeIds.Contains(item.RecipeId))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.Recipes.IgnoreQueryFilters()
                .Where(recipe => recipeIds.Contains(recipe.Id))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.ProductVariants.IgnoreQueryFilters()
                .Where(variant => variantIds.Contains(variant.Id))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.Products.IgnoreQueryFilters()
                .Where(product => productIds.Contains(product.Id))
                .ExecuteDeleteAsync(cancellationToken);

            await SeedVanillaBaselineAsync(organization, now, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        var deletedObjects = 0;
        var retainedObjects = 0;
        foreach (var storageKey in importKeys.Concat(artifactKeys).Distinct(StringComparer.Ordinal))
        {
            try
            {
                await objectStorage.DeleteIfExistsAsync(storageKey, cancellationToken);
                deletedObjects++;
            }
            catch (Exception exception)
            {
                retainedObjects++;
                logger.LogWarning(exception,
                    "Retained orphaned local authoring object {StorageKey}; orphan cleanup can retry it.", storageKey);
            }
        }

        logger.LogInformation(
            "Reset local ICEBOT-DEMO authoring/catalog fixture {OrganizationCode}: {Imports} imports, {Artifacts} artifacts, {Programs} programs, {Contracts} contracts, {Bindings} bindings, {Releases} releases, and {MenuItems} menu items removed.",
            OrganizationCode, importCount, artifactCount, programCount, contractCount, bindingCount,
            releaseIds.Length, menuItemIds.Length);

        return new DevelopmentIceBotDemoResetResult(
            organization.Id, importCount, artifactCount, programCount, contractCount, bindingCount,
            releaseIds.Length, menuItemIds.Length, deletedObjects, retainedObjects, deletedAutomationFixture);
    }

    public Task<bool> DeleteLegacyAutomationFixtureAsync(CancellationToken cancellationToken) =>
        DeleteAutomationFixtureAsync(cancellationToken);

    private async Task<bool> DeleteAutomationFixtureAsync(CancellationToken cancellationToken)
    {
        var organization = await dbContext.Organizations.IgnoreQueryFilters()
            .SingleOrDefaultAsync(candidate => candidate.Code == AutomationFixtureOrganizationCode, cancellationToken);
        if (organization is null)
            return false;

        var hasOperationalTopology =
            await dbContext.Stores.IgnoreQueryFilters().AnyAsync(store => store.OrganizationId == organization.Id, cancellationToken) ||
            await dbContext.Kiosks.IgnoreQueryFilters().AnyAsync(kiosk => kiosk.OrganizationId == organization.Id, cancellationToken);
        if (hasOperationalTopology)
        {
            throw new InvalidOperationException(
                $"{AutomationFixtureOrganizationCode} has Store or Kiosk data and cannot be deleted by the development reset.");
        }

        var importIds = await dbContext.RobotAuthoringImports.IgnoreQueryFilters()
            .Where(item => item.OrganizationId == organization.Id)
            .Select(item => item.Id)
            .ToArrayAsync(cancellationToken);
        var objectKeys = await dbContext.RobotAuthoringImports.IgnoreQueryFilters()
            .Where(item => item.OrganizationId == organization.Id)
            .Select(item => item.StagingStorageKey)
            .Concat(dbContext.RobotArtifacts.IgnoreQueryFilters()
                .Where(item => item.OrganizationId == organization.Id)
                .Select(item => item.StorageKey))
            .ToArrayAsync(cancellationToken);
        var programIds = await dbContext.RobotPrograms.IgnoreQueryFilters()
            .Where(item => item.OrganizationId == organization.Id)
            .Select(item => item.Id)
            .ToArrayAsync(cancellationToken);
        var contractIds = await dbContext.RobotArtifactTechnicalContracts.IgnoreQueryFilters()
            .Where(item => item.OrganizationId == organization.Id)
            .Select(item => item.Id)
            .ToArrayAsync(cancellationToken);
        var releaseIds = await dbContext.ConfigurationReleases.IgnoreQueryFilters()
            .Where(item => item.OrganizationId == organization.Id)
            .Select(item => item.Id)
            .ToArrayAsync(cancellationToken);
        var productIds = await dbContext.Products.IgnoreQueryFilters()
            .Where(item => item.OrganizationId == organization.Id)
            .Select(item => item.Id)
            .ToArrayAsync(cancellationToken);
        var variantIds = await dbContext.ProductVariants.IgnoreQueryFilters()
            .Where(item => productIds.Contains(item.ProductId))
            .Select(item => item.Id)
            .ToArrayAsync(cancellationToken);
        var recipeIds = await dbContext.Recipes.IgnoreQueryFilters()
            .Where(item => item.OrganizationId == organization.Id || variantIds.Contains(item.ProductVariantId))
            .Select(item => item.Id)
            .ToArrayAsync(cancellationToken);
        var menuItemIds = await dbContext.MenuItems.IgnoreQueryFilters()
            .Where(item => variantIds.Contains(item.ProductVariantId) ||
                           (item.RecipeId.HasValue && recipeIds.Contains(item.RecipeId.Value)))
            .Select(item => item.Id)
            .ToArrayAsync(cancellationToken);
        var optionGroupIds = await dbContext.OptionGroups.IgnoreQueryFilters()
            .Where(item => productIds.Contains(item.ProductId))
            .Select(item => item.Id)
            .ToArrayAsync(cancellationToken);
        var optionIds = await dbContext.ProductOptions.IgnoreQueryFilters()
            .Where(item => optionGroupIds.Contains(item.OptionGroupId))
            .Select(item => item.Id)
            .ToArrayAsync(cancellationToken);

        await EnsureNoRuntimeEvidenceAsync(organization.Id, releaseIds, menuItemIds, cancellationToken);

        var hasPackageState =
            await dbContext.ProductionPackageInstallations.IgnoreQueryFilters()
                .AnyAsync(item => item.OrganizationId == organization.Id, cancellationToken) ||
            await dbContext.ProductionPackageUpgrades.IgnoreQueryFilters()
                .AnyAsync(item => item.OrganizationId == organization.Id, cancellationToken);
        if (hasPackageState)
        {
            throw new InvalidOperationException(
                $"{AutomationFixtureOrganizationCode} has production package state and cannot be deleted by the development reset.");
        }

        await using (var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            await dbContext.RobotAuthoringImportItems.IgnoreQueryFilters()
                .Where(item => importIds.Contains(item.RobotAuthoringImportId))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.RobotAuthoringImports.IgnoreQueryFilters()
                .Where(item => item.OrganizationId == organization.Id)
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.MenuItemProductOptions.IgnoreQueryFilters()
                .Where(item => menuItemIds.Contains(item.MenuItemId))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.MenuItems.IgnoreQueryFilters()
                .Where(item => menuItemIds.Contains(item.Id))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.ExecutionRouteRobotBindings.IgnoreQueryFilters()
                .Where(item => releaseIds.Contains(item.ExecutionRoute.ConfigurationReleaseId))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.ExecutionRoutes.IgnoreQueryFilters()
                .Where(item => releaseIds.Contains(item.ConfigurationReleaseId))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.ProductionProgramBindings.IgnoreQueryFilters()
                .Where(item => item.OrganizationId == organization.Id)
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.ConfigurationReleases.IgnoreQueryFilters()
                .Where(item => releaseIds.Contains(item.Id))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.ProductionCompositions.IgnoreQueryFilters()
                .Where(item => item.OrganizationId == organization.Id ||
                               variantIds.Contains(item.ProductVariantId) ||
                               recipeIds.Contains(item.RecipeId) ||
                               (item.GeneratedRobotProgramId.HasValue && programIds.Contains(item.GeneratedRobotProgramId.Value)))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.RobotProgramArtifacts.IgnoreQueryFilters()
                .Where(item => programIds.Contains(item.RobotProgramId))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.RobotPrograms.IgnoreQueryFilters()
                .Where(item => programIds.Contains(item.Id))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.RobotArtifacts.IgnoreQueryFilters()
                .Where(item => item.OrganizationId == organization.Id)
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.RobotArtifactDeclaredEffects.IgnoreQueryFilters()
                .Where(item => contractIds.Contains(item.TechnicalContractId))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.RobotArtifactOrderingConstraints.IgnoreQueryFilters()
                .Where(item => contractIds.Contains(item.TechnicalContractId))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.RobotArtifactTechnicalContracts.IgnoreQueryFilters()
                .Where(item => contractIds.Contains(item.Id))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.ProductOptionIngredientRequirements.IgnoreQueryFilters()
                .Where(item => optionIds.Contains(item.ProductOptionId))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.ProductOptions.IgnoreQueryFilters()
                .Where(item => optionIds.Contains(item.Id))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.OptionGroups.IgnoreQueryFilters()
                .Where(item => optionGroupIds.Contains(item.Id))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.RecipeItems.IgnoreQueryFilters()
                .Where(item => recipeIds.Contains(item.RecipeId))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.Recipes.IgnoreQueryFilters()
                .Where(item => recipeIds.Contains(item.Id))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.ProductVariants.IgnoreQueryFilters()
                .Where(item => variantIds.Contains(item.Id))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.Products.IgnoreQueryFilters()
                .Where(item => productIds.Contains(item.Id))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.AccountRoles.IgnoreQueryFilters()
                .Where(item => item.OrganizationId == organization.Id)
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.Organizations.IgnoreQueryFilters()
                .Where(item => item.Id == organization.Id)
                .ExecuteDeleteAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        foreach (var objectKey in objectKeys.Distinct(StringComparer.Ordinal))
        {
            try
            {
                await objectStorage.DeleteIfExistsAsync(objectKey, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception,
                    "Retained orphaned automation fixture object {StorageKey}; orphan cleanup can retry it.", objectKey);
            }
        }

        logger.LogInformation("Deleted legacy development fixture organization {OrganizationCode}.",
            AutomationFixtureOrganizationCode);
        return true;
    }

    private async Task EnsureNoRuntimeEvidenceAsync(
        Guid organizationId,
        IReadOnlyCollection<Guid> releaseIds,
        IReadOnlyCollection<Guid> menuItemIds,
        CancellationToken cancellationToken)
    {
        var hasRuntimeEvidence =
            await dbContext.Orders.IgnoreQueryFilters()
                .AnyAsync(order => order.OrganizationId == organizationId, cancellationToken) ||
            await dbContext.KioskConfigurationDeployments.IgnoreQueryFilters()
                .AnyAsync(deployment => releaseIds.Contains(deployment.ConfigurationReleaseId), cancellationToken) ||
            await dbContext.ControllerArtifactSetDeployments.IgnoreQueryFilters()
                .AnyAsync(deployment => releaseIds.Contains(deployment.SourceConfigurationReleaseId), cancellationToken) ||
            await dbContext.OrderExecutionRecords.IgnoreQueryFilters()
                .AnyAsync(record => releaseIds.Contains(record.SourceConfigurationReleaseId), cancellationToken) ||
            await dbContext.KioskExecutionEndpoints.IgnoreQueryFilters()
                .AnyAsync(endpoint => endpoint.ActiveConfigurationReleaseId.HasValue &&
                                      releaseIds.Contains(endpoint.ActiveConfigurationReleaseId.Value), cancellationToken) ||
            await dbContext.ProductionPackageInstallations.IgnoreQueryFilters()
                .AnyAsync(installation => installation.DraftConfigurationReleaseId.HasValue &&
                                          releaseIds.Contains(installation.DraftConfigurationReleaseId.Value), cancellationToken) ||
            await dbContext.ProductionPackageUpgradeEndpointTargets.IgnoreQueryFilters()
                .AnyAsync(target => releaseIds.Contains(target.SourceConfigurationReleaseId), cancellationToken) ||
            await dbContext.ProductionPackageUpgradeMenuChanges.IgnoreQueryFilters()
                .AnyAsync(change => menuItemIds.Contains(change.MenuItemId), cancellationToken);

        if (hasRuntimeEvidence)
        {
            throw new InvalidOperationException(
                $"{OrganizationCode} has order, deployment, execution, active-release, or package evidence. " +
                "Reset refuses to erase runtime or commercial history.");
        }
    }

    private async Task SeedVanillaBaselineAsync(
        Organization organization,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var template = await dbContext.Products
            .AsSplitQuery()
            .Include(product => product.ProductVariants)
                .ThenInclude(variant => variant.Recipes)
                    .ThenInclude(recipe => recipe.RecipeItems)
            .SingleOrDefaultAsync(product => product.OrganizationId == null && product.StoreId == null &&
                product.KioskId == null && product.Code == ProductCode, cancellationToken)
            ?? throw new InvalidOperationException(
                "The global vanilla soft-serve template is missing. Start the backend once so the catalog template seed can complete.");

        var product = new Product
        {
            OrganizationId = organization.Id,
            TemplateProductId = template.Id,
            ScopeType = TenantScopeType.Organization,
            CategoryId = template.CategoryId,
            Code = template.Code,
            Name = template.Name,
            DisplayName = template.DisplayName,
            Description = template.Description,
            ProductType = template.ProductType,
            BasePrice = template.BasePrice,
            Currency = template.Currency,
            IsAvailable = false,
            PreparationTimeSeconds = template.PreparationTimeSeconds,
            ImageAssetId = template.ImageAssetId,
            ImageAltText = template.ImageAltText,
            MetadataJson = template.MetadataJson,
            CreatedAt = now
        };

        foreach (var sourceVariant in template.ProductVariants)
        {
            var variant = new ProductVariant
            {
                Code = sourceVariant.Code,
                Name = sourceVariant.Name,
                DisplayName = sourceVariant.DisplayName,
                Description = sourceVariant.Description,
                VariantType = sourceVariant.VariantType,
                FulfillmentType = sourceVariant.FulfillmentType,
                SizeCode = sourceVariant.SizeCode,
                BasePrice = sourceVariant.BasePrice,
                Currency = sourceVariant.Currency,
                IsAvailable = false,
                DisplayOrder = sourceVariant.DisplayOrder,
                PreparationTimeSeconds = sourceVariant.PreparationTimeSeconds,
                ImageAssetId = sourceVariant.ImageAssetId,
                ImageAltText = sourceVariant.ImageAltText,
                MetadataJson = sourceVariant.MetadataJson,
                CreatedAt = now
            };

            foreach (var sourceRecipe in sourceVariant.Recipes)
            {
                var recipe = new Recipe
                {
                    OrganizationId = organization.Id,
                    TemplateRecipeId = sourceRecipe.Id,
                    ScopeType = TenantScopeType.Organization,
                    Code = sourceRecipe.Code,
                    Name = sourceRecipe.Name,
                    Version = sourceRecipe.Version,
                    Status = sourceRecipe.Status,
                    IsDefault = sourceRecipe.IsDefault,
                    YieldQuantity = sourceRecipe.YieldQuantity,
                    Unit = sourceRecipe.Unit,
                    EstimatedDurationSeconds = sourceRecipe.EstimatedDurationSeconds,
                    EffectiveFrom = sourceRecipe.EffectiveFrom,
                    EffectiveTo = sourceRecipe.EffectiveTo,
                    InstructionsSchemaVersion = sourceRecipe.InstructionsSchemaVersion,
                    InstructionsJson = sourceRecipe.InstructionsJson,
                    CreatedAt = now
                };
                foreach (var sourceItem in sourceRecipe.RecipeItems)
                {
                    recipe.RecipeItems.Add(new RecipeItem
                    {
                        IngredientId = sourceItem.IngredientId,
                        Quantity = sourceItem.Quantity,
                        Unit = sourceItem.Unit,
                        StepOrder = sourceItem.StepOrder,
                        IsOptional = sourceItem.IsOptional,
                        Notes = sourceItem.Notes,
                        CreatedAt = now
                    });
                }

                variant.Recipes.Add(recipe);
            }

            product.ProductVariants.Add(variant);
        }

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
