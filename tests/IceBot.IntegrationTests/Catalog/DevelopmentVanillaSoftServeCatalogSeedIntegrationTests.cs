using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using Domain.Common.Enums;
using Domain.Catalog.Entities;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.Catalog.Bootstrap;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IceBot.IntegrationTests.Catalog;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class DevelopmentVanillaSoftServeCatalogSeedIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task FreshDatabase_SeedsTemplateAndDemoRecipeWithValidIngredientForeignKeys_Once()
    {
        await using (var setup = fixture.CreateDbContext())
        {
            setup.Organizations.Add(new Organization
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Code = "ICEBOT-DEMO",
                Name = "IceBot Demo",
                Status = EntityStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await setup.SaveChangesAsync();
        }

        using var services = CreateServices();
        var seed = CreateSeed(services);

        await seed.StartAsync(CancellationToken.None);
        await seed.StartAsync(CancellationToken.None);

        await using var assertion = fixture.CreateDbContext();
        var ingredients = await assertion.Ingredients
            .Where(ingredient => ingredient.Code == "FRESH-MILK" ||
                                 ingredient.Code == "COMPRITAL-SOFT-PREMIUM" ||
                                 ingredient.Code == "PURIFIED-WATER")
            .ToListAsync();
        var recipes = await assertion.Recipes
            .Include(recipe => recipe.RecipeItems)
                .ThenInclude(item => item.Ingredient)
            .Where(recipe => recipe.Code == "KEM-TUOI-VANI-80G-V1")
            .ToListAsync();
        var category = await assertion.ProductCategories
            .SingleAsync(candidate => candidate.Code == "SOFT-SERVE");
        var products = await assertion.Products
            .Where(product => product.Code == "KEM-TUOI-VANI")
            .ToListAsync();

        Assert.Equal(3, ingredients.Count);
        Assert.Equal("Kem tuoi", category.Name);
        Assert.Equal(2, products.Count);
        Assert.All(products, product => Assert.Equal(category.Id, product.CategoryId));
        Assert.Equal(2, recipes.Count);
        Assert.Contains(recipes, recipe => recipe.OrganizationId is null);
        Assert.Contains(recipes, recipe => recipe.OrganizationId == Guid.Parse("11111111-1111-1111-1111-111111111111"));
        Assert.All(recipes, recipe =>
        {
            Assert.Equal(3, recipe.RecipeItems.Count);
            Assert.All(recipe.RecipeItems, item =>
            {
                Assert.NotEqual(Guid.Empty, item.IngredientId);
                Assert.NotNull(item.Ingredient);
                Assert.Equal(item.IngredientId, item.Ingredient.Id);
            });
        });

        Assert.Equal(1, await assertion.Products.CountAsync(product =>
            product.OrganizationId == Guid.Parse("11111111-1111-1111-1111-111111111111") &&
            product.Code == "KEM-TUOI-VANI"));
    }

    private ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IceBotDbContext>(_ => fixture.CreateDbContext());
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
        return services.BuildServiceProvider();
    }

    private static DevelopmentVanillaSoftServeCatalogSeedHostedService CreateSeed(IServiceProvider services)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DevelopmentCatalogSeed:VanillaSoftServeEnabled"] = "true"
            })
            .Build();
        return new DevelopmentVanillaSoftServeCatalogSeedHostedService(
            services.GetRequiredService<IServiceScopeFactory>(),
            configuration,
            services.GetRequiredService<IHostEnvironment>(),
            services.GetRequiredService<ILogger<DevelopmentVanillaSoftServeCatalogSeedHostedService>>());
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "IceBot.IntegrationTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
