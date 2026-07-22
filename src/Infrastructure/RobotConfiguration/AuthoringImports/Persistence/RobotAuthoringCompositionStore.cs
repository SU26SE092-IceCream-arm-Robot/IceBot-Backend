using Application.RobotConfiguration.AuthoringImports.Composition;
using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using Domain.RobotConfiguration.ArtifactContracts;
using Domain.RobotConfiguration.Artifacts;
using Domain.RobotConfiguration.Programs;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.RobotConfiguration.AuthoringImports.Persistence;

public sealed class RobotAuthoringCompositionStore(IceBotDbContext dbContext) : IRobotAuthoringCompositionStore
{
    public Task<Recipe?> GetRecipeAsync(Guid organizationId, Guid recipeId, CancellationToken cancellationToken) =>
        dbContext.Recipes.AsNoTracking()
            .Include(recipe => recipe.RecipeItems).ThenInclude(item => item.Ingredient)
            .Include(recipe => recipe.ProductVariant).ThenInclude(variant => variant.Product)
                .ThenInclude(product => product.OptionGroups).ThenInclude(group => group.ProductOptions)
                .ThenInclude(option => option.IngredientRequirements).ThenInclude(requirement => requirement.Ingredient)
            .FirstOrDefaultAsync(recipe => recipe.Id == recipeId && recipe.DeletedAt == null &&
                recipe.ProductVariant.FulfillmentType == FulfillmentType.MachineProduced &&
                (recipe.Status == RecipeStatus.Published || recipe.Status == RecipeStatus.Active) &&
                (!recipe.OrganizationId.HasValue || recipe.OrganizationId == organizationId) &&
                (!recipe.ProductVariant.Product.OrganizationId.HasValue ||
                 recipe.ProductVariant.Product.OrganizationId == organizationId), cancellationToken);

    public Task<RobotProgram?> GetProgramAsync(Guid organizationId, Guid programId, CancellationToken cancellationToken) =>
        dbContext.RobotPrograms.AsNoTracking().Include(program => program.RobotProgramArtifacts)
            .FirstOrDefaultAsync(program => program.Id == programId && program.OrganizationId == organizationId,
                cancellationToken);

    public async Task<IReadOnlyList<RobotArtifact>> GetArtifactsAsync(Guid organizationId,
        IReadOnlyCollection<Guid> artifactIds, CancellationToken cancellationToken) =>
        await dbContext.RobotArtifacts.AsNoTracking()
            .Where(artifact => artifact.OrganizationId == organizationId && artifactIds.Contains(artifact.Id))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RobotArtifactTechnicalContract>> GetContractsAsync(Guid organizationId,
        IReadOnlyCollection<Guid> contractIds, CancellationToken cancellationToken) =>
        await dbContext.RobotArtifactTechnicalContracts.AsNoTracking()
            .Include(contract => contract.Effects).Include(contract => contract.OrderingConstraints)
            .Where(contract => contract.OrganizationId == organizationId && contractIds.Contains(contract.Id))
            .ToListAsync(cancellationToken);
}
