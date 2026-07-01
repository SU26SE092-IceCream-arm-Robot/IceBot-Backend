using Application.Catalog.Products.Commands;
using Application.Catalog.Products.Queries;
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

        return services;
    }
}
