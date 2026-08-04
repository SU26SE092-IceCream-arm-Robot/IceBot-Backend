using Application.ProductionConfiguration.Bindings;
using Domain.Catalog.Entities;
using Domain.ProductionConfiguration.Entities;
using Domain.RobotConfiguration.Programs;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Infrastructure.ProductionConfiguration.Persistence.Bindings;

public sealed class ProductionProgramBindingStore(IceBotDbContext db) : IProductionProgramBindingStore
{
    public Task<Recipe?> GetRecipeAsync(Guid recipeId, CancellationToken cancellationToken) =>
        db.Recipes.Include(recipe => recipe.ProductVariant).ThenInclude(variant => variant.Product)
            .ThenInclude(product => product.OptionGroups).ThenInclude(group => group.ProductOptions)
            .SingleOrDefaultAsync(recipe => recipe.Id == recipeId && recipe.DeletedAt == null, cancellationToken);

    public Task<RobotProgram?> GetRobotProgramAsync(Guid robotProgramId, CancellationToken cancellationToken) =>
        db.RobotPrograms.SingleOrDefaultAsync(program => program.Id == robotProgramId && program.DeletedAt == null, cancellationToken);

    public async Task<ProductionProgramBinding?> FindActiveEquivalentAsync(Guid organizationId, Guid recipeId, Guid robotProgramId,
        string requiredWorkcellCapabilityCode, IReadOnlyCollection<string> supportedOptionCodes, CancellationToken cancellationToken)
    {
        var requestedJson = JsonSerializer.Serialize(supportedOptionCodes.Select(code => code.Trim().ToUpperInvariant())
            .Order(StringComparer.Ordinal).ToArray());
        return await db.ProductionProgramBindings.SingleOrDefaultAsync(binding =>
            binding.OrganizationId == organizationId && binding.RecipeId == recipeId && binding.RobotProgramId == robotProgramId &&
            binding.RequiredWorkcellCapabilityCode == requiredWorkcellCapabilityCode && binding.SupportedOptionCodesJson == requestedJson &&
            binding.Status == ProductionProgramBindingStatus.Active && binding.DeletedAt == null, cancellationToken);
    }

    public Task<ProductionProgramBinding?> GetAsync(Guid organizationId, Guid bindingId, CancellationToken cancellationToken) =>
        db.ProductionProgramBindings.SingleOrDefaultAsync(binding => binding.Id == bindingId && binding.OrganizationId == organizationId && binding.DeletedAt == null, cancellationToken);

    public async Task<IReadOnlyList<ProductionProgramBinding>> ListAsync(Guid organizationId, ProductionProgramBindingStatus? status,
        CancellationToken cancellationToken) =>
        await db.ProductionProgramBindings.AsNoTracking().Where(binding => binding.OrganizationId == organizationId && binding.DeletedAt == null &&
            (!status.HasValue || binding.Status == status.Value)).OrderByDescending(binding => binding.CreatedAt).ToListAsync(cancellationToken);

    public Task AddAsync(ProductionProgramBinding binding, CancellationToken cancellationToken)
    {
        db.ProductionProgramBindings.Add(binding);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
