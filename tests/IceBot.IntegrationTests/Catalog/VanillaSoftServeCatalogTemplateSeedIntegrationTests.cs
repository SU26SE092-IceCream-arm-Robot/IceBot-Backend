using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using Domain.Common.Enums;
using Domain.Catalog.Entities;
using Domain.Devices.Catalog;
using Domain.Inventory.Enums;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.Catalog.Bootstrap;
using Infrastructure.Data;
using Infrastructure.Devices.Bootstrap;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IceBot.IntegrationTests.Catalog;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class VanillaSoftServeCatalogTemplateSeedIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task FreshDatabase_SeedsTemplateAndDemoRecipeWithOneOperationalMixIngredient_Once()
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
            setup.Stores.Add(new Store
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                OrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Code = "ICEBOT-DEMO-STORE",
                Name = "IceBot Demo Store",
                Status = EntityStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow
            });
            setup.Kiosks.Add(new Kiosk
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                OrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                StoreId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Code = "ICEBOT-DEMO-KIOSK",
                Name = "IceBot Demo Kiosk",
                CreatedAt = DateTimeOffset.UtcNow
            });
            await setup.SaveChangesAsync();
        }

        using var services = CreateServices();
        var seed = CreateSeed(services);
        var topologySeed = CreateTopologySeed(services);

        await seed.StartAsync(CancellationToken.None);
        await seed.StartAsync(CancellationToken.None);
        await topologySeed.StartAsync(CancellationToken.None);
        await topologySeed.StartAsync(CancellationToken.None);

        await using var assertion = fixture.CreateDbContext();
        var ingredients = await assertion.Ingredients
            .Where(ingredient => ingredient.Code == "VANILLA-SOFT-SERVE-MIX")
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

        Assert.Single(ingredients);
        Assert.Equal("Kem tuoi", category.Name);
        Assert.Equal(2, products.Count);
        Assert.All(products, product => Assert.Equal(category.Id, product.CategoryId));
        Assert.Equal(2, recipes.Count);
        Assert.Contains(recipes, recipe => recipe.OrganizationId is null);
        Assert.Contains(recipes, recipe => recipe.OrganizationId == Guid.Parse("11111111-1111-1111-1111-111111111111"));
        Assert.All(recipes, recipe =>
        {
            Assert.Single(recipe.RecipeItems);
            Assert.All(recipe.RecipeItems, item =>
            {
                Assert.NotEqual(Guid.Empty, item.IngredientId);
                Assert.NotNull(item.Ingredient);
                Assert.Equal(item.IngredientId, item.Ingredient.Id);
                Assert.Equal("VANILLA-SOFT-SERVE-MIX", item.Ingredient.Code);
                Assert.Equal(80m, item.Quantity);
            });
        });

        Assert.Equal(1, await assertion.Products.CountAsync(product =>
            product.OrganizationId == Guid.Parse("11111111-1111-1111-1111-111111111111") &&
            product.Code == "KEM-TUOI-VANI"));

        var state = await assertion.IngredientDispenserStates
            .Include(candidate => candidate.Device)
                .ThenInclude(candidate => candidate.DeviceModel)
            .SingleAsync(candidate => candidate.ContainerCode == "MIX_HOPPER");
        Assert.Equal("ICEBOT-DEMO-SOFT-SERVE-MACHINE", state.Device.Code);
        Assert.Equal("SOFT-SERVE-HOPPER-V1", state.Device.DeviceModel!.Code);
        Assert.Equal(InventoryTrackingMode.ManualEstimate, state.TrackingMode);
        Assert.Equal(6000m, state.EstimatedQuantity);
        Assert.Equal(6000m, state.CapacityQuantity);
        Assert.NotEqual(Guid.Empty, state.KioskIngredientInventoryId);
        Assert.Equal(1, await assertion.StockMovements.CountAsync(candidate =>
            candidate.KioskIngredientInventoryId == state.KioskIngredientInventoryId &&
            candidate.ReasonCode == "DEVELOPMENT_INITIAL_STOCK"));
    }

    private ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IceBotDbContext>(_ => fixture.CreateDbContext());
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
        return services.BuildServiceProvider();
    }

    private static VanillaSoftServeCatalogTemplateSeedHostedService CreateSeed(IServiceProvider services)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DemoCatalogSeed:IceBotDemoEnabled"] = "true"
            })
            .Build();
        return new VanillaSoftServeCatalogTemplateSeedHostedService(
            services.GetRequiredService<IServiceScopeFactory>(),
            configuration,
            services.GetRequiredService<IHostEnvironment>(),
            services.GetRequiredService<ILogger<VanillaSoftServeCatalogTemplateSeedHostedService>>());
    }

    private static DevelopmentVanillaSoftServeTopologySeedHostedService CreateTopologySeed(IServiceProvider services)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DemoCatalogSeed:IceBotDemoEnabled"] = "true",
                ["DemoCatalogSeed:SeedInventoryTopology"] = "true"
            })
            .Build();
        return new DevelopmentVanillaSoftServeTopologySeedHostedService(
            services.GetRequiredService<IServiceScopeFactory>(),
            configuration,
            services.GetRequiredService<IHostEnvironment>(),
            services.GetRequiredService<ILogger<DevelopmentVanillaSoftServeTopologySeedHostedService>>());
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "IceBot.IntegrationTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
