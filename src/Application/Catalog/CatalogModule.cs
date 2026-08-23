using Application.Catalog.Products.Commands;
using Application.Catalog.Products.Queries;
using Application.Catalog.Ingredients.Commands;
using Application.Catalog.Ingredients.Queries;
using Application.Catalog.ProductCategories.Commands;
using Application.Catalog.ProductCategories.Queries;
using Application.Catalog.Recipes.Commands;
using Application.Catalog.Recipes.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Catalog;

public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        services.AddScoped<ListProductsQueryHandler>();
        services.AddScoped<GetProductQueryHandler>();
        services.AddScoped<CreateProductCommandHandler>();
        services.AddScoped<CloneProductTemplateCommandHandler>();
        services.AddScoped<UpdateProductCommandHandler>();
        services.AddScoped<SetProductAvailabilityCommandHandler>();
        services.AddScoped<DeleteProductCommandHandler>();
        services.AddScoped<AddProductVariantCommandHandler>();
        services.AddScoped<UpdateProductVariantCommandHandler>();
        services.AddScoped<SetProductVariantAvailabilityCommandHandler>();
        services.AddScoped<DeleteProductVariantCommandHandler>();
        services.AddScoped<ReplaceCatalogImageCommandHandler>();
        services.AddScoped<ListProductCategoriesQueryHandler>();
        services.AddScoped<CreateProductCategoryCommandHandler>();
        services.AddScoped<UpdateProductCategoryCommandHandler>();
        services.AddScoped<SetProductCategoryStatusCommandHandler>();
        services.AddScoped<DeleteProductCategoryCommandHandler>();
        services.AddScoped<CreateOptionGroupCommandHandler>();
        services.AddScoped<UpdateOptionGroupCommandHandler>();
        services.AddScoped<SetOptionGroupStatusCommandHandler>();
        services.AddScoped<DeleteOptionGroupCommandHandler>();
        services.AddScoped<CreateProductOptionCommandHandler>();
        services.AddScoped<UpdateProductOptionCommandHandler>();
        services.AddScoped<SetProductOptionAvailabilityCommandHandler>();
        services.AddScoped<DeleteProductOptionCommandHandler>();
        services.AddScoped<ReplaceProductOptionIngredientRequirementsCommandHandler>();
        services.AddScoped<ListIngredientsQueryHandler>();
        services.AddScoped<GetIngredientQueryHandler>();
        services.AddScoped<CreateIngredientCommandHandler>();
        services.AddScoped<UpdateIngredientCommandHandler>();
        services.AddScoped<SetIngredientStatusCommandHandler>();
        services.AddScoped<DeleteIngredientCommandHandler>();
        services.AddScoped<ListRecipesQueryHandler>();
        services.AddScoped<GetRecipeQueryHandler>();
        services.AddScoped<CreateRecipeCommandHandler>();
        services.AddScoped<UpdateRecipeCommandHandler>();
        services.AddScoped<ReplaceRecipeItemsCommandHandler>();
        services.AddScoped<SetRecipeStatusCommandHandler>();
        services.AddScoped<CreateRecipeVersionCommandHandler>();

        return services;
    }
}
