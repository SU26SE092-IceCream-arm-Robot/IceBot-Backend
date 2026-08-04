using Application.RobotConfiguration.Storage.Abstractions;
using Domain.Catalog.Entities;
using Domain.Identity.Entities;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Catalog.Bootstrap;

public sealed record DevelopmentRobotAuthoringAutomationResetResult(
    Guid OrganizationId,
    int DeletedImportCount,
    int DeletedArtifactCount,
    int DeletedProgramCount,
    int DeletedContractCount,
    int DeletedObjectCount,
    int RetainedObjectCount);

/// <summary>
/// Destructive local-only fixture reset for the isolated robot authoring tenant.
/// It deliberately refuses to delete data once the tenant has a release or menu.
/// </summary>
public sealed class DevelopmentRobotAuthoringAutomationReset(
    IceBotDbContext dbContext,
    IArtifactObjectStorage objectStorage,
    ILogger<DevelopmentRobotAuthoringAutomationReset> logger)
{
    public const string OrganizationCode = "ICEBOT-AUTOMATION-TEST";
    private const string ProductCode = "KEM-TUOI-VANI";

    public async Task<DevelopmentRobotAuthoringAutomationResetResult> ResetAsync(
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var organization = await EnsureOrganizationAsync(now, cancellationToken);
        await EnsureLocalOrgAdminScopeAsync(organization.Id, now, cancellationToken);

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

        await EnsureNoOperationalReferencesAsync(organization.Id, variantIds, recipeIds, cancellationToken);

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

        await using (var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            // ExecuteDelete bypasses EF's tracked cascade behavior; remove the RESTRICT child rows explicitly.
            await dbContext.RobotAuthoringImportItems.IgnoreQueryFilters()
                .Where(item => importIds.Contains(item.RobotAuthoringImportId))
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.RobotAuthoringImports.IgnoreQueryFilters()
                .Where(importSession => importSession.OrganizationId == organization.Id)
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.RobotProgramArtifacts.IgnoreQueryFilters()
                .Where(item => item.RobotProgram.OrganizationId == organization.Id)
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
            "Reset local robot authoring automation fixture {OrganizationCode}: {Imports} imports, {Artifacts} artifacts, {Programs} programs, and {Contracts} contracts removed.",
            OrganizationCode, importCount, artifactCount, programCount, contractCount);

        return new DevelopmentRobotAuthoringAutomationResetResult(
            organization.Id, importCount, artifactCount, programCount, contractCount, deletedObjects, retainedObjects);
    }

    private async Task<Organization> EnsureOrganizationAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var organization = await dbContext.Organizations.IgnoreQueryFilters()
            .SingleOrDefaultAsync(candidate => candidate.Code == OrganizationCode, cancellationToken);
        if (organization is not null)
        {
            organization.DeletedAt = null;
            organization.DeletedByAccountId = null;
            organization.Status = Domain.Common.Enums.EntityStatus.Active;
            await dbContext.SaveChangesAsync(cancellationToken);
            return organization;
        }

        organization = new Organization
        {
            Code = OrganizationCode,
            Name = "IceBot Automation Test",
            LegalName = "Local development fixture only",
            Status = Domain.Common.Enums.EntityStatus.Active,
            MetadataJson = "{\"purpose\":\"local-robot-authoring-automation-test\"}",
            CreatedAt = now
        };
        dbContext.Organizations.Add(organization);
        await dbContext.SaveChangesAsync(cancellationToken);
        return organization;
    }

    private async Task EnsureLocalOrgAdminScopeAsync(Guid organizationId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var account = await dbContext.Accounts
            .Include(candidate => candidate.AccountRoles)
            .SingleOrDefaultAsync(candidate => candidate.Email == "orgadmin@icebot.local", cancellationToken);
        var role = await dbContext.Roles.SingleOrDefaultAsync(candidate => candidate.Code == "OrgAdmin", cancellationToken);
        if (account is null || role is null || account.AccountRoles.Any(assignment =>
                assignment.RoleId == role.Id && assignment.OrganizationId == organizationId &&
                assignment.StoreId is null && assignment.KioskId is null && assignment.IsActive))
            return;

        account.AccountRoles.Add(new AccountRole
        {
            RoleId = role.Id,
            OrganizationId = organizationId,
            IsActive = true,
            AssignedAt = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureNoOperationalReferencesAsync(
        Guid organizationId,
        IReadOnlyCollection<Guid> variantIds,
        IReadOnlyCollection<Guid> recipeIds,
        CancellationToken cancellationToken)
    {
        if (await dbContext.ConfigurationReleases.IgnoreQueryFilters()
                .AnyAsync(release => release.OrganizationId == organizationId, cancellationToken) ||
            await dbContext.MenuItems.IgnoreQueryFilters()
                .AnyAsync(item => variantIds.Contains(item.ProductVariantId) ||
                                  (item.RecipeId.HasValue && recipeIds.Contains(item.RecipeId.Value)), cancellationToken))
        {
            throw new InvalidOperationException(
                $"{OrganizationCode} has release or menu references. Reset is allowed only before publication or operational use.");
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
                "The global vanilla soft-serve template is missing. Start the Development backend once with DevelopmentCatalogSeed:VanillaSoftServeEnabled=true.");

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
            ImageUrl = template.ImageUrl,
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
                ImageUrl = sourceVariant.ImageUrl,
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
