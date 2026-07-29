using Application.ProductionConfiguration.Routes.Abstractions;
using Domain.Catalog.Entities;
using Domain.ProductionConfiguration.Entities;
using Domain.RobotConfiguration.Programs;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.ProductionConfiguration.Persistence.Routes;

public sealed class ConfigurationRouteStore : IConfigurationRouteStore
{
    private readonly IceBotDbContext _dbContext;

    public ConfigurationRouteStore(IceBotDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<ProductVariant>> ListProductVariantsByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default) =>
        await _dbContext.ProductVariants.AsNoTracking()
            .Include(variant => variant.Product)
                .ThenInclude(product => product.OptionGroups)
                    .ThenInclude(group => group.ProductOptions)
            .Where(variant => ids.Contains(variant.Id))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Recipe>> ListRecipesByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default) =>
        await _dbContext.Recipes.AsNoTracking()
            .Where(recipe => ids.Contains(recipe.Id))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RobotProgram>> ListRobotProgramsByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default) =>
        await _dbContext.RobotPrograms.AsNoTracking()
            .Where(program => ids.Contains(program.Id))
            .ToListAsync(cancellationToken);

    public async Task SaveReleaseReplacementAsync(
        IReadOnlyCollection<ExecutionRoute> removedRoutes,
        CancellationToken cancellationToken = default)
    {
        var routes = removedRoutes.ToArray();
        _dbContext.ExecutionRouteRobotBindings.RemoveRange(routes.SelectMany(route => route.RobotBindings));
        _dbContext.ExecutionRoutes.RemoveRange(routes);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
