using Application.RobotConfiguration.AuthoringImports.RecipeSuggestions;
using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using Domain.RobotConfiguration.ArtifactContracts;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.RobotConfiguration.AuthoringImports.Persistence;

public sealed class RobotAuthoringRecipeSuggestionStore(IceBotDbContext dbContext) : IRobotAuthoringRecipeSuggestionStore
{
    public async Task<IReadOnlyList<Recipe>> ListEligibleRecipesAsync(
        Guid organizationId,
        CancellationToken cancellationToken) =>
        await dbContext.Recipes.AsNoTracking()
            .Include(recipe => recipe.RecipeItems).ThenInclude(item => item.Ingredient)
            .Include(recipe => recipe.ProductVariant).ThenInclude(variant => variant.Product)
            .Where(recipe => recipe.DeletedAt == null &&
                recipe.OrganizationId == organizationId &&
                recipe.ProductVariant.FulfillmentType == FulfillmentType.MachineProduced &&
                (recipe.Status == RecipeStatus.Published || recipe.Status == RecipeStatus.Active) &&
                recipe.ProductVariant.Product.DeletedAt == null &&
                recipe.ProductVariant.Product.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RobotArtifactTechnicalContract>> GetContractsAsync(
        Guid organizationId,
        IReadOnlyCollection<Guid> contractIds,
        CancellationToken cancellationToken) =>
        await dbContext.RobotArtifactTechnicalContracts.AsNoTracking()
            .Include(contract => contract.Effects).Include(contract => contract.OrderingConstraints)
            .Where(contract => contract.OrganizationId == organizationId && contractIds.Contains(contract.Id))
            .ToListAsync(cancellationToken);
}
