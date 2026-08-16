using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Catalog.Bootstrap;

/// <summary>
/// Ensures the global vanilla soft-serve catalog template exists in every environment.
/// It only creates missing template data or repairs its category reference; it never
/// creates tenant, kiosk, inventory, or account data outside Development.
/// </summary>
public sealed class VanillaSoftServeCatalogTemplateSeedHostedService : IHostedService
{
    private const string DevelopmentOrganizationCode = "ICEBOT-DEMO";
    private const string CategoryCode = "SOFT-SERVE";
    private const string CategoryName = "Kem tuoi";
    private const string ProductCode = "KEM-TUOI-VANI";
    private const string ProductName = "Kem tuoi vi vani";
    private const string VariantCode = "80G";
    private const string VariantName = "Kem tuoi vani 80 g";
    private const string RecipeCode = "KEM-TUOI-VANI-80G-V1";
    private const string RecipeName = "Kem tuoi vani 80 g";
    private const string OperationalMixIngredientCode = "VANILLA-SOFT-SERVE-MIX";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<VanillaSoftServeCatalogTemplateSeedHostedService> _logger;

    public VanillaSoftServeCatalogTemplateSeedHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ILogger<VanillaSoftServeCatalogTemplateSeedHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IceBotDbContext>();
        var now = DateTimeOffset.UtcNow;
        var category = await EnsureSoftServeCategoryAsync(dbContext, now, cancellationToken);
        var ingredients = await EnsureIngredientsAsync(dbContext, now, cancellationToken);
        var template = await dbContext.Products
            .WhereNotDeleted()
            .AsSplitQuery()
            .Include(candidate => candidate.ProductVariants)
                .ThenInclude(candidate => candidate.Recipes)
                    .ThenInclude(candidate => candidate.RecipeItems)
            .SingleOrDefaultAsync(candidate =>
                candidate.OrganizationId == null &&
                candidate.StoreId == null &&
                candidate.KioskId == null &&
                candidate.Code == ProductCode,
                cancellationToken);

        var templateNameMatches = await dbContext.Products
            .WhereNotDeleted()
            .Where(candidate =>
                candidate.OrganizationId == null &&
                candidate.StoreId == null &&
                candidate.KioskId == null &&
                candidate.Name == ProductName)
            .ToListAsync(cancellationToken);
        EnsureNoNameCollision(
            "global product template",
            ProductCode,
            ProductName,
            template,
            templateNameMatches,
            candidate => candidate.Id,
            candidate => candidate.Code);

        if (template is null)
        {
            template = new Product
            {
                ScopeType = TenantScopeType.Global,
                CategoryId = category.Id,
                Code = ProductCode,
                Name = ProductName,
                DisplayName = ProductName,
                Description = "Kem tuoi vani 80 g. Seed ky thuat de cau hinh recipe va binding san xuat.",
                ProductType = "IceCream",
                BasePrice = 0m,
                Currency = "VND",
                IsAvailable = false,
                PreparationTimeSeconds = 90,
                MetadataJson = JsonSerializer.Serialize(new
                {
                    source = "Vanilla soft-serve operational mix",
                    servings = 75,
                    servingGrams = 80,
                    inventoryModel = "One pre-mixed hopper consumable",
                    sellingPrice = "not-provided"
                }),
                CreatedAt = now
            };
            dbContext.Products.Add(template);
        }
        else if (template.CategoryId != category.Id)
        {
            template.CategoryId = category.Id;
        }

        var variant = template.ProductVariants.SingleOrDefault(candidate => candidate.Code == VariantCode);
        EnsureNoNameCollision(
            "product template variant",
            VariantCode,
            VariantName,
            variant,
            template.ProductVariants.Where(candidate => candidate.Name == VariantName).ToList(),
            candidate => candidate.Id,
            candidate => candidate.Code);
        if (variant is null)
        {
            variant = new ProductVariant
            {
                ProductId = template.Id,
                Code = VariantCode,
                Name = VariantName,
                DisplayName = VariantName,
                Description = "Mot phan kem tuoi vani 80 g.",
                VariantType = "Serving",
                FulfillmentType = FulfillmentType.MachineProduced,
                SizeCode = "80G",
                BasePrice = 0m,
                Currency = "VND",
                IsAvailable = false,
                DisplayOrder = 1,
                PreparationTimeSeconds = 90,
                MetadataJson = JsonSerializer.Serialize(new
                {
                    servingQuantity = 80,
                    servingUnit = "gram",
                    operationalConsumableCode = OperationalMixIngredientCode
                }),
                CreatedAt = now
            };
            template.ProductVariants.Add(variant);
        }

        var recipe = variant.Recipes.SingleOrDefault(candidate => candidate.Code == RecipeCode && candidate.Version == 1);
        EnsureNoNameCollision(
            "product template recipe",
            RecipeCode,
            RecipeName,
            recipe,
            variant.Recipes.Where(candidate => candidate.Name == RecipeName && candidate.Version == 1).ToList(),
            candidate => candidate.Id,
            candidate => candidate.Code);
        if (recipe is null)
        {
            recipe = CreateRecipe(variant, ingredients, now);
            variant.Recipes.Add(recipe);
        }

        if (_hostEnvironment.IsDevelopment() &&
            _configuration.GetValue<bool>("DevelopmentCatalogSeed:VanillaSoftServeEnabled"))
        {
            await EnsureDevelopmentProductAsync(dbContext, template, category.Id, now, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Ensured global vanilla soft-serve catalog template; development demo materialization enabled: {DevelopmentMaterializationEnabled}.",
            _hostEnvironment.IsDevelopment() &&
            _configuration.GetValue<bool>("DevelopmentCatalogSeed:VanillaSoftServeEnabled"));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task<ProductCategory> EnsureSoftServeCategoryAsync(
        IceBotDbContext dbContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.ProductCategories
            .SingleOrDefaultAsync(candidate => candidate.Code == CategoryCode, cancellationToken);
        var nameMatches = await dbContext.ProductCategories
            .Where(candidate => candidate.Name == CategoryName)
            .ToListAsync(cancellationToken);
        EnsureNoNameCollision(
            "product category",
            CategoryCode,
            CategoryName,
            category,
            nameMatches,
            candidate => candidate.Id,
            candidate => candidate.Code);
        if (category is not null)
        {
            return category;
        }

        category = new ProductCategory
        {
            Code = CategoryCode,
            Name = CategoryName,
            Description = "Kem tuoi va cac san pham soft serve.",
            ProductType = "IceCream",
            DisplayOrder = 1,
            IsActive = true,
            CreatedAt = now
        };
        dbContext.ProductCategories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return category;
    }

    private static async Task<Dictionary<string, Ingredient>> EnsureIngredientsAsync(
        IceBotDbContext dbContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var definitions = new[]
        {
            new IngredientDefinition(
                OperationalMixIngredientCode,
                "Vanilla soft-serve mix",
                "PreMixedSoftServe",
                true,
                true,
                "Store and refill according to the machine and supplier instructions")
        };
        var ingredients = new Dictionary<string, Ingredient>(StringComparer.Ordinal);

        foreach (var definition in definitions)
        {
            var ingredient = await dbContext.Ingredients
                .WhereNotDeleted()
                .SingleOrDefaultAsync(candidate => candidate.Code == definition.Code, cancellationToken);
            var nameMatches = await dbContext.Ingredients
                .WhereNotDeleted()
                .Where(candidate => candidate.Name == definition.Name)
                .ToListAsync(cancellationToken);
            EnsureNoNameCollision(
                "ingredient",
                definition.Code,
                definition.Name,
                ingredient,
                nameMatches,
                candidate => candidate.Id,
                candidate => candidate.Code);
            if (ingredient is not null)
            {
                ingredients.Add(definition.Code, ingredient);
                continue;
            }

            ingredient = new Ingredient
            {
                Code = definition.Code,
                Name = definition.Name,
                IngredientType = definition.IngredientType,
                Unit = "gram",
                StorageRequirement = definition.StorageRequirement,
                IsPerishable = definition.IsPerishable,
                IsAllergen = definition.IsAllergen,
                IsActive = true,
                MetadataJson = JsonSerializer.Serialize(new
                {
                    source = "Vanilla soft-serve operational mix",
                    inventoryModel = "One pre-mixed hopper consumable"
                }),
                CreatedAt = now
            };
            dbContext.Ingredients.Add(ingredient);
            ingredients.Add(definition.Code, ingredient);
        }

        return ingredients;
    }

    private static async Task EnsureDevelopmentProductAsync(
        IceBotDbContext dbContext,
        Product template,
        long categoryId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var organization = await dbContext.Organizations
            .WhereNotDeleted()
            .SingleOrDefaultAsync(candidate => candidate.Code == DevelopmentOrganizationCode, cancellationToken);
        if (organization is null)
        {
            return;
        }

        var existing = await dbContext.Products
            .WhereNotDeleted()
            .SingleOrDefaultAsync(candidate =>
                candidate.OrganizationId == organization.Id &&
                candidate.StoreId == null &&
                candidate.KioskId == null &&
                candidate.Code == ProductCode,
                cancellationToken);
        var nameMatches = await dbContext.Products
            .WhereNotDeleted()
            .Where(candidate =>
                candidate.OrganizationId == organization.Id &&
                candidate.StoreId == null &&
                candidate.KioskId == null &&
                candidate.Name == ProductName)
            .ToListAsync(cancellationToken);
        EnsureNoNameCollision(
            "development organization product",
            ProductCode,
            ProductName,
            existing,
            nameMatches,
            candidate => candidate.Id,
            candidate => candidate.Code);
        if (existing is not null)
        {
            if (existing.CategoryId != categoryId)
            {
                existing.CategoryId = categoryId;
            }

            return;
        }

        var product = new Product
        {
            OrganizationId = organization.Id,
            TemplateProductId = template.Id,
            ScopeType = TenantScopeType.Organization,
            CategoryId = categoryId,
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

        foreach (var templateVariant in template.ProductVariants)
        {
            var variant = new ProductVariant
            {
                ProductId = product.Id,
                Code = templateVariant.Code,
                Name = templateVariant.Name,
                DisplayName = templateVariant.DisplayName,
                Description = templateVariant.Description,
                VariantType = templateVariant.VariantType,
                FulfillmentType = templateVariant.FulfillmentType,
                SizeCode = templateVariant.SizeCode,
                BasePrice = templateVariant.BasePrice,
                Currency = templateVariant.Currency,
                IsAvailable = false,
                DisplayOrder = templateVariant.DisplayOrder,
                PreparationTimeSeconds = templateVariant.PreparationTimeSeconds,
                ImageUrl = templateVariant.ImageUrl,
                MetadataJson = templateVariant.MetadataJson,
                CreatedAt = now
            };

            foreach (var templateRecipe in templateVariant.Recipes)
            {
                var recipe = new Recipe
                {
                    OrganizationId = organization.Id,
                    ProductVariantId = variant.Id,
                    TemplateRecipeId = templateRecipe.Id,
                    ScopeType = TenantScopeType.Organization,
                    Code = templateRecipe.Code,
                    Name = templateRecipe.Name,
                    Version = 1,
                    Status = templateRecipe.Status,
                    IsDefault = templateRecipe.IsDefault,
                    YieldQuantity = templateRecipe.YieldQuantity,
                    Unit = templateRecipe.Unit,
                    EstimatedDurationSeconds = templateRecipe.EstimatedDurationSeconds,
                    EffectiveFrom = templateRecipe.EffectiveFrom,
                    EffectiveTo = templateRecipe.EffectiveTo,
                    InstructionsSchemaVersion = templateRecipe.InstructionsSchemaVersion,
                    InstructionsJson = templateRecipe.InstructionsJson,
                    CreatedAt = now
                };
                foreach (var item in templateRecipe.RecipeItems)
                {
                    recipe.RecipeItems.Add(new RecipeItem
                    {
                        RecipeId = recipe.Id,
                        IngredientId = item.IngredientId,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        StepOrder = item.StepOrder,
                        IsOptional = item.IsOptional,
                        Notes = item.Notes,
                        CreatedAt = now
                    });
                }

                variant.Recipes.Add(recipe);
            }

            product.ProductVariants.Add(variant);
        }

        dbContext.Products.Add(product);
    }

    private static Recipe CreateRecipe(
        ProductVariant variant,
        IReadOnlyDictionary<string, Ingredient> ingredients,
        DateTimeOffset now)
    {
        var recipe = new Recipe
        {
            ScopeType = TenantScopeType.Global,
            ProductVariantId = variant.Id,
            Code = RecipeCode,
            Name = RecipeName,
            Version = 1,
            IsDefault = true,
            YieldQuantity = 1,
            Unit = "serving",
            EstimatedDurationSeconds = 90,
            InstructionsSchemaVersion = 1,
            InstructionsJson = JsonSerializer.Serialize(new
            {
                operationalConsumption = new
                {
                    ingredientCode = OperationalMixIngredientCode,
                    quantity = 80,
                    unit = "gram"
                },
                note = "This recipe models the pre-mixed hopper consumable. The supplier batch formulation is not runtime inventory evidence."
            }),
            CreatedAt = now
        };

        recipe.RecipeItems.Add(new RecipeItem
        {
            IngredientId = ingredients[OperationalMixIngredientCode].Id,
            Ingredient = ingredients[OperationalMixIngredientCode],
            Quantity = 80m,
            Unit = "gram",
            StepOrder = 1,
            Notes = "Operational hopper consumption per 80 g serving.",
            CreatedAt = now
        });

        recipe.Publish(null, now);
        recipe.Activate(null, now);
        return recipe;
    }

    private sealed record IngredientDefinition(
        string Code,
        string Name,
        string IngredientType,
        bool IsPerishable,
        bool IsAllergen,
        string? StorageRequirement);

    private static void EnsureNoNameCollision<TEntity, TKey>(
        string entityName,
        string expectedCode,
        string expectedName,
        TEntity? codeMatch,
        IReadOnlyCollection<TEntity> nameMatches,
        Func<TEntity, TKey> getId,
        Func<TEntity, string> getCode)
        where TEntity : class
        where TKey : notnull
    {
        var conflictingMatches = nameMatches
            .Where(candidate =>
                codeMatch is null ||
                !EqualityComparer<TKey>.Default.Equals(getId(candidate), getId(codeMatch)))
            .ToList();
        if (conflictingMatches.Count == 0)
        {
            return;
        }

        var conflictingCodes = string.Join(", ", conflictingMatches.Select(getCode).OrderBy(code => code, StringComparer.Ordinal));
        var reason = codeMatch is null
            ? "the expected code is absent"
            : "the expected code resolves to a different record";
        throw new InvalidOperationException(
            $"Cannot seed {entityName}. Name '{expectedName}' already exists with code(s) {conflictingCodes}, while expected code is '{expectedCode}' ({reason}). Reconcile the existing data before restarting.");
    }
}
